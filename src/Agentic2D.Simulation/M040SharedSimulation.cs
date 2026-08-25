using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agentic2D.Simulation;

public sealed record M040AbstractContinuation(
    string Schema,
    string Mode,
    string RegionId,
    string ActivityId,
    string WorkerId,
    string SourceId,
    string StorageId,
    int RegionRevision,
    int GraphRevision,
    string? NextTriggerId,
    long TargetMicroseconds,
    string Status);

public sealed record M040AbstractSave(
    string Schema,
    SimulationSave World,
    DiscreteEventSchedulerSave Queue,
    M040AbstractContinuation Continuation,
    IReadOnlyList<AbstractGraphEdge> Graph);

public sealed record M040AbstractRun(
    SimulationWorld World,
    DiscreteEventScheduler Scheduler,
    M040AbstractContinuation Continuation,
    IReadOnlyList<AbstractGraphEdge> Graph,
    IReadOnlyList<string> Transitions,
    IReadOnlyList<SimulationDiagnostic> Diagnostics,
    string Fingerprint);

public static class M040SharedSemantics
{
    public static WorkOpportunity SelectHarvest(SimulationWorld world)
        => M032AutonomousDetailedRegion.DeriveOpportunities(world, M032AutonomousDetailedRegion.InspectDesignations(world))
            .Where(x => x.Family == "harvest" && x.BlockingReason is null)
            .OrderByDescending(x => x.Priority).ThenBy(x => x.Key, StringComparer.Ordinal).First();

    public static WorkerDecision SelectWorker(SimulationWorld world, string workerId, IReadOnlyList<WorkOpportunity> opportunities, Func<WorkOpportunity, (bool Reachable, int Cost)> reachability)
    {
        var worker = world.Entities.SingleOrDefault(x => x.Id == workerId && x.Lifecycle == SimulationLifecycle.Active);
        if (worker is null) return new(workerId, null, "worker-unavailable", [], ["WORK-ELIGIBILITY0001"], 0, "not-attempted", "not-applicable", []);
        if (!world.TryGetComponent<M032WorkerComponent>(workerId, "component.m032.worker", out var workerState) || workerState is null) throw new InvalidOperationException("shared selection requires typed worker state");
        var evaluations = new List<WorkCandidateEvaluation>();
        foreach (var opportunity in opportunities.Where(x => x.RegionId == worker.RegionId).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var factors = new List<string> { "active-worker", "active-region", "capacity=" + workerState.Capacity, "priority=" + opportunity.Priority };
            var rejections = new List<string>();
            if (opportunity.BlockingReason is not null) rejections.Add(opportunity.Key + ":" + opportunity.BlockingReason);
            if (opportunity.Family is "eat" or "drink" or "rest" && !opportunity.Key.EndsWith(workerId, StringComparison.Ordinal)) rejections.Add(opportunity.Key + ":other-worker");
            if (world.Reservations.Any(x => x.SubjectId == opportunity.TargetId && x.Status == SimulationReservationStatus.Active)) rejections.Add(opportunity.Key + ":reservation-unavailable");
            var estimate = reachability(opportunity); if (!estimate.Reachable) rejections.Add(opportunity.Key + ":unreachable");
            factors.Add("reachability-cost=" + estimate.Cost);
            evaluations.Add(new(opportunity.Key, rejections.Count == 0, factors, rejections, estimate.Cost, rejections.Any(x => x.EndsWith(":reservation-unavailable", StringComparison.Ordinal)) ? "unavailable" : "available"));
        }
        var selected = evaluations.Where(x => x.Eligible).Join(opportunities, evaluation => evaluation.OpportunityKey, opportunity => opportunity.Key, (evaluation, opportunity) => (evaluation, opportunity)).OrderByDescending(x => x.opportunity.Priority).ThenBy(x => x.evaluation.PathCost).ThenBy(x => x.opportunity.Key, StringComparer.Ordinal).FirstOrDefault();
        var rejected = evaluations.SelectMany(x => x.RejectionCodes).Order(StringComparer.Ordinal).ToArray();
        return selected.opportunity is null
            ? new(workerId, null, "no-eligible-opportunity", evaluations.Select(x => x.OpportunityKey).ToArray(), rejected, 0, "not-attempted", "not-required", evaluations)
            : new(workerId, selected.opportunity.Key, "", evaluations.Select(x => x.OpportunityKey).ToArray(), rejected, selected.evaluation.PathCost, "available", selected.opportunity.Family is "eat" or "drink" or "rest" ? "mandatory-need" : "not-required", evaluations);
    }
}

