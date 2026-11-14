using MongoDB.Bson.Serialization.Conventions;

namespace Dilcore.MongoDB.Descriptors;

internal sealed record AdditionalConventionPack(
    string Name,
    IConventionPack Pack,
    Func<Type, bool> Filter);