using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Dilcore.MongoDB.Abstractions.Namespace;
using FluentResults;

namespace Dilcore.MongoDB.Namespace;

internal sealed partial class DefaultNamespaceResolver : INamespaceResolver
{
    private readonly IEnumerable<INamespaceSegmentContributor> _contributors;
    private readonly ConcurrentDictionary<string, string> _cache = new(StringComparer.Ordinal);
    private readonly char _separator;

    public DefaultNamespaceResolver(
        IEnumerable<INamespaceSegmentContributor> contributors,
        char separator = MongoDbDefaults.DefaultNamespaceSeparator)
    {
        _contributors = contributors.OrderBy(c => c.Order).ToList();
        _separator = separator;
    }

    public async Task<Result<string>> ResolveAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.LogicalName);

        var cacheKey = BuildCacheKey(request);
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            return Result.Ok(cached);
        }

        if (_cache.Count >= MongoDbDefaults.NamespaceCacheCapacity)
        {
            _cache.Clear();
        }

        var segments = new List<string>();

        foreach (var contributor in _contributors)
        {
            var segmentResult = await contributor.ContributeAsync(request, cancellationToken);
            if (segmentResult.IsFailed)
            {
                return segmentResult.ToResult();
            }

            if (!string.IsNullOrWhiteSpace(segmentResult.Value))
            {
                segments.Add(segmentResult.Value);
            }
        }

        segments.Add(request.LogicalName);

        var physicalName = string.Join(_separator, segments);
        var validation = ValidatePhysicalName(physicalName, request.Target);
        if (validation.IsFailed)
        {
            return validation;
        }

        _cache[cacheKey] = physicalName;
        return Result.Ok(physicalName);
    }

    private static string BuildCacheKey(NamespaceResolutionRequest request)
    {
        return string.Join('|',
            request.Target.ToString(),
            request.LogicalName,
            request.StaticPrefix ?? string.Empty,
            request.BindingKey?.Name ?? string.Empty,
            request.DatabaseKey?.Name ?? string.Empty);
    }

    private static Result ValidatePhysicalName(string name, NamespaceTarget target)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Fail($"{target} physical name cannot be empty.");
        }

        return target switch
        {
            NamespaceTarget.Database when !DatabaseNameRegex().IsMatch(name)
                => Result.Fail($"Invalid MongoDB database name '{name}'."),
            NamespaceTarget.Collection when !CollectionNameRegex().IsMatch(name)
                => Result.Fail($"Invalid MongoDB collection name '{name}'."),
            NamespaceTarget.Database or NamespaceTarget.Collection => Result.Ok(),
            _ => Result.Fail($"Unknown namespace target '{target}'.")
        };
    }

    // MongoDB: database names cannot contain /\. "$*<>:|? and null; max 63 bytes typically.
    [GeneratedRegex(@"^[^\0\/\\. ""$*<>:|?]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex DatabaseNameRegex();

    // Collection names: non-empty, no null byte, cannot start with system.
    [GeneratedRegex(@"^(?!system\.)[^\0]{1,120}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)]
    private static partial Regex CollectionNameRegex();
}
