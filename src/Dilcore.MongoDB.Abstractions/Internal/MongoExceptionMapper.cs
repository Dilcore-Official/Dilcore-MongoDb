using Dilcore.MongoDB.Abstractions.Results;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Internal;

internal static class MongoExceptionMapper
{
    public static Error ToError(Exception exception)
    {
        return exception switch
        {
            MongoWriteConcernException writeConcern => new WriteConcernFailureError(writeConcern.Message),
            MongoWriteException write => MapWrite(write),
            MongoBulkWriteException bulk => MapBulk(bulk),
            MongoCommandException command => MapCommand(command),
            MongoException mongo => MapMongo(mongo),
            _ => new Error(exception.Message)
        };
    }

    public static Result Fail(Exception exception) => Result.Fail(ToError(exception));

    public static Result<T> Fail<T>(Exception exception) => Result.Fail<T>(ToError(exception));

    private static Error MapWrite(MongoWriteException exception)
    {
        var code = exception.WriteError?.Code ?? 0;
        if (IsDuplicateKey(code, exception.Message))
        {
            return new DuplicateKeyError(exception.Message);
        }

        if (IsDocumentTooLarge(code, exception.Message))
        {
            return new DocumentTooLargeError(exception.Message);
        }

        return MapMongo(exception);
    }

    private static Error MapBulk(MongoBulkWriteException exception)
    {
        var items = new List<BulkWriteItemResult>();
        if (exception.WriteErrors is not null)
        {
            foreach (var error in exception.WriteErrors)
            {
                items.Add(new BulkWriteItemResult(error.Index, succeeded: false, error.Message));
            }
        }

        if (items.Exists(item => IsDuplicateKey(0, item.ErrorMessage ?? string.Empty))
            || exception.WriteErrors?.Any(error => IsDuplicateKey(error.Code, error.Message)) == true)
        {
            var duplicate = exception.WriteErrors?.FirstOrDefault(error => IsDuplicateKey(error.Code, error.Message));
            if (duplicate is not null && items.Count == 1)
            {
                return new DuplicateKeyError(duplicate.Message);
            }
        }

        if (items.Count == 0)
        {
            return MapMongo(exception);
        }

        return new BulkWritePartialFailureError(items, exception.Message);
    }

    private static Error MapCommand(MongoCommandException exception)
    {
        if (IsDuplicateKey(exception.Code, exception.Message))
        {
            return new DuplicateKeyError(exception.Message);
        }

        if (IsDocumentTooLarge(exception.Code, exception.Message))
        {
            return new DocumentTooLargeError(exception.Message);
        }

        return MapMongo(exception);
    }

    private static Error MapMongo(MongoException exception)
    {
        if (exception.HasErrorLabel("TransientTransactionError")
            || exception.HasErrorLabel("UnknownTransactionCommitResult"))
        {
            return new TransientWriteError(exception.Message);
        }

        if (IsDocumentTooLarge(0, exception.Message))
        {
            return new DocumentTooLargeError(exception.Message);
        }

        return new Error(exception.Message);
    }

    private static bool IsDuplicateKey(int code, string message)
        => code is 11000 or 11001 || message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase);

    private static bool IsDocumentTooLarge(int code, string message)
        => code is 10334 || message.Contains("object to insert too large", StringComparison.OrdinalIgnoreCase)
            || message.Contains("BSONObj size", StringComparison.OrdinalIgnoreCase);
}
