using System.Buffers.Text;
using System.Text;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Internal;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Internal;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Dilcore.MongoDB.UnitTests;

public class KeysetCursorTests
{
    [OneTimeSetUp]
    public static void RegisterGuidSerializer()
    {
        try
        {
            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
        }
        catch (BsonSerializationException)
        {
            // Already registered by another test fixture in this process.
        }
    }

    [Test]
    public void NormalizeSort_AppendsIdTieBreaker()
    {
        // Arrange
        var sort = Builders<CursorEntity>.Sort.Ascending(x => x.Name);
        var rendered = RenderSort(sort);

        // Act
        var result = KeysetCursor.NormalizeSort(sort);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[0].Field.ShouldBe(rendered.GetElement(0).Name);
        result.Value[0].Direction.ShouldBe(1);
        result.Value[1].Field.ShouldBe(DocumentElementNames.Id);
        result.Value[1].Direction.ShouldBe(1);
    }

    [Test]
    public void NormalizeSort_DoesNotDuplicateExistingId()
    {
        // Arrange
        var sort = Builders<CursorEntity>.Sort
            .Ascending(x => x.Name)
            .Descending(x => x.Id);

        // Act
        var result = KeysetCursor.NormalizeSort(sort);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Count.ShouldBe(2);
        result.Value[^1].Field.ShouldBe(DocumentElementNames.Id);
        result.Value[^1].Direction.ShouldBe(-1);
    }

    [Test]
    public void NormalizeSort_RejectsMetaSort()
    {
        // Arrange
        var sort = new BsonDocumentSortDefinition<CursorEntity>(
            new BsonDocument("score", new BsonDocument("$meta", "textScore")));

        // Act
        var result = KeysetCursor.NormalizeSort(sort);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("ascending");
    }

    [Test]
    public void NormalizeSort_EmptySort_Fails()
    {
        // Arrange
        var sort = new BsonDocumentSortDefinition<CursorEntity>(new BsonDocument());

        // Act
        var result = KeysetCursor.NormalizeSort(sort);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("non-empty");
    }

    [Test]
    public void NormalizeSort_RenderThrows_Fails()
    {
        // Arrange
        var sort = new ThrowingSortDefinition();

        // Act
        var result = KeysetCursor.NormalizeSort(sort);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Invalid sort");
    }

