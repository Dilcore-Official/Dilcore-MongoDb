using System.Linq.Expressions;
using Dilcore.DocumentDb.Abstractions;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Services;

internal class MongoCollectionProvider(
    IMongoDatabaseProvider mongoDatabaseProvider,
    IDocumentCollectionPrefixProvider collectionPrefixProvider)
    : IMongoDbCollectionProvider
{
    private const string DefaultConventions = nameof(DefaultConventions);

    public async Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(Action<GetCollectionOptions<TDocument>> collectionOptions, 
        CancellationToken cancellationToken = default) 
        where TDocument : class, IDocumentEntity
    {
        var options = new GetCollectionOptions<TDocument>();
        collectionOptions(options);

        var databaseResult = await mongoDatabaseProvider.GetDatabaseAsync(options.DatabaseName, cancellationToken);

        if (databaseResult.IsFailed)
        {
            return databaseResult.ToResult<IMongoCollection<TDocument>>();
        }
        
        var collectionNameResult = await GetCollectionNameAsync(options.CollectionName, cancellationToken);

        if(collectionNameResult.IsFailed)
        {
            return collectionNameResult.ToResult<IMongoCollection<TDocument>>();
        }
        
        options.WithCollectionName(collectionNameResult.ValueOrDefault);
        
        var database = databaseResult.ValueOrDefault;
        
        return await GetCollectionAsync(database, options, cancellationToken);
    }

    public async Task<Result<IMongoCollection<BsonDocument>>> GetCollectionAsync(string databaseName, string collectionName, CancellationToken cancellationToken = default)
    {
        var databaseResult = await mongoDatabaseProvider.GetDatabaseAsync(databaseName, cancellationToken);
        
        if (databaseResult.IsFailed)
        {
            return databaseResult.ToResult<IMongoCollection<BsonDocument>>();
        }
        
        var collectionNameResult = await GetCollectionNameAsync(collectionName, cancellationToken);
        
        if(collectionNameResult.IsFailed)
        {
            return collectionNameResult.ToResult<IMongoCollection<BsonDocument>>();
        }
        
        var database = databaseResult.ValueOrDefault;
        
        return Result.Ok(database.GetCollection<BsonDocument>(collectionNameResult.ValueOrDefault));
    }

    private async Task<Result<string>> GetCollectionNameAsync(string collectionName, CancellationToken cancellationToken)
    {
        var collectionPrefixResult = await collectionPrefixProvider.ResolveAsync(cancellationToken);
        
        if (collectionPrefixResult.IsFailed)
        {
            return collectionPrefixResult;
        }

        var collectionPrefix = collectionPrefixResult.ValueOrDefault;
        
        collectionName = string.IsNullOrWhiteSpace(collectionPrefix) 
            ? collectionName 
            : $"{collectionPrefix}_{collectionName}";
        
        return collectionName;
    }
    
    private static async Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(IMongoDatabase database,
        GetCollectionOptions<TDocument> options, CancellationToken cancellationToken) 
        where TDocument : IDocumentEntity
    {
        var pack = new ConventionPack
        {
            //this is needed to store enum values as strings rather then integers
            new EnumRepresentationConvention(BsonType.String),
            new CamelCaseElementNameConvention(),
            new IgnoreIfNullConvention(true),
            new IgnoreExtraElementsConvention(true)
        };
        
        ConventionRegistry.Register(DefaultConventions, pack, _ => true);

        var collection = database.GetCollection<TDocument>(options.CollectionName);
        
        if (options.CollectionItemsTimeToLive.HasValue)
        {
            await CreateTimeToLiveIndexAsync(collection, options.CollectionItemsTimeToLive.Value, options.TimeToLeavePropertySelector, cancellationToken);
        }

        if (options.Indices != null && options.Indices.Any())
        {
            await collection.Indexes.CreateManyAsync(options.Indices, cancellationToken);
        }
                
        return Result.Ok(collection);
    }

    private static async Task CreateTimeToLiveIndexAsync<TDocument>(IMongoCollection<TDocument> collection,
        TimeSpan timeToLeave, Expression<Func<TDocument, object>> optionsTimeToLeavePropertySelector, CancellationToken cancellationToken) 
        where TDocument : IDocumentEntity
    {
        var indexKeysDefinition = Builders<TDocument>.IndexKeys.Ascending(optionsTimeToLeavePropertySelector);
        
        var indexOptions = new CreateIndexOptions { ExpireAfter = timeToLeave };
        var indexModel = new CreateIndexModel<TDocument>(indexKeysDefinition, indexOptions);
        await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }
}