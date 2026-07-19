using System.Diagnostics;
using System.Text.Json;
using Agentic2D.Engine;
using Agentic2D.Metrics;
using Agentic2D.Contracts;
using Agentic2D.Persistence;
using Agentic2D.Presentation;

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
            new WorkloadDefinition("performance.runtime-reference-scaled", "runtime.smoke", 18000, new[] { "runtime.tick.duration", "runtime.commands.accepted", "runtime.events.emitted" }, true),
            new WorkloadDefinition("performance.entities-reference-scaled", "entity.component-runtime-smoke", 30000, new[] { "runtime.entities.active", "runtime.events.emitted" }, true),
            new WorkloadDefinition("performance.persistent-world-reference-scaled", "persistent-world-smoke", 96000, new[] { "runtime.tick.duration", "runtime.events.emitted" }, true),
            new WorkloadDefinition("performance.presentation-reference-scaled", "presentation.persistent-world-player-facing-smoke", 48000, new[] { "presentation.render-items", "presentation.effects.active", "runtime.tick.duration" }, true),
        };
        var results = new List<WorkloadCapture>();
        foreach (var workload in workloads)
        {
            _ = Run(workload); // one warm-up iteration, never reported
            var iterations = Enumerable.Range(0, 5).Select(_ => Run(workload)).ToArray();
            var median = Median(iterations.Select(x => x.ElapsedMilliseconds));
            var authoritative = workload.IsScaled && median >= 10;
            results.Add(new WorkloadCapture(workload, median, Median(iterations.Select(x => x.AllocatedBytes)), iterations[0].WorkCounters, authoritative, authoritative ? "scaled-real-workload-at-or-above-10ms-floor" : "reference median is below the 10ms timing-authority floor", median, authoritative ? "timing-authoritative" : "not-timing-authoritative"));
        }
        var capture = new PerformanceCapture("agentic2d.performance-capture.v2", label, "Release", "headless", 1, 5, "median", "same-machine-close-in-time-only", results, new[] { "Timing is observational, not a deterministic fingerprint.", "Sub-10ms fixed workloads retain correctness, deterministic counters, and allocation observations but are not timing-authoritative.", "Comparisons are advisory and are invalid across machines, configurations, seeds, or workload definitions." });
        await File.WriteAllTextAsync(Path.Combine(output, "performance-capture.json"), JsonSerializer.Serialize(capture, Json));
    }

    private static Iteration Run(WorkloadDefinition workload)
    {
        var before = GC.GetAllocatedBytesForCurrentThread();
        var clock = Stopwatch.StartNew();
        var work = workload.Id switch
        {
            var id when id.Contains("entities", StringComparison.Ordinal) => Entities(workload.MeasuredTickCount),
            var id when id.Contains("persistent-world", StringComparison.Ordinal) => PersistentWorld(workload.MeasuredTickCount),
            var id when id.Contains("presentation", StringComparison.Ordinal) => Presentation(workload.MeasuredTickCount),
            _ => Runtime(workload.MeasuredTickCount),
        };
        clock.Stop();
        return new Iteration(clock.Elapsed.TotalMilliseconds, GC.GetAllocatedBytesForCurrentThread() - before, work);
    }
    private static IReadOnlyDictionary<string, double> Runtime(int ticks)
    {
        var metrics = RuntimeSmokeScenario.RunWithMetrics(ticks, MetricsCollectionMode.Summary);
        return metrics.Summary.Where(x => x.Kind == RuntimeMetricKind.Counter).ToDictionary(x => x.Id, x => x.Total, StringComparer.Ordinal);
    }
    private static IReadOnlyDictionary<string, double> Entities(int ticks)
    {
        var runtime = new MinimalRuntime(MetricsCollectionMode.Summary); var entities = Math.Max(32, ticks / 4);
        for (var index = 0; index < entities; index++) runtime.CreateEntity(new EntityId("entity.performance." + index.ToString("D4")), index);
        var mover = new EntityId("entity.performance.0000"); _ = runtime.Submit(new MoveCommand(mover, 1)); runtime.Run(Math.Max(1, ticks / 2), new MoveCommand(mover, 1));
        var counters = runtime.Metrics!.Snapshot().Summary.Where(x => x.Kind == RuntimeMetricKind.Counter).ToDictionary(x => x.Id, x => x.Total, StringComparer.Ordinal); counters["performance.entities.created"] = entities; counters["performance.entities.queried"] = runtime.QueryEntities().Count; return counters;
    }
    private static IReadOnlyDictionary<string, double> PersistentWorld(int ticks)
    {
        var runtime = PersistentWorldRuntime.CreateInitial();
        for (var tick = 1; tick <= ticks; tick++) runtime.AdvanceTo(tick);
        var saves = new CanonicalSaveService(); var identity = CanonicalSaveService.DefaultIdentity("performance.persistent-world"); var rounds = Math.Max(1, ticks / 2000); SaveDocument? save = null;
        for (var round = 0; round < rounds; round++) { save = saves.Capture(runtime, identity); var loaded = saves.Load(save, identity); if (!loaded.Success) throw new InvalidOperationException("scaled persistent-world workload could not load its own canonical save."); }
        return new Dictionary<string, double>(StringComparer.Ordinal) { ["performance.persistent-world.ticks"] = ticks, ["performance.persistent-world.entities"] = runtime.Entities.Count, ["performance.persistent-world.contributors"] = save!.Manifest.Contributors.Count, ["performance.persistent-world.roundtrip"] = rounds };
    }
    private static IReadOnlyDictionary<string, double> Presentation(int ticks)
    {
        var emitter = new ParticleEmitterDefinition("emitter.performance", "visual-definition.performance", "particle", 48, 48, 32, [-1d, -1d], [1d, 1d], [-.1d, -.1d], [.1d, .1d], [.5d, 1d], [0d, 360d], [-8d, 8d], [32, 64, 96, 255], [180, 220, 255, 255], "linear-inverse", "linear-inverse", "foreground");
        var effects = Math.Max(2, ticks / 1200); var samples = 0;
        for (var ordinal = 0; ordinal < effects; ordinal++)
        {
            var effect = new EffectInstance("effect.performance." + ordinal, "effect.performance", "request.performance." + ordinal, "event.performance." + ordinal, ordinal, 48, "seed.performance-reference", 0, "active", [], "performance");
            var spawned = ParticleProjector.Spawn(emitter, effect, ordinal, "0,0", "seed.performance-reference"); samples += ParticleProjector.Sample(spawned, ordinal + 8, "linear-inverse", "linear-inverse").Count;
        }
        return new Dictionary<string, double>(StringComparer.Ordinal) { ["performance.presentation.effects"] = effects, ["performance.presentation.particle-samples"] = samples, ["performance.presentation.particles-per-effect"] = emitter.ParticleCount };
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
        var markdown = $"# Performance report — {milestone}\n\nStatus: `{comparison.Status}`\n\nThis is an advisory, same-machine comparison. Timing values are observational and not deterministic receipt fingerprints. Sub-10-ms references are not timing-authoritative; only scaled real workloads at or above 10 ms receive ordinary percentage classification.\n\n| Workload | Elapsed change | Allocation change | Timing authority | Status |\n|---|---:|---:|---|---|\n" + string.Join("\n", comparison.Workloads.Select(x => $"| {x.Id} | {x.ElapsedChangePercent:F2}% | {x.AllocationChangePercent:F2}% | {x.TimingAuthority} | {x.Status} |")) + "\n";
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
            rows.Add(new WorkloadComparison(baseline.Definition.Id, elapsed, allocations, Classify(baseline, candidate, elapsed), baseline.WorkCounters, candidate.WorkCounters, candidate.TimingAuthority, candidate.TimingAuthorityReason));
        }
        var status = rows.Any(x => x.Status is "possible-regression" or "catastrophic-advisory") ? "possible-regression" : rows.Any(x => x.Status == "improved") ? "improved" : rows.All(x => x.Status == "not-measured") ? "not-measured" : "within-noise";
        return new PerformanceComparison("agentic2d.performance-comparison.v1", before.Label, after.Label, status, rows, "same-machine-close-in-time-only");
    }

    private static PerformanceCapture ReadCapture(string directory) => JsonSerializer.Deserialize<PerformanceCapture>(File.ReadAllText(Path.Combine(directory, "performance-capture.json")), Json) ?? throw new JsonException("capture is malformed");
    private static double Median(IEnumerable<double> source) { var values = source.Order().ToArray(); return values[values.Length / 2]; }
    private static double Change(double before, double after) => before == 0 ? 0 : (after - before) / before * 100d;
    private static string Classify(WorkloadCapture before, WorkloadCapture after, double elapsedChange)
    {
        if (!before.TimingAuthority || !after.TimingAuthority)
            return after.ElapsedMedianMilliseconds >= 10 && after.ElapsedMedianMilliseconds >= before.ElapsedMedianMilliseconds * 4 ? "catastrophic-advisory" : "not-timing-authoritative";
        return Math.Abs(elapsedChange) < 5 ? "within-noise" : elapsedChange < 0 ? "improved" : "possible-regression";
    }
    private static string Absolute(string root, string path) => Path.IsPathRooted(path) ? path : Path.Combine(root, path);

    private sealed record Iteration(double ElapsedMilliseconds, double AllocatedBytes, IReadOnlyDictionary<string, double> WorkCounters);
}

