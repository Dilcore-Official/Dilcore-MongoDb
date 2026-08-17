# v1 Public API Inventory

Canonical inventory of the current (v1) public API surface for Dilcore DocumentDB packages.  
Tracked by [#2](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/2).  
Machine-readable baselines: `src/*/PublicAPI.Shipped.txt` (ready for `Microsoft.CodeAnalysis.PublicApiAnalyzers` in [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28)).

**Snapshot date:** 2026-08-02  
**TFM:** `net10.0`  
**Driver:** `MongoDB.Driver` 3.5.2

---

## Package reference graph

```text
Dilcore.DocumentDb.Abstractions
        ^
        |
Dilcore.DocumentDb.MongoDb.Abstractions
        ^                 ^
        |                 |
        +---- Dilcore.DocumentDb.MongoDb
                          ^
                          |
        Dilcore.DocumentDb.MongoDb.Repositories
```

| Package | Project | Direct NuGet deps | Project refs |
|---------|---------|-------------------|--------------|
| `Dilcore.DocumentDb.Abstractions` | `src/Dilcore.DocumentDb.Abstractions/` | FluentResults | — |
| `Dilcore.DocumentDb.MongoDb.Abstractions` | `src/Dilcore.DocumentDb.MongoDb.Abstractions/` | FluentResults, MongoDB.Driver | Abstractions |
| `Dilcore.DocumentDb.MongoDb` | `src/Dilcore.DocumentDb.MongoDb/` | FluentValidation, Microsoft.Extensions.DependencyInjection, MongoDB.Driver | Abstractions, MongoDb.Abstractions |
| `Dilcore.DocumentDb.MongoDb.Repositories` | `src/Dilcore.DocumentDb.MongoDb.Repositories/` | *(transitive)* | MongoDb.Abstractions, MongoDb |

**Primary consumer entry point:** `ServiceCollectionExtensions.AddMongoDb(...)` in `Dilcore.DocumentDb.MongoDb`.

---

## Baseline artifacts

| Package | Shipped baseline | Unshipped |
|---------|------------------|-----------|
| Abstractions | [`PublicAPI.Shipped.txt`](../../src/Dilcore.DocumentDb.Abstractions/PublicAPI.Shipped.txt) | [`PublicAPI.Unshipped.txt`](../../src/Dilcore.DocumentDb.Abstractions/PublicAPI.Unshipped.txt) |
| MongoDb.Abstractions | [`PublicAPI.Shipped.txt`](../../src/Dilcore.DocumentDb.MongoDb.Abstractions/PublicAPI.Shipped.txt) | [`PublicAPI.Unshipped.txt`](../../src/Dilcore.DocumentDb.MongoDb.Abstractions/PublicAPI.Unshipped.txt) |
| MongoDb | [`PublicAPI.Shipped.txt`](../../src/Dilcore.DocumentDb.MongoDb/PublicAPI.Shipped.txt) | [`PublicAPI.Unshipped.txt`](../../src/Dilcore.DocumentDb.MongoDb/PublicAPI.Unshipped.txt) |
| Repositories | [`PublicAPI.Shipped.txt`](../../src/Dilcore.DocumentDb.MongoDb.Repositories/PublicAPI.Shipped.txt) | [`PublicAPI.Unshipped.txt`](../../src/Dilcore.DocumentDb.MongoDb.Repositories/PublicAPI.Unshipped.txt) |

Analyzer activation and CI enforcement are deferred to [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28).

---

## 1. Dilcore.DocumentDb.Abstractions

**Namespace:** `Dilcore.DocumentDb.Abstractions` (+ `.Extensions`, `.Helpers`, `.Exceptions`)

| Type | Kind | Members / notes |
|------|------|-----------------|
| `IDocumentEntity` | interface | `Id`, `ETag`, `IsDeleted`, `CreatedAt`, `UpdatedAt` |
| `IDocumentPrefixProvider` | interface | `ResolveAsync(CancellationToken)` |
| `IDocumentDatabasePrefixProvider` | interface | extends prefix provider (file name says *Resolver*) |
| `IDocumentCollectionPrefixProvider` | interface | extends prefix provider (file name says *Resolver*) |
| `Constants` | static class | `EmptyETag` |
| `DocumentEntityExtensions` | static class | `GenerateETag`, `CreatedNow`, `UpdatedNow`, `NewId`, `IsIdEmpty`, `CheckId`, `IsNew` |
| `DocumentDbHelper` | class | public `GenerateEtag()` |
| `DocumentIdentifierIsEmptyException` | class | thrown by `CheckId` |

---

## 2. Dilcore.DocumentDb.MongoDb.Abstractions

**Critical:** `RootNamespace` is `Dilcore.DocumentDb.Abstractions`, so Mongo-specific types ship under the **same namespace** as the core abstractions package despite being a separate NuGet package. See [v1 defects](../product/v1-defects.md).

| Type | Kind | Namespace shipped | Notes |
|------|------|-------------------|-------|
| `GetCollectionOptions<TDocument>` | class | `Dilcore.DocumentDb.Abstractions` | Fluent options; includes unused `WithEmptyCollection` |
| `IMongoDbCollectionFactory` | interface | `Dilcore.DocumentDb.Abstractions` | |
| `IMongoDbCollectionProvider` | interface | `Dilcore.DocumentDb.Abstractions` | |
| `IMongoDatabaseProvider` | interface | `Dilcore.DocumentDb.Abstractions` | |
| `IBsonDocumentCollectionFactory` | interface | `Dilcore.DocumentDb.Abstractions` | |
| `IBsonDocumentRepository` | interface | `Dilcore.DocumentDb.Abstractions` | **Empty marker** |
| `BsonDocumentRepository` | abstract class | `Dilcore.DocumentDb.Abstractions` | |
| `BaseMongoDbRepository<TDocument>` | abstract class | `Dilcore.DocumentDb.Abstractions.Repositories` | |
| `DocumentEntityExtensions` | static class | `Dilcore.DocumentDb.Abstractions.Extensions` | **Duplicate type name**; adds `ToBsonUpdateDocument<T>` |

---

## 3. Dilcore.DocumentDb.MongoDb

**Namespace:** `Dilcore.DocumentDb.MongoDb` (+ `.Extensions`, `.Configuration.Client`, `.Helpers`)

| Type | Kind | Notes |
|------|------|-------|
| `ServiceCollectionExtensions` | static class | `AddMongoDb` entry point |
| `MongoDbContainer` | class | Multi-database registration builder |
| `MongoDatabaseContainer` | class | Per-database builder; **public fields** `Services`, `DbName` |
| `MongoDbConfigBuilder` | class | `UseConnectionString`, `UseMaxConnectionPoolSize` |
| `BsonDocumentCollectionFactory` | class | Pass-through over `IMongoDbCollectionFactory` |
| `MongoDbIndexFactory` | static class | Public helpers; **no in-repo references** |

Internal (not in baseline as public API): `MongoClientProvider`, `MongoCollectionProvider`, `MongoDatabaseProvider`, `MongoDbCollectionFactory`.

---

## 4. Dilcore.DocumentDb.MongoDb.Repositories

**Namespace:** `Dilcore.DocumentDb.MongoDb.Repositories` (+ `.Abstractions`)

| Type | Kind | Visibility notes |
|------|------|------------------|
| `IGenericRepository<TDocument>` | interface | CRUD + `IAsyncEnumerable` |
| `IGenericBulkRepository<TDocument>` | interface | `BulkStoreAsync`, `BulkStoreRangeAsync`, `BulkDeleteAsync` |
| `IGenericProjectionRepository<TDocument>` | interface | Projection get/list |
| `GenericRepositoryExtensions` | static class | Guid / expression convenience overloads |
| `MongoDatabaseContainerExtensions` | static class | `AddGenericRepository` |
| `RegisterRepositoryOptions` | class | `WithBulkRepository`, `WithProjectionRepository` |
| `GenericMongoDbRepository<T>` | class | **internal** (correct) |
| `GenericMongoDbBulkRepository<T>` | class | **public** (leaks concrete type) |
| `GenericMongoDbProjectionRepository<T>` | class | **public** (leaks concrete type) |

---

## Notable baseline risks for v2 diffs

1. Namespace collision across two packages under `Dilcore.DocumentDb.Abstractions`.
2. Duplicate `DocumentEntityExtensions` type name in the same namespace from two assemblies.
3. Empty `IBsonDocumentRepository` marker and unused `WithEmptyCollection` / `MongoDbIndexFactory`.
4. Inconsistent concrete repository visibility (generic internal vs bulk/projection public).
5. Public mutable `MongoDatabaseContainer.Services` service-locator field.

These feed the [defect inventory](../product/v1-defects.md) and [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13).

---

## M2.5 update (v2 entity model)

As of [ADR 0002](../adr/0002-generic-document-identifier.md) / milestone M2.5:

| Type | Change |
|------|--------|
| `IDocumentEntity` | Empty marker (no longer carries `Guid Id` / ETag / soft-delete / audit members) |
| `IDocumentEntity<TId>` | Typed identifier contract |
| `IHasConcurrencyToken` | Opt-in `ETag` |
| `ISoftDeletable` | Opt-in `IsDeleted` |
| `IAuditableDocument` | Opt-in `CreatedAt` / `UpdatedAt` |
| `GuidIdGenerationStrategy` | `Random` (default) or `SequentialVersion7` |
| `UnsupportedIdentifierTypeException` | Thrown when auto-generating unsupported `TId` |
| `GetCollectionOptions<TDocument>.WithGuidIdGeneration` | Per-collection Guid generation |
| `IMongoDocumentBindingBuilder<TDocument>.WithGuidIdGeneration` | Per-binding Guid generation |
| `DocumentEntityExtensions` | Policy-aware no-ops; `NewId` accepts optional Guid strategy |

Machine-readable baselines live under `src/Dilcore.MongoDB.Abstractions/PublicAPI.*.txt` and `src/Dilcore.MongoDB/PublicAPI.*.txt`.
