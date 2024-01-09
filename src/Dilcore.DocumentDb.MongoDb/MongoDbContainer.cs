using Dilcore.DocumentDb.MongoDb.Configuration.Client;
using Dilcore.DocumentDb.MongoDb.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.DocumentDb.MongoDb;

public class MongoDbContainer
{
    private readonly IServiceCollection _services;

    private MongoDbContainer(IServiceCollection services)
    {
        _services = services;
    }

    public MongoDbContainer AddDatabase(string dbName, Action<MongoDatabaseContainer> configureDatabase)
    {
        var mongoDatabaseContainer = MongoDatabaseContainer.Create(_services, dbName);
        
        configureDatabase(mongoDatabaseContainer);
        
        return this;
    }
    
    private MongoDbContainer AddMongoDbClient()
    {
        _services.AddSingleton<MongoClientProvider>();
        return this;
    }
    
    internal static MongoDbContainer Create(IServiceCollection services, Action<MongoDbConfigBuilder> configure)
    {
        services.AddSingleton(configure);

        return new MongoDbContainer(services)
            .AddMongoDbClient();
    }
}