using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Descriptors;
using Dilcore.MongoDB.Internal;
using System.Linq.Expressions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories;

internal sealed class TransactionalRepositoryResolver(
    IMongoDbCollectionFactory collectionFactory,
    MongoRegistrationGraph graph,
    MongoCallContext callContext) : IRepositoryResolver
{
    public IGenericRepository<TDocument> GetRepository<TDocument>()
        where TDocument : class, IDocumentEntity
        => CreateRepository<TDocument>(ResolveSingleBinding<TDocument>());

    public IGenericRepository<TDocument> GetRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity
        => CreateRepository<TDocument>(graph.GetBinding(new MongoDocumentBindingKey(bindingKey)));

    public IGenericBulkRepository<TDocument> GetBulkRepository<TDocument>()
        where TDocument : class, IDocumentEntity
        => CreateBulk<TDocument>(ResolveSingleBinding<TDocument>());

    public IGenericBulkRepository<TDocument> GetBulkRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity
        => CreateBulk<TDocument>(graph.GetBinding(new MongoDocumentBindingKey(bindingKey)));

    public IGenericProjectionRepository<TDocument> GetProjectionRepository<TDocument>()
        where TDocument : class, IDocumentEntity
        => CreateProjection<TDocument>(ResolveSingleBinding<TDocument>());

    public IGenericProjectionRepository<TDocument> GetProjectionRepository<TDocument>(string bindingKey)
        where TDocument : class, IDocumentEntity
        => CreateProjection<TDocument>(graph.GetBinding(new MongoDocumentBindingKey(bindingKey)));

    private DocumentBindingDescriptor ResolveSingleBinding<TDocument>()
    {
        var matches = graph.Bindings.Where(binding => binding.DocumentType == typeof(TDocument)).ToList();
        if (matches.Count != 1)
        {
            throw new InvalidOperationException(
                $"Unkeyed repository for '{typeof(TDocument).Name}' requires exactly one binding.");
        }

        return matches[0];
    }

    private MongoCallContext BindContext(DocumentBindingDescriptor binding)
    {
        var cluster = graph.GetDatabase(binding.DatabaseKey).ClusterKey;
        if (callContext.Cluster is not null && !callContext.Cluster.Key.Equals(cluster))
        {
            throw new CrossClusterRejectedException(cluster.Name);
        }

        return callContext;
    }

    private IGenericRepository<TDocument> CreateRepository<TDocument>(DocumentBindingDescriptor binding)
        where TDocument : class, IDocumentEntity
        => new GenericMongoDbRepository<TDocument>(
            CreateOptions<TDocument>(binding),
            ct => collectionFactory.GetCollectionAsync<TDocument>(binding.Key, ct),
            BindContext(binding));

    private IGenericBulkRepository<TDocument> CreateBulk<TDocument>(DocumentBindingDescriptor binding)
        where TDocument : class, IDocumentEntity
        => new GenericMongoDbBulkRepository<TDocument>(
            CreateOptions<TDocument>(binding),
            ct => collectionFactory.GetCollectionAsync<TDocument>(binding.Key, ct),
            BindContext(binding));

    private IGenericProjectionRepository<TDocument> CreateProjection<TDocument>(DocumentBindingDescriptor binding)
        where TDocument : class, IDocumentEntity
        => new GenericMongoDbProjectionRepository<TDocument>(
            CreateOptions<TDocument>(binding),
            ct => collectionFactory.GetCollectionAsync<TDocument>(binding.Key, ct),
            BindContext(binding));

    private static Action<GetCollectionOptions<TDocument>> CreateOptions<TDocument>(DocumentBindingDescriptor binding)
        where TDocument : class, IDocumentEntity
        => options =>
        {
            options.WithCollectionName(binding.CollectionName);
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
        };
}

internal sealed class CrossClusterRejectedException(string clusterName)
    : InvalidOperationException($"Binding targets cluster '{clusterName}', which is outside the current transaction.");
