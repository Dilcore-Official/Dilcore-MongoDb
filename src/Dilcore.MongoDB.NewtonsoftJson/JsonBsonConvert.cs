using System.Globalization;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Json;
using FluentResults;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Dilcore.MongoDB.NewtonsoftJson;

public static class JsonBsonConvert
{
    public static Result<BsonValue> ToBson(
        this JToken token,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions? options = null,
        JsonSerializerSettings? serializerSettings = null)
    {
        ArgumentNullException.ThrowIfNull(token);
        ArgumentNullException.ThrowIfNull(converter);
        if (serializerSettings is { TypeNameHandling: not TypeNameHandling.None })
        {
            return Result.Fail("TypeNameHandling is prohibited for untrusted JSON input.");
        }

        options ??= new JsonBsonConversionOptions();
        try
        {
            return Result.Ok(ConvertToken(token, converter, options, depth: 0));
        }
        catch (Exception exception) when (exception is JsonException or FormatException or BsonException
                                              or InvalidOperationException or OverflowException
                                              or ArgumentException)
        {
            return Result.Fail(new Error(exception.Message));
        }
    }

    public static Result<BsonValue> ToBson(
        this JObject document,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions? options = null,
        JsonSerializerSettings? serializerSettings = null)
        => ((JToken)document).ToBson(converter, options, serializerSettings);

    public static Result<JToken> ToJToken(
        this BsonValue value,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        ArgumentNullException.ThrowIfNull(converter);

        var json = converter.ToJson(value, options);
        if (json.IsFailed)
        {
            return json.ToResult();
        }

        try
        {
            using var reader = new JsonTextReader(new StringReader(json.Value))
            {
                DateParseHandling = DateParseHandling.None,
                FloatParseHandling = FloatParseHandling.Decimal
            };
            return Result.Ok(JToken.Load(reader));
        }
        catch (Exception exception)
        {
            return Result.Fail(new Error(exception.Message));
        }
    }

    public static Result<JObject> ToJObject(
        this BsonValue value,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions? options = null)
    {
        var token = value.ToJToken(converter, options);
        if (token.IsFailed)
        {
            return token.ToResult();
        }

        if (token.Value is not JObject document)
        {
            return Result.Fail<JObject>("JSON root must be a document.");
        }

        return Result.Ok(document);
    }

    private static BsonValue ConvertToken(
        JToken token,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions options,
        int depth)
    {
        if (depth > options.MaxDepth)
        {
            throw new JsonException($"JSON depth exceeds MaxDepth={options.MaxDepth}.");
        }

        return token.Type switch
        {
            JTokenType.Object => ConvertObject((JObject)token, converter, options, depth),
            JTokenType.Array => ConvertArray((JArray)token, converter, options, depth),
            JTokenType.Integer => ConvertInteger((JValue)token, options.NumberMode),
            JTokenType.Float => ConvertFloat((JValue)token, options.NumberMode),
            JTokenType.String => new BsonString(token.Value<string>()!),
            JTokenType.Boolean => token.Value<bool>() ? BsonBoolean.True : BsonBoolean.False,
            JTokenType.Null or JTokenType.Undefined => BsonNull.Value,
            JTokenType.Date => new BsonDateTime(token.Value<DateTime>()),
            JTokenType.Guid => new BsonBinaryData(token.Value<Guid>(), GuidRepresentation.Standard),
            JTokenType.Bytes => new BsonBinaryData(token.Value<byte[]>()!),
            JTokenType.Uri => new BsonString(token.Value<Uri>()!.ToString()),
            JTokenType.TimeSpan => new BsonString(token.Value<TimeSpan>().ToString()),
            _ => throw new JsonException($"Unsupported JSON token type '{token.Type}'.")
        };
    }

    private static BsonValue ConvertObject(
        JObject document,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions options,
        int depth)
    {
        if (options.InterpretExtendedJson && IsExtendedJsonDocument(document))
        {
            // Preserve Extended JSON fidelity via the shared converter on the exact fragment text.
            var fragment = document.ToString(Formatting.None);
            var parsed = converter.Parse(fragment, options);
            if (parsed.IsFailed)
            {
                throw new JsonException(string.Join("; ", parsed.Errors.Select(error => error.Message)));
            }

            return parsed.Value;
        }

        var bson = new BsonDocument();
        foreach (var property in document.Properties())
        {
            bson[property.Name] = ConvertToken(property.Value, converter, options, depth + 1);
        }

        return bson;
    }

    private static BsonValue ConvertArray(
        JArray array,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions options,
        int depth)
    {
        var bson = new BsonArray();
        foreach (var item in array)
        {
            bson.Add(ConvertToken(item, converter, options, depth + 1));
        }

        return bson;
    }

    private static bool IsExtendedJsonDocument(JObject document)
    {
        var count = 0;
        foreach (var property in document.Properties())
        {
            count++;
            if (!ExtendedJsonKeys.IsTypeMarker(property.Name))
            {
                return false;
            }
        }

        return count > 0;
    }

    private static BsonValue ConvertInteger(JValue value, OrdinaryJsonNumberMode mode)
    {
        // Prefer the raw token text when Newtonsoft kept it as a string-backed integer.
        if (value.Value is long or int or short or byte or sbyte or uint or ulong)
        {
            var raw = Convert.ToString(value.Value, CultureInfo.InvariantCulture)!;
            return BsonJsonConverter.ConvertOrdinaryNumber(raw, mode);
        }

        if (value.Value is decimal decimalValue)
        {
            return BsonJsonConverter.ConvertOrdinaryNumber(
                decimalValue.ToString(CultureInfo.InvariantCulture),
                mode);
        }

        return BsonJsonConverter.ConvertOrdinaryNumber(
            Convert.ToString(value.Value, CultureInfo.InvariantCulture)!,
            mode);
    }

    private static BsonValue ConvertFloat(JValue value, OrdinaryJsonNumberMode mode)
    {
        // FloatParseHandling.Decimal yields decimal; Double yields double. Preserve literal via invariant text.
        var raw = value.Value switch
        {
            decimal decimalValue => decimalValue.ToString(CultureInfo.InvariantCulture),
            double doubleValue => doubleValue.ToString("G17", CultureInfo.InvariantCulture),
            float floatValue => floatValue.ToString("G9", CultureInfo.InvariantCulture),
            _ => Convert.ToString(value.Value, CultureInfo.InvariantCulture)!
        };

        return BsonJsonConverter.ConvertOrdinaryNumber(raw, mode);
    }
}
