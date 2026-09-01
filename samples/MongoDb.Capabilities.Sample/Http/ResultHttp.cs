using Dilcore.MongoDB.Abstractions.Results;
using FluentResults;

namespace MongoDb.Capabilities.Sample.Http;

internal static class ResultHttp
{
    public static IResult ToHttp<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        return ToFailure(result);
    }

    public static IResult ToHttp(Result result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok();
        }

        return ToFailure(result);
    }

    private static IResult ToFailure(ResultBase result)
    {
        if (result.HasError<DocumentNotFoundError>())
        {
            return Results.NotFound(result.Errors);
        }

        if (result.HasError<ConcurrencyConflictError>())
        {
            return Results.Conflict(result.Errors);
        }

        if (result.HasError<TransactionBudgetExceededError>()
            || result.HasError<CrossClusterOperationError>())
        {
            return Results.BadRequest(result.Errors);
        }

        if (result.HasError<BulkWritePartialFailureError>())
        {
            return Results.UnprocessableEntity(result.Errors);
        }

        return Results.BadRequest(result.Errors);
    }
}
