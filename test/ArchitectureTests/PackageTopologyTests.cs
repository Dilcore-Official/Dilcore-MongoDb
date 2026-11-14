namespace Dilcore.MongoDB.ArchitectureTests;

public class PackageTopologyTests
{
    [Test]
    public void Src_HasFourPackableProjects()
    {
        // Arrange
        var projects = RepoLocator.GetSrcProjectFiles();

        // Act
        var names = projects.Select(Path.GetFileNameWithoutExtension).OrderBy(x => x).ToArray();

        // Assert
        projects.Count.ShouldBe(4);
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
        // Arrange
        var projects = RepoLocator.GetSrcProjectFiles();

        // Act & Assert
        foreach (var project in projects)
        {
            Path.GetFileName(project).ShouldNotContain("DocumentDb");
            Directory.GetParent(project)!.Name.ShouldNotContain("DocumentDb");
        }
    }
}