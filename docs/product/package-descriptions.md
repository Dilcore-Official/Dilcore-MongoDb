# Package descriptions

Canonical NuGet `PackageDescription` / package-selection copy for Dilcore MongoDB v2.  
Owned by [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12) (topology) and verified in publish metadata by [#30](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/30). Companion: [package-selection.md](package-selection.md). Decisions: [ADR 0001](../adr/0001-package-naming.md), [ADR 0002](../adr/0002-generic-document-identifier.md), [ADR 0003](../adr/0003-serialization-conventions.md).

**Rules**

- Descriptions must say **MongoDB** explicitly; never brand the product as DocumentDB.
- Core packages below are **shipped** in `src/`. Optional JSON packages are **shipped in M3**. OpenTelemetry and VectorData remain **planned** until their milestone creates the project.
- Keep `PackageDescription` ≤ ~300 characters, one sentence when possible, and aligned with this table.
- Tags should include `mongodb` and role-specific terms; do not include Amazon DocumentDB or competing vendor product names.

## Core (shipped — M2+)

| NuGet ID | When to use | PackageDescription |
|----------|-------------|--------------------|
| `Dilcore.MongoDB.Abstractions` | Reference contracts from libraries that must not take a DI host dependency | Contracts, entity and policy abstractions, and MongoDB-facing interfaces for Dilcore.MongoDB without DI host wiring |
| `Dilcore.MongoDB` | Default application dependency for DI, collections, repositories, and policies | Opinionated MongoDB application toolkit: validated multi-cluster DI, namespace resolution, repositories, document policies, and direct MongoDB.Driver escape hatches |

Core includes repository interfaces, composable document policies, named bindings, and process-wide serialization conventions. Copy in each `.csproj` `PackageDescription` must stay aligned with this table.

## Optional integrations

| NuGet ID | Milestone | When to use | PackageDescription |
|----------|-----------|-------------|--------------------|
| `Dilcore.MongoDB.SystemTextJson` | **Shipped M3** (#20) | Apps that store or convert documents via System.Text.Json | System.Text.Json adapters for Dilcore.MongoDB with Extended JSON type fidelity and the same database/collection resolvers as typed documents |
| `Dilcore.MongoDB.NewtonsoftJson` | **Shipped M3** (#20) | Apps that store or convert documents via Newtonsoft.Json | Newtonsoft.Json adapters for Dilcore.MongoDB with Extended JSON type fidelity; optional so System.Text.Json consumers never take a Newtonsoft dependency |
| `Dilcore.MongoDB.OpenTelemetry` | Planned M6 (#33) | Hosts that want library sources/meters registered without exporters | OpenTelemetry registration helpers for Dilcore.MongoDB ActivitySource and Meter; exporters remain host-owned |
| `Dilcore.MongoDB.VectorData` | Planned M7 (#36–#38) | Vector / semantic search on MongoDB Atlas or Search-capable servers | MongoDB Vector Search helpers for Dilcore.MongoDB, interoperable with Microsoft.Extensions.AI embeddings without owning embedding models |

## Streaming

Streaming ships as an **independent planned feature** ([#24](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/24)): prefer a separate namespace / opt-in registration inside `Dilcore.MongoDB` unless M4 proves a separate package is required for dependency lifecycle. `IAsyncEnumerable` query methods already exist on repositories; the M4 work is cursor lifecycle and change streams, not “introduce IAsyncEnumerable”.

| If kept in core | PackageDescription addition (README / docs, not a separate ID) |
|-----------------|----------------------------------------------------------------|
| Finite query + change streams | Cold `IAsyncEnumerable` query streaming and change-stream APIs with deterministic cursor disposal and the shared namespace resolver pipeline |

| If split later | PackageDescription |
|----------------|--------------------|
| `Dilcore.MongoDB.Streaming` (TBD) | Finite query streaming and MongoDB change streams for Dilcore.MongoDB with explicit checkpoints and no hidden auto-retry |

## Not separate packages

| Concern | Lives in | Reason |
|---------|----------|--------|
| Repositories / document policies | `Dilcore.MongoDB` | Folded into primary package (M2 complete) |
| Transactions | `Dilcore.MongoDB` | Thin coordinator over driver sessions (#21) — **current** |
| Provisioning / migrations | `Dilcore.MongoDB` | Idempotent runner outside request hot paths (#22) — **current** |
| Azure Monitor / App Insights / CloudWatch exporters | Host samples only | Never core or optional Dilcore packages (#35) |
