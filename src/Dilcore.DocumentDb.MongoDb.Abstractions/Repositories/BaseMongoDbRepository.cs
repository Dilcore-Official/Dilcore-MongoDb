using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.Abstractions.Repositories;

public abstract class BaseMongoDbRepository<TDocument> where TDocument : class, IDocumentEntity
{
    protected static readonly FilterDefinition<TDocument> NotDeletedFilter = Builders<TDocument>.Filter.Eq(x => x.IsDeleted, false);
    private readonly Action<GetCollectionOptions<TDocument>> _options;
    private readonly IMongoDbCollectionProvider _collectionProvider;

    protected BaseMongoDbRepository(Action<GetCollectionOptions<TDocument>> options,
        IMongoDbCollectionProvider collectionProvider)
    {
        _options = options;
        _collectionProvider = collectionProvider;
    }

    protected Task<Result<TDocument>> ExecuteAsync(Func<IMongoCollection<TDocument>, CancellationToken, Task<Result<TDocument>>> func, CancellationToken cancellationToken = default)
        => ExecuteAsync(func, result => Result.Fail<TDocument>(result.Errors), cancellationToken);
    
    protected Task<Result<IReadOnlyList<TDocument>>> ExecuteAsync(Func<IMongoCollection<TDocument>, CancellationToken, Task<Result<IReadOnlyList<TDocument>>>> func, CancellationToken cancellationToken = default) 
        => ExecuteAsync(func, result => Result.Fail<IReadOnlyList<TDocument>>(result.Errors), cancellationToken);

    protected Task<Result> ExecuteAsync(Func<IMongoCollection<TDocument>, CancellationToken, Task<Result>> func, CancellationToken cancellationToken = default)
        => ExecuteAsync(func, result => Result.Fail(result.Errors), cancellationToken);
    
    protected Task<Result<TResult>> ExecuteAsync<TResult>(Func<IMongoCollection<TDocument>, CancellationToken, Task<Result<TResult>>> func, CancellationToken cancellationToken = default)
        => ExecuteAsync(func, result => Result.Fail<TResult>(result.Errors), cancellationToken);
    
    protected async Task<TResult> ExecuteAsync<TResult>(Func<IMongoCollection<TDocument>, CancellationToken,  Task<TResult>> func, 
        Func<Result<IMongoCollection<TDocument>>, TResult> errorHandler, CancellationToken cancellationToken = default)
    {
        var collectionResult = await GetCollectionAsync(cancellationToken);
    
        if (collectionResult.IsFailed)
        {
            return errorHandler(collectionResult);
        }
    
        return await func(collectionResult.Value, cancellationToken);
    }

    protected GetCollectionOptions<TDocument> GetOptions()
    {
        var options = new GetCollectionOptions<TDocument>();
        _options(options);
        return options;
    }
    
    private Task<Result<IMongoCollection<TDocument>>> GetCollectionAsync(CancellationToken cancellationToken = default)
    {
        return _collectionProvider.GetCollectionAsync(_options, cancellationToken);
    }
}