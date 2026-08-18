using Dilcore.MongoDB.Descriptors;

namespace Dilcore.MongoDB.DependencyInjection;

internal sealed class MongoDbBuilder : IMongoDbBuilder
{
    private readonly List<ClusterDescriptor> _clusters = [];
    private readonly List<DatabaseDescriptor> _databases = [];
    private readonly List<DocumentBindingDescriptor> _bindings = [];
    private readonly HashSet<string> _clusterNames = new(StringComparer.Ordinal);
    private readonly HashSet<string> _databaseNames = new(StringComparer.Ordinal);
    private readonly HashSet<string> _bindingNames = new(StringComparer.Ordinal);
    private readonly ConventionsBuilder _conventions = new();
    private bool _conventionsConfigured;

    public IMongoDbBuilder ConfigureConventions(Action<IConventionsBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (_conventionsConfigured)
        {
            throw new InvalidOperationException(
                "ConfigureConventions has already been called. Call it at most once per AddMongoDb.");
        }

        _conventionsConfigured = true;
        configure(_conventions);
        return this;
    }

    public IMongoDbBuilder AddCluster(string name, Action<IMongoClusterBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        if (!_clusterNames.Add(name))
        {
            throw new InvalidOperationException(
                $"Duplicate cluster key '{name}'. Each AddCluster name must be unique.");
        }

        var builder = new MongoClusterBuilder();
        configure(builder);
        _clusters.Add(builder.Build(name));
        return this;
    }

    public IMongoDbBuilder AddDatabase(string name, Action<IMongoDatabaseBuilder> configure)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(configure);

        if (!_databaseNames.Add(name))
        {
            throw new InvalidOperationException(
                $"Duplicate database key '{name}'. Each AddDatabase name must be unique.");
        }

        var builder = new MongoDatabaseBuilder();
        configure(builder);
        _databases.Add(builder.Build(name));

        foreach (var binding in builder.Bindings)
        {
            if (!_bindingNames.Add(binding.Key.Name))
            {
                throw new InvalidOperationException(
                    $"Duplicate document binding key '{binding.Key.Name}'. Each AddDocumentBinding name must be unique.");
            }

            _bindings.Add(binding);
        }

        return this;
    }

    internal MongoRegistrationGraph Build()
    {
        Validate();
        return new MongoRegistrationGraph
        {
            Clusters = _clusters.ToList(),
            Databases = _databases.ToList(),
            Bindings = _bindings.ToList(),
            Conventions = _conventions.Build()
        };
    }

    private void Validate()
    {
        if (_clusters.Count == 0)
        {
            throw new InvalidOperationException(
                "AddMongoDb requires at least one AddCluster(...).");
        }

        foreach (var database in _databases.Where(d => _clusters.All(c => !c.Key.Equals(d.ClusterKey))))
        {
            throw new InvalidOperationException(
                $"Database '{database.Key.Name}' references unknown cluster '{database.ClusterKey.Name}'. " +
                "Call AddCluster before AddDatabase, and ensure OnCluster matches a registered cluster key.");
        }

        foreach (var binding in _bindings)
        {
            if (_databases.All(d => !d.Key.Equals(binding.DatabaseKey)))
            {
                throw new InvalidOperationException(
                    $"Document binding '{binding.Key.Name}' references unknown database '{binding.DatabaseKey.Name}'.");
            }

            if (string.IsNullOrWhiteSpace(binding.CollectionName))
            {
                throw new InvalidOperationException(
                    $"Document binding '{binding.Key.Name}' has an empty collection name.");
            }
        }

        var orphanClusters = _clusters
            .Where(c => _databases.All(d => !d.ClusterKey.Equals(c.Key)))
            .Select(c => c.Key.Name)
            .ToList();

        if (orphanClusters.Count > 0)
        {
            throw new InvalidOperationException(
                $"Cluster(s) [{string.Join(", ", orphanClusters)}] have no databases. " +
                "Either AddDatabase(...).OnCluster(...) for each cluster or remove unused clusters.");
        }
    }
}
