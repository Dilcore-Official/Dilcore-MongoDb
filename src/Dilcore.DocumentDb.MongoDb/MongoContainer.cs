using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Configuration.Client;
using Dilcore.DocumentDb.MongoDb.Defaults;
using Dilcore.DocumentDb.MongoDb.Repositories;
using Dilcore.DocumentDb.MongoDb.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.DocumentDb.MongoDb;

public class MongoContainer
{
    private readonly IServiceCollection _services;

    private MongoContainer(IServiceCollection services)
    {
        _services = services;
    }

    public MongoContainer AddDatabase(string dbName, Action<MongoDatabaseContainer> configureDatabase)
    {
        var mongoDatabaseContainer = MongoDatabaseContainer.Create(_services, dbName);
        
        configureDatabase(mongoDatabaseContainer);
        
        return this;
    }
    
    private MongoContainer AddMongoDbClient()
    {
        _services.AddSingleton<MongoClientProvider>();
        return this;
    }
    
    internal static MongoContainer Create(IServiceCollection services, Action<MongoDbConfigBuilder> configure)
    {
        services.AddSingleton(configure);

        return new MongoContainer(services)
            .AddMongoDbClient();
    }
}

public class MongoDatabaseContainer
{
    private readonly IServiceCollection _services;
    private readonly string _dbName;

    private MongoDatabaseContainer(IServiceCollection services, string dbName)
    {
        _services = services;
        _dbName = dbName;
    }

    public MongoDatabaseContainer AddCustomDatabasePrefixResolver<T>() 
        where T : class, IDocumentDatabasePrefixProvider
    {
        _services.AddKeyedSingleton<IDocumentDatabasePrefixProvider, T>(_dbName);
        return this;
    }
    
    public MongoDatabaseContainer AddCustomCollectionPrefixResolver<T>() 
        where T : class, IDocumentCollectionPrefixProvider
    {
        _services.AddKeyedSingleton<IDocumentCollectionPrefixProvider, T>(_dbName);
        return this;
    }
    
    public MongoDatabaseContainer AddGenericRepository<T>(Action<GetCollectionOptions<T>> options)
        where T : class, IDocumentEntity
    {
        _services.AddKeyedSingleton(options, _dbName);
        _services.AddSingleton<IGenericRepository<T>, GenericMongoDbRepository<T>>((provider) =>
        {
            var mongoDbCollectionProvider = provider.GetRequiredKeyedService<IMongoDbCollectionProvider>(_dbName);
            return new GenericMongoDbRepository<T>(options, mongoDbCollectionProvider);
        });
        
        return this;
    }
    
    private MongoDatabaseContainer AddDefaultPrefixProviders()
    {
        _services.AddKeyedSingleton<IDocumentDatabasePrefixProvider, DefaultDocumentDatabasePrefixProvider>(_dbName);
        _services.AddKeyedSingleton<IDocumentCollectionPrefixProvider, DefaultDocumentCollectionPrefixProvider>(_dbName);
        
        return this;
    }
    
    private MongoDatabaseContainer AddDatabaseProvider()
    {
        _services.AddKeyedSingleton<IMongoDatabaseProvider>(_dbName, (provider, _) =>
        {
            var prefixProvider = provider.GetRequiredKeyedService<IDocumentDatabasePrefixProvider>(_dbName);
            
            var mongoClientProvider = provider.GetRequiredService<MongoClientProvider>();

            return new MongoDatabaseProvider(prefixProvider, mongoClientProvider);
        });
        return this;
    }

    private MongoDatabaseContainer AddCollectionProvider()
    {
        _services.AddKeyedSingleton<IMongoDbCollectionProvider>(_dbName, (provider, _) =>
        {
            var mongoDatabaseProvider = provider.GetRequiredKeyedService<IMongoDatabaseProvider>(_dbName);
            var collectionPrefixProvider = provider.GetRequiredKeyedService<IDocumentCollectionPrefixProvider>(_dbName);

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