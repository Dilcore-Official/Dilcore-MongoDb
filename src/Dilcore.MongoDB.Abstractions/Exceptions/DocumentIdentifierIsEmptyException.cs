namespace Dilcore.MongoDB.Abstractions.Exceptions;

/// <summary>
/// Thrown when an operation requires a non-empty document <c>_id</c> (see <see cref="Extensions.DocumentEntityExtensions.CheckId{TDocument}"/>).
/// </summary>
public class DocumentIdentifierIsEmptyException() : Exception("Document identifier is empty");