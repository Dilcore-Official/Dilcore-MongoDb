# AGENTS.md

Maintainer and coding-agent guide for **Dilcore MongoDB**. Consumer usage belongs in README, samples, and (later) a consumer skill — not here.

## Mission

This library is an **opinionated .NET MongoDB application toolkit**. It exists so each service does not reimplement the same cross-cutting MongoDB application concerns: validated multi-cluster / multi-database DI, scoped tenant-aware **namespace resolution** (without first-class tenant types), composable document policies (concurrency, audit, soft delete), repository helpers with FluentResults, and **direct driver escape hatches**.

It is **not** a replacement for `MongoDB.Driver`, **not** Amazon DocumentDB, and **not** a provider-neutral repository. A simple app that needs one client and a few collection calls should use the driver directly.

## Why this shape

Teams copy the same MongoDB host wiring into every service: client lifetime, database/collection naming for tenants, soft delete, ETags, audit stamps, and “just give me the collection.” That copy-paste diverges. Dilcore MongoDB centralizes those policies **once**, keeps `IMongoClient` / `IMongoDatabase` / `IMongoCollection<T>` reachable, and fails closed at startup or namespace resolution instead of on first request.

The completion mission is a production-ready OSS integration for **common MongoDB application cases** already in scope: multi-cluster DI, named bindings, prefix resolvers, generic/bulk/projection repositories, BsonDocument path, Guid/ObjectId identifiers, global conventions, typed operation errors, and JSON adapters. Planned work (provisioning runners, multi-document transactions, streaming lifecycle, observability, vector search) is owned by [ROADMAP.md](ROADMAP.md). Do not add those to core unless the current milestone owns them.

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
| Versions / deps | `Directory.Packages.props`, `Directory.Build.props` |
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

Preserve [`.editorconfig`](.editorconfig) on new C#. Verify changed files with the CONTRIBUTING `dotnet format --verify-no-changes` check. Do not mass-reformat unrelated files. CodeRabbit uses this file plus ADRs as review guidelines; do not fight those invariants in review replies without an ADR.

## Security and OSS

Do not commit secrets, connection strings, or `.serena/project.local.yml`. Vulnerability reports go to [SECURITY.md](SECURITY.md). License is MIT; contributions follow [GOVERNANCE.md](GOVERNANCE.md). Prefer issue-linked, reviewable PRs over drive-by public-API expansion.

When a PR changes `src/`, say whether public API, docs, and tests moved together. When it only changes docs/agent config, say that no production code changed.

## Testing intent

- Architecture tests enforce topology and dependency boundaries (no Docker).
- Unit tests cover conventions, extensions, and isolated logic.
- `IntegrationTests` = DI acceptance (`ValidateScopes` / `ValidateOnBuild`).
- `Repositories.IntegrationTests` = repository behavior on real MongoDB.
- Fixing D14–D26 requires tests; do not “simplify” Result/streaming/ETag/soft-delete behavior without them.

Samples illustrate consumption; they are not the public API contract.

Default to the current milestone on `main` (see ROADMAP status). Historical v1 `Dilcore.DocumentDb.*` types exist only in docs labeled historical. Do not resurrect shims, prefix-provider names, or `MongoDbContainer` builders.

