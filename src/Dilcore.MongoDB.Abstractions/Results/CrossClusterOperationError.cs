namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// A repository operation targeted a cluster different from the active transaction.
/// </summary>
public sealed class CrossClusterOperationError : MongoOperationError
{
    /// <summary>
    /// Value of <see cref="Code"/> for this error type.
    /// </summary>
    public const string ErrorCode = "cross_cluster_operation";

    /// <summary>
    /// Creates a cross-cluster error with an optional custom message.
    /// </summary>
    /// <param name="message">Optional message; a default is used when null.</param>
    public CrossClusterOperationError(string? message = null)
        : base(message ?? "The operation targets a different MongoDB cluster than the current transaction.")
    {
    }

    /// <inheritdoc />
    public override string Code => ErrorCode;
}