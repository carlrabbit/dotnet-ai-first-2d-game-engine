using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agentic2D.Simulation;

/// <summary>Persistent state for the optional M033 future-input queue.  A trigger is not a domain event.</summary>
public enum ScheduledTriggerStatus { Scheduled, Delivered, Completed, Stale, Cancelled, Failed }
public enum RegionFidelity { Detailed, Abstract }
public enum RegionTransitionStatus { Stable, Preparing, Reconciling, Validating, Committing, Failed }

public sealed record ScheduledTrigger(
    string Id, SimulationInstant Due, int PriorityClass, long Sequence, string OwnerRegionId,
    string? OwnerActivityId, string? OwnerEntityId, string Kind, int? ExpectedActivityRevision,
    int ExpectedRegionRevision, string CorrelationId, string CausationId, JsonElement Payload,
    ScheduledTriggerStatus Status = ScheduledTriggerStatus.Scheduled, string? Outcome = null);
public sealed record ScheduledTriggerRequest(string Id, SimulationInstant Due, int PriorityClass, string OwnerRegionId,
    string? OwnerActivityId, string? OwnerEntityId, string Kind, int? ExpectedActivityRevision,
    int ExpectedRegionRevision, string CorrelationId, string CausationId, JsonElement Payload);
public sealed record TriggerDelivery(ScheduledTriggerStatus Status, SimulationCommandResult? Command, string Outcome);
public sealed record DiscreteEventLimits(int MaximumEvents = 100_000, int MaximumSameInstantEvents = 10_000)
{
    public static readonly DiscreteEventLimits Default = new();
}
public sealed record DiscreteEventAdvanceResult(int Delivered, bool SafetyStopped, IReadOnlyList<SimulationDiagnostic> Diagnostics);
public sealed record DiscreteEventSchedulerSave(string Schema, long Sequence, IReadOnlyList<ScheduledTrigger> Triggers);

/// <summary>
/// Deterministic, single-threaded future-input scheduler.  It intentionally delegates mutation
/// to the supplied command handler; it never changes gameplay state itself.
/// </summary>
public sealed class DiscreteEventScheduler
{
    public const string SaveSchema = "agentic2d.discrete-event-scheduler-save.v1";
    private readonly SortedDictionary<string, ScheduledTrigger> triggers = new(StringComparer.Ordinal);
    private long sequence;

    public IReadOnlyList<ScheduledTrigger> Inspect() => triggers.Values.OrderBy(Key).ToArray();
    public int PendingCount => triggers.Values.Count(trigger => trigger.Status == ScheduledTriggerStatus.Scheduled);

