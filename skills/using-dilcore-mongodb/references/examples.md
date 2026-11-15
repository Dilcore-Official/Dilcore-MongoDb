# Worked examples

The most likely combinations, each with comments that explain **why** a line is there. The shapes follow the task guides listed in [docs-index.md](docs-index.md), so check the guide for the exact members when the package version differs.

Namespaces used below:

```csharp
using System.Text.Json;                            // JsonElement
using Dilcore.MongoDB.Abstractions;                // IDocumentEntity<T>, GuidIdGenerationStrategy, IMongoDbCollectionFactory
using Dilcore.MongoDB.Abstractions.Json;           // JsonBsonConversionOptions, JsonBsonOutputMode
using Dilcore.MongoDB.Abstractions.Keys;           // MongoClusterKey, MongoDatabaseKey, MongoDocumentBindingKey
using Dilcore.MongoDB.Abstractions.Namespace;      // INamespacePrefixResolver, NamespaceResolutionRequest
using Dilcore.MongoDB.Abstractions.Options;        // KeysetPageRequest<T>, MongoBulkWriteOptions
using Dilcore.MongoDB.Abstractions.Policies;       // IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
using Dilcore.MongoDB.Abstractions.Provisioning;   // IMongoDbProvisioner
using Dilcore.MongoDB.Abstractions.Repositories;   // IGenericRepository<T>, IRepositoryResolver
using Dilcore.MongoDB.Abstractions.Results;        // DocumentNotFoundError, DuplicateKeyError, ...
using Dilcore.MongoDB.Abstractions.Transactions;   // IMongoDbTransactionRunner, MongoTransactionOptions
using Dilcore.MongoDB.Extensions;                  // AddMongoDb
using Dilcore.MongoDB.Repositories;                // GetAsync(id), GetListAsync(predicate)
using Dilcore.MongoDB.SystemTextJson;              // AddSystemTextJsonDocuments, ISystemTextJsonDocumentStoreFactory
using FluentResults;
using MongoDB.Bson;                                // BsonDocument
using MongoDB.Driver;
```

## 1. One database, CRUD with concurrency, soft delete and audit stamps

The most common start: a web API with one MongoDB database and one document type.

```csharp
// The document opts in to exactly the policies it needs. Nothing is added implicitly.
public sealed class Order : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
{
    public Guid Id { get; set; }              // Guid id, generated on insert (see the binding below)
    public long ETag { get; set; }            // optimistic concurrency token, managed by the repository
    public bool IsDeleted { get; set; }       // soft delete flag, managed by the repository
    public DateTime CreatedAt { get; set; }   // audit stamps, managed by the repository
    public DateTime UpdatedAt { get; set; }
    public string Sku { get; set; } = "";
    public DateTime ExpiresAt { get; set; }   // used by the TTL index in example 3
}

// Registration: once, at startup. The connection string comes from configuration, never from code.
services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(configuration.GetConnectionString("Mongo")!))
    .AddDatabase("shop", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Order>("orders", d => d
            .WithCollectionName("orders")
            .WithSoftDelete()                                                        // reads hide IsDeleted documents
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));     // time-ordered ids, Guid documents only
    }));

public sealed class OrderService(IGenericRepository<Order> orders)   // injectable directly: Order has one binding
{
    // StoreAsync inserts when Id is empty and updates when it is set. ETag and audit fields are staged for you.
    public Task<Result<Order>> CreateAsync(Order order, CancellationToken cancellationToken = default)
        => orders.StoreAsync(order, cancellationToken);

    public Task<Result<Order>> FindAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<Order>.Filter.Eq(x => x.Id, id);

        return orders.GetAsync(filter, cancellationToken);
    }

    // A stale ETag fails with ConcurrencyConflictError. Do not retry blindly: re-read the document first.
    public Task<Result<Order>> ReplaceAsync(Order changed, CancellationToken cancellationToken = default)
        => orders.ReplaceAsync(changed, cancellationToken);
}
```

## 2. Mapping typed errors to HTTP

Expected failures are values, not exceptions. Map by type in one place.

```csharp
public static class ResultHttp
{
    public static IResult ToHttp<T>(this Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Results.Ok(result.Value);
        }

        // Order matters only when one result can carry several errors; check the most specific first.
        if (result.HasError<DocumentNotFoundError>())
        {
            return Results.NotFound();
        }

        // Stale ETag or a duplicate key are both "the caller's view is out of date" for an API client.
        if (result.HasError<ConcurrencyConflictError>() || result.HasError<DuplicateKeyError>())
        {
            return Results.Conflict();
        }

        // The outcome may be unknown, including an unknown commit result that already succeeded.
        // Retry only an idempotent operation, or reconcile the write before trying again.
        if (result.HasError<TransientWriteError>())
        {
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        // Anything else is unexpected. Log the errors, do not leak them to the client.
        return Results.Problem();
    }
}
```

