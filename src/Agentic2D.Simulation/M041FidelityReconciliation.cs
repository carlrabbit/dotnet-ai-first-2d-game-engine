using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agentic2D.Simulation;

public enum M041TransitionFaultBoundary { None, AfterSourceHandoff, AfterTargetMaterialization, AfterSchedulerStaging, AfterRouteStaging, BeforeCommit }

public sealed record M041DetailedContinuation(
    string RegionId, string ActorId, string ActivityId, string Phase, DetailedCell Position,
    DetailedCell Destination, IReadOnlyList<DetailedCell> Route, int RouteIndex,
    long ProgressMicroseconds, long RemainingMicroseconds, int Epoch, int MappingRevision);

public sealed record M041AbstractContinuation(
    string RegionId, string ActorId, string ActivityId, string Phase, string NodeId,
    string DestinationNodeId, IReadOnlyList<string> EdgeIds, int EdgeIndex,
    long ProgressMicroseconds, long RemainingMicroseconds, string? NextTriggerId,
    int Epoch, int MappingRevision);

public sealed record M041RegionRuntime(
    string RegionId, RegionFidelity Fidelity, string ExecutorOwner, int Epoch,
    int MappingRevision, RegionTransitionStatus TransitionStatus,
    M041DetailedContinuation? Detailed, M041AbstractContinuation? Abstract);

public sealed record M041TransitionHandoff(
    string RegionId, RegionFidelity SourceFidelity, string ActorId, string ActivityId,
    string Phase, string Destination, long RemainingMicroseconds, string ContinuationFingerprint,
    int SourceEpoch, int MappingRevision);

public sealed record M041TransitionResult(
    string Status, string Direction, string SourceRegionId, string TargetRegionId,
    int SourceEpoch, int TargetEpoch, string BeforeFingerprint, string AfterFingerprint,
    bool SemanticStateUnchanged, bool QueueStaged, bool RouteStaged, bool TriggerInvalidated,
    bool MaterializedDeterministically, string? Diagnostic = null);

public sealed record M041Save(
    string Schema, SimulationSave World, DiscreteEventSchedulerSave Scheduler,
    IReadOnlyList<M041RegionRuntime> Regions, IReadOnlyList<M041TransitionResult> Transitions);

/// <summary>
/// M041's transition-only orchestration authority. It owns no gameplay components:
/// SimulationWorld remains the semantic authority and this class stages only executor
/// continuation, queue state, ownership and epochs.
/// </summary>
public sealed class M041FidelityCoordinator
{
    public const string SaveSchema = "agentic2d.m041.fidelity-reconciliation-save.v1";
    public const string MappingId = "m041.forest-grid-graph.v1";
    public const int MappingRevision = 1;
    private readonly SimulationWorld world;
    private DiscreteEventScheduler scheduler;
    private readonly SortedDictionary<string, M041RegionRuntime> regions;
    private readonly List<M041TransitionResult> transitions = [];

    public M041FidelityCoordinator(SimulationWorld world, DiscreteEventScheduler scheduler, IEnumerable<M041RegionRuntime> regions)
    {
        this.world = world;
        this.scheduler = scheduler;
        this.regions = new SortedDictionary<string, M041RegionRuntime>(StringComparer.Ordinal);
        foreach (var region in regions)
        {
            if (!this.regions.TryAdd(region.RegionId, region)) throw new InvalidOperationException("M041: duplicate region");
        }
        ValidateStable();
    }

    public SimulationWorld World => world;
    public DiscreteEventScheduler Scheduler => scheduler;
    public IReadOnlyList<M041RegionRuntime> Regions => regions.Values.ToArray();
    public IReadOnlyList<M041TransitionResult> Transitions => transitions.ToArray();
    public M041RegionRuntime DetailedRegion => regions.Values.Single(x => x.Fidelity == RegionFidelity.Detailed);
    public bool IsCurrentOwner(string regionId, RegionFidelity fidelity, int epoch) => regions.TryGetValue(regionId, out var region) && region.Fidelity == fidelity && region.Epoch == epoch && region.TransitionStatus == RegionTransitionStatus.Stable;

