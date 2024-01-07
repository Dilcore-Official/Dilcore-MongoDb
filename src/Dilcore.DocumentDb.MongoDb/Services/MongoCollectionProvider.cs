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
        
        var collectionPrefixResult = await collectionPrefixProvider.ResolveAsync(cancellationToken);
        
        if (collectionPrefixResult.IsFailed)
        {
            return collectionPrefixResult.ToResult<IMongoCollection<TDocument>>();
        }

        var database = databaseResult.ValueOrDefault;
        
        var collectionPrefix = collectionPrefixResult.ValueOrDefault;
        
        var collectionName = string.IsNullOrWhiteSpace(collectionPrefix) 
            ? options.CollectionName 
            : $"{collectionPrefix}_{options.CollectionName}";
        
        options.WithCollectionName(collectionName);
        
        return await GetCollectionAsync(database, options, cancellationToken);
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
            await CreateTimeToLiveIndexAsync(collection, options.CollectionItemsTimeToLive.Value, cancellationToken);
        }

        if (options.Indices != null && options.Indices.Any())
        {
            await collection.Indexes.CreateManyAsync(options.Indices, cancellationToken);
        }
                
        return Result.Ok(collection);
    }

    private static async Task CreateTimeToLiveIndexAsync<TDocument>(IMongoCollection<TDocument> collection,
        TimeSpan timeToLeave, CancellationToken cancellationToken) 
        where TDocument : IDocumentEntity
    {
        var indexKeysDefinition = Builders<TDocument>.IndexKeys.Ascending(x => x.ExpireAt);
        
        var indexOptions = new CreateIndexOptions { ExpireAfter = timeToLeave };
        var indexModel = new CreateIndexModel<TDocument>(indexKeysDefinition, indexOptions);
        await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }
}