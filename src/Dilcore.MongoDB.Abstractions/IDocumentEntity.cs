namespace Dilcore.MongoDB.Abstractions;

/// <summary>
/// Marker interface for documents managed by Dilcore.MongoDB repositories.
/// Prefer <see cref="IDocumentEntity{TId}"/> to declare the identifier type.
/// </summary>
public interface IDocumentEntity;

/// <summary>
/// Document with a typed identifier that maps to MongoDB <c>_id</c>.
/// </summary>
/// <typeparam name="TId">Identifier type (e.g. <see cref="Guid"/>, <see cref="MongoDB.Bson.ObjectId"/>, <see cref="string"/>).</typeparam>
public interface IDocumentEntity<TId> : IDocumentEntity
{
    TId Id { get; set; }
}