    public static M041FidelityCoordinator CreateFixture()
    {
        var world = M032AutonomousDetailedRegion.CreateInitial();
        var route = M032AutonomousDetailedRegion.FindRoute("m041.detailed.initial", "worker.001", new(1, 1), new(4, 3));
        var detailed = new M041DetailedContinuation("region.forest.active", "worker.001", "activity.m040.abstract.harvest.001", "travel-to-source", new(1, 1), new(4, 3), route.Path, 0, 1_000_000, 3_000_000, 1, MappingRevision);
        var abstractContinuation = new M041AbstractContinuation("region.forest.dormant", "worker.001", "activity.m040.abstract.harvest.001", "travel-to-source", "housing", "tree", ["edge.housing.source-a", "edge.source-a.tree"], 0, 0, M040AbstractExecutor.DurationMicroseconds("travel", 7), "m041.trigger.dormant.001", 1, MappingRevision);
        return new(world, new DiscreteEventScheduler(),
        [
            new("region.forest.active", RegionFidelity.Detailed, "detailed", 1, MappingRevision, RegionTransitionStatus.Stable, detailed, null),
            new("region.forest.dormant", RegionFidelity.Abstract, "abstract", 1, MappingRevision, RegionTransitionStatus.Stable, null, abstractContinuation)
        ]);
    }

    public M041TransitionHandoff PrepareHandoff(M041RegionRuntime region)
    {
        var continuation = region.Fidelity == RegionFidelity.Detailed ? region.Detailed?.ContinuationFingerprint() : region.Abstract?.ContinuationFingerprint();
        if (continuation is null) throw new InvalidOperationException("M041-HANDOFF: active continuation is missing");
        var actorId = region.Fidelity == RegionFidelity.Detailed ? region.Detailed!.ActorId : region.Abstract!.ActorId;
        var activityId = region.Fidelity == RegionFidelity.Detailed ? region.Detailed!.ActivityId : region.Abstract!.ActivityId;
        var phase = region.Fidelity == RegionFidelity.Detailed ? region.Detailed!.Phase : region.Abstract!.Phase;
        var remaining = region.Fidelity == RegionFidelity.Detailed ? region.Detailed!.RemainingMicroseconds : region.Abstract!.RemainingMicroseconds;
        return new(region.RegionId, region.Fidelity, actorId, activityId, phase,
            region.Fidelity == RegionFidelity.Detailed ? region.Detailed!.Destination.ToString() : region.Abstract!.DestinationNodeId,
            remaining, continuation, region.Epoch, region.MappingRevision);
    }

