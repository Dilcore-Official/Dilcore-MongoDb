using System.Reflection;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Abstractions.Repositories;
using Dilcore.MongoDB.Extensions;

namespace Dilcore.MongoDB.ArchitectureTests;

public class PublicApiBoundaryTests
{
    [Test]
    public void DeadTypes_AreNotPublic()
    {
        var forbidden = new[]
        {
            "IBsonDocumentRepository",
            "IBsonDocumentCollectionFactory",
            "BsonDocumentCollectionFactory",
            "MongoDbIndexFactory",
            "IDocumentPrefixProvider",
            "IDocumentDatabasePrefixProvider",
            "IDocumentCollectionPrefixProvider",
            "MongoClientProvider",
            "ITenantAccessor",
            "TenantRequirement",
            "TenantNamespaceSegmentContributor"
        };

        var types = typeof(IDocumentEntity).Assembly.GetExportedTypes()
            .Concat(typeof(ServiceCollectionExtensions).Assembly.GetExportedTypes())
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var name in forbidden)
        {
            types.ShouldNotContain(name);
        }
    }

    [Test]
    public void BulkAndProjectionConcreteTypes_AreInternal()
    {
        var primary = typeof(ServiceCollectionExtensions).Assembly;
        var bulk = primary.GetTypes().Single(t => t.Name.StartsWith("GenericMongoDbBulkRepository", StringComparison.Ordinal));
        var projection = primary.GetTypes().Single(t => t.Name.StartsWith("GenericMongoDbProjectionRepository", StringComparison.Ordinal));

        bulk.IsPublic.ShouldBeFalse();
        projection.IsPublic.ShouldBeFalse();
    }

    [Test]
    public void NoPublicServicesField_OnPublicTypes()
    {
        var assemblies = new[]
        {
            typeof(IDocumentEntity).Assembly,
            typeof(ServiceCollectionExtensions).Assembly
        };

        foreach (var assembly in assemblies)
        {
            foreach (var type in assembly.GetExportedTypes())
            {
                var servicesField = type.GetField("Services", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
                servicesField.ShouldBeNull($"{type.FullName} exposes public Services field");
            }
        }
    }

    [Test]
    public void RepositoryInterfaces_AreInAbstractions()
    {
        typeof(IGenericRepository<>).Assembly.ShouldBe(typeof(IDocumentEntity).Assembly);
        typeof(IGenericBulkRepository<>).Assembly.ShouldBe(typeof(IDocumentEntity).Assembly);
        typeof(IGenericProjectionRepository<>).Assembly.ShouldBe(typeof(IDocumentEntity).Assembly);
    }
}
