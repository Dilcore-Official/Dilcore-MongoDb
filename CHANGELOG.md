# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html)
as described in [docs/policies/versioning-and-support.md](docs/policies/versioning-and-support.md).

## [Unreleased]

### Added

- Open-source trust foundation: MIT `LICENSE`, contribution and security policies,
  community templates, CODEOWNERS, Dependabot for NuGet and GitHub Actions, and
  security automation (CodeQL, dependency review, Scorecard, workflow validation).
- M3 repository correctness: typed `DocumentNotFoundError` / `ConcurrencyConflictError` /
  `DocumentTooLargeError` / `BulkWritePartialFailureError`, collision-safe ETags,
  replace/snapshot/patch, and bulk write options with chunking and partial-failure results.
- Consumer how-tos: [document policies](docs/guides/document-policies.md) and
  [repositories](docs/guides/repositories.md).
- Keyset pagination (`GetPageAsync`), soft-delete `RestoreAsync` / `PurgeAsync`,
  `FindOptions` list overloads, and typed `DuplicateKeyError` / `TransientWriteError` /
  `WriteConcernFailureError` mapping.
- Budgeted multi-document transactions via `IMongoDbTransactionRunner` over
  `WithTransactionAsync`. How-to: [transactions](docs/guides/transactions.md).
- Optional JSON adapter packages `Dilcore.MongoDB.SystemTextJson` and `Dilcore.MongoDB.NewtonsoftJson`
  with Canonical Extended JSON conversion and `JsonDocumentStore`.
  How-to: [json adapters](docs/guides/json-adapters.md).

## [1.0.0] - 2025

### Added

- Initial DocumentDB / MongoDB repository toolkit packages published under the
  v1 package identity (`Dilcore.DocumentDb.*`).