    public M041TransitionResult SwitchDetailed(string targetRegionId, M041TransitionFaultBoundary fault = M041TransitionFaultBoundary.None)
    {
        if (!regions.TryGetValue(targetRegionId, out var target) || target.Fidelity != RegionFidelity.Abstract)
            return Failure(targetRegionId, "M041-STATE: target must be stable abstract region");
        var source = DetailedRegion;
        if (source.RegionId == target.RegionId) return Failure(targetRegionId, "M041-STATE: source and target overlap");
        var before = StableFingerprint();
        beforeSemantic = SemanticFingerprint();
        try
        {
            var sourceHandoff = PrepareHandoff(source);
            ThrowIf(fault, M041TransitionFaultBoundary.AfterSourceHandoff, "after-source-handoff");
            var preparedSource = ToAbstract(source, sourceHandoff);
            var preparedTarget = ToDetailed(target, PrepareHandoff(target));
            ThrowIf(fault, M041TransitionFaultBoundary.AfterTargetMaterialization, "after-target-materialization");

            var stagedScheduler = DiscreteEventScheduler.Restore(scheduler.Capture());
            foreach (var trigger in stagedScheduler.Inspect().Where(x => x.OwnerRegionId == target.RegionId && x.Status == ScheduledTriggerStatus.Scheduled)) stagedScheduler.Cancel(trigger.Id, "m041-materialized-old-epoch");
            stagedScheduler.Schedule(new("m041.trigger." + source.Epoch.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + target.Epoch.ToString(System.Globalization.CultureInfo.InvariantCulture), world.Clock.Now + new SimulationDuration(Math.Max(1, preparedSource.Abstract!.RemainingMicroseconds)), 20, source.RegionId, preparedSource.Abstract.ActivityId, preparedSource.Abstract.ActorId, "m041.abstract-continuation", null, preparedSource.Epoch, "m041.transition", "m041.handoff", JsonSerializer.SerializeToElement(new { mapping = MappingId }), ExpectedGraphRevision: 7));
            ThrowIf(fault, M041TransitionFaultBoundary.AfterSchedulerStaging, "after-scheduler-staging");
            ThrowIf(fault, M041TransitionFaultBoundary.AfterRouteStaging, "after-route-staging");
            var stagedSource = preparedSource with { Epoch = source.Epoch + 1, TransitionStatus = RegionTransitionStatus.Stable };
            var stagedTarget = preparedTarget with { Epoch = target.Epoch + 1, TransitionStatus = RegionTransitionStatus.Stable };
            ValidatePrepared(stagedSource, stagedTarget, stagedScheduler);
            ThrowIf(fault, M041TransitionFaultBoundary.BeforeCommit, "before-final-validation");

            regions[source.RegionId] = stagedSource;
            regions[target.RegionId] = stagedTarget;
            scheduler = stagedScheduler;
            ValidateStable();
            var after = StableFingerprint();
            var result = new M041TransitionResult("committed", "paired-swap", source.RegionId, target.RegionId, stagedSource.Epoch, stagedTarget.Epoch, before, after, SemanticFingerprint() == beforeSemantic, true, stagedTarget.Detailed is not null, true, true);
            transitions.Add(result);
            return result;
        }
        catch (Exception exception)
        {
            var result = new M041TransitionResult("failed", "rollback", source.RegionId, target.RegionId, source.Epoch, target.Epoch, before, StableFingerprint(), StableFingerprint() == before, false, false, false, false, exception.Message);
            transitions.Add(result);
            return result;
        }
    }

    private string beforeSemantic = string.Empty;
    private string SemanticFingerprint() => world.Fingerprint();
    private string StableFingerprint() => Hash(SemanticFingerprint() + JsonSerializer.Serialize(regions.Values, SimulationWorld.JsonOptions) + JsonSerializer.Serialize(scheduler.Capture(), SimulationWorld.JsonOptions));

    private M041RegionRuntime ToAbstract(M041RegionRuntime source, M041TransitionHandoff handoff)
    {
        var detailed = source.Detailed!;
        var node = detailed.Position.X <= 2 ? "housing" : detailed.Position.X <= 4 ? "source-a" : "tree";
        var edgeIndex = node == "housing" ? 0 : node == "source-a" ? 1 : 2;
        return source with { Fidelity = RegionFidelity.Abstract, ExecutorOwner = "abstract", Abstract = new(source.RegionId, detailed.ActorId, detailed.ActivityId, detailed.Phase, node, "tree", ["edge.housing.source-a", "edge.source-a.tree"], edgeIndex, detailed.ProgressMicroseconds, detailed.RemainingMicroseconds, "m041.trigger.converted." + source.Epoch, source.Epoch, MappingRevision), Detailed = null, TransitionStatus = RegionTransitionStatus.Validating };
    }

    private M041RegionRuntime ToDetailed(M041RegionRuntime target, M041TransitionHandoff handoff)
    {
        var abstractState = target.Abstract!;
        var position = abstractState.NodeId switch { "housing" => new DetailedCell(1, 1), "source-a" => new DetailedCell(3, 2), "tree" => new DetailedCell(4, 3), "storage" => new DetailedCell(2, 6), _ => throw new InvalidOperationException("RECONCILE-POSITION: unmapped abstract node") };
        var destination = abstractState.DestinationNodeId switch { "tree" => new DetailedCell(4, 3), "storage" => new DetailedCell(2, 6), _ => position };
        var route = M032AutonomousDetailedRegion.FindRoute("m041.materialize." + target.Epoch, abstractState.ActorId, position, destination);
        return target with { Fidelity = RegionFidelity.Detailed, ExecutorOwner = "detailed", Detailed = new(target.RegionId, abstractState.ActorId, abstractState.ActivityId, abstractState.Phase, position, destination, route.Path, 0, abstractState.ProgressMicroseconds, abstractState.RemainingMicroseconds, target.Epoch, MappingRevision), Abstract = null, TransitionStatus = RegionTransitionStatus.Validating };
    }

