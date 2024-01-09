using Dilcore.DocumentDb.Abstractions;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb;

internal class MongoDbCollectionFactory(IServiceProvider keyedServiceProvider) : IMongoDbCollectionFactory
{
    public async Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(string dbName,
        CancellationToken cancellationToken = default) where TDocument : class, IDocumentEntity
    {
        var options = keyedServiceProvider.GetKeyedService<Action<GetCollectionOptions<TDocument>>>(dbName);

        if (options is null)
        {
            return Result.Fail($"Cannot find a collection options for database {dbName}");
        }
        
        return await GetCollectionAsync(dbName, options, cancellationToken);
    }

    public async Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(string dbName, 
        Action<GetCollectionOptions<TDocument>> optionsAction, 
        CancellationToken cancellationToken = default) where TDocument : class, IDocumentEntity
    {
        var provider = keyedServiceProvider.GetKeyedService<IMongoDbCollectionProvider>(dbName);

        if (provider is null)
        {
            return Result.Fail($"Cannot find a collection for database {dbName}");
        }
        
        return await provider.GetCollectionAsync(optionsAction, cancellationToken);
    }
}