    public ScheduledTrigger Schedule(ScheduledTriggerRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Id) || string.IsNullOrWhiteSpace(request.OwnerRegionId) || string.IsNullOrWhiteSpace(request.Kind))
            throw new ArgumentException("DES-QUEUE: trigger requires stable identity, owner region, and kind.", nameof(request));
        if (triggers.ContainsKey(request.Id)) throw new InvalidOperationException("DES-QUEUE: duplicate scheduled trigger " + request.Id);
        var trigger = new ScheduledTrigger(request.Id, request.Due, request.PriorityClass, ++sequence, request.OwnerRegionId,
            request.OwnerActivityId, request.OwnerEntityId, request.Kind, request.ExpectedActivityRevision,
            request.ExpectedRegionRevision, request.CorrelationId, request.CausationId, request.Payload.Clone());
        triggers.Add(trigger.Id, trigger);
        return trigger;
    }

    public bool Cancel(string id, string reason)
    {
        if (!triggers.TryGetValue(id, out var trigger) || trigger.Status != ScheduledTriggerStatus.Scheduled) return false;
        triggers[id] = trigger with { Status = ScheduledTriggerStatus.Cancelled, Outcome = reason };
        return true;
    }

    public DiscreteEventAdvanceResult RunNext(SimulationWorld world, Func<ScheduledTrigger, TriggerDelivery> handler, DiscreteEventLimits? limits = null)
    {
        var next = Inspect().FirstOrDefault(trigger => trigger.Status == ScheduledTriggerStatus.Scheduled);
        return next is null ? new(0, false, []) : AdvanceTo(world, next.Due, handler, limits);
    }

    public DiscreteEventAdvanceResult AdvanceBy(SimulationWorld world, SimulationDuration duration, Func<ScheduledTrigger, TriggerDelivery> handler, DiscreteEventLimits? limits = null)
        => AdvanceTo(world, world.Clock.Now + duration, handler, limits);

    public DiscreteEventAdvanceResult AdvanceTo(SimulationWorld world, SimulationInstant target, Func<ScheduledTrigger, TriggerDelivery> handler, DiscreteEventLimits? limits = null)
    {
        if (target.CompareTo(world.Clock.Now) < 0) throw new InvalidOperationException("DES-ORDER: scheduler cannot move the simulation clock backwards.");
        limits ??= DiscreteEventLimits.Default;
        var diagnostics = new List<SimulationDiagnostic>();
        var delivered = 0;
        var sameInstant = 0;
        while (true)
        {
            var next = Inspect().FirstOrDefault(trigger => trigger.Status == ScheduledTriggerStatus.Scheduled && trigger.Due.CompareTo(target) <= 0);
            if (next is null) break;
            if (delivered >= limits.MaximumEvents || sameInstant >= limits.MaximumSameInstantEvents)
            {
                diagnostics.Add(new("DES-LIMIT", "error", "discrete-event safety limit reached", [next.Id]));
                return new(delivered, true, diagnostics);
            }
            if (next.Due.CompareTo(world.Clock.Now) > 0)
            {
                world.Advance(new SimulationDuration(next.Due.Microseconds - world.Clock.Now.Microseconds));
                sameInstant = 0;
            }
            sameInstant++;
            triggers[next.Id] = next with { Status = ScheduledTriggerStatus.Delivered };
            TriggerDelivery delivery;
            try { delivery = handler(next); }
            catch (Exception exception)
            {
                delivery = new(ScheduledTriggerStatus.Failed, null, "handler-failed:" + exception.GetType().Name);
                diagnostics.Add(new("DES-TRIGGER", "error", exception.Message, [next.Id]));
            }
            triggers[next.Id] = next with { Status = delivery.Status, Outcome = delivery.Outcome };
            delivered++;
        }
        if (target.CompareTo(world.Clock.Now) > 0) world.Advance(new SimulationDuration(target.Microseconds - world.Clock.Now.Microseconds));
        return new(delivered, false, diagnostics);
    }

    public DiscreteEventSchedulerSave Capture() => new(SaveSchema, sequence, Inspect());

    public static DiscreteEventScheduler Restore(DiscreteEventSchedulerSave save)
    {
        if (save.Schema != SaveSchema || save.Sequence < 0) throw new InvalidOperationException("DES-PERSISTENCE: unsupported scheduler save.");
        var scheduler = new DiscreteEventScheduler { sequence = save.Sequence };
        foreach (var trigger in save.Triggers.OrderBy(Key))
        {
            if (scheduler.triggers.ContainsKey(trigger.Id) || trigger.Sequence < 1) throw new InvalidOperationException("DES-PERSISTENCE: duplicate or invalid trigger.");
            scheduler.triggers.Add(trigger.Id, trigger with { Payload = trigger.Payload.Clone() });
        }
        return scheduler;
    }

    private static (long Due, int Priority, long Sequence, string Id) Key(ScheduledTrigger trigger) => (trigger.Due.Microseconds, trigger.PriorityClass, trigger.Sequence, trigger.Id);
}

public sealed record AbstractLocation(string EntityId, string NodeId, string? OriginNodeId, string? DestinationNodeId, long DepartureMicroseconds, long ArrivalMicroseconds);
public sealed record AbstractGraphEdge(string Id, string From, string To, int Cost, bool Accessible = true, int Revision = 1);
public sealed record AbstractRoute(string ActorId, string Origin, string Destination, IReadOnlyList<string> EdgeIds, int Cost, int GraphRevision, string Fingerprint);
public sealed record RegionFidelityState(string RegionId, RegionFidelity Fidelity, string ExecutorOwner, int Revision, RegionTransitionStatus TransitionStatus, long LastTransitionMicroseconds, string? Diagnostic = null);
public sealed record FidelityTransition(string Id, string Direction, string RegionId, RegionFidelity Previous, RegionFidelity Current, int Revision, SimulationInstant Instant, string Status, string Mapping, string? Diagnostic = null);
public sealed record MultiFidelitySave(string Schema, SimulationSave World, DiscreteEventSchedulerSave Queue, IReadOnlyList<RegionFidelityState> Regions, IReadOnlyList<AbstractLocation> Locations, IReadOnlyList<FidelityTransition> Transitions);

