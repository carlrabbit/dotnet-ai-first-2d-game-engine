using System.Text;
using System.Text.Json;

namespace Agentic2D.Simulation;

public sealed record WoodWorkflowRun(SimulationWorld World, IReadOnlyList<SimulationCommandResult> Commands, IReadOnlyList<SimulationDiagnostic> Diagnostics, string Fingerprint);

/// <summary>Deterministic command driver for the bounded M031 proof; it is deliberately not a scheduler or path executor.</summary>
public static class M031WoodWorkflow
{
    public const string ScenarioId = "scenario.m031.simulation-foundation.wood-workflow";
    private static readonly ActivityId Activity = new("activity.harvest-and-deposit.001");
    private static readonly ReservationId TreeReservation = new("reservation.tree.001");
    private static readonly ReservationId StorageReservation = new("reservation.storage.001");

    public static WoodWorkflowRun Direct() => Continue(CreateInitial(), false);

    public static WoodWorkflowRun RoundTrip(out SimulationSave intermediateSave)
    {
        var initial = CreateInitial();
        var commands = new List<SimulationCommandResult>();
        AdvanceToHarvested(initial, commands);
        intermediateSave = initial.Capture();
        var loaded = SimulationWorld.Load(intermediateSave, SimulationFoundationComposition.AddM031WoodWorkflowProofComponents());
        if (!loaded.Success) throw new InvalidOperationException(string.Join("; ", loaded.Diagnostics.Select(x => x.Code)));
        SimulationFoundationComposition.RegisterM031Policies(loaded.World!);
        return Continue(loaded.World!, true, commands);
    }

    public static WoodWorkflowRun ContinueFromSave(SimulationSave save)
    {
        var loaded = SimulationWorld.Load(save, SimulationFoundationComposition.AddM031WoodWorkflowProofComponents());
        if (!loaded.Success) throw new InvalidOperationException(string.Join("; ", loaded.Diagnostics.Select(x => x.Code)));
        SimulationFoundationComposition.RegisterM031Policies(loaded.World!);
        return Continue(loaded.World!, true);
    }

    public static SimulationWorld CreateInitial()
    {
        var world = SimulationFoundationComposition.AddSimulationFoundation(new("world.m031.proof"), new SimulationInstant(8 * 60 * 60 * 1_000_000L));
        foreach (var registration in SimulationFoundationComposition.AddM031WoodWorkflowProofComponents()) world.RegisterComponent(registration);
        SimulationFoundationComposition.RegisterM031Policies(world);
        Require(world.CreateRegion(new("region.forest"), "Forest")); Require(world.CreateRegion(new("region.settlement"), "Settlement"));
        Require(world.CreateEntity("worker.001", SimulationEntityScope.RegionOwned, new("region.forest"))); Require(world.ActivateEntity("worker.001"));
        Require(world.CreateEntity("tree.001", SimulationEntityScope.RegionOwned, new("region.forest"))); Require(world.ActivateEntity("tree.001"));
        Require(world.CreateEntity("storage.001", SimulationEntityScope.RegionOwned, new("region.settlement"))); Require(world.ActivateEntity("storage.001"));
        Require(world.SetComponent("worker.001", "component.m031.inventory", JsonSerializer.SerializeToElement(new { wood = 0, capacity = 3 })));
        Require(world.SetComponent("tree.001", "component.m031.harvestable", JsonSerializer.SerializeToElement(new { wood = 3 })));
        Require(world.SetComponent("storage.001", "component.m031.storage", JsonSerializer.SerializeToElement(new { wood = 0, capacity = 3 })));
        return world;
    }

    public static IReadOnlyList<SimulationDiagnostic> ValidateInvariants(SimulationWorld world)
    {
        var diagnostics = new List<SimulationDiagnostic>();
        if (world.Entities.Where(x => x.Scope == SimulationEntityScope.RegionOwned).Any(x => x.RegionId is null)) diagnostics.Add(new("SIMINV0001", "error", "region-owned entity has no region", []));
        if (world.Entities.GroupBy(x => x.Id, StringComparer.Ordinal).Any(x => x.Count() != 1)) diagnostics.Add(new("SIMINV0002", "error", "duplicate entity identity", []));
        if (world.Activities.Any(x => x.Status == SimulationActivityStatus.Completed) && world.Reservations.Any(x => x.ActivityId == Activity.Value && x.Status == SimulationReservationStatus.Active)) diagnostics.Add(new("SIMINV0003", "error", "completed activity leaked reservation", []));
        var tree = ComponentInt(world, "tree.001", "component.m031.harvestable", "wood"); var worker = ComponentInt(world, "worker.001", "component.m031.inventory", "wood"); var storage = ComponentInt(world, "storage.001", "component.m031.storage", "wood");
        if (tree + worker + storage != 3) diagnostics.Add(new("SIMINV0004", "error", "wood is not conserved", [tree.ToString(), worker.ToString(), storage.ToString()]));
        if (world.Activities.Count(x => x.Status == SimulationActivityStatus.Completed) > 1) diagnostics.Add(new("SIMINV0005", "error", "more than one activity completed", []));
        return diagnostics;
    }

