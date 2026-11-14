using System.Diagnostics;
using System.Xml.Linq;

namespace Dilcore.MongoDB.ConsumerTests;

public class PackedConsumerTests
{
    [Test]
    [CancelAfter(300000)]
    public async Task PackedPackages_CompileAndValidateScopesForAnExternalCaller(CancellationToken cancellationToken = default)
    {
        // Arrange
        var feed = Directory.CreateTempSubdirectory("dilcore-consumer-feed").FullName;
        PackagePacker.Pack(feed);
        var project = WriteConsumer(feed);

        // Act
        var result = await RunConsumer(project, cancellationToken);

        // Assert
        result.ExitCode.ShouldBe(0, result.Output);
    }

    private static string WriteConsumer(string feed)
    {
        var app = Directory.CreateTempSubdirectory("dilcore-consumer-app").FullName;
        var version = ReadVersion();
        var dependencyInjectionVersion = ReadCentralVersion("Microsoft.Extensions.DependencyInjection");
        var project = ConsumerProject(version, dependencyInjectionVersion);
        var program = ConsumerProgram();
        var config = ConsumerNuGetConfig(feed);

        var projectPath = Path.Join(app, "Consumer.csproj");
        var programPath = Path.Join(app, "Program.cs");
        var configPath = Path.Join(app, "nuget.config");
        File.WriteAllText(projectPath, project);
        File.WriteAllText(programPath, program);
        File.WriteAllText(configPath, config);
        return projectPath;
    }

    private static string ReadVersion()
    {
        var propsPath = Path.Join(RepoLocator.FindRepoRoot(), "src", "Directory.Build.props");
        var document = XDocument.Load(propsPath);
        var version = document.Descendants("Version").Single();
        return version.Value.Trim();
    }

    private static string ReadCentralVersion(string packageId)
    {
        var propsPath = Path.Join(RepoLocator.FindRepoRoot(), "Directory.Packages.props");
        var document = XDocument.Load(propsPath);
        var package = document.Descendants("PackageVersion")
            .Single(element => element.Attribute("Include")?.Value == packageId);
        var version = package.Attribute("Version");
        return version!.Value;
    }

    private static string ConsumerProject(string version, string dependencyInjectionVersion)
    {
        return $"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RestorePackagesWithLockFile>false</RestorePackagesWithLockFile>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Dilcore.MongoDB" Version="{version}" />
                <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="{dependencyInjectionVersion}" />
              </ItemGroup>
            </Project>
            """;
    }

    private static string ConsumerProgram()
    {
        return """
            using Dilcore.MongoDB.Abstractions.Namespace;
            using Dilcore.MongoDB.Extensions;
            using Microsoft.Extensions.DependencyInjection;

            var services = new ServiceCollection();
            services.AddMongoDb(mongo => mongo
                .AddCluster("primary", cluster => cluster.UseConnectionString("mongodb://127.0.0.1:27017"))
                .AddDatabase("ConsumerDb", database => database.OnCluster("primary")));

            using var provider = services.BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateScopes = true,
                ValidateOnBuild = true
            });

            using var scope = provider.CreateScope();
            _ = scope.ServiceProvider.GetRequiredService<INamespaceResolver>();
            return 0;
            """;
    }

    private static string ConsumerNuGetConfig(string feed)
    {
        return $"""
            <?xml version="1.0" encoding="utf-8"?>
            <configuration>
              <packageSources>
                <clear />
                <add key="local" value="{feed}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <packageSourceMapping>
                <packageSource key="local">
                  <package pattern="Dilcore.MongoDB" />
                  <package pattern="Dilcore.MongoDB.*" />
                </packageSource>
                <packageSource key="nuget.org">
                  <package pattern="*" />
                </packageSource>
              </packageSourceMapping>
            </configuration>
            """;
    }

    private static async Task<(int ExitCode, string Output)> RunConsumer(string projectPath, CancellationToken cancellationToken)
    {
        var workingDirectory = Path.GetDirectoryName(projectPath)!;
        var logPath = Path.Join(Path.GetTempPath(), $"dilcore-consumer-{Guid.NewGuid():N}.log");
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };
        startInfo.Environment["MSBUILDNODEREUSE"] = "0";
        startInfo.Environment["DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER"] = "1";
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--project");
        startInfo.ArgumentList.Add(projectPath);
        startInfo.ArgumentList.Add("--configuration");
        startInfo.ArgumentList.Add("Release");
        var fileLogger = $"-flp:logfile={logPath};verbosity=minimal";
        startInfo.ArgumentList.Add(fileLogger);

        using var process = Start(startInfo);
        await process.WaitForExitAsync(cancellationToken);
        var output = File.Exists(logPath) ? File.ReadAllText(logPath) : string.Empty;
        return (process.ExitCode, output);
    }

    private static Process Start(ProcessStartInfo startInfo)
    {
        var process = Process.Start(startInfo);
        if (process is not null)
        {
            return process;
        }

        throw new InvalidOperationException("Failed to start dotnet run for the packed consumer.");
    }
}