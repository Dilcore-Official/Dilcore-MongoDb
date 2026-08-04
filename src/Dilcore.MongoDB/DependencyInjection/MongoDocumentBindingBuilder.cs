using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Descriptors;
using MongoDB.Driver;

namespace Dilcore.MongoDB.DependencyInjection;

internal sealed class MongoDocumentBindingBuilder<TDocument> : IMongoDocumentBindingBuilder<TDocument>
    where TDocument : class, IDocumentEntity
{
    private string? _databaseName;
    private string? _collectionName;
    private bool _softDeleteEnabled;
    private bool _registerBulk;
    private bool _registerProjection;
    private string? _namespacePrefix;
    private IReadOnlyList<CreateIndexModel<TDocument>>? _indices;
    private TimeSpan? _ttl;
    private Expression<Func<TDocument, object>>? _ttlSelector;

    public IMongoDocumentBindingBuilder<TDocument> InDatabase(string databaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);
        _databaseName = databaseName;
        return this;
    }

    public IMongoDocumentBindingBuilder<TDocument> WithCollectionName(string collectionName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(collectionName);
        _collectionName = collectionName;
        return this;
    }

    public IMongoDocumentBindingBuilder<TDocument> WithSoftDelete()
    {
        _softDeleteEnabled = true;
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

    internal DocumentBindingDescriptor Build(string name)
    {
        if (string.IsNullOrWhiteSpace(_databaseName))
        {
            throw new InvalidOperationException(
                $"Document binding '{name}' must call InDatabase(\"<database-name>\").");
        }

        if (string.IsNullOrWhiteSpace(_collectionName))
        {
            throw new InvalidOperationException(
                $"Document binding '{name}' must call WithCollectionName(\"<collection-name>\").");
        }

        return new DocumentBindingDescriptor(
            new MongoDocumentBindingKey(name),
            typeof(TDocument),
            new MongoDatabaseKey(_databaseName),
            _collectionName,
            _softDeleteEnabled,
            _registerBulk,
            _registerProjection,
            _namespacePrefix,
            _indices?.Cast<object>().ToList(),
            _ttl,
            _ttlSelector);
    }
}
