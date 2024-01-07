using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Configuration;
using Dilcore.DocumentDb.MongoDb.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.DocumentDb.MongoDb.Extensions;

public static class ServiceCollectionExtensions
{
    public static MongoDbContainer ConfigureMongoDb(this IServiceCollection services, Action<MongoDbConfigBuilder> configure, Action<MongoDbContainer> configureContainer)
    {
        var mongoDbContainer = services.ConfigureMongoDb(configure);
        
        configureContainer(mongoDbContainer);
        return mongoDbContainer;
    }
    
    private static MongoDbContainer ConfigureMongoDb(this IServiceCollection services, Action<MongoDbConfigBuilder> configure)
    {
        var configBuilder = MongoDbConfigBuilder.Create();
        configure(configBuilder);

        services.AddSingleton<MongoDbProvider>();
        services.AddSingleton(configBuilder);

        services.AddSingleton<IMongoDbCollectionProvider, MongoDbCollectionProvider>();
        
        var mongoDbContainer = MongoDbContainer.Create(services);
        
        return mongoDbContainer;
    }
}