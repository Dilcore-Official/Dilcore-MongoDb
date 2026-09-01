using Dilcore.MongoDB.ArchitectureTests.Helpers;

namespace Dilcore.MongoDB.ArchitectureTests;

public class PackageTopologyTests
{
    [Test]
    public void Src_HasFourPackableProjects()
    {
        var projects = RepoLocator.GetSrcProjectFiles();
        projects.Count.ShouldBe(4);

        var names = projects.Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToArray();
        names.ShouldBe([
            "Dilcore.MongoDB",
            "Dilcore.MongoDB.Abstractions",
            "Dilcore.MongoDB.NewtonsoftJson",
            "Dilcore.MongoDB.SystemTextJson"
        ]);
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
