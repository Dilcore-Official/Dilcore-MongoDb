# Package selection

Canonical companion: [package-descriptions.md](package-descriptions.md). Naming: [ADR 0001](../adr/0001-package-naming.md). Scope: [ROADMAP.md](../../ROADMAP.md).

**Current:** two core packages in `src/` are available to consume. Optional IDs below are **not shipped**.

| Package | Use when |
|---------|----------|
| `Dilcore.MongoDB.Abstractions` | You only need contracts / interfaces (e.g. libraries that define repositories without hosting DI). |
| `Dilcore.MongoDB` | Application host: DI registration, namespace resolution, repositories, conventions, driver integration. |

Reference the primary package unless you are authoring a library that must stay DI-free.

### Optional future packages (not shipped yet)

These IDs are reserved for later milestones; do **not** create empty projects today:

| Future package | Intent |
|----------------|--------|
| `Dilcore.MongoDB.SystemTextJson` | System.Text.Json adapters through the same binding / namespace pipeline |
| `Dilcore.MongoDB.NewtonsoftJson` | Newtonsoft.Json adapters |
| `Dilcore.MongoDB.OpenTelemetry` | Opt-in OpenTelemetry instrumentation (exporter-neutral) |
| `Dilcore.MongoDB.VectorData` | MongoDB Vector Search / .NET vector abstractions |
