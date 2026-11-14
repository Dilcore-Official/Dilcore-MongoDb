namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// A single document exceeded the MongoDB 16 MiB BSON size limit.
/// </summary>
public sealed class DocumentTooLargeError : MongoOperationError
{
    /// <summary>
    /// Value of <see cref="Code"/> for this error type.
    /// </summary>
    public const string ErrorCode = "document_too_large";

    /// <summary>
    /// Creates a document-too-large error with an optional custom message.
    /// </summary>
    /// <param name="message">Optional message; a default is used when null.</param>
    public DocumentTooLargeError(string? message = null)
        : base(message ?? "A BSON document exceeded the 16 MiB limit.")
    {
    }

    /// <inheritdoc />
    public override string Code => ErrorCode;
}