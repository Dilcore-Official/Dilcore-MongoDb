namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// A write failed with a driver-classified transient error (retry may succeed).
/// </summary>
public sealed class TransientWriteError : MongoOperationError
{
    /// <summary>
    /// Value of <see cref="Code"/> for this error type.
    /// </summary>
    public const string ErrorCode = "transient_write";

    /// <summary>
    /// Creates a transient write error with an optional custom message.
    /// </summary>
    /// <param name="message">Optional message; a default is used when null.</param>
    public TransientWriteError(string? message = null)
        : base(message ?? "The write failed due to a transient MongoDB error.")
    {
    }

    /// <inheritdoc />
    public override string Code => ErrorCode;
}