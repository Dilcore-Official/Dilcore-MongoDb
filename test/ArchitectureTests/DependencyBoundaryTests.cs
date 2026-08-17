using Dilcore.MongoDB.ArchitectureTests.Helpers;

namespace Dilcore.MongoDB.ArchitectureTests;

public class DependencyBoundaryTests
{
    [Test]
    public void Abstractions_DoesNotReferencePrimaryOrDiHost()
    {
        var abstractions = RepoLocator.GetSrcProjectFiles()
            .Single(p => p.EndsWith("Dilcore.MongoDB.Abstractions.csproj", StringComparison.Ordinal));
        var doc = RepoLocator.LoadCsproj(abstractions);

        var projectRefs = RepoLocator.GetProjectReferences(doc).ToList();
        projectRefs.ShouldBeEmpty();

        var packages = RepoLocator.GetPackageReferences(doc).ToHashSet(StringComparer.OrdinalIgnoreCase);
        packages.ShouldContain("FluentResults");
        packages.ShouldContain("MongoDB.Driver");
        packages.ShouldNotContain("Microsoft.Extensions.DependencyInjection");
        packages.ShouldNotContain("FluentValidation");
    }

    [Test]
    public void Primary_HasAtMostThreeDirectPackageReferences_AndNoForbiddenPackages()
    {
        var primary = RepoLocator.GetSrcProjectFiles()
            .Single(p => Path.GetFileName(p) == "Dilcore.MongoDB.csproj");
        var doc = RepoLocator.LoadCsproj(primary);

        var packages = RepoLocator.GetPackageReferences(doc).ToList();
        packages.Count.ShouldBeLessThanOrEqualTo(3);
        packages.ShouldContain("Microsoft.Extensions.DependencyInjection");
        packages.ShouldContain("MongoDB.Driver");

        packages.ShouldNotContain("FluentValidation");
        packages.ShouldNotContain("Newtonsoft.Json");
        packages.ShouldNotContain("System.Text.Json");
        packages.ShouldNotContain("OpenTelemetry");
        packages.ShouldNotContain("Azure.Monitor.OpenTelemetry.Exporter");
        packages.ShouldNotContain("ApplicationInsights");

        var projectRefs = RepoLocator.GetProjectReferences(doc).ToList();
        projectRefs.Count.ShouldBe(1);
        projectRefs[0].ShouldContain("Dilcore.MongoDB.Abstractions");
    }
}
