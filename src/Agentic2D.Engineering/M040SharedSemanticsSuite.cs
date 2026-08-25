using System.Diagnostics;
using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Engineering;

internal static class M040SharedSemanticsSuite
{
    public static async Task<int> RunAsync(string root, string shard, TextWriter diagnostics)
    {
        var evidenceRoot = Path.Combine(root, "artifacts", "simulation", "M040");
        Directory.CreateDirectory(evidenceRoot);
        var result = shard switch
        {
            "shared-semantics" => Shared(root),
            "abstract-scheduler-guards" => SchedulerGuards(),
            "abstract-work-logistics" => Logistics(),
            "abstract-needs" => Needs(),
            "abstract-travel-duration" => Travel(root),
            "abstract-persistence-continuation" => await Persistence(root),
            "executor-separation" => Separation(root),
            "detailed-regression" => Detailed(),
            _ => (false, new { error = "unknown M040 shard" })
        };
        await File.WriteAllTextAsync(Path.Combine(evidenceRoot, shard + ".json"), JsonSerializer.Serialize(new { schema = "agentic2d.m040.observation.v1", milestone = "M040", shard, status = result.Item1 ? "passed" : "failed", observedAtUtc = DateTimeOffset.UtcNow, evidence = result.Item2 }, new JsonSerializerOptions { WriteIndented = true }));
        await diagnostics.WriteLineAsync($"m040 evidence written for {shard}: {(result.Item1 ? "passed" : "failed")}");
        return result.Item1 ? 0 : 1;
    }

    private static (bool, object) Shared(string root)
    {
        try
        {
            var run = M040AbstractExecutor.Create();
            var before = run.World.Fingerprint();
            var opportunities = M032AutonomousDetailedRegion.DeriveOpportunities(run.World, M032AutonomousDetailedRegion.InspectDesignations(run.World));
            var after = run.World.Fingerprint();
            var typedState = run.World.TryGetComponent<M032WorkerComponent>("worker.001", "component.m032.worker", out var worker) && worker is not null
                && run.World.TryGetComponent<M032StorageComponent>("storage.wood.001", "component.m032.storage", out var storage) && storage is not null;
            var noM033Gameplay = !File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Simulation", "M040SharedSimulation.cs")).Contains("M033WorkerComponent", StringComparison.Ordinal);
            var sameSemanticWorld = run.World.TryGetComponent<M032HarvestableComponent>("tree.001", "component.m032.harvestable", out _);
            return (typedState && sameSemanticWorld && before == after && opportunities.Any(x => x.Key == "harvest:tree.001") && noM033Gameplay, new { typedAuthoritativeState = typedState, sharedM032SemanticWorld = sameSemanticWorld, readOnlyDerivation = before == after, harvestOpportunityObserved = opportunities.Any(x => x.Key == "harvest:tree.001"), noDuplicateM033GameplayModel = noM033Gameplay });
        }
        catch (Exception exception) { return (false, new { error = exception.Message }); }
    }

