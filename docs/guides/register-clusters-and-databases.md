# Register clusters, databases and document types

Register MongoDB clusters, logical databases and typed document bindings once, with `AddMongoDb`. Package: `Dilcore.MongoDB`.

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

// connectionString comes from configuration or a secret store, never from source code.
services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddCluster("archive", c => c.UseConnectionString(archiveConnectionString))
    .AddDatabase("shop", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
    })
    .AddDatabase("shop-archive", db =>
    {
        db.OnCluster("archive");

        // Same CLR type, different binding key and database.
        db.AddDocumentBinding<Order>("archived-orders", d => d.WithCollectionName("orders"));
    }));
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Repositories;
using FluentResults;

public sealed class ArchiveService(IRepositoryResolver repositories)
{
    public async Task<Result> ArchiveAsync(Order order, CancellationToken cancellationToken)
    {
        // Two bindings share the Order type, so resolve by binding key.
        var archived = repositories.GetRepository<Order>("archived-orders");

        var stored = await archived.StoreAsync(order, cancellationToken);

        return stored.ToResult();
    }
}
```

Facts about the registration:

- `AddMongoDb` is the single entry point. It registers one singleton `IMongoClient` per cluster (keyed by the cluster name), scoped `IMongoDatabase` per database (keyed by the database name), and scoped repositories per binding (keyed by the binding key).
- `IGenericRepository<T>` can be injected without a key only when `T` has exactly one binding. With several bindings of the same type, use `IRepositoryResolver.GetRepository<T>(bindingKey)` or a keyed service.
- A cluster can also wrap a client you already own: `c.UseExistingClient(client, MongoClientOwnership.ExternalOwned)` (namespace `Dilcore.MongoDB.Abstractions.Ownership`). Use either a connection string or an existing client for one cluster, not both.
- `WithCollectionName` is required on every binding. Binding keys must be unique.

## Handle failures

Registration fails fast, at startup, with `InvalidOperationException` (duplicate cluster, database or binding key, no cluster registered, a cluster without databases, a database that references an unknown cluster, a missing collection name, `WithSoftDelete` on a type that is not `ISoftDeletable`). Fix the registration; there is nothing to catch at runtime.

Repository calls return `FluentResults` `Result` values. A database key that is unknown, or a namespace that cannot be resolved, is a failed `Result`. See [Handle typed errors](handle-errors.md).

## Pitfalls

- Call `AddMongoDb` once. The toolkit creates the clients, so do not create `MongoClient` instances per request or per service.
- Never hard-code a connection string and never log it. See [Production MongoDB posture](../security/production-mongodb.md).
- Repositories are scoped. Resolve them from a request scope, not from the root provider.
- BSON conventions are process-wide and set once in `AddMongoDb` (see [ADR 0003](../adr/0003-serialization-conventions.md)). Do not register conflicting conventions elsewhere.
- Bulk and projection repositories are opt-in per binding: [Bulk writes](bulk-writes.md), [Projections](projections.md).

## See also

- [Define document types](define-documents.md)
- [Multi-tenant database names](multi-tenant-databases.md)
- [Choose a package](../product/package-selection.md)
- [Samples](../../samples/MongoDb.WebApi.Sample)
- Runnable example: [RegisterClustersAndDatabasesExampleTests](../../test/IntegrationTests/Examples/RegisterClustersAndDatabasesExampleTests.cs)
