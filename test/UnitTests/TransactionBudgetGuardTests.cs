using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Abstractions.Transactions;
using Dilcore.MongoDB.Internal;

namespace Dilcore.MongoDB.UnitTests;

public class TransactionBudgetGuardTests
{
    [Test]
    public void Reserve_AllowsExactlyMaxOperations_ThenFails()
    {
        // Arrange
        var guard = Guard(maxOperations: 2);
        var first = guard.Reserve(1);
        var second = guard.Reserve(1);

        // Act
        var third = guard.Reserve(1);

        // Assert
        first.ShouldBeSuccess();
        second.ShouldBeSuccess();
        third.ShouldBeFailure();
        third.ShouldHaveError<TransactionBudgetExceededError>()
            .Message.ShouldContain("operation");
    }

    [Test]
    public void Reserve_AllowsBytesUpToTheCap_AndRejectsTheNextByte()
    {
        // Arrange
        var guard = Guard(maxEstimatedBytes: 100);
        var withinCap = guard.Reserve(100);

        // Act
        var next = guard.Reserve(1);
        var again = guard.Reserve(1);

        // Assert
        withinCap.ShouldBeSuccess();
        next.ShouldBeFailure();
        again.ShouldHaveError<TransactionBudgetExceededError>()
            .Message.ShouldContain("byte");
    }

    [Test]
    public void Reserve_ZeroByteCap_RejectsAnyPositiveEstimate()
    {
        // Arrange
        var guard = Guard(maxEstimatedBytes: 0);
        var zero = guard.Reserve(0);

        // Act
        var positive = guard.Reserve(1);

        // Assert
        zero.ShouldBeSuccess();
        positive.ShouldBeFailure();
    }

    [Test]
    public void Reserve_NegativeEstimate_FailsWithoutConsumingTheOperationSlot()
    {
        // Arrange
        var guard = Guard(maxOperations: 1);

        // Act
        var negative = guard.Reserve(-1);
        var after = guard.Reserve(1);

        // Assert
        negative.ShouldBeFailure();
        negative.ShouldHaveError<TransactionBudgetExceededError>();
        after.ShouldBeSuccess();
    }

    [Test]
    public void Reserve_DoesNotWrapIntMaxValueIntoASuccessfulReservation()
    {
        // Arrange
        var guard = Guard(maxEstimatedBytes: int.MaxValue, maxOperations: 5);
        var atCap = guard.Reserve(int.MaxValue);

        // Act
        var overflow = guard.Reserve(1);

        // Assert
        atCap.ShouldBeSuccess();
        overflow.ShouldBeFailure();
    }

    [Test]
    public void Reserve_TimeLimit_FailsUntilTheGuardIsRebuilt()
    {
        // Arrange
        var guard = Guard(timeLimit: TimeSpan.FromMilliseconds(20));
        Thread.Sleep(40);

        // Act
        var first = guard.Reserve(1);
        var second = guard.Reserve(1);
        guard.ResetAttempt();
        var afterReset = guard.Reserve(1);

        // Assert
        first.ShouldBeFailure();
        second.ShouldHaveError<TransactionBudgetExceededError>()
            .Message.ShouldContain("elapsed-time");
        afterReset.ShouldBeFailure();
    }

    [Test]
    public void ResetAttempt_ClearsOperationsAndBytesButNotTheDeadline()
    {
        // Arrange
        var guard = Guard(maxOperations: 1, maxEstimatedBytes: 10);
        var beforeOk = guard.Reserve(10);
        var beforeFail = guard.Reserve(1);

        // Act
        guard.ResetAttempt();
        var afterOk = guard.Reserve(10);
        var afterFail = guard.Reserve(1);

        // Assert
        beforeOk.ShouldBeSuccess();
        beforeFail.ShouldBeFailure();
        afterOk.ShouldBeSuccess();
        afterFail.ShouldBeFailure();
    }

    private static TransactionBudgetGuard Guard(
        int maxOperations = 1_000,
        int maxEstimatedBytes = 16 * 1024 * 1024,
        TimeSpan? timeLimit = null)
        => new(new MongoTransactionOptions
        {
            MaxOperations = maxOperations,
            MaxEstimatedBytes = maxEstimatedBytes,
            TimeLimit = timeLimit ?? TimeSpan.FromMinutes(1)
        });
}