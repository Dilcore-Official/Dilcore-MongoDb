# Provision collections, indexes and TTL

Declare indexes and expiry on a binding and apply them at startup or in a deploy job with `IMongoDbProvisioner`. Package: `Dilcore.MongoDB`.

## Setup

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")!;
var services = new ServiceCollection();

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("auth", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Session>("sessions", d => d
            .WithCollectionName("sessions")
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
            .WithIndexes(new CreateIndexModel<Session>(
                Builders<Session>.IndexKeys.Ascending(x => x.UserName),
                // Always name the index. Apply compares by name to detect drift and removals.
                new CreateIndexOptions { Name = "sessions_user", Unique = true }))

            // Expire at the timestamp stored in ExpiresAt. A non-zero lifetime is added to that field,
            // so one hour would mean an hour after ExpiresAt, not an hour after the session was created.
            // The server may delete the document after it becomes eligible.
            .WithCollectionItemsTimeToLive(TimeSpan.Zero, x => x.ExpiresAt));
    }));

public sealed class Session : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public DateTime ExpiresAt { get; set; }
}
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Provisioning;
using Microsoft.Extensions.DependencyInjection;

public static class Provisioning
{
    // Call after the host is built and before it serves requests, or from a deploy job.
    public static async Task ProvisionAsync(
        IServiceProvider services,
        bool allowIndexRemoval,
        CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

        // Preview first on an existing database. Applied is false and nothing changes.
        var preview = await provisioner.DryRunAsync(cancellationToken);
        if (preview.IsFailed)
        {
            throw new InvalidOperationException("MongoDB provisioning preview failed.");
        }

        var wouldRemove = preview.Value.Steps.Any(step => step.Action == "would-remove");
        if (wouldRemove && !allowIndexRemoval)
        {
            throw new InvalidOperationException("Review the index removal before applying.");
        }

        var report = await provisioner.ApplyAsync(cancellationToken);
        if (report.IsFailed)
        {
            throw new InvalidOperationException("MongoDB provisioning failed.");   // fail closed at startup
        }
    }
}
```

`ProvisioningReport` has `Applied` and `Steps` (`ProvisioningStepResult`: `Name`, `Action`, optional `Details`).

The declared indexes, plus the optional TTL index, are the **exclusive** desired set for each binding collection, besides MongoDB's own `_id_` index:

| Situation | Apply | DryRun |
|-----------|-------|--------|
| Index missing on the server | `create` | `would-create` |
| Same name, same definition | `skip` | `skip` |
| Same name, different keys or options | `recreate` (drop and create) | `would-recreate` |
| On the server, not on the binding | `remove` | `would-remove` |

Documents are never deleted, only index metadata changes. Comparison covers `key`, `unique`, `sparse` and TTL `expireAfterSeconds`. Apply is idempotent when everything is already aligned.

## Handle failures

```csharp
var report = await provisioner.ApplyAsync(cancellationToken);
if (report.IsFailed)
{
    // report.Errors holds the failing step as a plain message, for example when existing
    // duplicate values block a new unique index. That failure is not a DuplicateKeyError.
    // Stop the deployment.
}
```

## Pitfalls

- Provision at startup or in a deploy job, never in a request. Resolving a collection does not create indexes.
- `WithIndexes` takes `CreateIndexModel<T>` values, not raw `IndexKeys` definitions. Set `CreateIndexOptions.Name` on every index.
- Apply **removes** server indexes that are not declared on the binding. Declare every index you want to keep, and run `DryRunAsync` first on existing data.
- Run provisioning from one deployment step. `ApplyAsync` recomputes removals from the current index list, so an index created after `DryRunAsync` is removed without passing the `would-remove` check.
- The provisioning identity needs index and collection create privileges. Run it as a deployment step, see [Production MongoDB posture](../security/production-mongodb.md).
- Time series collections and other driver-only create options belong in a [custom step](custom-provisioning-steps.md) or a one-off script. Vector indexes are planned, see [ROADMAP](../../ROADMAP.md).

## See also

- [Expire documents automatically with TTL](expire-documents-with-ttl.md)
- [Custom provisioning steps](custom-provisioning-steps.md)
- [Register clusters and databases](register-clusters-and-databases.md)
- [Samples](../../samples/MongoDb.Capabilities.Sample)
- Runnable example: [ProvisionIndexesAndTtlExampleTests](../../test/Repositories.IntegrationTests/Examples/ProvisionIndexesAndTtlExampleTests.cs)
