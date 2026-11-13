using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Json;
using Dilcore.MongoDB.SystemTextJson;
using MongoDb.Capabilities.Sample.Http;
using MongoDB.Bson;
using System.Text.Json;

namespace MongoDb.Capabilities.Sample.Endpoints;

internal static class JsonEndpoints
{
    public const string LogicalCollectionName = "payloads";

    public static void MapJsonEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/json").WithTags("JSON adapters");

        group.MapPost("/payloads", async (
            JsonDocumentStore store,
            JsonElement body,
            CancellationToken ct) =>
        {
            // JsonDocumentStore is constructed in Program.cs; AddMongoDb does not register it.
            // Canonical Extended JSON preserves BSON types ($oid, $numberLong, $numberDecimal, …).
            var options = new JsonBsonConversionOptions
            {
                OutputMode = JsonBsonOutputMode.CanonicalExtendedJson
            };
            var result = await store.InsertAsync(LogicalCollectionName, body, options, ct);
            return ResultHttp.ToHttp(result);
        });

        group.MapGet("/payloads/{id}", async (
            JsonDocumentStore store,
            string id,
            CancellationToken ct) =>
        {
            var options = new JsonBsonConversionOptions
            {
                OutputMode = JsonBsonOutputMode.CanonicalExtendedJson
            };
            var result = await store.GetJsonDocumentByIdAsync(
                LogicalCollectionName,
                ParseId(id),
                options,
                ct);
            if (result.IsFailed)
            {
                return ResultHttp.ToHttp(result);
            }

            using var document = result.Value;
            return Results.Content(document.RootElement.GetRawText(), "application/json");
        });
    }

    private static BsonValue ParseId(string id)
        => ObjectId.TryParse(id, out var objectId) ? objectId : BsonValue.Create(id);
}
