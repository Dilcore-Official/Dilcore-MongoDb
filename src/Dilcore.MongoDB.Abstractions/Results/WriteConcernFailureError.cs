namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// The cluster did not acknowledge the write per the configured write concern.
/// </summary>
public sealed class WriteConcernFailureError : MongoOperationError
{
    /// <summary>
    /// Value of <see cref="Code"/> for this error type.
    /// </summary>
    public const string ErrorCode = "write_concern_failure";

    /// <summary>
    /// Creates a write concern failure with an optional custom message.
    /// </summary>
    /// <param name="message">Optional message; a default is used when null.</param>
    public WriteConcernFailureError(string? message = null)
        : base(message ?? "The write concern was not satisfied.")
    {
    }

    /// <inheritdoc />
    public override string Code => ErrorCode;
}