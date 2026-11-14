# Contributing

Thanks for contributing to **Dilcore MongoDB** (repository:
[Dilcore-Official/Dilcore-MongoDb](https://github.com/Dilcore-Official/Dilcore-MongoDb)).

Please read the [Code of Conduct](CODE_OF_CONDUCT.md) before participating.
Coding agents should follow [AGENTS.md](AGENTS.md).

## Ways to contribute

| Kind | Channel |
|------|---------|
| Questions / ideas | [Discussions](https://github.com/Dilcore-Official/Dilcore-MongoDb/discussions) |
| Bugs / features | [Issues](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/new/choose) (use the templates) |
| Security reports | [Private vulnerability reporting](SECURITY.md) — never open a public issue |
| Code / docs | Pull requests against `main` |

Roadmap work is tracked in [ROADMAP.md](ROADMAP.md) and issues labeled `roadmap`.

## Development setup

Requirements:

- .NET SDK **10.0.x**
- Docker (for integration tests and benchmarks that use Testcontainers)

```bash
dotnet restore Dilcore.MongoDB.sln
dotnet build Dilcore.MongoDB.sln --configuration Release
dotnet test test/UnitTests --configuration Release
dotnet test test/ArchitectureTests --configuration Release
# DI acceptance / integration (needs Docker for Testcontainers):
dotnet test test/IntegrationTests --configuration Release
# Point an integration project at another official mongo tag (default is 9.0):
MONGODB_IMAGE_TAG=8.0 dotnet test test/IntegrationTests --configuration Release
MONGODB_IMAGE_TAG=7.0 dotnet test test/Json.IntegrationTests --configuration Release
MONGODB_IMAGE_TAG=7.0 dotnet test test/Repositories.IntegrationTests --configuration Release
# Full suite:
dotnet test Dilcore.MongoDB.sln --configuration Release
# Performance benchmarks (needs Docker for repository/bulk/projection tiers):
dotnet run --project test/Benchmarks/Dilcore.MongoDB.Benchmarks -c Release -- --filter '*'
# Cold-start only (no Docker):
dotnet run --project test/Benchmarks/Dilcore.MongoDB.Benchmarks -c Release -- --filter '*ColdStart*'
```

Formatting and analyzer severities are defined in [`.editorconfig`](.editorconfig).
C# files do not end with a newline. Coding agents also follow
[csharp-code-style](.cursor/rules/csharp-code-style.mdc) and
[csharp-testing-di](.cursor/rules/csharp-testing-di.mdc) (linked from
[AGENTS.md](AGENTS.md)). Prefer editor format-on-save and verify without
rewriting the tree:

```bash
dotnet format Dilcore.MongoDB.sln --verify-no-changes
```

Use **Shouldly** for assertions (not FluentAssertions). Architecture tests enforce the
two-core-package plus optional JSON topology and dependency boundaries without Docker. DI acceptance tests
build the container with `ValidateScopes` / `ValidateOnBuild` via
`AcceptanceServiceProviderFactory` in `test/TestSupport`. CI runs Architecture Tests, DI Acceptance,
an Integration Matrix job (`TestCategory=M3Matrix`, Docker preflight), and the full Build & Test job.
Those jobs, and the MongoDB version workflows, call
[`.github/actions/dotnet-test`](.github/actions/dotnet-test/action.yml). That action runs
`dotnet test` with `GitHubActionsTestLogger` (job summary and failure annotations) and
uploads the TRX artifact on every run. CI also runs `.github/workflows/mongo-7.0.yml`,
`mongo-8.0.yml`, and `mongo-9.0.yml`. Each calls `.github/workflows/mongo-compat.yml`
with `MONGODB_IMAGE_TAG` set to that major.minor tag and runs the integration projects.
The existing DI Acceptance and Integration Matrix jobs in `ci.yml` stay on the default
`9.0` image. A weekly schedule
on those version workflows picks up new patch tags of the same major.minor image.
The **Benchmarks** workflow (`.github/workflows/benchmarks.yml`) runs BenchmarkDotNet on
PRs that touch `src/` or `test/Benchmarks/`, posts a non-blocking results comment, and
updates the historical baseline on `main` via `gh-pages`. Create an empty `gh-pages`
branch once if it does not exist yet (`git checkout --orphan gh-pages && git commit
--allow-empty -m "bench history" && git push -u origin gh-pages`).

Roadmap issue hygiene (needs `gh` + `jq`):

```bash
./scripts/verify-roadmap-coverage.sh
```

## Build quality gates

The SDK band is pinned in [`global.json`](global.json) (`rollForward: latestFeature` inside 10.0).
CI uses that file instead of a floating `dotnet-version`.

Libraries under `src/` generate XML documentation and fail the build on missing public
docs (`CS1591`). `Microsoft.CodeAnalysis.PublicApiAnalyzers` checks
`PublicAPI.Shipped.txt` and `PublicAPI.Unshipped.txt`. Release builds treat warnings as
errors. `CancellationToken` parameters are optional and last (`CA1068`), and cancellable
calls must receive the token (`CA2016`).

Restore uses a committed `packages.lock.json` per project. CI restore is
`dotnet restore Dilcore.MongoDB.sln --locked-mode`.

```bash
dotnet format Dilcore.MongoDB.sln --verify-no-changes
dotnet build Dilcore.MongoDB.sln --configuration Release -warnaserror -p:ContinuousIntegrationBuild=true
```

Coding agents follow [csharp-code-style](.cursor/rules/csharp-code-style.mdc) and
[csharp-testing-di](.cursor/rules/csharp-testing-di.mdc). Run `dotnet format` after each
implementation stage. C# files do not end with a newline. Member order is constants,
static fields, instance fields, constructors, properties, events, then methods.
`StyleCop.Analyzers` is enabled only for `SA1402`: one top-level type per file. Tests set that rule to none. Other StyleCop rules stay off; member order remains a review rule. `IDE0005` is an error, so an unused `using` fails the build. `NU1510` is an error for a direct package the .NET 10 SDK already supplies. A `PackageVersion` with no `PackageReference` fails the architecture tests.

## Agent tool workflow

Coding agents change C# via **Serena MCP** (discovery *and* mutation), not whole-file patches:

| Intent | Tools |
|--------|--------|
| Orient in a file | `get_symbols_overview` (depth 1) before reading the whole file |
| Open a type/method | `find_symbol` with `include_body` only when you need the body |
| Impact of a change | `find_referencing_symbols` before rename/delete/signature edits |
| Text that is not a symbol | `search_for_pattern` (optionally scoped with `relative_path`) |
| Replace a method/class/interface | `replace_symbol_body` |
| Add a neighbor type/method | `insert_after_symbol` / `insert_before_symbol` |
| Rename across references | `rename_symbol` (prefer this over manual search-replace) |
| Remove a whole symbol | `safe_delete_symbol` |
| Small in-method / regex edit | `replace_content` when replacing the entire symbol would be heavier |
| Durable agent facts | `list_memories` / `read_memory`; `write_memory` / `edit_memory` only for stable invariants + pointers — never version pins or command lists |

Activate/onboard the `Dilcore.MongoDB` project if Serena is not already on this repo. Prefer symbols over whole-file reads; prefer Serena edits over harness patches for C# in `src/` and `test/`. After a public rename/delete, update PublicAPI baselines and docs in the same change. Do not use Serena to rewrite Markdown, YAML, `.csproj`, or `PublicAPI.*.txt` — those stay with harness file tools. Never commit `.serena/project.local.yml`.

Other tools:

1. **SubAgents** — parallel bounded explores (architecture vs tests vs docs vs CI). Return paths + evidence. Do not delegate the whole task.
2. **Harness** — Glob/Read for docs and configs; Shell for `dotnet` / `gh` (commands above).
3. **Context7 MCP** — `MongoDB.Driver`, FluentResults, Testcontainers, NUnit, Shouldly, CodeRabbit. Always `resolve-library-id` then `query-docs`.
4. **Microsoft Learn MCP** — .NET, `Microsoft.Extensions.DependencyInjection`, EditorConfig, `dotnet format`. Search first; fetch the page when you need depth.

## Pull requests

1. Fork or branch from the latest `main`.
2. Keep changes focused; match existing coding style and prefer reuse over
   duplication.
3. Update docs or PublicAPI baselines when you change public surface area.
4. Fill out the pull request template and link related issues.
5. Ensure required CI checks pass. Dependabot and other automation PRs still need
   human review — there is no unconditional automerge.

CODEOWNERS requests review from [@aytymchuk](https://github.com/aytymchuk).

## Dependency updates

- Dependabot opens weekly PRs for **NuGet** (`Directory.Packages.props` and
  related manifests) and **GitHub Actions** workflow `uses:` references.
- Major / breaking upgrades are left ungrouped so they can be reviewed alone.
- Arbitrary YAML keys and non-ecosystem configuration are **out of scope** for
  Dependabot and must be updated manually.
- Prefer central package management and the rules in
  [docs/policies/versioning-and-support.md](docs/policies/versioning-and-support.md).

## License

By contributing, you agree that your contributions are licensed under the
[MIT License](LICENSE).
