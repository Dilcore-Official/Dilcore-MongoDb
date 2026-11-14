namespace Dilcore.MongoDB.Abstractions.Json;

/// <summary>
/// Extended JSON output style when serializing BSON to text.
/// </summary>
public enum JsonBsonOutputMode
{
    /// <summary>
    /// MongoDB canonical Extended JSON (type-preserving, verbose).
    /// </summary>
    CanonicalExtendedJson = 0,

    /// <summary>
    /// Relaxed Extended JSON (more readable literals where safe).
    /// </summary>
    RelaxedExtendedJson = 1
}