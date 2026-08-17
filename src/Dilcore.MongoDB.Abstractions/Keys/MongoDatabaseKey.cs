namespace Dilcore.MongoDB.Abstractions.Keys;

public readonly record struct MongoDatabaseKey(string Name)
{
    public override string ToString() => Name;

    public static implicit operator string(MongoDatabaseKey key) => key.Name;
}
