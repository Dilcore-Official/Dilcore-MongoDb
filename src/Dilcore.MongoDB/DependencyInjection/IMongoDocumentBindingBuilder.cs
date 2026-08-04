using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.DependencyInjection;

public interface IMongoDocumentBindingBuilder<TDocument>
    where TDocument : class, IDocumentEntity
{
    IMongoDocumentBindingBuilder<TDocument> InDatabase(string databaseName);

    IMongoDocumentBindingBuilder<TDocument> WithCollectionName(string collectionName);

    IMongoDocumentBindingBuilder<TDocument> WithSoftDelete();

    IMongoDocumentBindingBuilder<TDocument> WithBulkRepository();

    IMongoDocumentBindingBuilder<TDocument> WithProjectionRepository();

    IMongoDocumentBindingBuilder<TDocument> WithNamespacePrefix(string prefix);

    IMongoDocumentBindingBuilder<TDocument> WithIndexes(params CreateIndexModel<TDocument>[] indexes);

    IMongoDocumentBindingBuilder<TDocument> WithCollectionItemsTimeToLive(
        TimeSpan timeToLive,
        Expression<Func<TDocument, object>> propertySelector);
}
