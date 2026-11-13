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
        var parsed = Converter.Parse($$"""{"v":{{number}}}""");
        parsed.IsSuccess.ShouldBeTrue(string.Join(", ", parsed.Errors));
        parsed.Value.AsBsonDocument["v"].BsonType.ShouldBe(expected);
    }

    [Test]
    public void AlwaysDouble_RewritesOrdinaryIntegers_ButNotExtendedJsonNumberInt()
    {
        var options = new JsonBsonConversionOptions
        {
            NumberMode = OrdinaryJsonNumberMode.AlwaysDouble
        };

        var ordinary = Converter.Parse("""{"v":1}""", options).Value.AsBsonDocument;
        ordinary["v"].BsonType.ShouldBe(BsonType.Double);
        ordinary["v"].AsDouble.ShouldBe(1d);

        var extended = Converter.Parse("""{"v":{"$numberInt":"1"}}""", options).Value.AsBsonDocument;
        extended["v"].BsonType.ShouldBe(BsonType.Int32);
        extended["v"].AsInt32.ShouldBe(1);
    }

    [Test]
    public void PreferDecimal128_MapsFractionalOrdinaryNumbers()
    {
        var options = new JsonBsonConversionOptions
        {
            NumberMode = OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDecimal128
        };

        var parsed = Converter.Parse("""{"v":1.5}""", options).Value.AsBsonDocument;
        parsed["v"].BsonType.ShouldBe(BsonType.Decimal128);
    }

    [Test]
    public void NestedObjectInArray_PreservesTypes()
    {
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

        var doc = Converter.Parse(json).Value.AsBsonDocument;
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
        Converter.Parse("42").Value.BsonType.ShouldBe(BsonType.Int32);
        Converter.Parse("[1,{\"$numberLong\":\"2\"}]").Value.AsBsonArray[1].BsonType.ShouldBe(BsonType.Int64);
    }

    [Test]
    public void InterpretExtendedJsonFalse_KeepsWrapperAsDocument()
    {
        var options = new JsonBsonConversionOptions { InterpretExtendedJson = false };
        var parsed = Converter.Parse("""{"v":{"$oid":"573a1391f29313caabcd9637"}}""", options)
            .Value.AsBsonDocument;
        parsed["v"].BsonType.ShouldBe(BsonType.Document);
        parsed["v"].AsBsonDocument["$oid"].BsonType.ShouldBe(BsonType.String);
    }

    [Test]
    public void RoundTrip_CanonicalExtendedJson_PreservesObjectId()
    {
        var parsed = Converter.Parse("""{"id":{"$oid":"573a1391f29313caabcd9637"}}""").Value;
        var json = Converter.ToJson(parsed, new JsonBsonConversionOptions
        {
            OutputMode = JsonBsonOutputMode.CanonicalExtendedJson
        });
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
        Converter.Parse(json).IsFailed.ShouldBeTrue();
    }

    [Test]
    public void MaxUtf8Bytes_FailsWithDocumentTooLargeError()
    {
        var options = new JsonBsonConversionOptions { MaxUtf8Bytes = 8 };
        var result = Converter.Parse("""{"v":123456}""", options);
        result.IsFailed.ShouldBeTrue();
        result.HasError<DocumentTooLargeError>().ShouldBeTrue();
    }

    [Test]
    public void MaxDepth_FailsWhenExceeded()
    {
        var options = new JsonBsonConversionOptions { MaxDepth = 1 };
        var result = Converter.Parse("""{"a":{"b":1}}""", options);
        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public void InvalidExtendedJson_Fails()
    {
        Converter.Parse("""{"v":{"$oid":"nothex"}}""").IsFailed.ShouldBeTrue();
        Converter.Parse("""{"v":{"$numberLong":"abc"}}""").IsFailed.ShouldBeTrue();
    }

    [Test]
    public void DuplicateNames_AcceptedWhenAllowed()
    {
        var options = new JsonBsonConversionOptions { AllowDuplicateNames = true };
        var result = Converter.Parse("""{"v":1,"v":2}""", options);
        result.IsSuccess.ShouldBeTrue(string.Join(", ", result.Errors));
        result.Value.AsBsonDocument["v"].AsInt32.ShouldBe(2);
    }
}
