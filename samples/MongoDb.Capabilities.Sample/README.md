# Dilcore MongoDB capabilities sample

Commented WebAPI that exercises **current** shipped APIs: policies, provisioning (DryRun/Apply + custom step), keyset paging, bulk options, typed FluentResults errors, System.Text.Json adapters, multi-document transactions, and driver escape hatches.

For a shorter first-run host, use [`../MongoDb.WebApi.Sample`](../MongoDb.WebApi.Sample).

## Requirements

- Docker (Testcontainers starts `mongo:7.0` as a **replica set** so transactions work)
- .NET SDK 10.0.x

Standalone MongoDB cannot run multi-document transactions. This sample is not a production host pattern; inject a connection string instead of embedding Testcontainers.

Newtonsoft.Json adapters are documented in [json-adapters.md](../../docs/guides/json-adapters.md) and are not referenced here so System.Text.Json apps do not take a Newtonsoft dependency.

## Run

```bash
dotnet run --project samples/MongoDb.Capabilities.Sample
```

Open Swagger at `http://localhost:5250/swagger`. Request examples: `MongoDb.Capabilities.Sample.http`.

Guides: [repositories](../../docs/guides/repositories.md), [provisioning](../../docs/guides/provisioning.md), [transactions](../../docs/guides/transactions.md), [JSON](../../docs/guides/json-adapters.md), [policies](../../docs/guides/document-policies.md).
