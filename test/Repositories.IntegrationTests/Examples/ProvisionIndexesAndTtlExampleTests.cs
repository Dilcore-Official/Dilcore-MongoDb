using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Provisioning;
using Dilcore.MongoDB.Extensions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests.Examples;

/// <summary>Proves docs/guides/provision-indexes-and-ttl.md.</summary>
public class ProvisionIndexesAndTtlExampleTests : ExampleIntegrationTests
{
    public sealed class Session : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }

    public static class Provisioning
    {
        public static async Task ProvisionAsync(
            IServiceProvider services,
            bool allowIndexRemoval,
            CancellationToken cancellationToken = default)
        {
            using var scope = services.CreateScope();
            var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

            var preview = await provisioner.DryRunAsync(cancellationToken);
            if (preview.IsFailed)
            {
                throw new InvalidOperationException("MongoDB provisioning preview failed.");
            }

            var wouldRemove = preview.Value.Steps.Any(step => step.Action == "would-remove");
            if (wouldRemove && !allowIndexRemoval)
            {
                throw new InvalidOperationException("Review the index removal before applying.");
            }

            var report = await provisioner.ApplyAsync(cancellationToken);
            if (report.IsFailed)
            {
                throw new InvalidOperationException("MongoDB provisioning failed.");
            }
        }
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("auth", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<Session>("sessions", d => d
                    .WithCollectionName("sessions")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
                    .WithIndexes(new CreateIndexModel<Session>(
                        Builders<Session>.IndexKeys.Ascending(x => x.UserName),
                        new CreateIndexOptions { Name = "sessions_user", Unique = true }))

                    .WithCollectionItemsTimeToLive(TimeSpan.Zero, x => x.ExpiresAt));
            }));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    private async Task<List<BsonDocument>> ServerIndexesAsync()
    {
        using var client = new MongoClient(connectionString);
        var collection = client.GetDatabase("auth").GetCollection<BsonDocument>("sessions");
        var indexes = await collection.Indexes.ListAsync();

        return await indexes.ToListAsync();
    }

    [Test]
    public async Task DryRun_ChangesNothing_ApplyCreatesIndexes_ReapplySkips()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        // Act
        var preview = await provisioner.DryRunAsync();
        var indexesAfterPreview = await ServerIndexesAsync();
        await Provisioning.ProvisionAsync(provider, allowIndexRemoval: false, CancellationToken.None);
        var indexesAfterApply = await ServerIndexesAsync();
        var reapply = await provisioner.ApplyAsync();

        // Assert
        preview.Value.Applied.ShouldBeFalse();
        preview.Value.Steps.ShouldContain(s => s.Action == "would-create");
        indexesAfterPreview.ShouldBeEmpty();
        indexesAfterApply.Select(i => i["name"].AsString).ShouldBe(["_id_", "sessions_user", "sessions_ttl"], ignoreOrder: true);
        indexesAfterApply.Single(i => i["name"] == "sessions_ttl")["expireAfterSeconds"].ToInt32().ShouldBe(0);
        reapply.Value.Steps.ShouldNotContain(s => s.Action == "create");
    }

    [Test]
    public async Task Apply_RemovesIndexesNotDeclaredOnBinding()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
        using var client = new MongoClient(connectionString);
        var collection = client.GetDatabase("auth").GetCollection<BsonDocument>("sessions");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("other"), new CreateIndexOptions { Name = "stray" }));

        // Act
        var dryRun = await provisioner.DryRunAsync();
        await Provisioning.ProvisionAsync(provider, allowIndexRemoval: true, CancellationToken.None);
        var indexes = await ServerIndexesAsync();

        // Assert
        dryRun.Value.Steps.ShouldContain(s => s.Action == "would-remove");
        indexes.ShouldNotContain(i => i["name"] == "stray");
    }

    [Test]
    public async Task Apply_WithoutApproval_StopsWhenPreviewWouldRemoveAnIndex()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var client = new MongoClient(connectionString);
        var collection = client.GetDatabase("auth").GetCollection<BsonDocument>("sessions");
        await collection.Indexes.CreateOneAsync(new CreateIndexModel<BsonDocument>(
            Builders<BsonDocument>.IndexKeys.Ascending("other"), new CreateIndexOptions { Name = "stray" }));

        // Act & Assert
        var act = () => Provisioning.ProvisionAsync(provider, allowIndexRemoval: false, CancellationToken.None);
        await Should.ThrowAsync<InvalidOperationException>(act);
    }

    [Test]
    public async Task Apply_WithDuplicateDataForUniqueIndex_FailsClosed()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var client = new MongoClient(connectionString);
        var collection = client.GetDatabase("auth").GetCollection<BsonDocument>("sessions");
        await collection.InsertManyAsync([new BsonDocument("UserName", "a"), new BsonDocument("UserName", "a")]);

        // Act & Assert
        var act = () => Provisioning.ProvisionAsync(provider, allowIndexRemoval: false, CancellationToken.None);
        await Should.ThrowAsync<InvalidOperationException>(act);
    }
}