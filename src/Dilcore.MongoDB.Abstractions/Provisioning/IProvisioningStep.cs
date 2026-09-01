using Dilcore.MongoDB.Abstractions;
using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Provisioning;

/// <summary>
/// Extension step for <see cref="IMongoDbProvisioner"/>. Built-in M3 steps cover
/// collections and indexes; apps (and later M7 search/vector helpers) register extra
/// steps in DI without changing the runner.
/// </summary>
public interface IProvisioningStep
{
    string Name { get; }

    Task<Result<ProvisioningStepResult>> ExecuteAsync(
        IMongoDatabaseResolver databaseResolver,
        bool apply,
        CancellationToken cancellationToken);
}
