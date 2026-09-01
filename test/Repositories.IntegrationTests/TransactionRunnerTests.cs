using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Abstractions.Transactions;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.TestSupport;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[Category("M3Matrix")]
public class TransactionRunnerTests
{
    private MongoDbContainer _replicaSet = null!;

    [OneTimeSetUp]
    public async Task StartReplicaSet()
    {
        _replicaSet = MongoTestImages.CreateReplicaSet();
        await _replicaSet.StartAsync();
    }

    [OneTimeTearDown]
    public async Task StopReplicaSet() => await _replicaSet.DisposeAsync();

    [Test]
    public async Task Transaction_CommitsAcrossTwoCollections()
    {
        var (runner, _) = CreateHost();
        var result = await runner.ExecuteAsync(new MongoClusterKey("primary"), async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            var payments = tx.Repositories.GetRepository<Payment>("payments");
            (await orders.StoreAsync(new Order { Name = "o1" }, ct)).ShouldBeSuccess();
            (await payments.StoreAsync(new Payment { Name = "p1" }, ct)).ShouldBeSuccess();
            return Result.Ok(true);
        });
        result.ShouldBeSuccess();

        var (_, provider) = CreateHost();
        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");
        (await orders.HasAnyAsync(Builders<Order>.Filter.Empty)).Value.ShouldBeTrue();
    }

    [Test]
    public async Task Transaction_UserFailure_Aborts()
    {
        var (runner, provider) = CreateHost("abort-db");
        var result = await runner.ExecuteAsync(new MongoClusterKey("primary"), async (tx, ct) =>
        {
            var orders = tx.Repositories.GetRepository<Order>("orders");
            (await orders.StoreAsync(new Order { Name = "o-abort" }, ct)).ShouldBeSuccess();
            return Result.Fail<bool>("business rule");
        });
        result.ShouldBeFailure();

        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");
        (await orders.HasAnyAsync(Builders<Order>.Filter.Eq(x => x.Name, "o-abort"))).Value.ShouldBeFalse();
    }

    [Test]
    public async Task Transaction_Budget_FailsBeforeNextOperation()
    {
        var (runner, _) = CreateHost("budget-db");
        var result = await runner.ExecuteAsync(
            new MongoClusterKey("primary"),
            async (tx, ct) =>
            {
                var orders = tx.Repositories.GetRepository<Order>("orders");
                (await orders.StoreAsync(new Order { Name = "one" }, ct)).ShouldBeSuccess();
                var second = await orders.StoreAsync(new Order { Name = "two" }, ct);
                second.ShouldBeFailure();
                second.ShouldHaveError<TransactionBudgetExceededError>();
                return second.ToResult();
            },
            new MongoTransactionOptions { MaxOperations = 1 });
        result.ShouldBeFailure();
        result.ShouldHaveError<TransactionBudgetExceededError>();
    }

    private (IMongoDbTransactionRunner Runner, ServiceProvider Provider) CreateHost(string database = "TxDB")
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_replicaSet.GetConnectionString()))
            .AddDatabase(database, db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
                db.AddDocumentBinding<Payment>("payments", d => d.WithCollectionName("payments"));
            }));
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        return (provider.CreateScope().ServiceProvider.GetRequiredService<IMongoDbTransactionRunner>(), provider);
    }

    public sealed class Order : IDocumentEntity<Guid>, IHasConcurrencyToken
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public string? Name { get; set; }
    }

    public sealed class Payment : IDocumentEntity<Guid>, IHasConcurrencyToken
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public string? Name { get; set; }
    }
}
