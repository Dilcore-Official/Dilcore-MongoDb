using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Namespace;

public interface INamespacePrefixResolver
{
    Task<Result<string?>> ResolveAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default);
}
