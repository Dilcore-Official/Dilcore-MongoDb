# Use collections without repositories

Get a plain `IMongoCollection<T>` or `IMongoDatabase` through the namespace pipeline and call the MongoDB driver yourself, with no repository in between. Package: `Dilcore.MongoDB`.

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

        // Physical database name: catalog_shop
        db.WithNamespacePrefix("catalog");

        // A typed binding. Its collection name is feature_orders.
        db.AddDocumentBinding<Order>("orders", d => d
            .WithCollectionName("orders")
            .WithNamespacePrefix("feature"));
    }));
```

## Use

Pick the shortest option that fits. All of them go through the same prefixes and tenant resolution as the repositories.

| You have | Use |
|----------|-----|
| A registered binding, and you want to inject the collection | Keyed `IMongoCollection<T>` |
| A registered binding, resolved at run time | `IMongoDbCollectionFactory.GetCollectionAsync<T>(bindingKey)` |
| A collection with no binding (any name) | `GetCollectionAsync(databaseKey, logicalName)` returns `BsonDocument` |
| Several collections, a collection list, or a driver database call | Keyed `IMongoDatabase` or `IMongoDatabaseResolver` |
| Only the physical name | `ResolveCollectionNameAsync` |

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

public sealed class OrderCollections(
    // 1. Keyed injection. The key is the binding key. Scoped, resolved when the service is created.
    [FromKeyedServices("orders")] IMongoCollection<Order> injected,
    // 2. The factory, for run-time resolution and for collections without a binding.
    IMongoDbCollectionFactory collections,
    // 3. The database, by name. Keyed by the database key.
    [FromKeyedServices("shop")] IMongoDatabase database,
    IMongoDatabaseResolver databases)
{
    // Plain driver calls. No repository policies apply: no soft delete, ETag, audit or id generation.
    public async Task<long> CountBySkuAsync(string sku, CancellationToken cancellationToken)
    {
        var filter = Builders<Order>.Filter.Eq(x => x.Sku, sku);

        return await injected.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    // The typed collection from the factory returns a Result, so a failure is a value.
    public async Task<Result<IMongoCollection<Order>>> OrdersAsync(CancellationToken cancellationToken)
        => await collections.GetCollectionAsync<Order>(new MongoDocumentBindingKey("orders"), cancellationToken);

    // A collection that has no binding. Any logical name works, the database key picks the database.
    public async Task<Result> AuditAsync(string message, CancellationToken cancellationToken)
    {
        var resolved = await collections.GetCollectionAsync(
            new MongoDatabaseKey("shop"), "audit-log", cancellationToken: cancellationToken);
        if (resolved.IsFailed)
            return resolved.ToResult();

        await resolved.Value.InsertOneAsync(new BsonDocument("message", message), cancellationToken: cancellationToken);

        return Result.Ok();
    }

    // The physical name only, for logging or as a $lookup or $merge target.
    public async Task<string?> PhysicalNameAsync(CancellationToken cancellationToken)
    {
        var name = await collections.ResolveCollectionNameAsync(
            new MongoDatabaseKey("shop"), "audit-log", cancellationToken: cancellationToken);

        return name.IsSuccess ? name.Value : null;
    }

    // The keyed database for collections you name yourself. Note that the name is NOT prefixed here.
    public async Task<IReadOnlyList<string>> CollectionNamesAsync(CancellationToken cancellationToken)
    {
        var names = await database.ListCollectionNamesAsync(cancellationToken: cancellationToken);

        return await names.ToListAsync(cancellationToken);
    }

    // The resolver returns a Result instead of throwing.
    public async Task<string?> DatabaseNameAsync(CancellationToken cancellationToken)
    {
        var resolved = await databases.GetDatabaseAsync(new MongoDatabaseKey("shop"), cancellationToken);

        return resolved.IsSuccess ? resolved.Value.DatabaseNamespace.DatabaseName : null;
    }
}
```

With the registration above, `OrdersAsync` gives `catalog_shop.feature_orders`, the audit collection is `catalog_shop.audit-log`, and `DatabaseNameAsync` returns `catalog_shop`. Under a [tenant prefix resolver](multi-tenant-databases.md) the same calls return the names of the current tenant.

Pass a `staticPrefix` as the third argument of the untyped `GetCollectionAsync` and `ResolveCollectionNameAsync` to prefix a collection that has no binding, for example `staticPrefix: "feature"` gives `feature_audit-log`.

## Handle failures

The factory and the resolver return a `Result`. Check it before using `Value`:

```csharp
var resolved = await collections.GetCollectionAsync(
    new MongoDatabaseKey("unknown"), "audit-log", cancellationToken: cancellationToken);
if (resolved.IsFailed)
{
    // An unknown database or binding key, a missing tenant prefix, or an invalid physical name.
    // Plain FluentResults errors, not MongoOperationError.
}
```

- Keyed injection does not return a `Result`. It throws `InvalidOperationException` with the error messages when the collection or database cannot be resolved.
- Calls on the collection throw driver exceptions (`MongoException` and subclasses), as with the plain driver.

## Pitfalls

- `IMongoDbCollectionFactory`, `IMongoDatabaseResolver`, the keyed `IMongoCollection<T>` and the keyed `IMongoDatabase` are scoped. Do not inject them into a singleton. Create a scope per unit of work.
- A raw collection has none of the repository policies: soft delete is not filtered, `ETag` is not checked, audit fields and ids are not set. Use [repositories](crud-with-repositories.md) when you need them.
- The typed call needs a binding. The untyped call needs only a registered database.
- `database.GetCollection<T>("name")` on the keyed database uses the name as given. To get the prefixed name use the factory.
- Do not create your own `MongoClient`. The keyed one is a shared singleton per cluster.

## See also

- [Reach the MongoDB driver directly](../product/driver-escape-hatches.md)
- [Prefix database and collection names](namespace-prefixes.md)
- [Give each tenant its own database](multi-tenant-databases.md)
- Runnable example: [ResolveCollectionsExampleTests](../../test/Repositories.IntegrationTests/Examples/ResolveCollectionsExampleTests.cs)
