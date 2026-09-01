using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Internal;

internal interface IMongoOperationBudget
{
    Result Reserve(int estimatedBytes);
}
