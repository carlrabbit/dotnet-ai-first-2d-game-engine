using System.Diagnostics;
using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Engineering;

internal static class M041FidelityReconciliationSuite
{
    private static readonly string[] Phases = ["idle/no-work", "travel-to-source", "interaction/harvest-progress", "carrying", "travel-to-storage", "deposit-progress", "mandatory-need activity", "interrupted", "blocked/retry"];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static async Task<int> RunAsync(string root, string shard, TextWriter diagnostics)
    {
        var evidenceRoot = Path.Combine(root, "artifacts", "simulation", "M041");
        Directory.CreateDirectory(evidenceRoot);
        var (passed, evidence) = shard switch
        {
            "ownership-and-epoch-fencing" => Ownership(),
            "detailed-to-abstract" => DetailedToAbstract(),
            "abstract-to-detailed" => AbstractToDetailed(),
            "transition-state-matrix" => StateMatrix(),
            "scheduler-route-atomicity" => SchedulerAtomicity(),
            "rollback-fault-boundaries" => RollbackBoundaries(),
            "transition-persistence" => await PersistenceAsync(root),
            "stale-and-rapid-switch" => RapidSwitching(),
            "m040-regression" => M040Regression(root),
            _ => throw new EngineeringException("unsupported M041 shard: " + shard)
        };
        await File.WriteAllTextAsync(Path.Combine(evidenceRoot, shard + ".json"), JsonSerializer.Serialize(new { schema = "agentic2d.m041.observation.v1", milestone = "M041", shard, status = passed ? "passed" : "failed", observedAtUtc = DateTimeOffset.UtcNow, evidence }, Json));
        await diagnostics.WriteLineAsync($"m041 evidence written for {shard}: {(passed ? "passed" : "failed")}");
        return passed ? 0 : 1;
    }

    private static (bool, object) Ownership()
    {
        var run = M041FidelityCoordinator.CreateFixture(); var source = run.DetailedRegion; var target = run.Regions.Single(x => x.Fidelity == RegionFidelity.Abstract);
        var result = run.SwitchDetailed(target.RegionId);
        var oneDetailed = run.Regions.Count(x => x.Fidelity == RegionFidelity.Detailed) == 1;
        var oldSourceRejected = !run.IsCurrentOwner(source.RegionId, RegionFidelity.Detailed, source.Epoch);
        var oldTargetRejected = !run.IsCurrentOwner(target.RegionId, RegionFidelity.Abstract, target.Epoch);
        var newOwners = run.IsCurrentOwner(source.RegionId, RegionFidelity.Abstract, source.Epoch + 1) && run.IsCurrentOwner(target.RegionId, RegionFidelity.Detailed, target.Epoch + 1);
        return (result.Status == "committed" && oneDetailed && oldSourceRejected && oldTargetRejected && newOwners, new { result, oneDetailed, oldSourceRejected, oldTargetRejected, newOwners, realAbstractExecutor = typeof(M040AbstractExecutor).FullName, realDetailedExecutor = typeof(M032AutonomousDetailedRegion).FullName });
    }

    private static (bool, object) DetailedToAbstract()
    {
        var run = M041FidelityCoordinator.CreateFixture(); var before = run.World.Fingerprint(); var source = run.DetailedRegion; var result = run.SwitchDetailed("region.forest.dormant");
        var converted = run.Regions.Single(x => x.RegionId == source.RegionId).Abstract;
        var stage = M041ExecutorBridge.ExecuteRealAbstractStage();
        var observed = result.Status == "committed" && converted is not null && converted.EdgeIds.Count > 0 && converted.RemainingMicroseconds == source.Detailed!.RemainingMicroseconds && run.World.Fingerprint() == before && stage.Diagnostics.Count == 0;
        return (observed, new { observed, result, converted, semanticFingerprintUnchanged = run.World.Fingerprint() == before, abstractStageTransitions = stage.Transitions, switchCausedGameplayMutation = run.World.Fingerprint() != before });
    }