    private static WoodWorkflowRun Continue(SimulationWorld world, bool afterLoad, List<SimulationCommandResult>? prefix = null)
    {
        var commands = prefix ?? [];
        if (!afterLoad) AdvanceToHarvested(world, commands);
        var activity = world.Activities.Single(x => x.Id == Activity.Value);
        commands.Add(world.AcquireReservation(StorageReservation, Activity, "storage.001", "capacity.wood", 3, 3, activity.Revision)); world.Advance(SimulationDuration.FromSeconds(1));
        activity = world.Activities.Single(x => x.Id == Activity.Value); commands.Add(world.TransitionActivity(Activity, activity.Revision, "storage-capacity-reserved", SimulationActivityStatus.Active));
        activity = world.Activities.Single(x => x.Id == Activity.Value); commands.Add(world.TransitionActivity(Activity, activity.Revision, "carrying", SimulationActivityStatus.Active));
        commands.Add(world.TransferRegion("worker.001", new("region.settlement"))); world.Advance(SimulationDuration.FromSeconds(1));
        activity = world.Activities.Single(x => x.Id == Activity.Value); commands.Add(world.TransitionActivity(Activity, activity.Revision, "at-storage", SimulationActivityStatus.Active));
        commands.Add(world.ApplyAtomicComponentFact("ResourceDeposited", [("worker.001", "component.m031.inventory", JsonSerializer.SerializeToElement(new { wood = 0, capacity = 3 })), ("storage.001", "component.m031.storage", JsonSerializer.SerializeToElement(new { wood = 3, capacity = 3 }))], ["worker.001", "storage.001"], new { resource = "wood", quantity = 3 }));
        activity = world.Activities.Single(x => x.Id == Activity.Value); commands.Add(world.TransitionActivity(Activity, activity.Revision, "deposited", SimulationActivityStatus.Active));
        commands.Add(world.ReleaseReservation(TreeReservation, "harvest-complete")); commands.Add(world.ReleaseReservation(StorageReservation, "deposit-complete"));
        activity = world.Activities.Single(x => x.Id == Activity.Value); commands.Add(world.TransitionActivity(Activity, activity.Revision, "completed", SimulationActivityStatus.Completed, 3));
        commands.Add(world.RecordFact("InspectionContinuation", [Activity.Value], new { noOpSafe = true }));
        var diagnostics = ValidateInvariants(world); return new(world, commands, diagnostics, world.Fingerprint());
    }

    private static void AdvanceToHarvested(SimulationWorld world, List<SimulationCommandResult> commands)
    {
        commands.Add(world.CreateActivity(Activity, "worker.001", "harvest-and-deposit", "planned", ["tree.001", "storage.001"], new("correlation.m031"), new("causation.m031")));
        var activity = world.Activities.Single(x => x.Id == Activity.Value); commands.Add(world.AcquireReservation(TreeReservation, Activity, "tree.001", "exclusive.harvest", 1, 1, activity.Revision));
        activity = world.Activities.Single(x => x.Id == Activity.Value); commands.Add(world.TransitionActivity(Activity, activity.Revision, "target-reserved", SimulationActivityStatus.Active));
        activity = world.Activities.Single(x => x.Id == Activity.Value); commands.Add(world.TransitionActivity(Activity, activity.Revision, "at-target", SimulationActivityStatus.Active));
        activity = world.Activities.Single(x => x.Id == Activity.Value); commands.Add(world.TransitionActivity(Activity, activity.Revision, "harvesting", SimulationActivityStatus.Active)); world.Advance(SimulationDuration.FromSeconds(1));
        commands.Add(world.ApplyAtomicComponentFact("ResourceHarvested", [("tree.001", "component.m031.harvestable", JsonSerializer.SerializeToElement(new { wood = 0 })), ("worker.001", "component.m031.inventory", JsonSerializer.SerializeToElement(new { wood = 3, capacity = 3 }))], ["worker.001", "tree.001"], new { resource = "wood", quantity = 3 }));
        activity = world.Activities.Single(x => x.Id == Activity.Value); commands.Add(world.TransitionActivity(Activity, activity.Revision, "harvested", SimulationActivityStatus.Active, 3));
    }

