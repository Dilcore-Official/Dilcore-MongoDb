using System.IO.Compression;
using System.Xml.Linq;

namespace Dilcore.MongoDB.PackageTests;

public class PackageMetadataTests
{
    private const string RepositoryUrl = "https://github.com/Dilcore-Official/Dilcore-MongoDb";

    private static readonly string[] ForbiddenBrands = ["documentdb", "amazon"];

    [Test]
    [CancelAfter(300000)]
    public void PackedPackages_CarryCatalogDescriptionAndTags()
    {
        // Arrange
        var feed = Directory.CreateTempSubdirectory("dilcore-package-metadata").FullName;
        PackagePacker.Pack(feed);
        var packages = Directory.GetFiles(feed, "*.nupkg").OrderBy(path => path, StringComparer.Ordinal).ToArray();

        // Act
        var nuspecs = packages.Select(ReadNuspec).ToArray();

        // Assert
        nuspecs.Length.ShouldBe(4);
        foreach (var nuspec in nuspecs)
        {
            AssertCatalogMetadata(nuspec);
        }
    }

    [Test]
    [CancelAfter(300000)]
    public void PackedPackages_CarryLicenseRepositoryReadmeAndIcon()
    {
        // Arrange
        var feed = Directory.CreateTempSubdirectory("dilcore-package-identity").FullName;
        PackagePacker.Pack(feed);
        var packages = Directory.GetFiles(feed, "*.nupkg").OrderBy(path => path, StringComparer.Ordinal).ToArray();

        // Act
        var archives = packages.Select(path => (Nuspec: ReadNuspec(path), Entries: ReadEntries(path))).ToArray();

        // Assert
        archives.Length.ShouldBe(4);
        foreach (var archive in archives)
        {
            AssertIdentityMetadata(archive.Nuspec, archive.Entries);
        }
    }

    [Test]
    [CancelAfter(300000)]
    public void PackedPackages_ShipSymbolPackagesWithPortablePdb()
    {
        // Arrange
        var feed = Directory.CreateTempSubdirectory("dilcore-package-symbols").FullName;
        PackagePacker.Pack(feed);
        var packages = Directory.GetFiles(feed, "*.nupkg").OrderBy(path => path, StringComparer.Ordinal).ToArray();

        // Act
        var symbols = packages.Select(path => (Id: ReadValue(ReadNuspec(path), "id"), Path: Path.ChangeExtension(path, ".snupkg"))).ToArray();

        // Assert
        symbols.Length.ShouldBe(4);
        foreach (var symbol in symbols)
        {
            File.Exists(symbol.Path).ShouldBeTrue($"{symbol.Id} has no .snupkg next to its .nupkg");
            ReadEntries(symbol.Path).ShouldContain($"lib/net10.0/{symbol.Id}.pdb");
        }
    }

    [Test]
    [CancelAfter(300000)]
    public void PackedPackages_ShipTheirOwnReadmeWithInstallCommand()
    {
        // Arrange
        var feed = Directory.CreateTempSubdirectory("dilcore-package-readme").FullName;
        PackagePacker.Pack(feed);
        var packages = Directory.GetFiles(feed, "*.nupkg").OrderBy(path => path, StringComparer.Ordinal).ToArray();

        // Act
        var readmes = packages.Select(path => (Id: ReadValue(ReadNuspec(path), "id"), Text: ReadEntryText(path, "README.md"))).ToArray();

        // Assert
        readmes.Length.ShouldBe(4);
        foreach (var readme in readmes)
        {
            readme.Text.ShouldStartWith($"# {readme.Id}");
            readme.Text.ShouldContain($"dotnet add package {readme.Id}");
            readme.Text.ShouldContain("## Key features");
        }
    }

    private static void AssertCatalogMetadata(XDocument nuspec)
    {
        var id = ReadValue(nuspec, "id");
        var description = ReadValue(nuspec, "description");
        var tags = ReadValue(nuspec, "tags").Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var expectedDescription = ReadCatalogValue(id, tagsTable: false);
        var expectedTags = ReadCatalogValue(id, tagsTable: true).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var metadata = nuspec.ToString();

        description.ShouldBe(expectedDescription);
        description.ShouldContain("MongoDB");
        tags.ShouldBe(expectedTags);
        tags.ShouldContain("mongodb");
        foreach (var brand in ForbiddenBrands)
        {
            metadata.ShouldNotContain(brand, Case.Insensitive);
        }
    }

    private static void AssertIdentityMetadata(XDocument nuspec, string[] entries)
    {
        var license = nuspec.Descendants().Single(element => element.Name.LocalName == "license");
        var repository = nuspec.Descendants().Single(element => element.Name.LocalName == "repository");
        var icon = ReadValue(nuspec, "icon");
        var readme = ReadValue(nuspec, "readme");
        var licenseLines = File.ReadAllLines(Path.Join(RepoLocator.FindRepoRoot(), "LICENSE"));
        var copyrightLine = licenseLines.Single(line => line.StartsWith("Copyright (c)", StringComparison.Ordinal));

        ((string?)license.Attribute("type")).ShouldBe("expression");
        license.Value.Trim().ShouldBe("MIT");
        ReadValue(nuspec, "copyright").ShouldBe(copyrightLine);
        ((string?)repository.Attribute("type")).ShouldBe("git");
        ((string?)repository.Attribute("url")).ShouldBe(RepositoryUrl);
        ReadValue(nuspec, "projectUrl").ShouldBe(RepositoryUrl);
        entries.ShouldContain(icon);
        entries.ShouldContain(readme);
    }

    private static XDocument ReadNuspec(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var entry = archive.Entries.Single(candidate => candidate.FullName.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
        using var stream = entry.Open();

        return XDocument.Load(stream);
    }

    private static string ReadEntryText(string packagePath, string entryName)
    {
        using var archive = ZipFile.OpenRead(packagePath);
        var entry = archive.GetEntry(entryName);
        entry.ShouldNotBeNull($"{entryName} is missing from {Path.GetFileName(packagePath)}");
        using var reader = new StreamReader(entry.Open());

        return reader.ReadToEnd();
    }

    private static string[] ReadEntries(string packagePath)
    {
        using var archive = ZipFile.OpenRead(packagePath);

        return archive.Entries.Select(entry => entry.FullName.Replace('\\', '/')).ToArray();
    }

    private static string ReadValue(XDocument nuspec, string elementName)
    {
        var element = nuspec.Descendants().Single(candidate => candidate.Name.LocalName == elementName);

        return element.Value.Trim();
    }

    private static string ReadCatalogValue(string id, bool tagsTable)
    {
        var path = Path.Join(RepoLocator.FindRepoRoot(), "docs", "product", "package-descriptions.md");
        var lines = File.ReadAllLines(path);
        var rows = lines
            .Where(line => line.StartsWith("| `", StringComparison.Ordinal))
            .Select(line => line.Trim().Trim('|').Split('|').Select(cell => cell.Trim()).ToArray())
            .Where(cells => cells[0] == $"`{id}`" && (cells.Length == 2) == tagsTable)
            .ToArray();

        return rows.Single()[^1];
    }
}