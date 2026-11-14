using System.IO.Compression;
using System.Xml.Linq;

namespace Dilcore.MongoDB.PackageTests;

public class PackageContentTests
{
    private static readonly Dictionary<string, string[]> ExpectedDependencies = new(StringComparer.Ordinal)
    {
        ["Dilcore.MongoDB.Abstractions"] = ["FluentResults", "MongoDB.Driver"],
        ["Dilcore.MongoDB"] = ["Dilcore.MongoDB.Abstractions", "Microsoft.Extensions.DependencyInjection", "MongoDB.Driver"],
        ["Dilcore.MongoDB.NewtonsoftJson"] = ["Dilcore.MongoDB", "Newtonsoft.Json"],
        ["Dilcore.MongoDB.SystemTextJson"] = ["Dilcore.MongoDB"],
    };

    [Test]
    [CancelAfter(300000)]
    public void PackedPackages_ShipReadmeDocsAssemblyAndDeclaredDependencies()
    {
        // Arrange
        var feed = Directory.CreateTempSubdirectory("dilcore-package-tests").FullName;
        PackagePacker.Pack(feed);

        // Act
        var packages = Directory.GetFiles(feed, "*.nupkg").OrderBy(path => path, StringComparer.Ordinal).ToArray();

        // Assert
        packages.Length.ShouldBe(4);
        foreach (var package in packages)
        {
            AssertPackage(package);
        }
    }

    private static void AssertPackage(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var entries = archive.Entries.Select(entry => entry.FullName.Replace('\\', '/')).ToArray();
        var nuspecEntry = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        var nuspec = ReadEntry(nuspecEntry);
        var id = ReadIdentity(nuspec);
        var assembly = $"lib/net10.0/{id}.dll";
        var documentation = $"lib/net10.0/{id}.xml";
        var publicApiFiles = entries.Where(entry => entry.Contains("PublicAPI", StringComparison.OrdinalIgnoreCase)).ToArray();
        var dependencies = ReadDependencyIds(nuspec);

        entries.ShouldContain("README.md");
        entries.ShouldContain(assembly);
        entries.ShouldContain(documentation);
        publicApiFiles.ShouldBeEmpty();
        nuspec.ShouldNotContain("mongodb://");
        dependencies.ShouldBe(ExpectedDependencies[id]);
    }

    private static string ReadEntry(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static string ReadIdentity(string nuspec)
    {
        var document = XDocument.Parse(nuspec);
        var id = document.Descendants().Single(element => element.Name.LocalName == "id");
        return id.Value.Trim();
    }

    private static string[] ReadDependencyIds(string nuspec)
    {
        var document = XDocument.Parse(nuspec);

        var dependencies = document.Descendants()
            .Where(element => element.Name.LocalName == "dependency")
            .Select(element => element.Attribute("id")?.Value)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToArray();

        return dependencies;
    }
}