    private static (bool, object) SchedulerGuards()
    {
        try
        {
            var run = M040AbstractExecutor.Create();
            var need = run.Scheduler.Inspect().Single(x => x.Kind == "abstract.need-mandatory");
            var cancelled = run.Scheduler.Cancel(need.Id, "test-cancel");
            var duplicateRejected = false;
            try { run.Scheduler.Schedule(new(need.Id, need.Due, need.PriorityClass, need.OwnerRegionId, need.OwnerActivityId, need.OwnerEntityId, need.Kind, need.ExpectedActivityRevision, need.ExpectedRegionRevision, need.CorrelationId, need.CausationId, need.Payload)); }
            catch (InvalidOperationException) { duplicateRejected = true; }
            var staleSave = M040AbstractExecutor.Capture(run) with { Schema = "agentic2d.invalid-save.v1" };
            var staleRejected = false;
            try { _ = M040AbstractExecutor.Restore(staleSave); staleRejected = false; } catch { staleRejected = true; }
            var advanced = M040AbstractExecutor.Advance(run, run.World.Clock.Now + SimulationDuration.FromSeconds(2));
            var noCancelledSuccess = advanced.Transitions.All(x => x != "mandatory-need-interrupt");
            var staleRun = M040AbstractExecutor.Create();
            var staleAdvanced = M040AbstractExecutor.Advance(staleRun, staleRun.World.Clock.Now + SimulationDuration.FromSeconds(10));
            var stale = staleAdvanced.Scheduler.Inspect().Any(x => x.Kind == "abstract.travel.source" && x.Status == ScheduledTriggerStatus.Stale);
            var unknownRun = M040AbstractExecutor.Create();
            var unknown = unknownRun.Scheduler.Schedule(new("trigger.m040.unknown", unknownRun.World.Clock.Now + SimulationDuration.FromSeconds(1), 0, "region.forest.active", "activity.m040.abstract.harvest.001", "worker.001", "abstract.unknown", 1, 1, "correlation.unknown", "cause.unknown", JsonSerializer.SerializeToElement(new { })));
            var unknownAdvanced = M040AbstractExecutor.Advance(unknownRun, unknownRun.World.Clock.Now + SimulationDuration.FromSeconds(2));
            var unknownFailed = unknownAdvanced.Scheduler.Inspect().Single(x => x.Id == unknown.Id).Status == ScheduledTriggerStatus.Failed;
            return (cancelled && duplicateRejected && staleRejected && noCancelledSuccess && stale && unknownFailed, new { cancelled, duplicateRejected, malformedContinuationRejected = staleRejected, cancelledTriggerDidNotMutate = noCancelledSuccess, staleTriggerObserved = stale, unknownTriggerFailed = unknownFailed, triggerStates = advanced.Scheduler.Inspect().Select(x => new { x.Id, x.Kind, status = x.Status.ToString(), x.Outcome }).ToArray() });
        }
        catch (Exception exception) { return (false, new { error = exception.Message }); }
    }

    private static (bool, object) Logistics()
    {
        try
        {
            var run = M040AbstractExecutor.Advance(M040AbstractExecutor.Create(), M040AbstractExecutor.Create().World.Clock.Now + SimulationDuration.FromSeconds(60));
            var worker = run.World.TryGetComponent<M032WorkerComponent>("worker.001", "component.m032.worker", out var workerValue) ? workerValue!.Wood : -1;
            var storage = run.World.TryGetComponent<M032StorageComponent>("storage.wood.001", "component.m032.storage", out var storageValue) ? storageValue!.Wood : -1;
            var tree = run.World.TryGetComponent<M032HarvestableComponent>("tree.001", "component.m032.harvestable", out var treeValue) ? treeValue!.Wood : -1;
            var staged = new[] { "arrival:abstract.travel.source", "harvest-complete:source-to-inventory", "arrival:abstract.travel.storage", "deposit-complete:inventory-to-storage" }.All(run.Transitions.Contains);
            var terminal = run.World.Activities.Single(x => x.Id == "activity.m040.abstract.harvest.001").Status == SimulationActivityStatus.Completed && run.World.Reservations.All(x => x.Status != SimulationReservationStatus.Active);
            var conserved = tree + worker + storage == 3;
            return (staged && terminal && conserved, new { stagedTransitions = run.Transitions, sourceWood = tree, inventoryWood = worker, storageWood = storage, conserved, terminalReservationsReleased = terminal });
        }
        catch (Exception exception) { return (false, new { error = exception.Message }); }
    }

