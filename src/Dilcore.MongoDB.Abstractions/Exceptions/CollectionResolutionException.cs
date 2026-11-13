namespace Dilcore.MongoDB.Abstractions.Exceptions;

/// <summary>
/// Thrown when a streaming enumeration cannot resolve its collection.
/// Finite-query streaming remains exception-based until the M4 streaming redesign.
/// </summary>
public sealed class CollectionResolutionException : Exception
{
    public CollectionResolutionException(string message)
        : base(message)
    {
    }
}
