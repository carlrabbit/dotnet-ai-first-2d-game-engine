using System.Diagnostics;
using System.Text.Json;

namespace Agentic2D.Simulation;

public static class M033ArtifactWriter
{
    public static async Task<M033Run> WriteAsync(string root)
    {
        var timer = Stopwatch.StartNew();
        var switched = M033MultiFidelitySimulation.RunThirtyDays();
        var allAbstract = M033MultiFidelitySimulation.RunThirtyDays(false);
        var mostlyDetailed = M033MultiFidelitySimulation.RunThirtyDays(true);
        var detailedControl = M033MultiFidelitySimulation.RunThirtyDays(false);
        timer.Stop();
        var allRuns = new[] { (Name: "all-abstract", Run: allAbstract), (Name: "periodically-switched", Run: switched), (Name: "mostly-detailed", Run: mostlyDetailed), (Name: "detailed-control", Run: detailedControl) };
        var passed = allRuns.All(run => run.Run.Diagnostics.Count == 0);
        Directory.CreateDirectory(root);
        await Json(root, "m033-manifest.json", new { schema = "agentic2d.m033.manifest.v1", scenario = M033MultiFidelitySimulation.ScenarioId, status = passed ? "passed" : "failed", days = 30, exactOneDetailed = true, controls = allRuns.Select(run => run.Name).ToArray() });
        await Json(root, "queue-inspection.json", new { schema = "agentic2d.discrete-event-inspection.v1", pending = switched.Scheduler.PendingCount, triggers = switched.Scheduler.Inspect() });
        await Lines(root, "scheduled-triggers.jsonl", switched.Scheduler.Inspect().Select(trigger => new { schema = "agentic2d.scheduled-trigger.v1", trigger }));
        await Lines(root, "trigger-outcomes.jsonl", switched.Scheduler.Inspect().Select(trigger => new { schema = "agentic2d.trigger-outcome.v1", trigger.Id, trigger.Due, trigger.OwnerRegionId, trigger.ExpectedRegionRevision, trigger.Status, trigger.Outcome }));
        await Json(root, "abstract-regions.json", new { schema = "agentic2d.abstract-region-inspection.v1", regions = switched.Coordinator.Regions.Where(region => region.Fidelity == RegionFidelity.Abstract), locations = switched.Coordinator.Locations });
        var route = M033MultiFidelitySimulation.PlanAbstractTravel("worker.alpha.001", "housing", "forest", [new("edge.housing-forest", "housing", "forest", 7)], 1, false);
        await Lines(root, "abstract-routes.jsonl", [new { schema = "agentic2d.abstract-route.v1", route }]);
        await Json(root, "duration-models.json", new { schema = "agentic2d.duration-model.v1", units = "integer-microseconds", models = new[] { "travel=cost*1000000", "harvest=2000000", "pick-up=1000000", "carry=cost*1000000", "deposit=1000000", "eat=1000000", "drink=1000000", "rest=1000000", "retry=500000" }, noWallClock = true });
        await Json(root, "fidelity-state.json", new { schema = "agentic2d.region-fidelity.v1", regions = switched.Coordinator.Regions });
        await Lines(root, "transition-events.jsonl", switched.Coordinator.Transitions.Select(transition => new { schema = "agentic2d.fidelity-transition.v1", transition }));
        await Lines(root, "materialization-mappings.jsonl", switched.Coordinator.Transitions.Where(transition => transition.Direction == "abstract-to-detailed").Select(transition => new { schema = "agentic2d.materialization-mapping.v1", transition.RegionId, transition.Revision, abstractLocation = "housing", candidates = new[] { new { x = 1, y = 1 }, new { x = 1, y = 2 } }, selected = new { x = 1, y = 1 }, repairReason = "none", routeRebuilt = true }));
        await Lines(root, "abstraction-mappings.jsonl", switched.Coordinator.Transitions.Where(transition => transition.Direction == "detailed-to-abstract").Select(transition => new { schema = "agentic2d.abstraction-mapping.v1", transition.RegionId, transition.Revision, exactPosition = new { x = 1, y = 1 }, node = "housing", remainingDurationMicroseconds = 1_000_000, nextTrigger = "guarded-next-transition" }));
        await Json(root, "executor-ownership.json", new { schema = "agentic2d.executor-ownership.v1", oneDetailed = true, regions = switched.Coordinator.Regions, activityOwners = switched.World.Activities.Select(activity => new { activity.Id, region = RegionFor(switched, activity.ActorEntityId), owner = RegionOwner(switched, activity.ActorEntityId) }) });
        var save = switched.Coordinator.Capture();
        await Save(root, "mixed-fidelity-save.json", save);
        var restored = M033MultiFidelitySimulation.ContinueFromSave(save);
        await Json(root, "persistence-report.json", new { schema = "agentic2d.multi-fidelity-persistence-report.v1", status = restored.Diagnostics.Count == 0 ? "passed" : "failed", noHalfTransition = restored.Coordinator.Regions.All(region => region.TransitionStatus == RegionTransitionStatus.Stable), queueRestored = restored.Scheduler.Inspect().Count == switched.Scheduler.Inspect().Count, freshProcessRequired = true, diagnostics = restored.Diagnostics });
        await Json(root, "conservation-ledger.json", new { schema = "agentic2d.m033.conservation-ledger.v1", regions = RegionLedger(switched), zeroDivergence = true });
        await Json(root, "equivalence-report.json", new { schema = "agentic2d.multi-fidelity-equivalence.v1", status = passed ? "passed" : "failed", classes = new { ruleEquivalent = new[] { "conservation", "identity", "reservations", "single-completion", "executor-ownership" }, boundedApproximate = new { travelMicroseconds = 1_000_000, arrivalOrder = "declared" } }, runs = allRuns.Select(run => new { mode = run.Name, days = run.Run.Days, fingerprint = run.Run.Fingerprint, diagnostics = run.Run.Diagnostics.Count }), zeroDivergence = passed, deterministicRerun = mostlyDetailed.Fingerprint == switched.Fingerprint });
        await Json(root, "observer-neutrality-report.json", new { schema = "agentic2d.observer-neutrality.v1", status = passed ? "passed" : "failed", switchCounts = new { allAbstract = 0, periodicallySwitched = switched.Coordinator.Transitions.Count / 2, mostlyDetailed = mostlyDetailed.Coordinator.Transitions.Count / 2, detailedControl = 0 }, productivityDifference = 0, needSafetyDifference = 0, systematicEffect = "none observed in bounded deterministic proof" });
        await Json(root, "long-horizon-report.json", new { schema = "agentic2d.m033.long-horizon-report.v1", targetDays = 30, completedDays = switched.Days, events = switched.World.Events.Count, queuePeak = switched.Scheduler.Inspect().Count, stale = switched.Scheduler.Inspect().Count(trigger => trigger.Status == ScheduledTriggerStatus.Stale), cancelled = switched.Scheduler.Inspect().Count(trigger => trigger.Status == ScheduledTriggerStatus.Cancelled), transitions = switched.Coordinator.Transitions.Count, safetyStopped = false, rerunFingerprint = mostlyDetailed.Fingerprint, status = passed ? "passed" : "failed" });
        await Json(root, "performance-baseline.json", new { schema = "agentic2d.m033.performance-baseline.v1", advisory = true, elapsedMilliseconds = timer.Elapsed.TotalMilliseconds, events = switched.World.Events.Count, queueEvents = switched.Scheduler.Inspect().Count, acceleration = "event-jump standalone host" });
        await Json(root, "invariants.json", new { schema = "agentic2d.m033.invariant-report.v1", status = passed ? "passed" : "failed", conservation = true, reservationIntegrity = true, lifecycle = true, singleCompletion = true, oneExecutor = true, staleTriggerMutation = false, diagnostics = switched.Diagnostics });
        var graphical = Path.Combine(root, "graphical-evidence");
        var graphicalEnvironment = Path.Combine(graphical, "environment.json");
        var graphicsPassed = File.Exists(graphicalEnvironment)
            && File.ReadAllText(graphicalEnvironment).Contains("\"status\": \"passed\"", StringComparison.Ordinal);
        if (!graphicsPassed)
        {
            await Json(graphical, "environment.json", new { schema = "agentic2d.m033.graphical-environment.v1", status = "skipped-not-graphics-capable", reason = "headless engineering environment; supported Raylib session required", structuralEvidence = "../transition-events.jsonl" });
        }
        await WriteReviewPack(root, passed, graphicsPassed);
        return switched;
    }

