# Contributing

Thanks for contributing to **Dilcore MongoDB** (repository:
[Dilcore-Official/Dilcore-MongoDb](https://github.com/Dilcore-Official/Dilcore-MongoDb)).

Please read the [Code of Conduct](CODE_OF_CONDUCT.md) before participating.

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
- Docker (only for integration tests that use Testcontainers)

```bash
dotnet restore
dotnet build --configuration Release
dotnet test test/UnitTests --configuration Release
# Full suite (needs Docker):
dotnet test --configuration Release
```

Roadmap issue hygiene (needs `gh` + `jq`):

```bash
./scripts/verify-roadmap-coverage.sh
```

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
