using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;

namespace Dilcore.MongoDB.Json;

internal sealed class JsonDocumentStoreFactory(
    IMongoDbCollectionFactory collectionFactory,
    IBsonJsonConverter converter) : IJsonDocumentStoreFactory
{
    public IJsonDocumentStore Create(MongoDatabaseKey databaseKey, string? staticPrefix = null)
    {
        return new JsonDocumentStore(collectionFactory, converter, databaseKey, staticPrefix);
    }
}