/// <summary>
/// M040's executor-neutral semantic boundary and independent abstract continuation.
/// Gameplay values are the M032 typed components; this class owns only graph, duration,
/// trigger, and continuation mechanics.
/// </summary>
public static class M040AbstractExecutor
{
    public const string SaveSchema = "agentic2d.m040.abstract-executor-save.v1";
    public const string ContinuationSchema = "agentic2d.m040.abstract-continuation.v1";
    private const string Region = "region.forest.active";
    private const string Worker = "worker.001";
    private const string Source = "tree.001";
    private const string Storage = "storage.wood.001";
    private const string Activity = "activity.m040.abstract.harvest.001";
    private const int GraphRevision = 7;

    public static IReadOnlyList<AbstractGraphEdge> Graph() =>
    [
        new("edge.housing.source-a", "housing", "source-a", 3),
        new("edge.source-a.tree", "source-a", "tree", 4),
        new("edge.tree.storage", "tree", "storage", 5),
        new("edge.storage.housing", "storage", "housing", 2),
        new("edge.food.housing", "food", "housing", 1),
    ];

    public static M040AbstractRun Create()
    {
        var world = M032AutonomousDetailedRegion.CreateInitial();
        var scheduler = new DiscreteEventScheduler();
        var graph = Graph();
        var transitions = new List<string>();
        var opportunities = M032AutonomousDetailedRegion.DeriveOpportunities(world, M032AutonomousDetailedRegion.InspectDesignations(world));
        var opportunity = M040SharedSemantics.SelectHarvest(world);
        var decision = M040SharedSemantics.SelectWorker(world, Worker, opportunities, candidate => candidate.Family == "harvest" ? (true, PlanRoute(Worker, "housing", "tree", graph, false).Cost) : (true, 0));
        var selectedWorker = decision.WorkerId;
        Require(decision.SelectedOpportunityKey == opportunity.Key && selectedWorker == Worker && opportunity.TargetId == Source && opportunity.DestinationId == Storage, "M040 shared selection changed the bounded target");
        var activity = world.CreateActivityWithReservations(new(Activity), selectedWorker, "harvest-and-haul", "travel-to-tree", [opportunity.TargetId, opportunity.DestinationId!],
            [new(new("reservation.m040.source"), opportunity.TargetId, "exclusive.harvest", 1, 1)], new("correlation.m040.abstract"), new("cause.m040.create"));
        Require(activity.Status == "accepted", "M040 activity creation rejected");
        var current = world.Activities.Single(x => x.Id == Activity);
        var capacity = world.AcquireReservation(new("reservation.m040.storage"), new(Activity), Storage, "capacity.wood", 3, 18, current.Revision);
        Require(capacity.Status == "accepted", "M040 capacity reservation rejected");
        transitions.Add("assigned-and-reserved");
        var continuation = NewContinuation(world.Clock.Now.Microseconds, "abstract.travel.source");
        ScheduleTravel(world, scheduler, continuation, current.Revision, world.Clock.Now, "housing", "tree", false);
        ScheduleNeedInterrupt(scheduler, current.Revision, world.Clock.Now);
        transitions.Add("planned:abstract.travel.source");
        return new(world, scheduler, continuation, graph, transitions, [], Fingerprint(world, scheduler));
    }

    public static M040AbstractRun Advance(M040AbstractRun run, SimulationInstant target)
    {
        if (target.CompareTo(run.World.Clock.Now) < 0) throw new InvalidOperationException("M040 horizon cannot move backwards");
        var transitions = run.Transitions.ToList();
        var diagnostics = run.Diagnostics.ToList();
        var continuation = run.Continuation;
        var result = run.Scheduler.AdvanceTo(run.World, target, trigger => Deliver(run.World, run.Scheduler, run.Graph, trigger, transitions, ref continuation));
        diagnostics.AddRange(result.Diagnostics);
        if (result.SafetyStopped) diagnostics.Add(new("DES-LIMIT", "error", "abstract execution safety limit reached", []));
        continuation = continuation with { TargetMicroseconds = target.Microseconds, NextTriggerId = run.Scheduler.Inspect().FirstOrDefault(x => x.Status == ScheduledTriggerStatus.Scheduled)?.Id };
        return new(run.World, run.Scheduler, continuation, run.Graph, transitions, diagnostics, Fingerprint(run.World, run.Scheduler));
    }

