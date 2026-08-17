using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Descriptors;

namespace Dilcore.MongoDB.DependencyInjection;

internal sealed class MongoDatabaseBuilder : IMongoDatabaseBuilder
{
    private string? _clusterName;
    private string? _namespacePrefix;
    private Type? _namespacePrefixResolverType;
    private readonly List<DocumentBindingDescriptor> _bindings = [];
    private readonly HashSet<string> _bindingNames = new(StringComparer.Ordinal);
    private readonly List<(string Name, Func<MongoDatabaseKey, DocumentBindingDescriptor> Materialize)> _pending = [];

    public IMongoDatabaseBuilder OnCluster(string clusterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(clusterName);
        _clusterName = clusterName;
        return this;
    }

    public IMongoDatabaseBuilder WithNamespacePrefix(string prefix)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix);
        _namespacePrefix = prefix;
        return this;
    }

    public IMongoDatabaseBuilder WithNamespacePrefixResolver<TResolver>()
        where TResolver : class, INamespacePrefixResolver
    {
        if (_namespacePrefixResolverType is not null)
        {
            throw new InvalidOperationException(
                "WithNamespacePrefixResolver can only be called once per database.");
        }

        _namespacePrefixResolverType = typeof(TResolver);
        return this;
    }

    public IMongoDatabaseBuilder AddDocumentBinding<TDocument>(
        string name,
        Action<IMongoDocumentBindingBuilder<TDocument>> configure)
        where TDocument : class, IDocumentEntity
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        if (!_bindingNames.Add(name))
        {
            throw new InvalidOperationException(
                $"Duplicate document binding key '{name}'. Each AddDocumentBinding name must be unique.");
        }

        var builder = new MongoDocumentBindingBuilder<TDocument>();
        configure(builder);
        _pending.Add((name, databaseKey => builder.Build(name, databaseKey)));
        return this;
    }

    internal DatabaseDescriptor Build(string name)
    {
        if (string.IsNullOrWhiteSpace(_clusterName))
        {
            throw new InvalidOperationException(
                $"Database '{name}' must call OnCluster(\"<cluster-name>\").");
        }

        var databaseKey = new MongoDatabaseKey(name);

        foreach (var pending in _pending)
        {
            _bindings.Add(pending.Materialize(databaseKey));
        }

        return new DatabaseDescriptor(
            databaseKey,
            new MongoClusterKey(_clusterName),
            _namespacePrefix,
            _namespacePrefixResolverType);
    }

    internal IReadOnlyList<DocumentBindingDescriptor> Bindings => _bindings;
}