/// <summary>Serialized owner of the optional abstract/detailed bridge. Exactly one region may be detailed.</summary>
public sealed class RegionFidelityCoordinator
{
    public const string SaveSchema = "agentic2d.multi-fidelity-save.v1";
    private readonly SimulationWorld world;
    private readonly DiscreteEventScheduler scheduler;
    private readonly SortedDictionary<string, RegionFidelityState> states;
    private readonly SortedDictionary<string, AbstractLocation> locations = new(StringComparer.Ordinal);
    private readonly List<FidelityTransition> transitions = [];

    public RegionFidelityCoordinator(SimulationWorld world, DiscreteEventScheduler scheduler, IEnumerable<RegionFidelityState> states)
    {
        this.world = world;
        this.scheduler = scheduler;
        this.states = new SortedDictionary<string, RegionFidelityState>(StringComparer.Ordinal);
        foreach (var state in states)
        {
            if (!this.states.TryAdd(state.RegionId, state)) throw new InvalidOperationException("FIDELITY-STATE: duplicate region state " + state.RegionId);
        }
        ValidateOwnership();
    }

    public IReadOnlyList<RegionFidelityState> Regions => states.Values.ToArray();
    public IReadOnlyList<AbstractLocation> Locations => locations.Values.ToArray();
    public IReadOnlyList<FidelityTransition> Transitions => transitions.ToArray();
    public void SetLocation(AbstractLocation location) => locations[location.EntityId] = location;

    public bool IsAbstractOwner(ScheduledTrigger trigger) => states.TryGetValue(trigger.OwnerRegionId, out var state)
        && state.Fidelity == RegionFidelity.Abstract && state.Revision == trigger.ExpectedRegionRevision;

    public FidelityTransition SwitchDetailed(string destinationRegionId, bool forceInvalidMaterialization = false)
    {
        if (!states.TryGetValue(destinationRegionId, out var destination) || destination.TransitionStatus != RegionTransitionStatus.Stable)
            return Failed(destinationRegionId, "FIDELITY-STATE", "unknown or transitioning destination");
        var current = states.Values.SingleOrDefault(state => state.Fidelity == RegionFidelity.Detailed);
        if (current is null || current.RegionId == destinationRegionId) return Failed(destinationRegionId, "FIDELITY-STATE", "destination is already detailed or detailed owner is missing");
        var snapshot = states.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        try
        {
            states[current.RegionId] = current with { TransitionStatus = RegionTransitionStatus.Preparing };
            states[destinationRegionId] = destination with { TransitionStatus = RegionTransitionStatus.Reconciling };
            if (forceInvalidMaterialization) throw new InvalidOperationException("RECONCILE-POSITION: no valid materialization cell");
            states[current.RegionId] = current with { Fidelity = RegionFidelity.Abstract, ExecutorOwner = "abstract", Revision = current.Revision + 1, TransitionStatus = RegionTransitionStatus.Stable, LastTransitionMicroseconds = world.Clock.Now.Microseconds };
            states[destinationRegionId] = destination with { Fidelity = RegionFidelity.Detailed, ExecutorOwner = "detailed", Revision = destination.Revision + 1, TransitionStatus = RegionTransitionStatus.Stable, LastTransitionMicroseconds = world.Clock.Now.Microseconds };
            foreach (var trigger in scheduler.Inspect().Where(trigger => trigger.OwnerRegionId == destinationRegionId && trigger.Status == ScheduledTriggerStatus.Scheduled)) scheduler.Cancel(trigger.Id, "fidelity-materialized");
            ValidateOwnership();
            var transition = new FidelityTransition("transition." + transitions.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), "abstract-to-detailed", destinationRegionId, RegionFidelity.Abstract, RegionFidelity.Detailed, states[destinationRegionId].Revision, world.Clock.Now, "committed", "deterministic-reachable-cell", null);
            transitions.Add(transition);
            transitions.Add(new("transition." + transitions.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), "detailed-to-abstract", current.RegionId, RegionFidelity.Detailed, RegionFidelity.Abstract, states[current.RegionId].Revision, world.Clock.Now, "committed", "nearest-area-node", null));
            return transition;
        }
        catch (Exception exception)
        {
            foreach (var item in snapshot) states[item.Key] = item.Value;
            return Failed(destinationRegionId, "RECONCILE-ROLLBACK", exception.Message);
        }
    }

    public void ValidateOwnership()
    {
        if (states.Count == 0 || states.Values.Count(state => state.Fidelity == RegionFidelity.Detailed) != 1 || states.Values.Any(state => state.TransitionStatus != RegionTransitionStatus.Stable || state.ExecutorOwner != (state.Fidelity == RegionFidelity.Detailed ? "detailed" : "abstract")))
            throw new InvalidOperationException("FIDELITY-OWNER: exactly one stable executor owner is required.");
    }

    public MultiFidelitySave Capture() => new(SaveSchema, world.Capture(), scheduler.Capture(), Regions, Locations, Transitions);

    private FidelityTransition Failed(string region, string code, string message)
    {
        var state = states.TryGetValue(region, out var known) ? known : new(region, RegionFidelity.Abstract, "abstract", 0, RegionTransitionStatus.Stable, world.Clock.Now.Microseconds);
        var transition = new FidelityTransition("transition." + transitions.Count.ToString(System.Globalization.CultureInfo.InvariantCulture), "rollback", region, state.Fidelity, state.Fidelity, state.Revision, world.Clock.Now, "failed", "none", code + ": " + message);
        transitions.Add(transition);
        return transition;
    }
}

