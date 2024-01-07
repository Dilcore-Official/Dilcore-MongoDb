using System.Linq.Expressions;
using FluentResults;

namespace Dilcore.DocumentDb.Abstractions;

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