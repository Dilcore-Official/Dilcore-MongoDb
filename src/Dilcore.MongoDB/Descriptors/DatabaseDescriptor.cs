using Dilcore.MongoDB.Abstractions.Keys;

namespace Dilcore.MongoDB.Descriptors;

internal sealed record DatabaseDescriptor(
    MongoDatabaseKey Key,
    MongoClusterKey ClusterKey,
    string? NamespacePrefix);
