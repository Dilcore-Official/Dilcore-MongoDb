namespace Dilcore.MongoDB.Abstractions.Json;

/// <summary>
/// Canonical Extended JSON type-marker property names used when deciding whether a
/// JSON object should be interpreted as a BSON typed value.
/// </summary>
public static class ExtendedJsonKeys
{
    private static readonly HashSet<string> Keys = new(StringComparer.Ordinal)
    {
        "$oid",
        "$date",
        "$numberInt",
        "$numberLong",
        "$numberDouble",
        "$numberDecimal",
        "$binary",
        "$type",
        "$symbol",
        "$code",
        "$scope",
        "$timestamp",
        "$regularExpression",
        "$minKey",
        "$maxKey",
        "$undefined",
        "$dbPointer",
        "$uuid"
    };

    public static bool IsTypeMarker(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        return Keys.Contains(propertyName);
    }
}
