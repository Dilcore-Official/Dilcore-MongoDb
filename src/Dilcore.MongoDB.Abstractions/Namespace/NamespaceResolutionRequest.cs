using Dilcore.MongoDB.Abstractions.Keys;

namespace Dilcore.MongoDB.Abstractions.Namespace;

/// <summary>
/// Input for database and collection namespace resolution.
/// </summary>
public sealed class NamespaceResolutionRequest
{
    /// <summary>
    /// Logical database or collection name before prefixes and contributor segments.
    /// </summary>
    public required string LogicalName { get; init; }

    /// <summary>
    /// Whether <see cref="LogicalName"/> refers to a database or a collection.
    /// </summary>
    public required NamespaceTarget Target { get; init; }

    /// <summary>
    /// Document binding key when resolving a collection; used to load binding-specific prefix resolvers.
    /// </summary>
    public MongoDocumentBindingKey? BindingKey { get; init; }

    /// <summary>
    /// Database key when resolving a database or collection under a registered database.
    /// </summary>
    public MongoDatabaseKey? DatabaseKey { get; init; }

    /// <summary>
    /// Optional static prefix from registration (e.g. <c>WithNamespacePrefix</c>).
    /// Dynamic multi-tenant prefixes belong in app-owned <see cref="INamespaceSegmentContributor"/> implementations.
    /// </summary>
    public string? StaticPrefix { get; init; }

    /// <summary>
    /// Optional binding metadata passed through to prefix resolvers (key/value strings).
    /// </summary>
    public IReadOnlyDictionary<string, string>? BindingMetadata { get; init; }
}