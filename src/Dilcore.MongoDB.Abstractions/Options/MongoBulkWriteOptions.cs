namespace Dilcore.MongoDB.Abstractions.Options;

/// <summary>
/// Client-side options for generic bulk writes.
/// </summary>
public sealed class MongoBulkWriteOptions
{
    public bool IsOrdered { get; init; } = true;

    /// <summary>
    /// Maximum write models per driver batch. Null uses a size-based default.
    /// </summary>
    public int? MaxOperationsPerBatch { get; init; }
}
