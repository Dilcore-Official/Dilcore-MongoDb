namespace Dilcore.MongoDB.Abstractions.Json;

/// <summary>
/// How ordinary JSON numbers (not Extended JSON wrappers) are mapped to BSON numeric types on parse.
/// </summary>
public enum OrdinaryJsonNumberMode
{
    /// <summary>
    /// Prefer <see cref="int"/>, then <see cref="long"/>, then <see cref="double"/> by magnitude.
    /// </summary>
    PreferInt32ThenInt64ThenDouble = 0,

    /// <summary>
    /// Prefer <see cref="int"/>, then <see cref="long"/>, then <c>MongoDB.Bson.Decimal128</c> by magnitude.
    /// </summary>
    PreferInt32ThenInt64ThenDecimal128 = 1,

    /// <summary>
    /// Store all ordinary JSON numbers as <see cref="double"/>.
    /// </summary>
    AlwaysDouble = 2
}