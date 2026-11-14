using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[Category("M3Matrix")]
public class GenericBulkRepositoryCorrectnessTests : BaseIntegrationTests
{
    private static readonly Fixture Fixture = new();

    [Test]
    public async Task BulkStoreRange_Empty_Succeeds()
    {
        // Arrange
        var (bulk, _, _) = CreateBulk("bulk-empty");

        // Act
        var result = await bulk.BulkStoreRangeAsync(Array.Empty<GenericBulkRepositoryTests.TestEntity1>());

        // Assert
        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(0);
    }

    [Test]
    public async Task BulkStore_NoOpUpdate_Succeeds()
    {
        // Arrange
        var (bulk, _, _) = CreateBulk("bulk-noop");
        var entity = CreateEntity();
        var stored = await bulk.BulkStoreAsync([entity]);

        // Act
        var again = await bulk.BulkStoreAsync([entity]);

        // Assert
        stored.ShouldBeSuccess();
        again.ShouldBeSuccess();
        again.Value[0].Name.ShouldBe(entity.Name);
    }

    [Test]
    public async Task BulkStore_MixedCreateAndUpdate()
    {
        // Arrange
        var (bulk, repository, _) = CreateBulk("bulk-mixed");
        var existing = CreateEntity();
        var stored = await bulk.BulkStoreAsync([existing]);
        existing.Name = "updated";
        var created = CreateEntity();

        // Act
        var result = await bulk.BulkStoreAsync([existing, created]);

        // Assert
        stored.ShouldBeSuccess();
        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(2);

        var loadedExisting = await repository.GetAsync(existing.Id);
        loadedExisting.Value.Name.ShouldBe("updated");
        loadedExisting.Value.ETag.ShouldBe(existing.ETag);
        (await repository.GetAsync(created.Id)).Value.Name.ShouldBe(created.Name);
    }

    [Test]
    public async Task BulkStore_StaleETag_DoesNotReportSuccess()
    {
        // Arrange
        var (bulk, repository, _) = CreateBulk("bulk-stale-etag");
        var entity = CreateEntity();
        var stored = await bulk.BulkStoreAsync([entity]);
        var persistedEtag = entity.ETag;
        var persistedName = entity.Name;

        entity.Name = "should-not-persist";
        entity.ETag = 999;

        // Act
        var result = await bulk.BulkStoreAsync([entity]);

        // Assert
        stored.ShouldBeSuccess();
        result.ShouldBeFailure();
        result.ShouldHaveError<BulkWritePartialFailureError>();
        entity.ETag.ShouldBe(999);

        var loaded = await repository.GetAsync(entity.Id);
        loaded.Value.Name.ShouldBe(persistedName);
        loaded.Value.ETag.ShouldBe(persistedEtag);
    }

    [Test]
    public async Task BulkStore_UnorderedPartialFailure_KeepsSuccessfulEntitiesSynchronized()
    {
        // Arrange
        var (bulk, repository, _) = CreateBulk("bulk-partial");
        var first = CreateEntity();
        var stored = await bulk.BulkStoreAsync([first]);
        var duplicateInsert = CreateEntity();
        duplicateInsert.Id = first.Id;
        duplicateInsert.ETag = 0;
        var fresh = CreateEntity();

        // Act
        var result = await bulk.BulkStoreAsync(
            [duplicateInsert, fresh],
            new MongoBulkWriteOptions { IsOrdered = false });

        // Assert
        stored.ShouldBeSuccess();
        result.ShouldBeFailure();
        var error = result.ShouldHaveError<BulkWritePartialFailureError>();
        error.Items.ShouldContain(item => item.Index == 0 && !item.Succeeded);
        error.Items.ShouldContain(item => item.Index == 1 && item.Succeeded);
        duplicateInsert.ETag.ShouldBe(0);
        fresh.ETag.ShouldNotBe(0);

        var loadedFresh = await repository.GetAsync(fresh.Id);
        loadedFresh.ShouldBeSuccess();
        loadedFresh.Value.ETag.ShouldBe(fresh.ETag);
        loadedFresh.Value.Name.ShouldBe(fresh.Name);
    }

