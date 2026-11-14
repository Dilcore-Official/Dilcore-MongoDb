using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// Base typed failure for expected MongoDB operation outcomes.
/// </summary>
public abstract class MongoOperationError : Error
{
    /// <summary>
    /// Initializes the error with a human-readable message.
    /// </summary>
    /// <param name="message">Error description.</param>
    protected MongoOperationError(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Stable machine-readable code for this error kind.
    /// </summary>
    public abstract string Code { get; }
}