public sealed record WorkloadDefinition(string Id, string SourceScenario, int MeasuredTickCount, IReadOnlyList<string> RelevantMetrics, bool IsScaled = false)
{
    public string FixedSeed => "seed.performance-reference";
    public int WarmupIterations => 1;
    public int MeasuredIterations => 5;
    public string MetricsMode => "summary";
    public string ArtifactWritingBoundary => "after-measurement";
}
public sealed record WorkloadCapture(WorkloadDefinition Definition, double ElapsedMedianMilliseconds, double AllocatedBytesMedian, IReadOnlyDictionary<string, double> WorkCounters, bool TimingAuthority = false, string TimingAuthorityReason = "reference median is below the 10ms timing-authority floor", double ReferenceMedianMilliseconds = 0, string TimingStatus = "not-timing-authoritative");
public sealed record PerformanceCapture(string Schema, string Label, string Configuration, string Execution, int WarmupIterations, int MeasuredIterations, string PrimaryStatistic, string ComparisonScope, IReadOnlyList<WorkloadCapture> Workloads, IReadOnlyList<string> Limitations);
public sealed record WorkloadComparison(string Id, double ElapsedChangePercent, double AllocationChangePercent, string Status, IReadOnlyDictionary<string, double> BeforeWorkCounters, IReadOnlyDictionary<string, double> AfterWorkCounters, bool TimingAuthority = false, string TimingAuthorityReason = "reference median is below the 10ms timing-authority floor");
public sealed record PerformanceComparison(string Schema, string BeforeLabel, string AfterLabel, string Status, IReadOnlyList<WorkloadComparison> Workloads, string ComparisonScope);
public sealed record PerformanceReport(string Schema, string Milestone, string Status, PerformanceComparison Comparison, string Limitations);
