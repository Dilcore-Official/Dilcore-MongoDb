using Dilcore.MongoDB.Abstractions.Namespace;
using FluentResults;

namespace Dilcore.MongoDB.IntegrationTests.Infrastructure;

/// <summary>
/// App-owned dynamic prefix contributor used in tests to show multi-tenancy without library Tenant APIs.
/// </summary>
public sealed class AsyncLocalPrefixContributor : INamespaceSegmentContributor
{
    private static readonly AsyncLocal<string?> Current = new();

    public int Order => 50;

    public bool RequirePrefix { get; init; }

    public string? Prefix => Current.Value;

    public static IDisposable Use(string? prefix)
    {
        var previous = Current.Value;
        Current.Value = prefix;
        return new Restore(previous);
    }

    public Task<Result<string?>> ContributeAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (RequirePrefix && string.IsNullOrWhiteSpace(Current.Value))
        {
            return Task.FromResult(Result.Fail<string?>(
                "A required namespace prefix is missing from the current request context."));
        }

        return Task.FromResult(Result.Ok(string.IsNullOrWhiteSpace(Current.Value) ? null : Current.Value));
    }

    private sealed class Restore(string? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }
}
