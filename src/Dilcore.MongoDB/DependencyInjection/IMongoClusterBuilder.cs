using Dilcore.MongoDB.Abstractions.Ownership;
using MongoDB.Driver;

namespace Dilcore.MongoDB.DependencyInjection;

/// <summary>
/// Configures a named MongoDB cluster (connection or existing client) for DI registration.
/// </summary>
public interface IMongoClusterBuilder
{
    /// <summary>
    /// Connects the cluster using a MongoDB connection string; a singleton <see cref="IMongoClient"/> is created per cluster.
    /// </summary>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">Thrown after <c>UseExistingClient</c> was already called on this cluster.</exception>
    IMongoClusterBuilder UseConnectionString(string connectionString);

    /// <summary>
    /// Sets the driver maximum connection pool size for clients created from a connection string.
    /// </summary>
    /// <param name="maxConnectionPoolSize">Must be greater than zero.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="maxConnectionPoolSize"/> is not positive.</exception>
    IMongoClusterBuilder UseMaxConnectionPoolSize(int maxConnectionPoolSize);

    /// <summary>
    /// Registers an existing <see cref="IMongoClient"/> instead of creating one from a connection string.
    /// </summary>
    /// <param name="client">The client instance to register.</param>
    /// <param name="ownership">Whether the library or the application owns client disposal.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown after <c>UseConnectionString</c> was already called on this cluster.</exception>
    IMongoClusterBuilder UseExistingClient(IMongoClient client, MongoClientOwnership ownership);
}