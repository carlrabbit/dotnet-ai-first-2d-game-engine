using System.Diagnostics;
using System.Text.Json;
using Agentic2D.Engine;
using Agentic2D.Metrics;

namespace Agentic2D.Engineering;

public static class PerformanceCli
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<int> RunAsync(string[] args, string root, TextWriter output, TextWriter error)
    {
        try
        {
            if (args.Length == 1 && args[0] == "smoke")
            {
                var path = Path.Combine(root, "artifacts/performance/smoke");
                await CaptureAsync("smoke", path);
                await output.WriteLineAsync($"performance smoke: passed; capture: {path}");
                return 0;
            }
            if (args.Length == 5 && args[0] == "capture" && args[1] == "--label" && args[3] == "--output")
            {
                await CaptureAsync(args[2], Absolute(root, args[4]));
                await output.WriteLineAsync($"performance capture: passed; capture: {args[4]}/performance-capture.json");
                return 0;
            }
            if (args.Length == 5 && args[0] == "compare" && args[3] == "--output")
            {
                await CompareAsync(Absolute(root, args[1]), Absolute(root, args[2]), Absolute(root, args[4]));
                await output.WriteLineAsync($"performance compare: passed; comparison: {args[4]}/performance-comparison.json");
                return 0;
            }
            if (args.Length == 9 && args[0] == "report" && args[1] == "--milestone" && args[3] == "--before" && args[5] == "--after" && args[7] == "--output")
            {
                await ReportAsync(args[2], Absolute(root, args[4]), Absolute(root, args[6]), Absolute(root, args[8]));
                await output.WriteLineAsync($"performance report: passed; report: {args[8]}/performance-report.json");
                return 0;
            }
            await error.WriteLineAsync("usage: performance smoke | capture --label <label> --output <directory> | compare <before-directory> <after-directory> --output <directory> | report --milestone <id> --before <before-directory> --after <after-directory> --output <directory>");
            return 2;
        }
        catch (Exception exception) when (exception is IOException or JsonException or ArgumentException)
        {
            await error.WriteLineAsync("performance command failed: " + exception.Message);
            return 1;
        }
    }

    private static async Task CaptureAsync(string label, string output)
    {
        Directory.CreateDirectory(output);
        var workloads = new[]
        {
            new WorkloadDefinition("performance.runtime-reference", "runtime.smoke", 30, new[] { "runtime.tick.duration", "runtime.commands.accepted", "runtime.events.emitted" }),
            new WorkloadDefinition("performance.entities-reference", "entity.component-runtime-smoke", 40, new[] { "runtime.entities.active", "runtime.events.emitted" }),
            new WorkloadDefinition("performance.persistent-world-reference", "persistent-world-smoke", 50, new[] { "runtime.tick.duration", "runtime.events.emitted" }),
            new WorkloadDefinition("performance.presentation-reference", "presentation.persistent-world-player-facing-smoke", 60, new[] { "presentation.render-items", "presentation.effects.active", "runtime.tick.duration" }),
        };
        var results = new List<WorkloadCapture>();
        foreach (var workload in workloads)
        {
            _ = Run(workload); // one warm-up iteration, never reported
            var iterations = Enumerable.Range(0, 5).Select(_ => Run(workload)).ToArray();
            results.Add(new WorkloadCapture(workload, Median(iterations.Select(x => x.ElapsedMilliseconds)), Median(iterations.Select(x => x.AllocatedBytes)), iterations[0].WorkCounters));
        }
        var capture = new PerformanceCapture("agentic2d.performance-capture.v1", label, "Release", "headless", 1, 5, "median", "same-machine-close-in-time-only", results, new[] { "Timing is observational, not a deterministic fingerprint.", "Comparisons are advisory and are invalid across machines, configurations, seeds, or workload definitions." });
        await File.WriteAllTextAsync(Path.Combine(output, "performance-capture.json"), JsonSerializer.Serialize(capture, Json));
    }

    private static Iteration Run(WorkloadDefinition workload)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        var metrics = RuntimeSmokeScenario.RunWithMetrics(workload.MeasuredTickCount, MetricsCollectionMode.Summary);
        clock.Stop();
        var work = metrics.Summary.Where(x => x.Kind == RuntimeMetricKind.Counter).ToDictionary(x => x.Id, x => x.Total, StringComparer.Ordinal);
        return new Iteration(clock.Elapsed.TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - before, work);
    }

    private static async Task CompareAsync(string beforeDirectory, string afterDirectory, string output)
    {
        var comparison = Compare(ReadCapture(beforeDirectory), ReadCapture(afterDirectory));
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "performance-comparison.json"), JsonSerializer.Serialize(comparison, Json));
    }

    private static async Task ReportAsync(string milestone, string beforeDirectory, string afterDirectory, string output)
    {
        var comparison = Compare(ReadCapture(beforeDirectory), ReadCapture(afterDirectory));
        Directory.CreateDirectory(output);
        var report = new PerformanceReport("agentic2d.performance-report.v1", milestone, comparison.Status, comparison, "Advisory same-machine, same-configuration, fixed-seed, close-in-time comparison only; elapsed timing is not deterministic authority.");
        await File.WriteAllTextAsync(Path.Combine(output, "performance-report.json"), JsonSerializer.Serialize(report, Json));
        var markdown = $"# Performance report — {milestone}\n\nStatus: `{comparison.Status}`\n\nThis is an advisory, same-machine comparison. Timing values are observational and not deterministic receipt fingerprints.\n\n| Workload | Elapsed change | Allocation change | Status |\n|---|---:|---:|---|\n" + string.Join("\n", comparison.Workloads.Select(x => $"| {x.Id} | {x.ElapsedChangePercent:F2}% | {x.AllocationChangePercent:F2}% | {x.Status} |")) + "\n";
        await File.WriteAllTextAsync(Path.Combine(output, "performance-report.md"), markdown);
    }

    private static PerformanceComparison Compare(PerformanceCapture before, PerformanceCapture after)
    {
        var rows = new List<WorkloadComparison>();
        foreach (var baseline in before.Workloads)
        {
            var candidate = after.Workloads.SingleOrDefault(x => x.Definition.Id == baseline.Definition.Id);
            if (candidate is null) { rows.Add(new WorkloadComparison(baseline.Definition.Id, 0, 0, "not-measured", baseline.WorkCounters, new Dictionary<string, double>())); continue; }
            var elapsed = Change(baseline.ElapsedMedianMilliseconds, candidate.ElapsedMedianMilliseconds);
            var allocations = Change(baseline.AllocatedBytesMedian, candidate.AllocatedBytesMedian);
            rows.Add(new WorkloadComparison(baseline.Definition.Id, elapsed, allocations, Classify(elapsed), baseline.WorkCounters, candidate.WorkCounters));
        }
        var status = rows.Any(x => x.Status == "possible-regression") ? "possible-regression" : rows.Any(x => x.Status == "improved") ? "improved" : rows.All(x => x.Status == "not-measured") ? "not-measured" : "within-noise";
        return new PerformanceComparison("agentic2d.performance-comparison.v1", before.Label, after.Label, status, rows, "same-machine-close-in-time-only");
    }

    private static PerformanceCapture ReadCapture(string directory) => JsonSerializer.Deserialize<PerformanceCapture>(File.ReadAllText(Path.Combine(directory, "performance-capture.json")), Json) ?? throw new JsonException("capture is malformed");
    private static double Median(IEnumerable<double> source) { var values = source.Order().ToArray(); return values[values.Length / 2]; }
    private static double Change(double before, double after) => before == 0 ? 0 : (after - before) / before * 100d;
    private static string Classify(double elapsedChange) => Math.Abs(elapsedChange) < 5 ? "within-noise" : elapsedChange < 0 ? "improved" : "possible-regression";
    private static string Absolute(string root, string path) => Path.IsPathRooted(path) ? path : Path.Combine(root, path);

    private sealed record Iteration(double ElapsedMilliseconds, double AllocatedBytes, IReadOnlyDictionary<string, double> WorkCounters);
}

