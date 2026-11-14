using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Keys;
using Dilcore.MongoDB.Abstractions.Namespace;
using Dilcore.MongoDB.Abstractions.Ownership;
using Dilcore.MongoDB.Descriptors;
using Dilcore.MongoDB.Internal;
using FluentResults;
using MongoDB.Driver;

namespace Dilcore.MongoDB.UnitTests;

public class MongoDbCollectionFactoryTests
{
    private static readonly MongoClusterKey ClusterKey = new("primary");
    private static readonly MongoDatabaseKey DatabaseKey = new("app");
    private static readonly MongoDocumentBindingKey BindingKey = new("orders");

    [Test]
    public async Task GetCollectionAsync_UnknownBindingKey_Fails()
    {
        var factory = CreateFactory(graph: CreateGraph(typeof(OrderDoc)));

        var result = await factory.GetCollectionAsync<OrderDoc>(new MongoDocumentBindingKey("missing"));

        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("Unknown document binding key");
        result.Errors[0].Message.ShouldContain("missing");
    }

    [Test]
    public async Task GetCollectionAsync_BindingTypeMismatch_Fails()
    {
        var factory = CreateFactory(graph: CreateGraph(typeof(OrderDoc)));

        var result = await factory.GetCollectionAsync<OtherDoc>(BindingKey);

        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("registered for 'OrderDoc'");
        result.Errors[0].Message.ShouldContain("not 'OtherDoc'");
    }

    [Test]
    public async Task GetCollectionAsync_DatabaseResolverFailed_PropagatesFailure()
    {
        var factory = CreateFactory(
            graph: CreateGraph(typeof(OrderDoc)),
            databaseResolver: new StubDatabaseResolver(Result.Fail<IMongoDatabase>("database unavailable")));

        var result = await factory.GetCollectionAsync<OrderDoc>(BindingKey);

        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("database unavailable");
    }

    [Test]
    public async Task GetCollectionAsync_NamespaceResolverFailed_PropagatesFailure()
    {
        var factory = CreateFactory(
            graph: CreateGraph(typeof(OrderDoc)),
            namespaceResolver: new StubNamespaceResolver(Result.Fail<string>("prefix missing")));

        var result = await factory.GetCollectionAsync<OrderDoc>(BindingKey);

        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("prefix missing");
    }

    [Test]
    public async Task GetCollectionAsync_BsonPath_DatabaseResolverFailed_PropagatesFailure()
    {
        var factory = CreateFactory(
            graph: CreateGraph(typeof(OrderDoc)),
            databaseResolver: new StubDatabaseResolver(Result.Fail<IMongoDatabase>("db down")));

        var result = await factory.GetCollectionAsync(DatabaseKey, "raw");

        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("db down");
    }

    [Test]
    public async Task GetCollectionAsync_BsonPath_NamespaceResolverFailed_PropagatesFailure()
    {
        var factory = CreateFactory(
            graph: CreateGraph(typeof(OrderDoc)),
            namespaceResolver: new StubNamespaceResolver(Result.Fail<string>("bad collection name")));

        var result = await factory.GetCollectionAsync(DatabaseKey, "raw");

        result.ShouldBeFailure();
        result.Errors[0].Message.ShouldContain("bad collection name");
    }

    private static MongoDbCollectionFactory CreateFactory(
        MongoRegistrationGraph graph,
        IMongoDatabaseResolver? databaseResolver = null,
        INamespaceResolver? namespaceResolver = null)
    {
        return new MongoDbCollectionFactory(
            graph,
            databaseResolver ?? new StubDatabaseResolver(Result.Ok<IMongoDatabase>(null!)),
            namespaceResolver ?? new StubNamespaceResolver(Result.Ok("orders")));
    }

    private static MongoRegistrationGraph CreateGraph(Type documentType)
    {
        return new MongoRegistrationGraph
        {
            Clusters =
            [
                new ClusterDescriptor(
                    ClusterKey,
                    ConnectionString: "mongodb://localhost",
                    MaxConnectionPoolSize: 100,
                    ExistingClient: null,
                    Ownership: MongoClientOwnership.LibraryOwned)
            ],
            Databases =
            [
                new DatabaseDescriptor(DatabaseKey, ClusterKey, NamespacePrefix: null)
            ],
            Bindings =
            [
                new DocumentBindingDescriptor(
                    BindingKey,
                    documentType,
                    DatabaseKey,
                    CollectionName: "orders",
                    SoftDeleteEnabled: false,
                    RegisterBulkRepository: false,
                    RegisterProjectionRepository: false,
                    NamespacePrefix: null,
                    Indices: null,
                    CollectionItemsTimeToLive: null,
                    TimeToLeavePropertySelector: null)
            ],
            Conventions = ConventionsDescriptor.CreateDefault()
        };
    }

    private sealed class StubDatabaseResolver(Result<IMongoDatabase> result) : IMongoDatabaseResolver
    {
        public Task<Result<IMongoDatabase>> GetDatabaseAsync(
            MongoDatabaseKey databaseKey,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    private sealed class StubNamespaceResolver(Result<string> result) : INamespaceResolver
    {
        public Task<Result<string>> ResolveAsync(
            NamespaceResolutionRequest request,
            CancellationToken cancellationToken = default)
            => Task.FromResult(result);
    }

    private sealed class OrderDoc : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    private sealed class OtherDoc : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }
}