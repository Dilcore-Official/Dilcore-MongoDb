using System.Buffers.Text;
using System.Text;
using Dilcore.MongoDB.Abstractions.Internal;
using FluentResults;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Internal;

internal static class KeysetCursor
{
    private const int Version = 1;

    private static readonly JsonWriterSettings CanonicalJson = new()
    {
        OutputMode = JsonOutputMode.CanonicalExtendedJson
    };

    internal readonly record struct SortKey(string Field, int Direction);

    public static Result<IReadOnlyList<SortKey>> NormalizeSort<TDocument>(SortDefinition<TDocument> sort)
    {
        BsonDocument rendered;
        try
        {
            rendered = sort.Render(CreateRenderArgs<TDocument>());
        }
        catch (Exception exception) when (exception is BsonSerializationException
            or InvalidOperationException
            or ArgumentException
            or NotSupportedException
            or FormatException)
        {
            return Result.Fail($"Invalid sort for keyset paging: {exception.Message}");
        }

        if (rendered.ElementCount == 0)
        {
            return Result.Fail("Keyset paging requires a non-empty sort.");
        }

        var keys = new List<SortKey>(rendered.ElementCount + 1);
        foreach (var element in rendered)
        {
            if (!TryGetSortDirection(element.Value, out var direction))
            {
                return Result.Fail(
                    "Keyset paging supports only ascending (1) or descending (-1) sort keys.");
            }

            keys.Add(new SortKey(element.Name, direction));
        }

        if (!keys.Exists(key => key.Field == DocumentElementNames.Id))
        {
            keys.Add(new SortKey(DocumentElementNames.Id, 1));
        }

        return Result.Ok<IReadOnlyList<SortKey>>(keys);
    }

    public static SortDefinition<TDocument> ToSortDefinition<TDocument>(IReadOnlyList<SortKey> keys)
    {
        var document = new BsonDocument();
        foreach (var key in keys)
        {
            document.Add(key.Field, key.Direction);
        }

        return new BsonDocumentSortDefinition<TDocument>(document);
    }

    public static Result ValidateProjection<TDocument>(
        ProjectionDefinition<TDocument>? projection,
        IReadOnlyList<SortKey> keys)
    {
        if (projection is null)
        {
            return Result.Ok();
        }

        BsonDocument rendered;
        try
        {
            rendered = projection.Render(CreateRenderArgs<TDocument>());
        }
        catch (Exception exception) when (exception is BsonSerializationException
            or InvalidOperationException
            or ArgumentException
            or NotSupportedException
            or FormatException)
        {
            return Result.Fail($"Invalid projection for keyset paging: {exception.Message}");
        }

        if (rendered.ElementCount == 0)
        {
            return Result.Ok();
        }

        foreach (var key in keys)
        {
            if (rendered.TryGetValue(key.Field, out var projected)
                && !IsInclusionValue(projected)
                && !IsExclusionValue(projected))
            {
                return Result.Fail(
                    $"Keyset paging projection must not replace sort key '{key.Field}'.");
            }
        }

        var isInclusion = rendered.Any(element =>
            element.Name != DocumentElementNames.Id && IsInclusionValue(element.Value));

        if (isInclusion)
        {
            foreach (var key in keys)
            {
                if (key.Field == DocumentElementNames.Id)
                {
                    if (rendered.TryGetValue(DocumentElementNames.Id, out var idValue) && IsExclusionValue(idValue))
                    {
                        return Result.Fail(
                            $"Keyset paging projection must include all sort keys (including {DocumentElementNames.Id}).");
                    }

                    continue;
                }

                if (!rendered.TryGetValue(key.Field, out var value) || !IsInclusionValue(value))
                {
                    return Result.Fail(
                        $"Keyset paging projection must include sort key '{key.Field}'.");
                }
            }

            return Result.Ok();
        }

        foreach (var key in keys)
        {
            if (rendered.TryGetValue(key.Field, out var value) && IsExclusionValue(value))
            {
                return Result.Fail(
                    $"Keyset paging projection must not exclude sort key '{key.Field}'.");
            }
        }

        return Result.Ok();
    }

