using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.Abstractions.Results;
using Dilcore.MongoDB.Json;
using MongoDB.Bson;

namespace Dilcore.MongoDB.UnitTests;

public class BsonJsonConverterTests
{
    private static readonly BsonJsonConverter Converter = new();

    [TestCase("42", BsonType.Int32)]
    [TestCase("-1", BsonType.Int32)]
    [TestCase("2147483648", BsonType.Int64)]
    [TestCase("1.5", BsonType.Double)]
    [TestCase("1e2", BsonType.Double)]
    public void OrdinaryNumbers_DefaultMode_PickExactBsonType(string number, BsonType expected)
    {
        // Act
        var parsed = Converter.Parse($$"""{"v":{{number}}}""");

        // Assert
        parsed.IsSuccess.ShouldBeTrue(string.Join(", ", parsed.Errors));
        parsed.Value.AsBsonDocument["v"].BsonType.ShouldBe(expected);
    }

    [Test]
    public void AlwaysDouble_RewritesOrdinaryIntegers_ButNotExtendedJsonNumberInt()
    {
        // Arrange
        var options = new JsonBsonConversionOptions
        {
            NumberMode = OrdinaryJsonNumberMode.AlwaysDouble
        };

        // Act
        var ordinary = Converter.Parse("""{"v":1}""", options).Value.AsBsonDocument;
        var extended = Converter.Parse("""{"v":{"$numberInt":"1"}}""", options).Value.AsBsonDocument;

        // Assert
        ordinary["v"].BsonType.ShouldBe(BsonType.Double);
        ordinary["v"].AsDouble.ShouldBe(1d);
        extended["v"].BsonType.ShouldBe(BsonType.Int32);
        extended["v"].AsInt32.ShouldBe(1);
    }

    [Test]
    public void PreferDecimal128_MapsFractionalOrdinaryNumbers()
    {
        // Arrange
        var options = new JsonBsonConversionOptions
        {
            NumberMode = OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDecimal128
        };

        // Act
        var parsed = Converter.Parse("""{"v":1.5}""", options).Value.AsBsonDocument;

        // Assert
        parsed["v"].BsonType.ShouldBe(BsonType.Decimal128);
    }

    [TestCase("42", BsonType.Int32)]
    [TestCase("2147483648", BsonType.Int64)]
    [TestCase("1.5", BsonType.Decimal128)]
    public void PreferDecimal128_MapsIntegersAndFractions(string number, BsonType expected)
    {
        // Arrange
        var options = new JsonBsonConversionOptions
        {
            NumberMode = OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDecimal128
        };

        // Act
        var parsed = Converter.Parse($$"""{"v":{{number}}}""", options).Value.AsBsonDocument;

        // Assert
        parsed["v"].BsonType.ShouldBe(expected);
    }

