using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Bson;
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
        // Arrange
        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        // Act
        var result = await _repository.StoreAsync(entity);
        var result2 = result.IsSuccess
            ? await _repository.GetAsync(result.ValueOrDefault.Id)
            : null;
        var createdEntity = result2?.ValueOrDefault;

        // Assert
        result.ShouldBeSuccess();
        result2!.ShouldBeSuccess();
        createdEntity.ShouldNotBeNull();
        createdEntity.Id.ShouldNotBe(Guid.Empty);
        createdEntity.ETag.ShouldNotBe(0);
        createdEntity.Name.ShouldBe(entity.Name);
        createdEntity.Value.ShouldBe(entity.Value);
    }

    [Test]
    public async Task GenericRepository_Store_UpdateDocument()
    {
        // Arrange
        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();
        var createResult = await _repository.StoreAsync(entity);
        var entityToUpdate = createResult.IsSuccess
            ? (await _repository.GetAsync(createResult.ValueOrDefault.Id)).ValueOrDefault
            : null;
        if (entityToUpdate is not null)
        {
            entityToUpdate.Name = "UpdatedName";
            entityToUpdate.Value = "UpdatedValue";
        }

        // Act
        var updateResult = entityToUpdate is not null
            ? await _repository.StoreAsync(entityToUpdate)
            : null;
        var updatedEntity = updateResult is { IsSuccess: true }
            ? (await _repository.GetAsync(createResult.ValueOrDefault.Id)).ValueOrDefault
            : null;

        // Assert
        createResult.ShouldBeSuccess();
        updateResult!.ShouldBeSuccess();
        updatedEntity!.ETag.ShouldNotBe(createResult.ValueOrDefault.ETag);
        updatedEntity.Name.ShouldBe("UpdatedName");
        updatedEntity.Value.ShouldBe("UpdatedValue");
    }

    [Test]
    public async Task GenericRepository_Store_UpdateDocument_WithWrongEtag_ShouldFail()
    {
        // Arrange
        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();
        var createResult = await _repository.StoreAsync(entity);
        var entityToUpdate = createResult.IsSuccess
            ? (await _repository.GetAsync(createResult.ValueOrDefault.Id)).ValueOrDefault
            : null;
        if (entityToUpdate is not null)
        {
            entityToUpdate.Name = "UpdatedName";
            entityToUpdate.ETag = 123;
        }

        // Act
        var updateResult = entityToUpdate is not null
            ? await _repository.StoreAsync(entityToUpdate)
            : null;
        var unchanged = updateResult is { IsFailed: true }
            ? (await _repository.GetAsync(createResult.ValueOrDefault.Id)).ValueOrDefault
            : null;

        // Assert
        createResult.ShouldBeSuccess();
        updateResult!.ShouldBeFailure();
        unchanged!.ETag.ShouldBe(createResult.ValueOrDefault.ETag);
        unchanged.Name.ShouldBe(createResult.ValueOrDefault.Name);
    }

    [Test]
    public async Task GenericRepository_Get_DocumentList()
    {
        // Arrange
        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany()
            .ToArray();
        var stored = new List<Result<TestEntity1>>();
        foreach (var entity in entities)
        {
            stored.Add(await _repository.StoreAsync(entity));
        }

        // Act
        var result = stored.TrueForAll(static item => item.IsSuccess)
            ? await _repository.GetListAsync()
            : null;

        // Assert
        foreach (var item in stored)
        {
            item.ShouldBeSuccess();
        }

        result!.ShouldBeSuccess();
        foreach (var entity in entities)
        {
            result!.ValueOrDefault.ShouldContain(x => x.Id == entity.Id);
        }
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GenericRepository_Delete_ShouldBeDeletedFromDb(bool isSoftDelete)
    {
        // Arrange
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

        // Act
        var deleteResult = createResult.IsSuccess
            ? await repository.DeleteAsync(
                createResult.ValueOrDefault.Id,
                createResult.ValueOrDefault.ETag)
            : null;
        var factory = scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();
        var collectionResult = deleteResult is { IsSuccess: true }
            ? await factory.GetCollectionAsync<TestEntity1>(new MongoDocumentBindingKey("e1"))
            : null;
        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, createResult.ValueOrDefault.Id);
        var entityFromDb = collectionResult is { IsSuccess: true }
            ? await collectionResult.Value.Find(filter).FirstOrDefaultAsync()
            : null;

        // Assert
        createResult.ShouldBeSuccess();
        deleteResult!.ShouldBeSuccess();
        collectionResult!.ShouldBeSuccess();
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
        // Arrange
        var entity = Fixture.Build<DerivedTestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        // Act
        var createResult = await _repository.StoreAsync(entity);
        var derivedEntityResult = createResult.IsSuccess
            ? await _repository.GetAsync<TestEntity1, DerivedTestEntity1>(createResult.ValueOrDefault.Id)
            : null;

        // Assert
        createResult.ShouldBeSuccess();
        derivedEntityResult!.ShouldBeSuccess();
        derivedEntityResult!.ValueOrDefault!.SomeValue.ShouldBe(entity.SomeValue);
    }

    [BsonDiscriminator(nameof(TestEntity1))]
    [BsonKnownTypes(typeof(DerivedTestEntity1))]
    public class TestEntity1 : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
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

    public class MinimalEntity : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }

    public class ObjectIdEntity : IDocumentEntity<ObjectId>
    {
        public ObjectId Id { get; set; }
        public string? Name { get; set; }
    }
}

