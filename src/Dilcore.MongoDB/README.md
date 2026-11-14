# Dilcore.MongoDB

[![NuGet](https://img.shields.io/nuget/vpre/Dilcore.MongoDB?logo=nuget&label=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB)
[![Downloads](https://img.shields.io/nuget/dt/Dilcore.MongoDB?logo=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB)
[![CI](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/github/license/Dilcore-Official/Dilcore-MongoDb)](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/LICENSE)

An opinionated **MongoDB application toolkit for .NET**. It gives every service the same validated MongoDB setup, tenant-aware collection naming, reusable document policies, and repositories that return `Result<T>`, and it keeps the official `MongoDB.Driver` one call away.

> **Preview.** Version 2 is in preview. The public API can still change before `2.0.0`. Planned work (streaming, observability, vector search) is on the [roadmap](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/ROADMAP.md) and is **not** in these packages yet.

## Install

```bash
dotnet add package Dilcore.MongoDB --prerelease
```

Targets .NET 10. Tested against MongoDB 7.0, 8.0 and 9.0.

## Quick start

Describe a document. Policies are opt-in interfaces: add only the ones you need.

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
```

Register the cluster, database and binding once at startup:

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

Create indexes and collections explicitly, outside the request path:

```csharp
using var scope = app.Services.CreateScope();
var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
await provisioner.ApplyAsync();
```

Inject the repository and use it. Failures come back as typed errors, not exceptions:

```csharp
public sealed class OrderService(IGenericRepository<Order> orders)
{
    public Task<Result<Order>> CreateAsync(Order order, CancellationToken cancellationToken)
        => orders.StoreAsync(order, cancellationToken);

    public Task<Result<Order>> FindAsync(Guid id, CancellationToken cancellationToken)
        => orders.GetAsync(Builders<Order>.Filter.Eq(x => x.Id, id), cancellationToken);
}
```

A write with a stale `ETag` fails with `ConcurrencyConflictError`; a duplicate key fails with `DuplicateKeyError`.

## What you get

- **Multi-cluster, multi-database DI** that is validated at startup, with one `IMongoClient` per unique cluster settings.
- **Tenant-aware namespaces.** Your app supplies an `INamespacePrefixResolver`; a missing prefix fails closed. There are no first-class tenant types.
- **Document policies:** optimistic concurrency (`ETag`), audit timestamps, and soft delete with restore and purge.
- **Repositories** for generic, bulk and projection access, keyset paging, replace, snapshot and patch.
- **Explicit provisioning** of indexes and TTL with `DryRunAsync` and `ApplyAsync`.
- **Multi-document transactions** through `IMongoDbTransactionRunner` (replica set required), with client-side size budgets.
- **Process-wide BSON conventions** configured once on `AddMongoDb`.
- **Direct driver access.** Inject the keyed `IMongoClient`, `IMongoDatabase` or `IMongoDbCollectionFactory` whenever the toolkit is not enough.

## When not to use it

This is not a replacement for `MongoDB.Driver`, not Amazon DocumentDB, and not a provider-neutral repository. If your app needs one client and a few collection calls, use the driver directly.

## Optional packages

- [`Dilcore.MongoDB.SystemTextJson`](https://www.nuget.org/packages/Dilcore.MongoDB.SystemTextJson) and [`Dilcore.MongoDB.NewtonsoftJson`](https://www.nuget.org/packages/Dilcore.MongoDB.NewtonsoftJson) add JSON document stores that use the same database and namespace resolution.
- [`Dilcore.MongoDB.Abstractions`](https://www.nuget.org/packages/Dilcore.MongoDB.Abstractions) is already a dependency; reference it alone from libraries that must not take a DI host.

## Guides

| Topic | Guide |
|-------|-------|
| Repositories, paging, typed errors | [repositories](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/repositories.md) |
| Document policies and identifiers | [document policies](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/document-policies.md) |
| Namespace resolution and tenancy | [namespace resolution](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/namespace-resolution.md) |
| Provisioning, indexes, TTL | [provisioning](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/provisioning.md) |
| Transactions and budgets | [transactions](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/transactions.md) |
| BSON conventions | [ADR 0003](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/adr/0003-serialization-conventions.md) |
| Production MongoDB posture | [production MongoDB](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/security/production-mongodb.md) |

Runnable samples: [`MongoDb.WebApi.Sample`](https://github.com/Dilcore-Official/Dilcore-MongoDb/tree/main/samples/MongoDb.WebApi.Sample) and [`MongoDb.Capabilities.Sample`](https://github.com/Dilcore-Official/Dilcore-MongoDb/tree/main/samples/MongoDb.Capabilities.Sample).

## Packages

All packages are released together with the same version.

| Package | Use it when |
|---------|-------------|
| [`Dilcore.MongoDB`](https://www.nuget.org/packages/Dilcore.MongoDB) | You build an application host: `AddMongoDb`, repositories, provisioning, transactions, conventions |
| [`Dilcore.MongoDB.Abstractions`](https://www.nuget.org/packages/Dilcore.MongoDB.Abstractions) | You write a library that only needs the contracts and no DI host |
| [`Dilcore.MongoDB.SystemTextJson`](https://www.nuget.org/packages/Dilcore.MongoDB.SystemTextJson) | You store or read JSON documents with System.Text.Json |
| [`Dilcore.MongoDB.NewtonsoftJson`](https://www.nuget.org/packages/Dilcore.MongoDB.NewtonsoftJson) | You store or read JSON documents with Newtonsoft.Json |

## Links

- [Source and issues](https://github.com/Dilcore-Official/Dilcore-MongoDb)
- [Roadmap](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/ROADMAP.md) and [changelog](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/CHANGELOG.md)
- [Versioning and support policy](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/policies/versioning-and-support.md)
- [Security policy](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/SECURITY.md)

Licensed under the [MIT License](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/LICENSE).
