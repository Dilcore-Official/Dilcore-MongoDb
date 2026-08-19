# Public API inventory

Tracked by [#2](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/2).  
Machine-readable **current** baselines: [`src/Dilcore.MongoDB.Abstractions/PublicAPI.*.txt`](../../src/Dilcore.MongoDB.Abstractions/PublicAPI.Shipped.txt) and [`src/Dilcore.MongoDB/PublicAPI.*.txt`](../../src/Dilcore.MongoDB/PublicAPI.Shipped.txt). Analyzer enforcement remains [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28).

This file keeps the **historical v1** four-package snapshot (2026-08-02) for migration context. It is **not** the live public API. Inspect `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` and architecture tests for current truth.

**v1 snapshot date:** 2026-08-02  
**TFM (then and now):** `net10.0`  
**Driver at v1 snapshot:** `MongoDB.Driver` 3.5.2 (current pin: [`Directory.Packages.props`](../../Directory.Packages.props))

---

## Current v2 status (two-package)

Implemented topology ([ADR 0001](../adr/0001-package-naming.md), [package selection](../product/package-selection.md)):

| Package | Role |
|---------|------|
| `Dilcore.MongoDB.Abstractions` | Contracts, keys, namespace, policies, repository interfaces |
| `Dilcore.MongoDB` | DI (`AddMongoDb`), builders, repositories, conventions |

Do not enumerate members here. Shipped types are listed in the two `PublicAPI.Shipped.txt` files. `ConfigureConventions` / `IConventionsBuilder` ([ADR 0003](../adr/0003-serialization-conventions.md)) are implemented and currently recorded in `Dilcore.MongoDB/PublicAPI.Unshipped.txt`.

M2.5 entity model ([ADR 0002](../adr/0002-generic-document-identifier.md)): marker `IDocumentEntity`, `IDocumentEntity<TId>`, opt-in `IHasConcurrencyToken` / `ISoftDeletable` / `IAuditableDocument`, `GuidIdGenerationStrategy`.

v1 risks below (namespace collision, duplicate extensions, public bulk/projection concretes, `MongoDatabaseContainer.Services`) are **resolved in M2**. Remaining correctness defects are tracked in [v1-defects.md](../product/v1-defects.md) (#18).

---

## Historical v1 package graph (superseded)

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

| Package | Project (v1) | Direct NuGet deps | Project refs |
|---------|--------------|-------------------|--------------|
| `Dilcore.DocumentDb.Abstractions` | `src/Dilcore.DocumentDb.Abstractions/` | FluentResults | — |
| `Dilcore.DocumentDb.MongoDb.Abstractions` | `src/Dilcore.DocumentDb.MongoDb.Abstractions/` | FluentResults, MongoDB.Driver | Abstractions |
| `Dilcore.DocumentDb.MongoDb` | `src/Dilcore.DocumentDb.MongoDb/` | FluentValidation, Microsoft.Extensions.DependencyInjection, MongoDB.Driver | Abstractions, MongoDb.Abstractions |
| `Dilcore.DocumentDb.MongoDb.Repositories` | `src/Dilcore.DocumentDb.MongoDb.Repositories/` | *(transitive)* | MongoDb.Abstractions, MongoDb |

**v1 primary entry point:** `ServiceCollectionExtensions.AddMongoDb(...)` in `Dilcore.DocumentDb.MongoDb`.

v1 baseline paths under `src/Dilcore.DocumentDb.*` no longer exist; they were captured for #2 and then replaced by the two-package files linked above.

---

## 1. Dilcore.DocumentDb.Abstractions (v1)

**Namespace:** `Dilcore.DocumentDb.Abstractions` (+ `.Extensions`, `.Helpers`, `.Exceptions`)

| Type | Kind | Members / notes |
|------|------|-----------------|
| `IDocumentEntity` | interface | `Id`, `ETag`, `IsDeleted`, `CreatedAt`, `UpdatedAt` |
| `IDocumentPrefixProvider` | interface | `ResolveAsync(CancellationToken)` |
| `IDocumentDatabasePrefixProvider` | interface | extends prefix provider (file name said *Resolver*) |
| `IDocumentCollectionPrefixProvider` | interface | extends prefix provider (file name said *Resolver*) |
| `Constants` | static class | `EmptyETag` |
| `DocumentEntityExtensions` | static class | `GenerateETag`, `CreatedNow`, `UpdatedNow`, `NewId`, `IsIdEmpty`, `CheckId`, `IsNew` |
| `DocumentDbHelper` | class | public `GenerateEtag()` |
| `DocumentIdentifierIsEmptyException` | class | thrown by `CheckId` |

---

## 2. Dilcore.DocumentDb.MongoDb.Abstractions (v1)

**Critical (v1):** `RootNamespace` was `Dilcore.DocumentDb.Abstractions`, so Mongo-specific types shipped under the **same namespace** as the core abstractions package. See [v1 defects](../product/v1-defects.md) D1 (resolved in M2).

| Type | Kind | Namespace shipped | Notes |
|------|------|-------------------|-------|
| `GetCollectionOptions<TDocument>` | class | `Dilcore.DocumentDb.Abstractions` | Fluent options; included unused `WithEmptyCollection` |
| `IMongoDbCollectionFactory` | interface | `Dilcore.DocumentDb.Abstractions` | |
| `IMongoDbCollectionProvider` | interface | `Dilcore.DocumentDb.Abstractions` | |
| `IMongoDatabaseProvider` | interface | `Dilcore.DocumentDb.Abstractions` | |
| `IBsonDocumentCollectionFactory` | interface | `Dilcore.DocumentDb.Abstractions` | |
| `IBsonDocumentRepository` | interface | `Dilcore.DocumentDb.Abstractions` | **Empty marker** |
| `BsonDocumentRepository` | abstract class | `Dilcore.DocumentDb.Abstractions` | |
| `BaseMongoDbRepository<TDocument>` | abstract class | `Dilcore.DocumentDb.Abstractions.Repositories` | |
| `DocumentEntityExtensions` | static class | `Dilcore.DocumentDb.Abstractions.Extensions` | **Duplicate type name**; added `ToBsonUpdateDocument<T>` |

---

## 3. Dilcore.DocumentDb.MongoDb (v1)

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

## 4. Dilcore.DocumentDb.MongoDb.Repositories (v1)

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
| `GenericMongoDbBulkRepository<T>` | class | **public** (leaked concrete type) |
| `GenericMongoDbProjectionRepository<T>` | class | **public** (leaked concrete type) |

---

## Notable v1 baseline risks (resolved in M2 unless noted)

1. Namespace collision across two packages under `Dilcore.DocumentDb.Abstractions` — **resolved**.
2. Duplicate `DocumentEntityExtensions` type name — **resolved**.
3. Empty `IBsonDocumentRepository` marker and unused `WithEmptyCollection` / `MongoDbIndexFactory` — **removed**.
4. Inconsistent concrete repository visibility — **resolved** (concretes internal).
5. Public mutable `MongoDatabaseContainer.Services` — **removed** with the container API.

These fed the [defect inventory](../product/v1-defects.md) and [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13).
