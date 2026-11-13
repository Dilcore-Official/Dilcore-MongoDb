using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.NewtonsoftJson;
using Dilcore.MongoDB.SystemTextJson;
using Dilcore.MongoDB.TestSupport;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Testcontainers.MongoDb;

namespace Dilcore.MongoDB.Json.IntegrationTests;

[Category("M3Matrix")]
public class JsonDocumentStorePersistenceTests
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
    public async Task StaticPrefix_LandsInPrefixedCollection()
    {
        await using var host = CreateStjHost("PrefixDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("PrefixDB"), staticPrefix: "tenantA");
        var collectionFactory = host.Scope.ServiceProvider.GetRequiredService<IMongoDbCollectionFactory>();

        var insert = await store.InsertAsync(
            "payloads",
            """{"_id":{"$oid":"573a1391f29313caabcd9701"},"v":{"$numberInt":"9"}}""");
        insert.IsSuccess.ShouldBeTrue(string.Join(", ", insert.Errors));

        var resolvedName = await collectionFactory.ResolveCollectionNameAsync(
            new MongoDatabaseKey("PrefixDB"), "payloads", "tenantA");
        resolvedName.IsSuccess.ShouldBeTrue();
        resolvedName.Value.ShouldContain("tenantA");

        var collection = (await collectionFactory.GetCollectionAsync(
            new MongoDatabaseKey("PrefixDB"), "payloads", "tenantA")).Value;
        var stored = await collection.Find(Builders<BsonDocument>.Filter.Empty).FirstAsync();
        stored["v"].BsonType.ShouldBe(BsonType.Int32);
    }

    [Test]
    public async Task MissingPrefixResolver_FailsClosed()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_container.GetConnectionString()))
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

        var result = await store.InsertAsync("payloads", """{"v":1}""");
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("required namespace prefix");
    }

    [Test]
    public async Task InsertRootArray_Fails()
    {
        await using var host = CreateStjHost("RootArrayDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("RootArrayDB"));
        using var document = JsonDocument.Parse("[1,2]");
        var result = await store.InsertAsync("payloads", document);
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("JSON root must be a document");
    }

    [Test]
    public async Task InsertNullJsonNode_Fails()
    {
        await using var host = CreateStjHost("NullNodeDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("NullNodeDB"));
        JsonNode? node = null;
        var result = await store.InsertAsync("payloads", node);
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("JSON root must be a document");
    }

    [Test]
    public async Task DuplicateId_ReturnsDuplicateKeyError()
    {
        await using var host = CreateStjHost("DupIdDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("DupIdDB"));
        const string json = """{"_id":{"$oid":"573a1391f29313caabcd9702"},"v":1}""";
        (await store.InsertAsync("payloads", json)).IsSuccess.ShouldBeTrue();
        var second = await store.InsertAsync("payloads", json);
        second.IsFailed.ShouldBeTrue();
        second.HasError<DuplicateKeyError>().ShouldBeTrue();
    }

    [Test]
    public async Task GetById_Missing_ReturnsDocumentNotFoundError()
    {
        await using var host = CreateStjHost("MissingIdDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("MissingIdDB"));
        var result = await store.GetByIdAsync("payloads", ObjectId.GenerateNewId());
        result.IsFailed.ShouldBeTrue();
        result.HasError<DocumentNotFoundError>().ShouldBeTrue();
    }

    [Test]
    public async Task ReadPath_ReturnsNestedAndArrayContent()
    {
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
        insert.IsSuccess.ShouldBeTrue();

        var options = new JsonBsonConversionOptions { OutputMode = JsonBsonOutputMode.CanonicalExtendedJson };
        var loaded = await store.GetJsonDocumentByIdAsync("payloads", insert.Value["_id"], options);
        loaded.IsSuccess.ShouldBeTrue();
        using (loaded.Value)
        {
            loaded.Value.RootElement.GetProperty("child").GetProperty("n")
                .GetProperty("$numberInt").GetString().ShouldBe("3");
            loaded.Value.RootElement.GetProperty("items")[0]
                .GetProperty("$numberLong").GetString().ShouldBe("2");
        }

        var element = await store.GetJsonElementByIdAsync("payloads", insert.Value["_id"], options);
        element.IsSuccess.ShouldBeTrue();
        element.Value.GetProperty("items")[1].GetString().ShouldBe("x");
    }

    [Test]
    public async Task InsertString_RootArrayOrScalar_Fails()
    {
        await using var host = CreateStjHost("RootStringDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("RootStringDB"));

        var arrayRoot = await store.InsertAsync("payloads", "[1,2]");
        arrayRoot.IsFailed.ShouldBeTrue();
        arrayRoot.Errors[0].Message.ShouldContain("JSON root must be a document");

        var scalarRoot = await store.InsertAsync("payloads", "42");
        scalarRoot.IsFailed.ShouldBeTrue();
        scalarRoot.Errors[0].Message.ShouldContain("JSON root must be a document");
    }

    [Test]
    public async Task InsertOversizedBsonDocument_ReturnsDocumentTooLargeError()
    {
        await using var host = CreateStjHost("TooLargeDB");
        var store = host.StoreFactory.Create(new MongoDatabaseKey("TooLargeDB"));
        var document = new BsonDocument
        {
            ["_id"] = ObjectId.GenerateNewId(),
            ["blob"] = new BsonBinaryData(new byte[16 * 1024 * 1024])
        };

        var result = await store.InsertAsync("payloads", document);
        result.IsFailed.ShouldBeTrue();
        result.HasError<DocumentTooLargeError>().ShouldBeTrue();
    }

    [Test]
    public async Task Newtonsoft_GetJTokenByIdAsync_ReturnsNestedContent()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_container.GetConnectionString()))
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
        insert.IsSuccess.ShouldBeTrue(string.Join(", ", insert.Errors));

        var token = await store.GetJTokenByIdAsync(
            "payloads",
            insert.Value["_id"],
            new JsonBsonConversionOptions { OutputMode = JsonBsonOutputMode.CanonicalExtendedJson });
        token.IsSuccess.ShouldBeTrue();
        token.Value["items"]![0]!["$numberInt"]!.Value<string>().ShouldBe("4");
        token.Value["items"]![1]!.Value<string>().ShouldBe("y");
    }

    [Test]
    public async Task Newtonsoft_InsertAsync_TypeNameHandling_FailsBeforeWrite()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_container.GetConnectionString()))
            .AddDatabase("NsTypeNameDB", db => db.OnCluster("primary")));
        services.AddNewtonsoftJsonDocuments();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider
            .GetRequiredService<INewtonsoftJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("NsTypeNameDB"));

        var result = await store.InsertAsync(
            "payloads",
            JObject.Parse("""{"v":1}"""),
            serializerSettings: new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("TypeNameHandling");
    }

    [Test]
    public async Task Newtonsoft_DateLikeString_AndHighPrecisionDecimal_Survive()
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_container.GetConnectionString()))
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
        var insert = await store.InsertAsync("payloads", token, options);
        insert.IsSuccess.ShouldBeTrue(string.Join(", ", insert.Errors));
        insert.Value["when"].BsonType.ShouldBe(BsonType.String);
        insert.Value["money"].BsonType.ShouldBe(BsonType.Decimal128);

        var loaded = await store.GetJObjectByIdAsync("payloads", insert.Value["_id"]);
        loaded.IsSuccess.ShouldBeTrue();
        loaded.Value["when"]!.Type.ShouldBe(JTokenType.String);
        loaded.Value["when"]!.Value<string>().ShouldBe("2020-01-01T00:00:00Z");
    }

    [Test]
    public void Newtonsoft_TypeNameHandlingAuto_IsRejected()
    {
        var converter = new Dilcore.MongoDB.Json.BsonJsonConverter();
        var token = JObject.Parse("""{"v":1}""");
        var result = token.ToBson(
            converter,
            serializerSettings: new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("TypeNameHandling");
    }

    private StjHost CreateStjHost(string databaseName)
    {
        var services = new ServiceCollection();
        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(_container.GetConnectionString()))
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
