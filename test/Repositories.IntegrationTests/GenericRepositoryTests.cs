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

        result.Should().BeSuccess();

        var result2 = await _repository!.GetAsync(result.ValueOrDefault.Id);
        result2.Should().BeSuccess();

        var createdEntity = result2.ValueOrDefault;
        createdEntity.Should().NotBeNull();

        createdEntity.Id.Should().NotBeEmpty();
        createdEntity.ETag.Should().NotBe(0);

        createdEntity.Name.Should().Be(entity.Name);
        createdEntity.Value.Should().Be(entity.Value);
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

        createResult.Should().BeSuccess();

        var result2 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        result2.Should().BeSuccess();

        var entityToUpdate = result2.ValueOrDefault;

        entityToUpdate.Name = "UpdatedName";
        entityToUpdate.Value = "UpdatedValue";

        var updateResult = await _repository!.StoreAsync(entityToUpdate);
        updateResult.Should().BeSuccess();

        var result3 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        var updatedEntity = result3.ValueOrDefault;

        updatedEntity.Id.Should().NotBeEmpty();
        updatedEntity.Id.Should().Be(createResult.ValueOrDefault.Id);

        updatedEntity.ETag.Should().NotBe(0);
        updatedEntity.ETag.Should().NotBe(createResult.ValueOrDefault.ETag);

        updatedEntity.Name.Should().Be(entityToUpdate.Name);
        updatedEntity.Value.Should().Be(entityToUpdate.Value);
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

        createResult.Should().BeSuccess();

        var result2 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        result2.Should().BeSuccess();

        var entityToUpdate = result2.ValueOrDefault;

        entityToUpdate.Name = "UpdatedName";
        entityToUpdate.Value = "UpdatedValue";
        entityToUpdate.ETag = 123;

        var updateResult = await _repository!.StoreAsync(entityToUpdate);
        updateResult.Should().BeFailure();

        var result3 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        entity = result3.ValueOrDefault;

        entity.ETag.Should().Be(createResult.ValueOrDefault.ETag);
        entity.Name.Should().Be(createResult.ValueOrDefault.Name);
        entity.Value.Should().Be(createResult.ValueOrDefault.Value);
        entity.UpdatedAt.Should().BeSameDateAs(createResult.ValueOrDefault.UpdatedAt);
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

        createResult.Should().BeSuccess();

        var result2 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        result2.Should().BeSuccess();

        var entityToUpdate = result2.ValueOrDefault;

        entityToUpdate.Name = "UpdatedName";
        entityToUpdate.Value = "UpdatedValue";
        entityToUpdate.Id = Guid.NewGuid();

        var updateResult = await _repository!.StoreAsync(entityToUpdate);
        updateResult.Should().BeFailure();

        var result3 = await _repository!.GetAsync(entityToUpdate.Id);
        result3.Should().BeSuccess();
        result3.ValueOrDefault.Should().BeNull();

        var result4 = await _repository!.GetAsync(createResult.ValueOrDefault.Id);
        entity = result4.ValueOrDefault;

        entity.ETag.Should().Be(createResult.ValueOrDefault.ETag);
        entity.Name.Should().Be(createResult.ValueOrDefault.Name);
        entity.Value.Should().Be(createResult.ValueOrDefault.Value);
        entity.UpdatedAt.Should().BeSameDateAs(createResult.ValueOrDefault.UpdatedAt);
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
            createResult.Should().BeSuccess();
        }

        var result = await _repository!.GetListAsync();
        result.Should().BeSuccess();

        var entitiesFromDb = result.ValueOrDefault;
        entitiesFromDb.Should().NotBeNullOrEmpty();

        foreach (var entity in entities)
        {
            entitiesFromDb.Should().Contain(x => x.Id == entity.Id);
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
            createResult.Should().BeSuccess();
        }

        var ids = entities.Select(x => x.Id);

        var result = await _repository!.GetListAsync(x => ids.Contains(x.Id));
        result.Should().BeSuccess();

        var entitiesFromDb = result.ValueOrDefault;
        entitiesFromDb.Should().NotBeNullOrEmpty();

        entitiesFromDb.Should().HaveCount(entities.Length);
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
        createResult.Should().BeSuccess();

        var result = await repository.DeleteAsync(createResult.ValueOrDefault.Id, createResult.ValueOrDefault.ETag);
        result.Should().BeSuccess();

        var collectionFactory = services.BuildServiceProvider().GetRequiredService<IMongoDbCollectionFactory>();
        var collectionResult = await collectionFactory.GetCollectionAsync<TestEntity1>("TestDB1");
        collectionResult.Should().BeSuccess();

        var collection = collectionResult.ValueOrDefault;

        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, createResult.ValueOrDefault.Id);

        var entityFromDb = await collection.Find(filter).FirstOrDefaultAsync();

        if (isSoftDelete)
        {
            entityFromDb.Should().NotBeNull();
            entityFromDb!.IsDeleted.Should().BeTrue();
        }
        else
        {
            entityFromDb.Should().BeNull();
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
        createResult.Should().BeSuccess();
        
        var derivedEntityResult = await _repository!.GetAsync<TestEntity1, DerivedTestEntity1>(createResult.ValueOrDefault.Id);
        derivedEntityResult.Should().BeSuccess();
        
        var derivedEntity = derivedEntityResult.ValueOrDefault;
        derivedEntity.Should().NotBeNull();
        
        derivedEntity.SomeValue.Should().Be(entity.SomeValue);
        derivedEntity.Tags.Should().BeEquivalentTo(entity.Tags);
        derivedEntity.Name.Should().Be(entity.Name);
        derivedEntity.Value.Should().Be(entity.Value);
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
            createResult.Should().BeSuccess();
        }
        
        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(5).ToArray();
        
        foreach (var entity in entities)
        {
            var createResult = await _repository!.StoreAsync(entity);
            createResult.Should().BeSuccess();
        }

        var ids = entities.Select(x => x.Id).Concat(derivedEntities.Select(x => x.Id)).ToArray();
        
        var derivedEntityResult =
            await _repository!.GetListAsync<TestEntity1, DerivedTestEntity1>(x => ids.Contains(x.Id));
        derivedEntityResult.Should().BeSuccess();
        
        var derivedEntitiesFromDb = derivedEntityResult.ValueOrDefault;
        derivedEntitiesFromDb.Should().NotBeNullOrEmpty();
        
        derivedEntitiesFromDb.Should().HaveCount(derivedEntities.Length);

        derivedEntitiesFromDb.Should().AllSatisfy(derivedEntity =>
        {
            var entity = derivedEntities.First(x => x.Id == derivedEntity.Id);

            derivedEntity.SomeValue.Should().Be(entity.SomeValue);
            derivedEntity.Tags.Should().BeEquivalentTo(entity.Tags);
            derivedEntity.Name.Should().Be(entity.Name);
            derivedEntity.Value.Should().Be(entity.Value);
        });
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
            createResult.Should().BeSuccess();
        }
        
        var entities = Fixture.Build<TestEntity1>()
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Without(x => x.IsDeleted)
            .CreateMany(5);
        
        foreach (var entity in entities)
        {
            var createResult = await _repository!.StoreAsync(entity);
            createResult.Should().BeSuccess();
        }
        
        var entitiesResult = await _repository!.GetListAsync(x => true);
        entitiesResult.Should().BeSuccess();
        
        var entitiesFromDb = entitiesResult.ValueOrDefault;
        entitiesFromDb.Should().NotBeNullOrEmpty();
        
        var derivedEntitiesFromDb = entitiesFromDb.OfType<DerivedTestEntity1>().ToArray();

        derivedEntitiesFromDb.Should().AllSatisfy(derivedEntity =>
        {
            derivedEntity.SomeValue.Should().NotBeNullOrEmpty();
            derivedEntity.Tags.Should().NotBeNullOrEmpty();
            derivedEntity.Name.Should().NotBeNullOrEmpty();
            derivedEntity.Value.Should().NotBeNullOrEmpty();
        });
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
            createResult.Should().BeSuccess();
        }
        
        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, Guid.NewGuid());
        
        // Act
        var countResult = await _repository!.CountAsync(filter);
        
        // Assert
        countResult.Should().BeSuccess();
        
        countResult.ValueOrDefault.Should().Be(0);
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
            createResult.Should().BeSuccess();
        }
        
        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Name, name);
        
        // Act
        var countResult = await _repository!.CountAsync(filter);
        
        // Assert
        countResult.Should().BeSuccess();
        
        countResult.ValueOrDefault.Should().Be(5);
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
            createResult.Should().BeSuccess();
        }
        
        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, Guid.NewGuid());
        
        // Act
        var countResult = await _repository!.HasAnyAsync(filter);
        
        // Assert
        countResult.Should().BeSuccess();
        
        countResult.ValueOrDefault.Should().BeFalse();
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
            createResult.Should().BeSuccess();
        }
        
        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Name, name);
        
        // Act
        var countResult = await _repository!.HasAnyAsync(filter);
        
        // Assert
        countResult.Should().BeSuccess();
        
        countResult.ValueOrDefault.Should().BeTrue();
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
        createResult.Should().BeSuccess();

        var deleteResult = await repository.DeleteAsync(x => x.Id == entity.Id);
        deleteResult.Should().BeSuccess();
        
        var collectionFactory = services.BuildServiceProvider().GetRequiredService<IMongoDbCollectionFactory>();
        var collectionResult = await collectionFactory.GetCollectionAsync<TestEntity1>("TestDB1");
        collectionResult.Should().BeSuccess();
        
        var collection = collectionResult.ValueOrDefault;

        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, createResult.ValueOrDefault.Id);

        var entityFromDb = await collection.Find(filter).FirstOrDefaultAsync();

        if (isSoftDelete)
        {
            entityFromDb.Should().NotBeNull();
            entityFromDb!.IsDeleted.Should().BeTrue();
        }
        else
        {
            entityFromDb.Should().BeNull();
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

        public string Name { get; set; }
        public string Value { get; set; }
    }

    /// <inheritdoc />
    // ReSharper disable once ClassNeverInstantiated.Local
    private class DerivedTestEntity1 : TestEntity1
    {
        public string SomeValue { get; set; }
        public IEnumerable<string> Tags { get; set; }
    }
}