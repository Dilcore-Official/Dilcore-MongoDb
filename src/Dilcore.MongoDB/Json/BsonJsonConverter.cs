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

        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                AllowDuplicateProperties = options.AllowDuplicateNames,
                MaxDepth = options.MaxDepth
            });
        }
        catch (JsonException exception)
        {
            return Result.Fail(new Error(exception.Message));
        }

        try
        {
            using var reader = new JsonReader(json);
            var value = BsonSerializer.Deserialize<BsonValue>(reader);
            ApplyNumberMode(value, options.NumberMode);
            return Result.Ok(value);
        }
        catch (Exception exception) when (exception is FormatException or BsonSerializationException or BsonException)
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
        catch (Exception exception)
        {
            return Result.Fail(new Error(exception.Message));
        }
    }

    private static void ApplyNumberMode(BsonValue value, OrdinaryJsonNumberMode mode)
    {
        if (mode == OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDouble)
        {
            return;
        }

        Rewrite(value, mode);
    }

    private static BsonValue Rewrite(BsonValue value, OrdinaryJsonNumberMode mode)
    {
        switch (value.BsonType)
        {
            case BsonType.Document:
                var document = value.AsBsonDocument;
                foreach (var name in document.Names.ToList())
                {
                    document[name] = Rewrite(document[name], mode);
                }

                return document;
            case BsonType.Array:
                var array = value.AsBsonArray;
                for (var i = 0; i < array.Count; i++)
                {
                    array[i] = Rewrite(array[i], mode);
                }

                return array;
            case BsonType.Int32:
            case BsonType.Int64:
            case BsonType.Double:
            case BsonType.Decimal128:
                return mode switch
                {
                    OrdinaryJsonNumberMode.AlwaysDouble when value.BsonType != BsonType.Double
                        => new BsonDouble(value.ToDouble()),
                    OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDecimal128
                        when value.BsonType == BsonType.Double && value.AsDouble is var d
                             && Math.Abs(d % 1) < double.Epsilon
                        => new BsonDecimal128(Decimal128.Parse(d.ToString("G17"))),
                    _ => value
                };
            default:
                return value;
        }
    }
}
