using System.Text.RegularExpressions;

namespace Dilcore.MongoDB.ArchitectureTests;

/// <summary>
/// Keeps the consumer guides, their example tests, the guides index and the consumer skill consistent.
/// The rule is in .cursor/rules/docs-and-skill-sync.mdc.
/// </summary>
public partial class DocumentationConsistencyTests
{
    private const string GitHubBlob = "https://github.com/Dilcore-Official/Dilcore-MongoDb/blob/main/";

    private const string GitHubTree = "https://github.com/Dilcore-Official/Dilcore-MongoDb/tree/main/";

    private static readonly string[] RequiredHeadings =
    [
        "## Setup",
        "## Use",
        "## Handle failures",
        "## Pitfalls",
        "## See also"
    ];

    private static readonly string[] ProductAndSecurityPages =
    [
        "docs/product/package-selection.md",
        "docs/product/driver-escape-hatches.md",
        "docs/security/production-mongodb.md"
    ];

    private static readonly string RepoRoot = RepoLocator.FindRepoRoot();

    public static IEnumerable<string> PagePaths()
    {
        var guides = Directory.GetFiles(Path.Join(RepoRoot, "docs", "guides"), "*.md")
            .Where(p => !string.Equals(Path.GetFileName(p), "README.md", StringComparison.Ordinal))
            .Select(p => $"docs/guides/{Path.GetFileName(p)}");

        return guides.Concat(ProductAndSecurityPages).Order(StringComparer.Ordinal);
    }

    [Test]
    public void GuidePages_AreDiscovered()
    {
        // Arrange
        var minimumPages = 20;

        // Act
        var pages = PagePaths().ToList();

        // Assert
        pages.Count.ShouldBeGreaterThanOrEqualTo(minimumPages);
        pages.ForEach(p => File.Exists(Path.Join(RepoRoot, p)).ShouldBeTrue(p));
    }

    [TestCaseSource(nameof(PagePaths))]
    public void Page_FollowsTheTemplateAndHasACompilableCodeBlock(string page)
    {
        // Arrange
        var text = ReadRepoFile(page);
        var headings = text.Split('\n').Select(l => l.TrimEnd()).Where(l => RequiredHeadings.Contains(l)).ToList();

        // Act
        var firstLine = text.Split('\n')[0];

        // Assert
        firstLine.ShouldStartWith("# ");
        headings.ShouldBe(RequiredHeadings);
        text.ShouldContain("Package: `Dilcore.MongoDB");
        text.ShouldContain("```csharp");
    }

    [TestCaseSource(nameof(PagePaths))]
    public void Page_IsListedInTheGuidesIndexAndTheSkillIndex(string page)
    {
        // Arrange
        var guidesIndex = ReadRepoFile("docs/guides/README.md");
        var skillIndex = ReadRepoFile("skills/using-dilcore-mongodb/references/docs-index.md");
        var relativeToGuides = page.StartsWith("docs/guides/", StringComparison.Ordinal)
            ? page["docs/guides/".Length..]
            : $"../{page["docs/".Length..]}";

        // Act
        var inGuidesIndex = guidesIndex.Contains($"]({relativeToGuides})", StringComparison.Ordinal);
        var inSkillIndex = skillIndex.Contains($"({GitHubBlob}{page})", StringComparison.Ordinal);

        // Assert
        inGuidesIndex.ShouldBeTrue($"{page} is not linked from docs/guides/README.md");
        inSkillIndex.ShouldBeTrue($"{page} is not linked from the skill docs-index.md");
    }

