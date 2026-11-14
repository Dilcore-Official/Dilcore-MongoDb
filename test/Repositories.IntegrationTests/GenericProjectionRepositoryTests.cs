using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

public class GenericProjectionRepositoryTests : BaseIntegrationTests
{
    private static readonly Fixture Fixture = new();
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private IGenericBulkRepository<TestEntity1> _bulkRepository = null!;
    private IGenericProjectionRepository<TestEntity1> _projectionRepository = null!;

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
                    .WithCollectionName("projectionEntity1")
                    .WithBulkRepository()
                    .WithProjectionRepository());
            }));

        _provider = AcceptanceServiceProviderFactory.Create(services);
        _scope = _provider.CreateScope();
        _bulkRepository = _scope.ServiceProvider.GetRequiredService<IGenericBulkRepository<TestEntity1>>();
        _projectionRepository = _scope.ServiceProvider.GetRequiredService<IGenericProjectionRepository<TestEntity1>>();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Test]
    public async Task GenericProjectionRepository_GetProjected()
    {
        // Arrange
        var entities = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .Without(x => x.ETag)
            .CreateMany(20)
            .ToList();

        var stored = await _bulkRepository.BulkStoreAsync(entities.ToArray());

        var entity = entities.First();
        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, entity.Id);

        // Act
        var projectionResult = await _projectionRepository.GetAsync(filter, x => new TestEntityProjection
        {
            Id = x.Id,
            Name = x.Name
        });

        // Assert
        stored.ShouldBeSuccess();
        projectionResult.ShouldBeSuccess();
        projectionResult.ValueOrDefault!.Id.ShouldBe(entity.Id);
        projectionResult.ValueOrDefault.Name.ShouldBe(entity.Name);
    }

    [Test]
    public async Task GenericProjectionRepository_MissingDocument_ReturnsNotFound()
    {
        // Arrange
        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, Guid.NewGuid());

        // Act
        var result = await _projectionRepository.GetAsync(filter, x => new TestEntityProjection
        {
            Id = x.Id,
            Name = x.Name
        });

        // Assert
        result.ShouldBeFailure();
        result.ShouldHaveError<DocumentNotFoundError>();
    }

    [Test]
    public async Task GenericProjectionRepository_SoftDeleted_IsExcluded()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("ProjectionSoftDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<TestEntity1>("e1", d => d
                    .WithCollectionName("projectionSoft")
                    .WithSoftDelete()
                    .WithBulkRepository()
                    .WithProjectionRepository());
            }));

        using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity1>>();
        var projection = scope.ServiceProvider.GetRequiredService<IGenericProjectionRepository<TestEntity1>>();

        var entity = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .Without(x => x.ETag)
            .Without(x => x.Id)
            .Create();
        var stored = await repository.StoreAsync(entity);
        var deleted = await repository.DeleteAsync(entity.Id, entity.ETag);

        // Act
        var missing = await projection.GetAsync(
            Builders<TestEntity1>.Filter.Eq(x => x.Id, entity.Id),
            x => new TestEntityProjection { Id = x.Id, Name = x.Name });
        var list = await projection.GetListAsync(x => new TestEntityProjection { Id = x.Id, Name = x.Name });

        // Assert
        stored.ShouldBeSuccess();
        deleted.ShouldBeSuccess();
        missing.ShouldHaveError<DocumentNotFoundError>();

        list.ShouldBeSuccess();
        list.Value.ShouldNotContain(x => x.Id == entity.Id);
    }

    public class TestEntity1 : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Name { get; set; }
    }

    public class TestEntityProjection
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
    }
}