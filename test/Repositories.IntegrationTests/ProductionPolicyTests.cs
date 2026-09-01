using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Repositories;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[Category("M3Matrix")]
public class ProductionPolicyTests : BaseIntegrationTests
{
    [Test]
    public async Task GetPageAsync_ReturnsStableKeysetPages()
    {
        var repository = CreateRepository("pages", enableSoftDelete: false);
        for (var i = 0; i < 5; i++)
        {
            (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = $"n{i}" }))
                .ShouldBeSuccess();
        }

        var first = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Id),
            PageSize = 2
        });
        first.ShouldBeSuccess();
        first.Value.Items.Count.ShouldBe(2);
        first.Value.HasMore.ShouldBeTrue();
        first.Value.NextCursor.ShouldNotBeNull();

        var second = await repository.GetPageAsync(new KeysetPageRequest<GenericRepositoryTests.TestEntity1>
        {
            Filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Id),
            PageSize = 2,
            Cursor = first.Value.NextCursor
        });
        second.ShouldBeSuccess();
        second.Value.Items.Count.ShouldBe(2);
        second.Value.Items[0].Id.ShouldNotBe(first.Value.Items[0].Id);
        second.Value.Items[0].Id.ShouldNotBe(first.Value.Items[1].Id);
    }

    [Test]
    public async Task RestoreAndPurge_SoftDeletedDocuments()
    {
        var repository = CreateRepository("restore-purge", enableSoftDelete: true);
        var stored = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "gone" });
        stored.ShouldBeSuccess();
        (await repository.DeleteAsync(stored.Value.Id, stored.Value.ETag)).ShouldBeSuccess();

        var filter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id);
        (await repository.GetAsync(filter)).ShouldBeFailure();

        (await repository.RestoreAsync(filter)).ShouldBeSuccess();
        (await repository.GetAsync(filter)).ShouldBeSuccess();

        (await repository.DeleteAsync(stored.Value.Id, (await repository.GetAsync(filter)).Value.ETag)).ShouldBeSuccess();
        var purged = await repository.PurgeAsync(filter);
        purged.ShouldBeSuccess();
        purged.Value.ShouldBe(1);
        (await repository.RestoreAsync(filter)).ShouldBeFailure();
    }

    [Test]
    public async Task DuplicateKey_MapsToTypedError()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("DupDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.TestEntity1>("e1", d => d
                    .WithCollectionName("dup-names")
                    .WithIndexes(new CreateIndexModel<GenericRepositoryTests.TestEntity1>(
                        Builders<GenericRepositoryTests.TestEntity1>.IndexKeys.Ascending(x => x.Name),
                        new CreateIndexOptions { Unique = true })));
            }));

        var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var repository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.TestEntity1>>();

        (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(
            new Abstractions.Keys.MongoDocumentBindingKey("e1"))).ShouldBeSuccess();

        (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "unique-name" })).ShouldBeSuccess();
        var duplicate = await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = "unique-name" });
        duplicate.ShouldBeFailure();
        duplicate.ShouldHaveError<DuplicateKeyError>();
    }

    [Test]
    public async Task GetList_HonorsFindOptionsSortAndLimit()
    {
        var repository = CreateRepository("find-options", enableSoftDelete: false);
        foreach (var name in new[] { "c", "a", "b" })
        {
            (await repository.StoreAsync(new GenericRepositoryTests.TestEntity1 { Name = name })).ShouldBeSuccess();
        }

        var result = await repository.GetListAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Empty,
            new FindOptions<GenericRepositoryTests.TestEntity1, GenericRepositoryTests.TestEntity1>
            {
                Sort = Builders<GenericRepositoryTests.TestEntity1>.Sort.Ascending(x => x.Name),
                Limit = 2
            });
        result.ShouldBeSuccess();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Name.ShouldBe("a");
        result.Value[1].Name.ShouldBe("b");
    }

    private IGenericRepository<GenericRepositoryTests.TestEntity1> CreateRepository(string collection, bool enableSoftDelete)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("PolicyDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.TestEntity1>("e1", d =>
                {
                    d.WithCollectionName(collection);
                    if (enableSoftDelete)
                    {
                        d.WithSoftDelete();
                    }
                });
            }));

        return AcceptanceServiceProviderFactory.Create(services)
            .CreateScope()
            .ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.TestEntity1>>();
    }
}
