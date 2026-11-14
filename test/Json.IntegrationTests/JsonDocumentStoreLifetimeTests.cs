using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.NewtonsoftJson;
using Dilcore.MongoDB.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.MongoDB.Json.IntegrationTests;

/// <summary>
/// Locks optional JSON store factory lifetimes without opening a MongoDB connection.
/// </summary>
public class JsonDocumentStoreLifetimeTests
{
    private const string PlaceholderConnectionString = "mongodb://127.0.0.1:27017";

    [Test]
    public void AddSystemTextJsonDocuments_RegistersFactory_AsScoped()
    {
        // Arrange
        using var provider = BuildProvider(services => services.AddSystemTextJsonDocuments());

        // Act & Assert
        AssertScoped<ISystemTextJsonDocumentStoreFactory>(provider);
    }

    [Test]
    public void AddNewtonsoftJsonDocuments_RegistersFactory_AsScoped()
    {
        // Arrange
        using var provider = BuildProvider(services => services.AddNewtonsoftJsonDocuments());

        // Act & Assert
        AssertScoped<INewtonsoftJsonDocumentStoreFactory>(provider);
    }

    private static ServiceProvider BuildProvider(Action<IServiceCollection> registerJson)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", cluster => cluster.UseConnectionString(PlaceholderConnectionString))
            .AddDatabase("JsonLifetimeDb", database =>
            {
                database.OnCluster("primary");
                database.AddDocumentBinding<JsonLifetimeDoc>("docs", binding => binding.WithCollectionName("docs"));
            }));
        registerJson(services);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    private static void AssertScoped<TService>(ServiceProvider provider)
        where TService : notnull
    {
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var first = scope1.ServiceProvider.GetRequiredService<TService>();
        first.ShouldBeSameAs(scope1.ServiceProvider.GetRequiredService<TService>());
        first.ShouldNotBeSameAs(scope2.ServiceProvider.GetRequiredService<TService>());
    }

    private sealed class JsonLifetimeDoc : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }
}