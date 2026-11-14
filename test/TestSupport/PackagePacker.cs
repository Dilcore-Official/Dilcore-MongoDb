using System.Diagnostics;
using System.Text;

namespace Dilcore.MongoDB.TestSupport;

/// <summary>
/// Packs the four library projects into a folder. A file lock keeps parallel test assemblies
/// from packing the same projects at the same time, and a temp cache reuses a pack when sources
/// have not changed.
/// </summary>
public static class PackagePacker
{
    private static readonly TimeSpan LockWait = TimeSpan.FromMinutes(4);

    /// <summary>
    /// Packs every project under <c>src/</c> into <paramref name="destination"/> with the locked restore.
    /// A later call copies the cached packages when the sources are unchanged.
    /// </summary>
    /// <param name="destination">Directory that receives the <c>.nupkg</c> files. Created when missing.</param>
    public static void Pack(string destination)
    {
        Directory.CreateDirectory(destination);

        using var packLock = AcquirePackLock();
        EnsureCachedPackages();
        CopyCachedPackages(destination);
    }

    private static void EnsureCachedPackages()
    {
        var cache = CacheDirectory();
        Directory.CreateDirectory(cache);
        var stamp = SourceStamp();
        var stampPath = Path.Join(cache, "stamp.txt");
        var packages = Directory.GetFiles(cache, "*.nupkg");
        var cachedStamp = File.Exists(stampPath) ? File.ReadAllText(stampPath) : string.Empty;
        if (cachedStamp == stamp && packages.Length == 4)
        {
            return;
        }

        ClearCache(cache, stampPath);
        PackSources(cache);
        File.WriteAllText(stampPath, stamp);
    }

    private static void ClearCache(string cache, string stampPath)
    {
        if (File.Exists(stampPath))
        {
            File.Delete(stampPath);
        }

        DeletePackages(cache);
    }

    private static void DeletePackages(string cache)
    {
        var packages = Directory.GetFiles(cache, "*.nupkg").Concat(Directory.GetFiles(cache, "*.snupkg")).ToArray();
        foreach (var package in packages)
        {
            File.Delete(package);
        }
    }

    private static void CopyCachedPackages(string destination)
    {
        var cache = CacheDirectory();
        var packages = Directory.GetFiles(cache, "*.nupkg").Concat(Directory.GetFiles(cache, "*.snupkg")).ToArray();
        foreach (var package in packages)
        {
            var name = Path.GetFileName(package);
            var target = Path.Join(destination, name);
            File.Copy(package, target, true);
        }
    }

    private static string CacheDirectory()
        => Path.Join(Path.GetTempPath(), "dilcore-mongodb-nupkg-cache");

    private static string SourceStamp()
    {
        var root = RepoLocator.FindRepoRoot();
        var src = Path.Join(root, "src");
        var discovered = Directory.GetFiles(src, "*.*", SearchOption.AllDirectories);
        var readme = Path.Join(root, "README.md");
        var packageVersions = Path.Join(root, "Directory.Packages.props");
        var buildProps = Path.Join(root, "src", "Directory.Build.props");
        var sources = discovered
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Append(readme)
            .Append(packageVersions)
            .Append(buildProps)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        var builder = new StringBuilder();
        foreach (var file in sources)
        {
            var relative = Path.GetRelativePath(root, file);
            var ticks = File.GetLastWriteTimeUtc(file).Ticks;
            builder.Append(relative);
            builder.Append(':');
            builder.Append(ticks);
            builder.Append('\n');
        }

        return builder.ToString();
    }

    private static FileStream AcquirePackLock()
    {
        var lockPath = Path.Join(Path.GetTempPath(), "dilcore-mongodb-pack.lock");
        var started = Environment.TickCount64;
        var stream = TryOpenLock(lockPath);
        while (stream is null)
        {
            ThrowIfLockTimedOut(started);
            Thread.Sleep(200);
            stream = TryOpenLock(lockPath);
        }

        return stream;
    }

    private static FileStream? TryOpenLock(string lockPath)
    {
        try
        {
            return new FileStream(lockPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        catch (IOException)
        {
            return null;
        }
    }

    private static void ThrowIfLockTimedOut(long started)
    {
        var elapsed = TimeSpan.FromMilliseconds(Environment.TickCount64 - started);
        if (elapsed < LockWait)
        {
            return;
        }

        throw new TimeoutException("Timed out waiting to pack the library projects.");
    }

    private static void PackSources(string destination)
    {
        var root = RepoLocator.FindRepoRoot();
        var projects = RepoLocator.GetSrcProjectFiles();

        foreach (var project in projects)
        {
            PackProject(root, project, destination);
        }
    }

    private static void PackProject(string root, string project, string destination)
    {
        var logPath = Path.Join(Path.GetTempPath(), $"dilcore-pack-{Guid.NewGuid():N}.log");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = root,
            UseShellExecute = false,
        };
        startInfo.Environment["MSBUILDNODEREUSE"] = "0";
        startInfo.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
        startInfo.ArgumentList.Add("pack");
        startInfo.ArgumentList.Add(project);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        startInfo.ArgumentList.Add("--output");
        startInfo.ArgumentList.Add(destination);
        startInfo.ArgumentList.Add("-p:RestoreLockedMode=true");
        var fileLogger = $"-flp:logfile={logPath};verbosity=minimal";
        startInfo.ArgumentList.Add(fileLogger);

        using var process = StartPack(startInfo, project);
        process.WaitForExit();
        var log = ReadLog(logPath);
        EnsurePacked(process, project, log);
    }

    private static Process StartPack(ProcessStartInfo startInfo, string project)
    {
        var process = Process.Start(startInfo);
        if (process is not null)
        {
            return process;
        }

        throw new InvalidOperationException($"Failed to start dotnet pack for {project}.");
    }

    private static void EnsurePacked(Process process, string project, string log)
    {
        if (process.ExitCode == 0)
        {
            return;
        }

        var message = $"dotnet pack failed for {project} ({process.ExitCode}).{Environment.NewLine}{log}";
        throw new InvalidOperationException(message);
    }

    private static string ReadLog(string logPath)
    {
        if (!File.Exists(logPath))
        {
            return string.Empty;
        }

        return File.ReadAllText(logPath);
    }
}