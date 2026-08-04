using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.DependencyInjection;
using Dilcore.MongoDB.Extensions;
using Microsoft.Extensions.DependencyInjection;

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
                .AddDatabase("app", db => db.OnCluster("primary"))
                .AddDocumentBinding<TestDoc>("orders", d => d
                    .InDatabase("app")
                    .WithCollectionName("orders"))));

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
                .AddDatabase("app", db => db.OnCluster("primary"))
                .AddDocumentBinding<TestDoc>("orders", d => d
                    .InDatabase("app")
                    .WithCollectionName("orders"))));

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
                .AddDatabase("app", db => db.OnCluster("primary"))
                .AddDocumentBinding<TestDoc>("orders", d => d
                    .InDatabase("app")
                    .WithCollectionName("orders"))));
    }

    [Test]
    public void AddMongoDb_DuplicateDatabase_Throws()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db => db.OnCluster("primary"))
                .AddDatabase("app", db => db.OnCluster("primary"))
                .AddDocumentBinding<TestDoc>("orders", d => d
                    .InDatabase("app")
                    .WithCollectionName("orders"))));
    }

    [Test]
    public void AddMongoDb_OrphanDatabase_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db => db.OnCluster("missing"))
                .AddDocumentBinding<TestDoc>("orders", d => d
                    .InDatabase("app")
                    .WithCollectionName("orders"))));

        ex.Message.ShouldContain("unknown cluster");
    }

    [Test]
    public void AddMongoDb_OrphanBinding_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db => db.OnCluster("primary"))
                .AddDocumentBinding<TestDoc>("orders", d => d
                    .InDatabase("missing")
                    .WithCollectionName("orders"))));

        ex.Message.ShouldContain("unknown database");
    }

    [Test]
    public void AddMongoDb_OrphanCluster_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddCluster("unused", c => c.UseConnectionString("mongodb://localhost2"))
                .AddDatabase("app", db => db.OnCluster("primary"))
                .AddDocumentBinding<TestDoc>("orders", d => d
                    .InDatabase("app")
                    .WithCollectionName("orders"))));

        ex.Message.ShouldContain("unused");
    }

    [Test]
    public void AddMongoDb_MissingCollectionName_Throws()
    {
        var services = new ServiceCollection();

        Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db => db.OnCluster("primary"))
                .AddDocumentBinding<TestDoc>("orders", d => d.InDatabase("app"))));
    }

    private class TestDoc : IDocumentEntity
    {
        public Guid Id { get; set; }
        public long ETag { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
