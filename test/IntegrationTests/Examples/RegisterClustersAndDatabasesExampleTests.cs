using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.IntegrationTests.Examples;

/// <summary>Proves docs/guides/register-clusters-and-databases.md.</summary>
public class RegisterClustersAndDatabasesExampleTests : ExampleIntegrationTests
{
    public sealed class Order : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Sku { get; set; } = "";
    }

    public sealed class ArchiveService(IRepositoryResolver repositories)
    {
        public async Task<Result> ArchiveAsync(Order order, CancellationToken cancellationToken)
        {
            var archived = repositories.GetRepository<Order>("archived-orders");

            var stored = await archived.StoreAsync(order, cancellationToken);

            return stored.ToResult();
        }
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var archiveConnectionString = connectionString;

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddCluster("archive", c => c.UseConnectionString(archiveConnectionString))
            .AddDatabase("shop", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
            })
            .AddDatabase("shop-archive", db =>
            {
                db.OnCluster("archive");

                db.AddDocumentBinding<Order>("archived-orders", d => d.WithCollectionName("orders"));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task ArchiveByBindingKey_WritesToArchiveDatabaseOnly()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<ArchiveService>(scope.ServiceProvider);
        var order = new Order { Id = Guid.NewGuid(), Sku = "a" };

        // Act
        var archived = await service.ArchiveAsync(order, CancellationToken.None);
        var inShop = await scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("orders")
            .HasAnyAsync(Builders<Order>.Filter.Empty);
        var inArchive = await scope.ServiceProvider.GetRequiredKeyedService<IGenericRepository<Order>>("archived-orders")
            .HasAnyAsync(Builders<Order>.Filter.Empty);

        // Assert
        archived.ShouldBeSuccess();
        inShop.Value.ShouldBeFalse();
        inArchive.Value.ShouldBeTrue();
    }

    [Test]
    public async Task ClientsAreKeyedSingletons_AndUnkeyedRepositoryIsAbsentForSharedType()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();

        // Act
        var primary1 = provider.GetRequiredKeyedService<IMongoClient>("primary");
        var primary2 = provider.GetRequiredKeyedService<IMongoClient>("primary");
        var unkeyed = scope.ServiceProvider.GetService<IGenericRepository<Order>>();

        // Assert
        primary1.ShouldBeSameAs(primary2);
        unkeyed.ShouldBeNull();
    }

    [Test]
    public void InvalidRegistrations_ThrowAtStartup()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var duplicateBinding = () => services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("shop", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
                db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders2"));
            }));
        var missingCollection = () => services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("shop", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Order>("orders", _ => { });
            }));
        var unknownCluster = () => services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("shop", db => db.OnCluster("nope")));

        // Assert
        Should.Throw<InvalidOperationException>(duplicateBinding);
        Should.Throw<InvalidOperationException>(missingCollection);
        Should.Throw<InvalidOperationException>(unknownCluster);
    }
}