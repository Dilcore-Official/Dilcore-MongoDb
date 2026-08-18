using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Policies;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;

namespace Dilcore.MongoDB.UnitTests;

public class MongoDbBuilderValidationTests
{
    [Test]
    public void AddMongoDb_EmptyConnectionString_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<ArgumentException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString(" "))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));

        ex.ParamName.ShouldBe("connectionString");
    }

    [Test]
    public void AddMongoDb_PoolSizeLessOrEqualZero_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<ArgumentException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c
                    .UseConnectionString("mongodb://localhost")
                    .UseMaxConnectionPoolSize(0))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));

        ex.ParamName.ShouldBe("maxConnectionPoolSize");
    }

    [Test]
    public void AddMongoDb_DuplicateCluster_Throws()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost2"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));
    }

    [Test]
    public void AddMongoDb_DuplicateDatabase_Throws()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db => db.OnCluster("primary"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));
    }

    [Test]
    public void AddMongoDb_OrphanDatabase_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("missing");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));

        ex.Message.ShouldContain("unknown cluster");
    }

    [Test]
    public void AddMongoDb_DuplicateBindingAcrossDatabases_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })
                .AddDatabase("archive", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));

        ex.Message.ShouldContain("Duplicate document binding key");
    }

    [Test]
    public void AddMongoDb_OrphanCluster_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddCluster("unused", c => c.UseConnectionString("mongodb://localhost2"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));

        ex.Message.ShouldContain("unused");
    }

    [Test]
    public void AddMongoDb_MissingCollectionName_Throws()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", _ => { });
                })));
    }

    [Test]
    public void AddMongoDb_DuplicateNamespacePrefixResolverOnDatabase_Throws()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.WithNamespacePrefixResolver<NoOpPrefixResolver>();
                    db.WithNamespacePrefixResolver<NoOpPrefixResolver>();
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));
    }

    [Test]
    public void AddMongoDb_DuplicateNamespacePrefixResolverOnBinding_Throws()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d =>
                    {
                        d.WithCollectionName("orders");
                        d.WithNamespacePrefixResolver<NoOpPrefixResolver>();
                        d.WithNamespacePrefixResolver<NoOpPrefixResolver>();
                    });
                })));
    }

    [Test]
    public void AddMongoDb_SoftDeleteWithoutISoftDeletable_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<MinimalDoc>("orders", d => d
                        .WithCollectionName("orders")
                        .WithSoftDelete());
                })));

        ex.Message.ShouldContain(nameof(ISoftDeletable));
    }

    [Test]
    public void AddMongoDb_GuidIdGenerationOnNonGuidId_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<ObjectIdDoc>("orders", d => d
                        .WithCollectionName("orders")
                        .WithGuidIdGeneration(GuidIdGenerationStrategy.SequentialVersion7));
                })));

        ex.Message.ShouldContain("IDocumentEntity<Guid>");
    }

    private sealed class NoOpPrefixResolver : Dilcore.MongoDB.Abstractions.Namespace.INamespacePrefixResolver
    {
        public Task<FluentResults.Result<string?>> ResolveAsync(
            Dilcore.MongoDB.Abstractions.Namespace.NamespaceResolutionRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(FluentResults.Result.Ok<string?>(null));
    }

    private class TestDoc : IDocumentEntity<Guid>, IHasConcurrencyToken, ISoftDeletable, IAuditableDocument
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private class MinimalDoc : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }

    private class ObjectIdDoc : IDocumentEntity<ObjectId>
    {
        public ObjectId Id { get; set; }
    }
}