## 3. Startup provisioning with named indexes and a TTL

Indexes are created explicitly, outside the request path.

```csharp
db.AddDocumentBinding<Order>("orders", d => d
    .WithCollectionName("orders")
    .WithIndexes(new CreateIndexModel<Order>(
        Builders<Order>.IndexKeys.Ascending(x => x.Sku),
        // Always name the index. Apply compares by name to detect drift and removals.
        new CreateIndexOptions { Name = "orders_sku", Unique = true }))
    // Adds a named expire-after index on ExpiresAt. One day is added to that timestamp.
    // The server may delete the document after it becomes eligible.
    .WithCollectionItemsTimeToLive(TimeSpan.FromDays(1), x => x.ExpiresAt));

// In Program.cs, after the host is built and before it starts serving.
using var scope = app.Services.CreateScope();
var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

// Preview first. Apply can remove indexes that are not on the binding, including unique indexes.
var preview = await provisioner.DryRunAsync();
if (preview.IsFailed)
{
    throw new InvalidOperationException("MongoDB provisioning preview failed.");
}

var wouldRemove = preview.Value.Steps.Any(step => step.Action == "would-remove");
if (wouldRemove)
{
    throw new InvalidOperationException("Review the index removal and approve it before applying.");
}

// Keep the preview for that review. Do not call ApplyAsync until the removal is explicitly approved.
// Run provisioning from one deployment step. ApplyAsync recomputes removals from the current index list,
// so an index created after DryRunAsync is removed without passing the would-remove check.
```

## 4. Several clusters and databases

Each cluster gets one shared client. Bindings of the same type under different keys are resolved by key.

```csharp
services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(primaryConnectionString))
    .AddCluster("archive", c => c.UseConnectionString(archiveConnectionString))
    .AddDatabase("shop", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
    })
    .AddDatabase("shop-archive", db =>
    {
        db.OnCluster("archive");
        // Same CLR type, different binding key and database. Unkeyed IGenericRepository<Order> is no longer available.
        db.AddDocumentBinding<Order>("archived-orders", d => d.WithCollectionName("orders"));
    }));

public sealed class ArchiveService(IRepositoryResolver repositories)
{
    public async Task<Result> ArchiveAsync(Order order, CancellationToken cancellationToken = default)
    {
        // Resolve by binding key because two bindings share the Order type.
        var archived = repositories.GetRepository<Order>("archived-orders");

        var stored = await archived.StoreAsync(order, cancellationToken);

        return stored.ToResult();
    }
}
```

## 5. Multi-tenant database names that fail closed

The library has no tenant type. Your resolver supplies the prefix. No tenant means no database.

```csharp
public sealed class TenantDatabasePrefixResolver(ITenantAccessor tenants) : INamespacePrefixResolver   // ITenantAccessor is your app's service
{
    public async Task<Result<string?>> ResolveAsync(
        NamespaceResolutionRequest request,
        CancellationToken cancellationToken = default)
    {
        var tenant = await tenants.GetCurrentAsync(cancellationToken);

        // Fail closed. Never fall back to "" or a default tenant: that would read and write a shared database.
        if (tenant is null || string.IsNullOrWhiteSpace(tenant.Id))
        {
            return Result.Fail<string?>("Tenant context is required.");
        }

        return Result.Ok<string?>(tenant.Id);
    }
}

// AddMongoDb registers the resolver, not ITenantAccessor. Register your accessor first.
services.AddScoped<ITenantAccessor, RequestTenantAccessor>();

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("shop", db =>
    {
        db.OnCluster("primary");
        // The resolver is scoped, so it sees the current request's tenant. The physical name becomes "{tenantId}_shop".
        db.WithNamespacePrefixResolver<TenantDatabasePrefixResolver>();
        db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
    }));
```

A repository call made without a tenant returns a failed `Result`. Test that case explicitly.

## 6. A transaction across two collections

Needs a replica set. Repositories come from the transaction context.

