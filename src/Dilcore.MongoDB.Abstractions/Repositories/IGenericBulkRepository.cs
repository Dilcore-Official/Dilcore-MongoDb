using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions.Options;
using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Repositories;

/// <summary>
/// Bulk insert and delete operations for a registered document type.
/// </summary>
/// <typeparam name="TDocument">Document type stored in the collection.</typeparam>
public interface IGenericBulkRepository<TDocument>
    where TDocument : IDocumentEntity
{
    /// <summary>
    /// Upserts or inserts the given entities using default bulk write options.
    /// </summary>
    /// <param name="entities">Documents to write.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with the stored entities; a failed result may include <see cref="Results.BulkWritePartialFailureError"/> or other <see cref="Results.MongoOperationError"/> types.
    /// </returns>
    Task<Result<IReadOnlyList<TDocument>>> BulkStoreAsync(
        TDocument[] entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts or inserts the given entities with explicit batching and ordering options.
    /// </summary>
    /// <param name="entities">Documents to write.</param>
    /// <param name="options">Bulk write batching and ordering settings.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with the stored entities; otherwise a failed result with typed Mongo operation errors when applicable.
    /// </returns>
    Task<Result<IReadOnlyList<TDocument>>> BulkStoreAsync(
        TDocument[] entities,
        MongoBulkWriteOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts or inserts a sequence of entities using default bulk write options.
    /// </summary>
    /// <param name="entities">Documents to write.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with the stored entities; otherwise a failed result.
    /// </returns>
    Task<Result<IReadOnlyList<TDocument>>> BulkStoreRangeAsync(
        IEnumerable<TDocument> entities,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upserts or inserts a sequence of entities with explicit bulk write options.
    /// </summary>
    /// <param name="entities">Documents to write.</param>
    /// <param name="options">Bulk write batching and ordering settings.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with the stored entities; otherwise a failed result.
    /// </returns>
    Task<Result<IReadOnlyList<TDocument>>> BulkStoreRangeAsync(
        IEnumerable<TDocument> entities,
        MongoBulkWriteOptions options,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes documents matching <paramref name="expression"/> (soft delete when enabled, otherwise permanent delete).
    /// </summary>
    /// <param name="expression">Predicate that selects documents to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result when at least one document was deleted; otherwise a failed result (for example no match).
    /// </returns>
    Task<Result> BulkDeleteAsync(
        Expression<Func<TDocument, bool>> expression,
        CancellationToken cancellationToken = default);
}