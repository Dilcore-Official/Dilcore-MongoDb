using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Extensions;
using Dilcore.MongoDB.Abstractions.Helpers;
using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories;

internal class GenericMongoDbRepository<TDocument>(
    Action<GetCollectionOptions<TDocument>> options,
    Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> collectionProvider)
    : BaseMongoDbRepository<TDocument>(options, collectionProvider), IGenericRepository<TDocument>
    where TDocument : class, IDocumentEntity
{
    private static readonly bool HasConcurrencyToken =
        typeof(IHasConcurrencyToken).IsAssignableFrom(typeof(TDocument));

    private static readonly bool IsSoftDeletable =
        typeof(ISoftDeletable).IsAssignableFrom(typeof(TDocument));

    private static readonly IDocumentIdAccessor<TDocument> IdAccessor =
        DocumentIdAccessorCache.Get<TDocument>();

    public Task<Result<TDocument>> StoreAsync(TDocument entity, CancellationToken cancellationToken = default)
        => ExecuteAsync((collection, ct) => StoreEntityAsync(entity, collection, ct), cancellationToken);

    public Task<Result<TDocument>> GetAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var entity = await collection.Find(filter).FirstOrDefaultAsync(token);
            return Result.Ok(entity);
        }, cancellationToken);

    public Task<Result<TDerived>> GetAsync<TDerived>(FilterDefinition<TDerived> filter, CancellationToken cancellationToken = default)
        where TDerived : class, TDocument
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var entity = await collection.OfType<TDerived>()
                .Find(filter)
                .FirstOrDefaultAsync(token);
            return Result.Ok(entity);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> GetListAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (collection, token) =>
        {
            var filter = ApplyNotDeleteFilter();
            var entities = await collection.Find(filter).ToListAsync(token);
            return Result.Ok<IReadOnlyList<TDocument>>(entities);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> GetListAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var entities = await collection.Find(filter).ToListAsync(token);
            return Result.Ok<IReadOnlyList<TDocument>>(entities);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<TDerived>>> GetListAsync<TDerived>(FilterDefinition<TDerived> filter,
        CancellationToken cancellationToken = default)
        where TDerived : class, TDocument
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var entities = await collection.OfType<TDerived>().Find(filter).ToListAsync(token);
            return Result.Ok<IReadOnlyList<TDerived>>(entities);
        }, cancellationToken);

    public async IAsyncEnumerable<TDocument> GetAsyncEnumerable(
        FilterDefinition<TDocument> filter,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var collectionResult = await collectionProvider(cancellationToken);
        if (collectionResult.IsFailed)
        {
            throw new InvalidOperationException(
                $"Failed to get collection: {collectionResult.Errors.FirstOrDefault()?.Message}");
        }

        var collection = collectionResult.Value;
        filter = ApplyNotDeleteFilter(filter);

        using var cursor = await collection.Find(filter).ToCursorAsync(cancellationToken);
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
        var collectionResult = await collectionProvider(cancellationToken);
        if (collectionResult.IsFailed)
        {
            throw new InvalidOperationException(
                $"Failed to get collection: {collectionResult.Errors.FirstOrDefault()?.Message}");
        }

        var collection = collectionResult.Value;
        filter = ApplyNotDeleteFilter(filter);

        using var cursor = await collection.OfType<TDerived>().Find(filter).ToCursorAsync(cancellationToken);
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

    public Task<Result<bool>> HasAnyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            var any = await collection.Find(filter).AnyAsync(cancellationToken: token);
            return Result.Ok(any);
        }, cancellationToken);

    public Task<Result<long>> CountAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            var count = await collection.CountDocumentsAsync(filter, cancellationToken: token);
            return Result.Ok(count);
        }, cancellationToken);

    private Task<Result<TDocument>> StoreEntityAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken = default)
    {
        entity.UpdatedNow();

        if (HasConcurrencyToken)
        {
            var currentEtag = ((IHasConcurrencyToken)entity).ETag;
            return entity.IsNew()
                ? CreateAsync(entity, collection, cancellationToken)
                : UpdateWithConcurrencyAsync(entity, currentEtag, collection, cancellationToken);
        }

        return IdAccessor.IsEmpty(entity)
            ? CreateAsync(entity, collection, cancellationToken)
            : ReplaceWithoutConcurrencyAsync(entity, collection, cancellationToken);
    }

    private async Task<Result<TDocument>> CreateAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken)
    {
        if (IdAccessor.IsEmpty(entity))
        {
            IdAccessor.EnsureNewId(entity, GetOptions().GuidIdGenerationStrategy);
        }

        entity.CreatedNow();
        entity.GenerateETag();

        await collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
        return Result.Ok(entity);
    }

    private async Task<Result<TDocument>> UpdateWithConcurrencyAsync(
        TDocument entity,
        long currentEtag,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken)
    {
        var filter = IdAccessor.BuildIdFilter(entity);
        filter &= Builders<TDocument>.Filter.Eq("eTag", currentEtag);
        filter = ApplyNotDeleteFilter(filter);

        entity.GenerateETag();
        var updateDocument = entity.ToBsonUpdateDocument();

        var updateResult =
            await collection.UpdateOneAsync(filter, updateDocument, cancellationToken: cancellationToken);

        return updateResult.ModifiedCount == 1
            ? Result.Ok(entity)
            : Result.Fail($"Failed to update entity '{collection.CollectionNamespace}' with id filter");
    }

    private async Task<Result<TDocument>> ReplaceWithoutConcurrencyAsync(
        TDocument entity,
        IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken)
    {
        var filter = IdAccessor.BuildIdFilter(entity);
        filter = ApplyNotDeleteFilter(filter);

        entity.GenerateETag();
        var result = await collection.ReplaceOneAsync(filter, entity, cancellationToken: cancellationToken);

        return result.ModifiedCount == 1 || result.MatchedCount == 1
            ? Result.Ok(entity)
            : Result.Fail($"Failed to update entity '{collection.CollectionNamespace}' with id filter");
    }

    private static async Task<Result<bool>> SoftDeleteOneAsync(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken = default)
    {
        UpdateDefinition<TDocument> update = Builders<TDocument>.Update.Set("isDeleted", true);
        if (HasConcurrencyToken)
        {
            update = update.Set("eTag", MongoDbHelper.GenerateEtag());
        }

        var result = await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        return Result.Ok(result.ModifiedCount == 1);
    }

    private static async Task<Result<bool>> PermanentDeleteOneAsync(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken = default)
    {
        var result = await collection.DeleteOneAsync(filter, cancellationToken);
        return Result.Ok(result.DeletedCount == 1);
    }
}