```csharp
public sealed class CheckoutService(IMongoDbTransactionRunner runner)
{
    public Task<Result<Guid>> PlaceOrderAsync(Order order, CancellationToken cancellationToken = default)
    {
        var clusterKey = new MongoClusterKey("primary");
        var options = new MongoTransactionOptions
        {
            MaxOperations = 100,                     // client-side budget, tighter than the 1000 default
            TimeLimit = TimeSpan.FromSeconds(10)
        };

        return runner.WithTransactionAsync(
            clusterKey,
            async (tx, ct) =>
            {
                // Take repositories from the context, not from DI, so the writes join this session.
                var orders = tx.Repositories.GetRepository<Order>("orders");
                var payments = tx.Repositories.GetRepository<Payment>("payments");

                // A retry runs this callback again. A fresh instance keeps Id and ETag unset
                // after a rolled-back insert, so the next attempt still takes the insert path.
                var attempt = new Order
                {
                    Id = order.Id,
                    Sku = order.Sku,
                    ExpiresAt = order.ExpiresAt
                };
                var stored = await orders.StoreAsync(attempt, ct);
                if (stored.IsFailed)
                {
                    return stored.ToResult();            // a failed Result aborts the transaction
                }

                var paymentDocument = new Payment { OrderId = stored.Value.Id };
                var payment = await payments.StoreAsync(paymentDocument, ct);
                if (payment.IsFailed)
                {
                    return payment.ToResult();
                }

                return Result.Ok(stored.Value.Id);
            },
            options,
            cancellationToken);
    }
}
```

The callback can run again after a transient error. Build each stored entity inside the callback so a rolled-back attempt does not leave `Id` or `ETag` set on the caller's instance. Do not send e-mails or call other systems inside it. A failure before commit aborts the transaction. A commit-stage failure, such as `UnknownTransactionCommitResult`, can leave the outcome unknown.

## 7. Keyset paging

The `_id` tie-breaker orders equal sort values. An immutable sort key stops a document from moving when its sort value changes. Inserts and deletes between page reads can still add or drop documents. A consistent snapshot needs snapshot reads.

```csharp
public Task<Result<KeysetPage<Order>>> ListAsync(string? cursor, CancellationToken cancellationToken = default)
{
    var request = new KeysetPageRequest<Order>
    {
        Filter = Builders<Order>.Filter.Empty,
        Sort = Builders<Order>.Sort.Descending(x => x.CreatedAt),   // the cursor is bound to this sort
        PageSize = 50,
        Cursor = cursor                                              // NextCursor of the previous page, or null for the first
    };

    return orders.GetPageAsync(request, cancellationToken);
}

// The caller sends page.Value.NextCursor back while page.Value.HasMore is true.
// A malformed cursor, or a cursor used with another sort, returns a failed Result instead of throwing.
```

## 8. Documents without a CLR type (System.Text.Json)

For payloads whose shape you do not own. Uses the same database and prefix pipeline as typed documents.

```csharp
services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("app", db => db.OnCluster("primary")));

// Must come after AddMongoDb, otherwise registration throws.
services.AddSystemTextJsonDocuments();

public sealed class PayloadService(ISystemTextJsonDocumentStoreFactory stores)
{
    public async Task<Result<JsonElement>> RoundTripAsync(
        JsonElement payload,
        CancellationToken cancellationToken = default)
    {
        var databaseKey = new MongoDatabaseKey("app");
        var store = stores.Create(databaseKey);

        // The collection is a plain string. No typed binding is needed.
        var inserted = await store.InsertAsync("payloads", payload, cancellationToken: cancellationToken);
        if (inserted.IsFailed)
        {
            return inserted.ToResult();
        }

        // The result is Canonical Extended JSON unless you pass relaxed options (see below).
        return await store.GetJsonElementByIdAsync("payloads", inserted.Value["_id"], cancellationToken: cancellationToken);
    }
}
```

Output defaults to Canonical Extended JSON, which keeps BSON types (`ObjectId`, `long`, `decimal`, dates) but writes numbers as `{"$numberInt": "3"}`. Pass `JsonBsonConversionOptions` with `OutputMode = JsonBsonOutputMode.RelaxedExtendedJson` to read plain JSON back. For Newtonsoft use `AddNewtonsoftJsonDocuments()`, `InsertAsync("payloads", JObject.Parse(json))` and `GetJObjectByIdAsync`.

## 9. Bulk writes and partial failure

Enable the bulk repository on the binding first.

```csharp
db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders").WithBulkRepository());

public async Task<Result> ImportAsync(
    IReadOnlyList<Order> batch,
    IGenericBulkRepository<Order> bulk,
    CancellationToken cancellationToken = default)
{
    var options = new MongoBulkWriteOptions { IsOrdered = false };
    var result = await bulk.BulkStoreRangeAsync(batch, options, cancellationToken);

    // Unordered continues past a failing item only inside the current chunk. A write error returns before later chunks are sent.
    if (result.HasError<BulkWritePartialFailureError>())
    {
        var failed = result.Errors.OfType<BulkWritePartialFailureError>().SelectMany(e => e.Items).Where(i => !i.Succeeded);
        // Report or retry only the failed items.
    }

    return result.ToResult();
}
```

