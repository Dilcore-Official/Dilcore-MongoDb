using FluentResults;
using Shouldly;

namespace Dilcore.DocumentDb.Abstractions.UnitTests.Infrastructure;

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
}
