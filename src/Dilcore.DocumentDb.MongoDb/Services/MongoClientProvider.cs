using System.Security.Authentication;
using Dilcore.DocumentDb.MongoDb.Configuration.Client;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Services;

internal class MongoClientProvider
{
    private static readonly object _lockObject = new object();
    private static bool _isConfigured = false;

    static MongoClientProvider()
    {
        ConfigureBsonSerialization();
    }

    private static void ConfigureBsonSerialization()
    {
        if (_isConfigured) return;

        lock (_lockObject)
        {
            if (_isConfigured) return;

            // Configure GUID serialization for MongoDB.Driver 3.0.0 compatibility
            // In 3.0.0, the default GuidRepresentation changed from CSharpLegacy to Unspecified
            // We need to explicitly register a GuidSerializer to avoid BsonSerializationException
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            
            _isConfigured = true;
        }
    }
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