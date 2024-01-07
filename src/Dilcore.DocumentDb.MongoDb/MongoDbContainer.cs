using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Defaults;
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
    
    public MongoDbContainer AddCustomDatabasePrefixProvider<T>() 
        where T : class, IDocumentDatabasePrefixProvider
    {
        var descriptor = _services.SingleOrDefault(x => x.ServiceType == typeof(IDocumentDatabasePrefixProvider));
        _services.Remove(descriptor);
        
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

    private MongoDbContainer AddDefaultPrefixProviders()
    {
        _services.AddSingleton<IDocumentDatabasePrefixProvider, DefaultDocumentDatabasePrefixProvider>();
        _services.AddSingleton<IDocumentCollectionPrefixProvider, DefaultDocumentCollectionPrefixProvider>();
        
        return this;
    }

    private MongoDbContainer AddMongoDbCollectionProvider()
    {
        _services.AddSingleton<IMongoDbCollectionProvider, MongoDbCollectionProvider>();
        return this;
    }
    
    internal static MongoDbContainer Create(IServiceCollection services)
    {
        var container = new MongoDbContainer(services);
        return container.AddDefaultPrefixProviders()
            .AddMongoDbCollectionProvider();
    }
}