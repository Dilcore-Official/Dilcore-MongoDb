using Dilcore.MongoDB.Abstractions.Options;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Repositories;

/// <summary>
/// CRUD and query operations for a single registered document type, with optional concurrency, audit, and soft-delete policies.
/// </summary>
/// <typeparam name="TDocument">Document type stored in the collection.</typeparam>
public interface IGenericRepository<TDocument>
    where TDocument : IDocumentEntity
{
    /// <summary>
    /// Inserts a new document or updates an existing one according to identifier and concurrency rules.
    /// </summary>
    /// <param name="entity">Document to store.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with the persisted entity; failed results may include <see cref="Results.ConcurrencyConflictError"/> or other <see cref="Results.MongoOperationError"/> types.
    /// </returns>
    Task<Result<TDocument>> StoreAsync(TDocument entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces the entire document identified by <paramref name="entity"/> (fails when concurrency token does not match).
    /// </summary>
    /// <param name="entity">Replacement document.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with the stored entity; otherwise a typed failure.</returns>
    Task<Result<TDocument>> ReplaceAsync(TDocument entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates or partially updates a document snapshot, refreshing concurrency and audit fields when configured.
    /// </summary>
    /// <param name="entity">Document state to persist.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with the stored entity; otherwise a typed failure.</returns>
    Task<Result<TDocument>> UpdateSnapshotAsync(TDocument entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies <paramref name="update"/> to the first document matching <paramref name="filter"/> and returns the updated document.
    /// </summary>
    /// <param name="filter">MongoDB filter (not-deleted constraint applies when soft delete is enabled).</param>
    /// <param name="update">Update definition to apply.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with the updated document; <see cref="Results.DocumentNotFoundError"/> or <see cref="Results.ConcurrencyConflictError"/> on failure.
    /// </returns>
    Task<Result<TDocument>> PatchAsync(
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first document matching <paramref name="filter"/>.
    /// </summary>
    /// <param name="filter">MongoDB filter (not-deleted constraint applies when soft delete is enabled).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with the entity; <see cref="Results.DocumentNotFoundError"/> when no match.
    /// </returns>
    Task<Result<TDocument>> GetAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the first document of type <typeparamref name="TDerived"/> matching <paramref name="filter"/>.
    /// </summary>
    /// <typeparam name="TDerived">Derived document type to query.</typeparam>
    /// <param name="filter">MongoDB filter for the derived type.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with the entity; <see cref="Results.DocumentNotFoundError"/> when no match.
    /// </returns>
    Task<Result<TDerived>> GetAsync<TDerived>(FilterDefinition<TDerived> filter, CancellationToken cancellationToken = default)
        where TDerived : class, TDocument;

    /// <summary>
    /// Returns all non-deleted documents in the collection when soft delete is enabled.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with the list of entities; otherwise a failed result.</returns>
    Task<Result<IReadOnlyList<TDocument>>> GetListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all documents matching <paramref name="filter"/>.
    /// </summary>
    /// <param name="filter">MongoDB filter.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with matching entities; otherwise a failed result.</returns>
    Task<Result<IReadOnlyList<TDocument>>> GetListAsync(
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns documents matching <paramref name="filter"/> using optional driver find options.
    /// </summary>
    /// <param name="filter">MongoDB filter.</param>
    /// <param name="options">Optional sort, limit, and projection settings.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with matching entities; otherwise a failed result.</returns>
    Task<Result<IReadOnlyList<TDocument>>> GetListAsync(
        FilterDefinition<TDocument> filter,
        FindOptions<TDocument, TDocument>? options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all documents of type <typeparamref name="TDerived"/> matching <paramref name="filter"/>.
    /// </summary>
    /// <typeparam name="TDerived">Derived document type to query.</typeparam>
    /// <param name="filter">MongoDB filter.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with matching entities; otherwise a failed result.</returns>
    Task<Result<IReadOnlyList<TDerived>>> GetListAsync<TDerived>(
        FilterDefinition<TDerived> filter,
        CancellationToken cancellationToken = default)
        where TDerived : class, TDocument;

    /// <summary>
    /// Returns a keyset-paginated slice of documents for stable cursor-based paging.
    /// </summary>
    /// <param name="request">Page size, sort keys, and optional cursor.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with items and next cursor; otherwise a failed result (for example invalid page size).</returns>
    Task<Result<KeysetPage<TDocument>>> GetPageAsync(
        KeysetPageRequest<TDocument> request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams documents matching <paramref name="filter"/> without loading the full result set into memory.
    /// </summary>
    /// <param name="filter">MongoDB filter.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    /// <returns>An async sequence of documents; throws when collection resolution fails.</returns>
    IAsyncEnumerable<TDocument> GetAsyncEnumerable(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams documents of type <typeparamref name="TDerived"/> matching <paramref name="filter"/>.
    /// </summary>
    /// <typeparam name="TDerived">Derived document type to enumerate.</typeparam>
    /// <param name="filter">MongoDB filter.</param>
    /// <param name="cancellationToken">Token used to cancel enumeration.</param>
    /// <returns>An async sequence of documents; throws when collection resolution fails.</returns>
    IAsyncEnumerable<TDerived> GetAsyncEnumerable<TDerived>(FilterDefinition<TDerived> filter, CancellationToken cancellationToken = default)
        where TDerived : class, TDocument;

    /// <summary>
    /// Soft-deletes or permanently deletes the first document matching <paramref name="filter"/>.
    /// </summary>
    /// <param name="filter">MongoDB filter.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with <see langword="true"/> when a document was deleted; otherwise a failure such as <see cref="Results.DocumentNotFoundError"/>.
    /// </returns>
    Task<Result<bool>> DeleteAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the soft-delete flag on the first matching document when the type supports soft delete.
    /// </summary>
    /// <param name="filter">MongoDB filter (matched among deleted documents).</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with <see langword="true"/> when restored; otherwise a failure.
    /// </returns>
    Task<Result<bool>> RestoreAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently removes all documents matching <paramref name="filter"/> (ignores soft-delete semantics).
    /// </summary>
    /// <param name="filter">MongoDB filter.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with the number of deleted documents; otherwise a failed result.</returns>
    Task<Result<long>> PurgeAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines whether any document matches <paramref name="filter"/>.
    /// </summary>
    /// <param name="filter">MongoDB filter.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with the existence flag; otherwise a failed result.</returns>
    Task<Result<bool>> HasAnyAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts documents matching <paramref name="filter"/> (excludes soft-deleted when enabled).
    /// </summary>
    /// <param name="filter">MongoDB filter.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with the document count; otherwise a failed result.</returns>
    Task<Result<long>> CountAsync(FilterDefinition<TDocument> filter, CancellationToken cancellationToken = default);
}