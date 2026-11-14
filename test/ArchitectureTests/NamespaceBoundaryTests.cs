using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Extensions;

namespace Dilcore.MongoDB.ArchitectureTests;

public class NamespaceBoundaryTests
{
    [Test]
    public void PublicTypes_UseDilcoreMongoDBNamespaces()
    {
        // Arrange
        var assemblies = new[]
        {
            typeof(IDocumentEntity).Assembly,
            typeof(ServiceCollectionExtensions).Assembly,
            typeof(Dilcore.MongoDB.SystemTextJson.JsonBsonConvert).Assembly,
            typeof(Dilcore.MongoDB.NewtonsoftJson.JsonBsonConvert).Assembly
        };

        var types = assemblies.SelectMany(assembly => assembly.GetExportedTypes());

        // Act & Assert
        foreach (var type in types)
        {
            type.Namespace.ShouldNotBeNull();
            type.Namespace!.StartsWith("Dilcore.MongoDB").ShouldBeTrue(type.FullName);
            type.Namespace.ShouldNotContain("DocumentDb");
        }
    }
}