    public static M040AbstractSave Capture(M040AbstractRun run) => new(SaveSchema, run.World.Capture(), run.Scheduler.Capture(), run.Continuation, run.Graph);

    public static M040AbstractRun Restore(M040AbstractSave save)
    {
        if (save.Schema != SaveSchema || save.Continuation.Schema != ContinuationSchema || save.Continuation.Mode != "abstract") throw new InvalidOperationException("M040-PERSISTENCE: unsupported abstract continuation");
        var loaded = SimulationWorld.Load(save.World, M032AutonomousDetailedRegion.Registrations());
        if (!loaded.Success || loaded.World is null) throw new InvalidOperationException("M040-PERSISTENCE: world restore failed: " + string.Join(',', loaded.Diagnostics.Select(x => x.Code)));
        M032AutonomousDetailedRegion.RegisterPolicies(loaded.World);
        var scheduler = DiscreteEventScheduler.Restore(save.Queue);
        return new(loaded.World, scheduler, save.Continuation, save.Graph, [], [], Fingerprint(loaded.World, scheduler));
    }

    public static AbstractRoute PlanRoute(string actor, string origin, string destination, IReadOnlyList<AbstractGraphEdge> graph, bool carrying, int graphRevision = GraphRevision)
    {
        var available = graph.Where(x => x.Accessible).ToArray();
        var distances = new Dictionary<string, int>(StringComparer.Ordinal) { [origin] = 0 };
        var previous = new Dictionary<string, (string Node, string Edge)>(StringComparer.Ordinal);
        var open = new PriorityQueue<string, (int Cost, string Node)>(); open.Enqueue(origin, (0, origin));
        while (open.TryDequeue(out var node, out var priority))
        {
            if (priority.Cost != distances[node]) continue;
            foreach (var edge in available.Where(x => x.From == node).OrderBy(x => x.Id, StringComparer.Ordinal))
            {
                var cost = priority.Cost + edge.Cost * (carrying ? 2 : 1);
                if (!distances.TryGetValue(edge.To, out var known) || cost < known)
                {
                    distances[edge.To] = cost; previous[edge.To] = (node, edge.Id); open.Enqueue(edge.To, (cost, edge.To));
                }
            }
        }
        if (!distances.TryGetValue(destination, out var total)) throw new InvalidOperationException("ABS-GRAPH: disconnected abstract locations");
        var edges = new List<string>(); for (var cursor = destination; cursor != origin; cursor = previous[cursor].Node) edges.Add(previous[cursor].Edge); edges.Reverse();
        return new(actor, origin, destination, edges, total, graphRevision, Hash(actor + ":" + origin + ":" + destination + ":" + string.Join(',', edges) + ":" + total + ":" + graphRevision));
    }

    public static long DurationMicroseconds(string kind, int cost = 0, bool carrying = false) => kind switch
    {
        "travel" => SimulationDuration.FromSeconds(Math.Max(1, cost)).Microseconds,
        "harvest" => SimulationDuration.FromSeconds(2).Microseconds,
        "deposit" => SimulationDuration.FromSeconds(1).Microseconds,
        "eat" => SimulationDuration.FromSeconds(1).Microseconds,
        "retry" => SimulationDuration.FromSeconds(1).Microseconds,
        _ => SimulationDuration.FromSeconds(carrying ? 2 : 1).Microseconds
    };

