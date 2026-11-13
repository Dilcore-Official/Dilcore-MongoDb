using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace MongoDb.Capabilities.Sample.Endpoints;

internal static class EscapeHatchEndpoints
{
    public static void MapEscapeHatchEndpoints(this WebApplication app)
    {
        app.MapGet("/escape/client", async (
                [FromKeyedServices("primary")] IMongoClient client,
                CancellationToken ct) =>
            {
                // Keyed IMongoClient is the same singleton Dilcore registered for the cluster.
                var names = await client.ListDatabaseNames().ToListAsync(ct);
                return Results.Ok(names);
            })
            .WithTags("Driver escape hatches");

        app.MapGet("/escape/collection", async (
                IMongoDbCollectionFactory factory,
                CancellationToken ct) =>
            {
                // Same namespace-resolution pipeline as typed repositories.
                var collection = await factory.GetCollectionAsync<Documents.Order>(
                    new MongoDocumentBindingKey("orders"),
                    ct);
                if (collection.IsFailed)
                {
                    return Results.BadRequest(collection.Errors);
                }

                return Results.Ok(collection.Value.CollectionNamespace.FullName);
            })
            .WithTags("Driver escape hatches");
    }
}
