using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Repositories;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[Category("M3Matrix")]
public class GenericBulkRepositoryCorrectnessTests : BaseIntegrationTests
{
    private static readonly Fixture Fixture = new();

    [Test]
    public async Task BulkStoreRange_Empty_Succeeds()
    {
        var bulk = CreateBulk("bulk-empty");
        var result = await bulk.BulkStoreRangeAsync(Array.Empty<GenericBulkRepositoryTests.TestEntity1>());
        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(0);
    }

    [Test]
    public async Task BulkStore_NoOpUpdate_Succeeds()
    {
        var bulk = CreateBulk("bulk-noop");
        var entity = CreateEntity();
        (await bulk.BulkStoreAsync([entity])).ShouldBeSuccess();
        var again = await bulk.BulkStoreAsync([entity]);
        again.ShouldBeSuccess();
    }

    [Test]
    public async Task BulkStore_MixedCreateAndUpdate()
    {
        var bulk = CreateBulk("bulk-mixed");
        var existing = CreateEntity();
        (await bulk.BulkStoreAsync([existing])).ShouldBeSuccess();
        existing.Name = "updated";
        var created = CreateEntity();
        var result = await bulk.BulkStoreAsync([existing, created]);
        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(2);
    }

    [Test]
    public async Task BulkStore_PartialFailure_ReturnsPerItemResults()
    {
        var bulk = CreateBulk("bulk-partial");
        var first = CreateEntity();
        (await bulk.BulkStoreAsync([first])).ShouldBeSuccess();
        var duplicateInsert = CreateEntity();
        duplicateInsert.Id = first.Id;
        duplicateInsert.ETag = 0;
        var fresh = CreateEntity();
        var result = await bulk.BulkStoreAsync(
            [duplicateInsert, fresh],
            new MongoBulkWriteOptions { IsOrdered = false });
        result.ShouldBeFailure();
        result.ShouldHaveError<BulkWritePartialFailureError>();
    }

    [Test]
    public async Task BulkStore_ChunksByMaxOperations()
    {
        var bulk = CreateBulk("bulk-chunk");
        var entities = Enumerable.Range(0, 5).Select(_ => CreateEntity()).ToArray();
        var result = await bulk.BulkStoreAsync(
            entities,
            new MongoBulkWriteOptions { MaxOperationsPerBatch = 2 });
        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(5);
    }

    private IGenericBulkRepository<GenericBulkRepositoryTests.TestEntity1> CreateBulk(string collection)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("BulkCorrectnessDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericBulkRepositoryTests.TestEntity1>("e1", d => d
                    .WithCollectionName(collection)
                    .WithBulkRepository());
            }));

        var provider = AcceptanceServiceProviderFactory.Create(services);
        return provider.CreateScope().ServiceProvider
            .GetRequiredService<IGenericBulkRepository<GenericBulkRepositoryTests.TestEntity1>>();
    }

    private static GenericBulkRepositoryTests.TestEntity1 CreateEntity()
        => Fixture.Build<GenericBulkRepositoryTests.TestEntity1>()
            .With(x => x.IsDeleted, false)
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Create();
}
