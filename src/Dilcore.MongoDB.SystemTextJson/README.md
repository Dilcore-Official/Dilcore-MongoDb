# Dilcore.MongoDB.SystemTextJson

[![NuGet](https://img.shields.io/nuget/vpre/Dilcore.MongoDB.SystemTextJson?logo=nuget&label=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB.SystemTextJson)
[![Downloads](https://img.shields.io/nuget/dt/Dilcore.MongoDB.SystemTextJson?logo=nuget)](https://www.nuget.org/packages/Dilcore.MongoDB.SystemTextJson)
[![CI](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Dilcore-Official/Dilcore-MongoDb/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/github/license/Dilcore-Official/Dilcore-MongoDb)](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/LICENSE)

System.Text.Json adapters for the Dilcore MongoDB toolkit. Convert JSON to BSON and back without losing BSON types, and store untyped JSON documents through the same database and namespace resolution as your typed documents.

> **Preview.** Version 2 is in preview, so the public API can still change before `2.0.0`. Planned work (streaming, observability, vector search) is on the [roadmap](https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/ROADMAP.md) and is **not** in these packages yet.

## Install

```bash
dotnet add package Dilcore.MongoDB.SystemTextJson --prerelease
```

This package needs [`Dilcore.MongoDB`](https://www.nuget.org/packages/Dilcore.MongoDB) and an `AddMongoDb` call in your host. `AddSystemTextJsonDocuments` fails at startup if `AddMongoDb` was not called first.

## Key features

- **System.Text.Json types.** `JsonElement`, `JsonDocument` and `JsonNode` convert to and from BSON; strings and `BsonDocument` can be inserted directly.
- **Document store.** `ISystemTextJsonDocumentStoreFactory.Create(databaseKey, staticPrefix)` returns a store that inserts and reads documents by logical collection name, with no typed binding required.
- **BSON type fidelity.** Canonical Extended JSON keeps `$oid`, `$numberLong`, `$numberDecimal` and `$date`; relaxed output is also available.
- **Conversion options.** `JsonBsonConversionOptions` controls output mode, number typing, maximum depth, maximum size, duplicate property names and Extended JSON interpretation.
- **Same pipeline as typed documents.** Database keys, collection names and namespace prefixes resolve the same way, so tenant prefixes apply to JSON too.
- **Safe registration.** `AddSystemTextJsonDocuments` is idempotent and fails closed without `AddMongoDb`.
- **Typed reads.** `GetJsonElementByIdAsync` and `GetJsonDocumentByIdAsync` return the stored document as System.Text.Json types.

## Quick start

```csharp
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Extensions;
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

using System.Text.Json;

using var json = JsonDocument.Parse("""{ "sku": "A-1", "qty": 2 }""");
var inserted = await store.InsertAsync("payloads", json.RootElement);
var loaded = await store.GetJsonElementByIdAsync("payloads", inserted.Value["_id"]);
```

Every call returns a FluentResults `Result`. The collection name (`"payloads"`) is a plain string and does not need a typed document binding.

## Which JSON package?

Use the adapter that matches the JSON stack you already use. `Dilcore.MongoDB` itself depends on neither, so System.Text.Json apps never pull in Newtonsoft.Json.

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
