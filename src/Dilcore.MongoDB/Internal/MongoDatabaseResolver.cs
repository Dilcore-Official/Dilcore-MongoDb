using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Descriptors;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Internal;

internal sealed class MongoDatabaseResolver(
    MongoRegistrationGraph graph,
    INamespaceResolver namespaceResolver,
    IServiceProvider serviceProvider) : IMongoDatabaseResolver
{
    public async Task<Result<IMongoDatabase>> GetDatabaseAsync(
        MongoDatabaseKey databaseKey,
        CancellationToken cancellationToken = default)
    {
        DatabaseDescriptor database;
        try
        {
            database = graph.GetDatabase(databaseKey);
        }
        catch (InvalidOperationException)
        {
            return Result.Fail($"Unknown database key '{databaseKey.Name}'.");
        }

        var resolveResult = await namespaceResolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = database.Key.Name,
            Target = NamespaceTarget.Database,
            DatabaseKey = database.Key,
            StaticPrefix = database.NamespacePrefix
        }, cancellationToken);

        if (resolveResult.IsFailed)
        {
            return resolveResult.ToResult();
        }

        var holder = serviceProvider.GetRequiredKeyedService<MongoClientHolder>(database.ClusterKey.Name);
        var mongoDatabase = holder.Client.GetDatabase(resolveResult.Value);
        return Result.Ok(mongoDatabase);
    }
}
