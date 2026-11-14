using Dilcore.MongoDB.Abstractions.Keys;

namespace Dilcore.MongoDB.Abstractions.Json;

/// <summary>
/// Creates <see cref="IJsonDocumentStore"/> instances bound to a database key.
/// </summary>
public interface IJsonDocumentStoreFactory
{
    /// <summary>
    /// Creates a store scoped to <paramref name="databaseKey"/> and an optional static namespace prefix.
    /// </summary>
    /// <param name="databaseKey">Registered logical database.</param>
    /// <param name="staticPrefix">Optional prefix applied to every collection resolution (see <see cref="Namespace.NamespaceResolutionRequest.StaticPrefix"/>).</param>
    /// <returns>A document store that shares the typed pipeline's namespace rules.</returns>
    IJsonDocumentStore Create(MongoDatabaseKey databaseKey, string? staticPrefix = null);
}