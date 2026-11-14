namespace Dilcore.MongoDB.DependencyInjection;

/// <summary>
/// Fluent configuration surface passed to <c>AddMongoDb</c> for clusters, databases, and BSON conventions.
/// </summary>
public interface IMongoDbBuilder
{
    /// <summary>
    /// Registers a named MongoDB cluster and its connection settings.
    /// </summary>
    /// <param name="name">Unique cluster key.</param>
    /// <param name="configure">Configures the cluster connection or existing client.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="name"/> was already registered.</exception>
    IMongoDbBuilder AddCluster(string name, Action<IMongoClusterBuilder> configure);

    /// <summary>
    /// Registers a named logical database, its cluster binding, and document bindings.
    /// </summary>
    /// <param name="name">Unique database key.</param>
    /// <param name="configure">Configures cluster association, namespace rules, and bindings.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="name"/> was already registered.</exception>
    IMongoDbBuilder AddDatabase(string name, Action<IMongoDatabaseBuilder> configure);

    /// <summary>
    /// Configures global BSON conventions; may be called at most once per <c>AddMongoDb</c> invocation.
    /// </summary>
    /// <param name="configure">Applies enum, null, extra-element, and custom convention settings.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="configure"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when conventions were already configured.</exception>
    IMongoDbBuilder ConfigureConventions(Action<IConventionsBuilder> configure);
}