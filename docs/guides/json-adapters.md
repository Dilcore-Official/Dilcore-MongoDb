# JSON adapters

**Current (M3).** Optional packages convert JSON ↔ BSON through `IBsonJsonConverter` and persist untyped documents with `JsonDocumentStore` using the same database / collection resolvers as typed documents.

System.Text.Json and Newtonsoft conversion live in optional packages. Runnable coverage: `test/Json.IntegrationTests`.

## Packages

| Package | Use when |
|---------|----------|
| `Dilcore.MongoDB` | Always: registers `IBsonJsonConverter` (`BsonJsonConverter`) |
| `Dilcore.MongoDB.SystemTextJson` | `JsonElement` / `JsonDocument` / `JsonNode` extensions |
| `Dilcore.MongoDB.NewtonsoftJson` | `JToken` / `JObject` extensions; **do not** reference from STJ-only apps |

`AddMongoDb` does **not** register `JsonDocumentStore`. Construct it:

```csharp
var store = new JsonDocumentStore(
    collectionFactory,
    converter,
    new MongoDatabaseKey("CapabilitiesDB"));
```

The logical collection name is a string (`"payloads"`). It does not require a typed document binding.

## Conversion options

```csharp
var options = new JsonBsonConversionOptions
{
    OutputMode = JsonBsonOutputMode.CanonicalExtendedJson,
    NumberMode = OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDouble,
    MaxDepth = 64,
    MaxUtf8Bytes = 16 * 1024 * 1024,
    AllowDuplicateNames = false
};
```

Use **Canonical Extended JSON** when you need BSON type fidelity (`$oid`, `$numberInt`, `$numberLong`, `$numberDecimal`, `$date`). Relaxed mode is easier to read and may widen numbers.

`IBsonJsonConverter`:

- `Parse(string json, options)` → `Result<BsonValue>`
- `ToJson(BsonValue value, options)` → `Result<string>`

## System.Text.Json

```csharp
using Dilcore.MongoDB.SystemTextJson;

var inserted = await store.InsertAsync("payloads", jsonElement, options);
var loaded = await store.GetJsonDocumentByIdAsync("payloads", id, options);
```

Also: `InsertAsync(JsonDocument | JsonNode)`, `GetJsonElementByIdAsync`, and `ToBson` / `ToJsonElement` / `ToJsonDocument` on DOM types via `JsonBsonConvert`.

## Newtonsoft.Json

```csharp
using Dilcore.MongoDB.NewtonsoftJson;

var inserted = await store.InsertAsync("payloads", jObject, options);
var loaded = await store.GetJObjectByIdAsync("payloads", id, options);
```

`JsonSerializerSettings.TypeNameHandling` other than `None` is rejected (`TypeNameHandling is prohibited for untrusted JSON input.`).

## Store methods on core

`JsonDocumentStore` also accepts a JSON **string** or a `BsonDocument`. Oversized documents fail with `DocumentTooLargeError`. Missing ids fail with `DocumentNotFoundError`.
