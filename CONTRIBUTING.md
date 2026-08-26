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
# Full suite:
dotnet test Dilcore.MongoDB.sln --configuration Release
# Performance benchmarks (needs Docker for repository/bulk/projection tiers):
dotnet run --project test/Benchmarks/Dilcore.MongoDB.Benchmarks -c Release -- --filter '*'
# Cold-start only (no Docker):
dotnet run --project test/Benchmarks/Dilcore.MongoDB.Benchmarks -c Release -- --filter '*ColdStart*'
```

Formatting is defined in [`.editorconfig`](.editorconfig). Prefer editor format-on-save
and verify changed C# without rewriting the tree:

```bash
dotnet format Dilcore.MongoDB.sln --verify-no-changes
```

Use **Shouldly** for assertions (not FluentAssertions). Architecture tests enforce the
two-package topology and dependency boundaries without Docker. DI acceptance tests
build the container with `ValidateScopes` / `ValidateOnBuild` via
`AcceptanceServiceProviderFactory`. CI runs a dedicated Architecture Tests job and a
DI Acceptance job (Docker preflight) as required checks alongside the full Build & Test job.
The **Benchmarks** workflow (`.github/workflows/benchmarks.yml`) runs BenchmarkDotNet on
PRs that touch `src/` or `test/Benchmarks/`, posts a non-blocking results comment, and
updates the historical baseline on `main` via `gh-pages`. Create an empty `gh-pages`
branch once if it does not exist yet (`git checkout --orphan gh-pages && git commit
--allow-empty -m "bench history" && git push -u origin gh-pages`).

Roadmap issue hygiene (needs `gh` + `jq`):

```bash
./scripts/verify-roadmap-coverage.sh
```

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
