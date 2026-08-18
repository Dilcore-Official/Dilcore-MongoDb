using Dilcore.MongoDB.Descriptors;
using MongoDB.Bson.Serialization.Conventions;

namespace Dilcore.MongoDB.Internal;

internal static class MongoConventionRegistrar
{
    internal const string DefaultPackName = "Dilcore.MongoDB.DefaultConventions";

    private static readonly object Gate = new();
    private static string? _registeredSignature;
    private static IReadOnlyList<string>? _registeredPackNames;

    public static void EnsureRegistered(ConventionsDescriptor conventions)
    {
        ArgumentNullException.ThrowIfNull(conventions);
        var signature = ComputeSignature(conventions);

        if (_registeredSignature is not null)
        {
            ThrowIfConflict(_registeredSignature, signature);
            return;
        }

        lock (Gate)
        {
            if (_registeredSignature is not null)
            {
                ThrowIfConflict(_registeredSignature, signature);
                return;
            }

            var pack = new ConventionPack
            {
                new EnumRepresentationConvention(conventions.EnumRepresentation),
                conventions.ElementNameConvention,
                new IgnoreIfNullConvention(conventions.IgnoreIfNull),
                new IgnoreExtraElementsConvention(conventions.IgnoreExtraElements)
            };

            foreach (var additional in conventions.AdditionalConventions)
            {
                pack.Add(additional);
            }

            ConventionRegistry.Register(DefaultPackName, pack, _ => true);

            var packNames = new List<string> { DefaultPackName };
            foreach (var additionalPack in conventions.AdditionalPacks)
            {
                ConventionRegistry.Register(additionalPack.Name, additionalPack.Pack, additionalPack.Filter);
                packNames.Add(additionalPack.Name);
            }

            _registeredPackNames = packNames;
            _registeredSignature = signature;
        }
    }

    internal static void Reset()
    {
        lock (Gate)
        {
            if (_registeredPackNames is not null)
            {
                foreach (var name in _registeredPackNames)
                {
                    ConventionRegistry.Remove(name);
                }
            }

            _registeredPackNames = null;
            _registeredSignature = null;
        }
    }

    private static void ThrowIfConflict(string registered, string requested)
    {
        if (registered == requested)
        {
            return;
        }

        throw new InvalidOperationException(
            "Conflicting MongoDB serialization conventions were requested. " +
            "ConventionRegistry is process-wide; every AddMongoDb call must use the same ConfigureConventions settings. " +
            $"Already registered: '{registered}'. Requested: '{requested}'.");
    }

    private static string ComputeSignature(ConventionsDescriptor conventions)
    {
        var additionalConventionTypes = string.Join(
            ",",
            conventions.AdditionalConventions.Select(c => c.GetType().FullName));
        var additionalPacks = string.Join(
            ",",
            conventions.AdditionalPacks.Select(p => $"{p.Name}:{p.Pack.GetType().FullName}"));

        return string.Join(
            "|",
            conventions.EnumRepresentation,
            conventions.ElementNameConvention.GetType().FullName,
            conventions.IgnoreIfNull,
            conventions.IgnoreExtraElements,
            additionalConventionTypes,
            additionalPacks);
    }
}
