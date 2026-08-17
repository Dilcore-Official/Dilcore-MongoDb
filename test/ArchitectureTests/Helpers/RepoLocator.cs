using System.Xml.Linq;

namespace Dilcore.MongoDB.ArchitectureTests.Helpers;

public static class RepoLocator
{
    public static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var hasSln = dir.GetFiles("Dilcore.MongoDB.sln").Length > 0
                || dir.GetFiles("Dilcore.DocumentDb.sln").Length > 0;
            var hasSrc = Directory.Exists(Path.Join(dir.FullName, "src"));
            if (hasSln && hasSrc)
            {
                return dir.FullName;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root via solution markers.");
    }

    public static IReadOnlyList<string> GetSrcProjectFiles()
    {
        var src = Path.Join(FindRepoRoot(), "src");
        return Directory.GetFiles(src, "*.csproj", SearchOption.AllDirectories)
            .Where(p => !p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .ToList();
    }

    public static XDocument LoadCsproj(string path) => XDocument.Load(path);

    public static IEnumerable<string> GetPackageReferences(XDocument doc) =>
        doc.Descendants("PackageReference")
            .Select(x => (string?)x.Attribute("Include") ?? x.Element("Include")?.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .ToList();

    public static IEnumerable<string> GetProjectReferences(XDocument doc) =>
        doc.Descendants("ProjectReference")
            .Select(x => (string?)x.Attribute("Include") ?? string.Empty)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
}
