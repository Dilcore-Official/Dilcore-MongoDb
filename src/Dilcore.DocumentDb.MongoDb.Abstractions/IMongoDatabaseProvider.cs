using FluentResults;
using MongoDB.Driver;

namespace Dilcore.DocumentDb.Abstractions;

public interface IMongoDatabaseProvider
{
    Task<Result<IMongoDatabase>> GetDatabaseAsync(string databaseName, CancellationToken cancellationToken = default);
}