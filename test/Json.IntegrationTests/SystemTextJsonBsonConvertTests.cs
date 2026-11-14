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
        // Arrange
        const string intText = "42";
        const string decimalText = "1.5";
        const string doubleText = "7";

        // Act
        var preferInt32 = JsonBsonConvert.ConvertOrdinaryNumber(intText, OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDouble);
        var preferDecimal = JsonBsonConvert.ConvertOrdinaryNumber(decimalText, OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDecimal128);
        var alwaysDouble = JsonBsonConvert.ConvertOrdinaryNumber(doubleText, OrdinaryJsonNumberMode.AlwaysDouble);

        // Assert
        preferInt32.BsonType.ShouldBe(BsonType.Int32);
        preferDecimal.BsonType.ShouldBe(BsonType.Decimal128);
        alwaysDouble.BsonType.ShouldBe(BsonType.Double);
    }

    [Test]
    public void ToJsonDocument_AndToJsonElement_FailWhenConverterFails()
    {
        // Arrange
        var failing = new FailingBsonJsonConverter();
        var value = new BsonDocument("v", 1);

        // Act
        var document = value.ToJsonDocument(failing);
        var element = value.ToJsonElement(failing);

        // Assert
        document.IsFailed.ShouldBeTrue();
        document.Errors[0].Message.ShouldBe("to-json failed");
        element.IsFailed.ShouldBeTrue();
        element.Errors[0].Message.ShouldBe("to-json failed");
    }

    [Test]
    public void ToJsonElement_ClonesRootFromSuccessfulDocument()
    {
        // Arrange
        var bson = Converter.Parse("""{"v":{"$numberInt":"3"}}""").Value;
        var options = new JsonBsonConversionOptions
        {
            OutputMode = JsonBsonOutputMode.CanonicalExtendedJson
        };

        // Act
        var element = bson.ToJsonElement(Converter, options);

        // Assert
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