# ADR 0003: Process-wide BSON serialization conventions

- **Status:** Accepted
- **Date:** 2026-08-18
- **Issue:** [#63](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/63)
- **Implements:** [#64](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/64), [#65](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/65)

## Context

`MongoDbCollectionFactory` previously registered one hardcoded, process-wide `ConventionPack` (enum-as-string, camelCase names, ignore-null, ignore-extra-elements) lazily on first collection resolution. Consumers could not opt into enum-as-int, a different naming convention, or extra custom conventions without forking the library.

MongoDB.Driver's `ConventionRegistry` is static and process-wide. Per-cluster, per-database, or per-document packs would still collide unless they used disjoint type filters, and the generic repository remains a single serialization pipeline.

## Decision drivers

- Keep today's defaults when `ConfigureConventions` is not called.
- Fail closed at startup, not on first request.
- Match the existing `IMongoDbBuilder` fluent surface.
- Do not register conventions during ordinary collection resolution.

## Options considered

| Option | Summary | Rejected because |
|--------|---------|------------------|
| A. Per-document / per-binding packs | Configure conventions on `AddDocumentBinding` | `ConventionRegistry` is not binding-scoped; class maps are type-global |
| B. Per-cluster / per-database packs | Nested builder methods | Same process-wide registry; implied isolation would be false |
| **C. Global defaults on `AddMongoDb`** | One `ConfigureConventions` callback per call | **Accepted** |

## Decision

Serialization conventions are **global (process-wide)** and configured at most once per `AddMongoDb` via `IMongoDbBuilder.ConfigureConventions`.

- Defaults: `BsonType.String` enums, `CamelCaseElementNameConvention`, `IgnoreIfNullConvention(true)`, `IgnoreExtraElementsConvention(true)`.
- Registration is eager during `AddMongoDb` and idempotent when the structural signature matches.
- A later `AddMongoDb` in the same process with a different signature throws `InvalidOperationException`.
- Custom `IConvention` / named `IConventionPack` entries (with a type filter) are supported on the same builder.
- No per-document overrides in this milestone.

## Consequences

- Multiple hosts or test fixtures in one process must share the same convention settings (or reset registry state in tests).
- Changing conventions after types have already been class-mapped has no effect on those maps.
