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

    internal static IServiceCollection AddBsonDocumentRepository<TInterface, TImplementation>(this IServiceCollection services, string dbName)
        where TInterface : class, IBsonDocumentRepository
        where TImplementation : BsonDocumentRepository, TInterface
    {
        services.AddScoped<TInterface, TImplementation>(sp =>
        {
            var parameters = new object[] { dbName };
            var instance = ActivatorUtilities.CreateInstance<TImplementation>(sp, parameters);
            return instance;
        });

        return services;
    }

    internal static IServiceCollection AddBsonDocumentCollectionFactory(this IServiceCollection services)
    {
        return services.AddScoped<IBsonDocumentCollectionFactory, BsonDocumentCollectionFactory>();
    }
}