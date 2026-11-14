# Dilcore.MongoDB.Abstractions

[![NuGet](https://img.shields.io/nuget/vpre/Dilcore.MongoDB.Abstractions?logo=nuget&label=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB.Abstractions)
[![Downloads](https://img.shields.io/nuget/dt/Dilcore.MongoDB.Abstractions?logo=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB.Abstractions)
[![CI](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/github/license/Dilcore-Official/Dilcore-MongoDb)](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/LICENSE)

The **contracts** of the Dilcore MongoDB toolkit: document and policy interfaces, repository interfaces, namespace resolution, provisioning and transaction contracts, and typed operation errors. It has no DI host wiring, so a library can define its data access against these types and leave hosting to the application.

> **Preview.** Version 2 is in preview. The public API can still change before `2.0.0`. Planned work (streaming, observability, vector search) is on the [roadmap](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/ROADMAP.md) and is **not** in these packages yet.

## Install

```bash
dotnet add package Dilcore.MongoDB.Abstractions --prerelease
```

Targets .NET 10. Depends on `MongoDB.Driver` and `FluentResults`.

Building an application? Reference [`Dilcore.MongoDB`](https://www.nuget.org/packages/Dilcore.MongoDB) instead; it brings this package along.

## What is inside

- **Documents:** `IDocumentEntity` and `IDocumentEntity<TId>` (Guid, ObjectId or your own identifier type).
- **Opt-in policies:** `IHasConcurrencyToken`, `ISoftDeletable`, `IAuditableDocument`.
- **Repositories:** `IGenericRepository<T>`, `IGenericBulkRepository<T>`, `IGenericProjectionRepository<T>`, all returning `FluentResults` results.
- **Namespace resolution:** `INamespaceResolver` and `INamespacePrefixResolver` for tenant-aware database and collection names.
- **Provisioning and transactions:** `IMongoDbProvisioner`, `IMongoDbTransactionRunner` and their options.
- **Typed errors:** `MongoOperationError` and its subtypes, such as `ConcurrencyConflictError` and `DuplicateKeyError`.
- **Driver escape hatches:** `IMongoDbCollectionFactory` and `IMongoDatabaseResolver`.

```csharp
public sealed class Note : IDocumentEntity<Guid>, IAuditableDocument
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string Text { get; set; } = "";
}

public sealed class NoteArchive(IGenericRepository<Note> notes)
{
    public Task<Result<IReadOnlyList<Note>>> ListAsync(CancellationToken cancellationToken)
        => notes.GetListAsync(cancellationToken);
}
```

The implementation and `AddMongoDb` live in `Dilcore.MongoDB`.

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
