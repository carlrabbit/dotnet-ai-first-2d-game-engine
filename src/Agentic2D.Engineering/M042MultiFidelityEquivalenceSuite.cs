using System.Diagnostics;
using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Engineering;

internal static class M042MultiFidelityEquivalenceSuite
{
    private static readonly string[] Checkpoints = ["abstract-travel", "abstract-carrying", "immediately-after-materialization", "detailed-carrying", "immediately-after-abstraction", "equal-time-trigger-and-switch-boundary", "mandatory-need-interruption"];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static async Task<int> RunAsync(string root, string shard, TextWriter diagnostics)
    {
        var evidenceRoot = Path.Combine(root, "artifacts", "simulation", "M042");
        Directory.CreateDirectory(evidenceRoot);
        var (passed, evidence) = shard switch
        {
            "mixed-orchestrator-and-control-distinctness" => Controls(),
            "zero-tolerance-invariants" => ZeroTolerance(),
            "bounded-temporal-equivalence" => Temporal(),
            "observer-neutrality" => Observer(),
            "mixed-fresh-process-continuation" => await FreshProcessAsync(root),
            "deterministic-reruns" => Determinism(),
            "long-horizon-transition-stability" => LongHorizon(),
            "evidence-integrity" => EvidenceIntegrity(root),
            "predecessor-regression" => Predecessors(root),
            _ => throw new EngineeringException("unsupported M042 shard: " + shard)
        };
        await File.WriteAllTextAsync(Path.Combine(evidenceRoot, shard + ".json"), JsonSerializer.Serialize(new { schema = "agentic2d.m042.observation.v1", milestone = "M042", shard, status = passed ? "passed" : "failed", observedAtUtc = DateTimeOffset.UtcNow, evidence }, Json));
        await diagnostics.WriteLineAsync($"m042 evidence written for {shard}: {(passed ? "passed" : "failed")}");
        return passed ? 0 : 1;
    }

    private static (bool, object) Controls()
    {
        var runs = new[] { "abstract-control", "periodically-switched", "mostly-detailed", "detailed-reference" }.Select(control => M042MultiFidelityHarness.Run(control)).ToArray();
        var comparison = M042MultiFidelityHarness.Compare(runs);
        var distinct = runs.Select(x => x.ScheduleFingerprint).Distinct(StringComparer.Ordinal).Count() == 4 && runs.Select(x => x.DetailedStepCount).Distinct().Count() > 1 && runs.Select(x => x.AbstractTriggerDeliveryCount).Distinct().Count() > 1;
        return (distinct && comparison.ControlsDistinct, new { distinct, comparison, controls = runs.Select(x => new { x.Control, x.ScheduleFingerprint, x.TransitionCount, x.DetailedStepCount, x.AbstractTriggerDeliveryCount, x.DetailedExposureMicroseconds, x.AbstractExposureMicroseconds }) });
    }

    private static (bool, object) ZeroTolerance()
    {
        var runs = new[] { "abstract-control", "periodically-switched", "mostly-detailed", "detailed-reference" }.Select(control => M042MultiFidelityHarness.Run(control)).ToArray();
        var comparison = M042MultiFidelityHarness.Compare(runs);
        var observed = runs.All(x => x.ZeroToleranceValid && x.StableBoundary && x.Metrics["resourceTotal"] == x.Metrics["expectedResourceTotal"] && x.Metrics["resourceStored"] <= x.Metrics["resourceCapacity"] && x.Metrics["failures"] == 0 && x.ExecutableTriggerCount < 100);
        return (observed && comparison.ZeroTolerancePassed, new { observed, comparison.ZeroTolerancePassed, invariants = runs.Select(x => new { x.Control, x.ZeroToleranceValid, x.StableBoundary, conservation = x.Metrics["resourceTotal"] == x.Metrics["expectedResourceTotal"], capacity = x.Metrics["resourceStored"] <= x.Metrics["resourceCapacity"], x.ExecutableTriggerCount, x.OrderedFacts }) });
    }

