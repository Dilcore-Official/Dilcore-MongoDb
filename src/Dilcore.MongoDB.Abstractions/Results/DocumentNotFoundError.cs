namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// The requested document does not exist or is not visible (for example soft-deleted).
/// </summary>
public sealed class DocumentNotFoundError : MongoOperationError
{
    /// <summary>
    /// Value of <see cref="Code"/> for this error type.
    /// </summary>
    public const string ErrorCode = "document_not_found";

    /// <summary>
    /// Creates a document-not-found error with an optional custom message.
    /// </summary>
    /// <param name="message">Optional message; a default is used when null.</param>
    public DocumentNotFoundError(string? message = null)
        : base(message ?? "The document was not found.")
    {
    }

    /// <inheritdoc />
    public override string Code => ErrorCode;
}