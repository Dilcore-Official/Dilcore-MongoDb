using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Defaults;
using Dilcore.DocumentDb.MongoDb.Repositories;
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
    
    public MongoDbContainer AddCustomDatabasePrefixProvider<T>(bool replace = false) 
        where T : class, IDocumentDatabasePrefixProvider
    {
        if (replace)
        {
            var descriptor = _services.SingleOrDefault(x => x.ServiceType == typeof(IDocumentDatabasePrefixProvider));
            _services.Remove(descriptor);
        }
        
        _services.AddSingleton<IDocumentDatabasePrefixProvider, T>();
        
        return this;
    }
    
    public MongoDbContainer AddCustomCollectionPrefixProvider<T>() 
        where T : class, IDocumentCollectionPrefixProvider
    {
        var descriptor = _services.SingleOrDefault(x => x.ServiceType == typeof(IDocumentCollectionPrefixProvider));
        _services.Remove(descriptor);
        
        _services.AddSingleton<IDocumentCollectionPrefixProvider, T>();
        
        return this;
    }
    
    public MongoDbContainer AddGenericRepository<T>(Action<GetCollectionOptions<T>> options)
        where T : class, IDocumentEntity
    {
        _services.AddSingleton(options);
        _services.AddSingleton<IGenericRepository<T>, GenericMongoDbRepository<T>>();
        return this;
    }
    
    private MongoDbContainer AddDefaultPrefixProviders()
    {
        _services.AddSingleton<IDocumentDatabasePrefixProvider, DefaultDocumentDatabasePrefixProvider>();
        _services.AddSingleton<IDocumentCollectionPrefixProvider, DefaultDocumentCollectionPrefixProvider>();
        
        return this;
    }

    private MongoDbContainer AddMongoDbCollectionProvider()
    {
        _services.AddSingleton<IMongoDbCollectionProvider, MongoCollectionProvider>();
        return this;
    }

    private MongoDbContainer AddDefaultMongoDbProvider()
    {
        _services.AddSingleton<IMongoDatabaseProvider, MongoDatabaseProvider>();
        return this;
    }
    
    internal static MongoDbContainer Create(IServiceCollection services)
    {
        var container = new MongoDbContainer(services);
        return container.AddDefaultPrefixProviders()
            .AddMongoDbCollectionProvider()
            .AddDefaultMongoDbProvider();
    }
}