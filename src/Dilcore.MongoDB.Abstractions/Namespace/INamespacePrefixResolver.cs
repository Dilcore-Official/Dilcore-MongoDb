using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Namespace;

/// <summary>
/// App-owned resolver that supplies a dynamic namespace segment (for example a tenant id).
/// </summary>
public interface INamespacePrefixResolver
{
    /// <summary>
    /// Returns an optional prefix segment for the requested database or collection.
    /// </summary>
    /// <param name="request">Resolution context including target, keys, and static prefix.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A prefix string, <see langword="null"/> when no segment applies, or errors when resolution fails closed.</returns>
    Task<Result<string?>> ResolveAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default);
}