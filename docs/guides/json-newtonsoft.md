# Store JSON documents with Newtonsoft.Json

Persist documents without a CLR type as `JObject` or `JToken`. Package: `Dilcore.MongoDB.NewtonsoftJson` (with `Dilcore.MongoDB`). Do not reference it from an app that only uses System.Text.Json.

## Setup

```csharp
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.NewtonsoftJson;
using Microsoft.Extensions.DependencyInjection;

var connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")!;
var services = new ServiceCollection();

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("app", db => db.OnCluster("primary")));

// Must come after AddMongoDb, otherwise registration throws InvalidOperationException.
services.AddNewtonsoftJsonDocuments();
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.NewtonsoftJson;
using FluentResults;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public sealed class PayloadService(INewtonsoftJsonDocumentStoreFactory stores)
{
    public async Task<Result<JObject>> RoundTripAsync(string json, CancellationToken cancellationToken = default)
    {
        var store = stores.Create(new MongoDatabaseKey("app"));

        // Keep date-like strings and high-precision decimals intact while parsing.
        using var reader = new JsonTextReader(new StringReader(json))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal
        };
        JObject payload;
        try
        {
            payload = JObject.Load(reader);
            if (reader.Read() && reader.TokenType != JsonToken.Comment)
            {
                return Result.Fail<JObject>(new JsonConversionError(
                    "Additional text encountered after finished reading JSON content."));
            }
        }
        catch (JsonReaderException exception)
        {
            return Result.Fail<JObject>(new JsonConversionError(exception.Message));
        }

        var inserted = await store.InsertAsync("payloads", payload, cancellationToken: cancellationToken);
        if (inserted.IsFailed)
        {
            return inserted.ToResult();
        }

        // Reads return Canonical Extended JSON by default. Ask for relaxed output to get plain JSON back.
        var relaxed = new JsonBsonConversionOptions { OutputMode = JsonBsonOutputMode.RelaxedExtendedJson };

        return await store.GetJObjectByIdAsync("payloads", inserted.Value["_id"], relaxed, cancellationToken);
    }
}
```

Also available: `InsertAsync(JToken | string | BsonDocument)`, `GetJTokenByIdAsync` and `ToJToken` / `ToJObject` on BSON values. Output is parsed with `DateParseHandling.None` and `FloatParseHandling.Decimal`.

## Handle failures

```csharp
using Dilcore.MongoDB.Abstractions.Results;

var inserted = await store.InsertAsync("payloads", payload, cancellationToken: cancellationToken);
if (inserted.HasError<DuplicateKeyError>())
{
    // A document with this _id already exists.
}
```

The cases are the same as for [System.Text.Json](json-system-text-json.md): `JsonConversionError` for malformed JSON, `DocumentTooLargeError`, `DocumentNotFoundError`, `DuplicateKeyError`, and an `InvalidOperationException` at registration when `AddMongoDb` was not called first. `JsonSerializerSettings.TypeNameHandling` other than `None` is rejected only when those settings are passed into token conversion (`InsertAsync` or `ToBson`). That check cannot see JSON the caller already deserialized. Callers must use `TypeNameHandling.None` for their own deserialization.

## Pitfalls

- Never enable `TypeNameHandling` for untrusted input. The adapter rejects it only for settings supplied to token conversion, so use `TypeNameHandling.None` when you deserialize yourself.
- Call `AddNewtonsoftJsonDocuments()` after `AddMongoDb`.
- Parse with `DateParseHandling.None` (and `FloatParseHandling.Decimal` for money) before inserting, or dates and decimals can change type.
- Use Canonical Extended JSON when BSON type fidelity matters: [JSON conversion options](json-conversion-options.md).

## See also

- [Store JSON documents with System.Text.Json](json-system-text-json.md)
- [JSON conversion options](json-conversion-options.md)
- [Production MongoDB posture](../security/production-mongodb.md)
- Runnable example: [NewtonsoftJsonExampleTests](../../test/Json.IntegrationTests/Examples/NewtonsoftJsonExampleTests.cs)
