# Driver escape hatches

Dilcore MongoDB keeps `MongoDB.Driver` types visible. Use these recipes when repository helpers are not enough. This is **current** M3 guidance.

Getting-started sample: [MongoDb.WebApi.Sample](../../samples/MongoDb.WebApi.Sample).

## Clients, databases, collections

Resolve keyed `IMongoClient` / `IMongoDatabase` from DI, or `IMongoDbCollectionFactory` for the same namespace pipeline typed repositories use.

```csharp
var client = services.GetRequiredKeyedService<IMongoClient>("primary");
var collection = (await factory.GetCollectionAsync<Order>(new MongoDocumentBindingKey("orders"))).Value;
```

## Concerns, preference, retries

Set `WriteConcern`, `ReadConcern`, `ReadPreference`, retry, timeout, compression, and `applicationName` on `MongoClientSettings` when registering the cluster. Collection-level overrides use driver `MongoCollectionSettings` on `GetCollection` after you resolve the database.

## Sessions

Use `IClientSessionHandle` from `IMongoClient.StartSession()` with the collection overloads that take a session. Dilcore repositories accept a session when resolved inside a later transaction runner; until then, pass the session on driver APIs directly.

Do not start a second session for work that must stay atomic with an existing session.

## Aggregation, cursors, GridFS, CSE, time series

These remain driver APIs:

- Aggregation: `collection.Aggregate().Match(...).ToListAsync()`
- Cursors: `collection.Find(filter).ToCursorAsync()` — dispose the cursor
- GridFS: `new GridFSBucket(database)`
- Client-side encryption: configure on `MongoClientSettings.AutoEncryptionOptions`
- Time series: `database.CreateCollection(..., new CreateCollectionOptions { TimeSeriesOptions = ... })`

Do not hide these behind a provider-neutral abstraction in Dilcore.
