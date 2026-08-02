# v1 Defect and Positioning Inventory

Recorded for [#3](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/3).  
Feeds naming ADR ([#4](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/4)), dead-API removal ([#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13)), correctness fixes ([#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18)), quality gates ([#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28)), and README rewrite ([#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39)).

---

## Packaging and namespaces

| ID | Defect | Evidence | Owner |
|----|--------|----------|-------|
| D1 | Package/namespace collision | `Dilcore.DocumentDb.MongoDb.Abstractions.csproj` sets `RootNamespace` to `Dilcore.DocumentDb.Abstractions`, so Mongo types ship under the core abstractions namespace from a separate package | [#4](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/4), [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12), [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D2 | Duplicate `DocumentEntityExtensions` type name | Present in both Abstractions packages under `Dilcore.DocumentDb.Abstractions.Extensions` | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D3 | File/type naming drift | `IDocument*PrefixResolver.cs` files define `*PrefixProvider` types; `Default*PrefixResolver.cs` define `Default*PrefixProvider` | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D4 | Stale package metadata URLs | `src/Directory.Build.props` still points at `aytymchuk/Dilcore-Library-DocumentDb` while `origin` is `Dilcore-Official/Dilcore-MongoDb` | [#30](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/30) (packaging), interim note in [#5](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/5) |
| D5 | Publish feed / version chaos | Publish workflow targets `nuget.pkg.github.com/aytymchuk`; props say `1.0.0`; tags are `v0.0.x`; auto patch bump on every `src/**` push | [#5](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/5), [#30](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/30) |

---

## Dead / redundant APIs (feed M2 removal)

| ID | Defect | Evidence | Owner |
|----|--------|----------|-------|
| D6 | Empty `IBsonDocumentRepository` marker | Interface has no members | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D7 | Pass-through `BsonDocumentCollectionFactory` | Thin wrapper over `IMongoDbCollectionFactory` | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D8 | Unused `MongoDbIndexFactory` | No in-repo references (Serena reference search empty) | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D9 | Unused `WithEmptyCollection` | No callers outside declaration | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D10 | Public service-locator field | `MongoDatabaseContainer.Services` is a public mutable `IServiceCollection` field | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13), [#14](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/14) |
| D11 | Inconsistent concrete repository visibility | `GenericMongoDbRepository` is internal; bulk/projection concretes are public | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D12 | FluentValidation for a single guard | Used only in `MongoDbConfigBuilder` validation | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D13 | Empty `Configuration/Database/` folder | Declared in MongoDb `.csproj` with no content | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |

---

## Correctness defects

| ID | Defect | Evidence | Owner |
|----|--------|----------|-------|
| D14 | Soft-delete filter inconsistency | `GetAsync` / `GetListAsync` apply `ApplyNotDeleteFilter`; `HasAnyAsync` and `CountAsync` do not | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18) |
| D15 | Missing-document returns success with null | `GetAsync` returns `Result.Ok(entity)` after `FirstOrDefaultAsync` without null check | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18) |
| D16 | Streaming error model break | `GetAsyncEnumerable` throws `InvalidOperationException` on collection failure instead of `Result` | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18), [#25](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/25) |
| D17 | Primary-constructor capture warning | CS9107 in `GenericMongoDbRepository.cs` | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18) / [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) |

---

## Solution / CI / tooling

| ID | Defect | Evidence | Owner |
|----|--------|----------|-------|
| D18 | Missing `ado/` solution items | `Dilcore.DocumentDb.sln` references `ado/azure-pipelines.*` and `ado/variables/*`; directory absent | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) (remove refs) or restore if ADO retained |
| D19 | CI TFM drift | Workflows install SDK `9.0.x` while `Directory.Build.props` targets `net10.0` | **M0 fix in [#5](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/5)**; remaining gates in [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) |
| D20 | CI lacks Docker for Testcontainers | Integration tests fail without Docker; current CI does not provision it | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28), [#23](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/23) |
| D21 | Transitive vulnerability advisories | NU1902/NU1903 on `SharpCompress`, `Snappier` (driver/Testcontainers chain); `Microsoft.OpenApi` in sample | [#10](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/10), [#11](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/11), [#32](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/32) |
| D22 | Sample starts Testcontainers in host | `samples/MongoDb.WebApi.Sample/Program.cs` embeds container lifecycle in the web app | [#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39) |

---

## Positioning claims to remove or rewrite (M8)

Source: root [`README.md`](../../README.md). Full rewrite owned by [#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39).

| Claim / content | Problem | Action |
|-----------------|---------|--------|
| Title / product name “DocumentDB Library” | Confuses with Amazon DocumentDB; implies provider neutrality | Rename messaging to MongoDB toolkit per [#4](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/4) |
| “Clean Architecture principles” | Overstated layering for a MongoDB-specific toolkit | Remove / replace with honest architecture section |
| “abstracted interface for working with MongoDB” implying DB-agnostic repos | Contradicts Mongo-only v2 product definition | Rewrite as opinionated MongoDB application toolkit |
| “Thread Safety: Thread-safe operations…” | Unverified product claim | Remove or qualify after review |
| README Getting Started package versions | Documents MongoDB.Driver 3.5.0 and DI 9.0.9; actual are 3.5.2 and 10.0.1 | Update in M8; interim truth in support policy |
| Documents `IDocumentDatabasePrefixResolver` | Type is `IDocumentDatabasePrefixProvider` | Fix in M8 |
| Incomplete repository surface in docs | Omits `GetAsyncEnumerable`, derived overloads, `BulkStoreRangeAsync` | Align with API inventory |
| README ships as package readme | Stale docs ship inside every `.nupkg` | Keep until M8; then replace with concise package readme |

---

## CI TFM drift ownership

| Item | Owner | Status |
|------|-------|--------|
| Align GitHub Actions SDK to `10.0.x` | [#5](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/5) (M0) | Fixed in this milestone |
| `global.json`, analyzers, warnings-as-errors, format gates | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) (M5) | Deferred |
| Integration test Docker service in CI | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) / [#23](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/23) | Deferred |

---

## Dead API list for M2 (#13)

Explicit removal candidates:

1. Empty `IBsonDocumentRepository` (or promote to a real contract).
2. Pass-through `BsonDocumentCollectionFactory` / redundant factory boundary.
3. Unused `MongoDbIndexFactory`.
4. Unused `GetCollectionOptions.WithEmptyCollection`.
5. Duplicate `DocumentEntityExtensions` merge/rename.
6. Public `MongoDatabaseContainer.Services` field.
7. Public bulk/projection concrete repository types (make internal like generic).
8. FluentValidation dependency if only used for one config guard.
9. Empty `Configuration/Database/` project folder.
10. Redundant `Dilcore.DocumentDb.MongoDb.Abstractions` package boundary after topology redesign ([#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12)).

See also the [v1 public API inventory](../api/v1-public-api.md).
