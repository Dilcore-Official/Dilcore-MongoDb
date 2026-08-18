using Dilcore.MongoDB.Descriptors;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Conventions;

namespace Dilcore.MongoDB.DependencyInjection;

internal sealed class ConventionsBuilder : IConventionsBuilder
{
    private BsonType _enumRepresentation = BsonType.String;
    private IConvention _elementNameConvention = new CamelCaseElementNameConvention();
    private bool _ignoreIfNull = true;
    private bool _ignoreExtraElements = true;
    private readonly List<IConvention> _additionalConventions = [];
    private readonly List<AdditionalConventionPack> _additionalPacks = [];
    private readonly HashSet<string> _packNames = new(StringComparer.Ordinal);

    public IConventionsBuilder UseEnumRepresentation(BsonType representation)
    {
        switch (representation)
        {
            case BsonType.String:
            case BsonType.Int32:
            case BsonType.Int64:
                _enumRepresentation = representation;
                return this;
            default:
                throw new ArgumentException(
                    $"Enum representation '{representation}' is not supported. Use BsonType.String, BsonType.Int32, or BsonType.Int64.",
                    nameof(representation));
        }
    }

    public IConventionsBuilder UseElementNameConvention(IConvention convention)
    {
        ArgumentNullException.ThrowIfNull(convention);
        _elementNameConvention = convention;
        return this;
    }

    public IConventionsBuilder IgnoreIfNull(bool ignore = true)
    {
        _ignoreIfNull = ignore;
        return this;
    }

    public IConventionsBuilder IgnoreExtraElements(bool ignore = true)
    {
        _ignoreExtraElements = ignore;
        return this;
    }

    public IConventionsBuilder AddConvention(IConvention convention)
    {
        ArgumentNullException.ThrowIfNull(convention);
        _additionalConventions.Add(convention);
        return this;
    }

    public IConventionsBuilder AddConventionPack(string name, IConventionPack pack, Func<Type, bool> filter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(pack);
        ArgumentNullException.ThrowIfNull(filter);

        if (!_packNames.Add(name))
        {
            throw new InvalidOperationException(
                $"Duplicate convention pack name '{name}'. Each AddConventionPack name must be unique.");
        }

        _additionalPacks.Add(new AdditionalConventionPack(name, pack, filter));
        return this;
    }

    internal ConventionsDescriptor Build() => new(
        _enumRepresentation,
        _elementNameConvention,
        _ignoreIfNull,
        _ignoreExtraElements,
        _additionalConventions.ToList(),
        _additionalPacks.ToList());
}
