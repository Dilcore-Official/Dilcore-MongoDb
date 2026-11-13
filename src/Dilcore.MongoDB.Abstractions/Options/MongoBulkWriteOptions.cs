namespace Dilcore.MongoDB.Abstractions.Options;

/// <summary>
/// Client-side options for generic bulk writes.
/// </summary>
public sealed class MongoBulkWriteOptions
{
    /// <summary>
    /// When <see langword="true"/> (default), the driver stops at the first write error; when <see langword="false"/>, remaining operations still run.
    /// </summary>
    public bool IsOrdered { get; init; } = true;

    /// <summary>
    /// Maximum write models per driver batch. Null uses a size-based default.
    /// </summary>
    public int? MaxOperationsPerBatch { get; init; }
}