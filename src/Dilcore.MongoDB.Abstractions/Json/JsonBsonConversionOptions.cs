namespace Dilcore.MongoDB.Abstractions.Json;

/// <summary>
/// Limits and behavior for JSON ↔ BSON conversion.
/// </summary>
public sealed class JsonBsonConversionOptions
{
    /// <summary>
    /// Extended JSON format used when writing BSON as text.
    /// </summary>
    public JsonBsonOutputMode OutputMode { get; init; } = JsonBsonOutputMode.CanonicalExtendedJson;

    /// <summary>
    /// Numeric type selection for plain JSON numbers during parse.
    /// </summary>
    public OrdinaryJsonNumberMode NumberMode { get; init; } =
        OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDouble;

    /// <summary>
    /// Maximum JSON parse depth (passed to the underlying JSON parser).
    /// </summary>
    public int MaxDepth { get; init; } = 64;

    /// <summary>
    /// Maximum UTF-8 byte length allowed for an input JSON string.
    /// </summary>
    public int MaxUtf8Bytes { get; init; } = 16 * 1024 * 1024;

    /// <summary>
    /// When <see langword="true"/>, duplicate property names in JSON objects are allowed by the parser.
    /// </summary>
    public bool AllowDuplicateNames { get; init; }

    /// <summary>
    /// When true (default), single-purpose Extended JSON wrappers such as
    /// <c>$oid</c>, <c>$date</c>, and <c>$numberLong</c> become the matching BSON types.
    /// Ordinary JSON strings that look like ObjectIds stay strings.
    /// </summary>
    public bool InterpretExtendedJson { get; init; } = true;
}