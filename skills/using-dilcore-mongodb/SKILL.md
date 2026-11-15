---
name: using-dilcore-mongodb
description: Use when writing, configuring, testing or debugging .NET code that uses the Dilcore.MongoDB packages (AddMongoDb, IGenericRepository, document bindings, namespace prefix resolvers, IMongoDbProvisioner, IMongoDbTransactionRunner, IJsonDocumentStore, DuplicateKeyError and other typed MongoOperationError results). Also use to decide whether a project should use the toolkit or plain MongoDB.Driver.
license: MIT
compatibility: .NET 10, MongoDB.Driver, FluentResults. Version 2 is in preview, so check the installed package version against the docs.
metadata:
  repository: https://github.com/Dilcore-Official/Dilcore-MongoDb
  packages: Dilcore.MongoDB, Dilcore.MongoDB.Abstractions, Dilcore.MongoDB.SystemTextJson, Dilcore.MongoDB.NewtonsoftJson
---

# Using Dilcore MongoDB

Dilcore MongoDB is an opinionated **application toolkit on top of `MongoDB.Driver`**. It is not a driver replacement, not Amazon DocumentDB, and not a provider-neutral repository. This skill tells you how to use it correctly. It does not repeat the API reference: read the linked guides for signatures and details.

## Before you write code

1. **Find the installed version.** Look for `Dilcore.MongoDB*` in the `.csproj` or `Directory.Packages.props`. Version 2 is in preview (`2.0.0-preview.N`), so the API can change. If the code you are about to write uses a member you cannot confirm, check the docs for that version (see [references/docs-index.md](references/docs-index.md)) instead of guessing.
2. **Check what is actually shipped.** Streaming repositories beyond `GetAsyncEnumerable`, OpenTelemetry instrumentation and vector search are planned, not in the packages. Do not write code against `Dilcore.MongoDB.OpenTelemetry` or `Dilcore.MongoDB.VectorData`.
3. **Prefer docs over memory.** Driver 2.x and 3.x signatures differ. Verify `MongoDB.Driver` calls against the MongoDB C# driver documentation, or against a documentation tool you have (for example Context7 for `/mongodb/mongo-csharp-driver`, if available), rather than from memory.

## Decide: toolkit or plain driver

| Situation | Use |
|-----------|-----|
| One client, a few collection calls, no shared conventions | `MongoDB.Driver` directly. Say so and do not add the toolkit. |
| Several services that should share validated multi-cluster / multi-database setup, typed errors, tenancy, policies | The toolkit |
| Aggregation pipelines, GridFS, change streams, client-side encryption, time series | Driver APIs, reached through the escape hatches |

## Decide: which package

| Need | Package |
|------|---------|
| Application host (DI, repositories, provisioning, transactions) | `Dilcore.MongoDB` (reference this one by default) |
| A library that only needs contracts, no DI host | `Dilcore.MongoDB.Abstractions` |
| JSON documents with System.Text.Json | `Dilcore.MongoDB.SystemTextJson` |
| JSON documents with Newtonsoft.Json | `Dilcore.MongoDB.NewtonsoftJson` (never add it to an app that only uses System.Text.Json) |

Install previews with `dotnet add package Dilcore.MongoDB --prerelease`. All packages must have the same version.

## Decide: which feature

| Task | Feature | Guide |
|------|---------|-------|
| Register clusters, databases, document types | `AddMongoDb` + `AddDocumentBinding<T>` | [register clusters and databases](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/register-clusters-and-databases.md) |
| CRUD, paging, bulk, projections | `IGenericRepository<T>`, bulk and projection repositories | [crud](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/crud-with-repositories.md), [paging](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/keyset-paging.md), [bulk](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/bulk-writes.md), [projections](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/projections.md), [errors](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/handle-errors.md) |
| ETag, audit stamps, soft delete, Guid ids | Policy interfaces + binding options | [documents](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/define-documents.md), [ETag](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/optimistic-concurrency.md), [soft delete](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/soft-delete.md) |
| Per-tenant or per-environment database names | `INamespacePrefixResolver` | [tenants](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/multi-tenant-databases.md), [prefixes](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/namespace-prefixes.md) |
| Indexes, TTL, collections | `IMongoDbProvisioner` | [indexes and TTL](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/provision-indexes-and-ttl.md), [expire documents](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/expire-documents-with-ttl.md), [custom steps](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/custom-provisioning-steps.md) |
| Several writes that must commit together | `IMongoDbTransactionRunner` | [transactions](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/transactions.md) |
| Documents without a CLR type | `IJsonDocumentStore` | [System.Text.Json](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/json-system-text-json.md), [Newtonsoft.Json](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/json-newtonsoft.md), [conversion options](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/json-conversion-options.md) |
| Just the collection or database, no repository | Keyed `IMongoCollection<T>` (key = binding), `IMongoDbCollectionFactory`, keyed `IMongoDatabase` | [resolve collections](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/resolve-collections.md) |
| Anything the toolkit does not wrap | `IMongoDbCollectionFactory` for tenant-aware collections. The keyed `IMongoClient` is a raw client and bypasses namespace resolution | [driver escape hatches](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/product/driver-escape-hatches.md) |

Every guide has the same sections (Setup, Use, Handle failures, Pitfalls, See also). The full list of pages is in [references/docs-index.md](references/docs-index.md). Worked, commented examples for the most common combinations are in [references/examples.md](references/examples.md).

## Rules that prevent real bugs

