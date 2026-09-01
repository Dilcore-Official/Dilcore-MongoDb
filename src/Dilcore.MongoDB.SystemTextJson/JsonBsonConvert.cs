using System.Text.Json;
using System.Text.Json.Nodes;
using Dilcore.MongoDB.Abstractions.Json;
using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.SystemTextJson;

public static class JsonBsonConvert
{
    public static Result<BsonValue> ToBson(
        this JsonElement element,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(converter);
        return converter.Parse(element.GetRawText(), options);
    }

    public static Result<BsonValue> ToBson(
        this JsonDocument document,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.RootElement.ToBson(converter, options);
    }

    public static Result<BsonValue> ToBson(
        this JsonNode? node,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(converter);
        if (node is null)
        {
            return Result.Ok<BsonValue>(BsonNull.Value);
        }

        return converter.Parse(node.ToJsonString(), options);
    }

    public static Result<JsonElement> ToJsonElement(
        this BsonValue value,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions? options = null)
    {
        var document = value.ToJsonDocument(converter, options);
        if (document.IsFailed)
        {
            return document.ToResult();
        }

        using (document.Value)
        {
            return Result.Ok(document.Value.RootElement.Clone());
        }
    }

    public static Result<JsonDocument> ToJsonDocument(
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

        return Result.Ok(JsonDocument.Parse(json.Value));
    }
}
