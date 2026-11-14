namespace Dilcore.MongoDB.Abstractions.Exceptions;

/// <summary>
/// Thrown when a streaming enumeration cannot resolve its collection.
/// Finite-query streaming remains exception-based until the M4 streaming redesign.
/// </summary>
public sealed class CollectionResolutionException : Exception
{
    /// <summary>
    /// Initializes a new instance with the specified failure message.
    /// </summary>
    /// <param name="message">Description of why collection resolution failed.</param>
    public CollectionResolutionException(string message)
        : base(message)
    {
    }
}