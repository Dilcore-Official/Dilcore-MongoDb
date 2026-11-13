using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Extensions;
using Dilcore.MongoDB.Abstractions.Helpers;
using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Internal;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories;

internal class GenericMongoDbBulkRepository<TDocument> : BaseMongoDbRepository<TDocument>, IGenericBulkRepository<TDocument>
    where TDocument : class, IDocumentEntity
{
    private const int DefaultMaxOperationsPerBatch = 500;
    private const int MaxEstimatedBatchBytes = 12 * 1024 * 1024;

    private static readonly bool HasConcurrencyToken =
        typeof(IHasConcurrencyToken).IsAssignableFrom(typeof(TDocument));

    private static readonly bool IsSoftDeletable =
        typeof(ISoftDeletable).IsAssignableFrom(typeof(TDocument));

    private static readonly bool IsAuditable =
        typeof(IAuditableDocument).IsAssignableFrom(typeof(TDocument));

    private static readonly IDocumentIdAccessor<TDocument> IdAccessor =
        DocumentIdAccessorCache.Get<TDocument>();

    private readonly MongoCallContext _callContext;

    public GenericMongoDbBulkRepository(
        Action<GetCollectionOptions<TDocument>> options,
        Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> collectionProvider,
        MongoCallContext? callContext = null)
        : base(options, collectionProvider)
    {
        _callContext = callContext ?? MongoCallContext.None;
        Session = _callContext.Session;
        Budget = _callContext.Budget;
    }

    public Task<Result<IReadOnlyList<TDocument>>> BulkStoreAsync(
        TDocument[] entities,
        CancellationToken cancellationToken = default)
        => BulkStoreRangeAsync(entities, new MongoBulkWriteOptions(), cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> BulkStoreAsync(
        TDocument[] entities,
        MongoBulkWriteOptions options,
        CancellationToken cancellationToken = default)
        => BulkStoreRangeAsync(entities, options, cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> BulkStoreRangeAsync(
        IEnumerable<TDocument> entities,
        CancellationToken cancellationToken = default)
        => BulkStoreRangeAsync(entities, new MongoBulkWriteOptions(), cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> BulkStoreRangeAsync(
        IEnumerable<TDocument> entities,
        MongoBulkWriteOptions options,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            var entityList = entities as IReadOnlyList<TDocument> ?? entities.ToList();
            if (entityList.Count == 0)
            {
                return Result.Ok<IReadOnlyList<TDocument>>(entityList);
            }

            var snapshots = entityList.Select(Capture).ToList();
            var writeModels = CreateWriteModels(entityList).ToList();
            var batches = Chunk(writeModels, options);
            var bulkOptions = new BulkWriteOptions { IsOrdered = options.IsOrdered };
            var processed = 0;

            try
            {
                foreach (var batch in batches)
                {
                    if (Budget is not null)
                    {
                        foreach (var model in batch)
                        {
                            var reservation = Budget.Reserve(EstimateBytes(model));
                            if (reservation.IsFailed)
                            {
                                RestoreUncommitted(snapshots, processed, entityList.Count);
                                return Result.Fail<IReadOnlyList<TDocument>>(reservation.Errors);
                            }
                        }
                    }

                    try
                    {
                        var result = await MongoCollectionCalls.BulkWriteAsync(
                            collection, _callContext.Session, batch, bulkOptions, token);

                        if (result.RequestCount != batch.Count)
                        {
                            RestoreUncommitted(snapshots, processed, entityList.Count);
                            return Result.Fail<IReadOnlyList<TDocument>>("Not all entities were processed");
                        }

                        var unmatched = await CollectUnmatchedUpdatesAsync(
                            collection, entityList, writeModels, processed, batch.Count, token);
                        if (unmatched.Count > 0)
                        {
                            return CompletePartialFailure(
                                snapshots,
                                entityList.Count,
                                processed,
                                batch.Count,
                                failed: unmatched,
                                unprocessed: new HashSet<int>(),
                                includeSuccessesInBatch: true);
                        }

                        processed += batch.Count;
                    }
                    catch (MongoBulkWriteException<TDocument> exception)
                    {
                        var failed = exception.WriteErrors
                            .ToDictionary(error => error.Index + processed, error => error.Message);
                        var unprocessed = new HashSet<int>();
                        if (options.IsOrdered && failed.Count > 0)
                        {
                            var firstFailed = failed.Keys.Min();
                            for (var index = firstFailed + 1; index < processed + batch.Count; index++)
                            {
                                unprocessed.Add(index);
                            }
                        }

                        return CompletePartialFailure(
                            snapshots,
                            entityList.Count,
                            processed,
                            batch.Count,
                            failed,
                            unprocessed,
                            includeSuccessesInBatch: !options.IsOrdered);
                    }
                }

                return Result.Ok<IReadOnlyList<TDocument>>(entityList);
            }
            catch (Exception exception) when (exception is MongoException or FormatException)
            {
                RestoreUncommitted(snapshots, processed, entityList.Count);
                throw;
            }
        }, cancellationToken, estimatedBytes: null);

    public Task<Result> BulkDeleteAsync(
        Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default)
        => ExecuteAsync((collection, token) =>
        {
            var collectionOptions = GetOptions();
            var filter = Builders<TDocument>.Filter.Where(expression);

            if (collectionOptions.SoftDeleteDisabled || !IsSoftDeletable)
            {
                return PermanentDeleteAsync(collection, filter, token);
            }

            filter = ApplyNotDeleteFilter(filter);
            return SoftDeleteManyAsync(collection, filter, token);
        }, cancellationToken);

    private async Task<Result> SoftDeleteManyAsync(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        CancellationToken token)
    {
        UpdateDefinition<TDocument> update = Builders<TDocument>.Update.Set(
            PolicyElementNames.IsDeleted<TDocument>(), true);
        if (HasConcurrencyToken)
        {
            update = update.Set(PolicyElementNames.ETag<TDocument>(), MongoDbHelper.GenerateEtag());
        }

        if (IsAuditable)
        {
            update = update.Set(PolicyElementNames.UpdatedAt<TDocument>(), DateTime.UtcNow);
        }

        var result = await MongoCollectionCalls.UpdateManyAsync(
            collection, _callContext.Session, filter, update, options: null, token);
        return result.MatchedCount == 0
            ? Result.Fail(new DocumentNotFoundError("No entities were deleted"))
            : Result.Ok();
    }

    private async Task<Result> PermanentDeleteAsync(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        CancellationToken token)
    {
        var result = await MongoCollectionCalls.DeleteManyAsync(
            collection, _callContext.Session, filter, token);
        return result.DeletedCount == 0
            ? Result.Fail(new DocumentNotFoundError("No entities were deleted"))
            : Result.Ok();
    }

    private IEnumerable<WriteModel<TDocument>> CreateWriteModels(IReadOnlyList<TDocument> entities)
    {
        var guidStrategy = GetOptions().GuidIdGenerationStrategy;

        foreach (var entity in entities)
        {
            var isCreate = HasConcurrencyToken
                ? entity.IsNew()
                : IdAccessor.IsEmpty(entity);

            if (isCreate)
            {
                if (IdAccessor.IsEmpty(entity))
                {
                    IdAccessor.EnsureNewId(entity, guidStrategy);
                }

                if (HasConcurrencyToken)
                {
                    entity.GenerateETag();
                }

                if (IsAuditable)
                {
                    entity.CreatedNow();
                    entity.UpdatedNow();
                }

                yield return new InsertOneModel<TDocument>(entity);
                continue;
            }

            if (IsAuditable)
            {
                entity.UpdatedNow();
            }

            if (HasConcurrencyToken)
            {
                var currentETag = ((IHasConcurrencyToken)entity).ETag;
                entity.GenerateETag();

                var filter = IdAccessor.BuildIdFilter(entity);
                filter &= Builders<TDocument>.Filter.Eq(PolicyElementNames.ETag<TDocument>(), currentETag);
                filter = ApplyNotDeleteFilter(filter);

                var updateDocument = entity.ToBsonSnapshotUpdateDocument();
                yield return new UpdateOneModel<TDocument>(filter, updateDocument);
            }
            else
            {
                var filter = ApplyNotDeleteFilter(IdAccessor.BuildIdFilter(entity));
                yield return new ReplaceOneModel<TDocument>(filter, entity);
            }
        }
    }

    private static IReadOnlyList<IReadOnlyList<WriteModel<TDocument>>> Chunk(
        IReadOnlyList<WriteModel<TDocument>> models,
        MongoBulkWriteOptions options)
    {
        var maxOps = options.MaxOperationsPerBatch.GetValueOrDefault(DefaultMaxOperationsPerBatch);
        if (maxOps <= 0)
        {
            maxOps = DefaultMaxOperationsPerBatch;
        }

        var batches = new List<IReadOnlyList<WriteModel<TDocument>>>();
        var current = new List<WriteModel<TDocument>>();
        var estimatedBytes = 0;

        foreach (var model in models)
        {
            var modelBytes = EstimateBytes(model);
            if (current.Count >= maxOps || (current.Count > 0 && estimatedBytes + modelBytes > MaxEstimatedBatchBytes))
            {
                batches.Add(current);
                current = [];
                estimatedBytes = 0;
            }

            current.Add(model);
            estimatedBytes += modelBytes;
        }

        if (current.Count > 0)
        {
            batches.Add(current);
        }

        return batches;
    }

    private static int EstimateBytes(WriteModel<TDocument> model)
        => model switch
        {
            InsertOneModel<TDocument> insert => insert.Document.ToBsonDocument().ToBson().Length,
            ReplaceOneModel<TDocument> replace => replace.Replacement.ToBsonDocument().ToBson().Length,
            UpdateOneModel<TDocument> update => EstimateUpdateBytes(update),
            _ => 512
        };

    private static int EstimateUpdateBytes(UpdateOneModel<TDocument> update)
    {
        var rendered = update.Update.Render(
            new RenderArgs<TDocument>(
                BsonSerializer.LookupSerializer<TDocument>(),
                BsonSerializer.SerializerRegistry));
        return rendered.ToBson().Length;
    }

    private static EntitySnapshot Capture(TDocument entity) => new()
    {
        Entity = entity,
        ETag = HasConcurrencyToken ? ((IHasConcurrencyToken)entity).ETag : 0,
        UpdatedAt = IsAuditable ? ((IAuditableDocument)entity).UpdatedAt : default,
        CreatedAt = IsAuditable ? ((IAuditableDocument)entity).CreatedAt : default
    };

    private static void RestoreAll(IReadOnlyList<EntitySnapshot> snapshots)
    {
        RestoreUncommitted(snapshots, committedCount: 0, snapshots.Count);
    }

    private static void RestoreUncommitted(
        IReadOnlyList<EntitySnapshot> snapshots,
        int committedCount,
        int totalCount)
    {
        for (var index = committedCount; index < totalCount; index++)
        {
            snapshots[index].Restore();
        }
    }

    private async Task<Dictionary<int, string>> CollectUnmatchedUpdatesAsync(
        IMongoCollection<TDocument> collection,
        IReadOnlyList<TDocument> entities,
        IReadOnlyList<WriteModel<TDocument>> writeModels,
        int processed,
        int batchCount,
        CancellationToken cancellationToken)
    {
        var unmatched = new Dictionary<int, string>();
        for (var offset = 0; offset < batchCount; offset++)
        {
            var index = processed + offset;
            if (writeModels[index] is InsertOneModel<TDocument>)
            {
                continue;
            }

            var entity = entities[index];
            var persisted = await MongoCollectionCalls.Find(
                    collection,
                    _callContext.Session,
                    ApplyNotDeleteFilter(IdAccessor.BuildIdFilter(entity)))
                .FirstOrDefaultAsync(cancellationToken);
            if (persisted is null)
            {
                unmatched[index] = "The document was not found.";
                continue;
            }

            if (HasConcurrencyToken &&
                ((IHasConcurrencyToken)persisted).ETag != ((IHasConcurrencyToken)entity).ETag)
            {
                unmatched[index] = "The document was modified concurrently.";
            }
        }

        return unmatched;
    }

    private Result<IReadOnlyList<TDocument>> CompletePartialFailure(
        IReadOnlyList<EntitySnapshot> snapshots,
        int totalCount,
        int processed,
        int batchCount,
        IReadOnlyDictionary<int, string> failed,
        ISet<int> unprocessed,
        bool includeSuccessesInBatch)
    {
        var items = new List<BulkWriteItemResult>(totalCount);
        var batchEnd = processed + batchCount;
        for (var index = 0; index < totalCount; index++)
        {
            if (failed.TryGetValue(index, out var message))
            {
                snapshots[index].Restore();
                items.Add(new BulkWriteItemResult(index, succeeded: false, message));
                continue;
            }

            if (unprocessed.Contains(index) || index >= batchEnd)
            {
                snapshots[index].Restore();
                items.Add(new BulkWriteItemResult(index, succeeded: false, "The write was not processed."));
                continue;
            }

            if (index < processed || includeSuccessesInBatch)
            {
                items.Add(new BulkWriteItemResult(index, succeeded: true));
                continue;
            }

            snapshots[index].Restore();
            items.Add(new BulkWriteItemResult(index, succeeded: false, "The write was not processed."));
        }

        return Result.Fail<IReadOnlyList<TDocument>>(
            new BulkWritePartialFailureError(items, "One or more bulk write items failed."));
    }

    private sealed class EntitySnapshot
    {
        public required TDocument Entity { get; init; }

        public long ETag { get; init; }

        public DateTime UpdatedAt { get; init; }

        public DateTime CreatedAt { get; init; }

        public void Restore()
        {
            if (HasConcurrencyToken)
            {
                ((IHasConcurrencyToken)Entity).ETag = ETag;
            }

            if (IsAuditable)
            {
                ((IAuditableDocument)Entity).UpdatedAt = UpdatedAt;
                ((IAuditableDocument)Entity).CreatedAt = CreatedAt;
            }
        }
    }
}
