using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions;

/// <summary>
/// Resolves a registered logical database key to an <see cref="IMongoDatabase"/> after namespace contribution.
/// </summary>
public interface IMongoDatabaseResolver
{
    /// <summary>
    /// Gets the MongoDB database for <paramref name="databaseKey"/>.
    /// </summary>
    /// <param name="databaseKey">Registered logical database.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful database handle, or errors when the key is unknown or namespace resolution fails.</returns>
    Task<Result<IMongoDatabase>> GetDatabaseAsync(
        MongoDatabaseKey databaseKey,
        CancellationToken cancellationToken = default);
}