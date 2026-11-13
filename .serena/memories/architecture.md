# Architecture

Opinionated MongoDB application toolkit. Core is two packages:

- `Dilcore.MongoDB.Abstractions` — contracts, keys, namespace, policies, repository interfaces (no DI host wiring).
- `Dilcore.MongoDB` — DI, namespace pipeline, repositories, conventions, provisioning, transactions, driver integration.

Optional M3 JSON adapters (isolated graphs):

- `Dilcore.MongoDB.SystemTextJson`
- `Dilcore.MongoDB.NewtonsoftJson` — never referenced by core or the STJ package.

Single public DI entry: `AddMongoDb`. Repositories fold into the primary package. OpenTelemetry / VectorData IDs remain planned, not shipped.

v1 `Dilcore.DocumentDb.*` four-package graph is historical only. Decisions: [ADR 0001](../../docs/adr/0001-package-naming.md), [ADR 0002](../../docs/adr/0002-generic-document-identifier.md), [ADR 0003](../../docs/adr/0003-serialization-conventions.md). Topology is enforced by architecture tests; inspect `src/` and `PublicAPI.*.txt` for the live surface.
