using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Provisioning;

public interface IMongoDbProvisioner
{
    Task<Result<ProvisioningReport>> DryRunAsync(CancellationToken cancellationToken = default);

    Task<Result<ProvisioningReport>> ApplyAsync(CancellationToken cancellationToken = default);
}

public sealed class ProvisioningReport
{
    public required bool Applied { get; init; }

    public required IReadOnlyList<ProvisioningStepResult> Steps { get; init; }
}

public sealed class ProvisioningStepResult
{
    public required string Name { get; init; }

    public required string Action { get; init; }

    public string? Details { get; init; }
}
