namespace Dilcore.MongoDB.Abstractions.Keys;

/// <summary>
/// Registration key that identifies a MongoDB cluster in <c>AddMongoDb</c> configuration.
/// </summary>
/// <param name="Name">Logical cluster name used for DI keyed services and transactions.</param>
public readonly record struct MongoClusterKey(string Name)
{
    /// <inheritdoc />
    public override string ToString() => Name;

    /// <summary>
    /// Gets the cluster <see cref="Name"/> from <paramref name="key"/>.
    /// </summary>
    /// <param name="key">Key to unwrap.</param>
    public static implicit operator string(MongoClusterKey key) => key.Name;
}