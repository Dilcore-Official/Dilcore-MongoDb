# Dilcore MongoDB

[![CI](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/Dilcore-Official/Dilcore-MongoDb/graph/badge.svg?token=SZPZ8SWY8K)](https://codecov.io/gh/Dilcore-Official/Dilcore-MongoDb)
[![CodeRabbit Pull Request Reviews](https://img.shields.io/coderabbit/prs/github/Dilcore-Official/Dilcore-MongoDb?utm_source=oss&utm_medium=github&utm_campaign=Dilcore-Official%2FDilcore-MongoDb&labelColor=171717&color=FF570A&link=https%3A%2F%2Fcoderabbit.ai&label=CodeRabbit+Reviews)](https://coderabbit.ai)

An opinionated .NET **MongoDB application toolkit**: validated multi-cluster / multi-database DI, scoped tenant-aware namespace resolution, composable document policies, repository helpers with FluentResults, JSON adapters, budgeted transactions, and **direct `MongoDB.Driver` escape hatches**.

It is **not** a replacement for `MongoDB.Driver`, **not** Amazon DocumentDB, and **not** a provider-neutral repository. A simple app that needs one client and a few collection calls should use the driver directly.

> **v2 roadmap:** [ROADMAP.md](ROADMAP.md). Package selection: [docs/product/package-selection.md](docs/product/package-selection.md). Naming: [ADR 0001](docs/adr/0001-package-naming.md). How-tos: [docs/guides/](docs/guides/).

## Packages

| Package | Use when |
|---------|----------|
| `Dilcore.MongoDB.Abstractions` | Contracts only (libraries that must not take a DI host dependency) |
| `Dilcore.MongoDB` | Application host: `AddMongoDb`, repositories, provisioning, transactions, conventions |
| `Dilcore.MongoDB.SystemTextJson` | Optional System.Text.Json adapters (Canonical Extended JSON) |
| `Dilcore.MongoDB.NewtonsoftJson` | Optional Newtonsoft.Json adapters (never forced on STJ consumers) |

Pin versions in `Directory.Packages.props` (see [versioning and support](docs/policies/versioning-and-support.md)); do not copy numbers from this README.

OpenTelemetry and VectorData package IDs are **planned**, not shipped.

## Samples

| Sample | Role |
|--------|------|
| [`samples/MongoDb.WebApi.Sample`](samples/MongoDb.WebApi.Sample) | Getting started: standalone Testcontainers, two bindings, CRUD, `ApplyAsync` |
| [`samples/MongoDb.Capabilities.Sample`](samples/MongoDb.Capabilities.Sample) | Replica-set catalog: transactions, JSON, keyset paging, restore/purge, typed errors, bulk options, DryRun, escape hatches |

Production hosts should inject a connection string. Embedding Testcontainers in the host is a demo only.

## Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Application Layer                        │
│  Generic / bulk / projection repos · JSON store · tx runner │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                  Infrastructure Layer                       │
│  AddMongoDb · IMongoDbCollectionFactory                     │
│  IMongoDatabaseResolver · keyed IMongoClient                │
│  IMongoDbProvisioner                                        │
└─────────────────────────────────────────────────────────────┘
┌─────────────────────────────────────────────────────────────┐
│                   Abstractions Layer                        │
│  IDocumentEntity<TId> · policies · repository interfaces    │
│  namespace resolution · typed operation errors              │
└─────────────────────────────────────────────────────────────┘
```

## Key features (current)

- Validated multi-cluster / multi-database DI; singleton `IMongoClient` per unique cluster settings
- Scoped namespace resolution via app-owned prefix resolvers (no first-class tenant APIs)
- Composable policies: optimistic concurrency, audit timestamps, optional soft delete
- Generic, bulk, and projection repositories with FluentResults and typed `MongoOperationError`s
- Keyset paging (`GetPageAsync`), restore/purge, replace / snapshot / patch
- Index and TTL provisioning with `IMongoDbProvisioner.DryRunAsync` / `ApplyAsync` (not on collection resolve)
- Optional JSON adapters and `JsonDocumentStore` (same resolvers as typed documents)
- Multi-document transactions via `IMongoDbTransactionRunner` (replica set; client-side budget estimates)
- Direct keyed `IMongoClient` / `IMongoDatabase` / `IMongoDbCollectionFactory` escape hatches

**Planned (not in these packages):** streaming lifecycle (M4), exporter-neutral observability (M6), Vector Search (M7).

## Getting started

Minimal single-binding host (count this snippet against the ≤15-statement setup budget):

```csharp
services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("MyDatabase", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<MyEntity>("myEntities", d => d
            .WithCollectionName("myEntities"));
    }));
```

```csharp
public class MyEntity : IDocumentEntity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}
```

```csharp
public class MyService(IGenericRepository<MyEntity> repository)
{
    public Task<Result<MyEntity>> CreateAsync(MyEntity entity)
        => repository.StoreAsync(entity);
}
```

Entities, policies, and Guid v7: [document-policies.md](docs/guides/document-policies.md). Full repository surface: [repositories.md](docs/guides/repositories.md).

### Basic setup (from `samples/MongoDb.WebApi.Sample`)

```csharp
var mongoDbContainer = new MongoDbBuilder("mongo:7.0").Build();
await mongoDbContainer.StartAsync();
var connectionString = mongoDbContainer.GetConnectionString();

