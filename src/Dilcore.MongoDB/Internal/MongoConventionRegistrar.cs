using System.Runtime.CompilerServices;
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

            if (conventions.AdditionalPacks.Any(additionalPack =>
                    additionalPack.Name.Equals(DefaultPackName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    $"Convention pack name '{DefaultPackName}' is reserved for the default pack. Choose a different name.");
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
        var additionalConventions = string.Join(
            ",",
            conventions.AdditionalConventions.Select(DescribeConvention));
        var additionalPacks = string.Join(
            ",",
            conventions.AdditionalPacks.Select(DescribePack));

        return string.Join(
            "|",
            conventions.EnumRepresentation,
            conventions.ElementNameConvention.GetType().FullName,
            conventions.IgnoreIfNull,
            conventions.IgnoreExtraElements,
            additionalConventions,
            additionalPacks);
    }

    private static string DescribeConvention(IConvention convention)
    {
        var type = convention.GetType();
        var equalsMethod = type.GetMethod(nameof(Equals), [typeof(object)]);
        var hasValueEquality = equalsMethod is not null && equalsMethod.DeclaringType != typeof(object);
        return hasValueEquality
            ? $"{type.FullName}:{convention}"
            : $"{type.FullName}#{RuntimeHelpers.GetHashCode(convention)}";
    }

    private static string DescribeFilter(Func<Type, bool> filter) =>
        $"{filter.Method.DeclaringType?.FullName}.{filter.Method.Name}#{(filter.Target is null ? 0 : RuntimeHelpers.GetHashCode(filter.Target))}";

    private static string DescribePack(AdditionalConventionPack pack) =>
        $"{pack.Name}:[{string.Join(",", pack.Pack.Conventions.Select(DescribeConvention))}]:{DescribeFilter(pack.Filter)}";
}
