namespace Dilcore.MongoDB.Benchmarks;

/// <summary>
/// Placeholder for telemetry on/off overhead budgets from
/// <c>docs/product/v2-goals.md</c> (disabled ≤1%, enabled ≤3%).
/// </summary>
/// <remarks>
/// TODO(#33, #34 / M6): add ActivitySource + Meter instrumentation in the library,
/// then implement paired steady-state CRUD benchmarks with listeners attached vs absent.
/// This type intentionally has no <c>[Benchmark]</c> methods so it is not discovered
/// by BenchmarkDotNet until those features exist.
/// </remarks>
public static class TelemetryOverheadBenchmarks
{
    // Intentionally empty until M6 observability lands.
}