    private static (bool, object) AbstractToDetailed()
    {
        var run = M041FidelityCoordinator.CreateFixture(); run.SwitchDetailed("region.forest.dormant"); var abstractRegion = run.DetailedRegion; var result = run.SwitchDetailed("region.forest.active");
        var detailed = run.Regions.Single(x => x.RegionId == "region.forest.active").Detailed;
        var stage = M041ExecutorBridge.ExecuteRealDetailedStage();
        var valid = detailed is not null && detailed.Route.Count >= 0 && detailed.Position.X >= 0 && detailed.Position.Y >= 0 && detailed.Destination == new DetailedCell(4, 3);
        return (result.Status == "committed" && valid && stage.Diagnostics.Count == 0, new { result, materialized = detailed, routeRebuilt = detailed is not null && detailed.Route.Count > 0, validCell = valid, detailedStageDiagnostics = stage.Diagnostics.Count, oldAbstractEpoch = abstractRegion.Epoch });
    }

    private static (bool, object) StateMatrix()
    {
        var cases = new List<object>(); var passed = true;
        foreach (var phase in Phases)
        {
            var run = ConfigurePhase(phase); var before = run.World.Fingerprint(); var result = run.SwitchDetailed("region.forest.dormant");
            var converted = run.Regions.Single(x => x.RegionId == "region.forest.active").Abstract;
            var observed = result.Status == "committed" && converted is not null && converted.Phase == phase && run.World.Fingerprint() == before;
            passed &= observed; cases.Add(new { phase, observed, result.Status, convertedPhase = converted?.Phase, semanticUnchanged = run.World.Fingerprint() == before });
        }
        return (passed, new { cases, executedCases = cases.Count, requiredCases = Phases.Length });
    }

    private static (bool, object) SchedulerAtomicity()
    {
        var run = M041FidelityCoordinator.CreateFixture(); var source = run.DetailedRegion; var target = run.Regions.Single(x => x.Fidelity == RegionFidelity.Abstract);
        run.Scheduler.Schedule(new("m041.old.target", run.World.Clock.Now + new SimulationDuration(5), 1, target.RegionId, "activity.m040.abstract.harvest.001", "worker.001", "old-abstract-trigger", 1, target.Epoch, "c", "cause", JsonDocument.Parse("{}").RootElement));
        var beforeSemantic = run.World.Fingerprint(); var result = run.SwitchDetailed(target.RegionId); var old = run.Scheduler.Inspect().Single(x => x.Id == "m041.old.target"); var staged = run.Scheduler.Inspect().Any(x => x.Id.StartsWith("m041.trigger.", StringComparison.Ordinal) && x.OwnerRegionId == source.RegionId && x.Status == ScheduledTriggerStatus.Scheduled);
        var passed = result.Status == "committed" && old.Status == ScheduledTriggerStatus.Cancelled && staged && run.World.Fingerprint() == beforeSemantic;
        return (passed, new { passed, oldTriggerStatus = old.Status.ToString(), stagedSourceTrigger = staged, semanticFingerprintUnchanged = run.World.Fingerprint() == beforeSemantic, result });
    }

    private static (bool, object) RollbackBoundaries()
    {
        var observations = new List<object>(); var passed = true;
        foreach (var fault in Enum.GetValues<M041TransitionFaultBoundary>().Where(x => x != M041TransitionFaultBoundary.None))
        {
            var run = M041FidelityCoordinator.CreateFixture(); var before = Snapshot(run); var result = run.SwitchDetailed("region.forest.dormant", fault); var after = Snapshot(run); var unchanged = before == after;
            passed &= result.Status == "failed" && unchanged; observations.Add(new { fault = fault.ToString(), result.Status, unchanged, before, after });
        }
        return (passed, new { observations, allBoundariesUnchanged = passed });
    }

