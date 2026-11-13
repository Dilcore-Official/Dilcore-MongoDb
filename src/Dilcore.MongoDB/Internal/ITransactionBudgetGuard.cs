using Dilcore.MongoDB.Abstractions.Internal;

namespace Dilcore.MongoDB.Internal;

internal interface ITransactionBudgetGuard : IMongoOperationBudget
{
    void ResetAttempt();
}