using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Abstractions.Transactions;
using FluentResults;

namespace Dilcore.MongoDB.Internal;

internal sealed class TransactionBudgetGuard(MongoTransactionOptions options) : ITransactionBudgetGuard
{
    private int _operations;
    private int _bytes;
    private readonly DateTime _deadline = DateTime.UtcNow + options.TimeLimit;

    public void ResetAttempt()
    {
        _operations = 0;
        _bytes = 0;
    }

    public Result Reserve(int estimatedBytes)
    {
        if (DateTime.UtcNow > _deadline)
        {
            return Result.Fail(new TransactionBudgetExceededError("The transaction elapsed-time budget was exceeded."));
        }

        if (_operations + 1 > options.MaxOperations)
        {
            return Result.Fail(new TransactionBudgetExceededError("The transaction operation budget was exceeded."));
        }

        if (_bytes + estimatedBytes > options.MaxEstimatedBytes)
        {
            return Result.Fail(new TransactionBudgetExceededError(
                "The estimated BSON byte budget was exceeded. Totals are client estimates; MongoDB has no 16 MiB total-transaction cap."));
        }

        _operations++;
        _bytes += estimatedBytes;
        return Result.Ok();
    }
}
