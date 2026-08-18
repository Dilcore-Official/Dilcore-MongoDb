namespace Dilcore.MongoDB.Abstractions;

/// <summary>
/// Strategy used when auto-generating <see cref="Guid"/> document identifiers.
/// </summary>
public enum GuidIdGenerationStrategy
{
    /// <summary>
    /// Random UUID version 4 via <see cref="Guid.NewGuid"/>.
    /// </summary>
    Random = 0,

    /// <summary>
    /// Time-ordered UUID version 7 (RFC 9562) via <see cref="Guid.CreateVersion7()"/>.
    /// </summary>
    SequentialVersion7 = 1
}
