using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;

namespace Dilcore.MongoDB.NewtonsoftJson;

internal sealed class NewtonsoftJsonDocumentStoreFactory(IJsonDocumentStoreFactory storeFactory)
    : INewtonsoftJsonDocumentStoreFactory
{
    public INewtonsoftJsonDocumentStore Create(MongoDatabaseKey databaseKey, string? staticPrefix = null) =>
        new NewtonsoftJsonDocumentStore(storeFactory.Create(databaseKey, staticPrefix));
}