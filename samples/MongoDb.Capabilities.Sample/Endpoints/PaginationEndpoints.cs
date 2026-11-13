using Dilcore.MongoDB.Abstractions.Options;
using Dilcore.MongoDB.Abstractions.Repositories;
using MongoDb.Capabilities.Sample.Documents;
using MongoDb.Capabilities.Sample.Http;
using MongoDB.Driver;

namespace MongoDb.Capabilities.Sample.Endpoints;

internal static class PaginationEndpoints
{
    public static void MapPaginationEndpoints(this WebApplication app)
    {
        app.MapGet("/orders/pages", async (
                IGenericRepository<Order> repository,
                string? cursor,
                int pageSize,
                CancellationToken ct) =>
            {
                var result = await repository.GetPageAsync(new KeysetPageRequest<Order>
                {
                    Filter = Builders<Order>.Filter.Empty,
                    Sort = Builders<Order>.Sort.Ascending(x => x.Id),
                    PageSize = pageSize > 0 ? pageSize : 2,
                    Cursor = cursor
                }, ct);

                return ResultHttp.ToHttp(result);
            })
            .WithTags("Keyset paging");
    }
}