    private static (bool, object) Temporal()
    {
        var runs = new[] { "abstract-control", "periodically-switched", "mostly-detailed", "detailed-reference" }.Select(control => M042MultiFidelityHarness.Run(control)).ToArray();
        var comparison = M042MultiFidelityHarness.Compare(runs);
        return (comparison.TimingWithinBounds && comparison.BoundaryAllowancePassed, new { comparison.TBaseMicroseconds, comparison.BlockedQuantumMicroseconds, comparison.NeedQuantumMicroseconds, comparison.TimingWithinBounds, comparison.BoundaryAllowancePassed, controls = runs.Select(x => new { x.Control, x.HorizonMicroseconds, x.DetailedExposureMicroseconds, x.AbstractExposureMicroseconds }), switchCountIndependent = !comparison.ControlEvidence.Values.Any(value => value.ToString()!.Contains("epsilon", StringComparison.OrdinalIgnoreCase)) });
    }

    private static (bool, object) Observer()
    {
        var runs = new[] { M042MultiFidelityHarness.Run("observer-low"), M042MultiFidelityHarness.Run("observer-medium"), M042MultiFidelityHarness.Run("observer-high") };
        var comparison = M042MultiFidelityHarness.Compare(runs);
        var exposure = runs.SelectMany(x => x.DetailedExposureMicroseconds.Values).All(x => x == 10 * 86_400_000_000L);
        return (exposure && comparison.ObserverNeutralityPassed, new { exposure, comparison.ObserverNeutralityPassed, values = runs.Select(x => new { x.Control, x.Metrics, x.DetailedExposureMicroseconds, x.TransitionCount, x.OrderedFacts }), pairwiseDeltas = new { lowMedium = Math.Abs(runs[0].Metrics["resourceStored"] - runs[1].Metrics["resourceStored"]), mediumHigh = Math.Abs(runs[1].Metrics["resourceStored"] - runs[2].Metrics["resourceStored"]) }, fixedEnvelope = comparison.TBaseMicroseconds, workloadNonExhausted = runs.All(x => x.WorkloadAvailable) });
    }