public sealed record M033Run(SimulationWorld World, DiscreteEventScheduler Scheduler, RegionFidelityCoordinator Coordinator, IReadOnlyList<SimulationDiagnostic> Diagnostics, string Fingerprint, int Days);

/// <summary>Bounded three-region dogfood scenario and standalone host substrate.</summary>
public static class M033MultiFidelitySimulation
{
    public const string ScenarioId = "scenario.m033.multi-region-equivalence-and-switching";
    private static readonly string[] RegionIds = ["region.alpha", "region.beta", "region.gamma"];
    private static readonly string[] Families = ["travel", "harvest", "pick-up", "carry", "deposit", "eat", "drink", "rest"];

    public static IReadOnlyList<SimulationComponentRegistration> Registrations() =>
    [
        new("component.m033.worker", 1, PersistenceClassification.AuthoritativePersistent, "m033.abstract-activity"),
        new("component.m033.resource", 1, PersistenceClassification.AuthoritativePersistent, "m033.logistics"),
        new("component.m033.fidelity", 1, PersistenceClassification.AuthoritativePersistent, "m033.fidelity"),
    ];

    public static M033Run RunThirtyDays(bool switchRegions = true)
    {
        var world = new SimulationWorld(new("world.m033"));
        foreach (var registration in Registrations()) world.RegisterComponent(registration);
        foreach (var region in RegionIds) Require(world.CreateRegion(new(region), region).Status == "accepted");
        foreach (var region in RegionIds)
        {
            for (var worker = 1; worker <= 2; worker++) Create(world, "worker." + region.Split('.')[1] + "." + worker.ToString("D3", System.Globalization.CultureInfo.InvariantCulture), region, "component.m033.worker", new { wood = 0, food = 0, water = 0, comfort = 0, node = "housing" });
            Create(world, "resource." + region.Split('.')[1], region, "component.m033.resource", new { sourceWood = 60, storedWood = 0, capacity = 60 });
        }
        var scheduler = new DiscreteEventScheduler();
        var coordinator = new RegionFidelityCoordinator(world, scheduler, RegionIds.Select((region, index) => new RegionFidelityState(region, index == 0 ? RegionFidelity.Detailed : RegionFidelity.Abstract, index == 0 ? "detailed" : "abstract", 1, RegionTransitionStatus.Stable, 0)));
        foreach (var region in RegionIds)
            foreach (var worker in world.QueryRegion(new(region)).Where(entity => entity.Id.StartsWith("worker.", StringComparison.Ordinal))) coordinator.SetLocation(new(worker.Id, "housing", null, null, 0, 0));

        for (var day = 1; day <= 30; day++)
            foreach (var region in RegionIds.Where(region => coordinator.Regions.Single(state => state.RegionId == region).Fidelity == RegionFidelity.Abstract))
                ScheduleCycle(scheduler, coordinator, region, day);

        var diagnostics = new List<SimulationDiagnostic>();
        for (var day = 1; day <= 30; day++)
        {
            var target = new SimulationInstant(day * SimulationDuration.FromSeconds(86_400).Microseconds);
            var advanced = scheduler.AdvanceTo(world, target, trigger => Deliver(world, scheduler, coordinator, trigger));
            diagnostics.AddRange(advanced.Diagnostics);
            var detailed = coordinator.Regions.Single(state => state.Fidelity == RegionFidelity.Detailed);
            CompleteCycle(world, detailed.RegionId, day, "detailed");
            if (switchRegions && day is 2 or 5 or 9 or 14 or 20 or 27)
            {
                var next = RegionIds[(Array.IndexOf(RegionIds, detailed.RegionId) + 1) % RegionIds.Length];
                coordinator.SwitchDetailed(next);
                ScheduleCycle(scheduler, coordinator, detailed.RegionId, day + 1);
            }
        }
        diagnostics.AddRange(Validate(world, scheduler, coordinator));
        return new(world, scheduler, coordinator, diagnostics, Fingerprint(world.Fingerprint() + JsonSerializer.Serialize(coordinator.Regions, SimulationWorld.JsonOptions) + JsonSerializer.Serialize(scheduler.Capture(), SimulationWorld.JsonOptions)), 30);
    }

