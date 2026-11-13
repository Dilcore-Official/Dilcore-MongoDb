using Dilcore.MongoDB.Abstractions.Keys;

namespace Dilcore.MongoDB.SystemTextJson;

/// <summary>
/// Creates <see cref="ISystemTextJsonDocumentStore"/> instances bound to a database key.
/// </summary>
public interface ISystemTextJsonDocumentStoreFactory
{
    /// <summary>
    /// Creates a store bound to the specified database key and optional static namespace prefix.
    /// </summary>
    /// <param name="databaseKey">The database binding key.</param>
    /// <param name="staticPrefix">Optional fixed namespace prefix; tenant resolution applies when omitted.</param>
    /// <returns>A System.Text.Json document store for the binding.</returns>
    ISystemTextJsonDocumentStore Create(MongoDatabaseKey databaseKey, string? staticPrefix = null);
}