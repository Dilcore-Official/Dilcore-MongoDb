# Provisioning

**Current.** Declare collections, indexes, and TTL on document bindings. Apply them with `IMongoDbProvisioner` **outside the request hot path**. Collection resolution does not create indexes.

Sample: [samples/MongoDb.Capabilities.Sample](../../samples/MongoDb.Capabilities.Sample) (`DryRunAsync`, `ApplyAsync`, custom `IProvisioningStep`). Getting-started: [MongoDb.WebApi.Sample](../../samples/MongoDb.WebApi.Sample) (`ApplyAsync` at startup).

## Binding declarations

```csharp
db.AddDocumentBinding<Order>("orders", d => d
    .WithCollectionName("orders")
    .WithIndexes(new CreateIndexModel<Order>(
        Builders<Order>.IndexKeys.Ascending(x => x.Sku),
        new CreateIndexOptions { Name = "orders_sku" }))
    .WithCollectionItemsTimeToLive(TimeSpan.FromDays(1), x => x.ExpiresAt));
```

`WithIndexes` takes `params CreateIndexModel<TDocument>[]`, not raw `IndexKeys` definitions.

`WithCollectionItemsTimeToLive` adds an expire-after index on the selected property when you call `ApplyAsync`.

## Runner

`IMongoDbProvisioner` is registered by `AddMongoDb` (scoped):

```csharp
var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
var preview = await provisioner.DryRunAsync();   // Applied == false; Action is would-create or skip
var applied = await provisioner.ApplyAsync();    // mutates the server; idempotent
```

`ProvisioningReport` lists `ProvisioningStepResult` (`Name`, `Action`, optional `Details`). Fail closed if `IsFailed`.

Use a deploy job or host startup — not every HTTP request.

## Custom steps

Built-in steps cover collections and indexes from bindings. Register extra work in DI after `AddMongoDb`:

```csharp
builder.Services.AddSingleton<IProvisioningStep, SampleProvisioningStep>();
```

```csharp
public sealed class SampleProvisioningStep : IProvisioningStep
{
    public string Name => "sample-metadata";

    public async Task<Result<ProvisioningStepResult>> ExecuteAsync(
        IMongoDatabaseResolver databaseResolver,
        bool apply,
        CancellationToken cancellationToken)
    {
        var database = await databaseResolver.GetDatabaseAsync(
            new MongoDatabaseKey("CapabilitiesDB"),
            cancellationToken);
        if (database.IsFailed)
            return database.ToResult();

        return Result.Ok(new ProvisioningStepResult
        {
            Name = Name,
            Action = apply ? "applied" : "would-apply",
            Details = database.Value.DatabaseNamespace.DatabaseName
        });
    }
}
```

Time series collections and other driver-only create options belong here or in a one-off script. Vector indexes remain **planned** (M7).

Production least-privilege for the provisioner identity: [production-mongodb.md](../security/production-mongodb.md).
