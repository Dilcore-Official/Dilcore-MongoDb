using Dilcore.DocumentDb.Abstractions;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb;

internal class MongoDbCollectionFactory(IServiceProvider keyedServiceProvider) : IMongoDbCollectionFactory
{
    public Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(string dbName,
        CancellationToken cancellationToken = default) where TDocument : class, IDocumentEntity
    {
        var options = keyedServiceProvider.GetKeyedService<Action<GetCollectionOptions<TDocument>>>(dbName);

        return options is null
            ? Task.FromResult<Result<IMongoCollection<TDocument>>>(Result.Fail($"Cannot find a collection options for database {dbName}"))
            : GetCollectionAsync(dbName, options, cancellationToken);
    }

    public Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(string dbName,
        Action<GetCollectionOptions<TDocument>> optionsAction,
        CancellationToken cancellationToken = default) where TDocument : class, IDocumentEntity
    {
        var provider = keyedServiceProvider.GetKeyedService<IMongoDbCollectionProvider>(dbName);

        return provider is null
            ? Task.FromResult<Result<IMongoCollection<TDocument>>>(
                Result.Fail($"Cannot find a collection for database {dbName}"))
            : provider.GetCollectionAsync(optionsAction, cancellationToken);
    }

    public async Task<Result<IMongoCollection<BsonDocument>>> GetCollectionAsync(string dbName, string collectionName, CancellationToken cancellationToken = default)
    {
        var provider = keyedServiceProvider.GetKeyedService<IMongoDbCollectionProvider>(dbName);

        return provider is null
            ? Result.Fail<IMongoCollection<BsonDocument>>($"Cannot find a collection for database {dbName}")
            : await provider.GetCollectionAsync(dbName, collectionName, cancellationToken);
    }

    public Task<Result<string>> GetCollectionNameAsync(string dbName, string collectionName, CancellationToken cancellationToken = default)
    {
        var provider = keyedServiceProvider.GetKeyedService<IMongoDbCollectionProvider>(dbName);
        
        return provider is null
            ? Task.FromResult(Result.Fail<string>($"Cannot find a collection for database {dbName}"))
            : provider.GetCollectionNameAsync(collectionName, cancellationToken);
    }
}