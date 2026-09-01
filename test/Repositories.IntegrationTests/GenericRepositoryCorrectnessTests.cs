using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Repositories;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[Category("M3Matrix")]
public class GenericRepositoryCorrectnessTests : BaseIntegrationTests
{
    private static readonly Fixture Fixture = new();

    [Test]
    public async Task CountAndHasAny_ExcludeSoftDeletedDocuments()
    {
        var (repository, _) = CreateRepository("soft-count", enableSoftDelete: true);

        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var idFilter = Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id);
        (await repository.CountAsync(idFilter)).Value.ShouldBe(1);
        (await repository.HasAnyAsync(idFilter)).Value.ShouldBeTrue();

        (await repository.DeleteAsync(stored.Value.Id, stored.Value.ETag)).ShouldBeSuccess();

        (await repository.CountAsync(idFilter)).Value.ShouldBe(0);
        (await repository.HasAnyAsync(idFilter)).Value.ShouldBeFalse();
        (await repository.GetListAsync(idFilter)).Value.Count.ShouldBe(0);
        var missing = await repository.GetAsync(idFilter);
        missing.ShouldBeFailure();
        missing.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task GetAsync_MissingDocument_ReturnsNotFound()
    {
        var (repository, _) = CreateRepository("not-found", enableSoftDelete: false);
        var result = await repository.GetAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, Guid.NewGuid()));
        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task FailedEtagUpdate_DoesNotMutateEntity()
    {
        var (repository, _) = CreateRepository("etag-restore", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var entity = stored.Value;
        var originalEtag = entity.ETag;
        var originalUpdated = entity.UpdatedAt;
        var originalName = entity.Name;
        entity.Name = "mutated";
        entity.ETag = 123;

        var update = await repository.StoreAsync(entity);
        update.ShouldBeFailure();
        update.ShouldHaveError<ConcurrencyConflictError>();
        entity.ETag.ShouldBe(123);
        entity.UpdatedAt.ShouldBe(originalUpdated);
        entity.Name.ShouldBe("mutated");

        var persisted = await repository.GetAsync(entity.Id);
        persisted.ShouldBeSuccess();
        persisted.Value.ETag.ShouldBe(originalEtag);
        persisted.Value.Name.ShouldBe(originalName);
    }

    [Test]
    public async Task PatchAsync_DoesNotClobberUnrelatedFields()
    {
        var (repository, factory) = CreateRepository("patch", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();

        var collection = (await factory.GetCollectionAsync<GenericRepositoryTests.TestEntity1>(
            new MongoDocumentBindingKey("e1"))).Value;
        await collection.UpdateOneAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id),
            Builders<GenericRepositoryTests.TestEntity1>.Update.Set(x => x.Value, "concurrent"));

        var patched = await repository.PatchAsync(
            Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.Id, stored.Value.Id)
                & Builders<GenericRepositoryTests.TestEntity1>.Filter.Eq(x => x.ETag, stored.Value.ETag),
            Builders<GenericRepositoryTests.TestEntity1>.Update.Set(x => x.Name, "from-client"));
        patched.ShouldBeSuccess();
        patched.Value.Name.ShouldBe("from-client");
        patched.Value.Value.ShouldBe("concurrent");
    }

    [Test]
    public async Task ReplaceAsync_ReplacesEntireDocument()
    {
        var (repository, _) = CreateRepository("replace", enableSoftDelete: false);
        var stored = await repository.StoreAsync(CreateEntity());
        stored.ShouldBeSuccess();
        stored.Value.Name = "replaced";
        stored.Value.Value = null;
        var replaced = await repository.ReplaceAsync(stored.Value);
        replaced.ShouldBeSuccess();
        var loaded = await repository.GetAsync(stored.Value.Id);
        loaded.Value.Name.ShouldBe("replaced");
        loaded.Value.Value.ShouldBeNull();
    }

    private (IGenericRepository<GenericRepositoryTests.TestEntity1> Repository, IMongoDbCollectionFactory Factory)
        CreateRepository(string collection, bool enableSoftDelete)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("CorrectnessDB", db =>
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

        var provider = AcceptanceServiceProviderFactory.Create(services);
        var scope = provider.CreateScope();
        return (
            scope.ServiceProvider.GetRequiredService<IGenericRepository<GenericRepositoryTests.TestEntity1>>(),
            scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>());
    }

    private static GenericRepositoryTests.TestEntity1 CreateEntity()
        => Fixture.Build<GenericRepositoryTests.TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();
}
