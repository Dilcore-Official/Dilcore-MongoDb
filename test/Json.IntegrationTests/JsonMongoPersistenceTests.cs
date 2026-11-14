using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.NewtonsoftJson;
using Dilcore.MongoDB.SystemTextJson;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json.Linq;
using System.Text.Json;

namespace Dilcore.MongoDB.Json.IntegrationTests;

[Category("M3Matrix")]
public class JsonMongoPersistenceTests : BaseIntegrationTests
{
    [Test]
    public async Task InsertedCanonicalJson_PersistsExactBsonTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("JsonDB", db => db.OnCluster("primary")));
        services.AddSystemTextJsonDocuments();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<Dilcore.MongoDB.Abstractions.IMongoDbCollectionFactory>();
        var converter = scope.ServiceProvider.GetRequiredService<IBsonJsonConverter>();
        var store = scope.ServiceProvider
            .GetRequiredService<IJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("JsonDB"));
        const string json = """
            {
              "_id": { "$oid": "573a1391f29313caabcd9637" },
              "count": { "$numberInt": "7" },
              "big": { "$numberLong": "9007199254740993" },
              "money": { "$numberDecimal": "10.50" },
              "when": { "$date": { "$numberLong": "0" } }
            }
            """;

        // Act
        var insert = await store.InsertAsync("typed", json);
        var collection = (await factory.GetCollectionAsync(new MongoDatabaseKey("JsonDB"), "typed")).Value;
        var stored = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();
        var element = stored.ToJsonElement(converter);

        // Assert
        insert.IsSuccess.ShouldBeTrue(string.Join(", ", insert.Errors));
        stored["_id"].BsonType.ShouldBe(BsonType.ObjectId);
        stored["count"].BsonType.ShouldBe(BsonType.Int32);
        stored["count"].AsInt32.ShouldBe(7);
        stored["big"].BsonType.ShouldBe(BsonType.Int64);
        stored["money"].BsonType.ShouldBe(BsonType.Decimal128);
        stored["when"].BsonType.ShouldBe(BsonType.DateTime);
        element.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public async Task InsertedJsonDocumentAndJObject_PersistSameBsonTypes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("JsonDomDB", db => db.OnCluster("primary")));
        services.AddSystemTextJsonDocuments();
        services.AddNewtonsoftJsonDocuments();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<Dilcore.MongoDB.Abstractions.IMongoDbCollectionFactory>();
        _ = scope.ServiceProvider.GetRequiredService<IBsonJsonConverter>();
        var store = scope.ServiceProvider
            .GetRequiredService<IJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("JsonDomDB"));
        const string json = """
            {
              "_id": { "$oid": "573a1391f29313caabcd9638" },
              "count": { "$numberInt": "3" },
              "when": { "$date": { "$numberLong": "0" } }
            }
            """;
        using var stj = JsonDocument.Parse(json);
        var jObject = JObject.Parse(json);

        // Act
        var stjInsert = await store.InsertAsync("stj-dom", stj);
        var nsInsert = await store.InsertAsync("ns-dom", jObject);
        var stjRaw = (await factory.GetCollectionAsync(new MongoDatabaseKey("JsonDomDB"), "stj-dom")).Value;
        var nsRaw = (await factory.GetCollectionAsync(new MongoDatabaseKey("JsonDomDB"), "ns-dom")).Value;
        var stjStored = await stjRaw.Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();
        var nsStored = await nsRaw.Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();
        var asDocument = await store.GetJsonDocumentByIdAsync("stj-dom", stjStored["_id"]);
        var asJObject = await store.GetJObjectByIdAsync("ns-dom", nsStored["_id"]);

        // Assert
        stjInsert.IsSuccess.ShouldBeTrue(string.Join(", ", stjInsert.Errors));
        nsInsert.IsSuccess.ShouldBeTrue(string.Join(", ", nsInsert.Errors));
        stjStored["_id"].BsonType.ShouldBe(BsonType.ObjectId);
        stjStored["count"].BsonType.ShouldBe(BsonType.Int32);
        stjStored["when"].BsonType.ShouldBe(BsonType.DateTime);
        nsStored["_id"].BsonType.ShouldBe(stjStored["_id"].BsonType);
        nsStored["count"].BsonType.ShouldBe(stjStored["count"].BsonType);
        nsStored["when"].BsonType.ShouldBe(stjStored["when"].BsonType);
        asDocument.IsSuccess.ShouldBeTrue();
        using (asDocument.Value)
        {
            asDocument.Value.RootElement.GetProperty("count").GetProperty("$numberInt").GetString().ShouldBe("3");
        }

        asJObject.IsSuccess.ShouldBeTrue();
        asJObject.Value["count"]!["$numberInt"]!.Value<string>().ShouldBe("3");
    }
}