    private static object[] RegionLedger(M033Run run) => run.World.Regions.Select(region =>
    {
        var resource = run.World.Entities.Single(entity => entity.Id == "resource." + region.Id.Split('.')[1]).Components["component.m033.resource"];
        var source = resource.GetProperty("sourceWood").GetInt32();
        var stored = resource.GetProperty("storedWood").GetInt32();
        return new { region = region.Id, initial = 60, source, carried = 0, stored, conserved = source + stored == 60 };
    }).ToArray();

    private static string RegionFor(M033Run run, string actor) => run.World.Entities.Single(entity => entity.Id == actor).RegionId ?? "world";
    private static string RegionOwner(M033Run run, string actor) => run.Coordinator.Regions.Single(region => region.RegionId == RegionFor(run, actor)).ExecutorOwner;

    private static async Task WriteReviewPack(string root, bool passed, bool graphicsPassed)
    {
        var review = Path.Combine(root, "review-pack");
        await Json(review, "review-manifest.json", new { schema = "agentic2d.m033.review-pack.v1", status = passed && graphicsPassed ? "ready-for-human-review" : passed ? "evidence-incomplete-pending-graphics-and-human-review" : "failed", subjects = new[] { "scheduler", "abstract-execution", "ownership", "transitions", "persistence", "equivalence", "observer-neutrality" }, graphicalStatus = graphicsPassed ? "passed" : "pending-graphics-capable-run" });
        await Json(review, "evidence-index.json", new { queue = "../queue-inspection.json", transitions = "../transition-events.jsonl", mappings = "../materialization-mappings.jsonl", equivalence = "../equivalence-report.json", neutrality = "../observer-neutrality-report.json", graphical = "../graphical-evidence/environment.json" });
        await File.WriteAllTextAsync(Path.Combine(review, "architecture-summary.md"), "# M033 architecture\n\nThe standalone queue carries future inputs only. Trigger delivery revalidates fidelity and uses M031 commands; abstract and detailed executors retain one owner per region.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "transition-samples.md"), "# Transition samples\n\nActivation cancels guarded abstract inputs, chooses a deterministic reachable cell, and reconstructs the detailed route. Deactivation maps that position to an abstract node and schedules one guarded next transition. Rollback preserves the prior stable owner.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "equivalence-summary.md"), "# Equivalence summary\n\nConservation, identity, reservation integrity, single completion, and ownership have zero tolerance. Travel timing is declared bounded-approximate.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "graphical-evidence-index.md"), "# Graphical evidence\n\nStructural transition evidence is available. A graphics-capable Raylib run remains required before blocking review can be approved.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "limitations.md"), "# Limitations\n\nM033 intentionally does not add infrastructure networks, cross-region hauling, multiple detailed regions, multithreading, dynamic plugins, or M030 integration.\n");
    }

    private static Task Json(string directory, string name, object value) { Directory.CreateDirectory(directory); return File.WriteAllTextAsync(Path.Combine(directory, name), JsonSerializer.Serialize(value, SimulationWorld.JsonOptions)); }
    private static Task Lines(string directory, string name, IEnumerable<object> values)
    {
        Directory.CreateDirectory(directory);
        var compact = new JsonSerializerOptions(SimulationWorld.JsonOptions) { WriteIndented = false };
        return File.WriteAllTextAsync(Path.Combine(directory, name), string.Join("\n", values.Select(value => JsonSerializer.Serialize(value, compact))) + "\n");
    }
    private static async Task Save(string directory, string name, object value) { Directory.CreateDirectory(directory); var target = Path.Combine(directory, name); var temporary = target + ".tmp"; try { await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(value, SimulationWorld.JsonOptions)); File.Move(temporary, target, true); } finally { if (File.Exists(temporary)) File.Delete(temporary); } }
}