    public static M033Run ContinueFromSave(MultiFidelitySave save)
    {
        if (save.Schema != RegionFidelityCoordinator.SaveSchema) throw new InvalidOperationException("DES-PERSISTENCE: unsupported mixed-fidelity save.");
        var loaded = SimulationWorld.Load(save.World, Registrations());
        if (!loaded.Success || loaded.World is null) throw new InvalidOperationException("DES-PERSISTENCE: world restore failed.");
        var scheduler = DiscreteEventScheduler.Restore(save.Queue);
        var coordinator = new RegionFidelityCoordinator(loaded.World, scheduler, save.Regions);
        foreach (var location in save.Locations) coordinator.SetLocation(location);
        var diagnostics = Validate(loaded.World, scheduler, coordinator);
        return new(loaded.World, scheduler, coordinator, diagnostics, Fingerprint(loaded.World.Fingerprint() + JsonSerializer.Serialize(scheduler.Capture(), SimulationWorld.JsonOptions)), 0);
    }

    public static AbstractRoute PlanAbstractTravel(string actor, string origin, string destination, IReadOnlyList<AbstractGraphEdge> edges, int graphRevision, bool carrying)
    {
        var usable = edges.Where(edge => edge.Accessible).OrderBy(edge => edge.Id, StringComparer.Ordinal).ToArray();
        var edge = usable.FirstOrDefault(edge => edge.From == origin && edge.To == destination) ?? usable.FirstOrDefault(edge => edge.From == destination && edge.To == origin);
        if (edge is null) throw new InvalidOperationException("ABS-GRAPH: disconnected abstract locations.");
        var cost = checked(edge.Cost * (carrying ? 2 : 1));
        return new(actor, origin, destination, [edge.Id], cost, graphRevision, Fingerprint(actor + ":" + edge.Id + ":" + cost + ":" + graphRevision));
    }

