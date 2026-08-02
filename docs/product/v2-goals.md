# Measurable v2 goals

Tracked by [#6](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/6).  
Validated later by M5 packaging/benchmarks ([#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28)–[#31](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/31)) and M6 observability ([#33](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/33)–[#34](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/34)).

## Product shape

| Metric | Current (v1) | v2 target | Validated by |
|--------|--------------|-----------|--------------|
| Packable core packages | 4 | **2** (`Dilcore.MongoDB.Abstractions`, `Dilcore.MongoDB`) | [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12) |
| Third-party deps in Abstractions | FluentResults | **0** preferred; FluentResults only if Result remains a public contract and cannot be inlined | [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12) |
| Direct runtime deps in primary package | FluentValidation + DI + MongoDB.Driver (+ transitive Result) | **≤ 3** direct runtime PackageReferences (driver + DI abstractions/extensions + at most one Result/helper package); no FluentValidation unless justified | [#12](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/12), [#13](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/13) |
| Consumer single-binding setup | Multi-builder sample | **≤ 15** meaningful C# statements for one cluster + one document binding | [#17](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/17), [#39](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/39) |

Optional JSON / OpenTelemetry / VectorData packages are extra and do not count against the two-core-package goal.

## Quality

| Metric | Current | v2 target | Validated by |
|--------|---------|-----------|--------------|
| Line coverage (solution src) | Partial; unit-only Abstractions ~87%; integration Docker-dependent | **≥ 80%** line | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28), [#29](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/29) |
| Branch coverage (solution src) | Not gated | **≥ 70%** branch | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) |
| Public API XML docs | Missing | **100%** of public APIs documented | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28) |
| CI integration tests | No Docker service | Green with Testcontainers in CI | [#28](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/28), [#23](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/23) |

## Performance / telemetry budgets

Baselines must be captured before enforcement. BenchmarkDotNet suite lands in [#31](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/31).

| Metric | v2 budget | Notes |
|--------|-----------|-------|
| Cold-start: DI registration + first resolution vs direct driver | **≤ 5%** regression | Measure median; record machine/runtime |
| Telemetry disabled overhead | **≤ 1%** | No listeners / meters disabled |
| Telemetry enabled overhead | **≤ 3%** | Agreed balanced budget; validated in [#34](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/34) |

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
