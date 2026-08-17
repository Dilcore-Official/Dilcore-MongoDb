using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Namespace;

public interface INamespaceSegmentContributor
{
    int Order { get; }

    Task<Result<string?>> ContributeAsync(NamespaceResolutionRequest request, CancellationToken cancellationToken = default);
}
