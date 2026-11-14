namespace Dilcore.MongoDB.Abstractions.Provisioning;

/// <summary>
/// Outcome of a provisioning run.
/// </summary>
public sealed class ProvisioningReport
{
    /// <summary>
    /// <see langword="true"/> when changes were applied; <see langword="false"/> for a dry run.
    /// </summary>
    public required bool Applied { get; init; }

    /// <summary>
    /// Ordered results for each provisioning step.
    /// </summary>
    public required IReadOnlyList<ProvisioningStepResult> Steps { get; init; }
}