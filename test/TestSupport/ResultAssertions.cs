using FluentResults;
using Shouldly;

namespace Dilcore.MongoDB.TestSupport;

public static class ResultAssertions
{
    public static void ShouldBeSuccess(this Result result)
    {
        if (result.IsFailed)
        {
            result.IsSuccess.ShouldBeTrue($"Expected success but failed with errors: {string.Join(", ", result.Errors)}");
        }
    }

    public static void ShouldBeSuccess<T>(this Result<T> result)
    {
        if (result.IsFailed)
        {
            result.IsSuccess.ShouldBeTrue($"Expected success but failed with errors: {string.Join(", ", result.Errors)}");
        }
    }

    public static void ShouldBeFailure(this Result result)
    {
        result.IsSuccess.ShouldBeFalse("Expected failure but succeeded");
    }

    public static void ShouldBeFailure<T>(this Result<T> result)
    {
        result.IsSuccess.ShouldBeFalse("Expected failure but succeeded");
    }

    public static TError ShouldHaveError<TError>(this ResultBase result)
        where TError : class, IError
    {
        var error = result.Errors.OfType<TError>().FirstOrDefault();
        error.ShouldNotBeNull($"Expected {typeof(TError).Name} but got: {string.Join(", ", result.Errors.Select(e => e.GetType().Name + ": " + e.Message))}");
        return error!;
    }
}
