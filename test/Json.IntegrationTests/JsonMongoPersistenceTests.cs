using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Json;
using Dilcore.MongoDB.NewtonsoftJson;
using Dilcore.MongoDB.SystemTextJson;
using Dilcore.MongoDB.TestSupport;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.Json.IntegrationTests;

[Category("M3Matrix")]
public class JsonMongoPersistenceTests
{
    private MongoDbContainer _container = null!;

    [OneTimeSetUp]
    public async Task Start()
    {
        _container = MongoTestImages.CreateStandalone();
        await _container.StartAsync();
    }

    [OneTimeTearDown]
    public async Task Stop() => await _container.DisposeAsync();

    [Test]
    public async Task InsertedCanonicalJson_PersistsExactBsonTypes()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_container.GetConnectionString()))
            .AddDatabase("JsonDB", db => db.OnCluster("primary")));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<Dilcore.MongoDB.Abstractions.IMongoDbCollectionFactory>();
        var converter = scope.ServiceProvider.GetRequiredService<IBsonJsonConverter>();
        var store = new JsonDocumentStore(factory, converter, new MongoDatabaseKey("JsonDB"));

        const string json = """
            {
              "_id": { "$oid": "573a1391f29313caabcd9637" },
              "count": { "$numberInt": "7" },
              "big": { "$numberLong": "9007199254740993" },
              "money": { "$numberDecimal": "10.50" },
              "when": { "$date": { "$numberLong": "0" } }
            }
            """;

        var insert = await store.InsertAsync("typed", json);
        insert.IsSuccess.ShouldBeTrue(string.Join(", ", insert.Errors));

        var collection = (await factory.GetCollectionAsync(new MongoDatabaseKey("JsonDB"), "typed")).Value;
        var stored = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();
        stored["_id"].BsonType.ShouldBe(BsonType.ObjectId);
        stored["count"].BsonType.ShouldBe(BsonType.Int32);
        stored["count"].AsInt32.ShouldBe(7);
        stored["big"].BsonType.ShouldBe(BsonType.Int64);
        stored["money"].BsonType.ShouldBe(BsonType.Decimal128);
        stored["when"].BsonType.ShouldBe(BsonType.DateTime);

        var element = stored.ToJsonElement(converter);
        element.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task InsertedJsonDocumentAndJObject_PersistSameBsonTypes()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_container.GetConnectionString()))
            .AddDatabase("JsonDomDB", db => db.OnCluster("primary")));
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<Dilcore.MongoDB.Abstractions.IMongoDbCollectionFactory>();
        var converter = scope.ServiceProvider.GetRequiredService<IBsonJsonConverter>();
        var store = new JsonDocumentStore(factory, converter, new MongoDatabaseKey("JsonDomDB"));

        const string json = """
            {
              "_id": { "$oid": "573a1391f29313caabcd9638" },
              "count": { "$numberInt": "3" },
              "when": { "$date": { "$numberLong": "0" } }
            }
            """;

        using var stj = JsonDocument.Parse(json);
        var stjInsert = await store.InsertAsync("stj-dom", stj);
        stjInsert.IsSuccess.ShouldBeTrue(string.Join(", ", stjInsert.Errors));

        var jObject = JObject.Parse(json);
        var nsInsert = await store.InsertAsync("ns-dom", jObject);
        nsInsert.IsSuccess.ShouldBeTrue(string.Join(", ", nsInsert.Errors));

        var stjRaw = (await factory.GetCollectionAsync(new MongoDatabaseKey("JsonDomDB"), "stj-dom")).Value;
        var nsRaw = (await factory.GetCollectionAsync(new MongoDatabaseKey("JsonDomDB"), "ns-dom")).Value;
        var stjStored = await stjRaw.Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();
        var nsStored = await nsRaw.Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();

        stjStored["_id"].BsonType.ShouldBe(BsonType.ObjectId);
        stjStored["count"].BsonType.ShouldBe(BsonType.Int32);
        stjStored["when"].BsonType.ShouldBe(BsonType.DateTime);
        nsStored["_id"].BsonType.ShouldBe(stjStored["_id"].BsonType);
        nsStored["count"].BsonType.ShouldBe(stjStored["count"].BsonType);
        nsStored["when"].BsonType.ShouldBe(stjStored["when"].BsonType);

        var id = stjStored["_id"];
        var asDocument = await store.GetJsonDocumentByIdAsync("stj-dom", id);
        asDocument.IsSuccess.ShouldBeTrue();
        using (asDocument.Value)
        {
            asDocument.Value.RootElement.GetProperty("count").GetProperty("$numberInt").GetString().ShouldBe("3");
        }

        var asJObject = await store.GetJObjectByIdAsync("ns-dom", nsStored["_id"]);
        asJObject.IsSuccess.ShouldBeTrue();
        asJObject.Value["count"]!["$numberInt"]!.Value<string>().ShouldBe("3");
    }
}
