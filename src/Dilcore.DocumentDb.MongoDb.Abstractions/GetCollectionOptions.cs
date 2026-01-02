using System.Linq.Expressions;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.Abstractions;

public class GetCollectionOptions<TDocument>
    where TDocument : IDocumentEntity
{
    public string? DatabaseName { get; private set; }
    public string? CollectionName { get; private set; }
    public IReadOnlyList<CreateIndexModel<TDocument>>? Indices { get; private set; }
    public TimeSpan? CollectionItemsTimeToLive { get; private set; }
    public Expression<Func<TDocument, object>>? TimeToLeavePropertySelector { get; private set; }

    public bool CreateEmptyCollection { get; private set; }
    public bool SoftDeleteEnabled { get; private set; }
    public bool SoftDeleteDisabled => !SoftDeleteEnabled;

    public GetCollectionOptions<TDocument> WithDatabaseName(string dbName)
    {
        DatabaseName = dbName;
        return this;
    }

    public GetCollectionOptions<TDocument> WithCollectionName(string collectionName)
    {
        CollectionName = collectionName;
        return this;
    }
    public GetCollectionOptions<TDocument> WithIndexes(IReadOnlyList<CreateIndexModel<TDocument>> indexes)
    {
        Indices = indexes;
        return this;
    }

    public GetCollectionOptions<TDocument> WithIndexes(params CreateIndexModel<TDocument>[] indexes)
    {
        Indices = indexes.ToList();
        return this;
    }

    public GetCollectionOptions<TDocument> WithCollectionItemsTimeToLive(TimeSpan timeToLive, Expression<Func<TDocument, object>> propertySelector)
    {
        CollectionItemsTimeToLive = timeToLive;
        TimeToLeavePropertySelector = propertySelector;
        return this;
    }

    public GetCollectionOptions<TDocument> WithEmptyCollection()
    {
        CreateEmptyCollection = true;
        return this;
    }

    public GetCollectionOptions<TDocument> WithSoftDelete()
    {
        SoftDeleteEnabled = true;
        return this;
    }
}