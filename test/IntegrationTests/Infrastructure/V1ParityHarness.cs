using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Descriptors;
using Dilcore.MongoDB.Namespace;

namespace Dilcore.MongoDB.IntegrationTests.Infrastructure;

/// <summary>
/// Test-only harness that maps representative v1 configuration shapes to expected v2 physical names.
/// Does not ship as a public compatibility adapter.
/// </summary>
public static class V1ParityHarness
{
    public sealed record V1Config(
        string DatabaseKey,
        string LogicalDatabaseName,
        string LogicalCollectionName,
        string? DatabasePrefix,
        string? CollectionPrefix);

    public sealed record ExpectedPhysicalNames(string Database, string Collection);

    public static ExpectedPhysicalNames Project(V1Config config)
    {
        // v1 used registration key + WithDatabaseName as dual authority; v2 uses one logical name.
        // Parity assumes they already matched (the only supported migration mapping).
        if (!string.Equals(config.DatabaseKey, config.LogicalDatabaseName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "v1 configs where AddDatabase key differed from WithDatabaseName are not supported; " +
                "align names before migrating.");
        }

        var resolver = new DefaultNamespaceResolver(
        [
            new PrefixNamespaceSegmentContributor()
        ], new MongoRegistrationGraph
        {
            Clusters = [],
            Databases = [],
            Bindings = []
        });

        var database = resolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = config.LogicalDatabaseName,
            Target = NamespaceTarget.Database,
            StaticPrefix = config.DatabasePrefix,
            DatabaseKey = new MongoDatabaseKey(config.DatabaseKey)
        }).GetAwaiter().GetResult();

        var collection = resolver.ResolveAsync(new NamespaceResolutionRequest
        {
            LogicalName = config.LogicalCollectionName,
            Target = NamespaceTarget.Collection,
            StaticPrefix = config.CollectionPrefix,
            DatabaseKey = new MongoDatabaseKey(config.DatabaseKey)
        }).GetAwaiter().GetResult();

        if (database.IsFailed || collection.IsFailed)
        {
            throw new InvalidOperationException(
                $"Parity projection failed: db={string.Join(",", database.Errors.Select(e => e.Message))}; " +
                $"col={string.Join(",", collection.Errors.Select(e => e.Message))}");
        }

        return new ExpectedPhysicalNames(database.Value, collection.Value);
    }
}
