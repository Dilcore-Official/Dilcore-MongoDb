using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories;

public static class GenericRepositoryExtensions
{
    public static Task<Result<TDocument>> GetAsync<TDocument, TId>(
        this IGenericRepository<TDocument> repository,
        TId id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<TId>
        => repository.GetAsync(IdEquals<TDocument, TId>(id), cancellationToken);

    public static Task<Result<TDocument>> GetAsync<TDocument>(
        this IGenericRepository<TDocument> repository,
        Guid id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<Guid>
        => repository.GetAsync<TDocument, Guid>(id, cancellationToken);

    public static Task<Result<TDerived>> GetAsync<TDocument, TDerived, TId>(
        this IGenericRepository<TDocument> repository,
        TId id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<TId>
        where TDerived : class, TDocument
        => repository.GetAsync(IdEquals<TDerived, TId>(id), cancellationToken);

    public static Task<Result<TDerived>> GetAsync<TDocument, TDerived>(
        this IGenericRepository<TDocument> repository,
        Guid id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<Guid>
        where TDerived : class, TDocument
        => repository.GetAsync<TDocument, TDerived, Guid>(id, cancellationToken);

    public static Task<Result<TDocument>> GetAsync<TDocument>(
        this IGenericRepository<TDocument> repository,
        Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
        => repository.GetAsync(Builders<TDocument>.Filter.Where(expression), cancellationToken);

    public static Task<Result<IReadOnlyList<TDocument>>> GetListAsync<TDocument>(
        this IGenericRepository<TDocument> repository,
        Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
        => repository.GetListAsync(Builders<TDocument>.Filter.Where(expression), cancellationToken);

    public static Task<Result<IReadOnlyList<TDerived>>> GetListAsync<TDocument, TDerived>(
        this IGenericRepository<TDocument> repository,
        Expression<Func<TDerived, bool>> expression,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
        where TDerived : class, TDocument
        => repository.GetListAsync(Builders<TDerived>.Filter.Where(expression), cancellationToken);

    public static Task<Result<bool>> DeleteAsync<TDocument, TId>(
        this IGenericRepository<TDocument> repository,
        TId id,
        long eTag,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<TId>, IHasConcurrencyToken
        => repository.DeleteAsync(IdAndETagEquals<TDocument, TId>(id, eTag), cancellationToken);

    public static Task<Result<bool>> DeleteAsync<TDocument>(
        this IGenericRepository<TDocument> repository,
        Guid id,
        long eTag,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<Guid>, IHasConcurrencyToken
        => repository.DeleteAsync<TDocument, Guid>(id, eTag, cancellationToken);

    public static Task<Result<bool>> DeleteAsync<TDocument>(
        this IGenericRepository<TDocument> repository,
        Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
        => repository.DeleteAsync(Builders<TDocument>.Filter.Where(expression), cancellationToken);

    private static FilterDefinition<TDocument> IdEquals<TDocument, TId>(TId id)
        where TDocument : class, IDocumentEntity<TId>
        => Builders<TDocument>.Filter.Eq(x => x.Id, id);

    private static FilterDefinition<TDocument> IdAndETagEquals<TDocument, TId>(TId id, long eTag)
        where TDocument : class, IDocumentEntity<TId>, IHasConcurrencyToken
        => IdEquals<TDocument, TId>(id) & Builders<TDocument>.Filter.Eq(x => x.ETag, eTag);
}
