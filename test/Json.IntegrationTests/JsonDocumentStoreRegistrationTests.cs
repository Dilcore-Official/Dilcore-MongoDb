using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.NewtonsoftJson;
using Dilcore.MongoDB.SystemTextJson;
using Dilcore.MongoDB.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.Json.IntegrationTests;

[Category("M3Matrix")]
public class JsonDocumentStoreRegistrationTests
{
    private MongoDbContainer _container = null!;

    [OneTimeSetUp]
    public async Task Start()
    {
        _container = MongoTestImages.CreateStandalone();
        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task Stop() => await _container.DisposeAsync();

    [Test]
    public void AddSystemTextJsonDocuments_WithoutAddMongoDb_FailsClosed()
    {
        var services = new ServiceCollection();
        Should.Throw<InvalidOperationException>(() => services.AddSystemTextJsonDocuments())
            .Message.ShouldContain("AddMongoDb");
    }

    [Test]
    public void AddNewtonsoftJsonDocuments_WithoutAddMongoDb_FailsClosed()
    {
        var services = new ServiceCollection();
        Should.Throw<InvalidOperationException>(() => services.AddNewtonsoftJsonDocuments())
            .Message.ShouldContain("AddMongoDb");
    }

    [Test]
    public void AddSystemTextJsonDocuments_IsIdempotent_AndValidateOnBuild()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_container.GetConnectionString()))
            .AddDatabase("JsonRegDB", db => db.OnCluster("primary")));
        services.AddSystemTextJsonDocuments();
        services.AddSystemTextJsonDocuments();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<ISystemTextJsonDocumentStoreFactory>();
        var store = factory.Create(new MongoDatabaseKey("JsonRegDB"));
        store.Converter.ShouldBeOfType<Dilcore.MongoDB.Json.BsonJsonConverter>();
        scope.ServiceProvider.GetRequiredService<IJsonDocumentStoreFactory>().ShouldNotBeNull();
    }

    [Test]
    public void AddNewtonsoftJsonDocuments_IsIdempotent_AndValidateOnBuild()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_container.GetConnectionString()))
            .AddDatabase("JsonNsRegDB", db => db.OnCluster("primary")));
        services.AddNewtonsoftJsonDocuments();
        services.AddNewtonsoftJsonDocuments();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<INewtonsoftJsonDocumentStoreFactory>();
        factory.Create(new MongoDatabaseKey("JsonNsRegDB")).ShouldNotBeNull();
    }

    [Test]
    public async Task UnknownDatabaseKey_FailsClosed()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_container.GetConnectionString()))
            .AddDatabase("KnownDB", db => db.OnCluster("primary")));
        services.AddSystemTextJsonDocuments();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider
            .GetRequiredService<ISystemTextJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("MissingDB"));

        var result = await store.InsertAsync("payloads", """{"v":1}""");
        result.IsFailed.ShouldBeTrue();
    }
}
