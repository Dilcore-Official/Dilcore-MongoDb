using Dilcore.MongoDB.Abstractions.Internal;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Internal;

internal sealed class MongoCallContext
{
    public static MongoCallContext None { get; } = new();

    public IClientSessionHandle? Session { get; init; }

    public ITransactionBudgetGuard? Budget { get; init; }

    public MongoClusterKeyBound? Cluster { get; init; }
}

internal sealed class MongoClusterKeyBound
{
    public required Abstractions.Keys.MongoClusterKey Key { get; init; }
}

internal interface ITransactionBudgetGuard : IMongoOperationBudget
{
    void ResetAttempt();
}

internal static class MongoCollectionCalls
{
    public static Task InsertOneAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        TDocument document,
        InsertOneOptions? options,
        CancellationToken cancellationToken)
        => session is null
            ? collection.InsertOneAsync(document, options, cancellationToken)
            : collection.InsertOneAsync(session, document, options, cancellationToken);

    public static Task<ReplaceOneResult> ReplaceOneAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        TDocument document,
        ReplaceOptions? options,
        CancellationToken cancellationToken)
        => session is null
            ? collection.ReplaceOneAsync(filter, document, options, cancellationToken)
            : collection.ReplaceOneAsync(session, filter, document, options, cancellationToken);

    public static Task<UpdateResult> UpdateOneAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        UpdateOptions? options,
        CancellationToken cancellationToken)
        => session is null
            ? collection.UpdateOneAsync(filter, update, options, cancellationToken)
            : collection.UpdateOneAsync(session, filter, update, options, cancellationToken);

    public static Task<UpdateResult> UpdateManyAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        UpdateOptions? options,
        CancellationToken cancellationToken)
        => session is null
            ? collection.UpdateManyAsync(filter, update, options, cancellationToken)
            : collection.UpdateManyAsync(session, filter, update, options, cancellationToken);

    public static Task<DeleteResult> DeleteOneAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken)
        => session is null
            ? collection.DeleteOneAsync(filter, cancellationToken)
            : collection.DeleteOneAsync(session, filter, new DeleteOptions(), cancellationToken);

    public static Task<DeleteResult> DeleteManyAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        CancellationToken cancellationToken)
        => session is null
            ? collection.DeleteManyAsync(filter, cancellationToken)
            : collection.DeleteManyAsync(session, filter, new DeleteOptions(), cancellationToken);

    public static Task<BulkWriteResult<TDocument>> BulkWriteAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        IEnumerable<WriteModel<TDocument>> requests,
        BulkWriteOptions? options,
        CancellationToken cancellationToken)
        => session is null
            ? collection.BulkWriteAsync(requests, options, cancellationToken)
            : collection.BulkWriteAsync(session, requests, options, cancellationToken);

    public static Task<TDocument> FindOneAndUpdateAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        UpdateDefinition<TDocument> update,
        FindOneAndUpdateOptions<TDocument>? options,
        CancellationToken cancellationToken)
        => session is null
            ? collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken)
            : collection.FindOneAndUpdateAsync(session, filter, update, options, cancellationToken);

    public static IFindFluent<TDocument, TDocument> Find<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter)
        => session is null
            ? collection.Find(filter)
            : collection.Find(session, filter);

    public static Task<long> CountDocumentsAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IClientSessionHandle? session,
        FilterDefinition<TDocument> filter,
        CountOptions? options,
        CancellationToken cancellationToken)
        => session is null
            ? collection.CountDocumentsAsync(filter, options, cancellationToken)
            : collection.CountDocumentsAsync(session, filter, options, cancellationToken);
}
