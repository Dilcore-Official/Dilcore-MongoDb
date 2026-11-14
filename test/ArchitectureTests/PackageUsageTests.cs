using System.Xml.Linq;

namespace Dilcore.MongoDB.ArchitectureTests;

public class PackageUsageTests
{
    private static readonly string ObjDirectorySegment = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";

    private static readonly string BinDirectorySegment = $"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}";

    private static readonly string SrcDirectorySegment = $"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}";

    private static readonly HashSet<string> BuildOnlyPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.CodeAnalysis.PublicApiAnalyzers",
        "NUnit.Analyzers",
        "SonarAnalyzer.CSharp",
        "StyleCop.Analyzers",
        "Microsoft.NET.Test.Sdk",
        "NUnit3TestAdapter",
        "GitHubActionsTestLogger",
        "coverlet.collector",
    };

    private static readonly Dictionary<string, string[]> UsageTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AutoFixture"] = ["AutoFixture"],
        ["AutoFixture.NUnit4"] = ["AutoFixture", "InlineAutoData", "AutoData"],
        ["BenchmarkDotNet"] = ["BenchmarkDotNet"],
        ["FluentResults"] = ["FluentResults"],
        ["Microsoft.Extensions.DependencyInjection"] = ["IServiceCollection", "ServiceCollection", "DependencyInjection"],
        ["MongoDB.Driver"] = ["MongoDB.Driver", "MongoDB.Bson"],
        ["Newtonsoft.Json"] = ["Newtonsoft.Json"],
        ["NUnit"] = ["NUnit.Framework", "[Test]"],
        ["Shouldly"] = ["Shouldly"],
        ["Swashbuckle.AspNetCore"] = ["AddSwaggerGen", "UseSwagger", "Swashbuckle"],
        ["Testcontainers.MongoDb"] = ["Testcontainers"],
    };

    [Test]
    public void EveryCentralPackageVersion_IsReferencedByAProject()
    {
        // Arrange
        var root = RepoLocator.FindRepoRoot();
        var referenced = GetAllPackageReferences(root);
        var versions = GetCentralPackageVersions(root);

        // Act
        var unused = versions.Where(version => !referenced.Contains(version));

        // Assert
        unused.ShouldBeEmpty();
    }

    [Test]
    public void EveryRuntimePackageReference_IsUsedByThatProject()
    {
        // Arrange
        var root = RepoLocator.FindRepoRoot();
        var projects = GetProjectFiles(root);
        var failures = new List<string>();

        // Act
        foreach (var project in projects)
        {
            CollectPackageFailures(project, root, failures);
        }

        // Assert
        failures.ShouldBeEmpty();
    }

    private static HashSet<string> GetCentralPackageVersions(string root)
    {
        var packagesPropsPath = Path.Join(root, "Directory.Packages.props");
        var doc = XDocument.Load(packagesPropsPath);
        return doc.Descendants("PackageVersion")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> GetAllPackageReferences(string root)
    {
        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var propsFiles = Directory.GetFiles(root, "Directory.Build.props", SearchOption.AllDirectories);
        var buildProps = propsFiles.Where(path => !path.Contains(ObjDirectorySegment, StringComparison.Ordinal));

        var buildPropsPackages = buildProps
            .Select(props => XDocument.Load(props))
            .SelectMany(document => RepoLocator.GetPackageReferences(document));

        packages.UnionWith(buildPropsPackages);

        var projects = GetProjectFiles(root);

        var projectPackages = projects
            .Select(project => XDocument.Load(project))
            .SelectMany(document => RepoLocator.GetPackageReferences(document));

        packages.UnionWith(projectPackages);

        return packages;
    }

    private static IEnumerable<string> GetPackageReferences(string projectPath, string root)
    {
        var projectDocument = XDocument.Load(projectPath);
        var projectPackages = RepoLocator.GetPackageReferences(projectDocument);

        var rootPropsPath = Path.Join(root, "Directory.Build.props");
        var rootPropsDocument = XDocument.Load(rootPropsPath);
        var rootPropsPackages = RepoLocator.GetPackageReferences(rootPropsDocument);
        var packages = projectPackages.Concat(rootPropsPackages);

        if (!projectPath.Contains(SrcDirectorySegment, StringComparison.Ordinal))
        {
            return packages.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        var srcPropsPath = Path.Join(root, "src", "Directory.Build.props");
        var srcPropsDocument = XDocument.Load(srcPropsPath);
        var srcPropsPackages = RepoLocator.GetPackageReferences(srcPropsDocument);
        var withSrcProps = packages.Concat(srcPropsPackages);
        return withSrcProps.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetProjectFiles(string root)
    {
        var folders = new[] { "src", "test", "samples" };
        return folders.SelectMany(folder => ProjectFiles(root, folder));
    }

    private static IEnumerable<string> ProjectFiles(string root, string folder)
    {
        var folderPath = Path.Join(root, folder);
        var projectFiles = Directory.GetFiles(folderPath, "*.csproj", SearchOption.AllDirectories);
        return projectFiles.Where(path => !path.Contains(ObjDirectorySegment, StringComparison.Ordinal));
    }

    private static void CollectPackageFailures(string project, string root, List<string> failures)
    {
        var source = ReadProjectSource(project);
        var packages = GetPackageReferences(project, root);

        foreach (var package in packages)
        {
            RecordPackageFailure(project, source, package, failures);
        }
    }

    private static void RecordPackageFailure(string project, string source, string package, List<string> failures)
    {
        if (BuildOnlyPackages.Contains(package))
        {
            return;
        }

        var projectName = Path.GetFileName(project);
        if (!UsageTokens.TryGetValue(package, out var tokens))
        {
            failures.Add($"{projectName} references {package}, which has no usage token.");
            return;
        }

        var used = tokens.Any(token => source.Contains(token, StringComparison.Ordinal));
        if (used)
        {
            return;
        }

        failures.Add($"{projectName} references {package} but does not use it.");
    }

    private static string ReadProjectSource(string projectPath)
    {
        var directory = Path.GetDirectoryName(projectPath)!;
        var sourceFiles = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);

        var files = sourceFiles.Where(path => !path.Contains(ObjDirectorySegment, StringComparison.Ordinal)
            && !path.Contains(BinDirectorySegment, StringComparison.Ordinal));

        var contents = files.Select(File.ReadAllText);
        return string.Join('\n', contents);
    }
}