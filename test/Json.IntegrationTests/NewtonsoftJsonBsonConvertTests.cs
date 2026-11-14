using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.NewtonsoftJson;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;

namespace Dilcore.MongoDB.Json.IntegrationTests;

[Category("M3Matrix")]
public class NewtonsoftJsonBsonConvertTests
{
    private static readonly IBsonJsonConverter Converter = new BsonJsonConverter();

    [Test]
    public void JArray_OfMixedValues_PreservesElementTypes()
    {
        // Arrange
        var token = new JObject
        {
            ["v"] = new JArray
            {
                1,
                new JObject { ["$numberLong"] = "2" },
                "x",
                true,
                false,
                JValue.CreateNull(),
                new JObject { ["nested"] = new JArray() }
            }
        };

        // Act
        var array = token.ToBson(Converter).Value.AsBsonDocument["v"].AsBsonArray;

        // Assert
        array[0].BsonType.ShouldBe(BsonType.Int32);
        array[1].BsonType.ShouldBe(BsonType.Int64);
        array[2].BsonType.ShouldBe(BsonType.String);
        array[3].BsonType.ShouldBe(BsonType.Boolean);
        array[3].AsBoolean.ShouldBeTrue();
        array[4].BsonType.ShouldBe(BsonType.Boolean);
        array[4].AsBoolean.ShouldBeFalse();
        array[5].BsonType.ShouldBe(BsonType.Null);
        array[6].BsonType.ShouldBe(BsonType.Document);
        array[6].AsBsonDocument["nested"].BsonType.ShouldBe(BsonType.Array);
    }

    [Test]
    public void ProgrammaticJValues_MapToExactBsonTypes()
    {
        // Arrange
        var guid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var bytes = new byte[] { 1, 2, 3 };
        var when = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var token = new JObject
        {
            ["g"] = new JValue(guid),
            ["b"] = new JValue(bytes),
            ["u"] = new JValue(new Uri("https://example.test/path")),
            ["t"] = new JValue(TimeSpan.FromMinutes(5)),
            ["d"] = new JValue(when),
            ["f"] = new JValue(false)
        };

        // Act
        var doc = token.ToBson(Converter).Value.AsBsonDocument;

        // Assert
        doc["g"].BsonType.ShouldBe(BsonType.Binary);
        doc["g"].AsBsonBinaryData.ToGuid(GuidRepresentation.Standard).ShouldBe(guid);
        doc["b"].BsonType.ShouldBe(BsonType.Binary);
        doc["b"].AsByteArray.ShouldBe(bytes);
        doc["u"].BsonType.ShouldBe(BsonType.String);
        doc["u"].AsString.ShouldBe("https://example.test/path");
        doc["t"].BsonType.ShouldBe(BsonType.String);
        doc["d"].BsonType.ShouldBe(BsonType.DateTime);
        doc["f"].BsonType.ShouldBe(BsonType.Boolean);
        doc["f"].AsBoolean.ShouldBeFalse();
    }

    [Test]
    public void IntegerAndFloatBackings_HonorNumberMode()
    {
        // Arrange
        var defaultToken = new JObject
        {
            ["long"] = new JValue(42L),
            ["decInt"] = new JValue(7m),
            ["dbl"] = new JValue(1.5d),
            ["flt"] = new JValue(2.5f)
        };
        var decimalMode = new JsonBsonConversionOptions
        {
            NumberMode = OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDecimal128
        };
        var decimalToken = new JObject
        {
            ["big"] = new JValue(2147483648L),
            ["frac"] = new JValue(1.5d)
        };

        // Act
        var defaultMode = defaultToken.ToBson(Converter).Value.AsBsonDocument;
        var withDecimal = decimalToken.ToBson(Converter, decimalMode).Value.AsBsonDocument;

        // Assert
        defaultMode["long"].BsonType.ShouldBe(BsonType.Int32);
        defaultMode["decInt"].BsonType.ShouldBe(BsonType.Int32);
        defaultMode["dbl"].BsonType.ShouldBe(BsonType.Double);
        defaultMode["flt"].BsonType.ShouldBe(BsonType.Double);
        withDecimal["big"].BsonType.ShouldBe(BsonType.Int64);
        withDecimal["frac"].BsonType.ShouldBe(BsonType.Decimal128);
    }

    [Test]
    public void MaxDepthExceeded_Fails()
    {
        // Arrange
        var nested = new JObject { ["a"] = new JObject { ["b"] = 1 } };
        var options = new JsonBsonConversionOptions { MaxDepth = 0 };

        // Act
        var result = nested.ToBson(Converter, options);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasError<JsonConversionError>().ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("MaxDepth");
    }

    [Test]
    public void InvalidExtendedJson_Fails()
    {
        // Arrange
        var token = new JObject { ["v"] = new JObject { ["$oid"] = "nothex" } };

        // Act
        var result = token.ToBson(Converter);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasError<JsonConversionError>().ShouldBeTrue();
    }

    [Test]
    public void ToJObject_OnArrayRoot_Fails()
    {
        // Arrange
        var array = new BsonArray { 1, 2 };

        // Act
        var result = ((BsonValue)array).ToJObject(Converter);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("JSON root must be a document");
    }
}