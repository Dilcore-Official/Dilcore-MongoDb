using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.Abstractions.Json;

/// <summary>
/// Stack-neutral JSON string ↔ BSON conversion. JSON packages adapt their DOM types onto this contract.
/// </summary>
public interface IBsonJsonConverter
{
    Result<BsonValue> Parse(string json, JsonBsonConversionOptions? options = null);

    Result<string> ToJson(BsonValue value, JsonBsonConversionOptions? options = null);
}
