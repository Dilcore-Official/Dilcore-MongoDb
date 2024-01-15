using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.Abstractions.Extensions;
using Dilcore.DocumentDb.Abstractions.Helpers;
using Dilcore.DocumentDb.Abstractions.Repositories;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Repositories;

internal class GenericMongoDbRepository<TDocument>(
    Action<GetCollectionOptions<TDocument>> options,
    Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> collectionProvider)
    : BaseMongoDbRepository<TDocument>(options, collectionProvider), IGenericRepository<TDocument>
    where TDocument : class, IDocumentEntity
{
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
        =>
            ExecuteAsync(async (collection, token) =>
            {
                filter = ApplyNotDeleteFilter(filter);

                var entities = await collection.OfType<TDerived>().Find(filter).ToListAsync(token);

                return Result.Ok<IReadOnlyList<TDerived>>(entities);
            }, cancellationToken);

    public Task<Result<bool>> DeleteAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
        => ExecuteAsync((collection, token) =>
        {
            var collectionOptions = GetOptions();
            
            if (collectionOptions.SoftDeleteDisabled)
            {
                return PermanentDeleteOneAsync(collection, filter, token);
            }

            filter = ApplyNotDeleteFilter(filter);
            return SoftDeleteOneAsync(collection, filter, token);

        }, cancellationToken);

    #region Store

    private Task<Result<TDocument>> StoreEntityAsync(TDocument entity,
        IMongoCollection<TDocument> collection, CancellationToken cancellationToken = default)
    {
        var currentEtag = entity.ETag;

        entity.UpdatedNow();

        if (entity.IsNew())
        {
            return CreateAsync(entity, collection, cancellationToken);
        }

        return UpdateAsync(entity, currentEtag, collection, cancellationToken);
    }

    private static async Task<Result<TDocument>> CreateAsync(TDocument entity, IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken)
    {
        if (entity.IsIdEmpty())
        {
            entity.NewId();
        }

        entity.GenerateETag();

        await collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
        return Result.Ok(entity);
    }

    private async Task<Result<TDocument>> UpdateAsync(TDocument entity, long currentEtag,
        IMongoCollection<TDocument> collection, CancellationToken cancellationToken)
    {
        var filter = Builders<TDocument>.Filter.Eq(x => x.Id, entity.Id);
        filter &= Builders<TDocument>.Filter.Eq(x => x.ETag, currentEtag);

        filter = ApplyNotDeleteFilter(filter);

        entity.GenerateETag();

        var updateDocument = entity.ToBsonUpdateDocument();

        var updateResult =
            await collection.UpdateOneAsync(filter, updateDocument, cancellationToken: cancellationToken);

        return updateResult.ModifiedCount == 1 ? Result.Ok(entity) 
            : Result.Fail($"Failed to update entity '{collection.CollectionNamespace}' with id {entity.Id}");
    }

    #endregion

    #region Delete

    private static async Task<Result<bool>> SoftDeleteOneAsync(IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
    {
        var update = Builders<TDocument>.Update.Set(x => x.IsDeleted, true)
            .Set(x => x.ETag, DocumentDbHelper.GenerateEtag());

        var result = await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);

        return Result.Ok(result.ModifiedCount == 1);
    }

    private static async Task<Result<bool>> PermanentDeleteOneAsync(IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
    {
        var result = await collection.DeleteOneAsync(filter, cancellationToken);

        return Result.Ok(result.DeletedCount == 1);
    }

    #endregion
}