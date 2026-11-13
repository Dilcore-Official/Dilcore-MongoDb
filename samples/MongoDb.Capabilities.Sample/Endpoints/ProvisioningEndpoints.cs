using Dilcore.MongoDB.Abstractions.Provisioning;
using MongoDb.Capabilities.Sample.Http;

namespace MongoDb.Capabilities.Sample.Endpoints;

internal static class ProvisioningEndpoints
{
    public static void MapProvisioningEndpoints(this WebApplication app)
    {
        app.MapGet("/provisioning/dry-run", async (IMongoDbProvisioner provisioner, CancellationToken ct) =>
            {
                // DryRunAsync reports would-create vs skip without mutating the server.
                var result = await provisioner.DryRunAsync(ct);
                return ResultHttp.ToHttp(result);
            })
            .WithTags("Provisioning");
    }
}
