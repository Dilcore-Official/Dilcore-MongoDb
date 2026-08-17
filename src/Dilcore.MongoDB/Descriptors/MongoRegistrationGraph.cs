using Dilcore.MongoDB.Abstractions.Keys;

namespace Dilcore.MongoDB.Descriptors;

internal sealed class MongoRegistrationGraph
{
    public required IReadOnlyList<ClusterDescriptor> Clusters { get; init; }
    public required IReadOnlyList<DatabaseDescriptor> Databases { get; init; }
    public required IReadOnlyList<DocumentBindingDescriptor> Bindings { get; init; }

    public ClusterDescriptor GetCluster(MongoClusterKey key) =>
        Clusters.First(c => c.Key.Equals(key));

    public DatabaseDescriptor GetDatabase(MongoDatabaseKey key) =>
        Databases.First(d => d.Key.Equals(key));

    public DocumentBindingDescriptor GetBinding(MongoDocumentBindingKey key) =>
        Bindings.First(b => b.Key.Equals(key));
}
