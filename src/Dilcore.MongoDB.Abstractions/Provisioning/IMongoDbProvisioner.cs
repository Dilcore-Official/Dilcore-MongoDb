using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Provisioning;

/// <summary>
/// Runs registered provisioning steps against MongoDB databases (collections, indexes, and DI-registered extras).
/// </summary>
public interface IMongoDbProvisioner
{
    /// <summary>
    /// Simulates provisioning and returns the steps that would run without mutating the server.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result containing a <see cref="ProvisioningReport"/> with <see cref="ProvisioningReport.Applied"/> set to <see langword="false"/>;
    /// otherwise a failed result (for example namespace resolution or driver errors).
    /// </returns>
    Task<Result<ProvisioningReport>> DryRunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies provisioning steps and returns the actions performed.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    /// <returns>
    /// A successful result containing a <see cref="ProvisioningReport"/> with <see cref="ProvisioningReport.Applied"/> set to <see langword="true"/>;
    /// otherwise a failed result.
    /// </returns>
    Task<Result<ProvisioningReport>> ApplyAsync(CancellationToken cancellationToken = default);
}