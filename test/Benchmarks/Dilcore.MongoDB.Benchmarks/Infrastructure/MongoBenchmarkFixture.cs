using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Benchmarks.Models;
using Dilcore.MongoDB.DependencyInjection;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.Benchmarks.Infrastructure;

/// <summary>
/// Shared Testcontainers MongoDB lifecycle for repository benchmarks.
/// Mirrors <c>test/Repositories.IntegrationTests/Infrastructure/BaseIntegrationTests</c>.
/// </summary>
public sealed class MongoBenchmarkFixture : IAsyncDisposable
{
    private readonly MongoDbContainer _container;
    private ServiceProvider? _provider;
    private IServiceScope? _scope;
    private IMongoClient? _client;

    private MongoBenchmarkFixture(MongoDbContainer container)
    {
        _container = container;
    }

    public string ConnectionString => _container.GetConnectionString();

    public IServiceProvider Services =>
        _scope?.ServiceProvider
        ?? throw new InvalidOperationException("Call ConfigureServices before resolving services.");

    public IRepositoryResolver Resolver => Services.GetRequiredService<IRepositoryResolver>();

    public IGenericRepository<BenchmarkEntity> Repository =>
        Services.GetRequiredService<IGenericRepository<BenchmarkEntity>>();

    public IGenericBulkRepository<BenchmarkEntity> BulkRepository =>
        Services.GetRequiredService<IGenericBulkRepository<BenchmarkEntity>>();

    public IGenericProjectionRepository<BenchmarkEntity> ProjectionRepository =>
        Services.GetRequiredService<IGenericProjectionRepository<BenchmarkEntity>>();

    public IMongoCollection<BenchmarkEntity> Collection { get; private set; } = null!;

    public static async Task<MongoBenchmarkFixture> StartAsync()
    {
        var container = MongoTestImages.CreateStandalone();
        await container.StartAsync().ConfigureAwait(false);
        return new MongoBenchmarkFixture(container);
    }

    public void ConfigureServices(Action<IMongoDbBuilder> configure, string databaseName, string collectionName)
    {
        _scope?.Dispose();
        _provider?.Dispose();

        var services = new ServiceCollection();
        services.AddMongoDb(configure);

        _provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });
        _scope = _provider.CreateScope();

        // Resolve the library-owned client first so Guid serializer registration
        // happens via MongoClientHolder before any raw GetCollection mapping.
        _client = Services.GetRequiredKeyedService<IMongoClient>("primary");
        Collection = _client.GetDatabase(databaseName).GetCollection<BenchmarkEntity>(collectionName);
    }

    public void ConfigureServices(
        string databaseName,
        string collectionName,
        string bindingKey = "bench",
        bool softDelete = false,
        bool withBulk = false,
        bool withProjection = false)
    {
        ConfigureServices(
            mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString(ConnectionString))
                .AddDatabase(databaseName, db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<BenchmarkEntity>(bindingKey, d =>
                    {
                        d.WithCollectionName(collectionName);
                        if (softDelete)
                        {
                            d.WithSoftDelete();
                        }

                        if (withBulk)
                        {
                            d.WithBulkRepository();
                        }

                        if (withProjection)
                        {
                            d.WithProjectionRepository();
                        }
                    });
                }),
            databaseName,
            collectionName);
    }

    public IMongoCollection<BenchmarkEntity> GetCollection(string databaseName, string collectionName)
    {
        var client = _client
            ?? Services.GetRequiredKeyedService<IMongoClient>("primary");
        return client.GetDatabase(databaseName).GetCollection<BenchmarkEntity>(collectionName);
    }

    public static BenchmarkEntity NewEntity(string? name = null, int value = 0) =>
        new()
        {
            Name = name ?? $"entity-{Guid.NewGuid():N}",
            Value = value
        };

    public async ValueTask DisposeAsync()
    {
        _scope?.Dispose();
        if (_provider is not null)
        {
            await _provider.DisposeAsync().ConfigureAwait(false);
        }

        await _container.DisposeAsync().ConfigureAwait(false);
    }
}
