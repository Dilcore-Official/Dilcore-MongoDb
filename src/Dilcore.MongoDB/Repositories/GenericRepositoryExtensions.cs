using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories;

/// <summary>
/// Convenience extensions that map identifiers and LINQ predicates to repository filter operations.
/// </summary>
public static class GenericRepositoryExtensions
{
    /// <summary>
    /// Gets a document whose identifier equals <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <typeparam name="TId">The identifier type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching document or a not-found or driver error result.</returns>
    public static Task<Result<TDocument>> GetAsync<TDocument, TId>(
        this IGenericRepository<TDocument> repository,
        TId id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<TId>
        => repository.GetAsync(IdEquals<TDocument, TId>(id), cancellationToken);

    /// <summary>
    /// Gets a document whose <see cref="Guid"/> identifier equals <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TDocument">The document type with a <see cref="Guid"/> id.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching document or a not-found or driver error result.</returns>
    public static Task<Result<TDocument>> GetAsync<TDocument>(
        this IGenericRepository<TDocument> repository,
        Guid id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<Guid>
        => repository.GetAsync<TDocument, Guid>(id, cancellationToken);

    /// <summary>
    /// Gets a derived document whose identifier equals <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TDocument">The base document type stored in the repository.</typeparam>
    /// <typeparam name="TDerived">The derived type to materialize.</typeparam>
    /// <typeparam name="TId">The identifier type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching derived document or a not-found or driver error result.</returns>
    public static Task<Result<TDerived>> GetAsync<TDocument, TDerived, TId>(
        this IGenericRepository<TDocument> repository,
        TId id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<TId>
        where TDerived : class, TDocument
        => repository.GetAsync(IdEquals<TDerived, TId>(id), cancellationToken);

    /// <summary>
    /// Gets a derived document whose <see cref="Guid"/> identifier equals <paramref name="id"/>.
    /// </summary>
    /// <typeparam name="TDocument">The base document type stored in the repository.</typeparam>
    /// <typeparam name="TDerived">The derived type to materialize.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching derived document or a not-found or driver error result.</returns>
    public static Task<Result<TDerived>> GetAsync<TDocument, TDerived>(
        this IGenericRepository<TDocument> repository,
        Guid id,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<Guid>
        where TDerived : class, TDocument
        => repository.GetAsync<TDocument, TDerived, Guid>(id, cancellationToken);

    /// <summary>
    /// Gets a single document matching the LINQ predicate.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="expression">Filter predicate translated to a MongoDB filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching document or a not-found or driver error result.</returns>
    public static Task<Result<TDocument>> GetAsync<TDocument>(
        this IGenericRepository<TDocument> repository,
        Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
        => repository.GetAsync(Builders<TDocument>.Filter.Where(expression), cancellationToken);

    /// <summary>
    /// Lists documents matching the LINQ predicate.
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="expression">Filter predicate translated to a MongoDB filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching documents or a driver error result.</returns>
    public static Task<Result<IReadOnlyList<TDocument>>> GetListAsync<TDocument>(
        this IGenericRepository<TDocument> repository,
        Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
        => repository.GetListAsync(Builders<TDocument>.Filter.Where(expression), cancellationToken);

    /// <summary>
    /// Lists derived documents matching the LINQ predicate.
    /// </summary>
    /// <typeparam name="TDocument">The base document type stored in the repository.</typeparam>
    /// <typeparam name="TDerived">The derived type to materialize.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="expression">Filter predicate on <typeparamref name="TDerived"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Matching derived documents or a driver error result.</returns>
    public static Task<Result<IReadOnlyList<TDerived>>> GetListAsync<TDocument, TDerived>(
        this IGenericRepository<TDocument> repository,
        Expression<Func<TDerived, bool>> expression,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
        where TDerived : class, TDocument
        => repository.GetListAsync(Builders<TDerived>.Filter.Where(expression), cancellationToken);

    /// <summary>
    /// Deletes a document by identifier when its concurrency <c>ETag</c> matches <paramref name="eTag"/>.
    /// </summary>
    /// <typeparam name="TDocument">The document type with concurrency tokens.</typeparam>
    /// <typeparam name="TId">The identifier type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="eTag">Expected concurrency token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a document was deleted; otherwise <see langword="false"/> or an error result.</returns>
    public static Task<Result<bool>> DeleteAsync<TDocument, TId>(
        this IGenericRepository<TDocument> repository,
        TId id,
        long eTag,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<TId>, IHasConcurrencyToken
        => repository.DeleteAsync(IdAndETagEquals<TDocument, TId>(id, eTag), cancellationToken);

    /// <summary>
    /// Deletes a document by <see cref="Guid"/> identifier when its concurrency <c>ETag</c> matches <paramref name="eTag"/>.
    /// </summary>
    /// <typeparam name="TDocument">The document type with a <see cref="Guid"/> id and concurrency token.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="id">The document identifier.</param>
    /// <param name="eTag">Expected concurrency token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when a document was deleted; otherwise <see langword="false"/> or an error result.</returns>
    public static Task<Result<bool>> DeleteAsync<TDocument>(
        this IGenericRepository<TDocument> repository,
        Guid id,
        long eTag,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity<Guid>, IHasConcurrencyToken
        => repository.DeleteAsync<TDocument, Guid>(id, eTag, cancellationToken);

    /// <summary>
    /// Deletes documents matching the LINQ predicate (hard delete or soft delete per repository policy).
    /// </summary>
    /// <typeparam name="TDocument">The document type.</typeparam>
    /// <param name="repository">The repository instance.</param>
    /// <param name="expression">Filter predicate translated to a MongoDB filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when at least one document was deleted; otherwise <see langword="false"/> or an error result.</returns>
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