    public static Result<string> TryEncode<TDocument>(TDocument document, IReadOnlyList<SortKey> keys)
    {
        var bson = document.ToBsonDocument();
        var sortArray = new BsonArray(keys.Count);
        var values = new BsonArray(keys.Count);
        foreach (var key in keys)
        {
            var value = GetPathValue(bson, key.Field) ?? BsonNull.Value;
            if (value.IsBsonArray)
            {
                return Result.Fail(
                    $"Keyset paging does not support array-valued sort field '{key.Field}'.");
            }

            sortArray.Add(new BsonArray { key.Field, key.Direction });
            values.Add(value);
        }

        var payload = new BsonDocument
        {
            { "v", Version },
            { "sort", sortArray },
            { "values", values }
        };

        var json = payload.ToJson(CanonicalJson);
        return Result.Ok(Base64Url.EncodeToString(Encoding.UTF8.GetBytes(json)));
    }

    public static Result<FilterDefinition<TDocument>> TryDecodeSeekFilter<TDocument>(
        string cursor,
        IReadOnlyList<SortKey> expectedSort)
    {
        if (!TryDecodePayload(cursor, out var payload, out var error))
        {
            return Result.Fail(error);
        }

        if (!payload.TryGetValue("v", out var versionValue)
            || !versionValue.IsNumeric
            || versionValue.ToInt32() != Version)
        {
            return Result.Fail("Invalid cursor.");
        }

        if (!payload.TryGetValue("sort", out var sortValue)
            || sortValue is not BsonArray sortArray
            || !payload.TryGetValue("values", out var valuesValue)
            || valuesValue is not BsonArray valuesArray
            || sortArray.Count == 0
            || sortArray.Count != valuesArray.Count)
        {
            return Result.Fail("Invalid cursor.");
        }

        if (sortArray.Count != expectedSort.Count)
        {
            return Result.Fail("Cursor sort does not match the request sort.");
        }

        for (var i = 0; i < expectedSort.Count; i++)
        {
            if (sortArray[i] is not BsonArray pair
                || pair.Count != 2
                || pair[0] is not BsonString field
                || !TryGetSortDirection(pair[1], out var direction)
                || field.Value != expectedSort[i].Field
                || direction != expectedSort[i].Direction)
            {
                return Result.Fail("Cursor sort does not match the request sort.");
            }

            if (valuesArray[i].IsBsonArray)
            {
                return Result.Fail("Invalid cursor.");
            }
        }

        return Result.Ok(BuildSeekFilter<TDocument>(expectedSort, valuesArray));
    }

    internal static FilterDefinition<TDocument> BuildSeekFilter<TDocument>(
        IReadOnlyList<SortKey> keys,
        BsonArray values)
    {
        var branches = new List<FilterDefinition<TDocument>>(keys.Count);
        for (var i = 0; i < keys.Count; i++)
        {
            var value = values[i];
            var isNull = value.IsBsonNull;
            FilterDefinition<TDocument>? comparison = null;

            if (keys[i].Direction > 0)
            {
                // Null/missing sort lowest; $gt stays in-type, so OR later BSON types.
                comparison = isNull
                    ? Builders<TDocument>.Filter.Ne(keys[i].Field, BsonNull.Value)
                    : BuildTypeAwareComparison<TDocument>(keys[i].Field, value, ascending: true);
            }
            else if (!isNull)
            {
                // Descending: earlier in-type values, earlier BSON types, or null/missing.
                comparison = BuildTypeAwareComparison<TDocument>(keys[i].Field, value, ascending: false);
            }

            if (comparison is null)
            {
                continue;
            }

            if (i == 0)
            {
                branches.Add(comparison);
                continue;
            }

            var prefix = new FilterDefinition<TDocument>[i];
            for (var j = 0; j < i; j++)
            {
                prefix[j] = Builders<TDocument>.Filter.Eq(keys[j].Field, values[j]);
            }

            branches.Add(Builders<TDocument>.Filter.And(prefix.Append(comparison)));
        }

        return branches.Count == 0
            ? Builders<TDocument>.Filter.Empty
            : branches.Count == 1
                ? branches[0]
                : Builders<TDocument>.Filter.Or(branches);
    }

