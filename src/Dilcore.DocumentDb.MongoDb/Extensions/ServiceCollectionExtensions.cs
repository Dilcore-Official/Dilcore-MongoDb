using Dilcore.DocumentDb.MongoDb.Configuration.Client;
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

    public static IServiceCollection AddMongoDb(this IServiceCollection services, Action<MongoDbConfigBuilder> configure, Action<MongoContainer> action)
    {
        var mongoContainer = MongoContainer.Create(services, configure);
        action(mongoContainer);
        
        return services;
    }
    
    private static MongoDbContainer ConfigureMongoDb(this IServiceCollection services, Action<MongoDbConfigBuilder> configure)
    {
        services.AddSingleton(configure);
        
        var mongoDbContainer = MongoDbContainer.Create(services);
        
        return mongoDbContainer;
    }
}