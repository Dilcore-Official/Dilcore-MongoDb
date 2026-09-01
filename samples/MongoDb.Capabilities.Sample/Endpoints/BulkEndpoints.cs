using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Repositories;
using MongoDb.Capabilities.Sample.Documents;
using MongoDb.Capabilities.Sample.Http;

namespace MongoDb.Capabilities.Sample.Endpoints;

internal static class BulkEndpoints
{
    public static void MapBulkEndpoints(this WebApplication app)
    {
        app.MapPost("/orders/bulk", async (
                IGenericBulkRepository<Order> repository,
                Order[] orders,
                CancellationToken ct) =>
            {
                // Unordered bulk continues after item failures and returns BulkWritePartialFailureError.
                var result = await repository.BulkStoreAsync(
                    orders,
                    new MongoBulkWriteOptions { IsOrdered = false, MaxOperationsPerBatch = 100 },
                    ct);
                return ResultHttp.ToHttp(result);
            })
            .WithTags("Bulk");
    }
}
