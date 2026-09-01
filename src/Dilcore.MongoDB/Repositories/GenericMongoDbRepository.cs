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
using MongoDB.Bson.Serialization;
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
        => ExecuteAsync((collection, ct) => StoreEntityAsync(entity, collection, ct), cancellationToken);

    public Task<Result<TDocument>> ReplaceAsync(TDocument entity, CancellationToken cancellationToken = default)
        => ExecuteAsync((collection, ct) => ReplaceWithoutConcurrencyAsync(entity, collection, replaceEntireDocument: true, ct), cancellationToken);

    public Task<Result<TDocument>> UpdateSnapshotAsync(TDocument entity, CancellationToken cancellationToken = default)
        => ExecuteAsync((collection, ct) =>
        {
            if (HasConcurrencyToken)
            {
                var currentEtag = ((IHasConcurrencyToken)entity).ETag;
                return entity.IsNew()
                    ? CreateAsync(entity, collection, ct)
                    : UpdateWithConcurrencyAsync(entity, currentEtag, collection, ct);
            }

            return IdAccessor.IsEmpty(entity)
                ? CreateAsync(entity, collection, ct)
                : ReplaceWithoutConcurrencyAsync(entity, collection, replaceEntireDocument: false, ct);
        }, cancellationToken);

    public Task<Result<TDocument>> PatchAsync(
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            if (HasConcurrencyToken)
            {
                update = update.Set("eTag", MongoDbHelper.GenerateEtag());
            }

            if (IsAuditable)
            {
                update = update.Set("updatedAt", DateTime.UtcNow);
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
        }, cancellationToken);

    public Task<Result<TDocument>> GetAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var entity = await MongoCollectionCalls.Find(collection, _callContext.Session, filter)
                .FirstOrDefaultAsync(token);
            return entity is null
                ? Result.Fail<TDocument>(new DocumentNotFoundError())
                : Result.Ok(entity);
        }, cancellationToken);

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
        }, cancellationToken);

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
            var fluent = MongoCollectionCalls.Find(collection, _callContext.Session, filter);
            if (options?.Sort is not null)
            {
                fluent = fluent.Sort(options.Sort);
            }

            if (options?.Limit is not null)
            {
                fluent = fluent.Limit(options.Limit);
            }

            if (options?.Skip is not null)
            {
                fluent = fluent.Skip(options.Skip);
            }

            var entities = await fluent.ToListAsync(token);
            return Result.Ok<IReadOnlyList<TDocument>>(entities);
        }, cancellationToken);

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
        }, cancellationToken);

    public Task<Result<KeysetPage<TDocument>>> GetPageAsync(
        KeysetPageRequest<TDocument> request,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            if (request.PageSize <= 0)
            {
                return Result.Fail<KeysetPage<TDocument>>("PageSize must be greater than zero.");
            }

            var filter = ApplyNotDeleteFilter(request.Filter);
            if (!string.IsNullOrWhiteSpace(request.Cursor))
            {
                filter &= DecodeCursor(request.Cursor);
            }

            var take = request.PageSize + 1;
            var fluent = MongoCollectionCalls.Find(collection, _callContext.Session, filter)
                .Sort(request.Sort)
                .Limit(take);

            var documents = await fluent.ToListAsync(token);
            var hasMore = documents.Count > request.PageSize;
            if (hasMore)
            {
                documents.RemoveAt(documents.Count - 1);
            }

            var nextCursor = hasMore && documents.Count > 0
                ? EncodeCursor(documents[^1])
                : null;

            return Result.Ok(new KeysetPage<TDocument>
            {
                Items = documents,
                NextCursor = nextCursor,
                HasMore = hasMore
            });
        }, cancellationToken);

    public async IAsyncEnumerable<TDocument> GetAsyncEnumerable(
        FilterDefinition<TDocument> filter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var collectionResult = await GetCollectionAsync(cancellationToken);
        if (collectionResult.IsFailed)
        {
            throw new CollectionResolutionException(
                $"Failed to get collection: {collectionResult.Errors.FirstOrDefault()?.Message}");
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
                $"Failed to get collection: {collectionResult.Errors.FirstOrDefault()?.Message}");
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
        }, cancellationToken);

    public Task<Result<bool>> RestoreAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            if (!IsSoftDeletable)
            {
                return Result.Fail<bool>("Restore requires ISoftDeletable.");
            }

            UpdateDefinition<TDocument> update = Builders<TDocument>.Update.Set("isDeleted", false);
            if (HasConcurrencyToken)
            {
                update = update.Set("eTag", MongoDbHelper.GenerateEtag());
            }

            if (IsAuditable)
            {
                update = update.Set("updatedAt", DateTime.UtcNow);
            }

            var result = await MongoCollectionCalls.UpdateOneAsync(
                collection, _callContext.Session, filter, update, options: null, token);
            return result.MatchedCount == 1
                ? Result.Ok(true)
                : Result.Fail<bool>(new DocumentNotFoundError());
        }, cancellationToken);

    public Task<Result<long>> PurgeAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            var result = await MongoCollectionCalls.DeleteManyAsync(
                collection, _callContext.Session, filter, token);
            return Result.Ok(result.DeletedCount);
        }, cancellationToken);

    public Task<Result<bool>> HasAnyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var any = await MongoCollectionCalls.Find(collection, _callContext.Session, filter)
                .Limit(1)
                .AnyAsync(cancellationToken: token);
            return Result.Ok(any);
        }, cancellationToken);

    public Task<Result<long>> CountAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var count = await MongoCollectionCalls.CountDocumentsAsync(
                collection, _callContext.Session, filter, options: null, token);
            return Result.Ok(count);
        }, cancellationToken);

    private Task<Result<TDocument>> StoreEntityAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken = default)
    {
        if (HasConcurrencyToken)
        {
            return entity.IsNew()
                ? CreateAsync(entity, collection, cancellationToken)
                : UpdateWithConcurrencyAsync(entity, ((IHasConcurrencyToken)entity).ETag, collection, cancellationToken);
        }

        return IdAccessor.IsEmpty(entity)
            ? CreateAsync(entity, collection, cancellationToken)
            : ReplaceWithoutConcurrencyAsync(entity, collection, replaceEntireDocument: false, cancellationToken);
    }

    private async Task<Result<TDocument>> CreateAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken)
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
        catch (MongoException)
        {
            if (stagedId)
            {
                // Best-effort: leave generated id; insert retry with same id is safer than clearing.
            }

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

    private async Task<Result<TDocument>> UpdateWithConcurrencyAsync(
        TDocument entity,
        long currentEtag,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken)
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
        filter &= Builders<TDocument>.Filter.Eq("eTag", currentEtag);
        filter = ApplyNotDeleteFilter(filter);
        var updateDocument = entity.ToBsonSnapshotUpdateDocument();

        try
        {
            var updateResult = await MongoCollectionCalls.UpdateOneAsync(
                collection,
                _callContext.Session,
                filter,
                updateDocument,
                options: null,
                cancellationToken);

            if (updateResult.MatchedCount == 1)
            {
                return Result.Ok(entity);
            }

            RestoreStaged(entity, previousEtag, previousUpdated);
            return Result.Fail<TDocument>(new ConcurrencyConflictError());
        }
        catch (MongoException)
        {
            RestoreStaged(entity, previousEtag, previousUpdated);
            throw;
        }
    }

    private async Task<Result<TDocument>> ReplaceWithoutConcurrencyAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        bool replaceEntireDocument,
        CancellationToken cancellationToken)
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
        catch (MongoException)
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
        UpdateDefinition<TDocument> update = Builders<TDocument>.Update.Set("isDeleted", true);
        if (HasConcurrencyToken)
        {
            update = update.Set("eTag", MongoDbHelper.GenerateEtag());
        }

        if (IsAuditable)
        {
            update = update.Set("updatedAt", DateTime.UtcNow);
        }

        var result = await MongoCollectionCalls.UpdateOneAsync(
            collection, _callContext.Session, filter, update, options: null, cancellationToken);
        return Result.Ok(result.ModifiedCount == 1 || result.MatchedCount == 1);
    }

    private async Task<Result<bool>> PermanentDeleteOneAsync(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken = default)
    {
        var result = await MongoCollectionCalls.DeleteOneAsync(
            collection, _callContext.Session, filter, cancellationToken);
        return Result.Ok(result.DeletedCount == 1);
    }

    private static void RestoreStaged(TDocument entity, long previousEtag, DateTime previousUpdated)
    {
        if (HasConcurrencyToken)
        {
            ((IHasConcurrencyToken)entity).ETag = previousEtag;
        }

        RestoreAudit(entity, previousUpdated);
    }

    private static void RestoreAudit(TDocument entity, DateTime previousUpdated)
    {
        if (IsAuditable)
        {
            ((IAuditableDocument)entity).UpdatedAt = previousUpdated;
        }
    }

    private static string EncodeCursor(TDocument document)
    {
        var bson = document.ToBsonDocument();
        var id = bson["_id"];
        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(id.ToJson()));
    }

    private static FilterDefinition<TDocument> DecodeCursor(string cursor)
    {
        var json = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var id = BsonSerializer.Deserialize<BsonValue>(json);
        return Builders<TDocument>.Filter.Gt("_id", id);
    }
}