    [TestCaseSource(nameof(PagePaths))]
    public void Page_IsProvenByAnExampleTest(string page)
    {
        // Arrange
        var marker = $"Proves {page}.";
        var testRoot = Path.Join(RepoRoot, "test");

        // Act
        var provers = Directory.GetFiles(testRoot, "*.cs", SearchOptions)
            .Where(p => p.Contains($"{Path.DirectorySeparatorChar}Examples{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(p => File.ReadAllText(p).Contains(marker, StringComparison.Ordinal))
            .ToList();

        // Assert
        provers.ShouldNotBeEmpty($"No Examples/ test has the summary '{marker}'");
    }

    [TestCaseSource(nameof(PagePaths))]
    public void Page_HasNoMilestoneInternalOrToolingWording(string page)
    {
        // Arrange
        var text = ReadRepoFile(page);

        // Act
        var milestone = MilestoneToken().Match(text);
        var tooling = ToolingToken().Match(text);

        // Assert
        milestone.Success.ShouldBeFalse($"Milestone wording in {page}: {milestone.Value}");
        tooling.Success.ShouldBeFalse($"Internal or tooling wording in {page}: {tooling.Value}");
    }

    [TestCaseSource(nameof(PagePaths))]
    public void Page_RelativeLinksResolve(string page)
    {
        // Arrange
        var text = ReadRepoFile(page);
        var directory = Path.GetDirectoryName(Path.Join(RepoRoot, page))!;
        var targets = MarkdownLink().Matches(text)
            .Select(m => m.Groups[1].Value)
            .Where(t => !t.StartsWith("http", StringComparison.Ordinal) && !t.StartsWith('#'))
            .Select(t => t.Split('#')[0])
            .Distinct()
            .ToList();

        // Act
        var missing = targets
            .Where(t => !File.Exists(Path.GetFullPath(Path.Join(directory, t))) && !Directory.Exists(Path.GetFullPath(Path.Join(directory, t))))
            .ToList();

        // Assert
        missing.ShouldBeEmpty($"Broken links in {page}");
    }

    [Test]
    public void SkillIndex_LinksOnlyToExistingRepositoryFiles()
    {
        // Arrange
        var skillIndex = ReadRepoFile("skills/using-dilcore-mongodb/references/docs-index.md");
        var repoPaths = MarkdownLink().Matches(skillIndex)
            .Select(m => m.Groups[1].Value)
            .Where(t => t.StartsWith(GitHubBlob, StringComparison.Ordinal) || t.StartsWith(GitHubTree, StringComparison.Ordinal))
            .Select(t => t.StartsWith(GitHubBlob, StringComparison.Ordinal) ? t[GitHubBlob.Length..] : t[GitHubTree.Length..])
            .ToList();

        // Act
        var missing = repoPaths
            .Where(p => !File.Exists(Path.Join(RepoRoot, p)) && !Directory.Exists(Path.Join(RepoRoot, p)))
            .ToList();

        // Assert
        repoPaths.ShouldNotBeEmpty();
        missing.ShouldBeEmpty();
    }

    [Test]
    public void Skill_UsesOnlyAbsoluteOrSkillLocalLinks()
    {
        // Arrange
        var skillRoot = Path.Join(RepoRoot, "skills", "using-dilcore-mongodb");
        var files = Directory.GetFiles(skillRoot, "*.md", SearchOptions).ToList();

        // Act
        var offenders = files
            .SelectMany(f => SkillLinkOffenders(f, skillRoot))
            .ToList();

        // Assert
        files.ShouldNotBeEmpty();
        offenders.ShouldBeEmpty();
    }

    private static SearchOption SearchOptions => SearchOption.AllDirectories;

    private static IEnumerable<string> SkillLinkOffenders(string file, string skillRoot)
    {
        var text = File.ReadAllText(file);
        var directory = Path.GetDirectoryName(file)!;
        var relativeLinks = MarkdownLink().Matches(text)
            .Select(m => m.Groups[1].Value)
            .Where(t => !t.StartsWith("http", StringComparison.Ordinal) && !t.StartsWith('#'))
            .Select(t => t.Split('#')[0]);

        var escaping = relativeLinks
            .Where(t => !Path.GetFullPath(Path.Join(directory, t)).StartsWith(skillRoot, StringComparison.Ordinal)
                || !File.Exists(Path.GetFullPath(Path.Join(directory, t))))
            .Select(t => $"{Path.GetFileName(file)}: link {t}");

        var repoPaths = RepoRelativeCode().Matches(text).Select(m => $"{Path.GetFileName(file)}: path {m.Value}");

        return escaping.Concat(repoPaths);
    }

    private static string ReadRepoFile(string relativePath) => File.ReadAllText(Path.Join(RepoRoot, relativePath));

    [GeneratedRegex(@"\bM\d\b|PublicAPI|may redesign", RegexOptions.None, matchTimeoutMilliseconds: 2000)]
    private static partial Regex MilestoneToken();

    [GeneratedRegex(@"context7|\bskills?\b|AGENTS\.md", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 2000)]
    private static partial Regex ToolingToken();

    [GeneratedRegex(@"\]\(([^)\s]+)\)", RegexOptions.None, matchTimeoutMilliseconds: 2000)]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"`(docs|src|samples|test)/[^`]*`", RegexOptions.None, matchTimeoutMilliseconds: 2000)]
    private static partial Regex RepoRelativeCode();
}