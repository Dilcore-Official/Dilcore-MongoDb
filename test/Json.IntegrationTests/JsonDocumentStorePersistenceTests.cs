using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.NewtonsoftJson;
using Dilcore.MongoDB.SystemTextJson;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dilcore.MongoDB.Json.IntegrationTests;

[Category("M3Matrix")]
public class JsonDocumentStorePersistenceTests : BaseIntegrationTests
{
    [Test]
    public async Task StaticPrefix_LandsInPrefixedCollection()
    {
        // Arrange
        await using var host = CreateStjHost("PrefixDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("PrefixDB"), staticPrefix: "tenantA");
        var collectionFactory = host.Scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        // Act
        var insert = await store.InsertAsync(
            "payloads",
            """{"_id":{"$oid":"573a1391f29313caabcd9701"},"v":{"$numberInt":"9"}}""");
        var resolvedName = await collectionFactory.ResolveCollectionNameAsync(
            new MongoDatabaseKey("PrefixDB"), "payloads", "tenantA");
        var collection = (await collectionFactory.GetCollectionAsync(
            new MongoDatabaseKey("PrefixDB"), "payloads", "tenantA")).Value;
        var stored = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();

        // Assert
        insert.IsSuccess.ShouldBeTrue(string.Join(", ", insert.Errors));
        resolvedName.IsSuccess.ShouldBeTrue();
        resolvedName.Value.ShouldContain("tenantA");
        stored["v"].BsonType.ShouldBe(BsonType.Int32);
    }

    [Test]
    public async Task MissingPrefixResolver_FailsClosed()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("FailPrefixDB", db =>
            {
                db.OnCluster("primary");
                db.WithNamespacePrefixResolver<FailingPrefixResolver>();
            }));
        services.AddSystemTextJsonDocuments();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider
            .GetRequiredService<ISystemTextJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("FailPrefixDB"));

        // Act
        var result = await store.InsertAsync("payloads", """{"v":1}""");

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("required namespace prefix");
    }

    [Test]
    public async Task InsertRootArray_Fails()
    {
        // Arrange
        await using var host = CreateStjHost("RootArrayDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("RootArrayDB"));
        using var document = JsonDocument.Parse("[1,2]");

        // Act
        var result = await store.InsertAsync("payloads", document);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("JSON root must be a document");
    }

    [Test]
    public async Task InsertNullJsonNode_Fails()
    {
        // Arrange
        await using var host = CreateStjHost("NullNodeDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("NullNodeDB"));
        JsonNode? node = null;

        // Act
        var result = await store.InsertAsync("payloads", node);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("JSON root must be a document");
    }

    [Test]
    public async Task DuplicateId_ReturnsDuplicateKeyError()
    {
        // Arrange
        await using var host = CreateStjHost("DupIdDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("DupIdDB"));
        const string json = """{"_id":{"$oid":"573a1391f29313caabcd9702"},"v":1}""";
        var first = await store.InsertAsync("payloads", json);

        // Act
        var second = await store.InsertAsync("payloads", json);

        // Assert
        first.IsSuccess.ShouldBeTrue();
        second.IsFailed.ShouldBeTrue();
        second.HasError<DuplicateKeyError>().ShouldBeTrue();
    }

    [Test]
    public async Task GetById_Missing_ReturnsDocumentNotFoundError()
    {
        // Arrange
        await using var host = CreateStjHost("MissingIdDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("MissingIdDB"));
        var id = ObjectId.GenerateNewId();

        // Act
        var result = await store.GetByIdAsync("payloads", id);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasError<DocumentNotFoundError>().ShouldBeTrue();
    }

    [Test]
    public async Task ReadPath_ReturnsNestedAndArrayContent()
    {
        // Arrange
        await using var host = CreateStjHost("ReadPathDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("ReadPathDB"));
        const string json = """
            {
              "_id": { "$oid": "573a1391f29313caabcd9703" },
              "child": { "n": { "$numberInt": "3" } },
              "items": [ { "$numberLong": "2" }, "x" ]
            }
            """;
        var insert = await store.InsertAsync("payloads", json);
        var options = new JsonBsonConversionOptions { OutputMode = JsonBsonOutputMode.CanonicalExtendedJson };

        // Act
        var loaded = await store.GetJsonDocumentByIdAsync("payloads", insert.Value["_id"], options);
        var element = await store.GetJsonElementByIdAsync("payloads", insert.Value["_id"], options);

        // Assert
        insert.IsSuccess.ShouldBeTrue();
        loaded.IsSuccess.ShouldBeTrue();
        using (loaded.Value)
        {
            loaded.Value.RootElement.GetProperty("child").GetProperty("n")
                .GetProperty("$numberInt").GetString().ShouldBe("3");
            loaded.Value.RootElement.GetProperty("items")[0]
                .GetProperty("$numberLong").GetString().ShouldBe("2");
        }

        element.IsSuccess.ShouldBeTrue();
        element.Value.GetProperty("items")[1].GetString().ShouldBe("x");
    }

    [Test]
    public async Task InsertString_RootArrayOrScalar_Fails()
    {
        // Arrange
        await using var host = CreateStjHost("RootStringDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("RootStringDB"));

        // Act
        var arrayRoot = await store.InsertAsync("payloads", "[1,2]");
        var scalarRoot = await store.InsertAsync("payloads", "42");

        // Assert
        arrayRoot.IsFailed.ShouldBeTrue();
        arrayRoot.Errors[0].Message.ShouldContain("JSON root must be a document");
        scalarRoot.IsFailed.ShouldBeTrue();
        scalarRoot.Errors[0].Message.ShouldContain("JSON root must be a document");
    }

    [Test]
    public async Task InsertOversizedBsonDocument_ReturnsDocumentTooLargeError()
    {
        // Arrange
        await using var host = CreateStjHost("TooLargeDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("TooLargeDB"));
        var document = new BsonDocument
        {
            ["_id"] = ObjectId.GenerateNewId(),
            ["blob"] = new BsonBinaryData(new byte[16 * 1024 * 1024])
        };

        // Act
        var result = await store.InsertAsync("payloads", document);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasError<DocumentTooLargeError>().ShouldBeTrue();
    }

    [Test]
    public async Task Newtonsoft_GetJTokenByIdAsync_ReturnsNestedContent()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("NsReadDB", db => db.OnCluster("primary")));
        services.AddNewtonsoftJsonDocuments();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider
            .GetRequiredService<INewtonsoftJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("NsReadDB"));
        const string json = """
            {
              "_id": { "$oid": "573a1391f29313caabcd9705" },
              "items": [ { "$numberInt": "4" }, "y" ]
            }
            """;
        var insert = await store.InsertAsync("payloads", JObject.Parse(json));
        var options = new JsonBsonConversionOptions { OutputMode = JsonBsonOutputMode.CanonicalExtendedJson };

        // Act
        var token = await store.GetJTokenByIdAsync("payloads", insert.Value["_id"], options);

        // Assert
        insert.IsSuccess.ShouldBeTrue(string.Join(", ", insert.Errors));
        token.IsSuccess.ShouldBeTrue();
        token.Value["items"]![0]!["$numberInt"]!.Value<string>().ShouldBe("4");
        token.Value["items"]![1]!.Value<string>().ShouldBe("y");
    }

    [Test]
    public async Task Newtonsoft_InsertAsync_TypeNameHandling_FailsBeforeWrite()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("NsTypeNameDB", db => db.OnCluster("primary")));
        services.AddNewtonsoftJsonDocuments();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider
            .GetRequiredService<INewtonsoftJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("NsTypeNameDB"));
        var serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

        // Act
        var result = await store.InsertAsync(
            "payloads",
            JObject.Parse("""{"v":1}"""),
            serializerSettings: serializerSettings);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("TypeNameHandling");
    }

    [Test]
    public async Task Newtonsoft_DateLikeString_AndHighPrecisionDecimal_Survive()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase("NsFidelityDB", db => db.OnCluster("primary")));
        services.AddNewtonsoftJsonDocuments();
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider
            .GetRequiredService<INewtonsoftJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("NsFidelityDB"));
        using var reader = new JsonTextReader(new StringReader(
            """{"_id":{"$oid":"573a1391f29313caabcd9704"},"when":"2020-01-01T00:00:00Z","money":1.234567890123456789}"""))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal
        };
        var token = JToken.Load(reader);
        var options = new JsonBsonConversionOptions
        {
            NumberMode = OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDecimal128
        };

        // Act
        var insert = await store.InsertAsync("payloads", token, options);
        var loaded = await store.GetJObjectByIdAsync("payloads", insert.Value["_id"]);

        // Assert
        insert.IsSuccess.ShouldBeTrue(string.Join(", ", insert.Errors));
        insert.Value["when"].BsonType.ShouldBe(BsonType.String);
        insert.Value["money"].BsonType.ShouldBe(BsonType.Decimal128);
        loaded.IsSuccess.ShouldBeTrue();
        loaded.Value["when"]!.Type.ShouldBe(JTokenType.String);
        loaded.Value["when"]!.Value<string>().ShouldBe("2020-01-01T00:00:00Z");
    }

    [Test]
    public void Newtonsoft_TypeNameHandlingAuto_IsRejected()
    {
        // Arrange
        var converter = new Dilcore.MongoDB.Json.BsonJsonConverter();
        var token = JObject.Parse("""{"v":1}""");
        var serializerSettings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        // Act
        var result = token.ToBson(converter, serializerSettings: serializerSettings);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("TypeNameHandling");
    }

    private StjHost CreateStjHost(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(MongoDbContainer.GetConnectionString()))
            .AddDatabase(databaseName, db => db.OnCluster("primary")));
        services.AddSystemTextJsonDocuments();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        return new StjHost(
            provider,
            scope,
            scope.ServiceProvider.GetRequiredService<ISystemTextJsonDocumentStoreFactory>());
    }

    private sealed class StjHost(
        ServiceProvider provider,
        IServiceScope scope,
        ISystemTextJsonDocumentStoreFactory storeFactory) : IAsyncDisposable
    {
        public IServiceScope Scope { get; } = scope;
        public ISystemTextJsonDocumentStoreFactory StoreFactory { get; } = storeFactory;

        public ValueTask DisposeAsync()
        {
            Scope.Dispose();
            provider.Dispose();
            return ValueTask.CompletedTask;
        }
    }

    private sealed class FailingPrefixResolver : INamespacePrefixResolver
    {
        public Task<Result<string?>> ResolveAsync(
            NamespaceResolutionRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Fail<string?>("A required namespace prefix is missing from the current request context."));
    }
}