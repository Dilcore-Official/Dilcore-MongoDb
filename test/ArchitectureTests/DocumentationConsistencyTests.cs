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

    [Test]
    public void SkillLink_RejectsSiblingDirectoryThatSharesTheRootPrefix()
    {
        // Arrange
        var skillRoot = Directory.CreateTempSubdirectory("skill-root").FullName;
        var sibling = skillRoot + "-sibling";
        Directory.CreateDirectory(sibling);
        var siblingSkill = Path.Join(sibling, "SKILL.md");
        File.WriteAllText(siblingSkill, "# sibling");
        var skillFile = Path.Join(skillRoot, "SKILL.md");
        var siblingName = Path.GetFileName(sibling);
        File.WriteAllText(skillFile, $"[other](../{siblingName}/SKILL.md)");

        try
        {
            // Act
            var offenders = SkillLinkOffenders(skillFile, skillRoot).ToList();

            // Assert
            offenders.ShouldNotBeEmpty();
        }
        finally
        {
            Directory.Delete(skillRoot, recursive: true);
            Directory.Delete(sibling, recursive: true);
        }
    }

    public static IEnumerable<string> SnippetPaths()
        => RegistrationSnippetPaths().Append("skills/using-dilcore-mongodb/references/examples.md");

    public static IEnumerable<string> RegistrationSnippetPaths()
        => PagePaths().Concat(ReadmePaths());

    [TestCaseSource(nameof(RegistrationSnippetPaths))]
    public void Snippet_DeclaresRegistrationBeforeTypes(string page)
    {
        // Arrange
        var blocks = CSharpBlocks(ReadRepoFile(page)).ToList();

        // Act
        var offenders = blocks.SelectMany(RegistrationOrderOffenders).ToList();

        // Assert
        offenders.ShouldBeEmpty(page);
    }

    [TestCaseSource(nameof(SnippetPaths))]
    public void Snippet_FollowsRepositoryStyle(string page)
    {
        // Arrange
        var blocks = CSharpBlocks(ReadRepoFile(page)).ToList();

        // Act
        var offenders = blocks.SelectMany(SnippetStyleOffenders).ToList();

        // Assert
        offenders.ShouldBeEmpty(page);
    }

    private static IEnumerable<string> ReadmePaths()
        =>
        [
            "README.md",
            "src/Dilcore.MongoDB/README.md",
            "src/Dilcore.MongoDB.Abstractions/README.md",
            "src/Dilcore.MongoDB.SystemTextJson/README.md",
            "src/Dilcore.MongoDB.NewtonsoftJson/README.md"
        ];

    private static IEnumerable<string> CSharpBlocks(string markdown)
    {
        var lines = markdown.Split('\n');
        var block = new List<string>();
        var inside = false;
        foreach (var line in lines)
        {
            if (!inside && line.StartsWith("```csharp", StringComparison.Ordinal))
            {
                inside = true;
                block.Clear();
                continue;
            }

            if (inside && line.StartsWith("```", StringComparison.Ordinal))
            {
                inside = false;
                yield return string.Join('\n', block);
                continue;
            }

            if (inside)
            {
                block.Add(line);
            }
        }
    }

    private static IEnumerable<string> RegistrationOrderOffenders(string block)
    {
        const string addMongoDb = "services.AddMongoDb";
        var addIndex = block.IndexOf(addMongoDb, StringComparison.Ordinal);
        if (addIndex < 0)
        {
            yield break;
        }

        var typeIndex = FirstPublicTypeIndex(block);
        if (typeIndex >= 0 && typeIndex < addIndex)
        {
            yield return "A public type is declared before services.AddMongoDb.";
        }

        var servicesIndex = block.IndexOf("var services", StringComparison.Ordinal);
        var connectionIndex = block.IndexOf("var connectionString", StringComparison.Ordinal);
        if (servicesIndex >= 0 && servicesIndex > addIndex)
        {
            yield return "var services is declared after services.AddMongoDb.";
        }

        if (connectionIndex >= 0 && servicesIndex >= 0 && connectionIndex > servicesIndex)
        {
            yield return "var connectionString is declared after var services.";
        }

        if (typeIndex >= 0 && servicesIndex >= 0 && servicesIndex > typeIndex)
        {
            yield return "var services is declared after a public type.";
        }
    }

    private static int FirstPublicTypeIndex(string block)
    {
        string[] markers = ["public sealed class ", "public sealed record ", "public interface ", "public static class "];
        var indexes = markers
            .Select(marker => block.IndexOf(marker, StringComparison.Ordinal))
            .Where(index => index >= 0)
            .ToList();

        return indexes.Count == 0 ? -1 : indexes.Min();
    }

    private static IEnumerable<string> SnippetStyleOffenders(string block)
    {
        foreach (var offender in UsingOffenders(block))
        {
            yield return offender;
        }

        foreach (var offender in BraceOffenders(block))
        {
            yield return offender;
        }

        foreach (var offender in CancellationOffenders(block))
        {
            yield return offender;
        }

        foreach (var offender in ArgumentOffenders(block))
        {
            yield return offender;
        }
    }

    private static IEnumerable<string> UsingOffenders(string block)
    {
        var seenCode = false;
        foreach (var raw in block.Split('\n'))
        {
            var line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal))
            {
                continue;
            }

            if (line.StartsWith("using ", StringComparison.Ordinal)
                && !line.StartsWith("using var ", StringComparison.Ordinal)
                && !line.StartsWith("using (", StringComparison.Ordinal))
            {
                if (seenCode)
                {
                    yield return $"using after code: {line}";
                }

                continue;
            }

            seenCode = true;
        }
    }

    private static IEnumerable<string> BraceOffenders(string block)
    {
        var lines = block.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (!IfStatement().IsMatch(line))
            {
                continue;
            }

            var next = NextCodeLine(lines, i + 1);
            if (next is null || !next.TrimStart().StartsWith('{'))
            {
                yield return $"braceless if: {line}";
            }
        }
    }

    private static string? NextCodeLine(string[] lines, int start)
    {
        for (var i = start; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal))
            {
                return lines[i];
            }
        }

        return null;
    }

    private static IEnumerable<string> CancellationOffenders(string block)
    {
        foreach (var raw in block.Split('\n'))
        {
            if (MissingCancellationDefault().IsMatch(raw))
            {
                yield return $"CancellationToken is not optional: {raw.Trim()}";
            }
        }
    }

    private static IEnumerable<string> ArgumentOffenders(string block)
    {
        foreach (var raw in block.Split('\n'))
        {
            var line = raw.Trim();
            if (line.StartsWith("Builders<", StringComparison.Ordinal)
                && (line.Contains(".Filter.", StringComparison.Ordinal) || line.Contains(".Update.", StringComparison.Ordinal)))
            {
                yield return $"builder call used as an argument: {line}";
            }

            var parsesObject = line.Contains("JObject.Parse(", StringComparison.Ordinal)
                && !line.Contains("var ", StringComparison.Ordinal);
            if (line.Contains("FromResult(Result.Ok", StringComparison.Ordinal) || parsesObject)
            {
                yield return $"method call used as an argument: {line}";
            }
        }
    }

    private static SearchOption SearchOptions => SearchOption.AllDirectories;

    private static bool IsInsideSkill(string directory, string relativeTarget, string skillRootPrefix)
    {
        var fullPath = Path.GetFullPath(Path.Join(directory, relativeTarget));

        return fullPath.StartsWith(skillRootPrefix, StringComparison.Ordinal);
    }

    private static IEnumerable<string> SkillLinkOffenders(string file, string skillRoot)
    {
        var text = File.ReadAllText(file);
        var directory = Path.GetDirectoryName(file)!;
        var relativeLinks = MarkdownLink().Matches(text)
            .Select(m => m.Groups[1].Value)
            .Where(t => !t.StartsWith("http", StringComparison.Ordinal) && !t.StartsWith('#'))
            .Select(t => t.Split('#')[0]);

        var skillRootPrefix = Path.TrimEndingDirectorySeparator(Path.GetFullPath(skillRoot)) + Path.DirectorySeparatorChar;
        var escaping = relativeLinks
            .Where(t => !IsInsideSkill(directory, t, skillRootPrefix) || !File.Exists(Path.GetFullPath(Path.Join(directory, t))))
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

    [GeneratedRegex(@"^(else\s+)?if\b.*\)\s*$", RegexOptions.None, matchTimeoutMilliseconds: 2000)]
    private static partial Regex IfStatement();

    [GeneratedRegex(@"CancellationToken\s+cancellationToken(?!\s*=)", RegexOptions.None, matchTimeoutMilliseconds: 2000)]
    private static partial Regex MissingCancellationDefault();
}