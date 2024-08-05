using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Configuration.Client;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.DocumentDb.MongoDb.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDb(this IServiceCollection services, Action<MongoDbConfigBuilder> configure, Action<MongoDbContainer> action)
    {
        var mongoContainer = MongoDbContainer.Create(services, configure);
        action(mongoContainer);

        services.AddScoped<IMongoDbCollectionFactory, MongoDbCollectionFactory>();
        
        return services;
    }
}