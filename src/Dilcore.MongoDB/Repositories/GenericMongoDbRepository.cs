using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Exceptions;
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
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories;

internal class GenericMongoDbRepository<TDocument> : BaseMongoDbRepository<TDocument>, IGenericRepository<TDocument>
    where TDocument : class, IDocumentEntity
{
    private static readonly bool HasConcurrencyToken =
        typeof(IHasConcurrencyToken).IsAssignableFrom(typeof(TDocument));

    private static readonly bool IsSoftDeletable =
        typeof(ISoftDeletable).IsAssignableFrom(typeof(TDocument));

    private static readonly bool IsAuditable =
        typeof(IAuditableDocument).IsAssignableFrom(typeof(TDocument));

    private static readonly IDocumentIdAccessor<TDocument> IdAccessor =
        DocumentIdAccessorCache.Get<TDocument>();

    private readonly MongoCallContext _callContext;

    public GenericMongoDbRepository(
        Action<GetCollectionOptions<TDocument>> options,
        Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> collectionProvider,
        MongoCallContext? callContext = null)
        : base(options, collectionProvider)
    {
        _callContext = callContext ?? MongoCallContext.None;
        Session = _callContext.Session;
        Budget = _callContext.Budget;
    }

    public Task<Result<TDocument>> StoreAsync(TDocument entity, CancellationToken cancellationToken = default)
        => ExecuteAsync(
            (collection, ct) => StoreEntityAsync(entity, collection, ct),
            Estimate(entity),
            cancellationToken);

    public Task<Result<TDocument>> ReplaceAsync(TDocument entity, CancellationToken cancellationToken = default)
        => ExecuteAsync((collection, ct) =>
        {
            if (HasConcurrencyToken)
            {
                return entity.IsNew()
                    ? Task.FromResult(Result.Fail<TDocument>(new DocumentNotFoundError()))
                    : WriteWithConcurrencyAsync(entity, ((IHasConcurrencyToken)entity).ETag, collection, replaceEntireDocument: true, ct);
            }

            return ReplaceWithoutConcurrencyAsync(entity, collection, replaceEntireDocument: true, ct);
        }, Estimate(entity), cancellationToken);

    public Task<Result<TDocument>> UpdateSnapshotAsync(TDocument entity, CancellationToken cancellationToken = default)
        => ExecuteAsync((collection, ct) =>
        {
            if (HasConcurrencyToken)
            {
                var currentEtag = ((IHasConcurrencyToken)entity).ETag;
                return entity.IsNew()
                    ? CreateAsync(entity, collection, ct)
                    : WriteWithConcurrencyAsync(entity, currentEtag, collection, replaceEntireDocument: false, ct);
            }

            return IdAccessor.IsEmpty(entity)
                ? CreateAsync(entity, collection, ct)
                : ReplaceWithoutConcurrencyAsync(entity, collection, replaceEntireDocument: false, ct);
        }, Estimate(entity), cancellationToken);

    public Task<Result<TDocument>> PatchAsync(
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            if (HasConcurrencyToken)
            {
                update = update.Set(PolicyElementNames.ETag<TDocument>(), MongoDbHelper.GenerateEtag());
            }

            if (IsAuditable)
            {
                update = update.Set(PolicyElementNames.UpdatedAt<TDocument>(), DateTime.UtcNow);
            }

            var updated = await MongoCollectionCalls.FindOneAndUpdateAsync(
                collection,
                _callContext.Session,
                filter,
                update,
                new FindOneAndUpdateOptions<TDocument> { ReturnDocument = ReturnDocument.After },
                token);

            if (updated is null)
            {
                return HasConcurrencyToken
                    ? Result.Fail<TDocument>(new ConcurrencyConflictError())
                    : Result.Fail<TDocument>(new DocumentNotFoundError());
            }

            return Result.Ok(updated);
        }, cancellationToken: cancellationToken);

    public Task<Result<TDocument>> GetAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var entity = await MongoCollectionCalls.Find(collection, _callContext.Session, filter)
                .FirstOrDefaultAsync(token);
            return entity is null
                ? Result.Fail<TDocument>(new DocumentNotFoundError())
                : Result.Ok(entity);
        }, cancellationToken: cancellationToken);

    public Task<Result<TDerived>> GetAsync<TDerived>(FilterDefinition<TDerived> filter, CancellationToken cancellationToken = default)
        where TDerived : class, TDocument
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var query = _callContext.Session is null
                ? collection.OfType<TDerived>().Find(filter)
                : collection.OfType<TDerived>().Find(_callContext.Session, filter);
            var entity = await query.FirstOrDefaultAsync(token);

            return entity is null
                ? Result.Fail<TDerived>(new DocumentNotFoundError())
                : Result.Ok(entity);
        }, cancellationToken: cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> GetListAsync(CancellationToken cancellationToken = default) =>
        GetListAsync(ApplyNotDeleteFilter(), options: null, cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> GetListAsync(
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken = default)
        => GetListAsync(filter, options: null, cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> GetListAsync(
        FilterDefinition<TDocument> filter,
        FindOptions<TDocument, TDocument>? options,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            using var cursor = _callContext.Session is null
                ? await MongoCollectionCalls.FindAsync(collection, filter, options, token)
                : await MongoCollectionCalls.FindAsync(
                    collection, _callContext.Session, filter, options, token);
            var entities = await cursor.ToListAsync(token);
            return Result.Ok<IReadOnlyList<TDocument>>(entities);
        }, cancellationToken: cancellationToken);

    public Task<Result<IReadOnlyList<TDerived>>> GetListAsync<TDerived>(
        FilterDefinition<TDerived> filter,
        CancellationToken cancellationToken = default)
        where TDerived : class, TDocument
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var query = _callContext.Session is null
                ? collection.OfType<TDerived>().Find(filter)
                : collection.OfType<TDerived>().Find(_callContext.Session, filter);
            var entities = await query.ToListAsync(token);
            return Result.Ok<IReadOnlyList<TDerived>>(entities);
        }, cancellationToken: cancellationToken);

    public Task<Result<KeysetPage<TDocument>>> GetPageAsync(
        KeysetPageRequest<TDocument> request,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            if (request.PageSize <= 0)
            {
                return Result.Fail<KeysetPage<TDocument>>("PageSize must be greater than zero.");
            }

            if (request.PageSize == int.MaxValue)
            {
                return Result.Fail<KeysetPage<TDocument>>(
                    "PageSize is too large to reserve an extra document for HasMore.");
            }

            var sortResult = KeysetCursor.NormalizeSort(request.Sort);
            if (sortResult.IsFailed)
            {
                return Result.Fail<KeysetPage<TDocument>>(sortResult.Errors);
            }

            var sortKeys = sortResult.Value;
            var projectionResult = KeysetCursor.ValidateProjection(request.Projection, sortKeys);
            if (projectionResult.IsFailed)
            {
                return Result.Fail<KeysetPage<TDocument>>(projectionResult.Errors);
            }

            var filter = ApplyNotDeleteFilter(request.Filter);
            if (!string.IsNullOrWhiteSpace(request.Cursor))
            {
                var seekResult = KeysetCursor.TryDecodeSeekFilter<TDocument>(request.Cursor, sortKeys);
                if (seekResult.IsFailed)
                {
                    return Result.Fail<KeysetPage<TDocument>>(seekResult.Errors);
                }

                filter &= seekResult.Value;
            }

            var take = request.PageSize + 1;
            var fluent = MongoCollectionCalls.Find(collection, _callContext.Session, filter)
                .Sort(KeysetCursor.ToSortDefinition<TDocument>(sortKeys))
                .Limit(take);

            if (request.Projection is not null)
            {
                ProjectionDefinition<TDocument, TDocument> projection = request.Projection;
                fluent = fluent.Project(projection);
            }

            var documents = await fluent.ToListAsync(token);
            var hasMore = documents.Count > request.PageSize;
            if (hasMore)
            {
                documents.RemoveAt(documents.Count - 1);
            }

            string? nextCursor = null;
            if (hasMore && documents.Count > 0)
            {
                var encodeResult = KeysetCursor.TryEncode(documents[^1], sortKeys);
                if (encodeResult.IsFailed)
                {
                    return Result.Fail<KeysetPage<TDocument>>(encodeResult.Errors);
                }

                nextCursor = encodeResult.Value;
            }

            return Result.Ok(new KeysetPage<TDocument>
            {
                Items = documents,
                NextCursor = nextCursor,
                HasMore = hasMore
            });
        }, cancellationToken: cancellationToken);

    public async IAsyncEnumerable<TDocument> GetAsyncEnumerable(
        FilterDefinition<TDocument> filter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var collectionResult = await GetCollectionAsync(cancellationToken);
        if (collectionResult.IsFailed)
        {
            throw new CollectionResolutionException(
                $"Failed to get collection: {(collectionResult.Errors.Count > 0 ? collectionResult.Errors[0].Message : null)}");
        }

        var collection = collectionResult.Value;
        filter = ApplyNotDeleteFilter(filter);

        using var cursor = await MongoCollectionCalls.Find(collection, _callContext.Session, filter)
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var document in cursor.Current)
            {
                yield return document;
            }
        }
    }

    public async IAsyncEnumerable<TDerived> GetAsyncEnumerable<TDerived>(
        FilterDefinition<TDerived> filter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        where TDerived : class, TDocument
    {
        var collectionResult = await GetCollectionAsync(cancellationToken);
        if (collectionResult.IsFailed)
        {
            throw new CollectionResolutionException(
                $"Failed to get collection: {(collectionResult.Errors.Count > 0 ? collectionResult.Errors[0].Message : null)}");
        }

        var collection = collectionResult.Value;
        filter = ApplyNotDeleteFilter(filter);

        var query = _callContext.Session is null
            ? collection.OfType<TDerived>().Find(filter)
            : collection.OfType<TDerived>().Find(_callContext.Session, filter);

        using var cursor = await query.ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (var document in cursor.Current)
            {
                yield return document;
            }
        }
    }

    public Task<Result<bool>> DeleteAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync((collection, token) =>
        {
            var collectionOptions = GetOptions();

            if (collectionOptions.SoftDeleteDisabled || !IsSoftDeletable)
            {
                return PermanentDeleteOneAsync(collection, filter, token);
            }

            filter = ApplyNotDeleteFilter(filter);
            return SoftDeleteOneAsync(collection, filter, token);
        }, cancellationToken: cancellationToken);

    public Task<Result<bool>> RestoreAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            if (!IsSoftDeletable)
            {
                return Result.Fail<bool>("Restore requires ISoftDeletable.");
            }

            filter &= Builders<TDocument>.Filter.Eq(PolicyElementNames.IsDeleted<TDocument>(), true);

            UpdateDefinition<TDocument> update = Builders<TDocument>.Update.Set(
                PolicyElementNames.IsDeleted<TDocument>(), false);
            if (HasConcurrencyToken)
            {
                update = update.Set(PolicyElementNames.ETag<TDocument>(), MongoDbHelper.GenerateEtag());
            }

            if (IsAuditable)
            {
                update = update.Set(PolicyElementNames.UpdatedAt<TDocument>(), DateTime.UtcNow);
            }

            var result = await MongoCollectionCalls.UpdateOneAsync(
                collection, _callContext.Session, filter, update, options: null, token);
            return result.MatchedCount == 1
                ? Result.Ok(true)
                : Result.Fail<bool>(new DocumentNotFoundError());
        }, cancellationToken: cancellationToken);

    public Task<Result<long>> PurgeAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            var result = await MongoCollectionCalls.DeleteManyAsync(
                collection, _callContext.Session, filter, token);
            return Result.Ok(result.DeletedCount);
        }, cancellationToken: cancellationToken);

    public Task<Result<bool>> HasAnyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var any = await MongoCollectionCalls.Find(collection, _callContext.Session, filter)
                .Limit(1)
                .AnyAsync(cancellationToken: token);
            return Result.Ok(any);
        }, cancellationToken: cancellationToken);

    public Task<Result<long>> CountAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var count = await MongoCollectionCalls.CountDocumentsAsync(
                collection, _callContext.Session, filter, options: null, token);
            return Result.Ok(count);
        }, cancellationToken: cancellationToken);

    private Task<Result<TDocument>> StoreEntityAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken = default)
    {
        if (HasConcurrencyToken)
        {
            return entity.IsNew()
                ? CreateAsync(entity, collection, cancellationToken)
                : WriteWithConcurrencyAsync(
                    entity,
                    ((IHasConcurrencyToken)entity).ETag,
                    collection,
                    replaceEntireDocument: false,
                    cancellationToken);
        }

        if (IdAccessor.IsEmpty(entity))
        {
            return CreateAsync(entity, collection, cancellationToken);
        }

        return StoreAssignedIdWithoutConcurrencyAsync(entity, collection, cancellationToken);
    }

    private async Task<Result<TDocument>> CreateAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken = default)
    {
        var stagedId = false;
        if (IdAccessor.IsEmpty(entity))
        {
            IdAccessor.EnsureNewId(entity, GetOptions().GuidIdGenerationStrategy);
            stagedId = true;
        }

        var previousCreated = IsAuditable ? ((IAuditableDocument)entity).CreatedAt : default;
        var previousUpdated = IsAuditable ? ((IAuditableDocument)entity).UpdatedAt : default;
        var previousEtag = HasConcurrencyToken ? ((IHasConcurrencyToken)entity).ETag : 0L;

        if (IsAuditable)
        {
            entity.CreatedNow();
            entity.UpdatedNow();
        }

        if (HasConcurrencyToken)
        {
            entity.GenerateETag();
        }

        try
        {
            await MongoCollectionCalls.InsertOneAsync(
                collection, _callContext.Session, entity, new InsertOneOptions(), cancellationToken);
            return Result.Ok(entity);
        }
        catch (Exception exception) when (exception is MongoException or FormatException or OperationCanceledException)
        {
            if (stagedId && exception is OperationCanceledException)
            {
                IdAccessor.ClearId(entity);
            }

            // Best-effort: leave generated id after Mongo/format failure; insert retry with same id is safer than clearing.

            if (IsAuditable)
            {
                ((IAuditableDocument)entity).CreatedAt = previousCreated;
                ((IAuditableDocument)entity).UpdatedAt = previousUpdated;
            }

            if (HasConcurrencyToken)
            {
                ((IHasConcurrencyToken)entity).ETag = previousEtag;
            }

            throw;
        }
    }

    private async Task<Result<TDocument>> WriteWithConcurrencyAsync(
        TDocument entity,
        long currentEtag,
        IMongoCollection<TDocument> collection,
        bool replaceEntireDocument,
        CancellationToken cancellationToken = default)
    {
        var previousUpdated = IsAuditable ? ((IAuditableDocument)entity).UpdatedAt : default;
        var previousEtag = currentEtag;
        var nextEtag = MongoDbHelper.GenerateEtag();
        var nextUpdated = DateTime.UtcNow;

        if (IsAuditable)
        {
            ((IAuditableDocument)entity).UpdatedAt = nextUpdated;
        }

        ((IHasConcurrencyToken)entity).ETag = nextEtag;

        var filter = IdAccessor.BuildIdFilter(entity);
        filter &= Builders<TDocument>.Filter.Eq(PolicyElementNames.ETag<TDocument>(), currentEtag);
        filter = ApplyNotDeleteFilter(filter);

        try
        {
            long matched;
            if (replaceEntireDocument)
            {
                var replaceResult = await MongoCollectionCalls.ReplaceOneAsync(
                    collection, _callContext.Session, filter, entity, options: null, cancellationToken);
                matched = replaceResult.MatchedCount;
            }
            else
            {
                var updateResult = await MongoCollectionCalls.UpdateOneAsync(
                    collection,
                    _callContext.Session,
                    filter,
                    entity.ToBsonSnapshotUpdateDocument(),
                    options: null,
                    cancellationToken);
                matched = updateResult.MatchedCount;
            }

            if (matched == 1)
            {
                return Result.Ok(entity);
            }

            RestoreStaged(entity, previousEtag, previousUpdated);
            return await UnmatchedWriteErrorAsync(entity, collection, cancellationToken);
        }
        catch (Exception exception) when (exception is MongoException or FormatException or OperationCanceledException)
        {
            RestoreStaged(entity, previousEtag, previousUpdated);
            throw;
        }
    }

    private async Task<Result<TDocument>> StoreAssignedIdWithoutConcurrencyAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken = default)
    {
        var update = await ReplaceWithoutConcurrencyAsync(
            entity, collection, replaceEntireDocument: false, cancellationToken);
        if (update.IsSuccess || !update.HasError<DocumentNotFoundError>())
        {
            return update;
        }

        return await CreateAsync(entity, collection, cancellationToken);
    }

    private async Task<Result<TDocument>> UnmatchedWriteErrorAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken = default)
    {
        var exists = await MongoCollectionCalls.Find(
                collection,
                _callContext.Session,
                ApplyNotDeleteFilter(IdAccessor.BuildIdFilter(entity)))
            .Limit(1)
            .AnyAsync(cancellationToken);
        return exists
            ? Result.Fail<TDocument>(new ConcurrencyConflictError())
            : Result.Fail<TDocument>(new DocumentNotFoundError());
    }

    private async Task<Result<TDocument>> ReplaceWithoutConcurrencyAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        bool replaceEntireDocument,
        CancellationToken cancellationToken = default)
    {
        var previousUpdated = IsAuditable ? ((IAuditableDocument)entity).UpdatedAt : default;
        if (IsAuditable)
        {
            entity.UpdatedNow();
        }

        var filter = ApplyNotDeleteFilter(IdAccessor.BuildIdFilter(entity));

        try
        {
            if (replaceEntireDocument)
            {
                var result = await MongoCollectionCalls.ReplaceOneAsync(
                    collection, _callContext.Session, filter, entity, options: null, cancellationToken);
                if (result.MatchedCount == 1)
                {
                    return Result.Ok(entity);
                }

                RestoreAudit(entity, previousUpdated);
                return Result.Fail<TDocument>(new DocumentNotFoundError());
            }

            var updateResult = await MongoCollectionCalls.UpdateOneAsync(
                collection,
                _callContext.Session,
                filter,
                entity.ToBsonSnapshotUpdateDocument(),
                options: null,
                cancellationToken);

            if (updateResult.MatchedCount == 1)
            {
                return Result.Ok(entity);
            }

            RestoreAudit(entity, previousUpdated);
            return Result.Fail<TDocument>(new DocumentNotFoundError());
        }
        catch (Exception exception) when (exception is MongoException or FormatException or OperationCanceledException)
        {
            RestoreAudit(entity, previousUpdated);
            throw;
        }
    }

    private async Task<Result<bool>> SoftDeleteOneAsync(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken = default)
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

        var result = await MongoCollectionCalls.UpdateOneAsync(
            collection, _callContext.Session, filter, update, options: null, cancellationToken);
        if (result.ModifiedCount == 1 || result.MatchedCount == 1)
        {
            return Result.Ok(true);
        }

        return HasConcurrencyToken
            ? Result.Fail<bool>(new ConcurrencyConflictError())
            : Result.Fail<bool>(new DocumentNotFoundError());
    }

    private async Task<Result<bool>> PermanentDeleteOneAsync(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken = default)
    {
        var result = await MongoCollectionCalls.DeleteOneAsync(
            collection, _callContext.Session, filter, cancellationToken);
        if (result.DeletedCount == 1)
        {
            return Result.Ok(true);
        }

        return HasConcurrencyToken
            ? Result.Fail<bool>(new ConcurrencyConflictError())
            : Result.Fail<bool>(new DocumentNotFoundError());
    }

    private static void RestoreStaged(TDocument entity, long previousEtag, DateTime previousUpdated)
    {
        if (HasConcurrencyToken)
        {
            ((IHasConcurrencyToken)entity).ETag = previousEtag;
        }

        RestoreAudit(entity, previousUpdated);
    }

    private static int? Estimate(TDocument entity)
    {
        try
        {
            // Snapshot serialization registers the Guid serializer. Doing that before ToBson
            // avoids locking the process-wide Guid serializer to Unspecified.
            _ = entity.ToBsonSnapshotUpdateDocument();
            return entity.ToBson().Length;
        }
        catch (Exception exception) when (exception is FormatException or BsonSerializationException)
        {
            // Unserializable payloads (for example above the BSON document cap) are left
            // for the write path, which maps them to DocumentTooLargeError.
            return null;
        }
    }

    private static void RestoreAudit(TDocument entity, DateTime previousUpdated)
    {
        if (IsAuditable)
        {
            ((IAuditableDocument)entity).UpdatedAt = previousUpdated;
        }
    }

}