    [Test]
    public void Encode_RoundTripsSeekFilter_ForCompoundSort()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Name).Ascending(x => x.Id)).Value;
        var document = new CursorEntity
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Name = "alpha"
        };

        // Act
        var encoded = KeysetCursor.TryEncode(document, keys);

        // Assert
        encoded.IsSuccess.ShouldBeTrue();
        var seek = KeysetCursor.TryDecodeSeekFilter<CursorEntity>(encoded.Value, keys);
        seek.IsSuccess.ShouldBeTrue();
        var rendered = seek.Value.Render(CreateRenderArgs());
        rendered.ToJson().ShouldContain(keys[0].Field);
        rendered.ToJson().ShouldContain(DocumentElementNames.Id);
    }

    [Test]
    public void TryDecodeSeekFilter_MismatchedSort_Fails()
    {
        // Arrange
        var encodeKeys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Name)).Value;
        var otherKeys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Descending(x => x.Name)).Value;
        var encoded = KeysetCursor.TryEncode(
            new CursorEntity { Id = Guid.NewGuid(), Name = "x" },
            encodeKeys);

        // Act & Assert
        encoded.IsSuccess.ShouldBeTrue();
        var seek = KeysetCursor.TryDecodeSeekFilter<CursorEntity>(encoded.Value, otherKeys);
        seek.IsFailed.ShouldBeTrue();
        seek.Errors[0].Message.ShouldContain("sort");
    }

    [Test]
    public void TryDecodeSeekFilter_MalformedCursor_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(Builders<CursorEntity>.Sort.Ascending(x => x.Id)).Value;

        // Act
        var seek = KeysetCursor.TryDecodeSeekFilter<CursorEntity>("!", keys);

        // Assert
        seek.IsFailed.ShouldBeTrue();
        seek.Errors[0].Message.ShouldBe("Invalid cursor.");
    }

    [Test]
    public void TryDecodeSeekFilter_UnknownVersion_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(Builders<CursorEntity>.Sort.Ascending(x => x.Id)).Value;
        var cursor = EncodePayload(new BsonDocument
        {
            { "v", 99 },
            { "sort", new BsonArray { new BsonArray { DocumentElementNames.Id, 1 } } },
            { "values", new BsonArray { new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard) } }
        });

        // Act
        var seek = KeysetCursor.TryDecodeSeekFilter<CursorEntity>(cursor, keys);

        // Assert
        seek.IsFailed.ShouldBeTrue();
        seek.Errors[0].Message.ShouldBe("Invalid cursor.");
    }

    [Test]
    public void TryDecodeSeekFilter_MissingSort_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(Builders<CursorEntity>.Sort.Ascending(x => x.Id)).Value;
        var cursor = EncodePayload(new BsonDocument
        {
            { "v", 1 },
            { "values", new BsonArray { new BsonInt32(1) } }
        });

        // Act
        var seek = KeysetCursor.TryDecodeSeekFilter<CursorEntity>(cursor, keys);

        // Assert
        seek.IsFailed.ShouldBeTrue();
        seek.Errors[0].Message.ShouldBe("Invalid cursor.");
    }

    [Test]
    public void TryDecodeSeekFilter_SortValuesLengthMismatch_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Name).Ascending(x => x.Id)).Value;
        var cursor = EncodePayload(new BsonDocument
        {
            { "v", 1 },
            {
                "sort", new BsonArray
                {
                    new BsonArray { keys[0].Field, keys[0].Direction },
                    new BsonArray { keys[1].Field, keys[1].Direction }
                }
            },
            { "values", new BsonArray { "only-one" } }
        });

        // Act
        var seek = KeysetCursor.TryDecodeSeekFilter<CursorEntity>(cursor, keys);

        // Assert
        seek.IsFailed.ShouldBeTrue();
        seek.Errors[0].Message.ShouldBe("Invalid cursor.");
    }

    [Test]
    public void TryDecodeSeekFilter_SortKeyCountMismatch_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Name).Ascending(x => x.Id)).Value;
        var cursor = EncodePayload(new BsonDocument
        {
            { "v", 1 },
            { "sort", new BsonArray { new BsonArray { DocumentElementNames.Id, 1 } } },
            {
                "values",
                new BsonArray { new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard) }
            }
        });

        // Act
        var seek = KeysetCursor.TryDecodeSeekFilter<CursorEntity>(cursor, keys);

        // Assert
        seek.IsFailed.ShouldBeTrue();
        seek.Errors[0].Message.ShouldContain("sort");
    }

    [Test]
    public void Encode_NestedSortKey_RoundTrips()
    {
        // Arrange
        var sort = Builders<NestedCursorEntity>.Sort.Ascending(x => x.Profile.City);
        var keys = KeysetCursor.NormalizeSort(sort).Value;
        var document = new NestedCursorEntity
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Profile = new NestedProfile { City = "Oslo" }
        };

        // Act & Assert
        keys[0].Field.ShouldContain(".");
        var encoded = KeysetCursor.TryEncode(document, keys);
        encoded.IsSuccess.ShouldBeTrue();
        var seek = KeysetCursor.TryDecodeSeekFilter<NestedCursorEntity>(encoded.Value, keys);
        seek.IsSuccess.ShouldBeTrue();
        var rendered = seek.Value.Render(
            new RenderArgs<NestedCursorEntity>(
                BsonSerializer.LookupSerializer<NestedCursorEntity>(),
                BsonSerializer.SerializerRegistry));
        rendered.ToJson().ShouldContain("Oslo");
    }

    [Test]
    public void BuildSeekFilter_AscendingAfterNull_UsesNotEqualsNull()
    {
        // Arrange
        var nameField = RenderSort(Builders<CursorEntity>.Sort.Ascending(x => x.Name))
            .GetElement(0).Name;
        var keys = new KeysetCursor.SortKey[]
        {
            new(nameField, 1),
            new(DocumentElementNames.Id, 1)
        };
        var values = new BsonArray
        {
            BsonNull.Value,
            new BsonBinaryData(Guid.NewGuid(), GuidRepresentation.Standard)
        };

        // Act
        var filter = KeysetCursor.BuildSeekFilter<CursorEntity>(keys, values);
        var rendered = filter.Render(CreateRenderArgs());

        // Assert
        rendered.ToJson().ShouldContain("$ne");
    }

    [Test]
    public void BuildSeekFilter_DescendingAfterNull_HasNoBranches()
    {
        // Arrange
        var nameField = RenderSort(Builders<CursorEntity>.Sort.Descending(x => x.Name))
            .GetElement(0).Name;
        var keys = new KeysetCursor.SortKey[] { new(nameField, -1) };
        var values = new BsonArray { BsonNull.Value };

        // Act
        var filter = KeysetCursor.BuildSeekFilter<CursorEntity>(keys, values);
        var rendered = filter.Render(CreateRenderArgs());

        // Assert
        rendered.ElementCount.ShouldBe(0);
    }

    [Test]
    public void ValidateProjection_ExclusionOfSortKey_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Name)).Value;
        var projection = Builders<CursorEntity>.Projection.Exclude(x => x.Name);

        // Act
        var result = KeysetCursor.ValidateProjection(projection, keys);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain(keys[0].Field);
    }

    [Test]
    public void ValidateProjection_ExclusionOfNonSortKey_Succeeds()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Id)).Value;
        var projection = Builders<CursorEntity>.Projection.Exclude(x => x.Name);

        // Act
        var result = KeysetCursor.ValidateProjection(projection, keys);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ValidateProjection_EmptyProjection_Succeeds()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Id)).Value;

        // Act
        var result = KeysetCursor.ValidateProjection(new EmptyProjectionDefinition(), keys);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ValidateProjection_InclusionMissingSortKey_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Name)).Value;
        // Inclusion mode is detected from a non-_id include; Name (sort key) is omitted.
        var projection = Builders<CursorEntity>.Projection.Include(x => x.ETag);

        // Act
        var result = KeysetCursor.ValidateProjection(projection, keys);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain(keys[0].Field);
    }

    [Test]
    public void ValidateProjection_InclusionExcludingId_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Name)).Value;
        var projection = Builders<CursorEntity>.Projection
            .Exclude(x => x.Id)
            .Include(x => x.Name);

        // Act
        var result = KeysetCursor.ValidateProjection(projection, keys);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain(DocumentElementNames.Id);
    }

    [Test]
    public void ValidateProjection_InclusionWithAllSortKeys_Succeeds()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Name)).Value;
        var projection = Builders<CursorEntity>.Projection
            .Include(x => x.Name)
            .Include(x => x.Id);

        // Act
        var result = KeysetCursor.ValidateProjection(projection, keys);

        // Assert
        result.IsSuccess.ShouldBeTrue();
    }

    [Test]
    public void ValidateProjection_RenderThrows_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Id)).Value;

        // Act
        var result = KeysetCursor.ValidateProjection(new ThrowingProjectionDefinition(), keys);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("Invalid projection");
    }

    [Test]
    public void ValidateProjection_ComputedSortKey_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(
            Builders<CursorEntity>.Sort.Ascending(x => x.Name)).Value;
        var projection = new ComputedSortKeyProjectionDefinition(keys[0].Field);

        // Act
        var result = KeysetCursor.ValidateProjection(projection, keys);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors[0].Message.ShouldContain("replace");
    }

    [Test]
    public void TryEncode_ArraySortValue_Fails()
    {
        // Arrange
        var keys = new KeysetCursor.SortKey[]
        {
            new("Tags", 1),
            new(DocumentElementNames.Id, 1)
        };
        var document = new ArrayCursorEntity
        {
            Id = Guid.NewGuid(),
            Tags = ["a", "b"]
        };

        // Act
        var encoded = KeysetCursor.TryEncode(document, keys);

        // Assert
        encoded.IsFailed.ShouldBeTrue();
        encoded.Errors[0].Message.ShouldContain("array-valued");
    }

    [Test]
    public void TryDecodeSeekFilter_ArrayCursorValue_Fails()
    {
        // Arrange
        var keys = KeysetCursor.NormalizeSort(Builders<CursorEntity>.Sort.Ascending(x => x.Id)).Value;
        var cursor = EncodePayload(new BsonDocument
        {
            { "v", 1 },
            { "sort", new BsonArray { new BsonArray { DocumentElementNames.Id, 1 } } },
            { "values", new BsonArray { new BsonArray { 1, 2 } } }
        });

        // Act
        var seek = KeysetCursor.TryDecodeSeekFilter<CursorEntity>(cursor, keys);

        // Assert
        seek.IsFailed.ShouldBeTrue();
        seek.Errors[0].Message.ShouldBe("Invalid cursor.");
    }

    [Test]
    public void BuildSeekFilter_AscendingNumber_IncludesLaterBsonTypes()
    {
        // Arrange
        var keys = new KeysetCursor.SortKey[] { new("Score", 1) };
        var values = new BsonArray { new BsonInt32(5) };

        // Act
        var filter = KeysetCursor.BuildSeekFilter<ScoreEntity>(keys, values);
        var rendered = filter.Render(
            new RenderArgs<ScoreEntity>(
                BsonSerializer.LookupSerializer<ScoreEntity>(),
                BsonSerializer.SerializerRegistry));
        var json = rendered.ToJson();

        // Assert
        json.ShouldContain("$gt");
        json.ShouldContain("$type");
        json.ShouldContain("string");
    }

    [Test]
    public void BuildSeekFilter_DescendingString_IncludesEarlierBsonTypesAndNull()
    {
        // Arrange
        var keys = new KeysetCursor.SortKey[] { new("Name", -1) };
        var values = new BsonArray { "m" };

        // Act
        var filter = KeysetCursor.BuildSeekFilter<CursorEntity>(keys, values);
        var rendered = filter.Render(CreateRenderArgs());
        var json = rendered.ToJson();

        // Assert
        json.ShouldContain("$lt");
        json.ShouldContain("$type");
        json.ShouldContain("number");
        json.ShouldContain("null");
    }

    private static string EncodePayload(BsonDocument payload)
    {
        var json = payload.ToJson(new JsonWriterSettings { OutputMode = JsonOutputMode.CanonicalExtendedJson });
        return Base64Url.EncodeToString(Encoding.UTF8.GetBytes(json));
    }

    private static BsonDocument RenderSort(SortDefinition<CursorEntity> sort)
        => sort.Render(CreateRenderArgs());

    private static RenderArgs<CursorEntity> CreateRenderArgs()
        => new(
            BsonSerializer.LookupSerializer<CursorEntity>(),
            BsonSerializer.SerializerRegistry);

    private sealed class ThrowingSortDefinition : SortDefinition<CursorEntity>
    {
        public override BsonDocument Render(RenderArgs<CursorEntity> args)
            => throw new InvalidOperationException("render failed");
    }

    private sealed class EmptyProjectionDefinition : ProjectionDefinition<CursorEntity>
    {
        public override BsonDocument Render(RenderArgs<CursorEntity> args) => new();
    }

    private sealed class ThrowingProjectionDefinition : ProjectionDefinition<CursorEntity>
    {
        public override BsonDocument Render(RenderArgs<CursorEntity> args)
            => throw new InvalidOperationException("render failed");
    }

    private sealed class ComputedSortKeyProjectionDefinition(string sortKeyField)
        : ProjectionDefinition<CursorEntity>
    {
        public override BsonDocument Render(RenderArgs<CursorEntity> args)
            => new(sortKeyField, new BsonDocument("$literal", "x"));
    }

    private sealed class CursorEntity : IDocumentEntity<Guid>, IHasConcurrencyToken
    {
        public Guid Id { get; set; }

        public long ETag { get; set; }

        public string? Name { get; set; }
    }

    private sealed class NestedCursorEntity : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }

        public NestedProfile Profile { get; set; } = new();
    }

    private sealed class NestedProfile
    {
        public string? City { get; set; }
    }

    private sealed class ArrayCursorEntity : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }

        public string[] Tags { get; set; } = [];
    }

    private sealed class ScoreEntity : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }

        public int Score { get; set; }
    }
}