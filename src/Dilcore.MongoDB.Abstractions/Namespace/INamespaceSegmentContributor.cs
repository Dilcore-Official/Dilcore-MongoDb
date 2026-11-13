using FluentResults;

namespace Dilcore.MongoDB.Abstractions.Namespace;

/// <summary>
/// Contributes one namespace segment when composing a physical database or collection name.
/// </summary>
public interface INamespaceSegmentContributor
{
    /// <summary>
    /// Sort order relative to other contributors (lower values run first).
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Returns an optional segment for the current resolution request.
    /// </summary>
    /// <param name="request">Resolution context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A non-empty segment, <see langword="null"/> to skip, or errors to fail resolution.</returns>
    Task<Result<string?>> ContributeAsync(NamespaceResolutionRequest request, CancellationToken cancellationToken = default);
}