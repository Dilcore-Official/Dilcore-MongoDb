using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Descriptors;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Internal;

internal sealed class MongoDbCollectionFactory(
    MongoRegistrationGraph graph,
    IMongoDatabaseResolver databaseResolver,
    INamespaceResolver namespaceResolver) : IMongoDbCollectionFactory
{
    private const string DefaultConventions = nameof(DefaultConventions);
    private static readonly object ConventionLock = new();
    private static bool _conventionsRegistered;

    public async Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync<TDocument>(
        MongoDocumentBindingKey bindingKey,
        CancellationToken cancellationToken = default)
        where TDocument : class, IDocumentEntity
    {
        DocumentBindingDescriptor binding;
        try
        {
            binding = graph.GetBinding(bindingKey);
        }
        catch (InvalidOperationException)
        {
            return Result.Fail($"Unknown document binding key '{bindingKey.Name}'.");
        }

        if (binding.DocumentType != typeof(TDocument))
        {
            return Result.Fail(
                $"Binding '{bindingKey.Name}' is registered for '{binding.DocumentType.Name}', not '{typeof(TDocument).Name}'.");
        }

        var databaseResult = await databaseResolver.GetDatabaseAsync(binding.DatabaseKey, cancellationToken);
        if (databaseResult.IsFailed)
        {
            return databaseResult.ToResult();
        }

        var collectionNameResult = await ResolveNameAsync(
            binding.CollectionName,
            NamespaceTarget.Collection,
            binding.NamespacePrefix,
            binding.Key,
            binding.DatabaseKey,
            cancellationToken);

        if (collectionNameResult.IsFailed)
        {
            return collectionNameResult.ToResult();
        }

        var options = new GetCollectionOptions<TDocument>();
        options.WithCollectionName(collectionNameResult.Value);
        if (binding.SoftDeleteEnabled)
        {
            options.WithSoftDelete();
        }

        if (binding.Indices is { Count: > 0 })
        {
            options.WithIndexes(binding.Indices.Cast<CreateIndexModel<TDocument>>().ToArray());
        }

        if (binding is { CollectionItemsTimeToLive: not null, TimeToLeavePropertySelector: not null })
        {
            options.WithCollectionItemsTimeToLive(
                binding.CollectionItemsTimeToLive.Value,
                (Expression<Func<TDocument, object>>)binding.TimeToLeavePropertySelector);
        }

        return await GetTypedCollectionAsync(databaseResult.Value, options, cancellationToken);
    }

    public async Task<Result<IMongoCollection<BsonDocument>>> GetCollectionAsync(
        MongoDatabaseKey databaseKey,
        string logicalCollectionName,
        string? staticPrefix = null,
        CancellationToken cancellationToken = default)
    {
        var databaseResult = await databaseResolver.GetDatabaseAsync(databaseKey, cancellationToken);
        if (databaseResult.IsFailed)
        {
            return databaseResult.ToResult();
        }

        var collectionNameResult = await ResolveNameAsync(
            logicalCollectionName,
            NamespaceTarget.Collection,
            staticPrefix,
            bindingKey: null,
            databaseKey,
            cancellationToken);

        if (collectionNameResult.IsFailed)
        {
            return collectionNameResult.ToResult();
        }

        return Result.Ok(databaseResult.Value.GetCollection<BsonDocument>(collectionNameResult.Value));
    }

    public Task<Result<string>> ResolveCollectionNameAsync(
        MongoDatabaseKey databaseKey,
        string logicalCollectionName,
        string? staticPrefix = null,
        CancellationToken cancellationToken = default)
    {
        return ResolveNameAsync(
            logicalCollectionName,
            NamespaceTarget.Collection,
            staticPrefix,
            bindingKey: null,
            databaseKey,
            cancellationToken);
    }

    private Task<Result<string>> ResolveNameAsync(
        string logicalName,
        NamespaceTarget target,
        string? staticPrefix,
        MongoDocumentBindingKey? bindingKey,
        MongoDatabaseKey? databaseKey,
        CancellationToken cancellationToken)
    {
        return namespaceResolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = logicalName,
            Target = target,
            BindingKey = bindingKey,
            DatabaseKey = databaseKey,
            StaticPrefix = staticPrefix
        }, cancellationToken);
    }

    private static async Task<Result<IMongoCollection<TDocument>>> GetTypedCollectionAsync<TDocument>(
        IMongoDatabase database,
        GetCollectionOptions<TDocument> options,
        CancellationToken cancellationToken)
        where TDocument : IDocumentEntity
    {
        EnsureConventions();

        if (string.IsNullOrWhiteSpace(options.CollectionName))
        {
            return Result.Fail("Collection name is not provided");
        }

        var collection = database.GetCollection<TDocument>(options.CollectionName);

        if (options.CollectionItemsTimeToLive.HasValue && options.TimeToLeavePropertySelector is not null)
        {
            await CreateTimeToLiveIndexAsync(
                collection,
                options.CollectionItemsTimeToLive.Value,
                options.TimeToLeavePropertySelector,
                cancellationToken);
        }

        if (options.Indices is { Count: > 0 })
        {
            await collection.Indexes.CreateManyAsync(options.Indices, cancellationToken);
        }

        return Result.Ok(collection);
    }

    private static void EnsureConventions()
    {
        if (_conventionsRegistered)
        {
            return;
        }

        lock (ConventionLock)
        {
            if (_conventionsRegistered)
            {
                return;
            }

            var pack = new ConventionPack
            {
                new EnumRepresentationConvention(BsonType.String),
                new CamelCaseElementNameConvention(),
                new IgnoreIfNullConvention(true),
                new IgnoreExtraElementsConvention(true)
            };

            ConventionRegistry.Register(DefaultConventions, pack, _ => true);
            _conventionsRegistered = true;
        }
    }

    private static async Task CreateTimeToLiveIndexAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        TimeSpan timeToLeave,
        Expression<Func<TDocument, object>> propertySelector,
        CancellationToken cancellationToken)
        where TDocument : IDocumentEntity
    {
        var indexKeysDefinition = Builders<TDocument>.IndexKeys.Ascending(propertySelector);
        var indexOptions = new CreateIndexOptions { ExpireAfter = timeToLeave };
        var indexModel = new CreateIndexModel<TDocument>(indexKeysDefinition, indexOptions);
        await collection.Indexes.CreateOneAsync(indexModel, cancellationToken: cancellationToken);
    }
}