public class FlexibleDocumentEntityRepositoryTests : BaseIntegrationTests
{
    [Test]
    public async Task MinimalEntity_StoreGetDelete_WorksWithoutPolicies()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("MinimalDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.MinimalEntity>("minimal", d => d
                    .WithCollectionName("minimalEntities"));
            }));

        using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.MinimalEntity>>();
        var entity = new GenericRepositoryTests.MinimalEntity { Name = "min" };

        // Act
        var store = await repository.StoreAsync(entity);
        var get = store.IsSuccess
            ? await repository.GetAsync(
                Builders<GenericRepositoryTests.MinimalEntity>.Filter.Eq(x => x.Id, store.Value.Id))
            : null;
        if (store.IsSuccess)
        {
            store.Value.Name = "updated";
        }

        var update = store.IsSuccess
            ? await repository.StoreAsync(store.Value)
            : null;
        var deleted = update is { IsSuccess: true }
            ? await repository.DeleteAsync(
                Builders<GenericRepositoryTests.MinimalEntity>.Filter.Eq(x => x.Id, store.Value.Id))
            : null;

        // Assert
        store.ShouldBeSuccess();
        store.Value.Id.ShouldNotBe(Guid.Empty);
        store.Value.Id.Version.ShouldBe(4);
        var loaded = get!;
        var updated = update!;
        var removed = deleted!;
        loaded.ShouldBeSuccess();
        loaded.Value!.Name.ShouldBe("min");
        updated.ShouldBeSuccess();
        updated.Value.Name.ShouldBe("updated");
        removed.ShouldBeSuccess();
        removed.Value.ShouldBeTrue();
    }

    [Test]
    public async Task GuidIdGeneration_SequentialVersion7_ProducesUuidV7()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("Uuid7DB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.MinimalEntity>("uuid7", d => d
                    .WithCollectionName("uuid7Entities")
                    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
            }));

        using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.MinimalEntity>>();

        // Act
        var store = await repository.StoreAsync(new GenericRepositoryTests.MinimalEntity { Name = "v7" });

        // Assert
        store.ShouldBeSuccess();
        store.Value.Id.Version.ShouldBe(7);
    }

    [Test]
    public async Task ObjectIdEntity_StoreAndGet_Works()
    {
        // Arrange
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("ObjectIdDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<GenericRepositoryTests.ObjectIdEntity>("oids", d => d
                    .WithCollectionName("objectIdEntities"));
            }));

        using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<GenericRepositoryTests.ObjectIdEntity>>();

        // Act
        var store = await repository.StoreAsync(new GenericRepositoryTests.ObjectIdEntity { Name = "oid" });
        var get = store.IsSuccess
            ? await repository.GetAsync(
                Builders<GenericRepositoryTests.ObjectIdEntity>.Filter.Eq(x => x.Id, store.Value.Id))
            : null;

        // Assert
        store.ShouldBeSuccess();
        store.Value.Id.ShouldNotBe(ObjectId.Empty);
        get!.ShouldBeSuccess();
        get!.Value!.Name.ShouldBe("oid");
    }
}