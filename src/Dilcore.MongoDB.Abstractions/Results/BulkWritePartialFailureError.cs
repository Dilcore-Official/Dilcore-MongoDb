namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// A bulk write completed with one or more item failures.
/// </summary>
public sealed class BulkWritePartialFailureError : MongoOperationError
{
    /// <summary>
    /// Value of <see cref="Code"/> for this error type.
    /// </summary>
    public const string ErrorCode = "bulk_write_partial_failure";

    /// <summary>
    /// Creates an error describing per-index bulk write outcomes.
    /// </summary>
    /// <param name="items">Result for each bulk write index.</param>
    /// <param name="message">Optional message; a default is used when null.</param>
    public BulkWritePartialFailureError(
        IReadOnlyList<BulkWriteItemResult> items,
        string? message = null)
        : base(message ?? "One or more bulk write items failed.")
    {
        Items = items;
    }

    /// <summary>
    /// Per-index success or failure details from the bulk write.
    /// </summary>
    public IReadOnlyList<BulkWriteItemResult> Items { get; }

    /// <inheritdoc />
    public override string Code => ErrorCode;
}