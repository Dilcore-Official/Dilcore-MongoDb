namespace Dilcore.MongoDB.Abstractions.Provisioning;

/// <summary>
/// Result for a single provisioning step.
/// </summary>
public sealed class ProvisioningStepResult
{
    /// <summary>
    /// Step identifier (for example binding key and resource kind).
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Action taken or planned (for example create, skip, would-create).
    /// </summary>
    public required string Action { get; init; }

    /// <summary>
    /// Optional human-readable detail (for example resolved collection name).
    /// </summary>
    public string? Details { get; init; }
}