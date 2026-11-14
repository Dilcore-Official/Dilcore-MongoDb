namespace Dilcore.MongoDB.Abstractions.Namespace;

/// <summary>
/// Distinguishes database-name resolution from collection-name resolution.
/// </summary>
public enum NamespaceTarget
{
    /// <summary>
    /// Resolve a physical MongoDB database name.
    /// </summary>
    Database = 0,

    /// <summary>
    /// Resolve a physical MongoDB collection name.
    /// </summary>
    Collection = 1
}