using System.Text.Json;
using System.Text.Json.Nodes;
using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Json;
using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.SystemTextJson;

/// <summary>
/// Converts between System.Text.Json values and BSON.
/// </summary>
public static class JsonBsonConvert
{
    /// <summary>
    /// Converts a <see cref="JsonElement"/> to BSON by parsing its raw JSON text through <see cref="IBsonJsonConverter"/>.
    /// </summary>
    /// <param name="element">The element to convert.</param>
    /// <param name="converter">Shared converter that parses JSON text.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <returns>A converted <see cref="BsonValue"/>, or a conversion error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="converter"/> is null.</exception>
    public static Result<BsonValue> ToBson(
        this JsonElement element,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(converter);
        // Structural path: reuse converter.Parse on the exact raw text so number tokens and
        // Extended JSON wrappers keep the same fidelity as string Parse.
        return converter.Parse(element.GetRawText(), options);
    }

    /// <summary>
    /// Converts a <see cref="JsonDocument"/> root element to BSON.
    /// </summary>
    /// <param name="document">The document to convert.</param>
    /// <param name="converter">Shared converter that parses JSON text.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <returns>A converted <see cref="BsonValue"/>, or a conversion error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> or <paramref name="converter"/> is null.</exception>
    public static Result<BsonValue> ToBson(
        this JsonDocument document,
        IBsonJsonConverter converter,
        JsonBsonConversionOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(document);
        return document.RootElement.ToBson(converter, options);
    }

    /// <summary>
    /// Converts a <see cref="JsonNode"/> to BSON via its JSON text; null yields BSON null.
    /// </summary>
    /// <param name="node">The node to convert, or null for BSON null.</param>
    /// <param name="converter">Shared converter that parses JSON text.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <returns>A converted <see cref="BsonValue"/>, or a conversion error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="converter"/> is null.</exception>
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

        // JsonNode.ToJsonString preserves number literal text for JsonValue numbers.
        return converter.Parse(node.ToJsonString(), options);
    }

    /// <summary>
    /// Converts a <see cref="BsonValue"/> to a standalone root <see cref="JsonElement"/> clone.
    /// </summary>
    /// <param name="value">The BSON value to convert.</param>
    /// <param name="converter">Converter used to produce JSON text.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <returns>A cloned root element, or a conversion error.</returns>
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

    /// <summary>
    /// Converts a <see cref="BsonValue"/> to a <see cref="JsonDocument"/>; the caller owns disposal.
    /// </summary>
    /// <param name="value">The BSON value to convert.</param>
    /// <param name="converter">Converter used to produce JSON text.</param>
    /// <param name="options">Optional conversion rules.</param>
    /// <returns>A parsed <see cref="JsonDocument"/>, or a conversion error.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> or <paramref name="converter"/> is null.</exception>
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

    /// <summary>
    /// Maps an ordinary JSON number token using the shared core number-mode rules.
    /// </summary>
    public static BsonValue ConvertOrdinaryNumber(string rawToken, OrdinaryJsonNumberMode mode) =>
        BsonJsonConverter.ConvertOrdinaryNumber(rawToken, mode);
}