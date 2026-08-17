using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions;

public interface IMongoDatabaseResolver
{
    Task<Result<IMongoDatabase>> GetDatabaseAsync(
        MongoDatabaseKey databaseKey,
        CancellationToken cancellationToken = default);
}
