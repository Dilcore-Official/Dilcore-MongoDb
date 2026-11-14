namespace Dilcore.MongoDB.Abstractions.Keys;

/// <summary>
/// Registration key that identifies a typed document binding (database, collection, and policies).
/// </summary>
/// <param name="Name">Logical binding name used in DI and <see cref="Repositories.IRepositoryResolver"/>.</param>
public readonly record struct MongoDocumentBindingKey(string Name)
{
    /// <inheritdoc />
    public override string ToString() => Name;

    /// <summary>
    /// Gets the binding <see cref="Name"/> from <paramref name="key"/>.
    /// </summary>
    /// <param name="key">Key to unwrap.</param>
    public static implicit operator string(MongoDocumentBindingKey key) => key.Name;
}