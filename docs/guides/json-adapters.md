# JSON adapters

**Current (M3).** Optional packages convert JSON ↔ BSON through `IBsonJsonConverter` and persist untyped documents with `IJsonDocumentStore` using the same database / collection resolvers as typed documents.

System.Text.Json and Newtonsoft conversion live in optional packages. Runnable coverage: `test/Json.IntegrationTests`. Sample endpoints: `samples/MongoDb.WebApi.Sample` (`/json/*`, `/ns-json/*`). Catalog: [samples/MongoDb.Capabilities.Sample](../../samples/MongoDb.Capabilities.Sample) (`/json/payloads`). There is no Newtonsoft sample project.

## Packages

| Package | Use when |
|---------|----------|
| `Dilcore.MongoDB` | Always: registers `IBsonJsonConverter` (`BsonJsonConverter`) and `IJsonDocumentStoreFactory` |
| `Dilcore.MongoDB.SystemTextJson` | `JsonElement` / `JsonDocument` / `JsonNode` adapters + `AddSystemTextJsonDocuments()` |
| `Dilcore.MongoDB.NewtonsoftJson` | `JToken` / `JObject` adapters + `AddNewtonsoftJsonDocuments()`; **do not** reference from STJ-only apps |

`AddMongoDb` registers the stack-neutral store factory. Each JSON package adds its own facade registration and **fails closed** if `AddMongoDb` was not called first. Registration is idempotent (`TryAdd`).

```csharp
services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("CapabilitiesDB", db => db.OnCluster("primary")));

services.AddSystemTextJsonDocuments();
// or: services.AddNewtonsoftJsonDocuments();

using var scope = provider.CreateScope();
var store = scope.ServiceProvider
    .GetRequiredService<ISystemTextJsonDocumentStoreFactory>()
    .Create(new MongoDatabaseKey("CapabilitiesDB"));
```

You can also resolve `IJsonDocumentStoreFactory` from core and use the extension methods on `IJsonDocumentStore`.

The logical collection name is a string (`"payloads"`). It does not require a typed document binding. Optional `staticPrefix` flows through the same `INamespaceResolver` pipeline as `BsonDocumentRepository`.

## Conversion options

```csharp
var options = new JsonBsonConversionOptions
{
    OutputMode = JsonBsonOutputMode.CanonicalExtendedJson,
    NumberMode = OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDouble,
    MaxDepth = 64,
    MaxUtf8Bytes = 16 * 1024 * 1024,
    AllowDuplicateNames = false,
    InterpretExtendedJson = true
};
```

Use **Canonical Extended JSON** when you need BSON type fidelity (`$oid`, `$numberInt`, `$numberLong`, `$numberDecimal`, `$date`). Relaxed mode is easier to read and may widen numbers.

Ordinary JSON numbers are typed from the raw token text (Int32 → Int64 → Double or Decimal128 per `NumberMode`). Explicit Extended JSON wrappers are **not** rewritten by `NumberMode`. Nested objects and arrays are walked recursively. Set `InterpretExtendedJson = false` to keep `$…` wrappers as plain documents.

`IBsonJsonConverter`:

- `Parse(string json, options)` → `Result<BsonValue>`
- `ToJson(BsonValue value, options)` → `Result<string>`

## System.Text.Json

```csharp
using Dilcore.MongoDB.SystemTextJson;

services.AddSystemTextJsonDocuments();
var store = storeFactory.Create(new MongoDatabaseKey("CapabilitiesDB"));

var inserted = await store.InsertAsync("payloads", jsonElement, options);
var loaded = await store.GetJsonDocumentByIdAsync("payloads", id, options);
```

Also: `InsertAsync(JsonDocument | JsonNode | string | BsonDocument)`, `GetJsonElementByIdAsync`, and `ToBson` / `ToJsonElement` / `ToJsonDocument` on DOM types via `JsonBsonConvert`.

## Newtonsoft.Json

```csharp
using Dilcore.MongoDB.NewtonsoftJson;

services.AddNewtonsoftJsonDocuments();
var store = storeFactory.Create(new MongoDatabaseKey("CapabilitiesDB"));

var inserted = await store.InsertAsync("payloads", jObject, options);
var loaded = await store.GetJObjectByIdAsync("payloads", id, options);
```

Load tokens with `DateParseHandling.None` and `FloatParseHandling.Decimal` when you need date-like strings and high-precision decimals to survive. `ToJToken` / `ToJObject` parse Extended JSON output with those same settings.

`JsonSerializerSettings.TypeNameHandling` other than `None` is rejected (`TypeNameHandling is prohibited for untrusted JSON input.`).

## Failures

| Case | Result |
|------|--------|
| Malformed JSON | `JsonConversionError` (`json_conversion`) |
| Oversized JSON | `DocumentTooLargeError` |
| Root not a document on insert | `"JSON root must be a document."` |
| Missing id | `DocumentNotFoundError` |
| Duplicate `_id` | `DuplicateKeyError` |
| Unknown database / missing required prefix | failed `Result` from the shared resolver pipeline |
| Adapter registration without `AddMongoDb` | `InvalidOperationException` at registration |