    [Test]
    public void MaxDepthNegative_Fails()
    {
        // Arrange
        var options = new JsonBsonConversionOptions { MaxDepth = -1 };

        // Act
        var result = Converter.Parse("""{"v":1}""", options);

        // Assert
        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public void OrdinaryFalse_IsBsonBooleanFalse()
    {
        // Act
        var parsed = Converter.Parse("""{"v":false}""").Value.AsBsonDocument;

        // Assert
        parsed["v"].BsonType.ShouldBe(BsonType.Boolean);
        parsed["v"].AsBoolean.ShouldBeFalse();
    }

    [Test]
    public void RoundTrip_RelaxedExtendedJson_SucceedsForObjectIdAndDate()
    {
        // Act
        var parsed = Converter.Parse(
            """{"id":{"$oid":"573a1391f29313caabcd9637"},"when":{"$date":{"$numberLong":"0"}}}""").Value;
        var json = Converter.ToJson(parsed, new JsonBsonConversionOptions
        {
            OutputMode = JsonBsonOutputMode.RelaxedExtendedJson
        });

        // Assert
        json.IsSuccess.ShouldBeTrue();
        var again = Converter.Parse(json.Value).Value.AsBsonDocument;
        again["id"].BsonType.ShouldBe(BsonType.ObjectId);
        again["when"].BsonType.ShouldBe(BsonType.DateTime);
    }

    [Test]
    public void NestedObjectInArray_PreservesTypes()
    {
        // Arrange
        const string json = """
            {
              "items": [
                { "id": { "$oid": "573a1391f29313caabcd9637" }, "n": 1 },
                { "tags": ["a", "b"], "flag": true, "missing": null }
              ],
              "emptyObj": {},
              "emptyArr": [],
              "matrix": [[1, 2], [3]]
            }
            """;

        // Act
        var doc = Converter.Parse(json).Value.AsBsonDocument;

        // Assert
        doc["items"].BsonType.ShouldBe(BsonType.Array);
        var first = doc["items"].AsBsonArray[0].AsBsonDocument;
        first["id"].BsonType.ShouldBe(BsonType.ObjectId);
        first["n"].BsonType.ShouldBe(BsonType.Int32);
        var second = doc["items"].AsBsonArray[1].AsBsonDocument;
        second["tags"].AsBsonArray[0].BsonType.ShouldBe(BsonType.String);
        second["flag"].BsonType.ShouldBe(BsonType.Boolean);
        second["missing"].BsonType.ShouldBe(BsonType.Null);
        doc["emptyObj"].BsonType.ShouldBe(BsonType.Document);
        doc["emptyArr"].BsonType.ShouldBe(BsonType.Array);
        doc["matrix"].AsBsonArray[0].AsBsonArray[1].AsInt32.ShouldBe(2);
    }

    [Test]
    public void RootScalarAndRootArray_AreSupportedByParse()
    {
        // Act
        var scalar = Converter.Parse("42").Value;
        var array = Converter.Parse("[1,{\"$numberLong\":\"2\"}]").Value;

        // Assert
        scalar.BsonType.ShouldBe(BsonType.Int32);
        array.AsBsonArray[1].BsonType.ShouldBe(BsonType.Int64);
    }

    [Test]
    public void InterpretExtendedJsonFalse_KeepsWrapperAsDocument()
    {
        // Arrange
        var options = new JsonBsonConversionOptions { InterpretExtendedJson = false };

        // Act
        var parsed = Converter.Parse("""{"v":{"$oid":"573a1391f29313caabcd9637"}}""", options)
            .Value.AsBsonDocument;

        // Assert
        parsed["v"].BsonType.ShouldBe(BsonType.Document);
        parsed["v"].AsBsonDocument["$oid"].BsonType.ShouldBe(BsonType.String);
    }

    [Test]
    public void RoundTrip_CanonicalExtendedJson_PreservesObjectId()
    {
        // Act
        var parsed = Converter.Parse("""{"id":{"$oid":"573a1391f29313caabcd9637"}}""").Value;
        var json = Converter.ToJson(parsed, new JsonBsonConversionOptions
        {
            OutputMode = JsonBsonOutputMode.CanonicalExtendedJson
        });

        // Assert
        json.IsSuccess.ShouldBeTrue();
        json.Value.ShouldContain("$oid");
        var again = Converter.Parse(json.Value).Value.AsBsonDocument;
        again["id"].BsonType.ShouldBe(BsonType.ObjectId);
    }

    [TestCase("")]
    [TestCase("{")]
    [TestCase("""{"v":""")]
    public void MalformedJson_Fails(string json)
    {
        // Act
        var result = Converter.Parse(json);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasError<JsonConversionError>().ShouldBeTrue();
    }

    [Test]
    public void MaxUtf8Bytes_FailsWithDocumentTooLargeError()
    {
        // Arrange
        var options = new JsonBsonConversionOptions { MaxUtf8Bytes = 8 };

        // Act
        var result = Converter.Parse("""{"v":123456}""", options);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasError<DocumentTooLargeError>().ShouldBeTrue();
    }

    [Test]
    public void MaxDepth_FailsWhenExceeded()
    {
        // Arrange
        var options = new JsonBsonConversionOptions { MaxDepth = 1 };

        // Act
        var result = Converter.Parse("""{"a":{"b":1}}""", options);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.HasError<JsonConversionError>().ShouldBeTrue();
    }

    [Test]
    public void InvalidExtendedJson_Fails()
    {
        // Act
        var oid = Converter.Parse("""{"v":{"$oid":"nothex"}}""");
        var number = Converter.Parse("""{"v":{"$numberLong":"abc"}}""");

        // Assert
        oid.IsFailed.ShouldBeTrue();
        oid.HasError<JsonConversionError>().ShouldBeTrue();
        number.IsFailed.ShouldBeTrue();
        number.HasError<JsonConversionError>().ShouldBeTrue();
    }

    [Test]
    public void DuplicateNames_AcceptedWhenAllowed()
    {
        // Arrange
        var options = new JsonBsonConversionOptions { AllowDuplicateNames = true };

        // Act
        var result = Converter.Parse("""{"v":1,"v":2}""", options);

        // Assert
        result.IsSuccess.ShouldBeTrue(string.Join(", ", result.Errors));
        result.Value.AsBsonDocument["v"].AsInt32.ShouldBe(2);
    }
}