**Setup and DI**
- Call `AddMongoDb` once. Use one entry point for clusters, databases and bindings. The toolkit creates one singleton `IMongoClient` per unique cluster settings, so do not create `MongoClient` instances yourself.
- Never hard-code a connection string. Read it from configuration or a secret store. Production posture (TLS, least privilege): [production MongoDB guide](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/security/production-mongodb.md).
- `IGenericRepository<T>` can be injected directly only when `T` has **one** binding. With several bindings of the same type, resolve by key through `IRepositoryResolver.GetRepository<T>(bindingKey)`.
- BSON conventions are process-wide and set once in `AddMongoDb`. Do not register conflicting conventions elsewhere, and do not change them on existing data without reading ADR 0003.
- Enable bulk or projection repositories per binding (`WithBulkRepository`, `WithProjectionRepository`) before injecting them.

**Results and errors**
- Repository methods return `FluentResults` `Result<T>`. Check `IsFailed` or `HasError<TError>()`. Do not call `.Value` on a result you have not checked.
- Map expected failures by type: `DocumentNotFoundError`, `ConcurrencyConflictError`, `DuplicateKeyError`, `TransientWriteError`, `TransactionBudgetExceededError` and the rest (mapping in the [typed errors guide](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/handle-errors.md), cause and action per error in the [error reference](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/error-reference.md)). Do not catch `MongoException` for these cases.
- Always pass the `CancellationToken` through to every repository and runner call.

**Documents and policies**
- A document implements `IDocumentEntity<TId>`. Add `IHasConcurrencyToken`, `ISoftDeletable` and `IAuditableDocument` only when needed, and enable soft-delete **filters** with `WithSoftDelete()` on the binding.
- `WithGuidIdGeneration` is only for `Guid` ids. Do not use it with `ObjectId`.
- Choose the update method on purpose: `ReplaceAsync` replaces the document (ETag guarded), `UpdateSnapshotAsync` sets the mutable snapshot and keeps unknown stored fields, `PatchAsync` applies only the `UpdateDefinition` you give it.
- `PurgeAsync` permanently deletes every match, including documents that are not soft-deleted. Use it only when permanent deletion is the intent.
- Keyset paging cursors are bound to the sort that produced them. Do not reuse a cursor with a different sort.

**Tenancy (fail closed)**
- The library has no tenant type. The application owns the prefix resolver. If the tenant is unknown, return `Result.Fail` from `ResolveAsync`. Never return an empty or default prefix, because that would send the request to a shared database.
- Prefix resolvers are scoped. Do not cache the resolved prefix in a singleton.

**Provisioning**
- Create indexes and collections with `IMongoDbProvisioner` at startup or in a deploy job, never in a request. Resolving a collection does not create indexes.
- Set `CreateIndexOptions.Name` on every index. Apply removes server indexes that are not declared on the binding, so declare every index you want to keep. Run `DryRunAsync` first when changing indexes on existing data.
- A TTL field must be stored as a BSON `Date`. Use `DateTime` or `DateTime?` in UTC. `DateTimeOffset` is a subdocument by default, so the index is created and nothing expires. `null` or a missing field does not expire, which is how you keep a document.
- The server deletes eligible documents on a background pass (about once a minute). Filter reads on the deadline when a document must disappear at that instant. Nothing expires until `ApplyAsync` has created the index.

**Transactions**
- They need a replica set. Standalone `mongod` returns a failed `Result`.
- Resolve repositories from `IMongoDbTransactionContext.Repositories`, not from the request scope, or the writes will not join the session and survive an abort.
- Do not open a second runner or session inside the callback. Keep operations sequential. The callback can run more than once on a transient error, so it must be safe to repeat.
- Return a failed `Result` to abort. Budgets (`MaxOperations`, `MaxEstimatedBytes`, `TimeLimit`) are client-side estimates, not MongoDB server limits.

**JSON**
- Use Canonical Extended JSON when BSON type fidelity matters (`ObjectId`, `long`, `decimal`, dates). Relaxed output may widen numbers.
- Never enable Newtonsoft `TypeNameHandling` for untrusted input. The adapter rejects it only when those settings are supplied to token conversion. Use `TypeNameHandling.None` for your own deserialization.
- `AddSystemTextJsonDocuments()` and `AddNewtonsoftJsonDocuments()` must come **after** `AddMongoDb`.

**Escape hatches**
- Direct access through the keyed `IMongoClient` does not apply tenant resolution: `GetDatabase` bypasses the namespace pipeline. Use `IMongoDbCollectionFactory` for tenant-aware collection access. Dispose cursors you open. Do not wrap driver features in your own provider-neutral abstraction.

**Logging and secrets**
- Do not log connection strings, credentials or full documents that may hold personal data.

## Testing

- Test against a real MongoDB (for example Testcontainers) instead of mocking `IMongoCollection<T>`. Use a single-node **replica set** when the code under test uses transactions.
- Assert on result types (`result.HasError<DocumentNotFoundError>()`), not on exception messages.
- If the project already uses NUnit and Shouldly (or xUnit, or another stack), follow the project's existing style and test layout.

## Troubleshooting

| Symptom | Likely cause |
|---------|--------------|
| `InvalidOperationException` when registering a JSON adapter | `AddMongoDb` was not called first |
| Failed `Result` from a repository call with a namespace message | Your prefix resolver failed closed (no tenant), or the database key is unknown |
| `ConcurrencyConflictError` on every update | The caller sends a stale or zero `ETag`; re-read the document, then retry once |
| Transaction returns a failed result immediately | Standalone server (no replica set) |
| Writes in a transaction survive an abort | Repository was taken from the request scope, not from the transaction context |
| Indexes disappear after deploy | They were not declared on the binding and `ApplyAsync` removed them |
| TTL index exists but documents never expire | The field is not a BSON `Date` (`DateTimeOffset`, a string, or a number), or `ApplyAsync` never created the index |
| `CollectionResolutionException` from `GetAsyncEnumerable` | The streaming method throws on resolution failure instead of returning a `Result` |
