using System.Security.Authentication;
using Dilcore.MongoDB.Abstractions.Ownership;
using Dilcore.MongoDB.Descriptors;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;

namespace Dilcore.MongoDB.Internal;

internal sealed class MongoClientHolder : IDisposable
{
    private static readonly object SerializationLock = new();
    private static bool _serializationConfigured;

    private readonly IMongoClient _client;
    private readonly MongoClientOwnership _ownership;
    private bool _disposed;

    public MongoClientHolder(ClusterDescriptor descriptor)
    {
        EnsureBsonSerialization();

        _ownership = descriptor.Ownership;

        if (descriptor.ExistingClient is not null)
        {
            _client = descriptor.ExistingClient;
            return;
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.ConnectionString);

        var settings = MongoClientSettings.FromUrl(new MongoUrl(descriptor.ConnectionString));
        settings.SslSettings = new SslSettings
        {
            EnabledSslProtocols = SslProtocols.Tls12
        };
        settings.MaxConnectionPoolSize = descriptor.MaxConnectionPoolSize;
        settings.MaxConnectionIdleTime = TimeSpan.FromMinutes(MongoDbDefaults.MaxConnectionIdleTimeInMinutes);

        _client = new MongoClient(settings);
        _ownership = MongoClientOwnership.LibraryOwned;
    }

    public IMongoClient Client => _client;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_ownership == MongoClientOwnership.ExternalOwned)
        {
            return;
        }

        if (_client is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    private static void EnsureBsonSerialization()
    {
        if (_serializationConfigured)
        {
            return;
        }

        lock (SerializationLock)
        {
            if (_serializationConfigured)
            {
                return;
            }

            BsonSerializer.RegisterSerializer(new GuidSerializer(GuidRepresentation.Standard));
            _serializationConfigured = true;
        }
    }
}
