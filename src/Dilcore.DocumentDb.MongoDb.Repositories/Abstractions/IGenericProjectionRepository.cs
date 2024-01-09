using System.Linq.Expressions;
using Dilcore.DocumentDb.Abstractions;
using FluentResults;

namespace Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;

public interface IGenericProjectionRepository<TDocument>
    where TDocument : IDocumentEntity
{
    Task<Result<TProjection>> GetAsync<TProjection>(Expression<Func<TDocument, bool>> expression, Expression<Func<TDocument, TProjection>> projection)
        where TProjection : class;
    
    Task<Result<TProjection>> GetAsync<TProjection>(Expression<Func<TDocument, TProjection>> projection)
        where TProjection : class;
    
    Task<Result<IEnumerable<TProjection>>> GetListAsync<TProjection>(Expression<Func<TDocument, bool>> expression, Expression<Func<TDocument, TProjection>> projection)
        where TProjection : class;
    
    Task<Result<IEnumerable<TProjection>>> GetListAsync<TProjection>(Expression<Func<TDocument, TProjection>> projection)
        where TProjection : class;
}