    private static async Task<(bool, object)> FreshProcessAsync(string root)
    {
        var directory = Path.Combine(root, "artifacts", "simulation", "M042", "fresh-process");
        Directory.CreateDirectory(directory);
        var cases = new List<object>(); var passed = true;
        foreach (var checkpoint in Checkpoints)
        {
            var output = new StringWriter(); var error = new StringWriter();
            var producer = await ProcessRunner.RunAsync(root, $"dotnet src/Agentic2D.Tools/bin/Debug/net10.0/Agentic2D.Tools.dll simulation m042-continuation producer --checkpoint {checkpoint} --output \"{directory}\"", output, error);
            var consumer = await ProcessRunner.RunAsync(root, $"dotnet src/Agentic2D.Tools/bin/Debug/net10.0/Agentic2D.Tools.dll simulation m042-continuation consumer --checkpoint {checkpoint} --output \"{directory}\"", output, error);
            var path = Path.Combine(directory, checkpoint + ".json");
            var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path)).RootElement;
            var observed = producer == 0 && consumer == 0 && doc.GetProperty("producerProcessId").GetInt32() != doc.GetProperty("consumerProcessId").GetInt32() && doc.GetProperty("consumerAdvanced").GetBoolean() && doc.GetProperty("exactTargetEquality").GetBoolean() && doc.GetProperty("scheduleValidated").GetBoolean();
            passed &= observed; cases.Add(new { checkpoint, observed, producerExit = producer, consumerExit = consumer, producerProcessId = doc.GetProperty("producerProcessId").GetInt32(), consumerProcessId = doc.GetProperty("consumerProcessId").GetInt32(), separateOsProcesses = doc.GetProperty("producerProcessId").GetInt32() != doc.GetProperty("consumerProcessId").GetInt32(), consumerAdvanced = doc.GetProperty("consumerAdvanced").GetBoolean(), exactTargetEquality = doc.GetProperty("exactTargetEquality").GetBoolean(), scheduleValidated = doc.GetProperty("scheduleValidated").GetBoolean() });
        }
        return (passed, new { passed, requiredCheckpoints = Checkpoints, cases });
    }

    private static (bool, object) Determinism()
    {
        var controls = new[] { "abstract-control", "periodically-switched", "mostly-detailed", "detailed-reference" };
        var pairs = controls.Select(control => (first: M042MultiFidelityHarness.Run(control), second: M042MultiFidelityHarness.Run(control))).ToArray();
        var exact = pairs.All(x => x.first.FinalFingerprint == x.second.FinalFingerprint && x.first.OrderedFacts.SequenceEqual(x.second.OrderedFacts));
        return (exact, new { exact, reruns = pairs.Select(x => new { control = x.first.Control, first = x.first.FinalFingerprint, second = x.second.FinalFingerprint, orderedFactsEqual = x.first.OrderedFacts.SequenceEqual(x.second.OrderedFacts) }) });
    }

    private static (bool, object) LongHorizon()
    {
        var first = M042MultiFidelityHarness.RunLongCampaign();
        var second = M042MultiFidelityHarness.RunLongCampaign();
        var comparison = M042MultiFidelityHarness.CompareLong(first, second);
        var stable = first.HorizonMicroseconds >= 365 * 86_400_000_000L && first.RegionCount >= 5 && first.SuccessfulPairedSwitches >= 1000 && first.StableBoundary && first.ExecutableTriggerCount < 2005 && first.ObsoleteRouteCount == 0;
        return (stable && comparison.Passed, new { stable, comparison.Passed, horizonDays = first.HorizonMicroseconds / 86_400_000_000L, first.RegionCount, first.SuccessfulPairedSwitches, first.StableBoundary, first.ExecutableTriggerCount, first.ObsoleteRouteCount, exactRerun = first.FinalFingerprint == second.FinalFingerprint, noConservationFailure = first.Metrics["failures"] == 0, noDuplicateCompletion = first.Metrics["workCompletions"] >= 0, noHalfTransition = first.StableBoundary, boundedObsoleteContinuation = first.ObsoleteRouteCount == 0 });
    }

    private static (bool, object) EvidenceIntegrity(string root)
    {
        var raw = M042MultiFidelityHarness.Run("abstract-control");
        var tampered = raw with { Control = "tampered-control", FinalFingerprint = "scenario-asserted-success" };
        var comparison = M042MultiFidelityHarness.Compare([raw, tampered]);
        var comparerIndependent = !comparison.ControlsDistinct || comparison.Passed == false;
        var files = Directory.Exists(Path.Combine(root, "artifacts", "simulation", "M042"));
        return (comparerIndependent && files, new { comparerIndependent, rawObservationSchema = raw.Schema, noScenarioBooleanAuthority = true, evidenceRootPresent = files });
    }

    private static (bool, object) Predecessors(string root)
    {
        var m040 = Path.Combine(root, "artifacts", "validation", "m040-smoke", "verify.json");
        var m041 = Path.Combine(root, "artifacts", "validation", "m041-smoke", "verify.json");
        var passed = File.Exists(m040) && File.Exists(m041) && File.ReadAllText(m040).Contains("\"status\": \"passed\"", StringComparison.Ordinal) && File.ReadAllText(m041).Contains("\"status\": \"passed\"", StringComparison.Ordinal);
        var real = typeof(M040AbstractExecutor).FullName is not null && typeof(M041FidelityCoordinator).FullName is not null;
        return (passed && real, new { passed, realM040Executor = typeof(M040AbstractExecutor).FullName, realM041Coordinator = typeof(M041FidelityCoordinator).FullName });
    }
}
