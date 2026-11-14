namespace Dilcore.MongoDB.ArchitectureTests;

public class DependencyBoundaryTests
{
    [Test]
    public void Abstractions_DoesNotReferencePrimaryOrDiHost()
    {
        // Arrange
        var abstractions = RepoLocator.GetSrcProjectFiles()
            .Single(p => p.EndsWith("Dilcore.MongoDB.Abstractions.csproj", StringComparison.Ordinal));
        var doc = RepoLocator.LoadCsproj(abstractions);

        // Act
        var projectRefs = RepoLocator.GetProjectReferences(doc).ToList();
        var packages = RepoLocator.GetPackageReferences(doc).ToHashSet(StringComparer.OrdinalIgnoreCase);

        // Assert
        projectRefs.ShouldBeEmpty();
        packages.ShouldContain("FluentResults");
        packages.ShouldContain("MongoDB.Driver");
        packages.ShouldNotContain("Microsoft.Extensions.DependencyInjection");
        packages.ShouldNotContain("FluentValidation");
    }

    [Test]
    public void Primary_HasAtMostThreeDirectPackageReferences_AndNoForbiddenPackages()
    {
        // Arrange
        var primary = RepoLocator.GetSrcProjectFiles()
            .Single(p => Path.GetFileName(p) == "Dilcore.MongoDB.csproj");
        var doc = RepoLocator.LoadCsproj(primary);

        // Act
        var packages = RepoLocator.GetPackageReferences(doc).ToList();
        var projectRefs = RepoLocator.GetProjectReferences(doc).ToList();

        // Assert
        packages.Count.ShouldBeLessThanOrEqualTo(3);
        packages.ShouldContain("Microsoft.Extensions.DependencyInjection");
        packages.ShouldContain("MongoDB.Driver");

        packages.ShouldNotContain("FluentValidation");
        packages.ShouldNotContain("Newtonsoft.Json");
        packages.ShouldNotContain("System.Text.Json");
        packages.ShouldNotContain("OpenTelemetry");
        packages.ShouldNotContain("Azure.Monitor.OpenTelemetry.Exporter");
        packages.ShouldNotContain("ApplicationInsights");

        projectRefs.Count.ShouldBe(1);
        projectRefs[0].ShouldContain("Dilcore.MongoDB.Abstractions");
    }

    [Test]
    public void SystemTextJson_DoesNotReferenceNewtonsoft()
    {
        // Arrange
        var project = RepoLocator.GetSrcProjectFiles()
            .Single(p => Path.GetFileName(p) == "Dilcore.MongoDB.SystemTextJson.csproj");
        var doc = RepoLocator.LoadCsproj(project);

        // Act
        var packages = RepoLocator.GetPackageReferences(doc).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var projectReferences = RepoLocator.GetProjectReferences(doc);

        // Assert
        packages.ShouldNotContain("Newtonsoft.Json");
        projectReferences.ShouldContain(path => path.Contains("Dilcore.MongoDB.csproj"));
    }

    [Test]
    public void NewtonsoftJson_ReferencesNewtonsoftAndNotTheStjPackage()
    {
        // Arrange
        var project = RepoLocator.GetSrcProjectFiles()
            .Single(p => Path.GetFileName(p) == "Dilcore.MongoDB.NewtonsoftJson.csproj");
        var doc = RepoLocator.LoadCsproj(project);

        // Act
        var packages = RepoLocator.GetPackageReferences(doc).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var projectReferences = RepoLocator.GetProjectReferences(doc);

        // Assert
        packages.ShouldContain("Newtonsoft.Json");
        projectReferences.ShouldNotContain(path => path.Contains("SystemTextJson"));
    }
}