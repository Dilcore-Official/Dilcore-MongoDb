# Control JSON and BSON conversion

Choose how JSON maps to BSON types with `JsonBsonConversionOptions` and `IBsonJsonConverter`. Package: `Dilcore.MongoDB` (the converter is registered by `AddMongoDb`).

## Setup

```csharp
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

services.AddMongoDb(mongo => mongo
    .AddCluster("primary", c => c.UseConnectionString(connectionString))
    .AddDatabase("app", db => db.OnCluster("primary")));
```

## Use

```csharp
using Dilcore.MongoDB.Abstractions.Json;
using FluentResults;
using MongoDB.Bson;

public sealed class JsonRoundTrip(IBsonJsonConverter converter)
{
    public Result<string> Normalize(string json)
    {
        var options = new JsonBsonConversionOptions
        {
            // Canonical keeps BSON types: $oid, $numberInt, $numberLong, $numberDecimal, $date.
            OutputMode = JsonBsonOutputMode.CanonicalExtendedJson,
            NumberMode = OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDouble,
            MaxDepth = 64,
            MaxUtf8Bytes = 16 * 1024 * 1024,
            AllowDuplicateNames = false,
            InterpretExtendedJson = true
        };

        var parsed = converter.Parse(json, options);
        if (parsed.IsFailed)
            return parsed.ToResult();

        return converter.ToJson(parsed.Value, options);
    }
}
```

`IBsonJsonConverter.Parse(string, options)` returns `Result<BsonValue>` and `ToJson(BsonValue, options)` returns `Result<string>`. The options are also accepted by the JSON document stores: [System.Text.Json](json-system-text-json.md), [Newtonsoft.Json](json-newtonsoft.md).

- Ordinary JSON numbers are typed from the raw token text (Int32, then Int64, then Double or Decimal128 per `NumberMode`). Explicit Extended JSON wrappers are not rewritten by `NumberMode`.
- `InterpretExtendedJson = false` keeps `$...` wrappers as plain documents.
- Canonical Extended JSON is the default `OutputMode`. `JsonBsonOutputMode.RelaxedExtendedJson` is easier to read (plain numbers) and can widen numbers.

## Handle failures

```csharp
using Dilcore.MongoDB.Abstractions.Results;

var normalized = roundTrip.Normalize("{ not json");
if (normalized.HasError<JsonConversionError>())
{
    // Malformed JSON, nesting over MaxDepth, or duplicate names when they are not allowed.
}
```

A payload over `MaxUtf8Bytes` returns `DocumentTooLargeError`.

## Pitfalls

- Use Canonical Extended JSON when BSON type fidelity matters (`ObjectId`, `long`, `decimal`, dates).
- Keep `MaxDepth` and `MaxUtf8Bytes` for untrusted input. Do not raise them to "unlimited".
- Relaxed mode cannot preserve every BSON distinction.

## See also

- [Store JSON documents with System.Text.Json](json-system-text-json.md)
- [Store JSON documents with Newtonsoft.Json](json-newtonsoft.md)
- [Handle typed errors](handle-errors.md)
- Runnable example: [JsonConversionOptionsExampleTests](../../test/Json.IntegrationTests/Examples/JsonConversionOptionsExampleTests.cs)
