# Package descriptions

Canonical NuGet `PackageDescription` / package-selection copy for Dilcore MongoDB v2.  
Owned by [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12) (topology) and verified in publish metadata by [#30](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/30).

**Rules**

- Descriptions must say **MongoDB** explicitly; never brand the product as DocumentDB.
- Core packages ship in M2; optional IDs below are planned only until their milestone creates the project.
- Keep `PackageDescription` ≤ ~300 characters, one sentence when possible, and aligned with this table.
- Tags should include `mongodb` and role-specific terms; do not include Amazon DocumentDB or competing vendor product names.

## Core (M2)

| NuGet ID | When to use | PackageDescription |
|----------|-------------|--------------------|
| `Dilcore.MongoDB.Abstractions` | Reference contracts from libraries that must not take a DI host dependency | Contracts, entity and policy abstractions, and MongoDB-facing interfaces for Dilcore.MongoDB without DI host wiring |
| `Dilcore.MongoDB` | Default application dependency for DI, collections, repositories, and policies | Opinionated MongoDB application toolkit: validated multi-cluster DI, namespace resolution, repositories, document policies, and direct MongoDB.Driver escape hatches |

## Optional integrations (planned)

Confirm package creation before first publish of each ID ([ADR 0001](../adr/0001-package-naming.md)).

| NuGet ID | Milestone | When to use | PackageDescription |
|----------|-----------|-------------|--------------------|
| `Dilcore.MongoDB.SystemTextJson` | M3 (#20) | Apps that store or convert documents via System.Text.Json | System.Text.Json adapters for Dilcore.MongoDB with Extended JSON type fidelity and the same database/collection resolvers as typed documents |
| `Dilcore.MongoDB.NewtonsoftJson` | M3 (#20) | Apps that store or convert documents via Newtonsoft.Json | Newtonsoft.Json adapters for Dilcore.MongoDB with Extended JSON type fidelity; optional so System.Text.Json consumers never take a Newtonsoft dependency |
| `Dilcore.MongoDB.OpenTelemetry` | M6 (#33) | Hosts that want library sources/meters registered without exporters | OpenTelemetry registration helpers for Dilcore.MongoDB ActivitySource and Meter; exporters remain host-owned |
| `Dilcore.MongoDB.VectorData` | M7 (#36–#38) | Vector / semantic search on MongoDB Atlas or Search-capable servers | MongoDB Vector Search helpers for Dilcore.MongoDB, interoperable with Microsoft.Extensions.AI embeddings without owning embedding models |

## Streaming

Streaming ships as an **independent feature** ([#24](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/24)): prefer a separate namespace / opt-in registration inside `Dilcore.MongoDB` unless M4 proves a separate package is required for dependency lifecycle.

| If kept in core | PackageDescription addition (README / docs, not a separate ID) |
|-----------------|----------------------------------------------------------------|
| Finite query + change streams | Cold `IAsyncEnumerable` query streaming and change-stream APIs with deterministic cursor disposal and the shared namespace resolver pipeline |

| If split later | PackageDescription |
|----------------|--------------------|
| `Dilcore.MongoDB.Streaming` (TBD) | Finite query streaming and MongoDB change streams for Dilcore.MongoDB with explicit checkpoints and no hidden auto-retry |

## Not separate packages

| Concern | Lives in | Reason |
|---------|----------|--------|
| Repositories / document policies | `Dilcore.MongoDB` | Folded into primary package unless M2 proves independent value |
| Transactions | `Dilcore.MongoDB` | Thin coordinator over driver sessions (#21) |
| Provisioning / migrations | `Dilcore.MongoDB` | Idempotent runner outside request hot paths (#22) |
| Azure Monitor / App Insights / CloudWatch exporters | Host samples only | Never core or optional Dilcore packages (#35) |
