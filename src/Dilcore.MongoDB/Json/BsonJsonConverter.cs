using System.Globalization;
using System.Text;
using System.Text.Json;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Results;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Dilcore.MongoDB.Json;

public sealed class BsonJsonConverter : IBsonJsonConverter
{
    public Result<BsonValue> Parse(string json, JsonBsonConversionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(json);
        options ??= new JsonBsonConversionOptions();

        var utf8Bytes = Encoding.UTF8.GetByteCount(json);
        if (utf8Bytes > options.MaxUtf8Bytes)
        {
            return Result.Fail(new DocumentTooLargeError(
                $"JSON payload is {utf8Bytes} bytes, exceeding MaxUtf8Bytes={options.MaxUtf8Bytes}."));
        }

        if (options.MaxDepth < 0)
        {
            return Result.Fail(new Error("MaxDepth must be non-negative."));
        }

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowDuplicateProperties = options.AllowDuplicateNames,
                MaxDepth = options.MaxDepth
            });
            return Result.Ok(ConvertElement(document.RootElement, options, depth: 0));
        }
        catch (JsonException exception)
        {
            return Result.Fail(new Error(exception.Message));
        }
        catch (Exception exception) when (exception is FormatException or BsonSerializationException or BsonException
                                              or InvalidOperationException or OverflowException)
        {
            return Result.Fail(new Error(exception.Message));
        }
    }

    public Result<string> ToJson(BsonValue value, JsonBsonConversionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(value);
        options ??= new JsonBsonConversionOptions();
        var mode = options.OutputMode == JsonBsonOutputMode.RelaxedExtendedJson
            ? JsonOutputMode.RelaxedExtendedJson
            : JsonOutputMode.CanonicalExtendedJson;

        try
        {
            return Result.Ok(value.ToJson(new JsonWriterSettings { OutputMode = mode }));
        }
        catch (Exception exception) when (exception is FormatException or BsonSerializationException or BsonException
                                              or InvalidOperationException or ArgumentException)
        {
            return Result.Fail(new Error(exception.Message));
        }
    }

    /// <summary>
    /// Maps an ordinary JSON number token (not an Extended JSON wrapper) using <paramref name="mode"/>.
    /// </summary>
    public static BsonValue ConvertOrdinaryNumber(string rawToken, OrdinaryJsonNumberMode mode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawToken);

        var hasFractionOrExponent = rawToken.Contains('.')
                                    || rawToken.Contains('e', StringComparison.OrdinalIgnoreCase);

        return mode switch
        {
            OrdinaryJsonNumberMode.AlwaysDouble =>
                new BsonDouble(double.Parse(rawToken, CultureInfo.InvariantCulture)),
            OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDecimal128 =>
                ConvertPreferIntegerThenDecimal128(rawToken, hasFractionOrExponent),
            _ => ConvertPreferIntegerThenDouble(rawToken, hasFractionOrExponent)
        };
    }

    private static BsonValue ConvertElement(JsonElement element, JsonBsonConversionOptions options, int depth)
    {
        if (depth > options.MaxDepth)
        {
            throw new JsonException($"JSON depth exceeds MaxDepth={options.MaxDepth}.");
        }

        return element.ValueKind switch
        {
            JsonValueKind.Object => ConvertObject(element, options, depth),
            JsonValueKind.Array => ConvertArray(element, options, depth),
            JsonValueKind.String => new BsonString(element.GetString()!),
            JsonValueKind.Number => ConvertOrdinaryNumber(element.GetRawText(), options.NumberMode),
            JsonValueKind.True => BsonBoolean.True,
            JsonValueKind.False => BsonBoolean.False,
            JsonValueKind.Null => BsonNull.Value,
            JsonValueKind.Undefined => BsonUndefined.Value,
            _ => throw new JsonException($"Unsupported JSON value kind '{element.ValueKind}'.")
        };
    }

    private static BsonValue ConvertObject(JsonElement element, JsonBsonConversionOptions options, int depth)
    {
        if (options.InterpretExtendedJson && IsExtendedJsonDocument(element))
        {
            return ParseExtendedJsonFragment(element.GetRawText());
        }

        var document = new BsonDocument();
        foreach (var property in element.EnumerateObject())
        {
            // Indexer replaces when AllowDuplicateNames let JsonDocument keep multiple same-named properties.
            document[property.Name] = ConvertElement(property.Value, options, depth + 1);
        }

        return document;
    }

    private static BsonValue ConvertArray(JsonElement element, JsonBsonConversionOptions options, int depth)
    {
        var array = new BsonArray();
        foreach (var item in element.EnumerateArray())
        {
            array.Add(ConvertElement(item, options, depth + 1));
        }

        return array;
    }

    private static bool IsExtendedJsonDocument(JsonElement element)
    {
        var count = 0;
        foreach (var property in element.EnumerateObject())
        {
            count++;
            if (!ExtendedJsonKeys.IsTypeMarker(property.Name))
            {
                return false;
            }
        }

        return count > 0;
    }

    private static BsonValue ParseExtendedJsonFragment(string json)
    {
        using var reader = new JsonReader(json);
        return BsonSerializer.Deserialize<BsonValue>(reader);
    }

    private static BsonValue ConvertPreferIntegerThenDouble(string rawToken, bool hasFractionOrExponent)
    {
        if (!hasFractionOrExponent)
        {
            if (int.TryParse(rawToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var int32))
            {
                return new BsonInt32(int32);
            }

            if (long.TryParse(rawToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var int64))
            {
                return new BsonInt64(int64);
            }
        }

        return new BsonDouble(double.Parse(rawToken, CultureInfo.InvariantCulture));
    }

    private static BsonValue ConvertPreferIntegerThenDecimal128(string rawToken, bool hasFractionOrExponent)
    {
        if (!hasFractionOrExponent)
        {
            if (int.TryParse(rawToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var int32))
            {
                return new BsonInt32(int32);
            }

            if (long.TryParse(rawToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out var int64))
            {
                return new BsonInt64(int64);
            }
        }

        return new BsonDecimal128(Decimal128.Parse(rawToken));
    }
}
