using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/resolve-collections.md.</summary>
public class ResolveCollectionsExampleTests : ExampleIntegrationTests
{
    public sealed class Order : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = "";
    }

    public sealed class OrderCollections(
        // 1. Keyed injection. The key is the binding key. Scoped, resolved when the service is created.
        [FromKeyedServices("orders")] IMongoCollection<Order> injected,
        // 2. The factory, for run-time resolution and for collections without a binding.
        IMongoDbCollectionFactory collections,
        // 3. The database, by name. Keyed by the database key.
        [FromKeyedServices("shop")] IMongoDatabase database,
        IMongoDatabaseResolver databases)
    {
        // Plain driver calls. No repository policies apply: no soft delete, ETag, audit or id generation.
        public async Task<long> CountBySkuAsync(string sku, CancellationToken cancellationToken)
        {
            var filter = Builders<Order>.Filter.Eq(x => x.Sku, sku);

            return await injected.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        }

        // The typed collection from the factory returns a Result, so a failure is a value.
        public async Task<Result<IMongoCollection<Order>>> OrdersAsync(CancellationToken cancellationToken)
            => await collections.GetCollectionAsync<Order>(new MongoDocumentBindingKey("orders"), cancellationToken);

        // A collection that has no binding. Any logical name works, the database key picks the database.
        public async Task<Result> AuditAsync(string message, CancellationToken cancellationToken)
        {
            var resolved = await collections.GetCollectionAsync(
                new MongoDatabaseKey("shop"), "audit-log", cancellationToken: cancellationToken);
            if (resolved.IsFailed)
                return resolved.ToResult();

            await resolved.Value.InsertOneAsync(new BsonDocument("message", message), cancellationToken: cancellationToken);

            return Result.Ok();
        }

        // The physical name only, for logging or as a $lookup or $merge target.
        public async Task<string?> PhysicalNameAsync(CancellationToken cancellationToken)
        {
            var name = await collections.ResolveCollectionNameAsync(
                new MongoDatabaseKey("shop"), "audit-log", cancellationToken: cancellationToken);

            return name.IsSuccess ? name.Value : null;
        }

        // The keyed database for collections you name yourself. Note that the name is NOT prefixed here.
        public async Task<IReadOnlyList<string>> CollectionNamesAsync(CancellationToken cancellationToken)
        {
            var names = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken);

            return await names.ToListAsync(cancellationToken);
        }

        // The resolver returns a Result instead of throwing.
        public async Task<string?> DatabaseNameAsync(CancellationToken cancellationToken)
        {
            var resolved = await databases.GetDatabaseAsync(new MongoDatabaseKey("shop"), cancellationToken);

            return resolved.IsSuccess ? resolved.Value.DatabaseNamespace.DatabaseName : null;
        }
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("shop", db =>
            {
                db.OnCluster("primary");

                db.WithNamespacePrefix("catalog");

                db.AddDocumentBinding<Order>("orders", d => d
                    .WithCollectionName("orders")
                    .WithNamespacePrefix("feature"));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task KeyedCollection_QueriesThePrefixedCollection()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<OrderCollections>(scope.ServiceProvider);
        var orders = (await service.OrdersAsync(CancellationToken.None)).Value;
        await orders.InsertManyAsync([new Order { Id = Guid.NewGuid(), Sku = "a" }, new Order { Id = Guid.NewGuid(), Sku = "a" }]);

        // Act
        var count = await service.CountBySkuAsync("a", CancellationToken.None);

        // Assert
        count.ShouldBe(2);
        orders.Database.DatabaseNamespace.DatabaseName.ShouldBe("catalog_shop");
        orders.CollectionNamespace.CollectionName.ShouldBe("feature_orders");
    }

    [Test]
    public async Task UntypedCollection_WritesAndReportsItsPhysicalName()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<OrderCollections>(scope.ServiceProvider);

        // Act
        var audited = await service.AuditAsync("hello", CancellationToken.None);
        var name = await service.PhysicalNameAsync(CancellationToken.None);
        var names = await service.CollectionNamesAsync(CancellationToken.None);
        var database = await service.DatabaseNameAsync(CancellationToken.None);

        // Assert
        audited.ShouldBeSuccess();
        name.ShouldBe("audit-log");
        names.ShouldContain("audit-log");
        database.ShouldBe("catalog_shop");
    }

    [Test]
    public async Task StaticPrefix_PrefixesAnUnboundCollection()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var collections = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        // Act
        var name = await collections.ResolveCollectionNameAsync(new MongoDatabaseKey("shop"), "audit-log", staticPrefix: "feature");

        // Assert
        name.ShouldBeSuccess();
        name.Value.ShouldBe("feature_audit-log");
    }

    [Test]
    public async Task UnknownDatabase_ReturnsFailedResult()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var collections = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        // Act
        var resolved = await collections.GetCollectionAsync(
            new MongoDatabaseKey("unknown"), "audit-log", cancellationToken: CancellationToken.None);

        // Assert
        resolved.ShouldBeFailure();
    }
}