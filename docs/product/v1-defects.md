# v1 Defect and Positioning Inventory

Recorded for [#3](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/3).  
Feeds naming ADR ([#4](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/4)), dead-API removal ([#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13)), correctness fixes ([#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18)), quality gates ([#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28)), and README rewrite ([#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39)).

**How to read this file:** v1 evidence is preserved. **Status** is against current `src/` (two-package `Dilcore.MongoDB*`). Historical inventory: [v1-public-api.md](../api/v1-public-api.md). Entity model: [ADR 0002](../adr/0002-generic-document-identifier.md). Conventions: [ADR 0003](../adr/0003-serialization-conventions.md).

---

## Packaging and namespaces

| ID | Status | Defect | Evidence | Owner |
|----|--------|--------|----------|-------|
| D1 | **Resolved (M2)** | Package/namespace collision | v1 `Dilcore.DocumentDb.MongoDb.Abstractions` set `RootNamespace` to `Dilcore.DocumentDb.Abstractions`. Current: single `Dilcore.MongoDB.Abstractions` namespace. | [#4](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/4), [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12), [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D2 | **Resolved (M2)** | Duplicate `DocumentEntityExtensions` | v1: two assemblies, one namespace. Current: one type in Abstractions. | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D3 | **Resolved (M2)** | File/type naming drift (`*PrefixProvider` vs `*PrefixResolver`) | Current: `INamespacePrefixResolver` / `INamespaceSegmentContributor`. | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D4 | **Resolved (M2)** | Stale package metadata URLs | `src/Directory.Build.props` points at `Dilcore-Official/Dilcore-MongoDb`. | [#30](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/30), [#5](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/5) |
| D5 | **Partial** | Publish feed / version chaos | Current publish/version source of truth: [`.github/workflows/nuget-publish.yml`](../../.github/workflows/nuget-publish.yml). Remaining: auto-patch on `src/**` push, placeholder `Version` in `src/Directory.Build.props`, possible legacy feed/package IDs in `nuget.config`. | [#5](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/5), [#30](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/30) |

---

## Dead / redundant APIs (M2 #13)

| ID | Status | Defect | Evidence | Owner |
|----|--------|--------|----------|-------|
| D6 | **Resolved (M2)** | Empty `IBsonDocumentRepository` marker | Type absent from current public API. | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D7 | **Resolved (M2)** | Pass-through `BsonDocumentCollectionFactory` | Type absent. | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D8 | **Resolved (M2)** | Unused `MongoDbIndexFactory` | Type absent. | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D9 | **Resolved (M2)** | Unused `WithEmptyCollection` | Not on `GetCollectionOptions`. | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D10 | **Resolved (M2)** | Public service-locator field `MongoDatabaseContainer.Services` | Container API removed. | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13), [#14](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/14) |
| D11 | **Resolved (M2)** | Inconsistent concrete repository visibility | Bulk/projection concretes are internal (`PublicApiBoundaryTests`). | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D12 | **Resolved (M2)** | FluentValidation for a single guard | Not a primary-package dependency. | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| D13 | **Resolved (M2)** | Empty `Configuration/Database/` folder | Folder gone with v1 MongoDb project. | [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |

---

## Correctness defects

| ID | Status | Defect | Evidence | Owner |
|----|--------|--------|----------|-------|
| D14 | **Resolved (M3)** | Soft-delete filter inconsistency | `HasAnyAsync` and `CountAsync` apply the same not-deleted filter as reads. | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18) |
| D15 | **Resolved (M3)** | Missing-document returns success with null | `GetAsync` returns `DocumentNotFoundError`. | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18) |
| D16 | **Resolved (M3, streaming exception)** | Streaming error model break | `GetAsyncEnumerable` throws `CollectionResolutionException` on collection failure. M4 may still redesign streaming to Result. | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18), [#25](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/25) |
| D17 | **Resolved** | Primary-constructor capture warning CS9107 | Addressed in later hardening; do not reintroduce unused captured primary-constructor parameters. | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18) / [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) |
| D23 | **Resolved (M3)** | Timestamp ETag is not collision-safe | `MongoDbHelper.GenerateEtag()` uses a non-zero 64-bit random token (`long` type unchanged). | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18) |
| D24 | **Resolved (M3)** | Entities mutated before write success | Policy fields are staged and applied only after acknowledged writes. | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18) |
| D25 | **Resolved (M3)** | Full-document `$set` replace/patch risk | `ReplaceAsync` replaces stored state; `UpdateSnapshotAsync` `$set`s the mutable snapshot excluding `_id`; `PatchAsync` is caller-supplied. | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18) |
| D26 | **Resolved (M3)** | Bulk write edge cases incomplete | Empty/no-op, chunking, unordered partial failure, and per-item outcomes are covered. | [#18](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/18) |

---

## Solution / CI / tooling

| ID | Status | Defect | Evidence | Owner |
|----|--------|--------|----------|-------|
| D18 | **Resolved** | Missing `ado/` solution items | Current solution is `Dilcore.MongoDB.sln` without ADO refs. | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) |
| D19 | **Resolved (M0)** | CI TFM drift (historical: SDK `9.0.x` vs library TFM) | Current SDK pin lives in [`ci.yml`](../../.github/workflows/ci.yml), [`benchmarks.yml`](../../.github/workflows/benchmarks.yml), [`codeql.yml`](../../.github/workflows/codeql.yml), and [`nuget-publish.yml`](../../.github/workflows/nuget-publish.yml) (`actions/setup-dotnet`). Remaining analyzer/format gates: #28. | [#5](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/5), [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) |
| D20 | **Resolved** | CI lacked Docker for Testcontainers | CI runs Docker preflight + integration/DI jobs. Coverage **gates** still M5 (#28). | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28), [#23](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/23) |
| D21 | **Open / ongoing** | Transitive vulnerability advisories | Track via Dependabot and security workflows; driver bumps in #32. | [#10](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/10), [#11](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/11), [#32](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/32) |
| D22 | **Open** | Sample starts Testcontainers in host | `samples/MongoDb.WebApi.Sample/Program.cs` embeds container lifecycle. | [#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39) |

---

## Positioning claims (README / M8 #39)

Source: root [`README.md`](../../README.md). Full rewrite owned by [#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39).

| Claim / content | Status | Problem | Action |
|-----------------|--------|---------|--------|
| Title / product name “DocumentDB Library” | **Partial** | H1 is “Dilcore MongoDB”; closing copy and some phrasing may still say DocumentDB | Finish MongoDB-only messaging in #39 |
| “Clean Architecture principles” | **Resolved** | README no longer uses this phrasing | None |
| DB-agnostic repository implication | **Partial** | Product definition is Mongo-only; leftover diagram labels may still overstate | Rewrite as opinionated MongoDB application toolkit |
| “Thread Safety: Thread-safe operations…” | **Resolved** | Claim removed from README | None |
| Getting Started package versions | **Resolved** | README now says “Pin versions in `Directory.Packages.props`… do not copy numbers from this README” | None |
| `IDocumentDatabasePrefixResolver` | **Resolved in README API samples** | Current docs use `INamespacePrefixResolver` | Keep using namespace types |
| Incomplete repository surface | **Open** | README may omit `GetAsyncEnumerable`, derived overloads, `BulkStoreRangeAsync` | Align with PublicAPI baselines |
| `WithDatabaseName` in configuration samples | **Resolved** | README samples now use `AddDatabase` + namespace pipeline throughout | None |
| README ships as package readme | **Open** | Stale docs ship inside every `.nupkg` until dedicated package readme | Keep until M8; then replace |

---

## CI / quality gate ownership

| Item | Owner | Status |
|------|-------|--------|
| Align GitHub Actions SDK with library TFM | [#5](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/5) (M0) | **Done** (pin via `actions/setup-dotnet` in [`ci.yml`](../../.github/workflows/ci.yml), [`benchmarks.yml`](../../.github/workflows/benchmarks.yml), [`codeql.yml`](../../.github/workflows/codeql.yml), [`nuget-publish.yml`](../../.github/workflows/nuget-publish.yml)) |
| Integration test Docker in CI | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) / [#23](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/23) | **Done** (jobs exist) |
| `global.json`, analyzers, warnings-as-errors, format gates | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) (M5) | Deferred |

---

## Dead API list for M2 (#13) — completed

These were v1 removal candidates; they are **gone** from current `src/` (or folded/internal as designed):

1. Empty `IBsonDocumentRepository`
2. Pass-through `BsonDocumentCollectionFactory`
3. Unused `MongoDbIndexFactory`
4. Unused `GetCollectionOptions.WithEmptyCollection`
5. Duplicate `DocumentEntityExtensions`
6. Public `MongoDatabaseContainer.Services`
7. Public bulk/projection concrete repository types (now internal)
8. FluentValidation dependency
9. Empty `Configuration/Database/` folder
10. Separate `Dilcore.DocumentDb.MongoDb.Abstractions` package ([#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12))

Keep this list as the historical M2 checklist. See [v1 public API inventory](../api/v1-public-api.md).
