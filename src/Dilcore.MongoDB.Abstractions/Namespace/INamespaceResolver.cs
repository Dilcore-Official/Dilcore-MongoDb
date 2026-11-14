using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Namespace;

/// <summary>
/// Builds validated physical database or collection names from logical names and ordered contributors.
/// </summary>
public interface INamespaceResolver
{
    /// <summary>
    /// Resolves the physical MongoDB name for <paramref name="request"/>.
    /// </summary>
    /// <param name="request">Logical name, target, keys, and optional metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The physical name, or errors when a contributor fails or the name is invalid.</returns>
    Task<Result<string>> ResolveAsync(NamespaceResolutionRequest request, CancellationToken cancellationToken = default);
}