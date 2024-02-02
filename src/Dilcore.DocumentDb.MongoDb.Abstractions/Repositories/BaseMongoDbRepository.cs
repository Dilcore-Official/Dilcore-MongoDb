using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.Abstractions.Repositories;

public abstract class BaseMongoDbRepository<TDocument> where TDocument : class, IDocumentEntity
{
    private static readonly FilterDefinition<TDocument> NotDeletedFilter =
        Builders<TDocument>.Filter.Eq(x => x.IsDeleted, false);

    private readonly Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> _collectionProvider;
    private readonly GetCollectionOptions<TDocument> _options;

    protected BaseMongoDbRepository(Action<GetCollectionOptions<TDocument>> optionsAction,
        Func<CancellationToken, Task<Result<IMongoCollection<TDocument>>>> collectionProvider)
    {
        _collectionProvider = collectionProvider;

        var options = new GetCollectionOptions<TDocument>();
        optionsAction(options);
        _options = options;
    }

    protected Task<Result<TDocument>> ExecuteAsync(
        Func<IMongoCollection<TDocument>, CancellationToken, Task<Result<TDocument>>> func,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(func, result => Result.Fail<TDocument>(result.Errors), cancellationToken);

    protected Task<Result<IReadOnlyList<TDocument>>> ExecuteAsync(
        Func<IMongoCollection<TDocument>, CancellationToken, Task<Result<IReadOnlyList<TDocument>>>> func,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(func, result => Result.Fail<IReadOnlyList<TDocument>>(result.Errors), cancellationToken);

    protected Task<Result> ExecuteAsync(Func<IMongoCollection<TDocument>, CancellationToken, Task<Result>> func,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(func, result => Result.Fail(result.Errors), cancellationToken);

    protected Task<Result<TResult>> ExecuteAsync<TResult>(
        Func<IMongoCollection<TDocument>, CancellationToken, Task<Result<TResult>>> func,
        CancellationToken cancellationToken = default)
        => ExecuteAsync(func, result => Result.Fail<TResult>(result.Errors), cancellationToken);

    private async Task<TResult> ExecuteAsync<TResult>(
        Func<IMongoCollection<TDocument>, CancellationToken, Task<TResult>> func,
        Func<Result<IMongoCollection<TDocument>>, TResult> errorHandler, CancellationToken cancellationToken = default)
    {
        var collectionResult = await GetCollectionAsync(cancellationToken);

        if (collectionResult.IsFailed)
        {
            return errorHandler(collectionResult);
        }

        return await func(collectionResult.Value, cancellationToken);
    }

    protected GetCollectionOptions<TDocument> GetOptions() => _options;

    protected FilterDefinition<TDerived> ApplyNotDeleteFilter<TDerived>(FilterDefinition<TDerived> filter)
        where TDerived : class, TDocument
    {
        return _options.SoftDeleteDisabled 
            ? filter 
            : filter & Builders<TDerived>.Filter.Eq(x => x.IsDeleted, false);
    }

    protected FilterDefinition<TDocument> ApplyNotDeleteFilter(FilterDefinition<TDocument> filter)
    {
        return _options.SoftDeleteDisabled 
            ? filter 
            : filter & NotDeletedFilter;
    }

    protected FilterDefinition<TDocument> ApplyNotDeleteFilter()
    {
        return _options.SoftDeleteDisabled 
            ? Builders<TDocument>.Filter.Empty 
            : NotDeletedFilter;
    }

    private Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync(CancellationToken cancellationToken = default)
    {
        return _collectionProvider(cancellationToken);
    }
}