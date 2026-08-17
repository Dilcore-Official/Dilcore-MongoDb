using BenchmarkDotNet.Attributes;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Benchmarks.Models;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Benchmarks;

/// <summary>
/// Cold-start: DI registration + first repository resolve vs raw MongoClient + collection.
/// No live MongoDB connection is required (client construction is lazy).
/// </summary>
/// <remarks>
/// Telemetry on/off overhead budgets from docs/product/v2-goals.md are deferred until
/// M6 instrumentation lands (GitHub issues #33 / #34).
/// </remarks>
public class ColdStartBenchmarks
{
    private const string ConnectionString = "mongodb://127.0.0.1:27017";

    [Benchmark(Baseline = true)]
    public IMongoCollection<BenchmarkEntity> RawDriver_CreateClientAndGetCollection()
    {
        var client = new MongoClient(ConnectionString);
        return client.GetDatabase("bench-coldstart").GetCollection<BenchmarkEntity>("entities");
    }

    [Benchmark]
    public IGenericRepository<BenchmarkEntity> Library_ConfigureDiAndResolveBinding()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(ConnectionString))
            .AddDatabase("bench-coldstart", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<BenchmarkEntity>("e1", d => d
                    .WithCollectionName("entities"));
            }));

        // Intentionally not disposed: cold-start measures construction + first resolve only.
        var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = true
        });

        var scope = provider.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IGenericRepository<BenchmarkEntity>>();
    }
}
