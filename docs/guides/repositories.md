# Repositories

**Current.** Typed CRUD, bulk, and projection helpers over `MongoDB.Driver` with FluentResults. They share the same namespace-resolution pipeline as collections.

Sample: [samples/MongoDb.WebApi.Sample](../../samples/MongoDb.WebApi.Sample). Live members: `src/*/PublicAPI.*.txt`.

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
| `StoreAsync` | Insert (applies configured Guid generation and policies after ack) |
| `ReplaceAsync` | Replace stored document |
| `UpdateSnapshotAsync` | `$set` mutable snapshot; excludes `_id` |
| `PatchAsync` | Caller-supplied `UpdateDefinition` |
| `GetAsync` / `GetAsync<TDerived>` | Single document; missing → `DocumentNotFoundError` |
| `GetListAsync` (several overloads) | Lists with optional filter and derived types |
| `GetAsyncEnumerable` | Streaming read (see caveat below) |
| `DeleteAsync` | Soft-delete when `WithSoftDelete()` is on the binding |
| `HasAnyAsync` / `CountAsync` | Same not-deleted filter as reads when soft delete is enabled |

`Dilcore.MongoDB.Repositories.GenericRepositoryExtensions` adds `GetAsync(id)`, `GetListAsync(expression)`, and `DeleteAsync(id, eTag)` for `IDocumentEntity<TId>` / `IHasConcurrencyToken`.

### Replace vs snapshot vs patch

- **Replace** — full stored document replacement.
- **UpdateSnapshot** — `$set` of the mutable snapshot without `_id`.
- **Patch** — only the `UpdateDefinition` you pass; Dilcore does not invent a full-document `$set`.

### Streaming caveat

`GetAsyncEnumerable` throws `CollectionResolutionException` when collection resolution fails. Result-returning methods use FluentResults instead. M4 may still redesign streaming onto Result; do not treat the exception model as frozen.

## Bulk repository

`IGenericBulkRepository<TDocument>`:

- `BulkStoreAsync(TDocument[])` and `BulkStoreRangeAsync(IEnumerable<TDocument>)`
- Overloads that take `MongoBulkWriteOptions` (`IsOrdered`, default `true`; `MaxOperationsPerBatch`)
- `BulkDeleteAsync(Expression<Func<TDocument, bool>>)`

Unordered writes that partially fail return `BulkWritePartialFailureError` with `Items` (`BulkWriteItemResult`: index, succeeded, errorMessage).

## Projection repository

`IGenericProjectionRepository<TDocument>`: `GetAsync<TProjection>` and `GetListAsync<TProjection>` with a `FilterDefinition` plus `Expression<Func<TDocument, TProjection>>`, or list with projection only.

## Typed operation errors

Expected failures are `MongoOperationError` subtypes (FluentResults `Error`). Map by type or `Code`:

| Type | `Code` |
|------|--------|
| `DocumentNotFoundError` | `document_not_found` |
| `ConcurrencyConflictError` | `concurrency_conflict` |
| `DocumentTooLargeError` | `document_too_large` |
| `BulkWritePartialFailureError` | `bulk_write_partial_failure` |

```csharp
if (result.HasError<DocumentNotFoundError>())
    return Results.NotFound();
if (result.HasError<ConcurrencyConflictError>())
    return Results.Conflict();
```
