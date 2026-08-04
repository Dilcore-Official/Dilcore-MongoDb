using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

public class BsonDocumentRepositoryTests : BaseIntegrationTests
{
    private const string DatabaseName = "JsonDocuments";
    private const string CollectionName = "test";

    [Test]
    public async Task CustomBsonDocumentRepository_WhenMethodsCalled_ShouldBeSuccess()
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase(DatabaseName, db => db.OnCluster("primary"))
            .AddDocumentBinding<KeepAliveEntity>("keep", d => d
                .InDatabase(DatabaseName)
                .WithCollectionName("keep")));

        services.AddScoped<IBsonDocRepository>(sp =>
            new BsonDocRepository(
                new MongoDatabaseKey(DatabaseName),
                sp.GetRequiredService<IMongoDbCollectionFactory>()));

        using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBsonDocRepository>();

        var id = Guid.NewGuid().ToString();
        var document = new BsonDocument { ["_id"] = id, ["name"] = "test" };

        await repository.CreateAsync(document);
        var result = await repository.GetAsync(id);
        result.ShouldBeSuccess();
        result.Value!["name"].AsString.ShouldBe("test");
    }

    [Test]
    public async Task CustomBsonDocumentRepository_WithCollectionPrefix_ShouldUsePhysicalName()
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();
        const string prefix = "prefix";

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase(DatabaseName, db => db.OnCluster("primary"))
            .AddDocumentBinding<KeepAliveEntity>("keep", d => d
                .InDatabase(DatabaseName)
                .WithCollectionName("keep")));

        services.AddScoped<IBsonDocRepository>(sp =>
            new BsonDocRepository(
                new MongoDatabaseKey(DatabaseName),
                sp.GetRequiredService<IMongoDbCollectionFactory>(),
                staticPrefix: prefix));

        using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBsonDocRepository>();

        var id = Guid.NewGuid().ToString();
        await repository.CreateAsync(new BsonDocument { ["_id"] = id, ["name"] = "test" });

        var client = scope.ServiceProvider.GetRequiredKeyedService<IMongoClient>("primary");
        var collection = client.GetDatabase(DatabaseName).GetCollection<BsonDocument>($"{prefix}_{CollectionName}");
        var fromDb = await collection.Find(Builders<BsonDocument>.Filter.Eq("_id", id)).FirstOrDefaultAsync();
        fromDb.ShouldNotBeNull();
    }

    public interface IBsonDocRepository
    {
        Task<Result<BsonDocument?>> GetAsync(string id);
        Task CreateAsync(BsonDocument document);
    }

    public class BsonDocRepository(
        MongoDatabaseKey databaseKey,
        IMongoDbCollectionFactory collectionFactory,
        string? staticPrefix = null)
        : BsonDocumentRepository(databaseKey, collectionFactory, staticPrefix: staticPrefix), IBsonDocRepository
    {
        public Task<Result<BsonDocument?>> GetAsync(string id) =>
            ExecuteAsync(CollectionName, async collection =>
            {
                var filter = Builders<BsonDocument>.Filter.Eq("_id", id);
                var result = await collection.Find(filter).FirstOrDefaultAsync();
                return Result.Ok<BsonDocument?>(result);
            });

        public async Task CreateAsync(BsonDocument document)
        {
            await ExecuteAsync(CollectionName, async collection =>
            {
                await collection.InsertOneAsync(document);
                return Result.Ok();
            });
        }
    }

    public class KeepAliveEntity : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
