namespace Dilcore.MongoDB.Abstractions.Keys;

public readonly record struct MongoClusterKey(string Name)
{
    public override string ToString() => Name;

    public static implicit operator string(MongoClusterKey key) => key.Name;
}