    private static TriggerDelivery Deliver(SimulationWorld world, DiscreteEventScheduler scheduler, IReadOnlyList<AbstractGraphEdge> graph, ScheduledTrigger trigger, List<string> transitions, ref M040AbstractContinuation continuation)
    {
        if (trigger.OwnerRegionId != Region || trigger.ExpectedRegionRevision != continuation.RegionRevision) return new(ScheduledTriggerStatus.Stale, null, "stale-region-revision");
        if (trigger.ExpectedGraphRevision is not null && trigger.ExpectedGraphRevision != continuation.GraphRevision) return new(ScheduledTriggerStatus.Stale, null, "stale-graph-revision");
        var guardedWorker = Component<M032WorkerComponent>(world, Worker);
        if (trigger.ExpectedNeedRevision is not null && trigger.ExpectedNeedRevision != guardedWorker.NeedRevision) return new(ScheduledTriggerStatus.Stale, null, "stale-need-revision");
        if (trigger.ExpectedSubjectRevision is not null && trigger.ExpectedSubjectRevision != Component<M032HarvestableComponent>(world, Source).Revision) return new(ScheduledTriggerStatus.Stale, null, "stale-subject-revision");
        if (trigger.ExpectedStorageRevision is not null && trigger.ExpectedStorageRevision != Component<M032StorageComponent>(world, Storage).Revision) return new(ScheduledTriggerStatus.Stale, null, "stale-storage-revision");
        if (trigger.ExpectedReservationRevision is not null)
        {
            var reservationId = trigger.Kind.Contains("storage", StringComparison.Ordinal) || trigger.Kind.Contains("deposit", StringComparison.Ordinal) ? "reservation.m040.storage" : "reservation.m040.source";
            var reservation = world.Reservations.SingleOrDefault(x => x.Id == reservationId && x.Status == SimulationReservationStatus.Active);
            if (reservation is null || reservation.Revision != trigger.ExpectedReservationRevision) return new(ScheduledTriggerStatus.Stale, null, "stale-reservation-revision");
        }
        if (trigger.OwnerActivityId == Activity && (!world.Activities.Any(x => x.Id == Activity && x.Revision == trigger.ExpectedActivityRevision) || trigger.ExpectedActivityRevision is null)) return new(ScheduledTriggerStatus.Stale, null, "stale-activity-revision");
        if (trigger.Kind == "abstract.need-mandatory")
        {
            var worker = Component<M032WorkerComponent>(world, Worker);
            if (worker.Food < 2) world.SetComponent(Worker, "component.m032.worker", worker with { Food = 2, NeedRevision = worker.NeedRevision + 1 });
            var current = world.Activities.Single(x => x.Id == Activity);
            var interrupted = world.TransitionActivity(new(Activity), current.Revision, "interrupted-for-food", SimulationActivityStatus.Interrupted, null, "mandatory-food");
            if (interrupted.Status != "accepted") return new(ScheduledTriggerStatus.Stale, interrupted, "need-interruption-rejected");
            transitions.Add("mandatory-need-interrupt");
            var next = world.Activities.Single(x => x.Id == Activity);
            var updatedWorker = Component<M032WorkerComponent>(world, Worker);
            scheduler.Schedule(new("trigger.m040.need-satisfied", world.Clock.Now + new SimulationDuration(DurationMicroseconds("eat")), 5, Region, Activity, Worker, "abstract.need-satisfaction", next.Revision, continuation.RegionRevision, trigger.CorrelationId, trigger.Id, JsonSerializer.SerializeToElement(new { kind = "food" }), ExpectedGraphRevision: continuation.GraphRevision, ExpectedNeedRevision: updatedWorker.NeedRevision));
            return new(ScheduledTriggerStatus.Completed, interrupted, "need-interrupted");
        }
        if (trigger.Kind == "abstract.need-satisfaction")
        {
            var worker = Component<M032WorkerComponent>(world, Worker);
            var satisfied = world.ApplyAtomicTypedComponentFact("NeedSatisfied", [new(Worker, "component.m032.worker", worker with { Food = 0, NeedRevision = worker.NeedRevision + 1 })], [Worker], new { kind = "food", threshold = 2 });
            if (satisfied.Status != "accepted") return new(ScheduledTriggerStatus.Failed, satisfied, "need-satisfaction-rejected");
            var current = world.Activities.Single(x => x.Id == Activity);
            var resumed = world.TransitionActivity(new(Activity), current.Revision, "travel-to-tree", SimulationActivityStatus.Active, null, "need-satisfied");
            if (resumed.Status != "accepted") return new(ScheduledTriggerStatus.Failed, resumed, "work-resumption-rejected");
            transitions.Add("need-satisfied-and-re-evaluated");
            var next = world.Activities.Single(x => x.Id == Activity);
            ScheduleTravel(world, scheduler, continuation, next.Revision, world.Clock.Now, "housing", "tree", false);
            return new(ScheduledTriggerStatus.Completed, satisfied, "need-satisfied");
        }
        var activity = world.Activities.SingleOrDefault(x => x.Id == Activity);
        if (activity is null || activity.Status is SimulationActivityStatus.Completed or SimulationActivityStatus.Cancelled || activity.Revision != trigger.ExpectedActivityRevision) return new(ScheduledTriggerStatus.Stale, null, "stale-or-terminal-activity");
        switch (trigger.Kind)
        {
            case "abstract.travel.source":
                return StageTravel(world, scheduler, continuation, trigger, transitions, "at-tree", "abstract.harvest-complete", "harvest", 0, false);
            case "abstract.harvest-complete":
                var tree = Component<M032HarvestableComponent>(world, Source); var worker = Component<M032WorkerComponent>(world, Worker);
                var harvested = world.ApplyAtomicTypedComponentFact("ResourceHarvested", [new(Source, "component.m032.harvestable", tree with { Wood = Math.Max(0, tree.Wood - 3), Harvestable = tree.Wood > 3, Revision = tree.Revision + 1 }), new(Worker, "component.m032.worker", worker with { Wood = worker.Wood + 3 })], [Activity, Source, Worker], new { quantity = 3, executor = "abstract" });
                if (harvested.Status != "accepted") return new(ScheduledTriggerStatus.Failed, harvested, "harvest-rejected");
                world.ReleaseReservation(new("reservation.m040.source"), "harvest-complete");
                var afterHarvest = world.Activities.Single(x => x.Id == Activity); var carrying = world.TransitionActivity(new(Activity), afterHarvest.Revision, "carrying", SimulationActivityStatus.Active);
                if (carrying.Status != "accepted") return new(ScheduledTriggerStatus.Failed, carrying, "carry-transition-rejected");
                transitions.Add("harvest-complete:source-to-inventory");
                ScheduleTravel(world, scheduler, continuation, world.Activities.Single(x => x.Id == Activity).Revision, world.Clock.Now, "tree", "storage", true);
                return new(ScheduledTriggerStatus.Completed, harvested, "harvest-complete");
            case "abstract.travel.storage":
                return StageTravel(world, scheduler, continuation, trigger, transitions, "at-storage", "abstract.deposit-complete", "deposit", 0, true);
            case "abstract.deposit-complete":
                var carried = Component<M032WorkerComponent>(world, Worker); var storage = Component<M032StorageComponent>(world, Storage);
                var deposited = world.ApplyAtomicTypedComponentFact("ResourceDeposited", [new(Worker, "component.m032.worker", carried with { Wood = 0 }), new(Storage, "component.m032.storage", storage with { Wood = storage.Wood + carried.Wood, Revision = storage.Revision + 1 })], [Activity, Worker, Storage], new { quantity = carried.Wood, executor = "abstract" });
                if (deposited.Status != "accepted") return new(ScheduledTriggerStatus.Failed, deposited, "deposit-rejected");
                world.ReleaseReservation(new("reservation.m040.storage"), "deposit-complete");
                var completed = world.Activities.Single(x => x.Id == Activity); var done = world.TransitionActivity(new(Activity), completed.Revision, "completed", SimulationActivityStatus.Completed, carried.Wood, null);
                transitions.Add("deposit-complete:inventory-to-storage");
                return new(done.Status == "accepted" ? ScheduledTriggerStatus.Completed : ScheduledTriggerStatus.Failed, done, done.Status);
            default:
                return new(ScheduledTriggerStatus.Failed, null, "unknown-trigger-kind");
        }
    }

