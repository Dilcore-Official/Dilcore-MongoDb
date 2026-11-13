using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace Dilcore.MongoDB.DependencyInjection;

/// <summary>
/// Configures process-wide BSON serialization conventions applied once during <c>AddMongoDb</c>.
/// </summary>
public interface IConventionsBuilder
{
    /// <summary>
    /// Sets how enum values are represented in BSON.
    /// </summary>
    /// <param name="representation">Supported values are <see cref="BsonType.String"/>, <see cref="BsonType.Int32"/>, or <see cref="BsonType.Int64"/>.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="representation"/> is not supported.</exception>
    IConventionsBuilder UseEnumRepresentation(BsonType representation);

    /// <summary>
    /// Replaces the default element-name convention (camelCase) with a custom convention.
    /// </summary>
    /// <param name="convention">The element-name convention to apply.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convention"/> is <see langword="null"/>.</exception>
    IConventionsBuilder UseElementNameConvention(IConvention convention);

    /// <summary>
    /// Controls whether <see langword="null"/> properties are written to BSON documents.
    /// </summary>
    /// <param name="ignore">When <see langword="true"/> (default), null properties are omitted.</param>
    /// <returns>This builder for chaining.</returns>
    IConventionsBuilder IgnoreIfNull(bool ignore = true);

    /// <summary>
    /// Controls whether extra JSON/BSON elements not mapped on the type are ignored during deserialization.
    /// </summary>
    /// <param name="ignore">When <see langword="true"/> (default), unmapped elements are ignored.</param>
    /// <returns>This builder for chaining.</returns>
    IConventionsBuilder IgnoreExtraElements(bool ignore = true);

    /// <summary>
    /// Adds a standalone convention to the default convention pack.
    /// </summary>
    /// <param name="convention">The convention instance to register.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="convention"/> is <see langword="null"/>.</exception>
    IConventionsBuilder AddConvention(IConvention convention);

    /// <summary>
    /// Registers a named convention pack that applies only to types matched by <paramref name="filter"/>.
    /// </summary>
    /// <param name="name">Unique pack name; must not be the reserved default pack name.</param>
    /// <param name="pack">The convention pack to register.</param>
    /// <param name="filter">Selects document types that receive this pack.</param>
    /// <returns>This builder for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null, whitespace, or reserved.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pack"/> or <paramref name="filter"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when <paramref name="name"/> was already registered.</exception>
    IConventionsBuilder AddConventionPack(string name, IConventionPack pack, Func<Type, bool> filter);
}