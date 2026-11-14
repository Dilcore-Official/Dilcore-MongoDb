using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Provisioning;

/// <summary>
/// Extension step for <see cref="IMongoDbProvisioner"/>. Built-in M3 steps cover
/// collections and indexes; apps (and later M7 search/vector helpers) register extra
/// steps in DI without changing the runner.
/// </summary>
public interface IProvisioningStep
{
    /// <summary>
    /// Stable name of this step for reports and logging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Resolves targets via <paramref name="databaseResolver"/> and either simulates or applies the step.
    /// </summary>
    /// <param name="databaseResolver">Resolves tenant-aware database handles for bound keys.</param>
    /// <param name="apply"><see langword="true"/> to mutate MongoDB; <see langword="false"/> to describe planned actions only.</param>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result with step outcome metadata; otherwise a failed result (for example resolution or driver errors).
    /// </returns>
    Task<Result<ProvisioningStepResult>> ExecuteAsync(
        IMongoDatabaseResolver databaseResolver,
        bool apply,
        CancellationToken cancellationToken = default);
}