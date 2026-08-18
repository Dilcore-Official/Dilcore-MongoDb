using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Descriptors;
using MongoDB.Driver;

namespace Dilcore.MongoDB.DependencyInjection;

internal sealed class MongoDocumentBindingBuilder<TDocument> : IMongoDocumentBindingBuilder<TDocument>
    where TDocument : class, IDocumentEntity
{
    private string? _collectionName;
    private bool _softDeleteEnabled;
    private bool _registerBulk;
    private bool _registerProjection;
    private string? _namespacePrefix;
    private Type? _namespacePrefixResolverType;
    private IReadOnlyList<CreateIndexModel<TDocument>>? _indices;
    private TimeSpan? _ttl;
    private Expression<Func<TDocument, object>>? _ttlSelector;
    private GuidIdGenerationStrategy _guidIdGenerationStrategy = GuidIdGenerationStrategy.Random;
    private bool _guidIdGenerationConfigured;

    public IMongoDocumentBindingBuilder<TDocument> WithCollectionName(string collectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        _collectionName = collectionName;
        return this;
    }

    public IMongoDocumentBindingBuilder<TDocument> WithSoftDelete()
    {
        if (!typeof(ISoftDeletable).IsAssignableFrom(typeof(TDocument)))
        {
            throw new InvalidOperationException(
                $"WithSoftDelete requires '{typeof(TDocument).Name}' to implement {nameof(ISoftDeletable)}.");
        }

        _softDeleteEnabled = true;
        return this;
    }

    public IMongoDocumentBindingBuilder<TDocument> WithGuidIdGeneration(GuidIdGenerationStrategy strategy)
    {
        var identifierType = DocumentIdAccessorCache.ResolveIdentifierType(typeof(TDocument));
        if (identifierType != typeof(Guid))
        {
            throw new InvalidOperationException(
                $"WithGuidIdGeneration requires '{typeof(TDocument).Name}' to implement IDocumentEntity<Guid>, " +
                $"but its identifier type is '{identifierType.Name}'.");
        }

        _guidIdGenerationStrategy = strategy;
        _guidIdGenerationConfigured = true;
        return this;
    }

    public IMongoDocumentBindingBuilder<TDocument> WithBulkRepository()
    {
        _registerBulk = true;
        return this;
    }

    public IMongoDocumentBindingBuilder<TDocument> WithProjectionRepository()
    {
        _registerProjection = true;
        return this;
    }

    public IMongoDocumentBindingBuilder<TDocument> WithNamespacePrefix(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        _namespacePrefix = prefix;
        return this;
    }

    public IMongoDocumentBindingBuilder<TDocument> WithNamespacePrefixResolver<TResolver>()
        where TResolver : class, INamespacePrefixResolver
    {
        if (_namespacePrefixResolverType is not null)
        {
            throw new InvalidOperationException(
                "WithNamespacePrefixResolver can only be called once per document binding.");
        }

        _namespacePrefixResolverType = typeof(TResolver);
        return this;
    }

    public IMongoDocumentBindingBuilder<TDocument> WithIndexes(params CreateIndexModel<TDocument>[] indexes)
    {
        ArgumentNullException.ThrowIfNull(indexes);
        _indices = indexes;
        return this;
    }

    public IMongoDocumentBindingBuilder<TDocument> WithCollectionItemsTimeToLive(
        TimeSpan timeToLive,
        Expression<Func<TDocument, object>> propertySelector)
    {
        ArgumentNullException.ThrowIfNull(propertySelector);
        _ttl = timeToLive;
        _ttlSelector = propertySelector;
        return this;
    }

    internal DocumentBindingDescriptor Build(string name, MongoDatabaseKey databaseKey)
    {
        if (string.IsNullOrWhiteSpace(_collectionName))
        {
            throw new InvalidOperationException(
                $"Document binding '{name}' must call WithCollectionName(\"<collection-name>\").");
        }

        // Ensure the document declares IDocumentEntity<TId> (throws if missing).
        _ = DocumentIdAccessorCache.ResolveIdentifierType(typeof(TDocument));

        return new DocumentBindingDescriptor(
            new MongoDocumentBindingKey(name),
            typeof(TDocument),
            databaseKey,
            _collectionName,
            _softDeleteEnabled,
            _registerBulk,
            _registerProjection,
            _namespacePrefix,
            _indices?.Cast<object>().ToList(),
            _ttl,
            _ttlSelector,
            _namespacePrefixResolverType,
            _guidIdGenerationConfigured ? _guidIdGenerationStrategy : GuidIdGenerationStrategy.Random);
    }
}