    private static FilterDefinition<TDocument> BuildTypeAwareComparison<TDocument>(
        string field,
        BsonValue value,
        bool ascending)
    {
        var inType = ascending
            ? Builders<TDocument>.Filter.Gt(field, value)
            : Builders<TDocument>.Filter.Lt(field, value);

        var crossTypeAliases = ascending
            ? TypeAliasesAfter(value.BsonType)
            : TypeAliasesBefore(value.BsonType);

        var parts = new List<FilterDefinition<TDocument>>(1 + crossTypeAliases.Count + (ascending ? 0 : 1))
        {
            inType
        };

        if (crossTypeAliases.Count > 0)
        {
            parts.Add(Builders<TDocument>.Filter.Or(
                crossTypeAliases.Select(alias => Builders<TDocument>.Filter.Type(field, alias))));
        }

        if (!ascending)
        {
            parts.Add(Builders<TDocument>.Filter.Eq(field, BsonNull.Value));
        }

        return parts.Count == 1
            ? parts[0]
            : Builders<TDocument>.Filter.Or(parts);
    }

    // MongoDB comparison/sort type order (excluding null/missing, handled separately).
    private static readonly string[] ComparableTypeAliases =
    [
        "number",
        "string",
        "object",
        "array",
        "binData",
        "objectId",
        "bool",
        "date",
        "timestamp",
        "regex"
    ];

    private static int ComparableTypeRank(BsonType type)
        => type switch
        {
            BsonType.Double or BsonType.Int32 or BsonType.Int64 or BsonType.Decimal128 => 0,
            BsonType.String or BsonType.Symbol => 1,
            BsonType.Document => 2,
            BsonType.Array => 3,
            BsonType.Binary => 4,
            BsonType.ObjectId => 5,
            BsonType.Boolean => 6,
            BsonType.DateTime => 7,
            BsonType.Timestamp => 8,
            BsonType.RegularExpression => 9,
            _ => -1
        };

    private static IReadOnlyList<string> TypeAliasesAfter(BsonType type)
    {
        var rank = ComparableTypeRank(type);
        return rank < 0
            ? Array.Empty<string>()
            : ComparableTypeAliases.AsSpan(rank + 1).ToArray();
    }

    private static IReadOnlyList<string> TypeAliasesBefore(BsonType type)
    {
        var rank = ComparableTypeRank(type);
        return rank <= 0
            ? Array.Empty<string>()
            : ComparableTypeAliases.AsSpan(0, rank).ToArray();
    }

    private static bool TryDecodePayload(string cursor, out BsonDocument payload, out string error)
    {
        payload = null!;
        error = "Invalid cursor.";

        byte[] bytes;
        try
        {
            bytes = Base64Url.DecodeFromChars(cursor.AsSpan());
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        string json;
        try
        {
            json = Encoding.UTF8.GetString(bytes);
        }
        catch (ArgumentException)
        {
            return false;
        }

        try
        {
            payload = BsonDocument.Parse(json);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (BsonException)
        {
            return false;
        }
    }

    private static BsonValue? GetPathValue(BsonDocument document, string path)
    {
        BsonValue current = document;
        var start = 0;
        while (start <= path.Length)
        {
            var separator = path.IndexOf('.', start);
            var length = separator < 0 ? path.Length - start : separator - start;
            var segment = path.Substring(start, length);
            if (current is not BsonDocument currentDocument
                || !currentDocument.TryGetValue(segment, out current))
            {
                return null;
            }

            if (separator < 0)
            {
                return current;
            }

            start = separator + 1;
        }

        return null;
    }

    private static RenderArgs<TDocument> CreateRenderArgs<TDocument>()
        => new(
            BsonSerializer.LookupSerializer<TDocument>(),
            BsonSerializer.SerializerRegistry);

    private static bool TryGetSortDirection(BsonValue value, out int direction)
    {
        direction = 0;
        if (!value.IsNumeric)
        {
            return false;
        }

        var numeric = value.ToInt32();
        if (numeric is not (1 or -1))
        {
            return false;
        }

        direction = numeric;
        return true;
    }

    private static bool IsInclusionValue(BsonValue value)
        => (value.IsBoolean && value.AsBoolean)
            || (value.IsNumeric && value.ToInt32() == 1);

    private static bool IsExclusionValue(BsonValue value)
        => (value.IsBoolean && !value.AsBoolean)
            || (value.IsNumeric && value.ToInt32() == 0);
}