using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;

namespace Dilcore.MongoDB.Benchmarks;

/// <summary>
/// Default run shape: ≥1 warmup and ≥15 measured iterations (v2-goals protocol).
/// </summary>
public sealed class BenchmarkConfig : ManualConfig
{
    public static ManualConfig Default { get; } = new BenchmarkConfig();

    public BenchmarkConfig()
    {
        AddLogger(ConsoleLogger.Default);
        AddColumnProvider(DefaultColumnProviders.Instance);

        AddJob(Job.Default
            .WithWarmupCount(1)
            .WithIterationCount(15));

        AddDiagnoser(MemoryDiagnoser.Default);
        AddExporter(JsonExporter.Full);
        AddExporter(JsonExporter.FullCompressed);
        AddExporter(MarkdownExporter.GitHub);
    }
}
