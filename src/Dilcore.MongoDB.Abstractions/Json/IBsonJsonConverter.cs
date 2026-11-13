using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.Abstractions.Json;

/// <summary>
/// Stack-neutral JSON string ↔ BSON conversion. JSON packages adapt their DOM types onto this contract.
/// </summary>
public interface IBsonJsonConverter
{
    /// <summary>
    /// Parses a JSON string into a <see cref="BsonValue"/> using the supplied limits and Extended JSON rules.
    /// </summary>
    /// <param name="json">UTF-8 JSON text.</param>
    /// <param name="options">Optional conversion options; library defaults apply when <see langword="null"/>.</param>
    /// <returns>A successful BSON value or conversion/size errors.</returns>
    Result<BsonValue> Parse(string json, JsonBsonConversionOptions? options = null);

    /// <summary>
    /// Serializes a <see cref="BsonValue"/> to a JSON string.
    /// </summary>
    /// <param name="value">BSON value to serialize.</param>
    /// <param name="options">Optional output and number formatting options.</param>
    /// <returns>JSON text or conversion errors.</returns>
    Result<string> ToJson(BsonValue value, JsonBsonConversionOptions? options = null);
}