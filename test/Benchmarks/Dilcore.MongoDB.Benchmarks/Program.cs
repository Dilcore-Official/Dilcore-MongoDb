using BenchmarkDotNet.Running;

namespace Dilcore.MongoDB.Benchmarks;

public static class Program
{
    public static int Main(string[] args)
    {
        return BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(args, BenchmarkConfig.Default)
            .Any(summary => summary.HasCriticalValidationErrors)
            ? 1
            : 0;
    }
}
