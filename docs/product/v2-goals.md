# Measurable v2 goals

Tracked by [#6](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/6).  
Validated later by M5 packaging/benchmarks ([#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28)–[#31](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/31)) and M6 observability ([#33](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/33)–[#34](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/34)). Package catalog: [package-descriptions.md](package-descriptions.md). Support: [versioning policy](../policies/versioning-and-support.md).

**Branch status** is measured against current `src/` and CI, not against the retired four-package v1 tree.

## Product shape

| Metric | v1 baseline | Current branch | v2 target | Validated by |
|--------|-------------|----------------|-----------|--------------|
| Packable core packages | 4 | **2** | **2** (`Dilcore.MongoDB.Abstractions`, `Dilcore.MongoDB`) | [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12), `PackageTopologyTests` |
| Third-party deps in Abstractions | FluentResults | FluentResults + MongoDB.Driver | **0** preferred; FluentResults only if Result remains a public contract | [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12) |
| Direct runtime deps in primary package | FluentValidation + DI + MongoDB.Driver | DI + MongoDB.Driver | **≤ 3** direct runtime `PackageReference` items | [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12), [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| Consumer single-binding setup | Multi-builder sample | Sample is within budget | **≤ 15** meaningful C# statements for one cluster + one document binding | [#17](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/17), [#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39) |

Optional JSON packages (`Dilcore.MongoDB.SystemTextJson`, `Dilcore.MongoDB.NewtonsoftJson`) are **shipped (M3)** and do not count against the two-core-package goal. OpenTelemetry and VectorData IDs remain extra and **not shipped**.

### Counting rules

- **Direct runtime deps:** count each direct `PackageReference` in the primary package `.csproj` (not transitive packages). `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.DependencyInjection.Abstractions` count as two if both are referenced. Development-only / private-asset packages do not count.
- **Meaningful setup statements:** in a minimal single-binding sample, count executable C# statements that configure services or resolve the binding. Exclude `using` directives, namespace/type declarations, braces-only lines, blank lines, and comments.

## Quality

| Metric | Current | v2 target | Validated by |
|--------|---------|-----------|--------------|
| Line coverage (solution src) | Collected in CI; not gated at v2 target | **≥ 80%** line | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28), [#29](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/29) |
| Branch coverage (solution src) | Not gated | **≥ 70%** branch | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) |
| Public API XML docs | Missing | **100%** of public APIs documented | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) |
| CI integration tests | Docker preflight + DI acceptance + full solution job | Stay green with Testcontainers in CI | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28), [#23](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/23) |

M2.6 conventions are implemented; promoting `ConfigureConventions` from `PublicAPI.Unshipped.txt` to shipped is part of public-API analyzer work (#28), not a new feature.

## Performance / telemetry budgets

Baselines must be captured before enforcement. BenchmarkDotNet suite: [#31](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/31) (`test/Benchmarks/`). Telemetry overhead benches are placeholders until M6.

| Metric | v2 budget | Notes |
|--------|-----------|-------|
| Cold-start: DI registration + first resolution vs direct driver | **≤ 5%** regression | Median vs recorded baseline |
| Telemetry disabled overhead | **≤ 1%** | No listeners / meters disabled — **planned (M6)** |
| Telemetry enabled overhead | **≤ 3%** | Agreed balanced budget; validated in [#34](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/34) — **planned** |

### Benchmark protocol

Use BenchmarkDotNet for all three budgets. Record runtime, OS, CPU, and commit SHA with each baseline.

| Budget | Operation boundary | Comparison |
|--------|--------------------|------------|
| Cold-start | Library path: configure DI + resolve one document binding once. Control path: create `MongoClient` + get one collection once. | Median library vs median control |
| Telemetry disabled | Steady-state repository read/write with no `ActivityListener` / meter listeners | Median vs same workload baseline captured with telemetry hooks absent |
| Telemetry enabled | Same workload with ActivitySource + Meter listeners attached (exporter-neutral in-process listeners only) | Median vs the disabled-telemetry baseline for the same commit |

Default run shape unless a later ADR changes it: at least 1 warmup iteration and 15 measured iterations (or BenchmarkDotNet defaults that meet those floors). Fail the budget if median regression exceeds the table threshold.

## Documentation quality

| Metric | v2 target | Validated by |
|--------|-----------|--------------|
| README | Concise landing page, MongoDB-honest positioning | [#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39) |
| Structured docs | Cover package selection, DI, tenancy, policies, JSON, transactions, streaming, telemetry, vector, migration | [#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39) |
| Snippets | Compile / tested samples | [#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39) |
| Internal doc links | **0** broken links in README / docs / ROADMAP | [#46](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/46), M8 review |
| Context7 | Consumer docs indexed; AGENTS/skills excluded | [#40](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/40) |

## How goals are referenced

- M5 benchmarks ([#31](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/31)) must cite the cold-start and telemetry budgets above.
- M6 acceptance ([#33](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/33), [#34](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/34)) must prove disabled ≤1% and enabled ≤3%.
- ROADMAP M0 exit criteria link here and to the naming/support ADRs.