## 10. Reaching the driver when the toolkit is not enough

```csharp
public sealed class ReportService(IMongoDbCollectionFactory collections)
{
    public async Task<Result<IReadOnlyList<BsonDocument>>> GetTopSkusAsync(
        CancellationToken cancellationToken = default)
    {
        // The factory resolves the physical name through the same namespace pipeline (prefixes, tenants) as the repositories.
        var bindingKey = new MongoDocumentBindingKey("orders");
        var resolved = await collections.GetCollectionAsync<Order>(bindingKey, cancellationToken);
        if (resolved.IsFailed)
        {
            return resolved.ToResult();
        }

        var collection = resolved.Value;

        // Aggregation stays a driver API. Do not hide it behind your own abstraction.
        var top = await collection.Aggregate()
            .Group(new BsonDocument { { "_id", "$sku" }, { "count", new BsonDocument("$sum", 1) } })
            .SortByDescending(x => x["count"])
            .Limit(10)
            .ToListAsync(cancellationToken);

        return Result.Ok<IReadOnlyList<BsonDocument>>(top);
    }
}
```

When you need the client itself, resolve the keyed client: `provider.GetRequiredKeyedService<IMongoClient>("primary")`.

To get only a collection, with no repository, inject it by binding key, or use the factory for a collection that has no binding:

```csharp
public sealed class AuditWriter(
    [FromKeyedServices("orders")] IMongoCollection<Order> orders,   // key = binding key, scoped
    IMongoDbCollectionFactory collections)
{
    public async Task<Result> WriteAsync(string message, CancellationToken cancellationToken = default)
    {
        // Untyped collection by logical name. The namespace pipeline still applies.
        var databaseKey = new MongoDatabaseKey("shop");
        var audit = await collections.GetCollectionAsync(databaseKey, "audit-log", cancellationToken: cancellationToken);
        if (audit.IsFailed)
        {
            return audit.ToResult();
        }

        var document = new BsonDocument("message", message);
        await audit.Value.InsertOneAsync(document, cancellationToken: cancellationToken);
        return Result.Ok();
    }
}
```

A raw collection has no soft-delete filter, ETag check, audit stamp or id generation.

## 11. Expiring documents with TTL

`WithCollectionItemsTimeToLive` only records the index. Call `IMongoDbProvisioner.ApplyAsync` before serving traffic, or nothing expires. The monitor deletes on a background pass, so a read that must hide a document at its deadline filters on that timestamp.

```csharp
public sealed class UserSession : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = "";
    public DateTime? ExpiresAt { get; set; }   // UTC deadline, or null to keep the document
}

public sealed class AuditEntry : IDocumentEntity<Guid>, IAuditableDocument
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }    // stamped in UTC by the repository on insert
    public DateTime UpdatedAt { get; set; }
    public string Action { get; set; } = "";
}

db.AddDocumentBinding<UserSession>("sessions", d => d
    .WithCollectionName("sessions")
    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
    // Zero means "expire at ExpiresAt". A null field is not a BSON Date, so the monitor skips it.
    .WithCollectionItemsTimeToLive(TimeSpan.Zero, x => x.ExpiresAt));

db.AddDocumentBinding<AuditEntry>("audit-entries", d => d
    .WithCollectionName("audit_entries")
    .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
    // Eligible 30 days after CreatedAt. The index is named audit_entries_ttl.
    .WithCollectionItemsTimeToLive(TimeSpan.FromDays(30), x => x.CreatedAt));

public sealed class SessionService(IGenericRepository<UserSession> sessions)
{
    // Slide the deadline, or pin the session by removing the date.
    public Task<Result<UserSession>> ExtendAsync(
        Guid id,
        DateTime expiresAt,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<UserSession>.Filter.Eq(x => x.Id, id);
        var update = Builders<UserSession>.Update.Set(x => x.ExpiresAt, expiresAt);

        return sessions.PatchAsync(filter, update, cancellationToken);
    }

    public Task<Result<UserSession>> PinAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var filter = Builders<UserSession>.Filter.Eq(x => x.Id, id);
        var update = Builders<UserSession>.Update.Unset(x => x.ExpiresAt);

        return sessions.PatchAsync(filter, update, cancellationToken);
    }

    // Excludes a past deadline immediately. Do not wait for the TTL monitor.
    public Task<Result<IReadOnlyList<UserSession>>> ListActiveAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default)
        => sessions.GetListAsync(x => x.ExpiresAt == null || x.ExpiresAt > utcNow, cancellationToken);
}
```
