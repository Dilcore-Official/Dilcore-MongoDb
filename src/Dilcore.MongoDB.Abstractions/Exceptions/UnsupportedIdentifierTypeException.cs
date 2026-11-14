namespace Dilcore.MongoDB.Abstractions.Exceptions;

/// <summary>
/// Thrown when auto-generating an identifier for an <see cref="IDocumentEntity{TId}"/> whose
/// identifier type has no built-in generator.
/// </summary>
public sealed class UnsupportedIdentifierTypeException : Exception
{
    /// <summary>
    /// Initializes a new instance for an identifier type that cannot be auto-generated.
    /// </summary>
    /// <param name="identifierType">The document identifier CLR type.</param>
    public UnsupportedIdentifierTypeException(Type identifierType)
        : base(
            $"Cannot auto-generate an identifier of type '{identifierType.FullName}'. " +
            "Assign Id before calling StoreAsync, or use Guid / ObjectId.")
    {
        IdentifierType = identifierType;
    }

    /// <summary>
    /// The identifier type that has no built-in generator.
    /// </summary>
    public Type IdentifierType { get; }
}