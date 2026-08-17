using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Ownership;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Descriptors;

internal sealed record ClusterDescriptor(
    MongoClusterKey Key,
    string? ConnectionString,
    int MaxConnectionPoolSize,
    IMongoClient? ExistingClient,
    MongoClientOwnership Ownership);
