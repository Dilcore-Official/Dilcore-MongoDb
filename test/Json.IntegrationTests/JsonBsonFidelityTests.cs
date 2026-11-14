using Dilcore.MongoDB.Abstractions.Json;
using Dilcore.MongoDB.NewtonsoftJson;
using Dilcore.MongoDB.SystemTextJson;
using MongoDB.Bson;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Dilcore.MongoDB.Json.IntegrationTests;

[Category("M3Matrix")]
public class JsonBsonFidelityTests
{
    private static readonly IBsonJsonConverter Converter = new BsonJsonConverter();

    [TestCase("""{"v":{"$oid":"573a1391f29313caabcd9637"}}""", BsonType.ObjectId)]
    [TestCase("""{"v":{"$date":{"$numberLong":"0"}}}""", BsonType.DateTime)]
    [TestCase("""{"v":{"$numberInt":"42"}}""", BsonType.Int32)]
    [TestCase("""{"v":{"$numberLong":"9007199254740993"}}""", BsonType.Int64)]
    [TestCase("""{"v":{"$numberDouble":"1.5"}}""", BsonType.Double)]
    [TestCase("""{"v":{"$numberDecimal":"10.50"}}""", BsonType.Decimal128)]
    [TestCase("""{"v":{"$minKey":1}}""", BsonType.MinKey)]
    [TestCase("""{"v":{"$maxKey":1}}""", BsonType.MaxKey)]
    public void CanonicalExtendedJson_PreservesBsonType(string json, BsonType expected)
    {
        var parsed = Converter.Parse(json);
        parsed.IsSuccess.ShouldBeTrue(string.Join(", ", parsed.Errors));
        parsed.Value.AsBsonDocument["v"].BsonType.ShouldBe(expected);

        using var stj = JsonDocument.Parse(json);
        var stjParsed = stj.ToBson(Converter);
        stjParsed.IsSuccess.ShouldBeTrue();
        stjParsed.Value.AsBsonDocument["v"].BsonType.ShouldBe(expected);

        var nsParsed = JObject.Parse(json).ToBson(Converter);
        nsParsed.IsSuccess.ShouldBeTrue();
        nsParsed.Value.AsBsonDocument["v"].BsonType.ShouldBe(expected);
    }

    [Test]
    public void NestedDocumentAndArray_PreserveNestedTypes()
    {
        const string json = """
            {
              "child": { "id": { "$oid": "573a1391f29313caabcd9637" } },
              "items": [ { "$numberInt": "1" }, { "$numberLong": "2" }, "plain" ]
            }
            """;
        var parsed = Converter.Parse(json).Value.AsBsonDocument;
        parsed["child"].BsonType.ShouldBe(BsonType.Document);
        parsed["child"].AsBsonDocument["id"].BsonType.ShouldBe(BsonType.ObjectId);
        parsed["items"].BsonType.ShouldBe(BsonType.Array);
        parsed["items"].AsBsonArray[0].BsonType.ShouldBe(BsonType.Int32);
        parsed["items"].AsBsonArray[1].BsonType.ShouldBe(BsonType.Int64);
        parsed["items"].AsBsonArray[2].BsonType.ShouldBe(BsonType.String);
    }

    [Test]
    public void OrdinaryJsonString_IsNotReinterpretedAsObjectId()
    {
        var parsed = Converter.Parse("""{"v":"573a1391f29313caabcd9637"}""").Value.AsBsonDocument;
        parsed["v"].BsonType.ShouldBe(BsonType.String);
        parsed["v"].AsString.ShouldBe("573a1391f29313caabcd9637");
    }

    [Test]
    public void DuplicateNames_AreRejectedByDefault()
    {
        var result = Converter.Parse("""{"v":1,"v":2}""");
        result.IsFailed.ShouldBeTrue();
    }

