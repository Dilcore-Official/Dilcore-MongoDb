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

    /// <summary>
    /// Returns whether <paramref name="propertyName"/> is a recognized Extended JSON type-marker key (for example <c>$oid</c> or <c>$date</c>).
    /// </summary>
    /// <param name="propertyName">JSON object property name to test.</param>
    /// <returns><see langword="true"/> when the name is a known type marker; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is <see langword="null"/>.</exception>
    public static bool IsTypeMarker(string propertyName)
    {
        ArgumentNullException.ThrowIfNull(propertyName);
        return Keys.Contains(propertyName);
    }
}