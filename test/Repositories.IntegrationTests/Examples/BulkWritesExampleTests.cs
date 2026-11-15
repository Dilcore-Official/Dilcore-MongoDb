using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/bulk-writes.md.</summary>
public class BulkWritesExampleTests : ExampleIntegrationTests
{
    public sealed class Reading : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string Sensor { get; set; } = "";
        public double Value { get; set; }
    }

    public sealed class ReadingImporter(IGenericBulkRepository<Reading> bulk)
    {
        public IReadOnlyList<int> FailedIndexes { get; private set; } = [];

        public async Task<Result> ImportAsync(
            IReadOnlyList<Reading> batch,
            CancellationToken cancellationToken = default)
        {
            var options = new MongoBulkWriteOptions { IsOrdered = false };

            var result = await bulk.BulkStoreRangeAsync(batch, options, cancellationToken);

            var partial = result.Errors.OfType<BulkWritePartialFailureError>().FirstOrDefault();
            var failed = partial is null
                ? []
                : partial.Items.Where(i => !i.Succeeded).Select(i => i.Index);
            FailedIndexes = failed.ToList();

            return result.ToResult();
        }

        public Task<Result> RemoveSensorAsync(string sensor, CancellationToken cancellationToken = default)
            => bulk.BulkDeleteAsync(x => x.Sensor == sensor, cancellationToken);
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("iot", db =>
            {
                db.OnCluster("primary");

                db.AddDocumentBinding<Reading>("readings", d => d
                    .WithCollectionName("readings")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
                    .WithBulkRepository());
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task ImportThenDelete_Works()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var importer = ActivatorUtilities.CreateInstance<ReadingImporter>(scope.ServiceProvider);
        var batch = Enumerable.Range(0, 5).Select(i => new Reading { Sensor = "s1", Value = i }).ToList();

        // Act
        var imported = await importer.ImportAsync(batch, CancellationToken.None);
        var removed = await importer.RemoveSensorAsync("s1", CancellationToken.None);
        var removedAgain = await importer.RemoveSensorAsync("s1", CancellationToken.None);

        // Assert
        imported.ShouldBeSuccess();
        removed.ShouldBeSuccess();
        removedAgain.HasError<DocumentNotFoundError>().ShouldBeTrue();
    }

    [Test]
    public async Task DuplicateIds_ReportPartialFailureWithItemIndexes()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var importer = ActivatorUtilities.CreateInstance<ReadingImporter>(scope.ServiceProvider);
        var collections = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var collection = (await collections.GetCollectionAsync<Reading>(
            new Abstractions.Keys.MongoDocumentBindingKey("readings"))).Value;
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Reading>(
            Builders<Reading>.IndexKeys.Ascending(x => x.Sensor),
            new CreateIndexOptions { Unique = true, Name = "sensor_unique" }));
        await collection.InsertOneAsync(new Reading { Id = Guid.NewGuid(), Sensor = "a" });
        var batch = new List<Reading>
        {
            new() { Sensor = "x" },
            new() { Sensor = "a" },
            new() { Sensor = "b" }
        };

        // Act
        var result = await importer.ImportAsync(batch, CancellationToken.None);

        // Assert
        result.ShouldBeFailure();
        result.HasError<BulkWritePartialFailureError>().ShouldBeTrue();
        importer.FailedIndexes.ShouldBe([1]);
    }

    [Test]
    public async Task FailedIndexes_AreClearedByTheNextSuccessfulImport()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var importer = ActivatorUtilities.CreateInstance<ReadingImporter>(scope.ServiceProvider);
        var collections = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var resolved = await collections.GetCollectionAsync<Reading>(
            new Abstractions.Keys.MongoDocumentBindingKey("readings"));
        var collection = resolved.Value;
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<Reading>(
            Builders<Reading>.IndexKeys.Ascending(x => x.Sensor),
            new CreateIndexOptions { Unique = true, Name = "sensor_unique" }));
        await collection.InsertOneAsync(new Reading { Id = Guid.NewGuid(), Sensor = "a" });
        await importer.ImportAsync([new Reading { Sensor = "a" }], CancellationToken.None);

        // Act
        var succeeded = await importer.ImportAsync([new Reading { Sensor = "c" }], CancellationToken.None);

        // Assert
        succeeded.ShouldBeSuccess();
        importer.FailedIndexes.ShouldBeEmpty();
    }
}