using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Abstractions.Transactions;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/transactions.md.</summary>
public class TransactionsExampleTests
{
    private MongoDbContainer _replicaSet = null!;

    private string connectionString => _replicaSet.GetConnectionString();

    public sealed class Order : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = "";
    }

    public sealed class Payment : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
    }

    public sealed class CheckoutService(IMongoDbTransactionRunner runner)
    {
        public Task<Result<Guid>> PlaceOrderAsync(Order order, CancellationToken cancellationToken = default)
        {
            var clusterKey = new MongoClusterKey("primary");
            var options = new MongoTransactionOptions
            {
                MaxOperations = 100,
                TimeLimit = TimeSpan.FromSeconds(10)
            };

            return runner.WithTransactionAsync(
                clusterKey,
                async (tx, ct) =>
                {
                    var orders = tx.Repositories.GetRepository<Order>("orders");
                    var payments = tx.Repositories.GetRepository<Payment>("payments");

                    var attempt = new Order { Id = order.Id, Sku = order.Sku };
                    var stored = await orders.StoreAsync(attempt, ct);
                    if (stored.IsFailed)
                    {
                        return stored.ToResult();
                    }

                    var paymentDocument = new Payment { OrderId = stored.Value.Id };
                    var payment = await payments.StoreAsync(paymentDocument, ct);
                    if (payment.IsFailed)
                    {
                        return payment.ToResult();
                    }

                    return Result.Ok(stored.Value.Id);
                },
                options,
                cancellationToken);
        }
    }

    [OneTimeSetUp]
    public async Task StartReplicaSetAsync()
    {
        _replicaSet = MongoTestImages.CreateReplicaSet();
        await _replicaSet.StartAsync();
    }

    [OneTimeTearDown]
    public async Task StopReplicaSetAsync() => await _replicaSet.DisposeAsync();

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("shop", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Order>("orders", d => d
                    .WithCollectionName("orders")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
                db.AddDocumentBinding<Payment>("payments", d => d
                    .WithCollectionName("payments")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task PlaceOrder_CommitsBothCollections()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var checkout = ActivatorUtilities.CreateInstance<CheckoutService>(scope.ServiceProvider);
        var payments = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Payment>>("payments");

        // Act
        var placed = await checkout.PlaceOrderAsync(new Order { Sku = "commit" }, CancellationToken.None);
        var payment = await payments.GetAsync(Builders<Payment>.Filter.Eq(x => x.OrderId, placed.Value));

        // Assert
        placed.ShouldBeSuccess();
        payment.ShouldBeSuccess();
    }

    [Test]
    public async Task FailedResult_AbortsTheTransaction()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMongoDbTransactionRunner>();
        var orders = scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders");

        // Act
        var result = await runner.WithTransactionAsync(
            new MongoClusterKey("primary"),
            async (tx, ct) =>
            {
                var inTx = tx.Repositories.GetRepository<Order>("orders");
                (await inTx.StoreAsync(new Order { Sku = "abort" }, ct)).ShouldBeSuccess();

                return Result.Fail<Guid>("business rule");
            });
        var any = await orders.HasAnyAsync(Builders<Order>.Filter.Eq(x => x.Sku, "abort"));

        // Assert
        result.ShouldBeFailure();
        any.Value.ShouldBeFalse();
    }

    [Test]
    public async Task ExceededBudget_ReturnsTransactionBudgetExceededError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMongoDbTransactionRunner>();

        // Act
        var result = await runner.WithTransactionAsync(
            new MongoClusterKey("primary"),
            async (tx, ct) =>
            {
                var orders = tx.Repositories.GetRepository<Order>("orders");
                (await orders.StoreAsync(new Order { Sku = "one" }, ct)).ShouldBeSuccess();
                var second = await orders.StoreAsync(new Order { Sku = "two" }, ct);

                return second.ToResult();
            },
            new MongoTransactionOptions { MaxOperations = 1 });

        // Assert
        result.HasError<TransactionBudgetExceededError>().ShouldBeTrue();
    }
}