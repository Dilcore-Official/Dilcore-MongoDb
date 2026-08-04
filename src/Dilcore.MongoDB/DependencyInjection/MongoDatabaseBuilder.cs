using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Descriptors;

namespace Dilcore.MongoDB.DependencyInjection;

internal sealed class MongoDatabaseBuilder : IMongoDatabaseBuilder
{
    private string? _clusterName;
    private string? _namespacePrefix;

    public IMongoDatabaseBuilder OnCluster(string clusterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterName);
        _clusterName = clusterName;
        return this;
    }

    public IMongoDatabaseBuilder WithNamespacePrefix(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        _namespacePrefix = prefix;
        return this;
    }

    internal DatabaseDescriptor Build(string name)
    {
        if (string.IsNullOrWhiteSpace(_clusterName))
        {
            throw new InvalidOperationException(
                $"Database '{name}' must call OnCluster(\"<cluster-name>\").");
        }

        return new DatabaseDescriptor(
            new MongoDatabaseKey(name),
            new MongoClusterKey(_clusterName),
            _namespacePrefix);
    }
}
