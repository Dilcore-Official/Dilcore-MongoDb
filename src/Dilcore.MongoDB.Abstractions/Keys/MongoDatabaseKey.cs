namespace Dilcore.MongoDB.Abstractions.Keys;

/// <summary>
/// Registration key that identifies a logical MongoDB database registered under a cluster.
/// </summary>
/// <param name="Name">Logical database name passed to namespace resolution.</param>
public readonly record struct MongoDatabaseKey(string Name)
{
    /// <inheritdoc />
    public override string ToString() => Name;

    /// <summary>
    /// Gets the database <see cref="Name"/> from <paramref name="key"/>.
    /// </summary>
    /// <param name="key">Key to unwrap.</param>
    public static implicit operator string(MongoDatabaseKey key) => key.Name;
}