builder.Services.AddMongoDb(mongo => mongo
    .ConfigureConventions(c => c.UseEnumRepresentation(BsonType.Int32))
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("SampleDB", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<WeatherForecast>("weather", d => d
            .WithCollectionName("weatherForecasts")
            .WithSoftDelete()
            .WithBulkRepository()
            .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7)
            .WithIndexes(new CreateIndexModel<WeatherForecast>(
                Builders<WeatherForecast>.IndexKeys.Ascending(x => x.Date),
                new CreateIndexOptions { Name = "weather_date" })));
        db.AddDocumentBinding<Note>("notes", d => d
            .WithCollectionName("notes"));
    }));

using var scope = app.Services.CreateScope();
var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
var provisioned = await provisioner.ApplyAsync();
```

`WeatherForecast` composes concurrency + soft delete + audit. `Note` is identifier-only (`Note(string Text, NotePriority Priority)`); enum BSON Int32 comes from `ConfigureConventions`. Indexes use `CreateIndexModel<T>[]`.

## Multi-database registration

```csharp
services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("UserDB", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<User>("users", d => d.WithCollectionName("users"));
    })
    .AddDatabase("ProductDB", db =>
    {
        db.OnCluster("primary");
        db.WithNamespacePrefix("catalog");
        db.AddDocumentBinding<Product>("products", d => d
            .WithCollectionName("products")
            .WithBulkRepository()
            .WithProjectionRepository());
    }));
```

Tenancy / prefixes: [namespace-resolution.md](docs/guides/namespace-resolution.md).

## Serialization conventions

BSON conventions are **process-wide** (MongoDB.Driver's `ConventionRegistry`) and are registered once during `AddMongoDb`. Unconfigured consumers keep these defaults:

- enums as strings (`BsonType.String`)
- camelCase element names
- ignore null members
- ignore extra elements on deserialize

Override them with `ConfigureConventions`. Calling it more than once on the same builder throws. A later `AddMongoDb` in the same process with different settings also throws; identical settings are idempotent only when additional custom conventions, packs, and filters are the same instances (or have real value equality). Separately constructed custom conventions with equivalent intent still conflict.

> **Changing conventions after data exists?** See [ADR 0003 – Rollout guidance](docs/adr/0003-serialization-conventions.md#rollout-guidance).

```csharp
services.AddMongoDb(mongo => mongo
    .ConfigureConventions(conventions => conventions
        .UseEnumRepresentation(BsonType.Int32)
        .UseElementNameConvention(new CamelCaseElementNameConvention())
        .IgnoreIfNull(true)
        .IgnoreExtraElements(true)
        .AddConvention(new IgnoreIfDefaultConvention(true))
        .AddConventionPack("orders-only", new ConventionPack(), type => type == typeof(Order)))
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("app", db =>
    {
        db.OnCluster("primary");
        db.AddDocumentBinding<Order>("orders", d => d.WithCollectionName("orders"));
    }));
```

Decision: [ADR 0003](docs/adr/0003-serialization-conventions.md).

## Guides

| Topic | Doc |
|-------|-----|
| Repositories, paging, typed errors | [docs/guides/repositories.md](docs/guides/repositories.md) |
| Provisioning, indexes, TTL, custom steps | [docs/guides/provisioning.md](docs/guides/provisioning.md) |
| Transactions and budgets | [docs/guides/transactions.md](docs/guides/transactions.md) |
| JSON adapters | [docs/guides/json-adapters.md](docs/guides/json-adapters.md) |
| Document policies / ObjectId | [docs/guides/document-policies.md](docs/guides/document-policies.md) |
| Namespace resolution | [docs/guides/namespace-resolution.md](docs/guides/namespace-resolution.md) |
| Driver escape hatches | [docs/product/driver-escape-hatches.md](docs/product/driver-escape-hatches.md) |
| Production MongoDB posture | [docs/security/production-mongodb.md](docs/security/production-mongodb.md) |

Binding knobs live on `IMongoDocumentBindingBuilder<T>`: `WithCollectionName`, `WithSoftDelete`, `WithGuidIdGeneration`, `WithBulkRepository`, `WithProjectionRepository`, `WithNamespacePrefix` / `WithNamespacePrefixResolver<T>`, `WithIndexes(params CreateIndexModel<T>[])`, `WithCollectionItemsTimeToLive`.

## Testing

Test layout and commands: [CONTRIBUTING.md](CONTRIBUTING.md). Architecture tests (no Docker), unit tests, DI acceptance (`test/IntegrationTests`), repository behavior on real MongoDB (`test/Repositories.IntegrationTests`), JSON fidelity (`test/Json.IntegrationTests`). Assertions: NUnit + **Shouldly**.

Benchmarks: `test/Benchmarks/Dilcore.MongoDB.Benchmarks`. Telemetry on/off overhead is **planned (M6)**.

```bash
dotnet run --project test/Benchmarks/Dilcore.MongoDB.Benchmarks -c Release -- --filter '*ColdStart*'
```

## Community and trust

| Document | Purpose |
|----------|---------|
| [CONTRIBUTING.md](CONTRIBUTING.md) | How to contribute, develop, and open pull requests |
| [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md) | Community standards |
| [SECURITY.md](SECURITY.md) | Private vulnerability reporting |
| [docs/security/production-mongodb.md](docs/security/production-mongodb.md) | Host TLS, secrets, redaction, least privilege, tenant isolation |
| [SUPPORT.md](SUPPORT.md) | Where to get help (Issues vs Discussions) |
| [GOVERNANCE.md](GOVERNANCE.md) | Maintainers and decision making |
| [CHANGELOG.md](CHANGELOG.md) | Notable changes |
| [LICENSE](LICENSE) | MIT |
