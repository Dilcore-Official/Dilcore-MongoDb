using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Namespace;

namespace Dilcore.MongoDB.DependencyInjection;

/// <summary>
/// Configures a named logical database, its cluster, namespace rules, and document bindings.
/// </summary>
public interface IMongoDatabaseBuilder
{
    /// <summary>
    /// Associates this database with a cluster registered via <c>AddCluster</c>.
    /// </summary>
    /// <param name="clusterName">The cluster key name.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clusterName"/> is null or whitespace.</exception>
    IMongoDatabaseBuilder OnCluster(string clusterName);

    /// <summary>
    /// Sets a static namespace prefix applied to databases and collections for this registration.
    /// </summary>
    /// <param name="prefix">The prefix segment; must not be null or whitespace.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="prefix"/> is null or whitespace.</exception>
    IMongoDatabaseBuilder WithNamespacePrefix(string prefix);

    /// <summary>
    /// Registers a scoped resolver that supplies dynamic namespace prefix segments (for example, tenancy).
    /// </summary>
    /// <typeparam name="TResolver">An implementation of <see cref="INamespacePrefixResolver"/>.</typeparam>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a resolver was already configured for this database.</exception>
    IMongoDatabaseBuilder WithNamespacePrefixResolver<TResolver>()
        where TResolver : class, INamespacePrefixResolver;

    /// <summary>
    /// Registers a named typed document binding and its collection, policies, and optional repositories.
    /// </summary>
    /// <typeparam name="TDocument">The document entity type.</typeparam>
    /// <param name="name">Unique binding key used for keyed DI resolution.</param>
    /// <param name="configure">Configures collection name, indexes, and repository options.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="name"/> was already used for this database.</exception>
    IMongoDatabaseBuilder AddDocumentBinding<TDocument>(
        string name,
        Action<IMongoDocumentBindingBuilder<TDocument>> configure)
        where TDocument : class, IDocumentEntity;
}