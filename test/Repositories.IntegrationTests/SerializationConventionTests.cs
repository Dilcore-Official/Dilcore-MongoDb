using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Internal;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Repositories.IntegrationTests;

[NonParallelizable]
[Category("M3Matrix")]
public class SerializationConventionTests : BaseIntegrationTests
{
    [SetUp]
    public void SetUp() => MongoConventionRegistrar.Reset();

    [TearDown]
    public void TearDown() => MongoConventionRegistrar.Reset();

    [Test]
    public async Task EnumRepresentation_Int32_StoresEnumAsInt()
    {
        var services = new ServiceCollection();
        var connectionString = MongoDbContainer.GetConnectionString();

        services.AddMongoDb(mongo => mongo
            .ConfigureConventions(c => c.UseEnumRepresentation(BsonType.Int32))
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("EnumConvDB", db =>
            {
                db.OnCluster("primary");
                db.AddDocumentBinding<EnumConventionDocument>("enums", d => d
                    .WithCollectionName("enumDocs"));
            }));

        using var provider = AcceptanceServiceProviderFactory.Create(services);
        using var scope = provider.CreateScope();
        var repository = scope.ServiceProvider
            .GetRequiredService<IGenericRepository<EnumConventionDocument>>();

        var store = await repository.StoreAsync(new EnumConventionDocument
        {
            Status = EnumConventionStatus.Ready
        });
        store.ShouldBeSuccess();

        var client = scope.ServiceProvider.GetRequiredKeyedService<IMongoClient>("primary");
        var raw = await client
            .GetDatabase("EnumConvDB")
            .GetCollection<BsonDocument>("enumDocs")
            .Find(Builders<BsonDocument>.Filter.Eq("_id", store.Value.Id))
            .FirstAsync();

        raw["status"].BsonType.ShouldBe(BsonType.Int32);
        raw["status"].AsInt32.ShouldBe((int)EnumConventionStatus.Ready);
    }

    public enum EnumConventionStatus
    {
        Pending = 0,
        Ready = 1
    }

    public sealed class EnumConventionDocument : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
        public EnumConventionStatus Status { get; set; }
    }
}
