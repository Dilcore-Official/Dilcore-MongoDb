using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.IntegrationTests.Examples;

/// <summary>Proves docs/guides/multi-tenant-databases.md.</summary>
public class MultiTenantDatabasesExampleTests : ExampleIntegrationTests
{
    public interface ITenantAccessor
    {
        Task<string?> GetCurrentTenantIdAsync(CancellationToken cancellationToken = default);
    }

    public sealed class TestTenantAccessor : ITenantAccessor
    {
        public string? TenantId { get; set; }

        public Task<string?> GetCurrentTenantIdAsync(CancellationToken cancellationToken) => Task.FromResult(TenantId);
    }

    public sealed class TenantDatabasePrefixResolver(ITenantAccessor tenants) : INamespacePrefixResolver
    {
        public async Task<Result<string?>> ResolveAsync(
            NamespaceResolutionRequest request,
            CancellationToken cancellationToken = default)
        {
            var tenantId = await tenants.GetCurrentTenantIdAsync(cancellationToken);

            if (string.IsNullOrWhiteSpace(tenantId))
            {
                return Result.Fail<string?>("Tenant context is required.");
            }

            return Result.Ok<string?>(tenantId);
        }
    }

    public sealed class Order : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = "";
    }

    public sealed class OrderService(IGenericRepository<Order> orders)
    {
        public Task<Result<Order>> CreateAsync(Order order, CancellationToken cancellationToken = default)
            => orders.StoreAsync(order, cancellationToken);

        public Task<Result<IReadOnlyList<Order>>> ListAsync(CancellationToken cancellationToken = default)
            => orders.GetListAsync(cancellationToken);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddScoped<TestTenantAccessor>();
        services.AddScoped<ITenantAccessor>(sp => sp.GetRequiredService<TestTenantAccessor>());

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("shop", db =>
            {
                db.OnCluster("primary");

                db.WithNamespacePrefixResolver<TenantDatabasePrefixResolver>();
                db.AddDocumentBinding<Order>("orders", d => d
                    .WithCollectionName("orders")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    private async Task<List<string>> DatabaseNamesAsync()
    {
        using var client = new MongoClient(connectionString);
        var names = await client.ListDatabaseNamesAsync();

        return await names.ToListAsync();
    }

    [Test]
    public async Task EachTenant_WritesToItsOwnDatabase()
    {
        // Arrange
        await using var provider = BuildProvider();

        var tenants = new[] { "tenantA", "tenantB" };

        // Act
        foreach (var tenant in tenants)
        {
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<TestTenantAccessor>().TenantId = tenant;
            var service = ActivatorUtilities.CreateInstance<OrderService>(scope.ServiceProvider);
            (await service.CreateAsync(new Order { Sku = tenant }, CancellationToken.None)).ShouldBeSuccess();
        }

        var skusByTenant = new Dictionary<string, IReadOnlyList<string>>();
        foreach (var tenant in tenants)
        {
            using var scope = provider.CreateScope();
            scope.ServiceProvider.GetRequiredService<TestTenantAccessor>().TenantId = tenant;
            var service = ActivatorUtilities.CreateInstance<OrderService>(scope.ServiceProvider);
            var listed = await service.ListAsync(CancellationToken.None);
            listed.ShouldBeSuccess();
            skusByTenant[tenant] = listed.Value.Select(order => order.Sku).ToList();
        }

        var databases = await DatabaseNamesAsync();

        // Assert
        databases.ShouldContain("tenantA_shop");
        databases.ShouldContain("tenantB_shop");
        databases.ShouldNotContain("shop");
        skusByTenant["tenantA"].ShouldBe(["tenantA"]);
        skusByTenant["tenantB"].ShouldBe(["tenantB"]);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public async Task InvalidTenant_FailsClosedWithoutTouchingAnyDatabase(string? tenantId)
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<TestTenantAccessor>().TenantId = tenantId;
        var service = ActivatorUtilities.CreateInstance<OrderService>(scope.ServiceProvider);

        // Act
        var created = await service.CreateAsync(new Order { Sku = "x" }, CancellationToken.None);
        var databases = await DatabaseNamesAsync();

        // Assert
        created.ShouldBeFailure();
        created.Errors.ShouldContain(e => e.Message.Contains("Tenant context is required."));
        databases.ShouldNotContain("shop");
    }

    [Test]
    public async Task TenantPrefixCombinesWithStaticPrefix()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<TestTenantAccessor>();
        services.AddScoped<ITenantAccessor>(sp => sp.GetRequiredService<TestTenantAccessor>());
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("UserDB", db =>
            {
                db.OnCluster("primary");
                db.WithNamespacePrefix("catalog");
                db.WithNamespacePrefixResolver<TenantDatabasePrefixResolver>();
                db.AddDocumentBinding<Order>("orders", d => d
                    .WithCollectionName("orders")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
            }));
        await using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<TestTenantAccessor>().TenantId = "tenantA";
        var service = ActivatorUtilities.CreateInstance<OrderService>(scope.ServiceProvider);

        // Act
        (await service.CreateAsync(new Order { Sku = "x" }, CancellationToken.None)).ShouldBeSuccess();
        var databases = await DatabaseNamesAsync();

        // Assert
        databases.ShouldContain("tenantA_catalog_UserDB");
    }
}