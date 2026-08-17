using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Repositories;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

public class GenericBulkRepositoryTests : BaseIntegrationTests
{
    private static readonly Fixture Fixture = new();
    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private IGenericRepository<TestEntity1> _repository = null!;
    private IGenericBulkRepository<TestEntity1> _bulkRepository = null!;

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
                    .WithCollectionName("bulkEntity1")
                    .WithBulkRepository());
            }));

        _provider = AcceptanceServiceProviderFactory.Create(services);
        _scope = _provider.CreateScope();
        _repository = _scope.ServiceProvider.GetRequiredService<IGenericRepository<TestEntity1>>();
        _bulkRepository = _scope.ServiceProvider.GetRequiredService<IGenericBulkRepository<TestEntity1>>();
    }

    [TearDown]
    public void TearDown()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Test]
    public async Task GenericBulkRepository_BulkInsert()
    {
        var entities = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .Without(x => x.ETag)
            .CreateMany(20)
            .ToList();

        var createResult = await _bulkRepository.BulkStoreAsync(entities.ToArray());
        createResult.ShouldBeSuccess();

        var ids = entities.Select(x => x.Id);
        var list = await _repository.GetListAsync(x => ids.Contains(x.Id));
        list.ShouldBeSuccess();
        list.ValueOrDefault.Count.ShouldBe(entities.Count);
    }

    [Test]
    public async Task GenericBulkRepository_BulkDelete()
    {
        var entities = Fixture.Build<TestEntity1>()
            .With(x => x.IsDeleted, false)
            .Without(x => x.ETag)
            .CreateMany(5)
            .ToList();

        (await _bulkRepository.BulkStoreAsync(entities.ToArray())).ShouldBeSuccess();

        var ids = entities.Select(x => x.Id).ToHashSet();
        (await _bulkRepository.BulkDeleteAsync(x => ids.Contains(x.Id))).ShouldBeSuccess();

        var remaining = await _repository.GetListAsync(x => ids.Contains(x.Id));
        remaining.ShouldBeSuccess();
        remaining.ValueOrDefault.Count.ShouldBe(0);
    }

    public class TestEntity1 : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? Name { get; set; }
    }
}
