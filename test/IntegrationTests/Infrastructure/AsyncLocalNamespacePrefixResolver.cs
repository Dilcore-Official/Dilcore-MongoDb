using Dilcore.MongoDB.Abstractions.Namespace;
using FluentResults;

namespace Dilcore.MongoDB.IntegrationTests.Infrastructure;

/// <summary>
/// Fake async prefix resolver used in DI acceptance tests (simulates storage / API latency).
/// </summary>
public sealed class AsyncLocalNamespacePrefixResolver : INamespacePrefixResolver
{
    private static readonly AsyncLocal<string?> Current = new();
    private static readonly AsyncLocal<bool> FailNext = new();

    public string? Prefix
    {
        get => Current.Value;
        set => Current.Value = value;
    }

    public static IDisposable Use(string? prefix)
    {
        var previous = Current.Value;
        Current.Value = prefix;
        return new RestorePrefix(previous);
    }

    public static IDisposable UseFailure()
    {
        var previous = FailNext.Value;
        FailNext.Value = true;
        return new RestoreFail(previous);
    }

    public async Task<Result<string?>> ResolveAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        // Prove the await path (simulated I/O).
        await Task.Delay(1, cancellationToken);

        if (FailNext.Value)
        {
            return Result.Fail<string?>("Async namespace prefix resolution failed.");
        }

        return Result.Ok(string.IsNullOrWhiteSpace(Current.Value) ? null : Current.Value);
    }

    private sealed class RestorePrefix(string? previous) : IDisposable
    {
        public void Dispose() => Current.Value = previous;
    }

    private sealed class RestoreFail(bool previous) : IDisposable
    {
        public void Dispose() => FailNext.Value = previous;
    }
}
