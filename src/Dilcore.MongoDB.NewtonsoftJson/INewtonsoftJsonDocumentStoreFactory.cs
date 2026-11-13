using Dilcore.MongoDB.Abstractions.Keys;

namespace Dilcore.MongoDB.NewtonsoftJson;

/// <summary>
/// Creates <see cref="INewtonsoftJsonDocumentStore"/> instances bound to a database key.
/// </summary>
public interface INewtonsoftJsonDocumentStoreFactory
{
    /// <summary>
    /// Creates a store bound to the specified database key and optional static namespace prefix.
    /// </summary>
    /// <param name="databaseKey">The database binding key.</param>
    /// <param name="staticPrefix">Optional fixed namespace prefix; tenant resolution applies when omitted.</param>
    /// <returns>A Newtonsoft.Json document store for the binding.</returns>
    INewtonsoftJsonDocumentStore Create(MongoDatabaseKey databaseKey, string? staticPrefix = null);
}