    private static (bool, object) Needs()
    {
        try
        {
            var run = M040AbstractExecutor.Advance(M040AbstractExecutor.Create(), M040AbstractExecutor.Create().World.Clock.Now + SimulationDuration.FromSeconds(60));
            var worker = run.World.TryGetComponent<M032WorkerComponent>("worker.001", "component.m032.worker", out var value) ? value! : throw new InvalidOperationException("worker missing");
            var interrupted = run.Transitions.Contains("mandatory-need-interrupt");
            var satisfied = run.Transitions.Contains("need-satisfied-and-re-evaluated");
            var resumed = run.Transitions.Contains("deposit-complete:inventory-to-storage");
            return (interrupted && satisfied && resumed && worker.Food == 0, new { mandatoryInterruptObserved = interrupted, satisfactionObserved = satisfied, workReevaluationObserved = resumed, finalFood = worker.Food, needChangedExecution = interrupted && satisfied });
        }
        catch (Exception exception) { return (false, new { error = exception.Message }); }
    }

    private static (bool, object) Travel(string root)
    {
        try
        {
            var graph = M040AbstractExecutor.Graph();
            var route = M040AbstractExecutor.PlanRoute("worker.001", "housing", "tree", graph, false);
            var carry = M040AbstractExecutor.PlanRoute("worker.001", "tree", "storage", graph, true);
            var source = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Simulation", "M040SharedSimulation.cs"));
            var independent = !source.Contains("FindRoute", StringComparison.Ordinal) && !source.Contains("M032AutonomousDetailedRegion.FindRoute", StringComparison.Ordinal);
            var typedDue = M040AbstractExecutor.DurationMicroseconds("travel", route.Cost, false) != M040AbstractExecutor.DurationMicroseconds("travel", carry.Cost, true);
            return (route.EdgeIds.Count > 1 && carry.EdgeIds.Count > 0 && route.Cost > 0 && typedDue && independent, new { multiEdgeRoute = route.EdgeIds, carryingRoute = carry.EdgeIds, sourceCost = route.Cost, carryingCost = carry.Cost, durationDueInputsObserved = typedDue, noDetailedPathfinderDependency = independent });
        }
        catch (Exception exception) { return (false, new { error = exception.Message }); }
    }

    private static async Task<(bool, object)> Persistence(string root)
    {
        try
        {
            var initial = M040AbstractExecutor.Create();
            var checkpoint = M040AbstractExecutor.Advance(initial, initial.World.Clock.Now + SimulationDuration.FromSeconds(10));
            var target = checkpoint.World.Clock.Now + SimulationDuration.FromSeconds(40);
            var uninterrupted = M040AbstractExecutor.Advance(M040AbstractExecutor.Create(), target);
            var resumed = M040AbstractExecutor.Advance(M040AbstractExecutor.Restore(M040AbstractExecutor.Capture(checkpoint)), target);
            var malformedRejected = false;
            try { _ = M040AbstractExecutor.Restore(M040AbstractExecutor.Capture(checkpoint) with { Schema = "bad" }); } catch { malformedRejected = true; }
            var equal = resumed.Fingerprint == uninterrupted.Fingerprint && resumed.World.Clock.Now.Microseconds == target.Microseconds;
            var fresh = await FreshProcess(root);
            return (equal && malformedRejected && fresh.Passed, new { checkpoint = checkpoint.World.Clock.Now.Microseconds, target = target.Microseconds, advancedBeyondCheckpoint = target.Microseconds > checkpoint.World.Clock.Now.Microseconds, resumedFingerprint = resumed.Fingerprint, uninterruptedFingerprint = uninterrupted.Fingerprint, equal, malformedContinuationRejected = malformedRejected, freshProcess = fresh.Evidence });
        }
        catch (Exception exception) { return (false, new { error = exception.Message }); }
    }

