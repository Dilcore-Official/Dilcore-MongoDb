# Package selection

Canonical companion: [package-descriptions.md](package-descriptions.md). Naming: [ADR 0001](../adr/0001-package-naming.md). Scope: [ROADMAP.md](../../ROADMAP.md).

**Current:** two core packages plus two optional JSON adapter packages in `src/`. OpenTelemetry and VectorData IDs below are **not shipped**.

| Package | Use when |
|---------|----------|
| `Dilcore.MongoDB.Abstractions` | You only need contracts / interfaces (e.g. libraries that define repositories without hosting DI). |
| `Dilcore.MongoDB` | Application host: DI registration, namespace resolution, repositories, conventions, provisioning, transactions, driver integration. |
| `Dilcore.MongoDB.SystemTextJson` | System.Text.Json DOM adapters through the shared BSON conversion engine. |
| `Dilcore.MongoDB.NewtonsoftJson` | Newtonsoft.Json DOM adapters; do not reference this from STJ-only apps. |

Reference the primary package unless you are authoring a library that must stay DI-free.

### Optional future packages (not shipped yet)

These IDs are reserved for later milestones; do **not** create empty projects today:

| Future package | Intent |
|----------------|--------|
| `Dilcore.MongoDB.OpenTelemetry` | Opt-in OpenTelemetry instrumentation (exporter-neutral) |
| `Dilcore.MongoDB.VectorData` | MongoDB Vector Search / .NET vector abstractions |
