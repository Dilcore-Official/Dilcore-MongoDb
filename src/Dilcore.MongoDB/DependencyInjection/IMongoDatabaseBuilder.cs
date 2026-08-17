using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Namespace;

namespace Dilcore.MongoDB.DependencyInjection;

public interface IMongoDatabaseBuilder
{
    IMongoDatabaseBuilder OnCluster(string clusterName);

    IMongoDatabaseBuilder WithNamespacePrefix(string prefix);

    IMongoDatabaseBuilder WithNamespacePrefixResolver<TResolver>()
        where TResolver : class, INamespacePrefixResolver;

    IMongoDatabaseBuilder AddDocumentBinding<TDocument>(
        string name,
        Action<IMongoDocumentBindingBuilder<TDocument>> configure)
        where TDocument : class, IDocumentEntity;
}
