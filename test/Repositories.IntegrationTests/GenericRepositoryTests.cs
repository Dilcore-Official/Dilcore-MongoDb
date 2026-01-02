using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Repositories.IntegrationTests;

public class GenericRepositoryTests : BaseIntegrationTests
{
    private static readonly Fixture Fixture = new();
    private IGenericRepository<TestEntity1> _repository;

    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
        {
            builder.AddDatabase("TestDB1",
                db =>
                {
                    db.AddGenericRepository<TestEntity1>(options => options.WithCollectionName("testEntity1")
                        .WithDatabaseName("TestDB1"));
                });
        });

        _repository = services.BuildServiceProvider().GetRequiredService<IGenericRepository<TestEntity1>>();
    }

    [Test]
    public async Task GenericRepository_Store_CreateDocument()
    {
        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        var result = await _repository!.StoreAsync(entity);

        result.ShouldBeSuccess();

        var result2 = await _repository!.GetAsync(result.ValueOrDefault.Id);
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

        var createResult = await _repository!.StoreAsync(entity);

        createResult.ShouldBeSuccess();

        var result2 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        result2.ShouldBeSuccess();

        var entityToUpdate = result2.ValueOrDefault;

        entityToUpdate.Name = "UpdatedName";
        entityToUpdate.Value = "UpdatedValue";

        var updateResult = await _repository!.StoreAsync(entityToUpdate);
        updateResult.ShouldBeSuccess();

        var result3 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        var updatedEntity = result3.ValueOrDefault;

        updatedEntity.Id.ShouldNotBe(Guid.Empty);
        updatedEntity.Id.ShouldBe(createResult.ValueOrDefault.Id);

        updatedEntity.ETag.ShouldNotBe(0);
        updatedEntity.ETag.ShouldNotBe(createResult.ValueOrDefault.ETag);

        updatedEntity.Name.ShouldBe(entityToUpdate.Name);
        updatedEntity.Value.ShouldBe(entityToUpdate.Value);
    }

    [Test]
    public async Task GenericRepository_Store_UpdateDocument_WithWrongEtag_ShouldBeFailed_And_NotBeUpdated()
    {
        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        var createResult = await _repository!.StoreAsync(entity);

        createResult.ShouldBeSuccess();

        var result2 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        result2.ShouldBeSuccess();

        var entityToUpdate = result2.ValueOrDefault;

        entityToUpdate.Name = "UpdatedName";
        entityToUpdate.Value = "UpdatedValue";
        entityToUpdate.ETag = 123;

        var updateResult = await _repository!.StoreAsync(entityToUpdate);
        updateResult.ShouldBeFailure();

        var result3 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        entity = result3.ValueOrDefault;

        entity.ETag.ShouldBe(createResult.ValueOrDefault.ETag);
        entity.Name.ShouldBe(createResult.ValueOrDefault.Name);
        entity.Value.ShouldBe(createResult.ValueOrDefault.Value);
        entity.UpdatedAt.ShouldBe(createResult.ValueOrDefault.UpdatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public async Task GenericRepository_Store_UpdateDocument_WithWrongId_ShouldBeFailed_And_NotBeUpdated()
    {
        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        var createResult = await _repository!.StoreAsync(entity);

        createResult.ShouldBeSuccess();

        var result2 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        result2.ShouldBeSuccess();

        var entityToUpdate = result2.ValueOrDefault;

        entityToUpdate.Name = "UpdatedName";
        entityToUpdate.Value = "UpdatedValue";
        entityToUpdate.Id = Guid.NewGuid();

        var updateResult = await _repository!.StoreAsync(entityToUpdate);
        updateResult.ShouldBeFailure();

        var result3 = await _repository!.GetAsync(entityToUpdate.Id);
        result3.ShouldBeSuccess();
        result3.ValueOrDefault.ShouldBeNull();

        var result4 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        entity = result4.ValueOrDefault;

        entity.ETag.ShouldBe(createResult.ValueOrDefault.ETag);
        entity.Name.ShouldBe(createResult.ValueOrDefault.Name);
        entity.Value.ShouldBe(createResult.ValueOrDefault.Value);
        entity.UpdatedAt.ShouldBe(createResult.ValueOrDefault.UpdatedAt, TimeSpan.FromMilliseconds(1));
    }

    [Test]
    public async Task GenericRepository_Get_DocumentList()
    {
        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany().ToArray();

        foreach (var entity in entities)
        {
            var createResult = await _repository!.StoreAsync(entity);
            createResult.ShouldBeSuccess();
        }

        var result = await _repository!.GetListAsync();
        result.ShouldBeSuccess();

        var entitiesFromDb = result.ValueOrDefault;
        entitiesFromDb.ShouldNotBeEmpty();

        foreach (var entity in entities)
        {
            entitiesFromDb.ShouldContain(x => x.Id == entity.Id);
        }
    }

    [Test]
    public async Task GenericRepository_Get_DocumentList_WithFilter()
    {
        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany().ToArray();

        foreach (var entity in entities)
        {
            var createResult = await _repository!.StoreAsync(entity);
            createResult.ShouldBeSuccess();
        }

        var ids = entities.Select(x => x.Id);

        var result = await _repository!.GetListAsync(x => ids.Contains(x.Id));
        result.ShouldBeSuccess();

        var entitiesFromDb = result.ValueOrDefault;
        entitiesFromDb.ShouldNotBeEmpty();

        entitiesFromDb.Count.ShouldBe(entities.Length);
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GenericRepository_Delete_ShouldBeDeletedFromDb(bool isSoftDelete)
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
        {
            builder.AddDatabase("TestDB1",
                db =>
                {
                    db.AddGenericRepository<TestEntity1>(options =>
                    {
                        options.WithCollectionName("testEntity1")
                            .WithDatabaseName("TestDB1");

                        if (isSoftDelete)
                        {
                            options.WithSoftDelete();
                        }
                    });
                });
        });

        var repository = services.BuildServiceProvider().GetRequiredService<IGenericRepository<TestEntity1>>();

        var entity = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        var createResult = await repository.StoreAsync(entity);
        createResult.ShouldBeSuccess();

        var result = await repository.DeleteAsync(createResult.ValueOrDefault.Id, createResult.ValueOrDefault.ETag);
        result.ShouldBeSuccess();

        var collectionFactory = services.BuildServiceProvider().GetRequiredService<IMongoDbCollectionFactory>();
        var collectionResult = await collectionFactory.GetCollectionAsync<TestEntity1>("TestDB1");
        collectionResult.ShouldBeSuccess();

        var collection = collectionResult.ValueOrDefault;

        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, createResult.ValueOrDefault.Id);

        var entityFromDb = await collection.Find(filter).FirstOrDefaultAsync();

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

        var createResult = await _repository!.StoreAsync(entity);
        createResult.ShouldBeSuccess();

        var derivedEntityResult = await _repository!.GetAsync<TestEntity1, DerivedTestEntity1>(createResult.ValueOrDefault.Id);
        derivedEntityResult.ShouldBeSuccess();

        var derivedEntity = derivedEntityResult.ValueOrDefault;
        derivedEntity.ShouldNotBeNull();

        derivedEntity.SomeValue.ShouldBe(entity.SomeValue);
        derivedEntity.Tags.ShouldBe(entity.Tags);
        derivedEntity.Name.ShouldBe(entity.Name);
        derivedEntity.Value.ShouldBe(entity.Value);
    }

    [Test]
    public async Task GenericRepository_GetList_OnlyDerivedEntitiesShouldBeObtained()
    {
        var derivedEntities = Fixture.Build<DerivedTestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(5)
            .ToArray();

        foreach (var derivedEntity in derivedEntities)
        {
            var createResult = await _repository!.StoreAsync(derivedEntity);
            createResult.ShouldBeSuccess();
        }

        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(5).ToArray();

        foreach (var entity in entities)
        {
            var createResult = await _repository!.StoreAsync(entity);
            createResult.ShouldBeSuccess();
        }

        var ids = entities.Select(x => x.Id).Concat(derivedEntities.Select(x => x.Id)).ToArray();

        var derivedEntityResult =
            await _repository!.GetListAsync<TestEntity1, DerivedTestEntity1>(x => ids.Contains(x.Id));
        derivedEntityResult.ShouldBeSuccess();

        var derivedEntitiesFromDb = derivedEntityResult.ValueOrDefault;
        derivedEntitiesFromDb.ShouldNotBeEmpty();

        derivedEntitiesFromDb.Count.ShouldBe(derivedEntities.Length);

        foreach (var derivedEntity in derivedEntitiesFromDb)
        {
            var entity = derivedEntities.First(x => x.Id == derivedEntity.Id);

            derivedEntity.SomeValue.ShouldBe(entity.SomeValue);
            derivedEntity.Tags.ShouldBe(entity.Tags);
            derivedEntity.Name.ShouldBe(entity.Name);
            derivedEntity.Value.ShouldBe(entity.Value);
        }
    }

    [Test]
    public async Task GenericRepository_GetList_SpecificEntitiesShouldBeCastedToDerived()
    {
        var derivedEntities = Fixture.Build<DerivedTestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(5)
            .ToArray();

        foreach (var derivedEntity in derivedEntities)
        {
            var createResult = await _repository!.StoreAsync(derivedEntity);
            createResult.ShouldBeSuccess();
        }

        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(5);

        foreach (var entity in entities)
        {
            var createResult = await _repository!.StoreAsync(entity);
            createResult.ShouldBeSuccess();
        }

        var entitiesResult = await _repository!.GetListAsync(x => true);
        entitiesResult.ShouldBeSuccess();

        var entitiesFromDb = entitiesResult.ValueOrDefault;
        entitiesFromDb.ShouldNotBeEmpty();

        var derivedEntitiesFromDb = entitiesFromDb.OfType<DerivedTestEntity1>().ToArray();

        foreach (var derivedEntity in derivedEntitiesFromDb)
        {
            derivedEntity.SomeValue.ShouldNotBeEmpty();
            derivedEntity.Tags.ShouldNotBeEmpty();
            derivedEntity.Name.ShouldNotBeEmpty();
            derivedEntity.Value.ShouldNotBeEmpty();
        }
    }

    [Test]
    public async Task GenericRepository_Collection_Should_NotHaveCount()
    {
        // Arrange
        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(5);

        foreach (var entity in entities)
        {
            var createResult = await _repository!.StoreAsync(entity);
            createResult.ShouldBeSuccess();
        }

        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, Guid.NewGuid());

        // Act
        var countResult = await _repository!.CountAsync(filter);

        // Assert
        countResult.ShouldBeSuccess();

        countResult.ValueOrDefault.ShouldBe(0);
    }

    [Test]
    public async Task GenericRepository_Collection_Should_HaveCount()
    {
        // Arrange
        var name = Fixture.Create<string>();

        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .With(x => x.Name, name)
            .CreateMany(5).ToList();

        var otherEntities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(5).ToArray();

        entities.AddRange(otherEntities);

        foreach (var entity in entities)
        {
            var createResult = await _repository!.StoreAsync(entity);
            createResult.ShouldBeSuccess();
        }

        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Name, name);

        // Act
        var countResult = await _repository!.CountAsync(filter);

        // Assert
        countResult.ShouldBeSuccess();

        countResult.ValueOrDefault.ShouldBe(5);
    }

    [Test]
    public async Task GenericRepository_Collection_Should_NotHaveAny()
    {
        // Arrange
        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(5);

        foreach (var entity in entities)
        {
            var createResult = await _repository!.StoreAsync(entity);
            createResult.ShouldBeSuccess();
        }

        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, Guid.NewGuid());

        // Act
        var countResult = await _repository!.HasAnyAsync(filter);

        // Assert
        countResult.ShouldBeSuccess();

        countResult.ValueOrDefault.ShouldBeFalse();
    }

    [Test]
    public async Task GenericRepository_Collection_Should_HaveAny()
    {
        // Arrange
        const string name = "TestName";

        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .With(x => x.Name, name)
            .CreateMany(5).ToList();

        var otherEntities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(5).ToArray();

        entities.AddRange(otherEntities);

        foreach (var entity in entities)
        {
            var createResult = await _repository!.StoreAsync(entity);
            createResult.ShouldBeSuccess();
        }

        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Name, name);

        // Act
        var countResult = await _repository!.HasAnyAsync(filter);

        // Assert
        countResult.ShouldBeSuccess();

        countResult.ValueOrDefault.ShouldBeTrue();
    }

    [TestCase(true)]
    [TestCase(false)]
    public async Task GenericRepository_Delete_DerivedType_ShouldBeRemoved(bool isSoftDelete)
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
        {
            builder.AddDatabase("TestDB1",
                db =>
                {
                    db.AddGenericRepository<TestEntity1>(options =>
                    {
                        options.WithCollectionName("testEntity1")
                            .WithDatabaseName("TestDB1");

                        if (isSoftDelete)
                        {
                            options.WithSoftDelete();
                        }
                    });
                });
        });

        var repository = services.BuildServiceProvider().GetRequiredService<IGenericRepository<TestEntity1>>();

        var entity = Fixture.Build<DerivedTestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .Create();

        var createResult = await repository!.StoreAsync(entity);
        createResult.ShouldBeSuccess();

        var deleteResult = await repository.DeleteAsync(x => x.Id == entity.Id);
        deleteResult.ShouldBeSuccess();

        var collectionFactory = services.BuildServiceProvider().GetRequiredService<IMongoDbCollectionFactory>();
        var collectionResult = await collectionFactory.GetCollectionAsync<TestEntity1>("TestDB1");
        collectionResult.ShouldBeSuccess();

        var collection = collectionResult.ValueOrDefault;

        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, createResult.ValueOrDefault.Id);

        var entityFromDb = await collection.Find(filter).FirstOrDefaultAsync();

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

    [BsonKnownTypes(typeof(DerivedTestEntity1))]
    private class TestEntity1 : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public string? Name { get; set; }
        public string? Value { get; set; }
    }

    /// <inheritdoc />
    // ReSharper disable once ClassNeverInstantiated.Local
    private class DerivedTestEntity1 : TestEntity1
    {
        public string? SomeValue { get; set; }
        public IEnumerable<string>? Tags { get; set; }
    }
    [Test]
    public async Task GenericRepository_GetAsyncEnumerable_ShouldEnumerate()
    {
        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(10).ToArray();

        foreach (var entity in entities)
        {
            await _repository.StoreAsync(entity);
        }

        var count = 0;
        await foreach (var entity in _repository.GetAsyncEnumerable(Builders<TestEntity1>.Filter.Empty))
        {
            count++;
            entity.ShouldNotBeNull();
        }

        count.ShouldBe(10);
    }
}