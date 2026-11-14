using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Ownership;
using Dilcore.MongoDB.Extensions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.IntegrationTests.Examples;

/// <summary>Proves docs/security/production-mongodb.md.</summary>
public class ProductionMongoDbExampleTests : ExampleIntegrationTests
{
    public sealed class Order : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    [Test]
    public void ConfiguredClient_IsUsedByTheToolkit()
    {
        // Arrange
        var services = new ServiceCollection();
        var settings = MongoClientSettings.FromConnectionString(connectionString);
        settings.UseTls = true;
        settings.ApplicationName = "orders-api";
        settings.RetryWrites = true;

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseExistingClient(new MongoClient(settings), MongoClientOwnership.LibraryOwned))
            .AddDatabase("shop", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
            }));

        // Act
        using var provider = AcceptanceServiceProviderFactory.Create(services);
        var client = provider.GetRequiredKeyedService<IMongoClient>("primary");

        // Assert
        client.Settings.UseTls.ShouldBeTrue();
        client.Settings.ApplicationName.ShouldBe("orders-api");
    }

    [Test]
    public void ConnectionStringAndExistingClient_CannotBeCombined()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c
                .UseConnectionString(connectionString)
                .UseExistingClient(new MongoClient(connectionString), MongoClientOwnership.LibraryOwned))
            .AddDatabase("shop", db => db.OnCluster("primary")));

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }
}