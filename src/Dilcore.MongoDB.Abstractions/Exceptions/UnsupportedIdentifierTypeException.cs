namespace Dilcore.MongoDB.Abstractions.Exceptions;

/// <summary>
/// Thrown when auto-generating an identifier for a <see cref="IDocumentEntity{TId}"/> whose
/// <typeparamref name="TId"/> type has no built-in generator.
/// </summary>
public sealed class UnsupportedIdentifierTypeException : Exception
{
    public UnsupportedIdentifierTypeException(Type identifierType)
        : base(
            $"Cannot auto-generate an identifier of type '{identifierType.FullName}'. " +
            "Assign Id before calling StoreAsync, or use Guid / ObjectId.")
    {
        IdentifierType = identifierType;
    }

    public Type IdentifierType { get; }
}
