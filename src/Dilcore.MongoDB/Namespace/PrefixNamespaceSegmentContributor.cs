using Dilcore.MongoDB.Abstractions.Namespace;
using FluentResults;

namespace Dilcore.MongoDB.Namespace;

internal sealed class PrefixNamespaceSegmentContributor : INamespaceSegmentContributor
{
    public int Order => 100;

    public Task<Result<string?>> ContributeAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Ok(request.StaticPrefix));
    }
}
