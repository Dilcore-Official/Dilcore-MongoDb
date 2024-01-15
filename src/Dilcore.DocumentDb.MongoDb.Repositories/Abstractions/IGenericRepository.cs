using System.Linq.Expressions;
using Dilcore.DocumentDb.Abstractions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;

public interface IGenericRepository<TDocument>
    where TDocument : IDocumentEntity
{
    Task<Result<TDocument>> StoreAsync(TDocument entity, CancellationToken cancellationToken = default);

    Task<Result<TDocument>> GetAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);

    Task<Result<TDerived>> GetAsync<TDerived>(FilterDefinition<TDerived> filter,
        CancellationToken cancellationToken = default)
        where TDerived : class, TDocument;

    Task<Result<IReadOnlyList<TDocument>>> GetListAsync(CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TDocument>>> GetListAsync(FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<TDerived>>> GetListAsync<TDerived>(FilterDefinition<TDerived> filter,
        CancellationToken cancellationToken = default)
        where TDerived : class, TDocument;

    Task<Result<bool>> DeleteAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);
}