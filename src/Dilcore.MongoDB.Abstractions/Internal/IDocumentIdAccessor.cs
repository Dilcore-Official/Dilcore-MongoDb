using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Internal;

internal interface IDocumentIdAccessor<TDocument>
    where TDocument : class, IDocumentEntity
{
    Type IdentifierType { get; }

    bool IsGuidIdentifier { get; }

    bool IsEmpty(TDocument document);

    void EnsureNewId(TDocument document, GuidIdGenerationStrategy guidStrategy = GuidIdGenerationStrategy.Random);

    void ClearId(TDocument document);

    FilterDefinition<TDocument> BuildIdFilter(TDocument document);
}