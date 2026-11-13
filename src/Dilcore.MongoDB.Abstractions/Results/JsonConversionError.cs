namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// BSON/JSON serialization or deserialization failed for a document field.
/// </summary>
public sealed class JsonConversionError : MongoOperationError
{
    /// <summary>
    /// Value of <see cref="Code"/> for this error type.
    /// </summary>
    public const string ErrorCode = "json_conversion";

    /// <summary>
    /// Creates a JSON conversion error with an optional custom message.
    /// </summary>
    /// <param name="message">Optional message; a default is used when null.</param>
    public JsonConversionError(string? message = null)
        : base(message ?? "The JSON value could not be converted.")
    {
    }

    /// <inheritdoc />
    public override string Code => ErrorCode;
}