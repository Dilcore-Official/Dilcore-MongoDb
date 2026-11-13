namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// Outcome of a single operation within a bulk write batch.
/// </summary>
public sealed class BulkWriteItemResult
{
    /// <summary>
    /// Records the result for one bulk write index.
    /// </summary>
    /// <param name="index">Zero-based position in the bulk batch.</param>
    /// <param name="succeeded"><see langword="true"/> if the item succeeded.</param>
    /// <param name="errorMessage">Driver or library message when <paramref name="succeeded"/> is false.</param>
    public BulkWriteItemResult(int index, bool succeeded, string? errorMessage = null)
    {
        Index = index;
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Zero-based index of the bulk write operation.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Whether this bulk item completed successfully.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Failure detail when <see cref="Succeeded"/> is false.
    /// </summary>
    public string? ErrorMessage { get; }
}