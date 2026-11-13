using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Transactions;
using Dilcore.MongoDB.Descriptors;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Dilcore.MongoDB.UnitTests;

/// <summary>
/// Locks <c>AddMongoDb</c> lifetimes without opening a MongoDB connection.
/// </summary>
public class MongoDbRegistrationLifetimeTests
{
    private const string PlaceholderConnectionString = "mongodb://127.0.0.1:27017";

    [Test]
    public void AddMongoDb_RegistersSingletons_OncePerRootProvider()
    {
        using var provider = BuildProvider();

        AssertSingleton<MongoRegistrationGraph>(provider);
        AssertSingleton<IBsonJsonConverter>(provider);
        AssertKeyedSingleton<IMongoClient>(provider, "primary");
    }

    [Test]
    public void AddMongoDb_RegistersScopedServices_PerScope()
    {
        using var provider = BuildProvider();

        AssertScoped<INamespaceResolver>(provider);
        AssertScoped<IMongoDatabaseResolver>(provider);
        AssertScoped<IMongoDbCollectionFactory>(provider);
        AssertScoped<IMongoDbTransactionRunner>(provider);
        AssertScoped<IMongoDbProvisioner>(provider);
        AssertScoped<IJsonDocumentStoreFactory>(provider);
        AssertScoped<IRepositoryResolver>(provider);
        AssertKeyedScoped<IMongoDatabase>(provider, "LifetimeDb");
        AssertKeyedScoped<IGenericRepository<LifetimeDoc>>(provider, "docs");
        AssertKeyedScoped<IMongoCollection<LifetimeDoc>>(provider, "docs");
    }

    [Test]
    public void AddMongoDb_RegistersNamespaceContributors_AsScopedEnumerable()
    {
        using var provider = BuildProvider();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var first = scope1.ServiceProvider.GetServices<INamespaceSegmentContributor>().ToArray();
        var again = scope1.ServiceProvider.GetServices<INamespaceSegmentContributor>().ToArray();
        var second = scope2.ServiceProvider.GetServices<INamespaceSegmentContributor>().ToArray();

        first.Length.ShouldBe(2);
        first.Select(c => c.GetType()).ShouldBe(again.Select(c => c.GetType()));
        first[0].ShouldBeSameAs(again[0]);
        first[0].ShouldNotBeSameAs(second[0]);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", cluster => cluster.UseConnectionString(PlaceholderConnectionString))
            .AddDatabase("LifetimeDb", database =>
            {
                database.OnCluster("primary");
                database.AddDocumentBinding<LifetimeDoc>("docs", binding => binding.WithCollectionName("docs"));
            }));

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
    }

    private static void AssertSingleton<TService>(ServiceProvider provider)
        where TService : notnull
    {
        var root = provider.GetRequiredService<TService>();
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        root.ShouldBeSameAs(provider.GetRequiredService<TService>());
        root.ShouldBeSameAs(scope1.ServiceProvider.GetRequiredService<TService>());
        root.ShouldBeSameAs(scope2.ServiceProvider.GetRequiredService<TService>());
    }

    private static void AssertKeyedSingleton<TService>(ServiceProvider provider, string key)
        where TService : notnull
    {
        var root = provider.GetRequiredKeyedService<TService>(key);
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        root.ShouldBeSameAs(provider.GetRequiredKeyedService<TService>(key));
        root.ShouldBeSameAs(scope1.ServiceProvider.GetRequiredKeyedService<TService>(key));
        root.ShouldBeSameAs(scope2.ServiceProvider.GetRequiredKeyedService<TService>(key));
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

    private static void AssertKeyedScoped<TService>(ServiceProvider provider, string key)
        where TService : notnull
    {
        using var scope1 = provider.CreateScope();
        using var scope2 = provider.CreateScope();

        var first = scope1.ServiceProvider.GetRequiredKeyedService<TService>(key);
        first.ShouldBeSameAs(scope1.ServiceProvider.GetRequiredKeyedService<TService>(key));
        first.ShouldNotBeSameAs(scope2.ServiceProvider.GetRequiredKeyedService<TService>(key));
    }

    private sealed class LifetimeDoc : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }
}