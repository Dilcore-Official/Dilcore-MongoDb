using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions;

/// <summary>
/// Resolves collections by document binding or by logical name through the namespace pipeline.
/// </summary>
public interface IMongoDbCollectionFactory
{
    /// <summary>
    /// Gets the typed collection registered for <paramref name="bindingKey"/>.
    /// </summary>
    /// <typeparam name="TDocument">Document type that must match the binding registration.</typeparam>
    /// <param name="bindingKey">Registered document binding.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful collection handle, or errors when the key, type, or namespace resolution fails.</returns>
    Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(
        MongoDocumentBindingKey bindingKey,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity;

    /// <summary>
    /// Gets an untyped <see cref="BsonDocument"/> collection by logical name.
    /// </summary>
    /// <param name="databaseKey">Database that owns the collection.</param>
    /// <param name="logicalCollectionName">Logical collection name before namespace prefixes.</param>
    /// <param name="staticPrefix">Optional static prefix from registration (see <see cref="Namespace.NamespaceResolutionRequest.StaticPrefix"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful collection handle, or errors when resolution fails.</returns>
    Task<Result<IMongoCollection<BsonDocument>>> GetCollectionAsync(
        MongoDatabaseKey databaseKey,
        string logicalCollectionName,
        string? staticPrefix = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resolves the physical collection name without opening a collection handle.
    /// </summary>
    /// <param name="databaseKey">Database that owns the collection.</param>
    /// <param name="logicalCollectionName">Logical collection name before namespace prefixes.</param>
    /// <param name="staticPrefix">Optional static prefix from registration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The physical collection name, or errors when resolution fails.</returns>
    Task<Result<string>> ResolveCollectionNameAsync(
        MongoDatabaseKey databaseKey,
        string logicalCollectionName,
        string? staticPrefix = null,
        CancellationToken cancellationToken = default);
}