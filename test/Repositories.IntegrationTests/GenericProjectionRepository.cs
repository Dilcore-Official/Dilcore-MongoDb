using Dilcore.DocumentDb.Abstractions;
using Dilcore.DocumentDb.MongoDb.Extensions;
using Dilcore.DocumentDb.MongoDb.Repositories.Abstractions;
using Dilcore.DocumentDb.MongoDb.Repositories.IntegrationTests.Infrastructure;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.MongoDb.Repositories.IntegrationTests;

public class GenericProjectionRepository : BaseIntegrationTests
{
    private static readonly Fixture Fixture = new();

    private IGenericBulkRepository<TestEntity1> _bulkRepository;
    private IGenericProjectionRepository<TestEntity1> _projectionRepository;
    
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
                    db.AddGenericRepository<TestEntity1>(repositoryOptions => repositoryOptions
                            .WithBulkRepository()
                            .WithProjectionRepository(),
                        collectionOptions => collectionOptions.WithCollectionName("projectionEntity1")
                            .WithDatabaseName("TestDB1"));
                });
        });

        _bulkRepository = services.BuildServiceProvider().GetRequiredService<IGenericBulkRepository<TestEntity1>>();
        _projectionRepository = services.BuildServiceProvider().GetRequiredService<IGenericProjectionRepository<TestEntity1>>();
    }
    
    [Test]
    public async Task GenericProjectionRepository_GetProjected()
    {
        var entities = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .With(x => x.UpdateAt, DateTime.UtcNow)
            .Without(x => x.ETag)
            .CreateMany(20).ToList();

        var createResult = await _bulkRepository.BulkStoreAsync(entities.ToArray());
        createResult.Should().BeSuccess();

        var entity = entities.First();
        var id = entity.Id;
        
        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, id);
        var projectionResult = await _projectionRepository.GetAsync(filter, x =>
            new TestEntityProjection
            {
                Id = x.Id,
                Name = x.Name
            });

        projectionResult.Should().BeSuccess();
        
        projectionResult.ValueOrDefault.Should().NotBeNull();
        projectionResult.ValueOrDefault.Id.Should().Be(id);
        projectionResult.ValueOrDefault.Name.Should().Be(entity.Name);
    }
    
    [Test]
    public async Task GenericProjectionRepository_GetProjectedList()
    {
        var entities = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .With(x => x.UpdateAt, DateTime.UtcNow)
            .Without(x => x.ETag)
            .CreateMany(20).ToList();

        var createResult = await _bulkRepository.BulkStoreAsync(entities.ToArray());
        createResult.Should().BeSuccess();

        var entityIds = entities.Select(x => x.Id);
        
        var filter = Builders<TestEntity1>.Filter.In(x => x.Id, entityIds);
        var projectionResult = await _projectionRepository.GetListAsync(filter, x =>
            new TestEntityProjection
            {
                Id = x.Id,
                Name = x.Name
            });

        projectionResult.Should().BeSuccess();

        projectionResult.ValueOrDefault.Should().AllSatisfy(projected =>
        {
            var entity = entities.First(x => x.Id == projected.Id);
            projected.Name.Should().Be(entity.Name);
        });
    }
    
    private class TestEntity1 : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime UpdateAt { get; set; }
        
        public string Name { get; set; }
        public string Value { get; set; }
    }

    private class TestEntityProjection
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
    }
}