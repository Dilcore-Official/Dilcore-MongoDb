using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Defaults;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.DocumentDb.MongoDb;

public class MongoDatabaseContainer
{
    public readonly IServiceCollection Services;
    public readonly string DbName;

    private MongoDatabaseContainer(IServiceCollection services, string dbName)
    {
        Services = services;
        DbName = dbName;
    }

    public MongoDatabaseContainer AddCustomDatabasePrefixResolver<T>() 
        where T : class, IDocumentDatabasePrefixProvider
    {
        Services.AddKeyedSingleton<IDocumentDatabasePrefixProvider, T>(DbName);
        return this;
    }
    
    public MongoDatabaseContainer AddCustomCollectionPrefixResolver<T>() 
        where T : class, IDocumentCollectionPrefixProvider
    {
        Services.AddKeyedSingleton<IDocumentCollectionPrefixProvider, T>(DbName);
        return this;
    }

    public MongoDatabaseContainer AddMongoCollection<TDocument>(Action<GetCollectionOptions<TDocument>> action)
        where TDocument : class, IDocumentEntity
    {
        Services.AddKeyedSingleton(DbName, (_, _) => action);
        return this;
    }
    
    public MongoDatabaseContainer AddBsonDocumentCollectionFactory()
    {
        Services.AddBsonDocumentCollectionFactory();
        return this;
    }

    public MongoDatabaseContainer AddBsonDocumentRepository<TInterface, TImplementation>()
        where TInterface : class, IBsonDocumentRepository
        where TImplementation : BsonDocumentRepository, TInterface
    {
        Services.AddBsonDocumentRepository<TInterface, TImplementation>(DbName);
        return this;
    }
    
    private MongoDatabaseContainer AddDefaultPrefixProviders()
    {
        Services.AddKeyedSingleton<IDocumentDatabasePrefixProvider, DefaultDocumentDatabasePrefixProvider>(DbName);
        Services.AddKeyedSingleton<IDocumentCollectionPrefixProvider, DefaultDocumentCollectionPrefixProvider>(DbName);
        
        return this;
    }
    
    private MongoDatabaseContainer AddDatabaseProvider()
    {
        Services.AddKeyedSingleton<IMongoDatabaseProvider>(DbName, (provider, _) =>
        {
            var prefixProvider = provider.GetRequiredKeyedService<IDocumentDatabasePrefixProvider>(DbName);
            
            var mongoClientProvider = provider.GetRequiredService<MongoClientProvider>();

            return new MongoDatabaseProvider(prefixProvider, mongoClientProvider);
        });
        return this;
    }

    private MongoDatabaseContainer AddCollectionProvider()
    {
        Services.AddKeyedSingleton<IMongoDbCollectionProvider>(DbName, (provider, _) =>
        {
            var mongoDatabaseProvider = provider.GetRequiredKeyedService<IMongoDatabaseProvider>(DbName);

            var collectionPrefixProvider = provider.GetRequiredKeyedService<IDocumentCollectionPrefixProvider>(DbName);

            return new MongoCollectionProvider(mongoDatabaseProvider, collectionPrefixProvider);
        });
        
        return this;
    }
    
    internal static MongoDatabaseContainer Create(IServiceCollection services, string dbName)
    {
        return new MongoDatabaseContainer(services, dbName)
            .AddDefaultPrefixProviders()
            .AddDatabaseProvider()
            .AddCollectionProvider();
    }
}