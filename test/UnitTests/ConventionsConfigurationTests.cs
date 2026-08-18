using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Descriptors;
using Dilcore.MongoDB.Extensions;
using Dilcore.MongoDB.Internal;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Dilcore.MongoDB.UnitTests;

[NonParallelizable]
public class ConventionsConfigurationTests
{
    [SetUp]
    public void SetUp() => MongoConventionRegistrar.Reset();

    [TearDown]
    public void TearDown() => MongoConventionRegistrar.Reset();

    [Test]
    public void AddMongoDb_WithoutConfigureConventions_UsesDefaultSerialization()
    {
        var services = new ServiceCollection();
        RegisterMinimal(services);

        var bson = Serialize(new DefaultProbeDocument
        {
            Status = ProbeStatus.Active,
            Optional = null
        });

        bson["status"].BsonType.ShouldBe(BsonType.String);
        bson["status"].AsString.ShouldBe(nameof(ProbeStatus.Active));
        bson.Contains("optional").ShouldBeFalse();
    }

    [Test]
    public void AddMongoDb_ConfigureConventions_EnumAsInt_SerializesEnumAsInt32()
    {
        var services = new ServiceCollection();
        RegisterMinimal(services, mongo => mongo.ConfigureConventions(c =>
            c.UseEnumRepresentation(BsonType.Int32)));

        var bson = Serialize(new Int32ProbeDocument { Status = ProbeStatus.Inactive });

        bson["status"].BsonType.ShouldBe(BsonType.Int32);
        bson["status"].AsInt32.ShouldBe((int)ProbeStatus.Inactive);
    }

    [Test]
    public void AddMongoDb_ConfigureConventionsCalledTwice_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .ConfigureConventions(_ => { })
                .ConfigureConventions(_ => { })
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));

        ex.Message.ShouldContain("ConfigureConventions");
    }

    [Test]
    public void AddMongoDb_DuplicateConventionPackName_Throws()
    {
        var services = new ServiceCollection();
        var pack = new ConventionPack();

        var ex = Should.Throw<InvalidOperationException>(() =>
            services.AddMongoDb(mongo => mongo
                .ConfigureConventions(c => c
                    .AddConventionPack("custom", pack, _ => true)
                    .AddConventionPack("custom", pack, _ => true))
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));

        ex.Message.ShouldContain("Duplicate convention pack name");
    }

    [Test]
    public void AddMongoDb_ReservedDefaultConventionPackName_Throws()
    {
        var services = new ServiceCollection();
        var pack = new ConventionPack();

        var ex = Should.Throw<ArgumentException>(() =>
            services.AddMongoDb(mongo => mongo
                .ConfigureConventions(c => c
                    .AddConventionPack(MongoConventionRegistrar.DefaultPackName, pack, _ => true))
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));

        ex.ParamName.ShouldBe("name");
        ex.Message.ShouldContain("reserved");
    }

    [Test]
    public void EnsureRegistered_ReservedDefaultConventionPackName_ThrowsWithoutRegistering()
    {
        var conventions = ConventionsDescriptor.CreateDefault() with
        {
            AdditionalPacks =
            [
                new AdditionalConventionPack(
                    MongoConventionRegistrar.DefaultPackName,
                    new ConventionPack(),
                    _ => true)
            ]
        };

        var ex = Should.Throw<InvalidOperationException>(() =>
            MongoConventionRegistrar.EnsureRegistered(conventions));

        ex.Message.ShouldContain("reserved");

        Should.NotThrow(() => MongoConventionRegistrar.EnsureRegistered(ConventionsDescriptor.CreateDefault()));
    }

    [Test]
    public void AddMongoDb_CalledTwiceWithSameConventions_IsIdempotent()
    {
        var first = new ServiceCollection();
        RegisterMinimal(first);

        var second = new ServiceCollection();
        Should.NotThrow(() => RegisterMinimal(second));
    }

    [Test]
    public void AddMongoDb_CalledTwiceWithConflictingConventions_Throws()
    {
        var first = new ServiceCollection();
        RegisterMinimal(first);

        var second = new ServiceCollection();
        var ex = Should.Throw<InvalidOperationException>(() =>
            RegisterMinimal(second, mongo => mongo.ConfigureConventions(c =>
                c.UseEnumRepresentation(BsonType.Int32))));

        ex.Message.ShouldContain("Conflicting MongoDB serialization conventions");
        ex.Message.ShouldContain("ConfigureConventions");
    }

    [Test]
    public void AddMongoDb_CalledTwiceWithSameCustomConventionInstance_IsIdempotent()
    {
        var convention = new IgnoreIfDefaultConvention(true);
        var pack = new ConventionPack();
        Func<Type, bool> filter = static _ => true;

        var first = new ServiceCollection();
        RegisterMinimal(first, mongo => mongo.ConfigureConventions(c => c
            .AddConvention(convention)
            .AddConventionPack("custom", pack, filter)));

        var second = new ServiceCollection();
        Should.NotThrow(() => RegisterMinimal(second, mongo => mongo.ConfigureConventions(c => c
            .AddConvention(convention)
            .AddConventionPack("custom", pack, filter))));
    }

    [Test]
    public void AddMongoDb_CalledTwiceWithSeparateCustomConventionInstances_Throws()
    {
        var first = new ServiceCollection();
        RegisterMinimal(first, mongo => mongo.ConfigureConventions(c =>
            c.AddConvention(new IgnoreIfDefaultConvention(true))));

        var second = new ServiceCollection();
        var ex = Should.Throw<InvalidOperationException>(() =>
            RegisterMinimal(second, mongo => mongo.ConfigureConventions(c =>
                c.AddConvention(new IgnoreIfDefaultConvention(true)))));

        ex.Message.ShouldContain("Conflicting MongoDB serialization conventions");
    }

    [Test]
    public void UseEnumRepresentation_UnsupportedBsonType_Throws()
    {
        var services = new ServiceCollection();

        var ex = Should.Throw<ArgumentException>(() =>
            services.AddMongoDb(mongo => mongo
                .ConfigureConventions(c => c.UseEnumRepresentation(BsonType.Boolean))
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                })));

        ex.ParamName.ShouldBe("representation");
    }

    private static void RegisterMinimal(
        IServiceCollection services,
        Action<DependencyInjection.IMongoDbBuilder>? configureConventions = null)
    {
        services.AddMongoDb(mongo =>
        {
            configureConventions?.Invoke(mongo);
            mongo
                .AddCluster("primary", c => c.UseConnectionString("mongodb://localhost"))
                .AddDatabase("app", db =>
                {
                    db.OnCluster("primary");
                    db.AddDocumentBinding<TestDoc>("orders", d => d.WithCollectionName("orders"));
                });
        });
    }

    private static BsonDocument Serialize<T>(T document)
    {
        using var stream = new MemoryStream();
        using (var writer = new BsonBinaryWriter(stream))
        {
            BsonSerializer.Serialize(writer, document);
        }

        stream.Position = 0;
        using var reader = new BsonBinaryReader(stream);
        return BsonSerializer.Deserialize<BsonDocument>(reader);
    }

    private enum ProbeStatus
    {
        Active,
        Inactive
    }

    private sealed class DefaultProbeDocument
    {
        public ProbeStatus Status { get; set; }
        public string? Optional { get; set; }
    }

    private sealed class Int32ProbeDocument
    {
        public ProbeStatus Status { get; set; }
    }

    private sealed class TestDoc : IDocumentEntity<Guid>
    {
        public Guid Id { get; set; }
    }
}
