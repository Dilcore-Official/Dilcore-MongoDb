# Dilcore.MongoDB

[![NuGet](https://img.shields.io/nuget/vpre/Dilcore.MongoDB?logo=nuget&label=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB)
[![Downloads](https://img.shields.io/nuget/dt/Dilcore.MongoDB?logo=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB)
[![CI](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/github/license/Dilcore-Official/Dilcore-MongoDb)](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/LICENSE)

An opinionated **MongoDB application toolkit for .NET**. Every service gets the same validated MongoDB setup, tenant-aware collection naming, reusable document policies and repositories that return `Result<T>`, and the official `MongoDB.Driver` stays one call away.

> **Preview.** Version 2 is in preview, so the public API can still change before `2.0.0`. Planned work (streaming, observability, vector search) is on the [roadmap](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/ROADMAP.md) and is **not** in these packages yet.

## Install

```bash
dotnet add package Dilcore.MongoDB --prerelease
```

Targets .NET 10. Tested against MongoDB 7.0, 8.0 and 9.0.

## Key features

- **Multi-cluster, multi-database DI.** A single `AddMongoDb` entry point, validated at startup, with one singleton `IMongoClient` per unique cluster settings.
- **Tenant-aware namespaces.** Your app supplies an `INamespacePrefixResolver`; a missing prefix fails closed. The library has no first-class tenant types.
- **Document policies.** Optimistic concurrency (`IHasConcurrencyToken`), audit timestamps (`IAuditableDocument`) and soft delete (`ISoftDeletable`) are opt-in interfaces you compose on your document.
- **Repositories with typed failures.** Generic, bulk and projection repositories return FluentResults; errors such as `ConcurrencyConflictError` and `DuplicateKeyError` are values, not exceptions. Keyset paging, replace, snapshot, patch, restore and purge are built in.
- **Explicit provisioning.** Indexes and TTL are created by `IMongoDbProvisioner.DryRunAsync` / `ApplyAsync`, never as a side effect of resolving a collection.
- **Multi-document transactions.** `IMongoDbTransactionRunner` coordinates driver sessions on a replica set and applies client-side size budgets.
- **Process-wide BSON conventions.** Configured once in `AddMongoDb` with `ConfigureConventions`; the defaults are string enums, camelCase names, ignore nulls and ignore extra elements.
- **JSON conversion.** `IBsonJsonConverter` and `IJsonDocumentStoreFactory` are registered for the optional JSON packages.
- **Direct driver access.** Resolve the keyed `IMongoClient`, `IMongoDatabase` or `IMongoDbCollectionFactory` whenever the toolkit is not enough.

## Quick start

Describe a document. Policies are opt-in interfaces, so add only the ones you need.

```csharp
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;

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
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Extensions;

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
using Dilcore.MongoDB.Abstractions.Provisioning;

using var scope = app.Services.CreateScope();
var provisioner = scope.ServiceProvider.GetRequiredService<IMongoDbProvisioner>();
await provisioner.ApplyAsync();
```

Inject the repository and use it. Failures come back as typed errors:

```csharp
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Abstractions.Results;
using FluentResults;
using MongoDB.Driver;

public sealed class OrderService(IGenericRepository<Order> orders)
{
    public async Task<Result<Order>> CreateAsync(Order order, CancellationToken cancellationToken)
    {
        var stored = await orders.StoreAsync(order, cancellationToken);

        if (stored.HasError<DuplicateKeyError>())
        {
            // An order with the same key already exists.
        }

        return stored;
    }

    public Task<Result<Order>> FindAsync(Guid id, CancellationToken cancellationToken)
        => orders.GetAsync(Builders<Order>.Filter.Eq(x => x.Id, id), cancellationToken);
}
```

A write with a stale `ETag` fails with `ConcurrencyConflictError`.

When the toolkit is not enough, use the driver directly:

```csharp
using MongoDB.Driver;

var client = provider.GetRequiredKeyedService<IMongoClient>("primary");
```

## When not to use it

This is not a replacement for `MongoDB.Driver`, not Amazon DocumentDB, and not a provider-neutral repository. If your app needs one client and a few collection calls, use the driver directly.

## AI coding assistants

Install the usage skill so your assistant picks the right package and follows the rules (typed errors, fail-closed tenancy, explicit provisioning, transaction scope):

```bash
npx skills add https://github.com/Dilcore-Official/Dilcore-MongoDb --skill using-dilcore-mongodb
```

It is a standard [Agent Skills](https://agentskills.io) folder: [`skills/using-dilcore-mongodb`](https://github.com/Dilcore-Official/Dilcore-MongoDb/tree/main/skills/using-dilcore-mongodb).

## Guides

| Topic | Guide |
|-------|-------|
| All task guides (setup, repositories, tenancy, provisioning, transactions, JSON) | [guides index](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/README.md) |
| Choose a package | [package selection](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/product/package-selection.md) |
| Driver escape hatches | [driver escape hatches](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/product/driver-escape-hatches.md) |
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
