# Package selection

| Package | Use when |
|---------|----------|
| `Dilcore.MongoDB.Abstractions` | You only need contracts / interfaces (e.g. libraries that define repositories without hosting DI). |
| `Dilcore.MongoDB` | Application host: DI registration, namespace resolution, repositories, driver integration. |

### Optional future packages (not shipped yet)

These IDs are reserved for later milestones; do **not** create empty projects today:

| Future package | Intent |
|----------------|--------|
| `Dilcore.MongoDB.SystemTextJson` | System.Text.Json adapters through the same binding / namespace pipeline |
| `Dilcore.MongoDB.NewtonsoftJson` | Newtonsoft.Json adapters |
| `Dilcore.MongoDB.OpenTelemetry` | Opt-in OpenTelemetry instrumentation (exporter-neutral) |
| `Dilcore.MongoDB.VectorData` | MongoDB Vector Search / .NET vector abstractions |

Reference the primary package unless you are authoring a library that must stay DI-free.
