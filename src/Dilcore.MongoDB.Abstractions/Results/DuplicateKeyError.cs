namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// A write violated a unique index constraint.
/// </summary>
public sealed class DuplicateKeyError : MongoOperationError
{
    /// <summary>
    /// Value of <see cref="Code"/> for this error type.
    /// </summary>
    public const string ErrorCode = "duplicate_key";

    /// <summary>
    /// Creates a duplicate key error with an optional custom message.
    /// </summary>
    /// <param name="message">Optional message; a default is used when null.</param>
    public DuplicateKeyError(string? message = null)
        : base(message ?? "A document with the same unique key already exists.")
    {
    }

    /// <inheritdoc />
    public override string Code => ErrorCode;
}