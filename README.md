# Dilcore MongoDB

[![NuGet](https://img.shields.io/nuget/vpre/Dilcore.MongoDB?logo=nuget&label=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB)
[![CI](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Dilcore-Official/Dilcore-MongoDb/graph/badge.svg?token=SZPZ8SWY8K)](https://codecov.io/gh/Dilcore-Official/Dilcore-MongoDb)
[![License: MIT](https://img.shields.io/github/license/Dilcore-Official/Dilcore-MongoDb)](LICENSE)
[![MongoDB 7.0](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/mongo-7.0.yml/badge.svg)](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/mongo-7.0.yml)
[![MongoDB 8.0](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/mongo-8.0.yml/badge.svg)](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/mongo-8.0.yml)
[![MongoDB 9.0](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/mongo-9.0.yml/badge.svg)](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/mongo-9.0.yml)

An opinionated **MongoDB application toolkit for .NET**. Every service gets the same validated multi-cluster and multi-database setup, tenant-aware namespace resolution, composable document policies, repositories with typed `FluentResults` errors, optional JSON adapters and budgeted transactions, while the official `MongoDB.Driver` stays one call away.

It is **not** a replacement for `MongoDB.Driver`, **not** Amazon DocumentDB, and **not** a provider-neutral repository. A simple app that needs one client and a few collection calls should use the driver directly.

> **Preview.** Version 2 is in preview, so the public API can still change before `2.0.0`. What is planned next (streaming, observability, vector search) is in [ROADMAP.md](ROADMAP.md) and is not in the packages yet.

## Packages

All packages are released together with the same version. Reference the first one unless you are writing a library that must stay free of a DI host.

| Package | Use it when |
|---------|-------------|
| [`Dilcore.MongoDB`](https://www.nuget.org/packages/Dilcore.MongoDB) | You build an application host: `AddMongoDb`, repositories, provisioning, transactions, conventions |
| [`Dilcore.MongoDB.Abstractions`](https://www.nuget.org/packages/Dilcore.MongoDB.Abstractions) | You write a library that only needs the contracts |
| [`Dilcore.MongoDB.SystemTextJson`](https://www.nuget.org/packages/Dilcore.MongoDB.SystemTextJson) | You store or read JSON documents with System.Text.Json |
| [`Dilcore.MongoDB.NewtonsoftJson`](https://www.nuget.org/packages/Dilcore.MongoDB.NewtonsoftJson) | You store or read JSON documents with Newtonsoft.Json (never forced on System.Text.Json apps) |

```bash
dotnet add package Dilcore.MongoDB --prerelease
```

Targets .NET 10. Package selection details: [docs/product/package-selection.md](docs/product/package-selection.md).

## Key features

- Validated multi-cluster / multi-database DI through one entry point, `AddMongoDb`; one singleton `IMongoClient` per unique cluster settings
- Scoped namespace resolution through app-owned prefix resolvers (no first-class tenant types); a missing prefix fails closed
- Composable policies: optimistic concurrency, audit timestamps, optional soft delete
- Generic, bulk and projection repositories with typed `MongoOperationError`s; keyset paging, replace, snapshot, patch, restore and purge
- Explicit index and TTL provisioning with `IMongoDbProvisioner.DryRunAsync` / `ApplyAsync`, never as a side effect of resolving a collection
- Multi-document transactions through `IMongoDbTransactionRunner` (replica set required, client-side size budgets)
- Process-wide BSON conventions configured once on `AddMongoDb`
- Optional JSON adapters and an untyped `IJsonDocumentStore` that uses the same resolvers as typed documents
- Direct keyed `IMongoClient`, `IMongoDatabase` and `IMongoDbCollectionFactory` escape hatches

## Quick start

A minimal single-database host. The same code with the usings is in the [`Dilcore.MongoDB` package README](src/Dilcore.MongoDB/README.md).

```csharp
services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("shop", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Order>("orders", d => d
            .WithCollectionName("orders")
            .WithSoftDelete()
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
    }));
```

```csharp
public sealed class Order : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
{
    public Guid Id { get; set; }
    public long ETag { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Sku { get; set; } = "";
}

public sealed class OrderService(IGenericRepository<Order> orders)
{
    public Task<Result<Order>> CreateAsync(Order order, CancellationToken cancellationToken)
        => orders.StoreAsync(order, cancellationToken);
}
```

Create indexes and collections explicitly at startup with `IMongoDbProvisioner.ApplyAsync()`. Production hosts should inject a connection string; the samples start MongoDB with Testcontainers for demo purposes only.

## AI coding assistants

Install the usage skill so your assistant picks the right package, follows the rules (typed errors, fail-closed tenancy, explicit provisioning, transaction scope) and starts from tested examples:

```bash
npx skills add https://github.com/Dilcore-Official/Dilcore-MongoDb --skill using-dilcore-mongodb
```

It is a standard [Agent Skills](https://agentskills.io) folder, so you can also copy [`skills/using-dilcore-mongodb`](skills/using-dilcore-mongodb) into your agent's skills directory by hand. The documentation is also available through [Context7](https://context7.com) as `/dilcore-official/dilcore-mongodb` once the library is indexed.

## Documentation

| Topic | Doc |
|-------|-----|
| All task guides (setup, repositories, tenancy, provisioning, transactions, JSON) | [docs/guides/README.md](docs/guides/README.md) |
| Choose a package | [docs/product/package-selection.md](docs/product/package-selection.md) |
| Driver escape hatches | [docs/product/driver-escape-hatches.md](docs/product/driver-escape-hatches.md) |
| BSON conventions and rollout | [ADR 0003](docs/adr/0003-serialization-conventions.md) |
| Production MongoDB posture (TLS, secrets, least privilege) | [docs/security/production-mongodb.md](docs/security/production-mongodb.md) |
| Versioning and support | [docs/policies/versioning-and-support.md](docs/policies/versioning-and-support.md) |
| Decisions | [docs/adr/](docs/adr/) |

Binding options live on `IMongoDocumentBindingBuilder<T>`: `WithCollectionName`, `WithSoftDelete`, `WithGuidIdGeneration`, `WithBulkRepository`, `WithProjectionRepository`, `WithNamespacePrefix` / `WithNamespacePrefixResolver<T>`, `WithIndexes`, `WithCollectionItemsTimeToLive`.

### BSON conventions

Conventions are **process-wide** (`MongoDB.Driver`'s `ConventionRegistry`) and are registered once during `AddMongoDb`. The defaults are string enums, camelCase element names, ignore null members and ignore extra elements. Override them with `ConfigureConventions`; configuring different settings twice in one process throws. See [ADR 0003](docs/adr/0003-serialization-conventions.md) before changing conventions on existing data.

## Samples

| Sample | Role |
|--------|------|
| [`samples/MongoDb.WebApi.Sample`](samples/MongoDb.WebApi.Sample) | Getting started: two bindings, CRUD, `ApplyAsync` |
| [`samples/MongoDb.Capabilities.Sample`](samples/MongoDb.Capabilities.Sample) | Replica-set catalog: transactions, JSON, keyset paging, restore and purge, typed errors, bulk options, dry run, escape hatches |

## Architecture

```text
Application      generic / bulk / projection repositories, JSON store, transaction runner
Infrastructure   AddMongoDb, IMongoDbCollectionFactory, IMongoDatabaseResolver,
                 keyed IMongoClient, IMongoDbProvisioner
Abstractions     IDocumentEntity<TId>, policies, repository interfaces,
                 namespace resolution, typed operation errors
```

## Compatibility

| | |
|---|---|
| .NET | 10 |
| MongoDB server | 7.0, 8.0, 9.0 (each has its own integration workflow, see the badges above) |
| Driver | `MongoDB.Driver`, version pinned in [`Directory.Packages.props`](Directory.Packages.props) |

## Contributing

Build, test and release instructions are in [CONTRIBUTING.md](CONTRIBUTING.md). Tests use NUnit and Shouldly; integration tests need Docker.

```bash
dotnet test
```

## Community and trust

| Document | Purpose |
|----------|---------|
| [CONTRIBUTING.md](CONTRIBUTING.md) | How to contribute, develop, release and open pull requests |
| [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) | Community standards |
| [SECURITY.md](SECURITY.md) | Private vulnerability reporting |
| [SUPPORT.md](SUPPORT.md) | Where to get help (Issues vs Discussions) |
| [GOVERNANCE.md](GOVERNANCE.md) | Maintainers and decision making |
| [ROADMAP.md](ROADMAP.md) | What is planned |
| [CHANGELOG.md](CHANGELOG.md) | Notable changes |
| [LICENSE](LICENSE) | MIT |
