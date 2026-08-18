using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace Dilcore.MongoDB.Descriptors;

internal sealed record AdditionalConventionPack(
    string Name,
    IConventionPack Pack,
    Func<Type, bool> Filter);

internal sealed record ConventionsDescriptor(
    BsonType EnumRepresentation,
    IConvention ElementNameConvention,
    bool IgnoreIfNull,
    bool IgnoreExtraElements,
    IReadOnlyList<IConvention> AdditionalConventions,
    IReadOnlyList<AdditionalConventionPack> AdditionalPacks)
{
    public static ConventionsDescriptor CreateDefault() => new(
        BsonType.String,
        new CamelCaseElementNameConvention(),
        IgnoreIfNull: true,
        IgnoreExtraElements: true,
        AdditionalConventions: [],
        AdditionalPacks: []);
}
