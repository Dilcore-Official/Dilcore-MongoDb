# Test guide

Maintainer notes for the Dilcore MongoDB test suite. Consumer usage stays in the README and `docs/guides/`.

## Projects

| Project | What it locks | Docker |
|---------|----------------|--------|
| `UnitTests` | Conventions, extensions, cursors, exception mapping, DI lifetimes | No |
| `ArchitectureTests` | Package topology, dependency boundaries, public-API shape | No |
| `IntegrationTests` | DI acceptance and multi-cluster wiring on real MongoDB | Yes |
| `Repositories.IntegrationTests` | Repositories, policies, provisioning, transactions | Yes |
| `Json.IntegrationTests` | JSON adapter fidelity and persistence | Yes |
| `PackageTests` | Contents of the four packed `.nupkg` files | No |
| `ConsumerTests` | An external project that restores the packed `Dilcore.MongoDB` package and calls `AddMongoDb` | No |
| `TestSupport` | Shared fixtures. Not a test assembly | No |
| `Benchmarks` | BenchmarkDotNet. Not part of `dotnet test` acceptance | Sometimes |

`TestSupport` holds `BaseIntegrationTests`, `MongoTestImages`, `AcceptanceServiceProviderFactory`, `ResultAssertions`, `RepoLocator`, and `PackagePacker`. Integration projects reference it. Do not copy `BaseIntegrationTests` back into an integration project.

## Assertions and shape

Use NUnit and **Shouldly**. Do not add FluentAssertions.

Every `[Test]` method uses `// Arrange`, `// Act`, and `// Assert`. The rule, including when `// Act & Assert` is allowed, is in [csharp-testing-di](../.cursor/rules/csharp-testing-di.mdc).

DI lifetime tests build the provider with `ValidateScopes` and `ValidateOnBuild` through `AcceptanceServiceProviderFactory` (or the same options). Do not start MongoDB for lifetime tests.

## Package and consumer checks

`PackageTests` packs `src/` with a locked restore and opens each `.nupkg`. It expects the README, the `lib/net10.0` assembly, the XML docs file, the declared package dependencies, and no `PublicAPI` baselines or connection strings.

`ConsumerTests` packs the same way, writes a temporary project that references the package from that folder, and runs `AddMongoDb` with `ValidateScopes` and `ValidateOnBuild`. A placeholder connection string is enough. The test does not start a server.

Both run in the Architecture Tests job in [`.github/workflows/ci.yml`](../.github/workflows/ci.yml), and again in the unfiltered Build & Test job.

## What not to change

Benchmarks are not Arrange / Act / Assert tests. Samples are not the public API contract. Do not weaken repository Result, ETag, or soft-delete assertions to make a test pass.
