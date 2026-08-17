using Dilcore.MongoDB.Abstractions.Ownership;
using Dilcore.MongoDB.Descriptors;
using MongoDB.Driver;

namespace Dilcore.MongoDB.DependencyInjection;

internal sealed class MongoClusterBuilder : IMongoClusterBuilder
{
    private string? _connectionString;
    private int _maxConnectionPoolSize = MongoDbDefaults.MaxConnectionPoolSize;
    private IMongoClient? _existingClient;
    private MongoClientOwnership _ownership = MongoClientOwnership.LibraryOwned;
    private bool _hasSource;

    public IMongoClusterBuilder UseConnectionString(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        if (_existingClient is not null)
        {
            throw new InvalidOperationException(
                "Cannot call UseConnectionString after UseExistingClient on the same cluster.");
        }

        _connectionString = connectionString;
        _hasSource = true;
        return this;
    }

    public IMongoClusterBuilder UseMaxConnectionPoolSize(int maxConnectionPoolSize)
    {
        if (maxConnectionPoolSize <= 0)
        {
            throw new ArgumentException(
                "Max connection pool size should be greater than 0.",
                nameof(maxConnectionPoolSize));
        }

        _maxConnectionPoolSize = maxConnectionPoolSize;
        return this;
    }

    public IMongoClusterBuilder UseExistingClient(IMongoClient client, MongoClientOwnership ownership)
    {
        ArgumentNullException.ThrowIfNull(client);

        if (_connectionString is not null)
        {
            throw new InvalidOperationException(
                "Cannot call UseExistingClient after UseConnectionString on the same cluster.");
        }

        _existingClient = client;
        _ownership = ownership;
        _hasSource = true;
        return this;
    }

    internal ClusterDescriptor Build(string name)
    {
        if (!_hasSource)
        {
            throw new InvalidOperationException(
                $"Cluster '{name}' must configure UseConnectionString or UseExistingClient.");
        }

        return new ClusterDescriptor(
            new Abstractions.Keys.MongoClusterKey(name),
            _connectionString,
            _maxConnectionPoolSize,
            _existingClient,
            _ownership);
    }
}
