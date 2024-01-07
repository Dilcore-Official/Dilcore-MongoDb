using Dilcore.DocumentDb.Abstractions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Services;

internal class MongoDatabaseProvider : IMongoDatabaseProvider
{
    private readonly IDocumentDatabasePrefixProvider _prefixProvider;
    private readonly MongoClientProvider _mongoClientProvider;

    internal MongoDatabaseProvider(IDocumentDatabasePrefixProvider prefixProvider, MongoClientProvider mongoClientProvider)
    {
        _prefixProvider = prefixProvider;
        _mongoClientProvider = mongoClientProvider;
    }

    public async Task<Result<IMongoDatabase>> GetDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
    {
        var prefixResult = await _prefixProvider.ResolveAsync(cancellationToken);

        if (prefixResult.IsFailed)
        {
            return prefixResult.ToResult<IMongoDatabase>();
        }
        
        databaseName = string.IsNullOrWhiteSpace(prefixResult.Value) 
            ? databaseName 
            : $"{prefixResult.Value}_{databaseName}";
        
        var database = _mongoClientProvider.GetMongoClient().GetDatabase(databaseName);
        
        return Result.Ok(database);
    }
}