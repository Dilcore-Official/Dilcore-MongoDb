using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.NewtonsoftJson;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dilcore.MongoDB.Json.IntegrationTests.Examples;

/// <summary>Proves docs/guides/json-newtonsoft.md.</summary>
public class NewtonsoftJsonExampleTests : ExampleIntegrationTests
{
    public sealed class PayloadService(INewtonsoftJsonDocumentStoreFactory stores)
    {
        public async Task<Result<JObject>> RoundTripAsync(string json, CancellationToken cancellationToken = default)
        {
            var store = stores.Create(new MongoDatabaseKey("app"));

            using var reader = new JsonTextReader(new StringReader(json))
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Decimal
            };
            JObject payload;
            try
            {
                payload = JObject.Load(reader);
            }
            catch (JsonReaderException exception)
            {
                return Result.Fail<JObject>(new JsonConversionError(exception.Message));
            }

            var inserted = await store.InsertAsync("payloads", payload, cancellationToken: cancellationToken);
            if (inserted.IsFailed)
            {
                return inserted.ToResult();
            }

            // Reads return Canonical Extended JSON by default. Ask for relaxed output to get plain JSON back.
            var relaxed = new JsonBsonConversionOptions { OutputMode = JsonBsonOutputMode.RelaxedExtendedJson };

            return await store.GetJObjectByIdAsync("payloads", inserted.Value["_id"], relaxed, cancellationToken);
        }
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db => db.OnCluster("primary")));

        services.AddNewtonsoftJsonDocuments();

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task RoundTrip_KeepsDateLikeStrings()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<PayloadService>(scope.ServiceProvider);

        // Act
        var result = await service.RoundTripAsync("""{ "when": "2024-01-02T03:04:05Z", "n": 3 }""", CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        result.Value["when"]!.Type.ShouldBe(JTokenType.String);
        result.Value["when"]!.Value<string>().ShouldBe("2024-01-02T03:04:05Z");
        result.Value["n"]!.Value<int>().ShouldBe(3);
    }

    [Test]
    public async Task DuplicateId_ReturnsDuplicateKeyError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<INewtonsoftJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("app"));
        var payload = JObject.Parse("""{ "_id": 1 }""");
        (await store.InsertAsync("payloads", payload, cancellationToken: CancellationToken.None)).ShouldBeSuccess();

        // Act
        var inserted = await store.InsertAsync("payloads", payload, cancellationToken: CancellationToken.None);

        // Assert
        inserted.HasError<DuplicateKeyError>().ShouldBeTrue();
    }

    [Test]
    public async Task TypeNameHandling_IsRejected()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<INewtonsoftJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("app"));
        var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto };

        // Act
        var inserted = await store.InsertAsync(
            "payloads", JObject.Parse("""{ "a": 1 }"""), serializerSettings: settings, cancellationToken: CancellationToken.None);

        // Assert
        inserted.ShouldBeFailure();
        inserted.Errors.ShouldContain(e => e.Message.Contains("TypeNameHandling"));
    }

    [Test]
    public async Task RoundTrip_MalformedJson_ReturnsJsonConversionError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<PayloadService>(scope.ServiceProvider);

        // Act
        var result = await service.RoundTripAsync("{ nope", CancellationToken.None);

        // Assert
        result.HasError<JsonConversionError>().ShouldBeTrue();
    }

    [Test]
    public async Task MalformedJson_ReturnsJsonConversionError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<INewtonsoftJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("app"));

        // Act
        var inserted = await store.InsertAsync("payloads", "{ nope");

        // Assert
        inserted.HasError<JsonConversionError>().ShouldBeTrue();
    }

    [Test]
    public void RegistrationBeforeAddMongoDb_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddNewtonsoftJsonDocuments();

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }
}