    private static TriggerDelivery StageTravel(SimulationWorld world, DiscreteEventScheduler scheduler, M040AbstractContinuation continuation, ScheduledTrigger trigger, List<string> transitions, string stage, string nextKind, string durationKind, int unused, bool carrying)
    {
        var current = world.Activities.Single(x => x.Id == Activity); var transitioned = world.TransitionActivity(new(Activity), current.Revision, stage, SimulationActivityStatus.Active);
        if (transitioned.Status != "accepted") return new(ScheduledTriggerStatus.Failed, transitioned, "travel-arrival-rejected");
        transitions.Add("arrival:" + trigger.Kind);
        var next = world.Activities.Single(x => x.Id == Activity);
        var due = world.Clock.Now + new SimulationDuration(DurationMicroseconds(durationKind, carrying ? 2 : 2, carrying));
        scheduler.Schedule(new("trigger.m040." + nextKind, due, 20, Region, Activity, Worker, nextKind, next.Revision, continuation.RegionRevision, trigger.CorrelationId, trigger.Id, JsonSerializer.SerializeToElement(new { stage }), ExpectedGraphRevision: continuation.GraphRevision, ExpectedReservationRevision: ReservationRevision(world, nextKind), ExpectedSubjectRevision: nextKind == "abstract.harvest-complete" ? Component<M032HarvestableComponent>(world, Source).Revision : null, ExpectedStorageRevision: nextKind == "abstract.deposit-complete" ? Component<M032StorageComponent>(world, Storage).Revision : null));
        return new(ScheduledTriggerStatus.Completed, transitioned, "arrival-planned");
    }

