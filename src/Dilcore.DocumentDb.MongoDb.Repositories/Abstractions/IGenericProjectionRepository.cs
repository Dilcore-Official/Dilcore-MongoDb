using System.Linq.Expressions;
using Dilcore.DocumentDb.Abstractions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;

public interface IGenericProjectionRepository<TDocument>
    where TDocument : IDocumentEntity
{
    Task<Result<TProjection>> GetAsync<TProjection>(FilterDefinition<TDocument> filter,
        Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default)
        where TProjection : class;
    
    Task<Result<IReadOnlyList<TProjection>>> GetListAsync<TProjection>(FilterDefinition<TDocument> filter,
        Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default)
        where TProjection : class;
    
    Task<Result<IReadOnlyList<TProjection>>> GetListAsync<TProjection>(
        Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default)
        where TProjection : class;
}