    [Test]
    public void Newtonsoft_TypeNameHandling_IsRejected()
    {
        var token = JObject.Parse("""{"v":1}""");
        var result = token.ToBson(
            Converter,
            serializerSettings: new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All });
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("TypeNameHandling");
    }

    [Test]
    public void JsonNode_RoundTripsCanonicalObjectId()
    {
        var node = JsonNode.Parse("""{"id":{"$oid":"573a1391f29313caabcd9637"}}""")!;
        var bson = node.ToBson(Converter);
        bson.IsSuccess.ShouldBeTrue();
        bson.Value.AsBsonDocument["id"].BsonType.ShouldBe(BsonType.ObjectId);
        var json = Converter.ToJson(bson.Value, new JsonBsonConversionOptions
        {
            OutputMode = JsonBsonOutputMode.CanonicalExtendedJson
        });
        json.Value.ShouldContain("$oid");
    }

    [TestCase("""{"v":1}""", BsonType.Int32)]
    [TestCase("""{"v":2147483648}""", BsonType.Int64)]
    [TestCase("""{"v":1.5}""", BsonType.Double)]
    [TestCase("""{"v":true}""", BsonType.Boolean)]
    [TestCase("""{"v":null}""", BsonType.Null)]
    [TestCase("""{"v":"2020-01-01T00:00:00Z"}""", BsonType.String)]
    public void OrdinaryJson_PreservesExactBsonType_OnBothAdapters(string json, BsonType expected)
    {
        Converter.Parse(json).Value.AsBsonDocument["v"].BsonType.ShouldBe(expected);

        using var stj = JsonDocument.Parse(json);
        stj.ToBson(Converter).Value.AsBsonDocument["v"].BsonType.ShouldBe(expected);

        using var reader = new JsonTextReader(new StringReader(json))
        {
            DateParseHandling = DateParseHandling.None,
            FloatParseHandling = FloatParseHandling.Decimal
        };
        JToken.Load(reader).ToBson(Converter).Value.AsBsonDocument["v"].BsonType.ShouldBe(expected);
    }

    [Test]
    public void EmptyObjectAndArray_RoundTrip()
    {
        var parsed = Converter.Parse("""{"obj":{},"arr":[]}""").Value.AsBsonDocument;
        parsed["obj"].AsBsonDocument.ElementCount.ShouldBe(0);
        parsed["arr"].AsBsonArray.Count.ShouldBe(0);

        var json = Converter.ToJson(parsed).Value;
        using var stj = JsonDocument.Parse(json);
        stj.RootElement.GetProperty("obj").EnumerateObject().Any().ShouldBeFalse();
        stj.RootElement.GetProperty("arr").GetArrayLength().ShouldBe(0);
    }

    [Test]
    public void ArrayOfMixedTypes_PreservesEachElement()
    {
        const string json = """{"v":[1,{"$numberLong":"2"},"x",true,null,{"nested":[]}]}""";
        var array = Converter.Parse(json).Value.AsBsonDocument["v"].AsBsonArray;
        array[0].BsonType.ShouldBe(BsonType.Int32);
        array[1].BsonType.ShouldBe(BsonType.Int64);
        array[2].BsonType.ShouldBe(BsonType.String);
        array[3].BsonType.ShouldBe(BsonType.Boolean);
        array[4].BsonType.ShouldBe(BsonType.Null);
        array[5].BsonType.ShouldBe(BsonType.Document);
    }

    [Test]
    public void Newtonsoft_ToJToken_DoesNotCoerceExtendedDateStrings()
    {
        var bson = Converter.Parse("""{"when":{"$date":{"$numberLong":"0"}}}""").Value;
        var token = bson.ToJToken(Converter, new JsonBsonConversionOptions
        {
            OutputMode = JsonBsonOutputMode.CanonicalExtendedJson
        });
        token.IsSuccess.ShouldBeTrue();
        token.Value["when"]!["$date"]!["$numberLong"]!.Type.ShouldBe(JTokenType.String);
    }
}