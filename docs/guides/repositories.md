# Repositories

**Current.** Typed CRUD, bulk, and projection helpers over `MongoDB.Driver` with FluentResults. They share the same namespace-resolution pipeline as collections.

Samples: [samples/MongoDb.Capabilities.Sample](../../samples/MongoDb.Capabilities.Sample), [samples/MongoDb.WebApi.Sample](../../samples/MongoDb.WebApi.Sample). Live members: [Abstractions Shipped](../../src/Dilcore.MongoDB.Abstractions/PublicAPI.Shipped.txt) / [Unshipped](../../src/Dilcore.MongoDB.Abstractions/PublicAPI.Unshipped.txt), [MongoDB Shipped](../../src/Dilcore.MongoDB/PublicAPI.Shipped.txt) / [Unshipped](../../src/Dilcore.MongoDB/PublicAPI.Unshipped.txt).

Enable bulk or projection per binding:

```csharp
db.AddDocumentBinding<Order>("orders", d => d
    .WithCollectionName("orders")
    .WithBulkRepository()
    .WithProjectionRepository());
```

Unkeyed `IGenericRepository<T>` is registered only when a type has a single binding. Multiple bindings of the same type must be resolved with a key or `IRepositoryResolver.GetRepository<T>(bindingKey)`.

## Generic repository

`IGenericRepository<TDocument>` (`TDocument : IDocumentEntity`):

| Method | Role |
|--------|------|
| `StoreAsync` | Insert or update. Empty Id inserts; a caller-assigned Id inserts when missing and snapshots when present. Policy fields (ETag, audit) are staged before the write and restored on `MongoException`, oversized BSON `FormatException`, or cancellation (generated Id cleared only on cancellation). |
| `ReplaceAsync` | Full stored-document replacement. With `IHasConcurrencyToken`, a stale ETag returns `ConcurrencyConflictError`; a missing document returns `DocumentNotFoundError`. |
| `UpdateSnapshotAsync` | `$set` mutable snapshot; excludes `_id` (unknown stored fields are kept). Honors the same ETag rules as `StoreAsync`. |
| `PatchAsync` | Caller-supplied `UpdateDefinition`. Miss with a concurrency token → `ConcurrencyConflictError`; without a token → `DocumentNotFoundError`. |
| `GetAsync` / `GetAsync<TDerived>` | Single document; missing → `DocumentNotFoundError` |
| `GetListAsync` (several overloads) | Lists with optional filter/`FindOptions` and derived types |
| `GetPageAsync` | Keyset page (`KeysetPageRequest<T>` / `KeysetPage<T>`) |
| `GetAsyncEnumerable` | Streaming read (see caveat below) |
| `DeleteAsync` | Soft-delete when `WithSoftDelete()` is on the binding. No match → `DocumentNotFoundError`; `DeleteAsync(id, eTag)` with a stale token → `ConcurrencyConflictError`. |
| `RestoreAsync` / `PurgeAsync` | Restore a soft-deleted document; `PurgeAsync` permanently deletes every filter match (including documents that are not soft-deleted) |
| `HasAnyAsync` / `CountAsync` | Same not-deleted filter as reads when soft delete is enabled |

`Dilcore.MongoDB.Repositories.GenericRepositoryExtensions` adds `GetAsync(id)`, `GetListAsync(expression)`, and `DeleteAsync(id, eTag)` for `IDocumentEntity<TId>` / `IHasConcurrencyToken`.

### Replace vs snapshot vs patch

- **Replace** — full stored document replacement; ETag-guarded when the document implements `IHasConcurrencyToken`.
- **UpdateSnapshot** — `$set` of the mutable snapshot without `_id`; extra BSON fields not on the CLR type remain.
- **Patch** — only the `UpdateDefinition` you pass; Dilcore does not invent a full-document `$set`. Policy field names follow the document's BSON mapping (`[BsonElement]`), not hardcoded camelCase.

### Keyset paging

```csharp
var page = await repository.GetPageAsync(new KeysetPageRequest<Order>
{
    Filter = Builders<Order>.Filter.Empty,
    Sort = Builders<Order>.Sort.Descending(x => x.CreatedAt),
    PageSize = 50,
    Cursor = previous?.NextCursor
});
```

`KeysetPage<T>` returns `Items`, `NextCursor`, and `HasMore`.

`NextCursor` is an opaque token bound to the sort that produced it. Reusing a
cursor with a different sort fails. The repository appends `_id` ascending as a
tie-breaker when the requested sort does not already include `_id`, so pages stay
stable when sort values collide. Sort directions must be `1` or `-1` (no `$meta`
sorts). Optional `Projection` must keep every sort key (including `_id`) and must
not replace a sort key with a computed expression. Array-valued sort fields are
rejected. Continuation seeks follow MongoDB BSON type order for scalar values.
Malformed cursors return a failed `Result` instead of throwing.

### Streaming caveat

`GetAsyncEnumerable` throws `CollectionResolutionException` when collection resolution fails. Result-returning methods use FluentResults instead. M4 may still redesign streaming onto Result; do not treat the exception model as frozen.

## Bulk repository

`IGenericBulkRepository<TDocument>`:

- `BulkStoreAsync(TDocument[])` and `BulkStoreRangeAsync(IEnumerable<TDocument>)`
- Overloads that take `MongoBulkWriteOptions` (`IsOrdered`, default `true`; `MaxOperationsPerBatch`)
- `BulkDeleteAsync(Expression<Func<TDocument, bool>>)`

Partial failures return `BulkWritePartialFailureError` with `Items` (`BulkWriteItemResult`: original input index, succeeded, errorMessage). Indices stay aligned across `MaxOperationsPerBatch` chunks. Unmatched ETag updates are failures, not silent success. Entities whose writes committed keep staged ETag/audit values; uncommitted items are restored. A bulk delete that matches nothing returns `DocumentNotFoundError`.

## Projection repository

`IGenericProjectionRepository<TDocument>`: `GetAsync<TProjection>` and `GetListAsync<TProjection>` with a `FilterDefinition` plus `Expression<Func<TDocument, TProjection>>`, or list with projection only.

## Typed operation errors

Expected failures are `MongoOperationError` subtypes (FluentResults `Error`). Map by type or `Code`:

| Type | `Code` |
|------|--------|
| `DocumentNotFoundError` | `document_not_found` |
| `ConcurrencyConflictError` | `concurrency_conflict` |
| `DuplicateKeyError` | `duplicate_key` |
| `TransientWriteError` | `transient_write` |
| `WriteConcernFailureError` | `write_concern_failure` |
| `DocumentTooLargeError` | `document_too_large` |
| `CrossClusterOperationError` | `cross_cluster_operation` |
| `TransactionBudgetExceededError` | `transaction_budget_exceeded` |
| `BulkWritePartialFailureError` | `bulk_write_partial_failure` |

```csharp
if (result.HasError<DocumentNotFoundError>())
    return Results.NotFound();
if (result.HasError<ConcurrencyConflictError>())
    return Results.Conflict();
```

## Transaction-scoped repositories

Inside `IMongoDbTransactionRunner` callbacks, resolve through `IMongoDbTransactionContext.Repositories` (`IRepositoryResolver`) so work uses the session. See [transactions.md](transactions.md).
