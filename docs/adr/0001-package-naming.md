# ADR 0001: Package naming and reserved NuGet IDs

- **Status:** Accepted
- **Date:** 2026-08-02
- **Issue:** [#4](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/4)
- **Blocks:** [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12) package topology

## Context

The repository currently ships four NuGet packages under `Dilcore.DocumentDb.*` while implementing only MongoDB. The product name collides with Amazon DocumentDB messaging, and `Dilcore.DocumentDb.MongoDb.Abstractions` already ships types under the `Dilcore.DocumentDb.Abstractions` namespace. There are no external consumers requiring a compatibility path.

## Decision drivers

- Honest positioning as a MongoDB application toolkit (not provider-neutral).
- Avoid Amazon DocumentDB confusion.
- Support the agreed two-package M2 topology.
- Prefer a clean break because nobody consumes v1 yet.

## Options considered

| Option | Summary | Rejected because |
|--------|---------|------------------|
| A. Keep `Dilcore.DocumentDb.*` | Rename messaging only | Still implies DocumentDB/provider neutrality; keeps Amazon confusion |
| B. Keep current `Dilcore.DocumentDb.MongoDb*` graph | Mongo-qualified DocumentDb prefix | Four-package graph remains; still DocumentDb-branded |
| **C. Rename to `Dilcore.MongoDB*`** | MongoDB-first brand and namespaces | **Accepted** |

## Decision

v2 packages and namespaces rename to **`Dilcore.MongoDB`**.

### Required package IDs

| NuGet ID | Role |
|----------|------|
| `Dilcore.MongoDB.Abstractions` | Contracts, entity/policy abstractions, MongoDB-facing interfaces without DI host wiring |
| `Dilcore.MongoDB` | Primary package: DI, providers, repositories, policies, driver integration |

NuGet.org availability check on 2026-08-02 returned HTTP 404 for both IDs (not published). Owner still must reserve/claim IDs on first publish.

### Optional integration IDs

**Shipped in M3** (projects exist under `src/`):

- `Dilcore.MongoDB.SystemTextJson`
- `Dilcore.MongoDB.NewtonsoftJson`

Recorded for later milestones; do **not** create empty reservation packages:

- `Dilcore.MongoDB.OpenTelemetry`
- `Dilcore.MongoDB.VectorData`

### Namespace / project map (v1 → v2)

| v1 package | v2 package | Notes |
|------------|------------|-------|
| `Dilcore.DocumentDb.Abstractions` | `Dilcore.MongoDB.Abstractions` | Merge surviving contracts |
| `Dilcore.DocumentDb.MongoDb.Abstractions` | `Dilcore.MongoDB.Abstractions` | Eliminate separate package + fix namespace |
| `Dilcore.DocumentDb.MongoDb` | `Dilcore.MongoDB` | Primary implementation package |
| `Dilcore.DocumentDb.MongoDb.Repositories` | `Dilcore.MongoDB` | Fold repositories into primary package unless M2 proves independent value |

Root namespaces become `Dilcore.MongoDB` / `Dilcore.MongoDB.Abstractions` (exact sub-namespaces decided in [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12)).

## Amazon DocumentDB

The library is **not** an Amazon DocumentDB client library. Documentation and package descriptions must state MongoDB compatibility explicitly and must not use “DocumentDB” as the product name after v2 rename. MongoDB-compatible services may work when the driver does, but they are not a supported product identity.

## Consequences

- Breaking rename for package IDs, assembly names, and namespaces.
- No compatibility packages, type forwards, or obsolete aliases for v1 → v2 (see [versioning policy](../policies/versioning-and-support.md)).
- Repository metadata, README, Context7 library ID, and consumer skill names must follow the MongoDB naming in later milestones.
- M2 topology work ([#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12), [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13)) implements the two-package **core**.
- M3 ([#20](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/20)) ships the two JSON adapter packages. Newtonsoft remains isolated from core and from System.Text.Json consumers.
