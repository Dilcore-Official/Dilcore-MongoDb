using Dilcore.MongoDB.Extensions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.IntegrationTests.Examples;

/// <summary>Proves docs/product/package-selection.md.</summary>
public class PackageSelectionExampleTests : ExampleIntegrationTests
{
    [Test]
    public void MinimalRegistration_BuildsWithScopeAndBuildValidation()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db => db.OnCluster("primary")));

        // Act
        using var provider = AcceptanceServiceProviderFactory.Create(services);
        var client = provider.GetRequiredKeyedService<IMongoClient>("primary");

        // Assert
        client.ShouldNotBeNull();
    }

    [Test]
    public void RegistrationWithoutCluster_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddMongoDb(_ => { });

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }
}