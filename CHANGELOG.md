# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
as described in [docs/policies/versioning-and-support.md](docs/policies/versioning-and-support.md).

## [Unreleased]

### Added

- Per-package NuGet READMEs (`src/<package>/README.md`) with install command, key features, quick start and preview note, and a rewritten repository README. `test/PackageTests` checks that each package ships its own README.
- Package `Copyright` metadata that matches the `LICENSE` copyright line; `test/PackageTests` asserts it.
- Test suites for packed package contents and for an external caller that restores
  `Dilcore.MongoDB` from those packages and builds a container with `ValidateScopes`
  and `ValidateOnBuild`. Shared MongoDB fixture startup lives in `test/TestSupport`.
  NUnit tests use Arrange / Act / Assert comments. Shouldly stays the assertion library.
- Build quality gates: `global.json` (SDK 10.0 feature band), package lock files,
  public API analyzer baselines, XML documentation on the public surface, and
  Release warnings-as-errors. CI restores with `--locked-mode`, verifies
  `dotnet format`, and builds with `-warnaserror`.
- Strict DI lifetime tests for `AddMongoDb` registrations and the optional JSON store factories.

- Packaging for NuGet.org ([#30](https://github.com/Dilcore-Official/Dilcore-MongoDb/issues/30)): package
  validation, Source Link, `.snupkg` symbol packages, package icon, and `PackageTags` that match
  `docs/product/package-descriptions.md`. `test/PackageTests` asserts description, tags, license, repository,
  README, icon, and symbols on the packed packages.
- Release workflow with OIDC trusted publishing to NuGet.org. A merge into `main` that changes library code
  publishes a preview automatically; stable releases and dry runs are manual. All libraries ship together. Each run computes the next version from
  the git tags (`2.0.0-preview.N` for previews, `2.X.Y` for stable), tags the commit, and creates a GitHub
  Release. A `dry_run` input tests the OIDC login without tagging or publishing. See CONTRIBUTING.md, "Releasing".

### Changed

- `LICENSE` copyright holder is Arsen Tymchuk (MIT text unchanged).
- The blind patch-bump publish to GitHub Packages on every `src/**` push is replaced by the gated NuGet.org workflow above.
- The placeholder `Version` in `src/Directory.Build.props` is `2.0.0-preview.0` (local builds only).
- `IProvisioningStep.ExecuteAsync` takes `CancellationToken cancellationToken = default`.
- Protected `BaseMongoDbRepository.ExecuteAsync` overloads take `estimatedBytes` before
  `CancellationToken`, so the token is the last parameter. Positional callers must swap those arguments.
  C# sources do not end with a trailing newline.
- Unused `using` directives fail the build (`IDE0005`). Samples no longer reference
  `Microsoft.AspNetCore.OpenApi`; Swashbuckle is the sample OpenAPI host.

### Fixed

- Transaction budgets reserve rendered BSON size for document and bulk writes, reject negative
  estimates without integer wrap, and enforce the elapsed-time budget when the callback returns.
  Estimating a document before the client exists no longer breaks Guid serializer registration.
  Session-bound repository calls rethrow retryable transaction errors so the driver can retry.
- Repository writes honor BSON policy element names, ETag-guarded replace, assigned-id inserts
  without concurrency, typed delete misses, oversized-document `FormatException` mapping, and
  bulk unmatched-ETag / chunk-index / in-memory restore outcomes.

### Added

- Open-source trust foundation: MIT `LICENSE`, contribution and security policies,
  community templates, CODEOWNERS, Dependabot for NuGet and GitHub Actions, and
  security automation (CodeQL, dependency review, Scorecard, workflow validation).
- M3 repository correctness: typed `DocumentNotFoundError` / `ConcurrencyConflictError` /
  `DocumentTooLargeError` / `BulkWritePartialFailureError`, collision-safe ETags,
  replace/snapshot/patch, and bulk write options with chunking and partial-failure results.
- Consumer how-tos: [document policies](docs/guides/document-policies.md) and
  [repositories](docs/guides/repositories.md).
- Idempotent `IMongoDbProvisioner` (DryRun/Apply + `IProvisioningStep`) outside collection
  resolution. Desired-state index reconcile: create, recreate (drop+create on definition drift),
  and remove indexes dropped from bindings; documents and `_id_` are preserved.
  How-to: [provisioning](docs/guides/provisioning.md).
- Optional JSON adapter packages `Dilcore.MongoDB.SystemTextJson` and `Dilcore.MongoDB.NewtonsoftJson`
  with Canonical Extended JSON conversion and `JsonDocumentStore`.
  How-to: [json adapters](docs/guides/json-adapters.md).
- Keyset pagination (`GetPageAsync`) with sort-aware opaque cursors (implicit `_id`
  tie-breaker), soft-delete `RestoreAsync` / `PurgeAsync`, `FindOptions` list
  overloads, and typed `DuplicateKeyError` / `TransientWriteError` /
  `WriteConcernFailureError` mapping.
- Budgeted multi-document transactions via `IMongoDbTransactionRunner` over
  `WithTransactionAsync`. How-to: [transactions](docs/guides/transactions.md).
- Replica-set capabilities catalog sample, CI `integration-matrix` job (`TestCategory=M3Matrix`),
  and [production MongoDB security guidance](docs/security/production-mongodb.md).

## [1.0.0] - 2025

### Added

- Initial DocumentDB / MongoDB repository toolkit packages published under the
  v1 package identity (`Dilcore.DocumentDb.*`).
