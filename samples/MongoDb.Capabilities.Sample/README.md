# Dilcore MongoDB capabilities sample

Commented WebAPI that exercises **current** shipped APIs: policies, provisioning (DryRun/Apply + custom step), keyset paging, bulk options, typed FluentResults errors, System.Text.Json adapters, multi-document transactions, and driver escape hatches.

For a shorter first-run host, use [`../MongoDb.WebApi.Sample`](../MongoDb.WebApi.Sample).

## Requirements

- Docker (Testcontainers starts `mongo:9.0` as a **replica set** so transactions work)
- .NET SDK 10.0.x

Standalone MongoDB cannot run multi-document transactions. This sample is not a production host pattern; inject a connection string instead of embedding Testcontainers.

Newtonsoft.Json adapters are documented in [json-newtonsoft.md](../../docs/guides/json-newtonsoft.md) and are not referenced here so System.Text.Json apps do not take a Newtonsoft dependency.

## Run

```bash
dotnet run --project samples/MongoDb.Capabilities.Sample
```

Open Swagger at `http://localhost:5250/swagger`. Request examples: `MongoDb.Capabilities.Sample.http`.

Guides: [index](../../docs/guides/README.md), [CRUD](../../docs/guides/crud-with-repositories.md), [provisioning](../../docs/guides/provision-indexes-and-ttl.md), [transactions](../../docs/guides/transactions.md), [JSON](../../docs/guides/json-system-text-json.md), [documents](../../docs/guides/define-documents.md).
