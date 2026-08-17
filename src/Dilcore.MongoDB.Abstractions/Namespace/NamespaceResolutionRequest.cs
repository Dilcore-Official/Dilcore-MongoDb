using Dilcore.MongoDB.Abstractions.Keys;

namespace Dilcore.MongoDB.Abstractions.Namespace;

public sealed class NamespaceResolutionRequest
{
    public required string LogicalName { get; init; }

    public required NamespaceTarget Target { get; init; }

    public MongoDocumentBindingKey? BindingKey { get; init; }

    public MongoDatabaseKey? DatabaseKey { get; init; }

    /// <summary>
    /// Optional static prefix from registration (e.g. <c>WithNamespacePrefix</c>).
    /// Dynamic multi-tenant prefixes belong in app-owned <see cref="INamespaceSegmentContributor"/> implementations.
    /// </summary>
    public string? StaticPrefix { get; init; }

    public IReadOnlyDictionary<string, string>? BindingMetadata { get; init; }
}
