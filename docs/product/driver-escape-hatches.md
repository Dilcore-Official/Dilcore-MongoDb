# Reach the MongoDB driver directly

Use `MongoDB.Driver` types when the repository helpers are not enough, without bypassing the namespace pipeline. Package: `Dilcore.MongoDB`.

## Setup

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

public sealed class Order : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = "";
}

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("shop", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
    }));
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

public sealed class ReportService(IMongoDbCollectionFactory collections, IServiceProvider provider)
{
    public async Task<IReadOnlyList<BsonDocument>> TopSkusAsync(CancellationToken cancellationToken)
    {
        // The factory resolves the physical name through the same namespace pipeline
        // (prefixes, tenants) as the repositories.
        var resolved = await collections.GetCollectionAsync<Order>(new MongoDocumentBindingKey("orders"), cancellationToken);
        var collection = resolved.Value;

        // Aggregation stays a driver API. Stored field names follow the BSON conventions
        // (camelCase by default), so Sku is stored as "sku".
        // Do not hide it behind your own abstraction.
        return await collection.Aggregate()
            .Group(new BsonDocument { { "_id", "$sku" }, { "count", new BsonDocument("$sum", 1) } })
            .SortByDescending(x => x["count"])
            .Limit(10)
            .ToListAsync(cancellationToken);
    }

    // The singleton client of a cluster, keyed by the cluster name.
    public IMongoClient Client() => provider.GetRequiredKeyedService<IMongoClient>("primary");
}
```

What you can resolve:

| Type | How |
|------|-----|
| `IMongoClient` | Keyed singleton, key is the cluster name |
| `IMongoDatabase` | Keyed scoped service, key is the database name |
| `IMongoCollection<T>` | Keyed scoped service (key is the binding key), or `IMongoDbCollectionFactory.GetCollectionAsync<T>(bindingKey)` |
| `IMongoCollection<BsonDocument>` | `GetCollectionAsync(databaseKey, logicalCollectionName)` |

Every option, with failures and names, is in [Use collections without repositories](../guides/resolve-collections.md).

Other driver-only features stay driver APIs:

- Cursors: `collection.Find(filter).ToCursorAsync()`. Dispose the cursor.
- GridFS: `new GridFSBucket(database)`.
- Client-side encryption: configure `AutoEncryptionOptions` on the client you supply with `UseExistingClient`.
- Time series: create the collection in a [custom provisioning step](../guides/custom-provisioning-steps.md).
- Sessions: inside a [transaction](../guides/transactions.md) callback, `IMongoDbTransactionContext.Session` is the driver session. Pass it to `IMongoCollection<T>` overloads that take an `IClientSessionHandle`.

Write concern, read concern, read preference, retries, timeouts and compression are driver settings. Set them on the connection string, or build a `MongoClientSettings` and give the client to `UseExistingClient`, see [Production MongoDB posture](../security/production-mongodb.md).

## Handle failures

`GetCollectionAsync` returns a `Result`. Check it before using `Value`:

```csharp
var resolved = await collections.GetCollectionAsync<Order>(new MongoDocumentBindingKey("orders"), cancellationToken);
if (resolved.IsFailed)
{
    // An unknown binding, a missing tenant prefix or an invalid physical name. Plain FluentResults errors.
}
```

Driver calls on the collection throw driver exceptions (`MongoException` and subclasses), not `Result` values. Handle them as you would with the plain driver.

## Pitfalls

- Reach the driver through the keyed client, the keyed database or `IMongoDbCollectionFactory`. Do not create your own `MongoClient`, and the same namespace pipeline then applies.
- Dispose cursors and sessions you open.
- Inside a transaction callback, do not start a second session. Cross-cluster work is rejected as `CrossClusterOperationError`.
- Do not wrap driver features in your own provider-neutral abstraction.

## See also

- [Use collections without repositories](../guides/resolve-collections.md)
- [Transactions](../guides/transactions.md)
- [Custom provisioning steps](../guides/custom-provisioning-steps.md)
- [Multi-tenant database names](../guides/multi-tenant-databases.md)
- [Samples](../../samples/MongoDb.Capabilities.Sample)
- Runnable example: [DriverEscapeHatchesExampleTests](../../test/Repositories.IntegrationTests/Examples/DriverEscapeHatchesExampleTests.cs)
