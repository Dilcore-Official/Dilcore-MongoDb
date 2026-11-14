using Dilcore.MongoDB.Abstractions.Json;

namespace Dilcore.MongoDB.SystemTextJson;

internal sealed class SystemTextJsonDocumentStoreFactory(IJsonDocumentStoreFactory storeFactory)
    : ISystemTextJsonDocumentStoreFactory
{
    public ISystemTextJsonDocumentStore Create(
        Dilcore.MongoDB.Abstractions.Keys.MongoDatabaseKey databaseKey,
        string? staticPrefix = null) =>
        new SystemTextJsonDocumentStore(storeFactory.Create(databaseKey, staticPrefix));
}