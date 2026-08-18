using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace Dilcore.MongoDB.DependencyInjection;

public interface IConventionsBuilder
{
    IConventionsBuilder UseEnumRepresentation(BsonType representation);

    IConventionsBuilder UseElementNameConvention(IConvention convention);

    IConventionsBuilder IgnoreIfNull(bool ignore = true);

    IConventionsBuilder IgnoreExtraElements(bool ignore = true);

    IConventionsBuilder AddConvention(IConvention convention);

    IConventionsBuilder AddConventionPack(string name, IConventionPack pack, Func<Type, bool> filter);
}
