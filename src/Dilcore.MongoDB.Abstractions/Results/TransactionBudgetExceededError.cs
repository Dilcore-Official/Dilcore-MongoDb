namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// Client-side transaction limits (operations, bytes, or time) were exceeded.
/// </summary>
public sealed class TransactionBudgetExceededError : MongoOperationError
{
    /// <summary>
    /// Value of <see cref="Code"/> for this error type.
    /// </summary>
    public const string ErrorCode = "transaction_budget_exceeded";

    /// <summary>
    /// Creates a transaction budget error with an optional custom message.
    /// </summary>
    /// <param name="message">Optional message; a default is used when null.</param>
    public TransactionBudgetExceededError(string? message = null)
        : base(message ?? "The client-side transaction budget was exceeded.")
    {
    }

    /// <inheritdoc />
    public override string Code => ErrorCode;
}