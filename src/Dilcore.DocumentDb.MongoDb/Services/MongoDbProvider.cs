using System.Security.Authentication;
using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Configuration;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Services;

internal class MongoDbProvider
{
    private readonly IMongoClient _mongoClient;
    private readonly MongoDbConfig _dbConfig;
    private readonly IDocumentDatabasePrefixProvider _prefixProvider;
        
    public MongoDbProvider(Action<MongoDbConfigBuilder> dbConfigAction, IDocumentDatabasePrefixProvider prefixProvider)
    {
        _prefixProvider = prefixProvider;
        var builder = MongoDbConfigBuilder.Create();
        dbConfigAction(builder);
        
        _dbConfig = builder.Build();
        _mongoClient = CreateMongoDbClient(_dbConfig);
    }

    internal async Task<Result<IMongoDatabase>> GetDatabaseAsync(CancellationToken cancellationToken = default)
    {
        var prefixResult = await _prefixProvider.ResolveAsync(cancellationToken);

        if (prefixResult.IsFailed)
        {
            return prefixResult.ToResult<IMongoDatabase>();
        }
        
        var databaseName = string.IsNullOrWhiteSpace(prefixResult.Value) 
            ? _dbConfig.DatabaseName 
            : $"{prefixResult.Value}_{_dbConfig.DatabaseName}";
        
        var database = _mongoClient.GetDatabase(databaseName);
        
        return Result.Ok(database);
    }
    
    private static IMongoClient CreateMongoDbClient(MongoDbConfig dbConfig)
    {
        var settings = MongoClientSettings.FromUrl(new MongoUrl(dbConfig.ConnectionString));

        settings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12
        };
        
        settings.MaxConnectionPoolSize = dbConfig.MaxConnectionPoolSize ?? Constants.MaxConnectionPoolSize;
        settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(Constants.MaxConnectionIdleTimeInMinutes);
        
        return new MongoClient(settings);
    }
}