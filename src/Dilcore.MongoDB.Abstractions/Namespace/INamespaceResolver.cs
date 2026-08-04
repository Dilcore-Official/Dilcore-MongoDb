using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Namespace;

public interface INamespaceResolver
{
    Task<Result<string>> ResolveAsync(NamespaceResolutionRequest request, CancellationToken cancellationToken = default);
}
