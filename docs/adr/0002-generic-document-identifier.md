# ADR 0002: Generic document identifier and composable entity policies

- **Status:** Accepted
- **Date:** 2026-08-18
- **Issue:** [#60](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/60)
- **Blocks:** [#61](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/61), [#62](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/62)

## Context

`IDocumentEntity` currently hardcodes `Guid Id` and forces every document to carry `ETag`, `IsDeleted`, `CreatedAt`, and `UpdatedAt`. Consumers need `ObjectId`, `string`, `long`, or custom value-type identifiers, and many documents need only a subset of the policy properties. The design principle already states that optional policies should be composable interfaces rather than one mandatory entity shape. Repositories (`IGenericRepository<TDocument>` and friends), DI bindings, and the collection factory must keep working through a single generic-repository pattern without requiring a second type argument at every call site.

## Decision drivers

- Support any identifier type while preserving `IGenericRepository<TDocument>` (one type argument).
- Keep repositories, serialization, and BSON `_id` conventions strongly typed where possible.
- Prefer a clean break: no v1 shims (see [versioning policy](../policies/versioning-and-support.md) and ROADMAP non-goals).
- Stay compatible with the M2 named-binding / DI model.
- Allow Guid consumers to opt into RFC 9562 UUID version 7 generation without changing BSON representation.

## Options considered

| Option | Summary | Rejected because |
|--------|---------|------------------|
| A. `IDocumentEntity<TId>` only; repositories become `IGenericRepository<TDocument, TId>` | Fully static typing everywhere | Extra `TId` at every binding/repository call site; larger public API break for little consumer benefit |
| B. Non-generic `object Id` / `BsonValue Id` | Single type argument preserved | Loses type safety; poor ergonomics for filters and helpers |
| **C. Marker `IDocumentEntity` + `IDocumentEntity<TId>`; repositories stay single-generic** | Internal id-accessor cache resolves `TId` once per document type | **Accepted** |

## Decision

### Identifier shape

- `IDocumentEntity` becomes an empty marker interface.
- `IDocumentEntity<TId>` extends the marker and exposes `TId Id { get; set; }`.
- Generic repositories, collection factory, and DI builders remain constrained to `where TDocument : class, IDocumentEntity` (marker only).
- An internal per-`TDocument` id accessor cache resolves the closed `IDocumentEntity<TId>`, provides `IsEmpty` / `EnsureNewId` / `BuildIdFilter`, and is cached in a `ConcurrentDictionary`. Built-in generators cover `Guid` and `ObjectId`; unsupported `TId` types throw a clear exception instructing callers to assign `Id` before `StoreAsync`.

### Guid generation

- Default remains `Guid.NewGuid()` (UUID v4) via `GuidIdGenerationStrategy.Random`.
- Consumers may opt into `Guid.CreateVersion7()` (UUID v7) per document binding with `WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)`.
- BSON wire format is unchanged (`GuidRepresentation.Standard`); only generation differs.

### Policy interfaces (implemented with [#61](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/61))

Independent, opt-in interfaces composed on concrete document types:

| Interface | Members |
|-----------|---------|
| `IHasConcurrencyToken` | `long ETag` |
| `ISoftDeletable` | `bool IsDeleted` |
| `IAuditableDocument` | `DateTime CreatedAt`, `DateTime UpdatedAt` |

Repository soft-delete filtering, ETag concurrency, and audit stamping activate only when the corresponding interface is implemented. DI features such as `WithSoftDelete()` and `WithGuidIdGeneration(...)` fail closed at registration when the document type lacks the required capability.

### Serialization / BSON id conventions

- Property named `Id` continues to map to `_id` via driver conventions; `[BsonId]` is not required.
- Existing `GuidSerializer(GuidRepresentation.Standard)` registration remains process-wide and is independent of UUID version.

## Consequences

- Hard breaking change to `IDocumentEntity` and related public surface; no compatibility shims.
- Call sites that previously assumed `Guid Id` / mandatory ETag / soft-delete / audit properties must compose the new interfaces explicitly.
- Public API baseline (`PublicAPI.*.txt`, `docs/api/v1-public-api.md`), README, and samples must be updated in the same milestone.
- Small one-time reflection cost per document type at first id-accessor resolution; hot paths use the cached typed accessor.
- `GenericRepositoryExtensions` helpers that take `Guid id` constrain to `IDocumentEntity<Guid>`; ETag-aware delete helpers also require `IHasConcurrencyToken`.
