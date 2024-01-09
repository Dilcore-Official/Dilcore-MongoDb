using System.Linq.Expressions;
using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.Abstractions.Extensions;
using Dilcore.DocumentDb.Abstractions.Repositories;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Repositories;

internal class GenericMongoDbRepository<TDocument>(Action<GetCollectionOptions<TDocument>> options, Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> collectionProvider)
    : BaseMongoDbRepository<TDocument>(options, collectionProvider), IGenericRepository<TDocument>
    where TDocument : class, IDocumentEntity 
{
    public Task<Result<TDocument>> StoreAsync(TDocument entity, CancellationToken cancellationToken = default)
        => ExecuteAsync((collection, ct) => StoreEntityAsync(entity, collection, ct), cancellationToken);

    public Task<Result<TDocument>> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (collection, token) =>
        {
            var filter = Builders<TDocument>.Filter.Eq(x => x.Id, id);
            filter &= NotDeletedFilter;

            var entity = await collection.Find(filter).FirstOrDefaultAsync(token);
            return Result.Ok(entity);
        }, cancellationToken);

    public Task<Result<TDocument>> GetAsync(Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (collection, token) =>
        {
            var filter = Builders<TDocument>.Filter.Where(expression);
            filter &= NotDeletedFilter;

            var entity = await collection.Find(filter).FirstOrDefaultAsync(token);
            return Result.Ok(entity);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> GetListAsync(CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (collection, token) =>
        {
            var filter = NotDeletedFilter;

            var entities = await collection.Find(filter).ToListAsync(token);

            return Result.Ok<IReadOnlyList<TDocument>>(entities);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> GetListAsync(Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (collection, token) =>
        {
            var filter = Builders<TDocument>.Filter.Where(expression);
            filter &= NotDeletedFilter;

            var entities = await collection.Find(filter).ToListAsync(token);

            return Result.Ok<IReadOnlyList<TDocument>>(entities);
        }, cancellationToken);

    public Task<Result<bool>> DeleteAsync(Guid id, long eTag, CancellationToken cancellationToken = default) =>
        ExecuteAsync(async (collection, token) =>
        {
            var collectionOptions = GetOptions();

            var filter = Builders<TDocument>.Filter.Eq(x => x.Id, id);
            filter &= Builders<TDocument>.Filter.Eq(x => x.ETag, eTag);

            if (collectionOptions.SoftDeleteDisabled)
            {
                return await PermanentDeleteOneAsync(collection, filter, token);
            }
            
            filter &= NotDeletedFilter;
            return await SoftDeleteOneAsync(collection, filter, token);

        }, cancellationToken);

    public Task<Result<bool>> DeleteAsync(Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default)=>
        ExecuteAsync(async (collection, token) =>
        {
            var filter = Builders<TDocument>.Filter.Where(expression);
           

            var collectionOptions = GetOptions();

            if (collectionOptions.SoftDeleteDisabled)
            {
                return await PermanentDeleteOneAsync(collection, filter, token);
            }
            
            filter &= NotDeletedFilter;
            return await SoftDeleteOneAsync(collection, filter, token);

        }, cancellationToken);
    
    #region Store

    private static async Task<Result<TDocument>> StoreEntityAsync(TDocument entity,
        IMongoCollection<TDocument> collection, CancellationToken cancellationToken = default)
    {
        var currentEtag = entity.ETag;

        entity.UpdatedNow();

        if (entity.IsNew())
        {
            await CreateAsync(entity, collection, cancellationToken);
        }
        else
        {
            var success = await UpdateAsync(entity, currentEtag, collection, cancellationToken);

            if (!success)
            {
                return Result.Fail($"Failed to update entity '{collection.CollectionNamespace}' with id {entity.Id}");
            }
        }

        return Result.Ok(entity);
    }

    private static async Task CreateAsync(TDocument entity, IMongoCollection<TDocument> collection,
        CancellationToken cancellationToken)
    {
        if (entity.IsIdEmpty())
        {
            entity.NewId();
        }
        entity.GenerateETag();

        await collection.InsertOneAsync(entity, new InsertOneOptions(), cancellationToken);
    }

    private static async Task<bool> UpdateAsync(TDocument entity, long currentEtag,
        IMongoCollection<TDocument> collection, CancellationToken cancellationToken)
    {
        var filter = Builders<TDocument>.Filter.And(
            Builders<TDocument>.Filter.Eq(x => x.Id, entity.Id),
            NotDeletedFilter,
            Builders<TDocument>.Filter.Eq(x => x.ETag, currentEtag));

        entity.GenerateETag();

        var updateDocument = entity.ToBsonUpdateDocument();

        var updateResult =
            await collection.UpdateOneAsync(filter, updateDocument, cancellationToken: cancellationToken);

        return updateResult.ModifiedCount == 1;
    }

    // TODO: move to bulk repository
    private static IEnumerable<WriteModel<TDocument>> GetUpdateModels(IEnumerable<TDocument> entities)
    {
        foreach (var entity in entities)
        {
            entity.UpdatedNow();
            entity.GenerateETag();

            var filter = Builders<TDocument>.Filter.Eq(x => x.Id, entity.Id);

            var updateDoc = entity.ToBsonUpdateDocument();

            var upsertOne = new UpdateOneModel<TDocument>(filter, updateDoc)
            {
                IsUpsert = true
            };

            yield return upsertOne;
        }
    }

    #endregion

    #region Delete
    
    private static async Task<Result<bool>> SoftDeleteOneAsync(IMongoCollection<TDocument> collection, FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
    {
        var update = Builders<TDocument>.Update.Set(x => x.IsDeleted, true);

        var result = await collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
        
        return Result.Ok(result.ModifiedCount == 1);
    }
    
    private static async Task<Result<bool>> PermanentDeleteOneAsync(IMongoCollection<TDocument> collection, FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default)
    {
        var result = await collection.DeleteOneAsync(filter, cancellationToken);
        
        return Result.Ok(result.DeletedCount == 1);
    }
    
    #endregion
}