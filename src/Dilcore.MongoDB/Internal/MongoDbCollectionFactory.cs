using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Descriptors;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Internal;

internal sealed class MongoDbCollectionFactory(
    MongoRegistrationGraph graph,
    IMongoDatabaseResolver databaseResolver,
    INamespaceResolver namespaceResolver) : IMongoDbCollectionFactory
{
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

        options.WithGuidIdGeneration(binding.GuidIdGenerationStrategy);

        if (binding.Indices is { Count: > 0 })
        {
            options.WithIndexes(binding.Indices.Cast<CreateIndexModel<TDocument>>().ToArray());
        }

        if (binding is { CollectionItemsTimeToLive: { } ttl, TimeToLeavePropertySelector: not null })
        {
            options.WithCollectionItemsTimeToLive(
                ttl,
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

    private static Task<Result<IMongoCollection<TDocument>>> GetTypedCollectionAsync<TDocument>(
        IMongoDatabase database,
        GetCollectionOptions<TDocument> options,
        CancellationToken cancellationToken)
        where TDocument : IDocumentEntity
    {
        _ = cancellationToken;
        if (string.IsNullOrWhiteSpace(options.CollectionName))
        {
            return Task.FromResult(Result.Fail<IMongoCollection<TDocument>>("Collection name is not provided"));
        }

        var collection = database.GetCollection<TDocument>(options.CollectionName);
        return Task.FromResult(Result.Ok(collection));
    }
}
