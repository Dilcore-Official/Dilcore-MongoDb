using System.Linq.Expressions;
using FluentResults;

namespace Dilcore.DocumentDb.Abstractions;

public interface IGenericRepository<TDocument>
    where TDocument : IDocumentEntity
{
    Task<Result<TDocument>> StoreAsync(TDocument entity, CancellationToken cancellationToken = default);

    Task<Result<TDocument>> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<TDocument>> GetAsync(Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default);
    
    Task<Result<IReadOnlyList<TDocument>>> GetListAsync(CancellationToken cancellationToken = default);
    Task<Result<IReadOnlyList<TDocument>>> GetListAsync(Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default);
    
    Task<Result<bool>> DeleteAsync(Guid id, long eTag, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default);
}