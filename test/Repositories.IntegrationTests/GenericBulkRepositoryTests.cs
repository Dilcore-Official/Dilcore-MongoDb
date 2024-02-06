using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using Dilcore.DocumentDb.MongoDb.Repositories.IntegrationTests.Infrastructure;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Repositories.IntegrationTests;

public class GenericBulkRepositoryTests : BaseIntegrationTests
{
    private static readonly Fixture Fixture = new();

    private IGenericRepository<TestEntity1> _repository;
    private IGenericBulkRepository<TestEntity1> _bulkRepository;
    
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
                    db.AddGenericRepository<TestEntity1>(repositoryOptions => repositoryOptions.WithBulkRepository(),
                        collectionOptions => collectionOptions.WithCollectionName("testEntity1")
                            .WithDatabaseName("TestDB1"));
                });
        });

        _repository = services.BuildServiceProvider().GetRequiredService<IGenericRepository<TestEntity1>>();
        _bulkRepository = services.BuildServiceProvider().GetRequiredService<IGenericBulkRepository<TestEntity1>>();
    }
    
    [Test]
    public async Task GenericBulkRepository_BulkInsert()
    {
        var entities = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .With(x => x.UpdatedAt, DateTime.UtcNow)
            .Without(x => x.ETag)
            .CreateMany(20).ToList();

        var createResult = await _bulkRepository.BulkStoreAsync(entities.ToArray());
        createResult.Should().BeSuccess();

        var ids = entities.Select(x => x.Id);
        var entitiesListResult = await _repository.GetListAsync(x => ids.Contains(x.Id));
        entitiesListResult.Should().BeSuccess();
        
        entitiesListResult.ValueOrDefault.Should().HaveCount(entities.Count);
        entitiesListResult.ValueOrDefault.Should().AllSatisfy(x =>
        {
            x.Id.Should().NotBeEmpty();
            x.ETag.Should().NotBe(0);
            x.IsDeleted.Should().BeFalse();
            x.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(10));
        });
    }
    
    [Test]
    public async Task GenericBulkRepository_BulkInsertOrUpdate()
    {
        var entitiesList = new List<TestEntity1>();
        
        var entities = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .With(x => x.UpdatedAt, DateTime.UtcNow)
            .Without(x => x.ETag)
            .CreateMany(20).ToList();

        entitiesList.AddRange(entities);
        
        var createResult = await _bulkRepository.BulkStoreAsync(entitiesList.ToArray());
        createResult.Should().BeSuccess();
        createResult.ValueOrDefault.Should().NotBeNullOrEmpty();

        var createdEntities = createResult.ValueOrDefault;
        
        entitiesList.Clear();

        foreach (var entity in createdEntities)
        {
            entity.Name = "Updated";
            entity.Value = "Updated";
            
            entitiesList.Add(entity);
        }
        
        entities = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .With(x => x.UpdatedAt, DateTime.UtcNow)
            .With(x => x.Name, "New")
            .Without(x => x.ETag)
            .CreateMany(10).ToList();
        
        entitiesList.AddRange(entities);
        
        var createAndUpdateResult = await _bulkRepository.BulkStoreAsync(entitiesList.ToArray());
        createAndUpdateResult.Should().BeSuccess();
        
        var existingEntities = await _repository.GetListAsync();
        existingEntities.Should().BeSuccess();
        existingEntities.ValueOrDefault.Should().NotBeNullOrEmpty();
         
        existingEntities.ValueOrDefault.Where(x => x.Name == "Updated").Should().HaveCount(20);
        existingEntities.ValueOrDefault.Where(x => x.Name == "New").Should().HaveCount(10);
    }
    
    [TestCase(true)]
    [TestCase(false)]
    public async Task GenericBulkRepository_BulkDelete_WithFilter(bool isSoftDelete)
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(configure => configure.UseConnectionString(connectionString), builder =>
        {
            builder.AddDatabase("TestDB1",
                db =>
                {
                    db.AddGenericRepository<TestEntity1>(repositoryOptions => repositoryOptions.WithBulkRepository(),
                        collectionOptions =>
                        {
                            collectionOptions.WithCollectionName("testEntity1")
                                .WithDatabaseName("TestDB1");

                            if (isSoftDelete)
                            {
                                collectionOptions.WithSoftDelete();
                            }
                        });
                });
        });

        var repository = services.BuildServiceProvider().GetRequiredService<IGenericRepository<TestEntity1>>();
        var bulkRepository = services.BuildServiceProvider().GetRequiredService<IGenericBulkRepository<TestEntity1>>();
        
        var entities = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .With(x => x.UpdatedAt, DateTime.UtcNow)
            .Without(x => x.ETag)
            .CreateMany(20).ToList();

        var createResult = await bulkRepository.BulkStoreAsync(entities.ToArray());
        createResult.Should().BeSuccess();
        
        var entitiesListResult = await repository.GetListAsync();
        entitiesListResult.Should().BeSuccess();
        
        var ids = entitiesListResult.ValueOrDefault.Select(x => x.Id).ToArray();
        
        var deleteResult = await bulkRepository.BulkDeleteAsync(x => ids.Contains(x.Id));
        deleteResult.Should().BeSuccess();
        
        entitiesListResult = await repository.GetListAsync();
        entitiesListResult.Should().BeSuccess();
        
        var collectionFactory = services.BuildServiceProvider().GetRequiredService<IMongoDbCollectionFactory>();
        var collectionResult = await collectionFactory.GetCollectionAsync<TestEntity1>("TestDB1");
        collectionResult.Should().BeSuccess();
        
        var collection = collectionResult.ValueOrDefault;
        
        var filter = Builders<TestEntity1>.Filter.In(x => x.Id, ids);
        
        var entitiesFromDb = await collection.Find(filter).ToListAsync();

        if (isSoftDelete)
        {
            entitiesFromDb.Should().NotBeNullOrEmpty();
            entitiesFromDb!.Should().AllSatisfy(x =>
            {
                x.IsDeleted.Should().BeTrue();
            });
        }
        else
        {
            entitiesFromDb.Should().BeNullOrEmpty();
        }
    }
    
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
}