    private void ValidatePrepared(M041RegionRuntime source, M041RegionRuntime target, DiscreteEventScheduler staged)
    {
        if (source.Fidelity != RegionFidelity.Abstract || target.Fidelity != RegionFidelity.Detailed || source.ExecutorOwner != "abstract" || target.ExecutorOwner != "detailed") throw new InvalidOperationException("M041-OWNER: prepared paired swap is invalid");
        if (source.Abstract is null || target.Detailed is null || source.MappingRevision != MappingRevision || target.MappingRevision != MappingRevision) throw new InvalidOperationException("M041-MAPPING: prepared continuation is invalid");
        if (staged.Inspect().Any(x => x.Status == ScheduledTriggerStatus.Scheduled && x.OwnerRegionId == target.RegionId)) throw new InvalidOperationException("M041-QUEUE: old target trigger remained executable");
    }

    public void ValidateStable()
    {
        if (regions.Count == 0 || regions.Values.Count(x => x.Fidelity == RegionFidelity.Detailed) != 1 || regions.Values.Any(x => x.TransitionStatus != RegionTransitionStatus.Stable || x.ExecutorOwner != (x.Fidelity == RegionFidelity.Detailed ? "detailed" : "abstract"))) throw new InvalidOperationException("M041-OWNER: exactly one stable detailed owner is required");
        if (regions.Values.Any(x => x.Fidelity == RegionFidelity.Detailed ? x.Detailed is null : x.Abstract is null)) throw new InvalidOperationException("M041-CONTINUATION: every owner needs one continuation");
    }

    public M041Save Capture() => new(SaveSchema, world.Capture(), scheduler.Capture(), Regions, transitions);

    public static M041FidelityCoordinator Restore(M041Save save)
    {
        if (save.Schema != SaveSchema) throw new InvalidOperationException("M041-PERSISTENCE: unsupported save schema");
        var loaded = SimulationWorld.Load(save.World, M032AutonomousDetailedRegion.Registrations());
        if (!loaded.Success || loaded.World is null) throw new InvalidOperationException("M041-PERSISTENCE: world restore failed");
        M032AutonomousDetailedRegion.RegisterPolicies(loaded.World);
        var coordinator = new M041FidelityCoordinator(loaded.World, DiscreteEventScheduler.Restore(save.Scheduler), save.Regions);
        coordinator.transitions.AddRange(save.Transitions);
        return coordinator;
    }

    public static string Fingerprint(M041FidelityCoordinator coordinator) => coordinator.StableFingerprint();
    private M041TransitionResult Failure(string target, string diagnostic) => new("failed", "rollback", DetailedRegion.RegionId, target, DetailedRegion.Epoch, regions.TryGetValue(target, out var state) ? state.Epoch : 0, StableFingerprint(), StableFingerprint(), true, false, false, false, false, diagnostic);
    private static void ThrowIf(M041TransitionFaultBoundary actual, M041TransitionFaultBoundary expected, string message) { if (actual == expected) throw new InvalidOperationException("M041-FAULT: " + message); }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public static class M041ExecutorBridge
{
    public static M040AbstractRun ExecuteRealAbstractStage() => M040AbstractExecutor.Advance(M040AbstractExecutor.Create(), new SimulationInstant(8 * 60 * 60 * 1_000_000L + M040AbstractExecutor.DurationMicroseconds("travel", 7)));
    public static M032Run ExecuteRealDetailedStage() => M032AutonomousDetailedRegion.Direct();
}

internal static class M041ContinuationExtensions
{
    public static string ContinuationFingerprint(this M041DetailedContinuation continuation) => Hash(JsonSerializer.Serialize(continuation, SimulationWorld.JsonOptions));
    public static string ContinuationFingerprint(this M041AbstractContinuation continuation) => Hash(JsonSerializer.Serialize(continuation, SimulationWorld.JsonOptions));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
