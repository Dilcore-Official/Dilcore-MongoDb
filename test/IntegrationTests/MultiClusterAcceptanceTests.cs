using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.IntegrationTests.Infrastructure;
using Dilcore.MongoDB.TestSupport;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.IntegrationTests;

/// <summary>
/// Exercises <c>AddCluster</c> with two genuinely separate MongoDB clusters (two containers),
/// as opposed to <see cref="DiAcceptanceTests"/> which points multiple cluster keys at the same
/// underlying server.
/// </summary>
public class MultiClusterAcceptanceTests
{
    private readonly MongoDbContainer _clusterA = MongoTestImages.CreateStandalone();
    private readonly MongoDbContainer _clusterB = MongoTestImages.CreateStandalone();

    [OneTimeSetUp]
    public async Task InitializeAsync()
    {
        await Task.WhenAll(_clusterA.StartAsync(), _clusterB.StartAsync());
    }

    [OneTimeTearDown]
    public async Task CleanupAsync()
    {
        await _clusterA.DisposeAsync();
        await _clusterB.DisposeAsync();
    }

    [Test]
    public async Task DistinctCollections_AcrossTwoClusters_StoreAndIsolateData()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("clusterA", c => c.UseConnectionString(_clusterA.GetConnectionString()))
            .AddCluster("clusterB", c => c.UseConnectionString(_clusterB.GetConnectionString()))
            .AddDatabase("ShopDb", db =>
            {
                db.OnCluster("clusterA");
                db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
            })
            .AddDatabase("AnalyticsDb", db =>
            {
                db.OnCluster("clusterB");
                db.AddDocumentBinding<PageView>("page-views", d => d.WithCollectionName("pageViews"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;

        await sp.GetRequiredService<IGenericRepository<Order>>().StoreAsync(new Order { Value = 1 });
        await sp.GetRequiredService<IGenericRepository<PageView>>().StoreAsync(new PageView { Value = "home" });

        var clientA = sp.GetRequiredKeyedService<IMongoClient>("clusterA");
        var clientB = sp.GetRequiredKeyedService<IMongoClient>("clusterB");

        var order = await clientA.GetDatabase("ShopDb").GetCollection<Order>("orders")
            .Find(x => x.Value == 1).FirstOrDefaultAsync();
        order.ShouldNotBeNull();

        var pageView = await clientB.GetDatabase("AnalyticsDb").GetCollection<PageView>("pageViews")
            .Find(x => x.Value == "home").FirstOrDefaultAsync();
        pageView.ShouldNotBeNull();

        // Neither database exists on the other cluster.
        var namesOnA = await (await clientA.ListDatabaseNamesAsync()).ToListAsync();
        namesOnA.ShouldNotContain("AnalyticsDb");

        var namesOnB = await (await clientB.ListDatabaseNamesAsync()).ToListAsync();
        namesOnB.ShouldNotContain("ShopDb");
    }

    [Test]
    public async Task SameCollectionNameAndDocumentType_AcrossTwoClusters_RemainIndependent()
    {
        var services = new ServiceCollection();

        // Both clusters bind the same TDocument to a collection literally named "orders" -
        // interesting because the two physical namespaces coincide even though the data must
        // stay fully isolated per cluster.
        services.AddMongoDb(mongo => mongo
            .AddCluster("clusterA", c => c.UseConnectionString(_clusterA.GetConnectionString()))
            .AddCluster("clusterB", c => c.UseConnectionString(_clusterB.GetConnectionString()))
            .AddDatabase("AppDbOnA", db =>
            {
                db.OnCluster("clusterA");
                db.AddDocumentBinding<Order>("orders-a", d => d.WithCollectionName("orders"));
            })
            .AddDatabase("AppDbOnB", db =>
            {
                db.OnCluster("clusterB");
                db.AddDocumentBinding<Order>("orders-b", d => d.WithCollectionName("orders"));
            }));

        using var root = AcceptanceServiceProviderFactory.Create(services);
        using var scope = root.CreateScope();
        var sp = scope.ServiceProvider;
        var repositories = sp.GetRequiredService<IRepositoryResolver>();

        // Same TDocument bound twice: the unkeyed repository must stay ambiguous.
        Should.Throw<InvalidOperationException>(() => repositories.GetRepository<Order>());

        var ordersA = repositories.GetRepository<Order>("orders-a");
        var ordersB = repositories.GetRepository<Order>("orders-b");

        await ordersA.StoreAsync(new Order { Value = 1 });
        await ordersB.StoreAsync(new Order { Value = 2 });

        var clientA = sp.GetRequiredKeyedService<IMongoClient>("clusterA");
        var clientB = sp.GetRequiredKeyedService<IMongoClient>("clusterB");

        var collectionA = clientA.GetDatabase("AppDbOnA").GetCollection<Order>("orders");
        var collectionB = clientB.GetDatabase("AppDbOnB").GetCollection<Order>("orders");

        (await collectionA.CountDocumentsAsync(FilterDefinition<Order>.Empty)).ShouldBe(1);
        (await collectionB.CountDocumentsAsync(FilterDefinition<Order>.Empty)).ShouldBe(1);

        (await collectionA.Find(x => x.Value == 1).FirstOrDefaultAsync()).ShouldNotBeNull();
        (await collectionA.Find(x => x.Value == 2).FirstOrDefaultAsync()).ShouldBeNull();

        (await collectionB.Find(x => x.Value == 2).FirstOrDefaultAsync()).ShouldNotBeNull();
        (await collectionB.Find(x => x.Value == 1).FirstOrDefaultAsync()).ShouldBeNull();
    }

    private class Order : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Value { get; set; }
    }

    private class PageView : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Value { get; set; }
    }
}