public sealed record WorkloadDefinition(string Id, string SourceScenario, int MeasuredTickCount, IReadOnlyList<string> RelevantMetrics)
{
    public string FixedSeed => "seed.performance-reference";
    public int WarmupIterations => 1;
    public int MeasuredIterations => 5;
    public string MetricsMode => "summary";
    public string ArtifactWritingBoundary => "after-measurement";
}
public sealed record WorkloadCapture(WorkloadDefinition Definition, double ElapsedMedianMilliseconds, double AllocatedBytesMedian, IReadOnlyDictionary<string, double> WorkCounters);
public sealed record PerformanceCapture(string Schema, string Label, string Configuration, string Execution, int WarmupIterations, int MeasuredIterations, string PrimaryStatistic, string ComparisonScope, IReadOnlyList<WorkloadCapture> Workloads, IReadOnlyList<string> Limitations);
public sealed record WorkloadComparison(string Id, double ElapsedChangePercent, double AllocationChangePercent, string Status, IReadOnlyDictionary<string, double> BeforeWorkCounters, IReadOnlyDictionary<string, double> AfterWorkCounters);
public sealed record PerformanceComparison(string Schema, string BeforeLabel, string AfterLabel, string Status, IReadOnlyList<WorkloadComparison> Workloads, string ComparisonScope);
public sealed record PerformanceReport(string Schema, string Milestone, string Status, PerformanceComparison Comparison, string Limitations);
