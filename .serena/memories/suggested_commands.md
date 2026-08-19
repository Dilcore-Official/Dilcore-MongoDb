# Suggested commands

Authoritative command list: [CONTRIBUTING.md](../../CONTRIBUTING.md).

Test-suite intent:

- `test/UnitTests` and `test/ArchitectureTests` — fast; no Docker.
- `test/IntegrationTests` — DI acceptance / namespace / multi-cluster; Docker + Testcontainers.
- `test/Repositories.IntegrationTests` — repository behavior against real MongoDB; Docker.
- `test/Benchmarks` — performance; most filters need Docker (cold-start can run without).

Roadmap hygiene: `./scripts/verify-roadmap-coverage.sh` (needs `gh` + `jq`).
