# Dilcore.MongoDB.SystemTextJson

[![NuGet](https://img.shields.io/nuget/vpre/Dilcore.MongoDB.SystemTextJson?logo=nuget&label=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB.SystemTextJson)
[![Downloads](https://img.shields.io/nuget/dt/Dilcore.MongoDB.SystemTextJson?logo=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB.SystemTextJson)
[![CI](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/github/license/Dilcore-Official/Dilcore-MongoDb)](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/LICENSE)

System.Text.Json adapters for the Dilcore MongoDB toolkit. Convert JSON to BSON and back without losing BSON types, and store untyped JSON documents through the same database and namespace resolution as your typed documents.

> **Preview.** Version 2 is in preview. The public API can still change before `2.0.0`. Planned work (streaming, observability, vector search) is on the [roadmap](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/ROADMAP.md) and is **not** in these packages yet.

## Install

```bash
dotnet add package Dilcore.MongoDB.SystemTextJson --prerelease
```

This package needs [`Dilcore.MongoDB`](https://www.nuget.org/packages/Dilcore.MongoDB) and an `AddMongoDb` call in your host. It fails at startup if `AddMongoDb` was not called first.

## Quick start

```csharp
using Dilcore.MongoDB.SystemTextJson;

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("app", db => db.OnCluster("primary")));

services.AddSystemTextJsonDocuments();
```

```csharp
using var scope = provider.CreateScope();
var store = scope.ServiceProvider
    .GetRequiredService<ISystemTextJsonDocumentStoreFactory>()
    .Create(new MongoDatabaseKey("app"));

var inserted = await store.InsertAsync("payloads", jsonElement);
var loaded = await store.GetJsonDocumentByIdAsync("payloads", id);
```

The collection name (`"payloads"`) is a plain string. It does not need a typed document binding, and it flows through the same namespace resolvers as typed documents.

## What you get

- **System.Text.Json types:** `JsonElement`, `JsonDocument` and `JsonNode` convert to and from BSON.
- **Canonical Extended JSON** output keeps BSON types (`$oid`, `$numberLong`, `$numberDecimal`, `$date`). Relaxed mode is also available.
- **Conversion limits** for depth and size, and a switch to reject duplicate property names.
- **Same pipeline as typed documents:** database keys, collection names and namespace prefixes behave the same.

## When to choose this package

Use one JSON adapter per application. Pick the one that matches the JSON stack you already use. `Dilcore.MongoDB` itself has no dependency on either stack, so System.Text.Json apps never pull in Newtonsoft.Json.

## Packages

All packages are released together with the same version.

| Package | Use it when |
|---------|-------------|
| [`Dilcore.MongoDB`](https://www.nuget.org/packages/Dilcore.MongoDB) | You build an application host: `AddMongoDb`, repositories, provisioning, transactions, conventions |
| [`Dilcore.MongoDB.Abstractions`](https://www.nuget.org/packages/Dilcore.MongoDB.Abstractions) | You write a library that only needs the contracts and no DI host |
| [`Dilcore.MongoDB.SystemTextJson`](https://www.nuget.org/packages/Dilcore.MongoDB.SystemTextJson) | You store or read JSON documents with System.Text.Json |
| [`Dilcore.MongoDB.NewtonsoftJson`](https://www.nuget.org/packages/Dilcore.MongoDB.NewtonsoftJson) | You store or read JSON documents with Newtonsoft.Json |

## Guide

[JSON adapters guide](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/guides/json-adapters.md): conversion options, number handling and persistence.

## Links

- [Source and issues](https://github.com/Dilcore-Official/Dilcore-MongoDb)
- [Roadmap](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/ROADMAP.md) and [changelog](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/CHANGELOG.md)
- [Versioning and support policy](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/docs/policies/versioning-and-support.md)
- [Security policy](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/SECURITY.md)

Licensed under the [MIT License](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/LICENSE).
