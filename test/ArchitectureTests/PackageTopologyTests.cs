using Dilcore.MongoDB.ArchitectureTests.Helpers;

namespace Dilcore.MongoDB.ArchitectureTests;

public class PackageTopologyTests
{
    [Test]
    public void Src_HasExactlyTwoPackableProjects()
    {
        var projects = RepoLocator.GetSrcProjectFiles();
        projects.Count.ShouldBe(2);

        var names = projects.Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToArray();
        names.ShouldBe(["Dilcore.MongoDB", "Dilcore.MongoDB.Abstractions"]);
    }

    [Test]
    public void Src_Projects_DoNotUseDocumentDbNames()
    {
        foreach (var project in RepoLocator.GetSrcProjectFiles())
        {
            Path.GetFileName(project).ShouldNotContain("DocumentDb");
            Directory.GetParent(project)!.Name.ShouldNotContain("DocumentDb");
        }
    }
}
