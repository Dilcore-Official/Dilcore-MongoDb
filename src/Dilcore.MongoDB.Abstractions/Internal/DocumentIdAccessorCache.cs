using System.Reflection;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Abstractions.Internal;

internal static class DocumentIdAccessorCache
{
    private static readonly MethodInfo CreateCoreMethod =
        typeof(DocumentIdAccessorCache).GetMethod(
            nameof(CreateCore),
            BindingFlags.NonPublic | BindingFlags.Static)!;

    public static IDocumentIdAccessor<TDocument> Get<TDocument>()
        where TDocument : class, IDocumentEntity
        => Holder<TDocument>.Instance;

    public static Type ResolveIdentifierType(Type documentType)
    {
        ArgumentNullException.ThrowIfNull(documentType);

        var idInterface = documentType.GetInterfaces()
            .FirstOrDefault(iface => iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IDocumentEntity<>));

        if (idInterface is null)
        {
            throw new InvalidOperationException(
                $"Type '{documentType.FullName}' must implement IDocumentEntity<TId>.");
        }

        return idInterface.GetGenericArguments()[0];
    }

    private static class Holder<TDocument>
        where TDocument : class, IDocumentEntity
    {
        public static readonly IDocumentIdAccessor<TDocument> Instance = Create<TDocument>();
    }

    private static IDocumentIdAccessor<TDocument> Create<TDocument>()
        where TDocument : class, IDocumentEntity
    {
        var idType = ResolveIdentifierType(typeof(TDocument));
        var factory = CreateCoreMethod.MakeGenericMethod(typeof(TDocument), idType);
        return (IDocumentIdAccessor<TDocument>)factory.Invoke(null, null)!;
    }

    private static DocumentIdAccessor<TDocument, TId> CreateCore<TDocument, TId>()
        where TDocument : class, IDocumentEntity
        => new();
}