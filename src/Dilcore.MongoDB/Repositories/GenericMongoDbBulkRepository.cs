using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Extensions;
using Dilcore.MongoDB.Abstractions.Helpers;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories;

internal class GenericMongoDbBulkRepository<TDocument>(
    Action<GetCollectionOptions<TDocument>> options,
    Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> collectionProvider)
    : BaseMongoDbRepository<TDocument>(options, collectionProvider), IGenericBulkRepository<TDocument>
    where TDocument : class, IDocumentEntity
{
    public Task<Result<IReadOnlyList<TDocument>>> BulkStoreAsync(
        TDocument[] entities,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            var writeModels = CreateWriteModels(entities).ToList();
            var result = await collection.BulkWriteAsync(writeModels, cancellationToken: token);

            if (result.RequestCount != writeModels.Count)
            {
                return Result.Fail("Not all entities were processed");
            }

            if (result.InsertedCount != writeModels.Count(x => x.ModelType == WriteModelType.InsertOne))
            {
                return Result.Fail("Not all entities were created");
            }

            if (result.ModifiedCount != writeModels.Count(x => x.ModelType == WriteModelType.UpdateOne))
            {
                return Result.Fail("Not all entities were updated");
            }

            return Result.Ok<IReadOnlyList<TDocument>>(entities);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<TDocument>>> BulkStoreRangeAsync(
        IEnumerable<TDocument> entities,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(async (collection, token) =>
        {
            var entityList = entities as IReadOnlyList<TDocument> ?? entities.ToList();

            if (entityList.Count == 0)
            {
                return Result.Ok<IReadOnlyList<TDocument>>(entityList);
            }

            var writeModels = CreateWriteModels(entityList).ToList();
            var result = await collection.BulkWriteAsync(writeModels, cancellationToken: token);

            if (result.RequestCount != writeModels.Count)
            {
                return Result.Fail("Not all entities were processed");
            }

            if (result.InsertedCount != writeModels.Count(x => x.ModelType == WriteModelType.InsertOne))
            {
                return Result.Fail("Not all entities were created");
            }

            if (result.ModifiedCount != writeModels.Count(x => x.ModelType == WriteModelType.UpdateOne))
            {
                return Result.Fail("Not all entities were updated");
            }

            return Result.Ok<IReadOnlyList<TDocument>>(entityList);
        }, cancellationToken);

    public Task<Result> BulkDeleteAsync(
        Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default)
        => ExecuteAsync((collection, token) =>
        {
            var collectionOptions = GetOptions();
            var filter = Builders<TDocument>.Filter.Where(expression);

            if (collectionOptions.SoftDeleteDisabled)
            {
                return PermanentDeleteAsync(collection, filter, token);
            }

            filter = ApplyNotDeleteFilter(filter);
            return SoftDeleteManyAsync(collection, filter, token);
        }, cancellationToken);

    private static async Task<Result> SoftDeleteManyAsync(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        CancellationToken token)
    {
        var update = Builders<TDocument>.Update.Set(x => x.IsDeleted, true)
            .Set(x => x.ETag, MongoDbHelper.GenerateEtag());

        var result = await collection.UpdateManyAsync(filter, update, cancellationToken: token);
        return result.MatchedCount == 0 ? Result.Fail("No entities were deleted") : Result.Ok();
    }

    private static async Task<Result> PermanentDeleteAsync(
        IMongoCollection<TDocument> collection,
        FilterDefinition<TDocument> filter,
        CancellationToken token)
    {
        var result = await collection.DeleteManyAsync(filter, token);
        return result.DeletedCount == 0 ? Result.Fail("No entities were deleted") : Result.Ok();
    }

    private IEnumerable<WriteModel<TDocument>> CreateWriteModels(IEnumerable<TDocument> entities)
    {
        foreach (var entity in entities)
        {
            if (entity.IsNew())
            {
                if (entity.IsIdEmpty())
                {
                    entity.NewId();
                }

                entity.GenerateETag();
                entity.CreatedNow();
                entity.UpdatedNow();

                yield return new InsertOneModel<TDocument>(entity);
                continue;
            }

            var currentETag = entity.ETag;
            entity.GenerateETag();
            entity.UpdatedNow();

            var filter = Builders<TDocument>.Filter.Eq(x => x.Id, entity.Id);
            filter &= Builders<TDocument>.Filter.Eq(x => x.ETag, currentETag);
            filter = ApplyNotDeleteFilter(filter);

            var updateDocument = entity.ToBsonUpdateDocument();
            yield return new UpdateOneModel<TDocument>(filter, updateDocument);
        }
    }
}
