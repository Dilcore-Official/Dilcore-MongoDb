using System.Security.Authentication;
using Dilcore.DocumentDb.MongoDb.Configuration.Client;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Services;

internal class MongoClientProvider
{
    private readonly IMongoClient _mongoClient;
    
    public MongoClientProvider(Action<MongoDbConfigBuilder> dbConfigAction)
    {
        var builder = MongoDbConfigBuilder.Create();
        dbConfigAction(builder);
        
         var dbConfig = builder.Build();
        _mongoClient = CreateMongoDbClient(dbConfig);
    }
    
    internal IMongoClient GetMongoClient() => _mongoClient;
    
    private static IMongoClient CreateMongoDbClient(MongoDbClientConfig dbClientConfig)
    {
        var settings = MongoClientSettings.FromUrl(new MongoUrl(dbClientConfig.ConnectionString));

        settings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12
        };
        
        settings.MaxConnectionPoolSize = dbClientConfig.MaxConnectionPoolSize ?? Constants.MaxConnectionPoolSize;
        settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(Constants.MaxConnectionIdleTimeInMinutes);
        
        return new MongoClient(settings);
    }
}