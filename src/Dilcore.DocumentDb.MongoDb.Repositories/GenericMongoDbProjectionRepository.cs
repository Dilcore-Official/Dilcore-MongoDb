using System.Linq.Expressions;
using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.Abstractions.Repositories;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Repositories;

public class GenericMongoDbProjectionRepository<TDocument>(
    Action<GetCollectionOptions<TDocument>> optionsAction,
    Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> collectionProvider)
    : BaseMongoDbRepository<TDocument>(optionsAction, collectionProvider),
        IGenericProjectionRepository<TDocument>
    where TDocument : class, IDocumentEntity
{
    public Task<Result<TProjection>> GetAsync<TProjection>(FilterDefinition<TDocument> filter,
        Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default)
        where TProjection : class
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);

            var entity = await collection
                .Find(filter)
                .Project(projection)
                .FirstOrDefaultAsync(token);

            return Result.Ok(entity);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<TProjection>>> GetListAsync<TProjection>(FilterDefinition<TDocument> filter,
        Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default)
        where TProjection : class
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);

            var entities = await collection
                .Find(filter)
                .Project(projection)
                .ToListAsync(token);

            return Result.Ok<IReadOnlyList<TProjection>>(entities);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<TProjection>>> GetListAsync<TProjection>(
        Expression<Func<TDocument, TProjection>> projection,
        CancellationToken cancellationToken = default)
        where TProjection : class
        => ExecuteAsync(async (collection, token) =>
        {
            var filter = ApplyNotDeleteFilter();

            var entities = await collection
                .Find(filter)
                .Project(projection)
                .ToListAsync(token);

            return Result.Ok<IReadOnlyList<TProjection>>(entities);
        }, cancellationToken);
}