    private static void ScheduleTravel(SimulationWorld world, DiscreteEventScheduler scheduler, M040AbstractContinuation continuation, int revision, SimulationInstant now, string origin, string destination, bool carrying)
    {
        var route = PlanRoute(Worker, origin, destination, Graph(), carrying);
        var kind = destination == "tree" ? "abstract.travel.source" : "abstract.travel.storage";
        scheduler.Schedule(new("trigger.m040." + kind + "." + revision.ToString(System.Globalization.CultureInfo.InvariantCulture) + "." + scheduler.PendingCount, now + new SimulationDuration(DurationMicroseconds("travel", route.Cost, carrying)), 10, Region, Activity, Worker, kind, revision, continuation.RegionRevision, "correlation.m040.abstract", "cause.m040.plan", JsonSerializer.SerializeToElement(new { route = route.Fingerprint, route.Cost, carrying }), ExpectedGraphRevision: continuation.GraphRevision, ExpectedNeedRevision: Component<M032WorkerComponent>(world, Worker).NeedRevision, ExpectedReservationRevision: ReservationRevision(world, kind), ExpectedSubjectRevision: kind == "abstract.travel.source" ? Component<M032HarvestableComponent>(world, Source).Revision : null, ExpectedStorageRevision: kind == "abstract.travel.storage" ? Component<M032StorageComponent>(world, Storage).Revision : null));
    }

    private static void ScheduleNeedInterrupt(DiscreteEventScheduler scheduler, int revision, SimulationInstant now)
        => scheduler.Schedule(new("trigger.m040.need-mandatory", now + new SimulationDuration(DurationMicroseconds("retry")), 1, Region, Activity, Worker, "abstract.need-mandatory", revision, 1, "correlation.m040.abstract", "cause.m040.need", JsonSerializer.SerializeToElement(new { kind = "food", expectedLevel = 0, expectedRevision = revision }), ExpectedGraphRevision: GraphRevision, ExpectedNeedRevision: 0));

    private static int? ReservationRevision(SimulationWorld world, string triggerKind)
    {
        var id = triggerKind.Contains("storage", StringComparison.Ordinal) || triggerKind.Contains("deposit", StringComparison.Ordinal) ? "reservation.m040.storage" : "reservation.m040.source";
        return world.Reservations.SingleOrDefault(x => x.Id == id && x.Status == SimulationReservationStatus.Active)?.Revision;
    }

    private static M040AbstractContinuation NewContinuation(long now, string next) => new(ContinuationSchema, "abstract", Region, Activity, Worker, Source, Storage, 1, GraphRevision, next, now, "running");
    private static T Component<T>(SimulationWorld world, string id) where T : notnull => world.TryGetComponent<T>(id, typeof(T) == typeof(M032HarvestableComponent) ? "component.m032.harvestable" : typeof(T) == typeof(M032StorageComponent) ? "component.m032.storage" : "component.m032.worker", out var value) && value is not null ? value : throw new InvalidOperationException("M040 missing typed component");
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static string Hash(string value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string Fingerprint(SimulationWorld world, DiscreteEventScheduler scheduler) => Hash(world.Fingerprint() + JsonSerializer.Serialize(scheduler.Capture(), SimulationWorld.JsonOptions));
}
