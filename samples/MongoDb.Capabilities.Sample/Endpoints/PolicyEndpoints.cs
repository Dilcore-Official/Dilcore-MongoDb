using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Repositories;
using MongoDb.Capabilities.Sample.Documents;
using MongoDb.Capabilities.Sample.Http;
using MongoDB.Driver;

namespace MongoDb.Capabilities.Sample.Endpoints;

internal static class PolicyEndpoints
{
    public static void MapPolicyEndpoints(this WebApplication app)
    {
        var orders = app.MapGroup("/orders").WithTags("Policies and CRUD");

        orders.MapPost("/", async (IGenericRepository<Order> repository, Order order, CancellationToken ct) =>
        {
            // StoreAsync applies Guid v7 (when configured), ETag, and audit stamps after the write is acknowledged.
            var result = await repository.StoreAsync(order, ct);
            return ResultHttp.ToHttp(result);
        });

        orders.MapGet("/{id:guid}", async (IGenericRepository<Order> repository, Guid id, CancellationToken ct) =>
        {
            // GetAsync returns DocumentNotFoundError instead of a successful null value.
            var result = await repository.GetAsync(id, ct);
            return ResultHttp.ToHttp(result);
        });

        orders.MapPut("/{id:guid}", async (IGenericRepository<Order> repository, Guid id, Order order, CancellationToken ct) =>
        {
            // ReplaceAsync replaces stored state. Send the current ETag for optimistic concurrency.
            order.Id = id;
            var result = await repository.ReplaceAsync(order, ct);
            return ResultHttp.ToHttp(result);
        });

        orders.MapPost("/{id:guid}/snapshot", async (IGenericRepository<Order> repository, Guid id, Order order, CancellationToken ct) =>
        {
            // UpdateSnapshotAsync $set-s mutable fields and excludes _id.
            order.Id = id;
            var result = await repository.UpdateSnapshotAsync(order, ct);
            return ResultHttp.ToHttp(result);
        });

        orders.MapPatch("/{id:guid}", async (IGenericRepository<Order> repository, Guid id, int quantity, CancellationToken ct) =>
        {
            // PatchAsync is a caller-supplied UpdateDefinition (not a full-document $set).
            var result = await repository.PatchAsync(
                Builders<Order>.Filter.Eq(x => x.Id, id),
                Builders<Order>.Update.Set(x => x.Quantity, quantity),
                ct);
            return ResultHttp.ToHttp(result);
        });

        orders.MapDelete("/{id:guid}", async (IGenericRepository<Order> repository, Guid id, long eTag, CancellationToken ct) =>
        {
            // Soft-delete when WithSoftDelete() is on the binding. Wrong ETag → ConcurrencyConflictError (409).
            var result = await repository.DeleteAsync(id, eTag, ct);
            return ResultHttp.ToHttp(result);
        });

        orders.MapPost("/{id:guid}/restore", async (IGenericRepository<Order> repository, Guid id, CancellationToken ct) =>
        {
            var result = await repository.RestoreAsync(Builders<Order>.Filter.Eq(x => x.Id, id), ct);
            return ResultHttp.ToHttp(result);
        });

        orders.MapDelete("/{id:guid}/purge", async (IGenericRepository<Order> repository, Guid id, CancellationToken ct) =>
        {
            // PurgeAsync hard-deletes, including already soft-deleted documents.
            var result = await repository.PurgeAsync(Builders<Order>.Filter.Eq(x => x.Id, id), ct);
            return ResultHttp.ToHttp(result);
        });

        orders.MapGet("/projections", async (IGenericProjectionRepository<Order> repository, CancellationToken ct) =>
        {
            var result = await repository.GetListAsync(x => new OrderSummary
            {
                Id = x.Id,
                Sku = x.Sku,
                Quantity = x.Quantity
            }, ct);
            return ResultHttp.ToHttp(result);
        });

        var notes = app.MapGroup("/notes").WithTags("Minimal document");
        notes.MapGet("/", async (IGenericRepository<Note> repository, CancellationToken ct) =>
            ResultHttp.ToHttp(await repository.GetListAsync(ct)));
        notes.MapPost("/", async (IGenericRepository<Note> repository, Note note, CancellationToken ct) =>
            ResultHttp.ToHttp(await repository.StoreAsync(note, ct)));
    }
}
