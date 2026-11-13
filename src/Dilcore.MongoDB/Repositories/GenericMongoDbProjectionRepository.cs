using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Internal;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories;

internal class GenericMongoDbProjectionRepository<TDocument> : BaseMongoDbRepository<TDocument>,
    IGenericProjectionRepository<TDocument>
    where TDocument : class, IDocumentEntity
{
    private readonly MongoCallContext _callContext;

    public GenericMongoDbProjectionRepository(
        Action<GetCollectionOptions<TDocument>> optionsAction,
        Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> collectionProvider,
        MongoCallContext? callContext = null)
        : base(optionsAction, collectionProvider)
    {
        _callContext = callContext ?? MongoCallContext.None;
        Session = _callContext.Session;
        Budget = _callContext.Budget;
    }

    public Task<Result<TProjection>> GetAsync<TProjection>(
        FilterDefinition<TDocument> filter,
        Expression<Func<TDocument, TProjection>> projection,
        CancellationToken cancellationToken = default)
        where TProjection : class
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var entity = await MongoCollectionCalls.Find(collection, _callContext.Session, filter)
                .Project(projection)
                .FirstOrDefaultAsync(token);
            return entity is null
                ? Result.Fail<TProjection>(new DocumentNotFoundError())
                : Result.Ok(entity);
        }, cancellationToken);

    public Task<Result<IReadOnlyList<TProjection>>> GetListAsync<TProjection>(
        FilterDefinition<TDocument> filter,
        Expression<Func<TDocument, TProjection>> projection,
        CancellationToken cancellationToken = default)
        where TProjection : class
        => ExecuteAsync(async (collection, token) =>
        {
            filter = ApplyNotDeleteFilter(filter);
            var entities = await MongoCollectionCalls.Find(collection, _callContext.Session, filter)
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
            var entities = await MongoCollectionCalls.Find(collection, _callContext.Session, filter)
                .Project(projection)
                .ToListAsync(token);
            return Result.Ok<IReadOnlyList<TProjection>>(entities);
        }, cancellationToken);
}
