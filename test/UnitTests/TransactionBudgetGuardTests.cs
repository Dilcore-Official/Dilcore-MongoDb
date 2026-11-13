using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Abstractions.Transactions;
using Dilcore.MongoDB.Internal;

namespace Dilcore.MongoDB.UnitTests;

public class TransactionBudgetGuardTests
{
    [Test]
    public void Reserve_AllowsExactlyMaxOperations_ThenFails()
    {
        var guard = Guard(maxOperations: 2);

        guard.Reserve(1).ShouldBeSuccess();
        guard.Reserve(1).ShouldBeSuccess();

        var third = guard.Reserve(1);
        third.ShouldBeFailure();
        third.ShouldHaveError<TransactionBudgetExceededError>()
            .Message.ShouldContain("operation");
    }

    [Test]
    public void Reserve_AllowsBytesUpToTheCap_AndRejectsTheNextByte()
    {
        var guard = Guard(maxEstimatedBytes: 100);

        guard.Reserve(100).ShouldBeSuccess();
        guard.Reserve(1).ShouldBeFailure();
        guard.Reserve(1).ShouldHaveError<TransactionBudgetExceededError>()
            .Message.ShouldContain("byte");
    }

    [Test]
    public void Reserve_ZeroByteCap_RejectsAnyPositiveEstimate()
    {
        var guard = Guard(maxEstimatedBytes: 0);

        guard.Reserve(0).ShouldBeSuccess();
        guard.Reserve(1).ShouldBeFailure();
    }

    [Test]
    public void Reserve_NegativeEstimate_FailsWithoutConsumingTheOperationSlot()
    {
        var guard = Guard(maxOperations: 1);

        var negative = guard.Reserve(-1);
        negative.ShouldBeFailure();
        negative.ShouldHaveError<TransactionBudgetExceededError>();

        guard.Reserve(1).ShouldBeSuccess();
    }

    [Test]
    public void Reserve_DoesNotWrapIntMaxValueIntoASuccessfulReservation()
    {
        var guard = Guard(maxEstimatedBytes: int.MaxValue, maxOperations: 5);

        guard.Reserve(int.MaxValue).ShouldBeSuccess();
        guard.Reserve(1).ShouldBeFailure();
    }

    [Test]
    public void Reserve_TimeLimit_FailsUntilTheGuardIsRebuilt()
    {
        var guard = Guard(timeLimit: TimeSpan.FromMilliseconds(20));
        Thread.Sleep(40);

        guard.Reserve(1).ShouldBeFailure();
        guard.Reserve(1).ShouldHaveError<TransactionBudgetExceededError>()
            .Message.ShouldContain("elapsed-time");

        guard.ResetAttempt();
        guard.Reserve(1).ShouldBeFailure();
    }

    [Test]
    public void ResetAttempt_ClearsOperationsAndBytesButNotTheDeadline()
    {
        var guard = Guard(maxOperations: 1, maxEstimatedBytes: 10);

        guard.Reserve(10).ShouldBeSuccess();
        guard.Reserve(1).ShouldBeFailure();

        guard.ResetAttempt();
        guard.Reserve(10).ShouldBeSuccess();
        guard.Reserve(1).ShouldBeFailure();
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