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
        // Arrange
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

        var abstractionTypes = typeof(IDocumentEntity).Assembly.GetExportedTypes();
        var primaryTypes = typeof(ServiceCollectionExtensions).Assembly.GetExportedTypes();
        var exportedTypes = abstractionTypes.Concat(primaryTypes);

        // Act
        var typeNames = exportedTypes
            .Select(type => type.Name)
            .ToHashSet(StringComparer.Ordinal);

        // Assert
        foreach (var name in forbidden)
        {
            typeNames.ShouldNotContain(name);
        }
    }

    [Test]
    public void BulkAndProjectionConcreteTypes_AreInternal()
    {
        // Arrange
        var primary = typeof(ServiceCollectionExtensions).Assembly;

        // Act
        var bulk = primary.GetTypes().Single(t => t.Name.StartsWith("GenericMongoDbBulkRepository", StringComparison.Ordinal));
        var projection = primary.GetTypes().Single(t => t.Name.StartsWith("GenericMongoDbProjectionRepository", StringComparison.Ordinal));

        // Assert
        bulk.IsPublic.ShouldBeFalse();
        projection.IsPublic.ShouldBeFalse();
    }

    [Test]
    public void NoPublicServicesField_OnPublicTypes()
    {
        // Arrange
        var assemblies = new[]
        {
            typeof(IDocumentEntity).Assembly,
            typeof(ServiceCollectionExtensions).Assembly
        };

        var types = assemblies.SelectMany(assembly => assembly.GetExportedTypes());

        // Act & Assert
        foreach (var type in types)
        {
            var servicesField = type.GetField("Services", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            servicesField.ShouldBeNull($"{type.FullName} exposes public Services field");
        }
    }

    [Test]
    public void RepositoryInterfaces_AreInAbstractions()
    {
        // Arrange
        var abstractions = typeof(IDocumentEntity).Assembly;

        // Act & Assert
        typeof(IGenericRepository<>).Assembly.ShouldBe(abstractions);
        typeof(IGenericBulkRepository<>).Assembly.ShouldBe(abstractions);
        typeof(IGenericProjectionRepository<>).Assembly.ShouldBe(abstractions);
        typeof(IRepositoryResolver).Assembly.ShouldBe(abstractions);
    }

    [Test]
    public void NamespacePrefixResolver_IsInAbstractions()
    {
        // Arrange
        var abstractions = typeof(IDocumentEntity).Assembly;

        // Act & Assert
        typeof(Dilcore.MongoDB.Abstractions.Namespace.INamespacePrefixResolver).Assembly
            .ShouldBe(abstractions);
    }
}