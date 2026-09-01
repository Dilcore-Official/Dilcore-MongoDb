namespace Dilcore.MongoDB.Abstractions.Json;

public enum JsonBsonOutputMode
{
    CanonicalExtendedJson = 0,
    RelaxedExtendedJson = 1
}

public enum OrdinaryJsonNumberMode
{
    PreferInt32ThenInt64ThenDouble = 0,
    PreferInt32ThenInt64ThenDecimal128 = 1,
    AlwaysDouble = 2
}

public sealed class JsonBsonConversionOptions
{
    public JsonBsonOutputMode OutputMode { get; init; } = JsonBsonOutputMode.CanonicalExtendedJson;

    public OrdinaryJsonNumberMode NumberMode { get; init; } =
        OrdinaryJsonNumberMode.PreferInt32ThenInt64ThenDouble;

    public int MaxDepth { get; init; } = 64;

    public int MaxUtf8Bytes { get; init; } = 16 * 1024 * 1024;

    public bool AllowDuplicateNames { get; init; }
}
