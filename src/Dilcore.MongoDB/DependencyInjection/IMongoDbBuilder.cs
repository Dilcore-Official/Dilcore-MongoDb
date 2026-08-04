using Dilcore.MongoDB.Abstractions;

namespace Dilcore.MongoDB.DependencyInjection;

public interface IMongoDbBuilder
{
    IMongoDbBuilder AddCluster(string name, Action<IMongoClusterBuilder> configure);

    IMongoDbBuilder AddDatabase(string name, Action<IMongoDatabaseBuilder> configure);

    IMongoDbBuilder AddDocumentBinding<TDocument>(string name, Action<IMongoDocumentBindingBuilder<TDocument>> configure)
        where TDocument : class, IDocumentEntity;
}
