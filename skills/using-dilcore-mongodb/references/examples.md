# Worked examples

The most likely combinations, each with comments that explain **why** a line is there. The shapes follow the samples and guides in the repository (`samples/`, `docs/guides/`), so check the guide for the exact members when the package version differs.

Namespaces used below:

```csharp
using Dilcore.MongoDB.Abstractions;                // IDocumentEntity<T>, policy interfaces, GuidIdGenerationStrategy
using Dilcore.MongoDB.Abstractions.Keys;           // MongoClusterKey, MongoDatabaseKey, MongoDocumentBindingKey
using Dilcore.MongoDB.Abstractions.Namespace;      // INamespacePrefixResolver, NamespaceResolutionRequest
using Dilcore.MongoDB.Abstractions.Options;        // KeysetPageRequest<T>, MongoBulkWriteOptions
using Dilcore.MongoDB.Abstractions.Provisioning;   // IMongoDbProvisioner
using Dilcore.MongoDB.Abstractions.Repositories;   // IGenericRepository<T>, IRepositoryResolver
using Dilcore.MongoDB.Abstractions.Results;        // DocumentNotFoundError, DuplicateKeyError, ...
using Dilcore.MongoDB.Abstractions.Transactions;   // IMongoDbTransactionRunner, MongoTransactionOptions
using Dilcore.MongoDB.Extensions;                  // AddMongoDb
using FluentResults;
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
    public Task<Result<Order>> CreateAsync(Order order, CancellationToken cancellationToken)
        => orders.StoreAsync(order, cancellationToken);

    public Task<Result<Order>> FindAsync(Guid id, CancellationToken cancellationToken)
        => orders.GetAsync(Builders<Order>.Filter.Eq(x => x.Id, id), cancellationToken);

    // A stale ETag fails with ConcurrencyConflictError. Do not retry blindly: re-read the document first.
    public Task<Result<Order>> ReplaceAsync(Order changed, CancellationToken cancellationToken)
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
            return Results.Ok(result.Value);

        // Order matters only when one result can carry several errors; check the most specific first.
        if (result.HasError<DocumentNotFoundError>())
            return Results.NotFound();

        // Stale ETag or a duplicate key are both "the caller's view is out of date" for an API client.
        if (result.HasError<ConcurrencyConflictError>() || result.HasError<DuplicateKeyError>())
            return Results.Conflict();

        // Transient write errors are safe to retry. Tell the client so.
        if (result.HasError<TransientWriteError>())
            return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);

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
    // Adds a named expire-after index on ExpiresAt. Documents are removed by the server after one day.
    .WithCollectionItemsTimeToLive(TimeSpan.FromDays(1), x => x.ExpiresAt));

// In Program.cs, after the host is built and before it starts serving.
using (var scope = app.Services.CreateScope())
{
    var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();

    // Optional but wise for an existing database: preview what would change.
    var preview = await provisioner.DryRunAsync();

    var report = await provisioner.ApplyAsync();
    if (report.IsFailed)
        throw new InvalidOperationException("MongoDB provisioning failed.");   // fail closed at startup
}
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
    public async Task<Result> ArchiveAsync(Order order, CancellationToken cancellationToken)
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
        if (tenant is null)
            return Result.Fail<string?>("Tenant context is required.");

        return Result.Ok<string?>(tenant.Id);
    }
}

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
    public Task<Result<Guid>> PlaceOrderAsync(Order order, CancellationToken cancellationToken)
        => runner.WithTransactionAsync(
            new MongoClusterKey("primary"),
            async (tx, ct) =>
            {
                // Take repositories from the context, not from DI, so the writes join this session.
                var orders = tx.Repositories.GetRepository<Order>("orders");
                var payments = tx.Repositories.GetRepository<Payment>("payments");

                var stored = await orders.StoreAsync(order, ct);
                if (stored.IsFailed)
                    return stored.ToResult();            // a failed Result aborts the transaction

                var payment = await payments.StoreAsync(new Payment { OrderId = stored.Value.Id }, ct);
                if (payment.IsFailed)
                    return payment.ToResult();

                return Result.Ok(stored.Value.Id);
            },
            new MongoTransactionOptions
            {
                MaxOperations = 100,                     // client-side budget, tighter than the 1000 default
                TimeLimit = TimeSpan.FromSeconds(10)
            },
            cancellationToken);
}
```

The callback can run again after a transient error. Do not send e-mails or call other systems inside it.

## 7. Keyset paging

Stable pages that do not skip or repeat rows when sort values collide.

```csharp
public async Task<Result<KeysetPage<Order>>> ListAsync(string? cursor, CancellationToken cancellationToken)
    => await orders.GetPageAsync(
        new KeysetPageRequest<Order>
        {
            Filter = Builders<Order>.Filter.Empty,
            Sort = Builders<Order>.Sort.Descending(x => x.CreatedAt),   // the cursor is bound to this sort
            PageSize = 50,
            Cursor = cursor                                              // NextCursor of the previous page, or null for the first
        },
        cancellationToken);

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
    public async Task<Result<JsonElement>> RoundTripAsync(JsonElement payload, CancellationToken cancellationToken)
    {
        var store = stores.Create(new MongoDatabaseKey("app"));

        // The collection is a plain string. No typed binding is needed.
        var inserted = await store.InsertAsync("payloads", payload);
        if (inserted.IsFailed)
            return inserted.ToResult();

        return await store.GetJsonElementByIdAsync("payloads", inserted.Value["_id"]);
    }
}
```

For BSON type fidelity (`ObjectId`, `long`, `decimal`, dates) pass `JsonBsonConversionOptions` with `OutputMode = JsonBsonOutputMode.CanonicalExtendedJson`. For Newtonsoft use `AddNewtonsoftJsonDocuments()`, `InsertAsync("payloads", JObject.Parse(json))` and `GetJObjectByIdAsync`.

## 9. Bulk writes and partial failure

Enable the bulk repository on the binding first.

```csharp
db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders").WithBulkRepository());

public async Task<Result> ImportAsync(IReadOnlyList<Order> batch, IGenericBulkRepository<Order> bulk, CancellationToken cancellationToken)
{
    var result = await bulk.BulkStoreRangeAsync(batch, new MongoBulkWriteOptions { IsOrdered = false }, cancellationToken);

    // Some items can succeed while others fail. The error lists every item by its index in the input.
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
    public async Task<IReadOnlyList<BsonDocument>> TopSkusAsync(CancellationToken cancellationToken)
    {
        // The factory resolves the physical name through the same namespace pipeline (prefixes, tenants) as the repositories.
        var collection = (await collections.GetCollectionAsync<Order>(new MongoDocumentBindingKey("orders"))).Value;

        // Aggregation stays a driver API. Do not hide it behind your own abstraction.
        return await collection.Aggregate()
            .Group(new BsonDocument { { "_id", "$sku" }, { "count", new BsonDocument("$sum", 1) } })
            .SortByDescending(x => x["count"])
            .Limit(10)
            .ToListAsync(cancellationToken);
    }
}
```

When you need the client itself, resolve the keyed client: `provider.GetRequiredKeyedService<IMongoClient>("primary")`.
