namespace Dilcore.MongoDB.DependencyInjection;

public interface IMongoDbBuilder
{
    IMongoDbBuilder AddCluster(string name, Action<IMongoClusterBuilder> configure);

    IMongoDbBuilder AddDatabase(string name, Action<IMongoDatabaseBuilder> configure);

    IMongoDbBuilder ConfigureConventions(Action<IConventionsBuilder> configure);
}
