using System.Linq.Expressions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Repositories;

/// <summary>
/// Read-only queries that project documents into smaller shapes without loading full entities.
/// </summary>
/// <typeparam name="TDocument">Document type stored in the collection.</typeparam>
public interface IGenericProjectionRepository<TDocument>
    where TDocument : IDocumentEntity
{
    /// <summary>
    /// Finds the first document matching <paramref name="filter"/> and returns the projected shape.
    /// </summary>
    /// <typeparam name="TProjection">Projected result type.</typeparam>
    /// <param name="filter">MongoDB filter (soft-delete constraints apply when enabled).</param>
    /// <param name="projection">Field selection expression.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with the projection; a failed result includes <see cref="Results.DocumentNotFoundError"/> when no document matches.
    /// </returns>
    Task<Result<TProjection>> GetAsync<TProjection>(FilterDefinition<TDocument> filter,
        Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default)
        where TProjection : class;

    /// <summary>
    /// Finds all documents matching <paramref name="filter"/> and projects each to <typeparamref name="TProjection"/>.
    /// </summary>
    /// <typeparam name="TProjection">Projected result type.</typeparam>
    /// <param name="filter">MongoDB filter (soft-delete constraints apply when enabled).</param>
    /// <param name="projection">Field selection expression.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with the list of projections; otherwise a failed result.</returns>
    Task<Result<IReadOnlyList<TProjection>>> GetListAsync<TProjection>(FilterDefinition<TDocument> filter,
        Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default)
        where TProjection : class;

    /// <summary>
    /// Projects all non-deleted documents in the collection when soft delete is enabled.
    /// </summary>
    /// <typeparam name="TProjection">Projected result type.</typeparam>
    /// <param name="projection">Field selection expression.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>A successful result with the list of projections; otherwise a failed result.</returns>
    Task<Result<IReadOnlyList<TProjection>>> GetListAsync<TProjection>(
        Expression<Func<TDocument, TProjection>> projection, CancellationToken cancellationToken = default)
        where TProjection : class;
}