    private static void ScheduleCycle(DiscreteEventScheduler scheduler, RegionFidelityCoordinator coordinator, string region, int day)
    {
        var state = coordinator.Regions.Single(item => item.RegionId == region);
        var id = "trigger." + region + "." + day.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + ".r" + state.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture);
        if (scheduler.Inspect().Any(trigger => trigger.Id == id && trigger.Status == ScheduledTriggerStatus.Scheduled)) return;
        scheduler.Schedule(new(id, new SimulationInstant(day * SimulationDuration.FromSeconds(86_400).Microseconds), 10, region, null, null, "abstract-cycle", null, state.Revision, "correlation." + region + "." + day, "cause.schedule", JsonSerializer.SerializeToElement(new { day })));
    }

    private static TriggerDelivery Deliver(SimulationWorld world, DiscreteEventScheduler scheduler, RegionFidelityCoordinator coordinator, ScheduledTrigger trigger)
    {
        if (!coordinator.IsAbstractOwner(trigger)) return new(ScheduledTriggerStatus.Stale, null, "stale-fidelity-or-revision");
        if (trigger.Kind != "abstract-cycle") return new(ScheduledTriggerStatus.Failed, null, "unknown-trigger-kind");
        var day = trigger.Payload.GetProperty("day").GetInt32();
        var result = CompleteCycle(world, trigger.OwnerRegionId, day, "abstract");
        return new(result.Status == "accepted" ? ScheduledTriggerStatus.Completed : ScheduledTriggerStatus.Stale, result, result.Status);
    }

    private static SimulationCommandResult CompleteCycle(SimulationWorld world, string region, int day, string executor)
    {
        var actor = world.QueryRegion(new(region)).First(entity => entity.Id.StartsWith("worker.", StringComparison.Ordinal)).Id;
        var resource = "resource." + region.Split('.')[1];
        var family = Families[(day - 1) % Families.Length];
        var id = "activity." + region + "." + day.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
        var create = world.CreateActivity(new(id), actor, family, "planned", [resource], new("correlation." + id), new("cause." + executor));
        if (create.Status != "accepted") return create;
        var activity = world.Activities.Single(item => item.Id == id);
        var active = world.TransitionActivity(new(id), activity.Revision, "executing", SimulationActivityStatus.Active, day);
        if (active.Status != "accepted") return active;
        activity = world.Activities.Single(item => item.Id == id);
        var complete = world.TransitionActivity(new(id), activity.Revision, "completed", SimulationActivityStatus.Completed, day);
        if (complete.Status == "accepted" && family is "harvest" or "deposit" or "carry")
        {
            var value = world.Entities.Single(entity => entity.Id == resource).Components["component.m033.resource"];
            var source = value.GetProperty("sourceWood").GetInt32();
            var stored = value.GetProperty("storedWood").GetInt32();
            if (source > 0) world.SetComponent(resource, "component.m033.resource", JsonSerializer.SerializeToElement(new { sourceWood = source - 1, storedWood = stored + 1, capacity = value.GetProperty("capacity").GetInt32() }));
            world.RecordFact("AbstractActivityCompleted", [id, actor, resource], new { executor, family, day });
        }
        return complete;
    }

    private static IReadOnlyList<SimulationDiagnostic> Validate(SimulationWorld world, DiscreteEventScheduler scheduler, RegionFidelityCoordinator coordinator)
    {
        var diagnostics = new List<SimulationDiagnostic>();
        try { coordinator.ValidateOwnership(); } catch (InvalidOperationException exception) { diagnostics.Add(new("FIDELITY-OWNER", "error", exception.Message, [])); }
        if (world.Activities.Any(activity => activity.Status != SimulationActivityStatus.Completed)) diagnostics.Add(new("ABS-ACTIVITY", "error", "activity did not complete", world.Activities.Where(activity => activity.Status != SimulationActivityStatus.Completed).Select(activity => activity.Id).ToArray()));
        if (world.Reservations.Any(reservation => reservation.Status == SimulationReservationStatus.Active)) diagnostics.Add(new("LOGISTICS-CONSERVATION", "error", "reservation leaked", world.Reservations.Where(reservation => reservation.Status == SimulationReservationStatus.Active).Select(reservation => reservation.Id).ToArray()));
        if (scheduler.Inspect().Any(trigger => trigger.Status == ScheduledTriggerStatus.Delivered)) diagnostics.Add(new("DES-TRIGGER", "error", "delivered trigger lacks final outcome", scheduler.Inspect().Where(trigger => trigger.Status == ScheduledTriggerStatus.Delivered).Select(trigger => trigger.Id).ToArray()));
        return diagnostics;
    }

    private static void Create(SimulationWorld world, string id, string region, string component, object value)
    {
        Require(world.CreateEntity(id, SimulationEntityScope.RegionOwned, new(region)).Status == "accepted");
        Require(world.ActivateEntity(id).Status == "accepted");
        Require(world.SetComponent(id, component, JsonSerializer.SerializeToElement(value)).Status == "accepted");
    }

    private static void Require(bool condition) { if (!condition) throw new InvalidOperationException("M033 scenario setup command rejected."); }
    private static string Fingerprint(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
