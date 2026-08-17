using System.Reflection;
using Dilcore.MongoDB.Abstractions;
using Dilcore.MongoDB.Extensions;

namespace Dilcore.MongoDB.ArchitectureTests;

public class NamespaceBoundaryTests
{
    [Test]
    public void PublicTypes_UseDilcoreMongoDBNamespaces()
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
                type.Namespace.ShouldNotBeNull();
                type.Namespace!.StartsWith("Dilcore.MongoDB").ShouldBeTrue(type.FullName);
                type.Namespace.ShouldNotContain("DocumentDb");
            }
        }
    }
}
