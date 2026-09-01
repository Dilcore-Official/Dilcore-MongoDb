using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Provisioning;
using FluentResults;

namespace MongoDb.Capabilities.Sample.Provisioning;

/// <summary>
/// Extra provisioning work registered in DI. Built-in steps already create collections and indexes
/// from document bindings; apps add <see cref="IProvisioningStep"/> for anything else (time series, etc.).
/// </summary>
public sealed class SampleProvisioningStep : IProvisioningStep
{
    public const string DatabaseName = "CapabilitiesDB";

    public string Name => "sample-metadata";

    public async Task<Result<ProvisioningStepResult>> ExecuteAsync(
        IMongoDatabaseResolver databaseResolver,
        bool apply,
        CancellationToken cancellationToken)
    {
        var database = await databaseResolver.GetDatabaseAsync(
            new MongoDatabaseKey(DatabaseName),
            cancellationToken);
        if (database.IsFailed)
        {
            return database.ToResult();
        }

        return Result.Ok(new ProvisioningStepResult
        {
            Name = Name,
            Action = apply ? "applied" : "would-apply",
            Details = $"Custom step against {database.Value.DatabaseNamespace.DatabaseName}."
        });
    }
}
