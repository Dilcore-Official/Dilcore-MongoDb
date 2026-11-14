namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// An optimistic concurrency check failed (ETag mismatch or missing document).
/// </summary>
public sealed class ConcurrencyConflictError : MongoOperationError
{
    /// <summary>
    /// Value of <see cref="Code"/> for this error type.
    /// </summary>
    public const string ErrorCode = "concurrency_conflict";

    /// <summary>
    /// Creates a concurrency conflict error with an optional custom message.
    /// </summary>
    /// <param name="message">Optional message; a default is used when null.</param>
    public ConcurrencyConflictError(string? message = null)
        : base(message ?? "The document was modified concurrently.")
    {
    }

    /// <inheritdoc />
    public override string Code => ErrorCode;
}