using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
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
        var entities = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .Without(x => x.ETag)
            .CreateMany(20)
            .ToList();

        (await _bulkRepository.BulkStoreAsync(entities.ToArray())).ShouldBeSuccess();

        var entity = entities.First();
        var filter = Builders<TestEntity1>.Filter.Eq(x => x.Id, entity.Id);
        var projectionResult = await _projectionRepository.GetAsync(filter, x => new TestEntityProjection
        {
            Id = x.Id,
            Name = x.Name
        });

        projectionResult.ShouldBeSuccess();
        projectionResult.ValueOrDefault!.Id.ShouldBe(entity.Id);
        projectionResult.ValueOrDefault.Name.ShouldBe(entity.Name);
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
