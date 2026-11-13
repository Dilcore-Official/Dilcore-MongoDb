using System.Linq.Expressions;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Options;

/// <summary>
/// Per-request collection settings derived from registration and namespace resolution.
/// </summary>
/// <typeparam name="TDocument">Document type for index and TTL expressions.</typeparam>
public class GetCollectionOptions<TDocument>
    where TDocument : IDocumentEntity
{
    /// <summary>
    /// Physical collection name after namespace resolution.
    /// </summary>
    public string? CollectionName { get; private set; }

    /// <summary>
    /// Index models to ensure on the collection when non-null.
    /// </summary>
    public IReadOnlyList<CreateIndexModel<TDocument>>? Indices { get; private set; }

    /// <summary>
    /// TTL duration when a TTL index is configured on the binding.
    /// </summary>
    public TimeSpan? CollectionItemsTimeToLive { get; private set; }

    /// <summary>
    /// Document field used as the TTL index key.
    /// </summary>
    public Expression<Func<TDocument, object>>? TimeToLeavePropertySelector { get; private set; }

    /// <summary>
    /// Whether soft-delete filtering is enabled for this collection.
    /// </summary>
    public bool SoftDeleteEnabled { get; private set; }

    /// <summary>
    /// <see langword="true"/> when <see cref="SoftDeleteEnabled"/> is <see langword="false"/>.
    /// </summary>
    public bool SoftDeleteDisabled => !SoftDeleteEnabled;

    /// <summary>
    /// Strategy for auto-generated <see cref="Guid"/> identifiers on this binding.
    /// </summary>
    public GuidIdGenerationStrategy GuidIdGenerationStrategy { get; private set; } =
        GuidIdGenerationStrategy.Random;

    /// <summary>
    /// Sets <see cref="CollectionName"/>.
    /// </summary>
    /// <param name="collectionName">Resolved physical collection name.</param>
    /// <returns>This instance for chaining.</returns>
    public GetCollectionOptions<TDocument> WithCollectionName(string collectionName)
    {
        CollectionName = collectionName;
        return this;
    }

    /// <summary>
    /// Sets <see cref="Indices"/> from a list.
    /// </summary>
    /// <param name="indexes">Index models to apply.</param>
    /// <returns>This instance for chaining.</returns>
    public GetCollectionOptions<TDocument> WithIndexes(IReadOnlyList<CreateIndexModel<TDocument>> indexes)
    {
        Indices = indexes;
        return this;
    }

    /// <summary>
    /// Sets <see cref="Indices"/> from a parameter array.
    /// </summary>
    /// <param name="indexes">Index models to apply.</param>
    /// <returns>This instance for chaining.</returns>
    public GetCollectionOptions<TDocument> WithIndexes(params CreateIndexModel<TDocument>[] indexes)
    {
        Indices = indexes.ToList();
        return this;
    }

    /// <summary>
    /// Configures a TTL index on the selected document field.
    /// </summary>
    /// <param name="timeToLive">Expiration interval.</param>
    /// <param name="propertySelector">Field that stores the expiration timestamp.</param>
    /// <returns>This instance for chaining.</returns>
    public GetCollectionOptions<TDocument> WithCollectionItemsTimeToLive(
        TimeSpan timeToLive,
        Expression<Func<TDocument, object>> propertySelector)
    {
        CollectionItemsTimeToLive = timeToLive;
        TimeToLeavePropertySelector = propertySelector;
        return this;
    }

    /// <summary>
    /// Enables soft-delete semantics for repositories using these options.
    /// </summary>
    /// <returns>This instance for chaining.</returns>
    public GetCollectionOptions<TDocument> WithSoftDelete()
    {
        SoftDeleteEnabled = true;
        return this;
    }

    /// <summary>
    /// Sets <see cref="GuidIdGenerationStrategy"/>.
    /// </summary>
    /// <param name="strategy">Guid generation mode.</param>
    /// <returns>This instance for chaining.</returns>
    public GetCollectionOptions<TDocument> WithGuidIdGeneration(GuidIdGenerationStrategy strategy)
    {
        GuidIdGenerationStrategy = strategy;
        return this;
    }
}