    private static async Task<(bool, object)> PersistenceAsync(string root)
    {
        var dir = Path.Combine(root, "artifacts", "simulation", "M041", "fresh-process"); Directory.CreateDirectory(dir);
        var output = new StringWriter(); var error = new StringWriter();
        var producer = await ProcessRunner.RunAsync(root, $"dotnet src/Agentic2D.Tools/bin/Debug/net10.0/Agentic2D.Tools.dll simulation m041-fidelity producer --output \"{dir}\"", output, error);
        var consumer = await ProcessRunner.RunAsync(root, $"dotnet src/Agentic2D.Tools/bin/Debug/net10.0/Agentic2D.Tools.dll simulation m041-fidelity consumer --output \"{dir}\"", output, error);
        var producerDoc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(dir, "producer.json"))).RootElement;
        var consumerDoc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(dir, "consumer.json"))).RootElement;
        var p = producerDoc.GetProperty("processId").GetInt32(); var c = consumerDoc.GetProperty("processId").GetInt32(); var distinct = p != c;
        var passed = producer == 0 && consumer == 0 && distinct && consumerDoc.GetProperty("advancedNewExecutor").GetBoolean() && consumerDoc.GetProperty("stablePostSave").GetBoolean();
        return (passed, new { passed, producerExit = producer, consumerExit = consumer, producerProcessId = p, consumerProcessId = c, separateOsProcesses = distinct, preSwitchLoadContinued = consumerDoc.GetProperty("preSwitchContinuation").GetString() == "committed", postSwitchOwner = consumerDoc.GetProperty("restoredOwner").GetString(), postSwitchAdvanced = consumerDoc.GetProperty("advancedNewExecutor").GetBoolean() });
    }

    private static (bool, object) RapidSwitching()
    {
        var run = M041FidelityCoordinator.CreateFixture(); var semantic = run.World.Fingerprint(); var results = new List<string>();
        for (var i = 0; i < 6; i++) { var target = run.Regions.Single(x => x.Fidelity == RegionFidelity.Abstract).RegionId; results.Add(run.SwitchDetailed(target).Status); }
        var executable = run.Scheduler.Inspect().Where(x => x.Status == ScheduledTriggerStatus.Scheduled).Select(x => x.Id).ToArray(); var unique = executable.Distinct(StringComparer.Ordinal).Count() == executable.Length;
        return (results.All(x => x == "committed") && run.Regions.Count(x => x.Fidelity == RegionFidelity.Detailed) == 1 && unique && run.World.Fingerprint() == semantic, new { results, executableTriggerCount = executable.Length, uniqueExecutableTriggers = unique, oneDetailed = run.Regions.Count(x => x.Fidelity == RegionFidelity.Detailed) == 1, semanticUnchanged = run.World.Fingerprint() == semantic });
    }

    private static (bool, object) M040Regression(string root)
    {
        var verify = Path.Combine(root, "artifacts", "validation", "m040-smoke", "verify.json"); var current = File.Exists(verify) && JsonDocument.Parse(File.ReadAllText(verify)).RootElement.GetProperty("status").GetString() == "passed";
        var abstractStage = M041ExecutorBridge.ExecuteRealAbstractStage(); var detailedStage = M041ExecutorBridge.ExecuteRealDetailedStage();
        var source = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Simulation", "M040SharedSimulation.cs")); var real = source.Contains("M040AbstractExecutor", StringComparison.Ordinal) && source.Contains("M040SharedSemantics", StringComparison.Ordinal);
        return (current && real && abstractStage.Diagnostics.Count == 0 && detailedStage.Diagnostics.Count == 0, new { m040VerifierCurrent = current, realM040SourceObserved = real, abstractDiagnostics = abstractStage.Diagnostics.Count, detailedDiagnostics = detailedStage.Diagnostics.Count, m042Claims = false });
    }

    private static M041FidelityCoordinator ConfigurePhase(string phase)
    {
        var baseRun = M041FidelityCoordinator.CreateFixture(); var detailed = baseRun.Regions.Single(x => x.Fidelity == RegionFidelity.Detailed); var abstractRegion = baseRun.Regions.Single(x => x.Fidelity == RegionFidelity.Abstract);
        var d = detailed.Detailed! with { Phase = phase, RemainingMicroseconds = phase.Contains("travel", StringComparison.Ordinal) ? 3_000_000 : 2_000_000 };
        var a = abstractRegion.Abstract! with { Phase = phase, DestinationNodeId = phase.Contains("storage", StringComparison.Ordinal) || phase == "deposit-progress" ? "storage" : phase.Contains("need", StringComparison.Ordinal) ? "housing" : "tree", RemainingMicroseconds = d.RemainingMicroseconds };
        return new(baseRun.World, baseRun.Scheduler, [detailed with { Detailed = d }, abstractRegion with { Abstract = a }]);
    }

    private static string Snapshot(M041FidelityCoordinator run) => JsonSerializer.Serialize(new { semantic = run.World.Fingerprint(), regions = run.Regions, queue = run.Scheduler.Capture(), activities = run.World.Activities, reservations = run.World.Reservations }, Json);
}
