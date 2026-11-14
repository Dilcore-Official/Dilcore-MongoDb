using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.SystemTextJson;
using FluentResults;
using MongoDB.Bson;

namespace Dilcore.MongoDB.Json.IntegrationTests;

[Category("M3Matrix")]
public class SystemTextJsonBsonConvertTests
{
    private static readonly IBsonJsonConverter Converter = new BsonJsonConverter();

    [Test]
    public void ConvertOrdinaryNumber_MatchesCoreNumberMode()
    {
        JsonBsonConvert.ConvertOrdinaryNumber("42", OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDouble)
            .BsonType.ShouldBe(BsonType.Int32);
        JsonBsonConvert.ConvertOrdinaryNumber("1.5", OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDecimal128)
            .BsonType.ShouldBe(BsonType.Decimal128);
        JsonBsonConvert.ConvertOrdinaryNumber("7", OrdinaryJsonNumberMode.AlwaysDouble)
            .BsonType.ShouldBe(BsonType.Double);
    }

    [Test]
    public void ToJsonDocument_AndToJsonElement_FailWhenConverterFails()
    {
        var failing = new FailingBsonJsonConverter();
        var value = new BsonDocument("v", 1);

        var document = value.ToJsonDocument(failing);
        document.IsFailed.ShouldBeTrue();
        document.Errors[0].Message.ShouldBe("to-json failed");

        var element = value.ToJsonElement(failing);
        element.IsFailed.ShouldBeTrue();
        element.Errors[0].Message.ShouldBe("to-json failed");
    }

    [Test]
    public void ToJsonElement_ClonesRootFromSuccessfulDocument()
    {
        var bson = Converter.Parse("""{"v":{"$numberInt":"3"}}""").Value;
        var element = bson.ToJsonElement(Converter, new JsonBsonConversionOptions
        {
            OutputMode = JsonBsonOutputMode.CanonicalExtendedJson
        });
        element.IsSuccess.ShouldBeTrue();
        element.Value.GetProperty("v").GetProperty("$numberInt").GetString().ShouldBe("3");
    }

    private sealed class FailingBsonJsonConverter : IBsonJsonConverter
    {
        public Result<BsonValue> Parse(string json, JsonBsonConversionOptions? options = null) =>
            Result.Fail("parse failed");

        public Result<string> ToJson(BsonValue value, JsonBsonConversionOptions? options = null) =>
            Result.Fail("to-json failed");
    }
}