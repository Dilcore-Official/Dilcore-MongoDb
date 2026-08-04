namespace Dilcore.MongoDB.Abstractions.Keys;

public readonly record struct MongoDocumentBindingKey(string Name)
{
    public override string ToString() => Name;

    public static implicit operator string(MongoDocumentBindingKey key) => key.Name;
}
