using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.NewtonsoftJson;
using Dilcore.MongoDB.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.MongoDB.Json.IntegrationTests;

/// <summary>
/// DI registration tests only — no live MongoDB. Uses a placeholder connection string
/// so parallel Testcontainers startups in this assembly cannot fail OneTimeSetUp.
/// </summary>
[Category("M3Matrix")]
public class JsonDocumentStoreRegistrationTests
{
    private const string PlaceholderConnectionString = "mongodb://localhost";

    [Test]
    public void AddSystemTextJsonDocuments_WithoutAddMongoDb_FailsClosed()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => services.AddSystemTextJsonDocuments())
            .Message.ShouldContain("AddMongoDb");
    }

    [Test]
    public void AddNewtonsoftJsonDocuments_WithoutAddMongoDb_FailsClosed()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<InvalidOperationException>(() => services.AddNewtonsoftJsonDocuments())
            .Message.ShouldContain("AddMongoDb");
    }

    [Test]
    public void AddSystemTextJsonDocuments_IsIdempotent_AndValidateOnBuild()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(PlaceholderConnectionString))
            .AddDatabase("JsonRegDB", db => db.OnCluster("primary")));
        services.AddSystemTextJsonDocuments();
        services.AddSystemTextJsonDocuments();

        // Act
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ISystemTextJsonDocumentStoreFactory>();
        var store = factory.Create(new MongoDatabaseKey("JsonRegDB"));
        var documentStoreFactory = scope.ServiceProvider.GetRequiredService<IJsonDocumentStoreFactory>();

        // Assert
        store.Converter.ShouldBeOfType<Dilcore.MongoDB.Json.BsonJsonConverter>();
        documentStoreFactory.ShouldNotBeNull();
    }

    [Test]
    public void AddNewtonsoftJsonDocuments_IsIdempotent_AndValidateOnBuild()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(PlaceholderConnectionString))
            .AddDatabase("JsonNsRegDB", db => db.OnCluster("primary")));
        services.AddNewtonsoftJsonDocuments();
        services.AddNewtonsoftJsonDocuments();

        // Act
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<INewtonsoftJsonDocumentStoreFactory>();
        var store = factory.Create(new MongoDatabaseKey("JsonNsRegDB"));

        // Assert
        store.ShouldNotBeNull();
    }

    [Test]
    public async Task UnknownDatabaseKey_FailsClosed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(PlaceholderConnectionString))
            .AddDatabase("KnownDB", db => db.OnCluster("primary")));
        services.AddSystemTextJsonDocuments();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider
            .GetRequiredService<ISystemTextJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("MissingDB"));

        // Act
        var result = await store.InsertAsync("payloads", """{"v":1}""");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Unknown database key");
    }
}