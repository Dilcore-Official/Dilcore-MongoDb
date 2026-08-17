using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Repositories;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

public class GenericRepositoryTests : BaseIntegrationTests
{
    private static readonly Fixture Fixture = new();
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private IGenericRepository<TestEntity1> _repository = null!;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("TestDB1", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity1>("e1", d => d
                    .WithCollectionName("testEntity1"));
            }));

        _provider = AcceptanceServiceProviderFactory.Create(services);
        _scope = _provider.CreateScope();
        _repository = _scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity1>>();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Test]
    public async Task GenericRepository_Store_CreateDocument()
    {
        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        var result = await _repository.StoreAsync(entity);
        result.ShouldBeSuccess();

        var result2 = await _repository.GetAsync(result.ValueOrDefault.Id);
        result2.ShouldBeSuccess();

        var createdEntity = result2.ValueOrDefault;
        createdEntity.ShouldNotBeNull();
        createdEntity.Id.ShouldNotBe(Guid.Empty);
        createdEntity.ETag.ShouldNotBe(0);
        createdEntity.Name.ShouldBe(entity.Name);
        createdEntity.Value.ShouldBe(entity.Value);
    }

    [Test]
    public async Task GenericRepository_Store_UpdateDocument()
    {
        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        var createResult = await _repository.StoreAsync(entity);
        createResult.ShouldBeSuccess();

        var entityToUpdate = (await _repository.GetAsync(createResult.ValueOrDefault.Id)).ValueOrDefault!;
        entityToUpdate.Name = "UpdatedName";
        entityToUpdate.Value = "UpdatedValue";

        var updateResult = await _repository.StoreAsync(entityToUpdate);
        updateResult.ShouldBeSuccess();

        var updatedEntity = (await _repository.GetAsync(createResult.ValueOrDefault.Id)).ValueOrDefault!;
        updatedEntity.ETag.ShouldNotBe(createResult.ValueOrDefault.ETag);
        updatedEntity.Name.ShouldBe("UpdatedName");
        updatedEntity.Value.ShouldBe("UpdatedValue");
    }

    [Test]
    public async Task GenericRepository_Store_UpdateDocument_WithWrongEtag_ShouldFail()
    {
        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        var createResult = await _repository.StoreAsync(entity);
        createResult.ShouldBeSuccess();

        var entityToUpdate = (await _repository.GetAsync(createResult.ValueOrDefault.Id)).ValueOrDefault!;
        entityToUpdate.Name = "UpdatedName";
        entityToUpdate.ETag = 123;

        var updateResult = await _repository.StoreAsync(entityToUpdate);
        updateResult.ShouldBeFailure();

        var unchanged = (await _repository.GetAsync(createResult.ValueOrDefault.Id)).ValueOrDefault!;
        unchanged.ETag.ShouldBe(createResult.ValueOrDefault.ETag);
        unchanged.Name.ShouldBe(createResult.ValueOrDefault.Name);
    }

    [Test]
    public async Task GenericRepository_Get_DocumentList()
    {
        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany()
            .ToArray();

        foreach (var entity in entities)
        {
            (await _repository.StoreAsync(entity)).ShouldBeSuccess();
        }

        var result = await _repository.GetListAsync();
        result.ShouldBeSuccess();
        foreach (var entity in entities)
        {
            result.ValueOrDefault.ShouldContain(x => x.Id == entity.Id);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GenericRepository_Delete_ShouldBeDeletedFromDb(bool isSoftDelete)
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("TestDB1", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity1>("e1", d =>
                {
                    d.WithCollectionName("testEntity1-delete");
                    if (isSoftDelete)
                    {
                        d.WithSoftDelete();
                    }
                });
            }));

        using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity1>>();

        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        var createResult = await repository.StoreAsync(entity);
        createResult.ShouldBeSuccess();

        var deleteResult = await repository.DeleteAsync(
            createResult.ValueOrDefault.Id,
            createResult.ValueOrDefault.ETag);
        deleteResult.ShouldBeSuccess();

        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var collectionResult = await factory.GetCollectionAsync<TestEntity1>(new MongoDocumentBindingKey("e1"));
        collectionResult.ShouldBeSuccess();

        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, createResult.ValueOrDefault.Id);
        var entityFromDb = await collectionResult.Value.Find(filter).FirstOrDefaultAsync();

        if (isSoftDelete)
        {
            entityFromDb.ShouldNotBeNull();
            entityFromDb!.IsDeleted.ShouldBeTrue();
        }
        else
        {
            entityFromDb.ShouldBeNull();
        }
    }

    [Test]
    public async Task GenericRepository_Store_DerivedEntityShouldBeSaved()
    {
        var entity = Fixture.Build<DerivedTestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        var createResult = await _repository.StoreAsync(entity);
        createResult.ShouldBeSuccess();

        var derivedEntityResult = await _repository.GetAsync<TestEntity1, DerivedTestEntity1>(createResult.ValueOrDefault.Id);
        derivedEntityResult.ShouldBeSuccess();
        derivedEntityResult.ValueOrDefault!.SomeValue.ShouldBe(entity.SomeValue);
    }

    [BsonDiscriminator(nameof(TestEntity1))]
    [BsonKnownTypes(typeof(DerivedTestEntity1))]
    public class TestEntity1 : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    [BsonDiscriminator(nameof(DerivedTestEntity1))]
    public class DerivedTestEntity1 : TestEntity1
    {
        public string? SomeValue { get; set; }
    }
}
