# Driver escape hatches

Dilcore MongoDB keeps `MongoDB.Driver` types visible. Use these recipes when repository helpers are not enough. This is **current** M3 guidance.

Runnable catalog: [samples/MongoDb.Capabilities.Sample](../../samples/MongoDb.Capabilities.Sample) (`/escape/client`, `/escape/collection`). Transactions how-to: [transactions.md](../guides/transactions.md).

## Clients, databases, collections

Resolve keyed `IMongoClient` / `IMongoDatabase` from DI, or `IMongoDbCollectionFactory` for the same namespace pipeline typed repositories use.

```csharp
var client = services.GetRequiredKeyedService<IMongoClient>("primary");
var collection = (await factory.GetCollectionAsync<Order>(new MongoDocumentBindingKey("orders"))).Value;
```

## Concerns, preference, retries

Set `WriteConcern`, `ReadConcern`, `ReadPreference`, retry, timeout, compression, and `applicationName` on `MongoClientSettings` when registering the cluster. Collection-level overrides use driver `CollectionNamespace` / `MongoCollectionSettings` on `GetCollection` after you resolve the database.

## Sessions and transactions

Prefer `IMongoDbTransactionRunner.WithTransactionAsync`. The callback’s `IMongoDbTransactionContext.Session` is the driver session. For operations Dilcore does not wrap (aggregation, GridFS, time series), use `Session` plus `IMongoCollection<T>` overloads that take `IClientSessionHandle`.

Do not start a second session inside the callback. Cross-cluster work is rejected before dispatch.

## Aggregation, cursors, GridFS, CSE, time series

These remain driver APIs:

- Aggregation: `collection.Aggregate().Match(...).ToListAsync()`
- Cursors: `collection.Find(filter).ToCursorAsync()` — dispose the cursor
- GridFS: `new GridFSBucket(database)`
- Client-side encryption: configure on `MongoClientSettings.AutoEncryptionOptions`
- Time series: create the collection in provisioning (`CreateCollectionOptions.TimeSeriesOptions`) or a custom `IProvisioningStep`

Do not hide these behind a provider-neutral abstraction in Dilcore.