    private static async Task<(bool Passed, object Evidence)> FreshProcess(string root)
    {
        var output = Path.Combine(root, "artifacts", "simulation", "M040", "fresh-process");
        Directory.CreateDirectory(output);
        var tool = Path.Combine(root, "src", "Agentic2D.Tools", "bin", "Debug", "net10.0", "Agentic2D.Tools.dll");
        async Task<(int Exit, int Pid, string Stdout, string Stderr)> Run(string mode)
        {
            using var process = Process.Start(new ProcessStartInfo("dotnet", $"\"{tool}\" simulation m040-abstract {mode} --output \"{output}\"") { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true })!;
            var stdout = await process.StandardOutput.ReadToEndAsync(); var stderr = await process.StandardError.ReadToEndAsync(); await process.WaitForExitAsync();
            return (process.ExitCode, process.Id, stdout, stderr);
        }
        var producer = await Run("producer");
        var consumer = await Run("consumer");
        using var producerDoc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "producer.json")));
        using var consumerDoc = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(output, "consumer.json")));
        var p = producerDoc.RootElement; var c = consumerDoc.RootElement;
        var passed = producer.Exit == 0 && consumer.Exit == 0 && producer.Pid != consumer.Pid && c.GetProperty("status").GetString() == "passed" && c.GetProperty("advancedBeyondCheckpoint").GetBoolean() && c.GetProperty("resumedFingerprint").GetString() == c.GetProperty("uninterruptedFingerprint").GetString();
        return (passed, new { separateOsProcesses = producer.Pid != consumer.Pid, producerProcessId = producer.Pid, consumerProcessId = consumer.Pid, producerExitCode = producer.Exit, consumerExitCode = consumer.Exit, producerOutput = producer.Stdout.Trim(), producerError = producer.Stderr.Trim(), resumedFingerprint = c.GetProperty("resumedFingerprint").GetString(), uninterruptedFingerprint = c.GetProperty("uninterruptedFingerprint").GetString(), advancedBeyondCheckpoint = c.GetProperty("advancedBeyondCheckpoint").GetBoolean(), consumerStatus = c.GetProperty("status").GetString() });
    }

    private static (bool, object) Separation(string root)
    {
        var abstractSource = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Simulation", "M040SharedSimulation.cs"));
        var detailedSource = File.ReadAllText(Path.Combine(root, "src", "Agentic2D.Simulation", "M032AutonomousDetailedRegion.cs"));
        var commonCommands = abstractSource.Contains("ApplyAtomicTypedComponentFact", StringComparison.Ordinal) && detailedSource.Contains("ApplyAtomicTypedComponentFact", StringComparison.Ordinal);
        var abstractIndependent = !abstractSource.Contains("FindRoute", StringComparison.Ordinal) && !abstractSource.Contains("RegionFidelityCoordinator", StringComparison.Ordinal);
        var detailedPathfinderObserved = detailedSource.Contains("FindRoute", StringComparison.Ordinal);
        return (commonCommands && abstractIndependent && detailedPathfinderObserved, new { commonSemanticCommandObserved = commonCommands, abstractIndependentContinuation = abstractIndependent, detailedPathfinderObserved, staticModeOnly = !abstractSource.Contains("Switch", StringComparison.Ordinal) });
    }

    private static (bool, object) Detailed()
    {
        try
        {
            var run = M032AutonomousDetailedRegion.Direct();
            var realDetailed = run.Navigation.Count > 0 && run.Navigation.Any(x => x.Path.Count > 0) && run.RouteEvents.Any(x => x.StartsWith("replanned:", StringComparison.Ordinal));
            var outcomes = run.World.Activities.Any(x => x.Kind == "harvest-and-haul" && x.Status == SimulationActivityStatus.Completed) && run.Diagnostics.Count == 0;
            return (realDetailed && outcomes, new { navigationSamples = run.Navigation.Count, pathObserved = run.Navigation.Any(x => x.Path.Count > 0), routeReplanObserved = run.RouteEvents.Any(x => x.StartsWith("replanned:", StringComparison.Ordinal)), semanticOutcomes = outcomes, diagnostics = run.Diagnostics.Select(x => x.Code).ToArray() });
        }
        catch (Exception exception) { return (false, new { error = exception.Message }); }
    }
}
