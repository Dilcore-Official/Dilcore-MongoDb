using System.Xml.Linq;
using Dilcore.MongoDB.ArchitectureTests.Helpers;

namespace Dilcore.MongoDB.ArchitectureTests;

public class PackageUsageTests
{
    private static readonly HashSet<string> BuildOnlyPackages = new(StringComparer.OrdinalIgnoreCase)
    {
        "Microsoft.CodeAnalysis.PublicApiAnalyzers",
        "NUnit.Analyzers",
        "StyleCop.Analyzers",
        "Microsoft.NET.Test.Sdk",
        "NUnit3TestAdapter",
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
        var root = RepoLocator.FindRepoRoot();
        var referenced = GetAllPackageReferences(root);
        var versions = GetCentralPackageVersions(root);

        var unused = versions.Where(version => !referenced.Contains(version)).ToList();
        unused.ShouldBeEmpty();
    }

    [Test]
    public void EveryRuntimePackageReference_IsUsedByThatProject()
    {
        var root = RepoLocator.FindRepoRoot();
        var failures = new List<string>();

        foreach (var project in GetProjectFiles(root))
        {
            var source = ReadProjectSource(project);
            foreach (var package in GetPackageReferences(project, root))
            {
                if (BuildOnlyPackages.Contains(package))
                {
                    continue;
                }

                if (!UsageTokens.TryGetValue(package, out var tokens))
                {
                    failures.Add($"{Path.GetFileName(project)} references {package}, which has no usage token.");
                    continue;
                }

                if (!tokens.Any(token => source.Contains(token, StringComparison.Ordinal)))
                {
                    failures.Add($"{Path.GetFileName(project)} references {package} but does not use it.");
                }
            }
        }

        failures.ShouldBeEmpty();
    }

    private static HashSet<string> GetCentralPackageVersions(string root)
    {
        var doc = XDocument.Load(Path.Join(root, "Directory.Packages.props"));
        return doc.Descendants("PackageVersion")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static HashSet<string> GetAllPackageReferences(string root)
    {
        var packages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var props in Directory.GetFiles(root, "Directory.Build.props", SearchOption.AllDirectories))
        {
            if (props.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            packages.UnionWith(RepoLocator.GetPackageReferences(XDocument.Load(props)));
        }

        foreach (var project in GetProjectFiles(root))
        {
            packages.UnionWith(RepoLocator.GetPackageReferences(XDocument.Load(project)));
        }

        return packages;
    }

    private static IEnumerable<string> GetPackageReferences(string projectPath, string root)
    {
        var packages = RepoLocator.GetPackageReferences(XDocument.Load(projectPath)).ToList();
        packages.AddRange(RepoLocator.GetPackageReferences(XDocument.Load(Path.Join(root, "Directory.Build.props"))));

        var srcProps = Path.Join(root, "src", "Directory.Build.props");
        if (projectPath.Contains($"{Path.DirectorySeparatorChar}src{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
        {
            packages.AddRange(RepoLocator.GetPackageReferences(XDocument.Load(srcProps)));
        }

        return packages.Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> GetProjectFiles(string root)
    {
        foreach (var folder in new[] { "src", "test", "samples" })
        {
            foreach (var project in Directory.GetFiles(Path.Join(root, folder), "*.csproj", SearchOption.AllDirectories))
            {
                if (!project.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    yield return project;
                }
            }
        }
    }

    private static string ReadProjectSource(string projectPath)
    {
        var directory = Path.GetDirectoryName(projectPath)!;
        var files = Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
        return string.Join('\n', files.Select(File.ReadAllText));
    }
}