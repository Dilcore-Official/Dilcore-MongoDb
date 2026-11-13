using System.Linq.Expressions;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Namespace;
using MongoDB.Driver;

namespace Dilcore.MongoDB.DependencyInjection;

/// <summary>
/// Configures a single typed document binding: collection name, policies, indexes, and repository registrations.
/// </summary>
/// <typeparam name="TDocument">The document entity type for this binding.</typeparam>
public interface IMongoDocumentBindingBuilder<TDocument>
    where TDocument : class, IDocumentEntity
{
    /// <summary>
    /// Sets the logical MongoDB collection name (required before the binding is built).
    /// </summary>
    /// <param name="collectionName">The collection name; must not be null or whitespace.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="collectionName"/> is null or whitespace.</exception>
    IMongoDocumentBindingBuilder<TDocument> WithCollectionName(string collectionName);

    /// <summary>
    /// Enables soft-delete behavior on the generic repository for documents implementing <see cref="Abstractions.Policies.ISoftDeletable"/>.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when <typeparamref name="TDocument"/> does not implement soft-delete.</exception>
    IMongoDocumentBindingBuilder<TDocument> WithSoftDelete();

    /// <summary>
    /// Configures how new <see cref="Guid"/> identifiers are generated on insert for <c>IDocumentEntity&lt;Guid&gt;</c> documents.
    /// </summary>
    /// <param name="strategy">The Guid generation strategy.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the document identifier type is not <see cref="Guid"/>.</exception>
    IMongoDocumentBindingBuilder<TDocument> WithGuidIdGeneration(GuidIdGenerationStrategy strategy);

    /// <summary>
    /// Registers <see cref="Abstractions.Repositories.IGenericBulkRepository{TDocument}"/> for this binding in DI.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    IMongoDocumentBindingBuilder<TDocument> WithBulkRepository();

    /// <summary>
    /// Registers <see cref="Abstractions.Repositories.IGenericProjectionRepository{TDocument}"/> for this binding in DI.
    /// </summary>
    /// <returns>This builder for chaining.</returns>
    IMongoDocumentBindingBuilder<TDocument> WithProjectionRepository();

    /// <summary>
    /// Overrides the database-level static namespace prefix for this binding only.
    /// </summary>
    /// <param name="prefix">The prefix segment; must not be null or whitespace.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix"/> is null or whitespace.</exception>
    IMongoDocumentBindingBuilder<TDocument> WithNamespacePrefix(string prefix);

    /// <summary>
    /// Overrides the database-level namespace prefix resolver for this binding only.
    /// </summary>
    /// <typeparam name="TResolver">An implementation of <see cref="INamespacePrefixResolver"/>.</typeparam>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a resolver was already configured for this binding.</exception>
    IMongoDocumentBindingBuilder<TDocument> WithNamespacePrefixResolver<TResolver>()
        where TResolver : class, INamespacePrefixResolver;

    /// <summary>
    /// Declares index models to provision for this binding's collection.
    /// </summary>
    /// <param name="indexes">Index definitions applied at provisioning time.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="indexes"/> is <see langword="null"/>.</exception>
    IMongoDocumentBindingBuilder<TDocument> WithIndexes(params CreateIndexModel<TDocument>[] indexes);

    /// <summary>
    /// Configures a TTL index on the selected date or numeric field for expiring collection items.
    /// </summary>
    /// <param name="timeToLive">Documents expire after this duration from the indexed field's value.</param>
    /// <param name="propertySelector">Expression selecting the BSON field used for TTL.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="propertySelector"/> is <see langword="null"/>.</exception>
    IMongoDocumentBindingBuilder<TDocument> WithCollectionItemsTimeToLive(
        TimeSpan timeToLive,
        Expression<Func<TDocument, object>> propertySelector);
}