namespace Dilcore.MongoDB.Abstractions;

/// <summary>
/// Document with a typed identifier that maps to MongoDB <c>_id</c>.
/// </summary>
/// <typeparam name="TId">Identifier type, for example <see cref="Guid"/>, <c>MongoDB.Bson.ObjectId</c>, or <see cref="string"/>.</typeparam>
public interface IDocumentEntity<TId> : IDocumentEntity
{
    /// <summary>
    /// Document identifier stored as MongoDB <c>_id</c>.
    /// </summary>
    TId Id { get; set; }
}