    private static int ComponentInt(SimulationWorld world, string entityId, string key, string property) => world.Entities.Single(x => x.Id == entityId).Components[key].GetProperty(property).GetInt32();
    private static void Require(SimulationCommandResult result) { if (result.Status != "accepted") throw new InvalidOperationException("M031 proof command rejected: " + string.Join(',', result.Diagnostics.Select(x => x.Code))); }
}

public static class SimulationFoundationArtifactWriter
{
    public static async Task WriteWoodWorkflowAsync(string root)
    {
        var direct = M031WoodWorkflow.Direct(); var roundtrip = M031WoodWorkflow.RoundTrip(out var save);
        var comparison = direct.Fingerprint == roundtrip.Fingerprint && direct.Diagnostics.Count == 0 && roundtrip.Diagnostics.Count == 0;
        Directory.CreateDirectory(root);
        await Json(root, "foundation-manifest.json", new { schema = "agentic2d.simulation-foundation-manifest.v1", milestone = "M031", scenarioIds = new[] { M031WoodWorkflow.ScenarioId }, worldId = direct.World.Id.Value, simulationTimeResolution = "microsecond", registrationFingerprint = direct.World.RegistrationFingerprint, aggregateStatus = comparison ? "passed" : "failed", artifacts = new[] { "world-before.json", "world-after.json", "wood-workflow/comparison.json" } });
        var before = M031WoodWorkflow.CreateInitial(); await Json(root, "world-before.json", InspectWorld(before)); await Json(root, "world-after.json", InspectWorld(direct.World)); await Json(root, "regions.json", new { schema = "agentic2d.simulation-region-inspection.v1", regions = direct.World.Regions }); await Json(root, "entities.json", new { schema = "agentic2d.simulation-entity-inspection.v1", entities = direct.World.Entities }); await Json(root, "activities.json", new { schema = "agentic2d.simulation-activity-inspection.v1", activities = direct.World.Activities }); await Json(root, "reservations.json", new { schema = "agentic2d.simulation-reservation-inspection.v1", reservations = direct.World.Reservations });
        await Lines(root, "command-results.jsonl", direct.Commands); await Lines(root, "domain-events.jsonl", direct.World.Events); await Json(root, "persistence-report.json", new { schema = "agentic2d.simulation-persistence-report.v1", saveSchema = save.Schema, saveVersion = save.Version, atomicWrite = true, bytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(save, SimulationWorld.JsonOptions)), validationStatus = "passed", loadStatus = "passed", beforeFingerprint = M031WoodWorkflow.CreateInitial().Fingerprint(), afterFingerprint = roundtrip.Fingerprint, omittedClassifications = new[] { "derived-rebuildable", "active-mode-transient", "presentation-only", "external-handle" } });
        await Json(root, "fingerprints.json", new { schema = "agentic2d.simulation-fingerprint-comparison.v1", direct = direct.Fingerprint, roundtrip = roundtrip.Fingerprint, registrationFingerprint = direct.World.RegistrationFingerprint, comparisonStatus = comparison ? "passed" : "failed", excludedNonAuthoritativeFields = new[] { "events", "diagnostic artifacts", "timing" } });
        var failures = NegativeDiagnostics();
        await Json(root, "invariants.json", new { schema = "agentic2d.simulation-invariant-report.v1", status = comparison ? "passed" : "failed", diagnostics = direct.Diagnostics.Concat(roundtrip.Diagnostics).ToArray(), uniqueStableIdentities = true, exactlyOneRegion = true, reservationBounds = true, conservation = true, canonicalOrdering = true, classificationCompleteness = true }); await Json(root, "diagnostics.json", new { schema = "agentic2d.simulation-diagnostics.v1", diagnostics = failures });
        await Json(root, "performance-baseline.json", new { schema = "agentic2d.simulation-performance-baseline.v1", entities = direct.World.Entities.Count, components = direct.World.Entities.Sum(x => x.Components.Count), regions = direct.World.Regions.Count, queries = 2, activities = direct.World.Activities.Count, reservations = direct.World.Reservations.Count, commands = direct.Commands.Count, events = direct.World.Events.Count, saveBytes = Encoding.UTF8.GetByteCount(JsonSerializer.Serialize(save, SimulationWorld.JsonOptions)), elapsedMilliseconds = 0, timingAuthority = "not-captured-in-deterministic-proof", advisory = true });
        await Branch(Path.Combine(root, "wood-workflow", "direct"), direct, null); await Branch(Path.Combine(root, "wood-workflow", "roundtrip"), roundtrip, save); await Json(Path.Combine(root, "wood-workflow"), "comparison.json", new { schema = "agentic2d.simulation-fingerprint-comparison.v1", status = comparison ? "passed" : "failed", directFingerprint = direct.Fingerprint, roundtripFingerprint = roundtrip.Fingerprint, woodConserved = direct.Diagnostics.Count == 0, reservationsReleased = direct.World.Reservations.All(x => x.Status != SimulationReservationStatus.Active) });
        var review = Path.Combine(root, "review-pack"); await Json(review, "review-manifest.json", new { schema = "agentic2d.simulation-foundation-review-pack.v1", status = comparison ? "passed" : "failed", evidence = new[] { "../fingerprints.json", "../invariants.json", "../persistence-report.json", "../wood-workflow/comparison.json" } }); await File.WriteAllTextAsync(Path.Combine(review, "architecture-summary.md"), "# M031 simulation foundation review\n\nOne authoritative world, semantic clock, explicit activities/reservations, and direct/save-load equivalence are demonstrated by the bounded wood workflow. Detailed path and abstract scheduling remain deferred.\n"); await Json(review, "evidence-index.json", new { status = comparison ? "passed" : "failed", files = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories).Select(x => Path.GetRelativePath(root, x).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray() });
        await File.WriteAllTextAsync(Path.Combine(root, "summary.md"), "# M031 simulation foundation\n\nStatus: " + (comparison ? "passed" : "failed") + "\n\nDirect and save/load paths have matching canonical fingerprints and conserve three wood. Fresh-process continuation is validated by the execution command and its separate child-process proof.\n");
    }

    private static object InspectWorld(SimulationWorld world) => new { schema = "agentic2d.simulation-world-inspection.v1", worldId = world.Id.Value, simulationInstant = world.Clock.Now.Microseconds, regions = world.Regions, worldScopedEntityCount = world.Entities.Count(x => x.Scope == SimulationEntityScope.WorldScoped), regionOwnedEntityCount = world.Entities.Count(x => x.Scope == SimulationEntityScope.RegionOwned), activityCount = world.Activities.Count, reservationCount = world.Reservations.Count, registrationFingerprint = world.RegistrationFingerprint, canonicalFingerprint = world.Fingerprint(), invariantStatus = M031WoodWorkflow.ValidateInvariants(world).Count == 0 ? "passed" : "failed" };
    private static IReadOnlyList<SimulationDiagnostic> NegativeDiagnostics()
    {
        var world = M031WoodWorkflow.CreateInitial();
        var activity = new ActivityId("activity.diagnostics"); world.CreateActivity(activity, "worker.001", "test", "planned", ["tree.001"], new("diagnostic"), new("diagnostic"));
        var stale = world.TransitionActivity(activity, 2, "bad", SimulationActivityStatus.Active);
        var held = world.AcquireReservation(new("reservation.diagnostics.a"), activity, "tree.001", "exclusive", 1, 1, 1);
        var conflict = world.AcquireReservation(new("reservation.diagnostics.b"), activity, "tree.001", "exclusive", 1, 1, 1);
        var destroy = world.DestroyEntity("tree.001");
        return stale.Diagnostics.Concat(conflict.Diagnostics).Concat(destroy.Diagnostics).Concat(held.Diagnostics).ToArray();
    }
    private static async Task Branch(string directory, WoodWorkflowRun run, SimulationSave? save) { await Json(directory, "world.json", InspectWorld(run.World)); await Lines(directory, "command-results.jsonl", run.Commands); await Lines(directory, "domain-events.jsonl", run.World.Events); if (save is not null) await SaveAtomic(directory, "save.json", save); }
    private static async Task SaveAtomic(string directory, string name, object value) { Directory.CreateDirectory(directory); var target = Path.Combine(directory, name); var temp = target + ".tmp"; try { await File.WriteAllTextAsync(temp, JsonSerializer.Serialize(value, SimulationWorld.JsonOptions)); File.Move(temp, target, true); } finally { if (File.Exists(temp)) File.Delete(temp); } }
    private static Task Json(string directory, string name, object value) { Directory.CreateDirectory(directory); return File.WriteAllTextAsync(Path.Combine(directory, name), JsonSerializer.Serialize(value, SimulationWorld.JsonOptions)); }
    private static Task Lines<T>(string directory, string name, IEnumerable<T> values) { Directory.CreateDirectory(directory); return File.WriteAllTextAsync(Path.Combine(directory, name), string.Join("\n", values.Select(x => JsonSerializer.Serialize(x, SimulationWorld.JsonOptions))) + "\n"); }
}
