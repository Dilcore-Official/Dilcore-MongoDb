namespace Dilcore.MongoDB.Abstractions;

/// <summary>
/// Marker interface for documents managed by Dilcore.MongoDB repositories.
/// Prefer <see cref="IDocumentEntity{TId}"/> to declare the identifier type.
/// </summary>
public interface IDocumentEntity;