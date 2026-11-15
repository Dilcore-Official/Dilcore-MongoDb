using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Extensions;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace Dilcore.MongoDB.Json.IntegrationTests.Examples;

/// <summary>Proves docs/guides/json-conversion-options.md.</summary>
public class JsonConversionOptionsExampleTests : ExampleIntegrationTests
{
    public sealed class JsonRoundTrip(IBsonJsonConverter converter)
    {
        public Result<string> Normalize(string json)
        {
            var options = new JsonBsonConversionOptions
            {
                OutputMode = JsonBsonOutputMode.CanonicalExtendedJson,
                NumberMode = OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDouble,
                MaxDepth = 64,
                MaxUtf8Bytes = 16 * 1024 * 1024,
                AllowDuplicateNames = false,
                InterpretExtendedJson = true
            };

            var parsed = converter.Parse(json, options);
            if (parsed.IsFailed)
            {
                return parsed.ToResult();
            }

            return converter.ToJson(parsed.Value, options);
        }
    }

    private ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();

        services.AddMongoDb(mongo => mongo
            .AddCluster("primary", c => c.UseConnectionString(connectionString))
            .AddDatabase("app", db => db.OnCluster("primary")));

        return AcceptanceServiceProviderFactory.Create(services);
    }

    [Test]
    public async Task CanonicalMode_PreservesBsonTypes()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var roundTrip = ActivatorUtilities.CreateInstance<JsonRoundTrip>(scope.ServiceProvider);
        const string json = """{ "id": { "$oid": "573a1391f29313caabcd9701" }, "big": { "$numberLong": "5" }, "price": { "$numberDecimal": "1.10" }, "small": 7 }""";

        // Act
        var normalized = roundTrip.Normalize(json);
        var bson = BsonDocument.Parse(normalized.Value);

        // Assert
        normalized.ShouldBeSuccess();
        bson["id"].BsonType.ShouldBe(BsonType.ObjectId);
        bson["big"].BsonType.ShouldBe(BsonType.Int64);
        bson["price"].BsonType.ShouldBe(BsonType.Decimal128);
        bson["small"].BsonType.ShouldBe(BsonType.Int32);
    }

    [Test]
    public async Task MalformedJsonAndDuplicateNames_ReturnJsonConversionError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var roundTrip = ActivatorUtilities.CreateInstance<JsonRoundTrip>(scope.ServiceProvider);

        // Act
        var malformed = roundTrip.Normalize("{ not json");
        var duplicate = roundTrip.Normalize("""{ "a": 1, "a": 2 }""");

        // Assert
        malformed.HasError<JsonConversionError>().ShouldBeTrue();
        duplicate.HasError<JsonConversionError>().ShouldBeTrue();
    }

    [Test]
    public async Task PayloadOverMaxUtf8Bytes_ReturnsDocumentTooLargeError()
    {
        // Arrange
        await using var provider = BuildProvider();
        using var scope = provider.CreateScope();
        var converter = scope.ServiceProvider.GetRequiredService<IBsonJsonConverter>();
        var options = new JsonBsonConversionOptions { MaxUtf8Bytes = 10 };

        // Act
        var parsed = converter.Parse("""{ "name": "longer than ten bytes" }""", options);

        // Assert
        parsed.HasError<DocumentTooLargeError>().ShouldBeTrue();
    }
}