using System.Text.Json;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.SystemTextJson;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;

namespace Dilcore.MongoDB.Json.IntegrationTests.Examples;

/// <summary>Proves docs/guides/json-system-text-json.md.</summary>
public class SystemTextJsonExampleTests : ExampleIntegrationTests
{
    public sealed class PayloadService(ISystemTextJsonDocumentStoreFactory stores)
    {
        public async Task<Result<JsonElement>> RoundTripAsync(
            JsonElement payload,
            CancellationToken cancellationToken = default)
        {
            var store = stores.Create(new MongoDatabaseKey("app"));

            var inserted = await store.InsertAsync("payloads", payload, cancellationToken: cancellationToken);
            if (inserted.IsFailed)
            {
                return inserted.ToResult();
            }

            // Reads return Canonical Extended JSON by default (numbers become {"$numberInt": "3"}).
            // Ask for relaxed output to get plain JSON back.
            var relaxed = new JsonBsonConversionOptions { OutputMode = JsonBsonOutputMode.RelaxedExtendedJson };

            return await store.GetJsonElementByIdAsync("payloads", inserted.Value["_id"], relaxed, cancellationToken);
        }
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db => db.OnCluster("primary")));

        services.AddSystemTextJsonDocuments();

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task RoundTrip_ReturnsStoredPayload()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var service = ActivatorUtilities.CreateInstance<PayloadService>(scope.ServiceProvider);
        using var payload = JsonDocument.Parse("""{ "name": "a", "n": 3 }""");

        // Act
        var result = await service.RoundTripAsync(payload.RootElement, CancellationToken.None);

        // Assert
        result.ShouldBeSuccess();
        result.Value.GetProperty("name").GetString().ShouldBe("a");
        result.Value.GetProperty("n").GetInt32().ShouldBe(3);
    }

    [Test]
    public async Task DuplicateId_ReturnsDuplicateKeyError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISystemTextJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("app"));
        (await store.InsertAsync("payloads", """{ "_id": 1 }""", cancellationToken: CancellationToken.None)).ShouldBeSuccess();

        // Act
        var inserted = await store.InsertAsync("payloads", """{ "_id": 1 }""", cancellationToken: CancellationToken.None);

        // Assert
        inserted.HasError<DuplicateKeyError>().ShouldBeTrue();
    }

    [Test]
    public async Task MalformedJsonMissingIdAndNonObjectRoot_Fail()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISystemTextJsonDocumentStoreFactory>()
            .Create(new MongoDatabaseKey("app"));

        // Act
        var malformed = await store.InsertAsync("payloads", "{ nope");
        var notObject = await store.InsertAsync("payloads", "[1]");
        var missing = await store.GetJsonElementByIdAsync("payloads", 99);

        // Assert
        malformed.HasError<JsonConversionError>().ShouldBeTrue();
        notObject.ShouldBeFailure();
        notObject.Errors.ShouldContain(e => e.Message.Contains("JSON root must be a document."));
        missing.HasError<DocumentNotFoundError>().ShouldBeTrue();
    }

    [Test]
    public void RegistrationBeforeAddMongoDb_Throws()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var act = () => services.AddSystemTextJsonDocuments();

        // Assert
        Should.Throw<InvalidOperationException>(act);
    }
}