namespace Dilcore.MongoDB.DependencyInjection;

public interface IMongoDatabaseBuilder
{
    IMongoDatabaseBuilder OnCluster(string clusterName);

    IMongoDatabaseBuilder WithNamespacePrefix(string prefix);
}
