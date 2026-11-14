using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/product/driver-escape-hatches.md.</summary>
public class DriverEscapeHatchesExampleTests : ExampleIntegrationTests
{
    public sealed class Order : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = "";
    }

    public sealed class ReportService(IMongoDbCollectionFactory collections, IServiceProvider provider)
    {
        public async Task<IReadOnlyList<BsonDocument>> TopSkusAsync(CancellationToken cancellationToken)
        {
            var resolved = await collections.GetCollectionAsync<Order>(new MongoDocumentBindingKey("orders"), cancellationToken);
            var collection = resolved.Value;

            return await collection.Aggregate()
                .Group(new BsonDocument { { "_id", "$sku" }, { "count", new BsonDocument("$sum", 1) } })
                .SortByDescending(x => x["count"])
                .Limit(10)
                .ToListAsync(cancellationToken);
        }

        public IMongoClient Client() => provider.GetRequiredKeyedService<IMongoClient>("primary");
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("shop", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task Aggregation_RunsThroughResolvedCollection()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var orders = scope.ServiceProvider.GetRequiredService<IGenericRepository<Order>>();
        foreach (var sku in new[] { "a", "a", "b" })
        {
            (await orders.StoreAsync(new Order { Sku = sku })).ShouldBeSuccess();
        }

        var service = ActivatorUtilities.CreateInstance<ReportService>(scope.ServiceProvider);

        // Act
        var top = await service.TopSkusAsync(CancellationToken.None);

        // Assert
        top[0]["_id"].AsString.ShouldBe("a");
        top[0]["count"].ToInt32().ShouldBe(2);
        service.Client().ShouldBeSameAs(provider.GetRequiredKeyedService<IMongoClient>("primary"));
    }

    [Test]
    public async Task UnknownBinding_ReturnsFailedResult()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var collections = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        // Act
        var resolved = await collections.GetCollectionAsync<Order>(new MongoDocumentBindingKey("missing"));

        // Assert
        resolved.ShouldBeFailure();
    }
}