    [Test]
    public async Task BulkStore_OrderedFailure_DoesNotProcessLaterItems()
    {
        // Arrange
        var (bulk, repository, _) = CreateBulk("bulk-ordered");
        var first = CreateEntity();
        var stored = await bulk.BulkStoreAsync([first]);
        var duplicateInsert = CreateEntity();
        duplicateInsert.Id = first.Id;
        duplicateInsert.ETag = 0;
        var later = CreateEntity();

        // Act
        var result = await bulk.BulkStoreAsync(
            [duplicateInsert, later],
            new MongoBulkWriteOptions { IsOrdered = true });

        // Assert
        stored.ShouldBeSuccess();
        result.ShouldBeFailure();
        var error = result.ShouldHaveError<BulkWritePartialFailureError>();
        error.Items.ShouldContain(item => item.Index == 0 && !item.Succeeded);
        error.Items.ShouldContain(item => item.Index == 1 && !item.Succeeded);
        later.ETag.ShouldBe(0);

        var missing = await repository.GetAsync(later.Id);
        missing.ShouldBeFailure();
        missing.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task BulkStore_LaterChunkFailure_ReportsOriginalIndicesAndKeepsPriorChunkSynchronized()
    {
        // Arrange
        var (bulk, repository, _) = CreateBulk("bulk-chunk-fail");
        var first = CreateEntity();
        var duplicate = CreateEntity();
        var stored = await bulk.BulkStoreAsync([duplicate]);
        duplicate.ETag = 0;

        var colliding = CreateEntity();
        colliding.Id = duplicate.Id;
        colliding.ETag = 0;

        // Act
        var result = await bulk.BulkStoreAsync(
            [first, colliding],
            new MongoBulkWriteOptions { MaxOperationsPerBatch = 1, IsOrdered = true });

        // Assert
        stored.ShouldBeSuccess();
        result.ShouldBeFailure();
        var error = result.ShouldHaveError<BulkWritePartialFailureError>();
        error.Items.ShouldContain(item => item.Index == 1 && !item.Succeeded);
        first.ETag.ShouldNotBe(0);

        var loadedFirst = await repository.GetAsync(first.Id);
        loadedFirst.ShouldBeSuccess();
        loadedFirst.Value.ETag.ShouldBe(first.ETag);
        loadedFirst.Value.Name.ShouldBe(first.Name);
    }

    [Test]
    public async Task BulkStore_ChunksByMaxOperations()
    {
        // Arrange
        var (bulk, repository, _) = CreateBulk("bulk-chunk");
        var entities = Enumerable.Range(0, 5).Select(_ => CreateEntity()).ToArray();

        // Act
        var result = await bulk.BulkStoreAsync(
            entities,
            new MongoBulkWriteOptions { MaxOperationsPerBatch = 2 });

        // Assert
        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(5);
        foreach (var entity in entities)
        {
            var loaded = await repository.GetAsync(entity.Id);
            loaded.Value.Name.ShouldBe(entity.Name);
            loaded.Value.ETag.ShouldBe(entity.ETag);
        }
    }

    [Test]
    public async Task BulkDelete_NoMatches_ReturnsNotFound()
    {
        // Arrange
        var (bulk, _, _) = CreateBulk("bulk-delete-missing");

        // Act
        var result = await bulk.BulkDeleteAsync(x => x.Id == Guid.NewGuid());

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task BulkDelete_SoftDelete_UpdatesETagAndAudit()
    {
        // Arrange
        var (bulk, repository, factory) = CreateBulk("bulk-soft-delete", enableSoftDelete: true);
        var entity = CreateEntity();
        var stored = await bulk.BulkStoreAsync([entity]);
        var previousEtag = entity.ETag;

        // Act
        var deleted = await bulk.BulkDeleteAsync(x => x.Id == entity.Id);

        // Assert
        stored.ShouldBeSuccess();
        deleted.ShouldBeSuccess();
        (await repository.GetAsync(entity.Id)).ShouldHaveError<DocumentNotFoundError>();

        var collection = (await factory.GetCollectionAsync<GenericBulkRepositoryTests.TestEntity1>(
            new MongoDocumentBindingKey("e1"))).Value;
        var raw = await collection.Find(
                Builders<GenericBulkRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, entity.Id))
            .FirstAsync();
        raw.IsDeleted.ShouldBeTrue();
        raw.ETag.ShouldNotBe(previousEtag);
        raw.UpdatedAt.ShouldBeGreaterThan(entity.CreatedAt);
    }

    private (IGenericBulkRepository<GenericBulkRepositoryTests.TestEntity1> Bulk,
        IGenericRepository<GenericBulkRepositoryTests.TestEntity1> Repository,
        IMongoDbCollectionFactory Factory) CreateBulk(string collection, bool enableSoftDelete = false)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("BulkCorrectnessDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericBulkRepositoryTests.TestEntity1>("e1", d =>
                {
                    d.WithCollectionName(collection).WithBulkRepository();
                    if (enableSoftDelete)
                    {
                        d.WithSoftDelete();
                    }
                });
            }));

        var provider = AcceptanceServiceProviderFactory.Create(services);
        var scope = provider.CreateScope().ServiceProvider;
        return (
            scope.GetRequiredService<IGenericBulkRepository<GenericBulkRepositoryTests.TestEntity1>>(),
            scope.GetRequiredService<IGenericRepository<GenericBulkRepositoryTests.TestEntity1>>(),
            scope.GetRequiredService<IMongoDbCollectionFactory>());
    }

    private static GenericBulkRepositoryTests.TestEntity1 CreateEntity()
        => Fixture.Build<GenericBulkRepositoryTests.TestEntity1>()
            .With(x => x.IsDeleted, false)
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Create();
}