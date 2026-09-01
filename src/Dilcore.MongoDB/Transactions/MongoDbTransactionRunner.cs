using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Abstractions.Transactions;
using Dilcore.MongoDB.Descriptors;
using Dilcore.MongoDB.Internal;
using Dilcore.MongoDB.Repositories;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Transactions;

internal sealed class MongoDbTransactionContext : IMongoDbTransactionContext
{
    public MongoDbTransactionContext(
        MongoClusterKey clusterKey,
        IClientSessionHandle session,
        IRepositoryResolver repositories)
    {
        ClusterKey = clusterKey;
        Session = session;
        Repositories = repositories;
    }

    public MongoClusterKey ClusterKey { get; }

    public IClientSessionHandle Session { get; }

    public IRepositoryResolver Repositories { get; }
}

internal sealed class MongoDbTransactionRunner(
    IServiceProvider serviceProvider,
    MongoRegistrationGraph graph) : IMongoDbTransactionRunner
{
    public async Task<Result> ExecuteAsync(
        MongoClusterKey clusterKey,
        Func<IMongoDbTransactionContext, CancellationToken, Task<Result>> callback,
        MongoTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(
            clusterKey,
            async (context, token) =>
            {
                var inner = await callback(context, token);
                return inner.IsSuccess ? Result.Ok(true) : Result.Fail<bool>(inner.Errors);
            },
            options,
            cancellationToken);
        return result.IsSuccess ? Result.Ok() : Result.Fail(result.Errors);
    }

    public async Task<Result<TResult>> ExecuteAsync<TResult>(
        MongoClusterKey clusterKey,
        Func<IMongoDbTransactionContext, CancellationToken, Task<Result<TResult>>> callback,
        MongoTransactionOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(callback);
        options ??= new MongoTransactionOptions();

        MongoClientHolder holder;
        try
        {
            holder = serviceProvider.GetRequiredKeyedService<MongoClientHolder>(clusterKey.Name);
        }
        catch (InvalidOperationException)
        {
            return Result.Fail<TResult>($"Unknown cluster '{clusterKey.Name}'.");
        }

        using var session = await holder.Client.StartSessionAsync(cancellationToken: cancellationToken);
        var budget = new TransactionBudgetGuard(options);
        var factory = serviceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        try
        {
            return await session.WithTransactionAsync(async (activeSession, token) =>
            {
                budget.ResetAttempt();
                var callContext = new MongoCallContext
                {
                    Session = activeSession,
                    Budget = budget,
                    Cluster = new MongoClusterKeyBound { Key = clusterKey }
                };
                var repositories = new TransactionalRepositoryResolver(factory, graph, callContext);
                var context = new MongoDbTransactionContext(clusterKey, activeSession, repositories);
                var result = await callback(context, token);
                if (result.IsFailed)
                {
                    throw new TransactionCallbackFailedException(result);
                }

                return result;
            }, options.DriverOptions, cancellationToken);
        }
        catch (TransactionCallbackFailedException exception)
        {
            return (Result<TResult>)exception.FailedResult;
        }
        catch (CrossClusterRejectedException exception)
        {
            return Result.Fail<TResult>(new CrossClusterOperationError(exception.Message));
        }
        catch (MongoException exception)
        {
            return MongoExceptionMapper.Fail<TResult>(exception);
        }
    }

    private sealed class TransactionCallbackFailedException(IResultBase result) : Exception
    {
        public IResultBase FailedResult { get; } = result;
    }
}
