# AGENTS.md

Maintainer and coding-agent guide for **Dilcore MongoDB**. Consumer usage belongs in README, samples, and (later) a consumer skill — not here.

## Mission

This library is an **opinionated .NET MongoDB application toolkit**. It exists so each service does not reimplement the same cross-cutting MongoDB application concerns: validated multi-cluster / multi-database DI, scoped tenant-aware **namespace resolution** (without first-class tenant types), composable document policies (concurrency, audit, soft delete), repository helpers with FluentResults, and **direct driver escape hatches**.

It is **not** a replacement for `MongoDB.Driver`, **not** Amazon DocumentDB, and **not** a provider-neutral repository. A simple app that needs one client and a few collection calls should use the driver directly.

## Why this shape

Teams copy the same MongoDB host wiring into every service: client lifetime, database/collection naming for tenants, soft delete, ETags, audit stamps, and “just give me the collection.” That copy-paste diverges. Dilcore MongoDB centralizes those policies **once**, keeps `IMongoClient` / `IMongoDatabase` / `IMongoCollection<T>` reachable, and fails closed at startup or namespace resolution instead of on first request.

The completion mission is a production-ready OSS integration for **common MongoDB application cases** already in scope: multi-cluster DI, named bindings, prefix resolvers, generic/bulk/projection repositories, BsonDocument path, Guid/ObjectId identifiers, global conventions, typed operation errors, explicit provisioning, JSON adapters, and budgeted multi-document transactions. Planned work (streaming lifecycle, observability, vector search) is owned by [ROADMAP.md](ROADMAP.md). Do not add those to core unless the current milestone owns them.

Treat this as an **externally consumed library**: public surface is a contract, samples must compile against shipped APIs, and breaking changes follow the versioning policy. Prefer small, tested, documented PRs over silent refactors.

## Architecture decisions (do not re-litigate)

- [ADR 0001](docs/adr/0001-package-naming.md) — core packages `Dilcore.MongoDB.Abstractions` and `Dilcore.MongoDB`; optional M3 JSON packages `Dilcore.MongoDB.SystemTextJson` and `Dilcore.MongoDB.NewtonsoftJson`; MongoDB-first naming; no v1 shims.
- [ADR 0002](docs/adr/0002-generic-document-identifier.md) — marker `IDocumentEntity` + `IDocumentEntity<TId>`; repositories stay single-generic; policies are opt-in interfaces.
- [ADR 0003](docs/adr/0003-serialization-conventions.md) — process-wide BSON conventions configured once on `AddMongoDb`; never during collection resolution.

## Constraints

- Two core packable `src/` projects plus two optional JSON adapter packages. Abstractions has no DI host wiring. `Newtonsoft.Json` stays out of core and out of `Dilcore.MongoDB.SystemTextJson`.
- Single public DI entry: `AddMongoDb`.
- No first-class tenant APIs; apps own `INamespacePrefixResolver`; missing prefixes fail closed.
- Optional policies compose on document types; DI features that require a capability fail closed at registration.
- No vendor exporters (Azure Monitor, App Insights, AWS, OpenAI) in core packages.
- Do not hide MongoDB query types behind a lowest-common-denominator abstraction.
- Singleton `IMongoClient` per unique cluster settings; do not log credentials, connection strings, or resume tokens.
- Public API changes update `PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` in the same PR.
- Tests: NUnit + **Shouldly** (not FluentAssertions). Formatting: [`.editorconfig`](.editorconfig).

## Code style rules

Cursor rules are the agent-facing style contract. Do not copy their text here.

