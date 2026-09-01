using Dilcore.MongoDB.Abstractions.Json;
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

        return converter.Parse(token.ToString(Formatting.None), options);
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

        return Result.Ok(JToken.Parse(json.Value));
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
}
