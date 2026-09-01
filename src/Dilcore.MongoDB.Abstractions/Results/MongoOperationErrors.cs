using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Results;

/// <summary>
/// Base typed failure for expected MongoDB operation outcomes.
/// </summary>
public abstract class MongoOperationError : Error
{
    protected MongoOperationError(string message)
        : base(message)
    {
    }

    public abstract string Code { get; }
}

public sealed class DocumentNotFoundError : MongoOperationError
{
    public const string ErrorCode = "document_not_found";

    public DocumentNotFoundError(string? message = null)
        : base(message ?? "The document was not found.")
    {
    }

    public override string Code => ErrorCode;
}

public sealed class ConcurrencyConflictError : MongoOperationError
{
    public const string ErrorCode = "concurrency_conflict";

    public ConcurrencyConflictError(string? message = null)
        : base(message ?? "The document was modified concurrently.")
    {
    }

    public override string Code => ErrorCode;
}

public sealed class DuplicateKeyError : MongoOperationError
{
    public const string ErrorCode = "duplicate_key";

    public DuplicateKeyError(string? message = null)
        : base(message ?? "A document with the same unique key already exists.")
    {
    }

    public override string Code => ErrorCode;
}

public sealed class TransientWriteError : MongoOperationError
{
    public const string ErrorCode = "transient_write";

    public TransientWriteError(string? message = null)
        : base(message ?? "The write failed due to a transient MongoDB error.")
    {
    }

    public override string Code => ErrorCode;
}

public sealed class WriteConcernFailureError : MongoOperationError
{
    public const string ErrorCode = "write_concern_failure";

    public WriteConcernFailureError(string? message = null)
        : base(message ?? "The write concern was not satisfied.")
    {
    }

    public override string Code => ErrorCode;
}

public sealed class DocumentTooLargeError : MongoOperationError
{
    public const string ErrorCode = "document_too_large";

    public DocumentTooLargeError(string? message = null)
        : base(message ?? "A BSON document exceeded the 16 MiB limit.")
    {
    }

    public override string Code => ErrorCode;
}

public sealed class CrossClusterOperationError : MongoOperationError
{
    public const string ErrorCode = "cross_cluster_operation";

    public CrossClusterOperationError(string? message = null)
        : base(message ?? "The operation targets a different MongoDB cluster than the current transaction.")
    {
    }

    public override string Code => ErrorCode;
}

public sealed class TransactionBudgetExceededError : MongoOperationError
{
    public const string ErrorCode = "transaction_budget_exceeded";

    public TransactionBudgetExceededError(string? message = null)
        : base(message ?? "The client-side transaction budget was exceeded.")
    {
    }

    public override string Code => ErrorCode;
}

public sealed class BulkWritePartialFailureError : MongoOperationError
{
    public const string ErrorCode = "bulk_write_partial_failure";

    public BulkWritePartialFailureError(
        IReadOnlyList<BulkWriteItemResult> items,
        string? message = null)
        : base(message ?? "One or more bulk write items failed.")
    {
        Items = items;
    }

    public IReadOnlyList<BulkWriteItemResult> Items { get; }

    public override string Code => ErrorCode;
}

public sealed class BulkWriteItemResult
{
    public BulkWriteItemResult(int index, bool succeeded, string? errorMessage = null)
    {
        Index = index;
        Succeeded = succeeded;
        ErrorMessage = errorMessage;
    }

    public int Index { get; }

    public bool Succeeded { get; }

    public string? ErrorMessage { get; }
}