- [C# code style](.cursor/rules/csharp-code-style.mdc) — no trailing newline on C# files, one top-level type per file (`SA1402`, tests exempt), unused `using` directives removed (`IDE0005` error), unused NuGet packages removed at the project and from central versions, `dotnet format` after each stage, `CancellationToken cancellationToken = default` forwarded into every cancellable call, single-purpose methods, a loop source and a method argument are locals (a lambda may be an argument), LINQ stays deferred unless a concrete collection or a second pass is required (`CA1851` error), blank lines split setup from the query and the result, a method has at most one nested block, XML docs on the interface (or on the type when it has no interface), standard member order, current .NET practices checked with Context7 and Microsoft Learn.
- [Tests and DI lifetimes](.cursor/rules/csharp-testing-di.mdc) — NUnit + Shouldly, Arrange / Act / Assert comments on every `[Test]`, and a strict singleton / scoped / transient test for every DI registration (`ValidateScopes` and `ValidateOnBuild`). Test layout is in [test/AGENTS.md](test/AGENTS.md).

## Build props and package versions

Two MSBuild files cover every project. Do not copy version numbers from them into docs or into a `.csproj`.

[Directory.Build.props](Directory.Build.props) sits at the repository root. The SDK imports the nearest `Directory.Build.props` it finds by walking up from the project. It is the shared compile surface: `net10.0`, latest C#, implicit usings, nullable, deterministic builds, .NET analyzers at `AnalysisLevel=latest` and `AnalysisMode=Default`, `StyleCop.Analyzers` with only `SA1402` enabled (one top-level type per file; tests set that rule to none), `SonarAnalyzer.CSharp` with only `S3267` enabled (`SonarAnalyzer.globalconfig` turns the other rule ids off; a loop that maps its iteration variable uses LINQ), editorconfig style rules enforced in the build, Release warnings-as-errors, NuGet audit, and `RestorePackagesWithLockFile`. `NU1608` stays a warning because AutoFixture.NUnit4 does not declare NUnit 5. `ContinuousIntegrationBuild` turns on when `GITHUB_ACTIONS` is set.

[src/Directory.Build.props](src/Directory.Build.props) is closer to the libraries, so it is the file the SDK imports for `src/` projects. It re-imports the root file with `GetPathOfFileAbove`, then adds the packable layer: `IsPackable`, package metadata, the README packed at the package root, `GenerateDocumentationFile`, `AnalysisMode=Recommended`, and `Microsoft.CodeAnalysis.PublicApiAnalyzers` (private assets). Tests and samples stay on the root file only, so they stay unpackable and on `AnalysisMode=Default`.

[Directory.Packages.props](Directory.Packages.props) is central package management (`ManagePackageVersionsCentrally`, transitive pinning off). A package version is added there once. Projects reference it with `PackageReference Include="…"` and no `Version`. Analyzer packages set `PrivateAssets` and `IncludeAssets` on the `PackageVersion` and again on the `PackageReference` that opts in. Restore writes `packages.lock.json` next to each project; CI restore uses `--locked-mode`.

### Namespace

`RootNamespace` is a per-project property. The props files supply compile and package settings. Each library and test `.csproj` sets `RootNamespace` to that project's identity:

| Project | `RootNamespace` |
|---------|-----------------|
| `Dilcore.MongoDB.Abstractions` | `Dilcore.MongoDB.Abstractions` |
| `Dilcore.MongoDB` | `Dilcore.MongoDB` |
| `Dilcore.MongoDB.SystemTextJson` | `Dilcore.MongoDB.SystemTextJson` |
| `Dilcore.MongoDB.NewtonsoftJson` | `Dilcore.MongoDB.NewtonsoftJson` |
| `test/*` | The test assembly name (`Dilcore.MongoDB.UnitTests`, and the same pattern for the other test projects) |

A source file's namespace is that `RootNamespace` plus the folder path under the project. `src/Dilcore.MongoDB/Extensions/ServiceCollectionExtensions.cs` is `namespace Dilcore.MongoDB.Extensions`. The declaration is written in the file; MSBuild does not generate it. A new file uses the same rule. Samples omit `RootNamespace`, so the SDK default is the project file name.

## Documentation authority

| Question | Source |
|----------|--------|
| What we build and why / milestone scope | [ROADMAP.md](ROADMAP.md) |
| Decisions | [docs/adr/](docs/adr/) |
| How to build, test, PR | [CONTRIBUTING.md](CONTRIBUTING.md) |
| Versioning / support | [docs/policies/versioning-and-support.md](docs/policies/versioning-and-support.md) |
| Package catalog / selection | [docs/product/](docs/product/) |
| Consumer how-tos | [docs/guides/](docs/guides/) |
| Historical v1 API | [docs/api/v1-public-api.md](docs/api/v1-public-api.md) |
| Open correctness defects | [docs/product/v1-defects.md](docs/product/v1-defects.md) |
| Live public surface | `src/*/PublicAPI.*.txt` |
| Versions / deps / `RootNamespace` | [Build props and package versions](#build-props-and-package-versions); pins stay in `Directory.Packages.props` and `Directory.Build.props` |
| Serena invariants | [`.serena/memories/`](.serena/memories/) |

Do not duplicate command lists, version pins, file trees, or PublicAPI member lists in this file.

## Keep documentation in sync with code

Every change that alters behavior, package topology, public API, compatibility, setup, samples, or milestone **status** must update the affected docs and PublicAPI baselines **in the same change**.

- Label claims **historical**, **current**, or **planned**. Never present planned ROADMAP work as shipped.
- Volatile values (driver pin, package versions, publish feed, git tags) **link** to manifests or workflows; do not copy numbers that Dependabot will move.
- If README, ROADMAP, ADRs, `docs/*`, samples, or Serena memories contradict `src/` or tests, fix the docs in that PR.
- Consumer docs stay honest MongoDB toolkit copy; this file stays maintainer/agent guidance (exclude from Context7 consumer indexes).

## How to work (tools)

This file is What/Why. **Serena is the C# How** — discovery *and* mutation. Do not treat it as read-only. The authoritative tool workflow (Serena MCP mapping, SubAgents, Context7, Microsoft Learn MCP) lives in [CONTRIBUTING.md](CONTRIBUTING.md#agent-tool-workflow); do not duplicate it here.

If docs and code disagree, **code + tests + PublicAPI baselines** win, then update docs. Confirm driver APIs with Context7, not memorized 2.x signatures. CodeRabbit: [`.coderabbit.yaml`](.coderabbit.yaml).

## Contribution boundaries

Follow [CONTRIBUTING.md](CONTRIBUTING.md): .NET SDK 10.0.x, Docker for integration/benchmarks, Shouldly, PublicAPI baselines, roadmap script when ROADMAP changes, focused PRs, no secrets. Security reports go through [SECURITY.md](SECURITY.md), never a public issue.

Preserve [`.editorconfig`](.editorconfig) on new C#. Verify changed files with the CONTRIBUTING `dotnet format --verify-no-changes` check. Do not mass-reformat unrelated files. CodeRabbit uses this file, the cursor rules, `.editorconfig`, `SonarAnalyzer.globalconfig`, and ADRs as review guidelines; do not fight those invariants in review replies without an ADR.

## Security and OSS

Do not commit secrets, connection strings, or `.serena/project.local.yml`. Vulnerability reports go to [SECURITY.md](SECURITY.md). License is MIT; contributions follow [GOVERNANCE.md](GOVERNANCE.md). Prefer issue-linked, reviewable PRs over drive-by public-API expansion.

When a PR changes `src/`, say whether public API, docs, and tests moved together. When it only changes docs/agent config, say that no production code changed.

## Testing intent

- Architecture tests enforce topology and dependency boundaries (no Docker).
- Unit tests cover conventions, extensions, and isolated logic.
- `IntegrationTests` = DI acceptance (`ValidateScopes` / `ValidateOnBuild`).
- `Repositories.IntegrationTests` = repository behavior on real MongoDB.
- `Json.IntegrationTests` = JSON adapter fidelity and persistence.
- `PackageTests` inspect packed `.nupkg` contents. `ConsumerTests` compile and run `AddMongoDb` against those packages. Neither needs Docker.
- Shared fixtures live in `test/TestSupport`. Details and the AAA rule are in [test/AGENTS.md](test/AGENTS.md).
- Fixing D14–D26 requires tests; do not “simplify” Result/streaming/ETag/soft-delete behavior without them.

Samples illustrate consumption; they are not the public API contract.

Default to the current milestone on `main` (see ROADMAP status). Historical v1 `Dilcore.DocumentDb.*` types exist only in docs labeled historical. Do not resurrect shims, prefix-provider names, or `MongoDbContainer` builders.

