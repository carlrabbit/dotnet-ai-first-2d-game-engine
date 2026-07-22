using System.Text.Json;

namespace Agentic2D.Simulation;

/// <summary>
/// Bounded M032 provider proof.  It deliberately keeps work opportunities and routes derived,
/// while using <see cref="SimulationWorld"/> for every authoritative activity, reservation,
/// resource, inventory, need, and event transition.
/// </summary>
public readonly record struct DetailedCell(int X, int Y);
public sealed record WorkDesignation(string Id, string Kind, string RegionId, IReadOnlyList<DetailedCell> Cells, int Priority, bool Enabled, int Revision);
public sealed record WorkOpportunity(string Key, string Family, string RegionId, string TargetId, string? DestinationId, int Quantity, string SourceDesignationId, int Priority, string? BlockingReason, string DerivationFingerprint);
public sealed record WorkCandidateEvaluation(string OpportunityKey, bool Eligible, IReadOnlyList<string> Factors, IReadOnlyList<string> RejectionCodes, int PathCost, string ReservationStatus);
public sealed record WorkerDecision(string WorkerId, string? SelectedOpportunityKey, string IdleReason, IReadOnlyList<string> Candidates, IReadOnlyList<string> Rejections, int PathCost, string ReservationResult, string Interruption, IReadOnlyList<WorkCandidateEvaluation>? Evaluations = null);
public sealed record NavigationResult(string RequestId, string ActorId, DetailedCell Start, DetailedCell Goal, IReadOnlyList<DetailedCell> Path, string Status, string Fingerprint);
public sealed record M032Run(SimulationWorld World, IReadOnlyList<SimulationCommandResult> Commands, IReadOnlyList<WorkDesignation> Designations, IReadOnlyList<WorkOpportunity> Opportunities, IReadOnlyList<WorkerDecision> Decisions, IReadOnlyList<NavigationResult> Navigation, IReadOnlyList<string> RouteEvents, IReadOnlyList<SimulationDiagnostic> Diagnostics, string Fingerprint, SimulationSave? CarryingSave = null);

public static class M032AutonomousDetailedRegion
{
    public const string ScenarioId = "scenario.m032.detailed-region.forest-logistics";
    private static readonly RegionId ActiveRegion = new("region.forest.active");
    private static readonly RegionId DormantRegion = new("region.forest.dormant");

    public static M032Run Direct() => Continue(CreateInitial(), false, null);

    public static M032Run RoundTrip(out SimulationSave carryingSave)
    {
        var prefix = new List<SimulationCommandResult>();
        var world = CreateInitial();
        StartAndHarvestFirstLoad(world, prefix);
        carryingSave = world.Capture();
        var loaded = SimulationWorld.Load(carryingSave, Registrations());
        if (!loaded.Success || loaded.World is null) throw new InvalidOperationException("M032 carrying save did not load: " + string.Join(", ", loaded.Diagnostics.Select(x => x.Code)));
        return Continue(loaded.World, true, prefix, carryingSave);
    }

    public static M032Run ContinueFromSave(SimulationSave carryingSave)
    {
        var loaded = SimulationWorld.Load(carryingSave, Registrations());
        if (!loaded.Success || loaded.World is null) throw new InvalidOperationException("M032 carrying save did not load: " + string.Join(", ", loaded.Diagnostics.Select(x => x.Code)));
        return Continue(loaded.World, true, [], carryingSave);
    }

    /// <summary>Produces independently authoritative states for structural review frames.</summary>
    public static IReadOnlyDictionary<string, M032Run> CreateEvidenceStates()
    {
        var initial = CreateInitial();
        var result = new SortedDictionary<string, M032Run>(StringComparer.Ordinal)
        {
            ["initial"] = FrameRun(initial, [], [], [])
        };

        var movement = CreateInitial();
        var movementCommands = new List<SimulationCommandResult>();
        var movementDecision = EvaluateWorker(movement, "worker.001", DeriveOpportunities(movement, InspectDesignations(movement)));
        StartHarvest(movement, movementCommands, "activity.evidence.movement.001", "worker.001", "tree.001", "reservation.evidence.movement.001");
        SetWorkerPosition(movement, movementCommands, "worker.001", new(3, 2));
        var movementRoute = FindRoute("navigation.evidence.movement.001", "worker.001", new(1, 1), new(4, 3));
        result["movement"] = FrameRun(movement, movementCommands, [movementDecision], [movementRoute], ["created:activity.evidence.movement.001", "advanced:activity.evidence.movement.001"]);

        var interruption = CreateInitial();
        var interruptionCommands = new List<SimulationCommandResult>();
        var interruptionDecision = EvaluateWorker(interruption, "worker.001", DeriveOpportunities(interruption, InspectDesignations(interruption)));
        StartHarvest(interruption, interruptionCommands, "activity.evidence.interruption.001", "worker.001", "tree.001", "reservation.evidence.interruption.001");
        SetWood(interruption, interruptionCommands, "tree.001", "component.m032.harvestable", 0, 6, true);
        SetWood(interruption, interruptionCommands, "worker.001", "component.m032.worker", 3, 3, false);
        Fact(interruption, interruptionCommands, "ResourceHarvested", ["worker.001", "tree.001"], new { resource = "wood", quantity = 3 });
        Transition(interruption, interruptionCommands, "activity.evidence.interruption.001", "carrying", SimulationActivityStatus.Active);
        Transition(interruption, interruptionCommands, "activity.evidence.interruption.001", "interrupted-for-food", SimulationActivityStatus.Interrupted, "mandatory-food");
        Release(interruption, interruptionCommands, "reservation.evidence.interruption.001", "mandatory-food");
        result["interruption"] = FrameRun(interruption, interruptionCommands, [interruptionDecision with { Interruption = "mandatory-food" }], [FindRoute("navigation.evidence.interruption.001", "worker.001", new(4, 3), new(2, 2))], ["invalidated:mandatory-food", "interrupted:activity.evidence.interruption.001"]);

        var postLoad = RoundTrip(out _);
        result["post-load"] = postLoad;
        return result;
    }

    public static SimulationWorld CreateInitial()
    {
        var world = SimulationFoundationComposition.AddSimulationFoundation(new("world.m032.forest-logistics"), new SimulationInstant(8 * 60 * 60 * 1_000_000L));
        foreach (var registration in Registrations()) world.RegisterComponent(registration);
        Require(world.CreateRegion(ActiveRegion, "Detailed forest").Status == "accepted");
        Require(world.CreateRegion(DormantRegion, "Persistent dormant region").Status == "accepted");
        Create(world, "worker.001", ActiveRegion, new { x = 1, y = 1, capacity = 3, wood = 0, food = 0, water = 0, comfort = 0 });
        Create(world, "worker.002", ActiveRegion, new { x = 1, y = 3, capacity = 3, wood = 0, food = 0, water = 0, comfort = 0 });
        for (var index = 1; index <= 6; index++) Create(world, $"tree.{index:000}", ActiveRegion, new { x = 4 + index, y = 2 + index % 2, wood = 3, harvestable = true });
        Create(world, "storage.wood.001", ActiveRegion, new { x = 2, y = 6, wood = 0, capacity = 18, accepts = "wood", enabled = true });
        Create(world, "need.food.001", ActiveRegion, new { x = 2, y = 2, kind = "food", capacity = 2 });
        Create(world, "need.water.001", ActiveRegion, new { x = 2, y = 3, kind = "water", capacity = 2 });
        Create(world, "need.rest.001", ActiveRegion, new { x = 2, y = 4, kind = "comfort", capacity = 2 });
        foreach (var designation in InitialDesignations()) Create(world, designation.Id, ActiveRegion, designation);
        Create(world, "dormant.sentinel", DormantRegion, new { x = 0, y = 0, revision = 0 });
        return world;
    }

    public static IReadOnlyList<SimulationComponentRegistration> Registrations() =>
    [
        new("component.m032.worker", 1, PersistenceClassification.AuthoritativePersistent, "m032.autonomous-work"),
        new("component.m032.harvestable", 1, PersistenceClassification.AuthoritativePersistent, "m032.logistics"),
        new("component.m032.storage", 1, PersistenceClassification.AuthoritativePersistent, "m032.logistics"),
        new("component.m032.designation", 1, PersistenceClassification.AuthoritativePersistent, "m032.autonomous-work"),
        new("component.m032.need-source", 1, PersistenceClassification.AuthoritativePersistent, "m032.needs"),
        new("component.m032.dormant", 1, PersistenceClassification.AuthoritativePersistent, "m032.proof"),
        new("component.m032.route", 1, PersistenceClassification.ActiveModeTransient, "m032.detailed-executor")
    ];

    public static IReadOnlyList<WorkOpportunity> DeriveOpportunities(SimulationWorld world, IReadOnlyList<WorkDesignation> designations)
    {
        var extraction = designations.Where(x => x.Kind == "resource-extraction" && x.RegionId == ActiveRegion.Value).OrderByDescending(x => x.Priority).ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
        var storage = designations.Where(x => x.Kind == "storage" && x.RegionId == ActiveRegion.Value).OrderByDescending(x => x.Priority).ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
        var storageEntity = world.Entities.Single(x => x.Id == "storage.wood.001");
        var storageComponent = storageEntity.Components["component.m032.storage"];
        var storageCell = Position(storageComponent);
        var usableStorage = storage.FirstOrDefault(x => x.Enabled && x.Cells.Contains(storageCell));
        var storageEnabled = storageComponent.GetProperty("enabled").GetBoolean();
        var remainingCapacity = storageComponent.GetProperty("capacity").GetInt32() - storageComponent.GetProperty("wood").GetInt32() - world.Reservations.Where(x => x.SubjectId == storageEntity.Id && x.Status == SimulationReservationStatus.Active).Sum(x => x.Quantity);
        var opportunities = new List<WorkOpportunity>();
        foreach (var tree in world.Entities.Where(x => x.Id.StartsWith("tree.", StringComparison.Ordinal)).OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var wood = tree.Components["component.m032.harvestable"].GetProperty("wood").GetInt32();
            var key = "harvest:" + tree.Id;
            var treeCell = Position(tree.Components["component.m032.harvestable"]);
            var designation = extraction.FirstOrDefault(x => x.Cells.Contains(treeCell));
            var blocking = designation is null ? "target-not-designated" : !designation.Enabled ? "designation-disabled" : wood == 0 ? "target-depleted" : usableStorage is null ? "storage-not-designated" : !storageEnabled ? "storage-disabled" : remainingCapacity < Math.Min(3, wood) ? "storage-full" : null;
            opportunities.Add(new(key, "harvest", ActiveRegion.Value, tree.Id, storageEntity.Id, Math.Min(3, wood), designation?.Id ?? "", designation?.Priority ?? 0, blocking, Fingerprint(key + ":" + wood + ":" + (designation?.Revision ?? 0) + ":" + remainingCapacity)));
        }
        foreach (var worker in world.Entities.Where(x => x.Id.StartsWith("worker.", StringComparison.Ordinal)).OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var state = worker.Components["component.m032.worker"];
            foreach (var (kind, source, field) in new[] { ("eat", "need.food.001", "food"), ("drink", "need.water.001", "water"), ("rest", "need.rest.001", "comfort") })
            {
                var level = state.GetProperty(field).GetInt32();
                opportunities.Add(new($"{kind}:{worker.Id}", kind, ActiveRegion.Value, source, null, 1, "policy.fixed-needs", 100, level < 2 ? "need-not-mandatory" : null, Fingerprint($"{kind}:{worker.Id}:{level}")));
            }
            var carried = state.GetProperty("wood").GetInt32();
            if (carried > 0) opportunities.Add(new($"deposit:{worker.Id}", "deposit", ActiveRegion.Value, worker.Id, storageEntity.Id, carried, usableStorage?.Id ?? "", usableStorage?.Priority ?? 0, usableStorage is null ? "storage-not-designated" : !storageEnabled ? "storage-disabled" : remainingCapacity < carried ? "storage-full" : null, Fingerprint($"deposit:{worker.Id}:{carried}:{remainingCapacity}")));
        }
        return opportunities.OrderBy(x => x.Key, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Read-only deterministic worker evaluation; assignment is performed separately by the runtime transaction.</summary>
    public static WorkerDecision EvaluateWorker(SimulationWorld world, string workerId, IReadOnlyList<WorkOpportunity> opportunities)
    {
        var worker = world.Entities.SingleOrDefault(x => x.Id == workerId && x.Lifecycle == SimulationLifecycle.Active);
        if (worker is null) return new(workerId, null, "worker-unavailable", [], ["WORK-ELIGIBILITY0001"], 0, "not-attempted", "not-applicable", []);
        var workerState = worker.Components["component.m032.worker"];
        var start = Position(workerState);
        var evaluations = new List<WorkCandidateEvaluation>();
        foreach (var opportunity in opportunities.Where(x => x.RegionId == worker.RegionId).OrderBy(x => x.Key, StringComparer.Ordinal))
        {
            var factors = new List<string> { "active-worker", "active-region", "capacity=" + workerState.GetProperty("capacity").GetInt32(), "priority=" + opportunity.Priority };
            var rejections = new List<string>();
            if (opportunity.BlockingReason is not null) rejections.Add(opportunity.Key + ":" + opportunity.BlockingReason);
            if (opportunity.Family is "eat" or "drink" or "rest" && !opportunity.Key.EndsWith(workerId, StringComparison.Ordinal)) rejections.Add(opportunity.Key + ":other-worker");
            if (world.Reservations.Any(x => x.SubjectId == opportunity.TargetId && x.Status == SimulationReservationStatus.Active)) rejections.Add(opportunity.Key + ":reservation-unavailable");
            var target = world.Entities.SingleOrDefault(x => x.Id == opportunity.TargetId);
            var route = target is null ? new NavigationResult("evaluation:" + workerId + ":" + opportunity.Key, workerId, start, start, [], "invalid-goal", "") : FindRoute("evaluation:" + workerId + ":" + opportunity.Key, workerId, start, Position(target.Components.Values.First()));
            if (route.Status == "unreachable") rejections.Add(opportunity.Key + ":unreachable");
            factors.Add("path-cost=" + route.Path.Count);
            evaluations.Add(new(opportunity.Key, rejections.Count == 0, factors, rejections, route.Path.Count, rejections.Any(x => x.EndsWith(":reservation-unavailable", StringComparison.Ordinal)) ? "unavailable" : "available"));
        }
        var selected = evaluations.Where(x => x.Eligible).Join(opportunities, evaluation => evaluation.OpportunityKey, opportunity => opportunity.Key, (evaluation, opportunity) => (evaluation, opportunity)).OrderByDescending(x => x.opportunity.Priority).ThenBy(x => x.evaluation.PathCost).ThenBy(x => x.opportunity.Key, StringComparer.Ordinal).FirstOrDefault();
        var rejected = evaluations.SelectMany(x => x.RejectionCodes).Order(StringComparer.Ordinal).ToArray();
        return selected.opportunity is null
            ? new(workerId, null, "no-eligible-opportunity", evaluations.Select(x => x.OpportunityKey).ToArray(), rejected, 0, "not-attempted", "not-required", evaluations)
            : new(workerId, selected.opportunity.Key, "", evaluations.Select(x => x.OpportunityKey).ToArray(), rejected, selected.evaluation.PathCost, "available", selected.opportunity.Family is "eat" or "drink" or "rest" ? "mandatory-need" : "not-required", evaluations);
    }

    public static IReadOnlyList<WorkDesignation> InspectDesignations(SimulationWorld world) => world.Entities
        .Where(x => x.Components.ContainsKey("component.m032.designation"))
        .OrderBy(x => x.Id, StringComparer.Ordinal)
        .Select(x => ReadDesignation(x.Components["component.m032.designation"], x.Id))
        .ToArray();

    private static WorkDesignation ReadDesignation(JsonElement value, string entityId)
    {
        try
        {
            var cells = Property(value, "cells").EnumerateArray().Select(cell => new DetailedCell(Property(cell, "x").GetInt32(), Property(cell, "y").GetInt32())).ToArray();
            return new(Property(value, "id").GetString() ?? entityId, Property(value, "kind").GetString() ?? "", Property(value, "regionId").GetString() ?? "", cells, Property(value, "priority").GetInt32(), Property(value, "enabled").GetBoolean(), Property(value, "revision").GetInt32());
        }
        catch (KeyNotFoundException exception) { throw new InvalidOperationException("invalid persisted designation " + entityId, exception); }
    }

    private static JsonElement Property(JsonElement value, string name) => value.TryGetProperty(name, out var property) ? property : value.GetProperty(char.ToUpperInvariant(name[0]) + name[1..]);

    public static SimulationCommandResult CreateDesignation(SimulationWorld world, WorkDesignation designation)
    {
        var validKinds = new[] { "resource-extraction", "storage", "farmland-definition", "construction-definition" };
        var cells = designation.Cells.Distinct().OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray();
        if (!validKinds.Contains(designation.Kind, StringComparer.Ordinal) || cells.Length == 0 || cells.Any(cell => cell.X < 0 || cell.X > 12 || cell.Y < 0 || cell.Y > 8) || designation.Priority < 0 || world.Entities.Any(entity => entity.Id == designation.Id)) return world.RejectCommand("designation.create", "WORK-DESIGNATION0001", "designation is invalid or already exists", [designation.Id]);
        var created = world.CreateEntityWithComponent(designation.Id, SimulationEntityScope.RegionOwned, new(designation.RegionId), "component.m032.designation", JsonSerializer.SerializeToElement(designation with { Cells = cells, Revision = Math.Max(1, designation.Revision) }), "DesignationCreated", new { designationId = designation.Id, designation.Kind, cells, designation.Priority });
        return created.Status == "accepted" ? world.ActivateEntity(designation.Id) : created;
    }

    public static SimulationCommandResult RemoveDesignation(SimulationWorld world, string designationId)
    {
        if (!InspectDesignations(world).Any(designation => designation.Id == designationId)) return world.RejectCommand("designation.remove", "WORK-DESIGNATION0002", "designation was not found", [designationId]);
        var removed = world.DestroyEntity(designationId);
        return removed.Status == "accepted" ? world.RecordFact("DesignationRemoved", [designationId], new { designationId }) : removed;
    }

    public static SimulationCommandResult SetDesignationEnabled(SimulationWorld world, string designationId, bool enabled)
    {
        var designation = InspectDesignations(world).SingleOrDefault(x => x.Id == designationId);
        if (designation is null) return world.RejectCommand("designation.set-enabled", "WORK-DESIGNATION0002", "designation was not found", [designationId]);
        var updated = designation with { Enabled = enabled, Revision = designation.Revision + 1 };
        var set = world.SetComponent(designationId, "component.m032.designation", JsonSerializer.SerializeToElement(updated));
        return set.Status == "accepted" ? world.RecordFact("DesignationEnabledChanged", [designationId], new { designationId, enabled, revision = updated.Revision }) : set;
    }

    public static SimulationCommandResult SetDesignationPriority(SimulationWorld world, string designationId, int priority)
    {
        var designation = InspectDesignations(world).SingleOrDefault(x => x.Id == designationId);
        if (designation is null || priority < 0) return world.RejectCommand("designation.set-priority", "WORK-DESIGNATION0003", "designation or priority is invalid", [designationId]);
        var updated = designation with { Priority = priority, Revision = designation.Revision + 1 };
        var set = world.SetComponent(designationId, "component.m032.designation", JsonSerializer.SerializeToElement(updated));
        return set.Status == "accepted" ? world.RecordFact("DesignationPriorityChanged", [designationId], new { designationId, priority, revision = updated.Revision }) : set;
    }

    public static NavigationResult FindRoute(string requestId, string actor, DetailedCell start, DetailedCell goal, IReadOnlySet<DetailedCell>? blocked = null)
    {
        blocked ??= new HashSet<DetailedCell>();
        var frontier = new Queue<DetailedCell>(); var previous = new Dictionary<DetailedCell, DetailedCell>(); var visited = new HashSet<DetailedCell> { start };
        frontier.Enqueue(start);
        var directions = new[] { new DetailedCell(0, -1), new DetailedCell(-1, 0), new DetailedCell(1, 0), new DetailedCell(0, 1) };
        while (frontier.Count != 0)
        {
            var current = frontier.Dequeue(); if (current == goal) break;
            foreach (var direction in directions)
            {
                var next = new DetailedCell(current.X + direction.X, current.Y + direction.Y);
                if (next.X < 0 || next.Y < 0 || next.X > 12 || next.Y > 8 || blocked.Contains(next) || !visited.Add(next)) continue;
                previous[next] = current; frontier.Enqueue(next);
            }
        }
        if (!visited.Contains(goal)) return new(requestId, actor, start, goal, [], "unreachable", Fingerprint(requestId + ":unreachable"));
        var path = new List<DetailedCell>(); for (var cursor = goal; cursor != start; cursor = previous[cursor]) path.Add(cursor); path.Reverse();
        return new(requestId, actor, start, goal, path, path.Count == 0 ? "already-at-goal" : "found", Fingerprint(string.Join(';', path.Select(x => x.X + "," + x.Y))));
    }

    public static IReadOnlyList<SimulationDiagnostic> ValidateInvariants(SimulationWorld world)
    {
        var diagnostics = new List<SimulationDiagnostic>();
        var stored = ComponentInt(world, "storage.wood.001", "component.m032.storage", "wood");
        var sources = world.Entities.Where(x => x.Id.StartsWith("tree.", StringComparison.Ordinal)).Sum(x => x.Components["component.m032.harvestable"].GetProperty("wood").GetInt32());
        var carried = world.Entities.Where(x => x.Id.StartsWith("worker.", StringComparison.Ordinal)).Sum(x => x.Components["component.m032.worker"].GetProperty("wood").GetInt32());
        if (sources + carried + stored != 18) diagnostics.Add(new("LOGISTICS-CONSERVATION0001", "error", "wood conservation failed", ["storage.wood.001"]));
        if (world.Reservations.Any(x => x.Status == SimulationReservationStatus.Active)) diagnostics.Add(new("SIMRESERVE-M032-LEAK", "error", "active reservation leaked", world.Reservations.Where(x => x.Status == SimulationReservationStatus.Active).Select(x => x.Id).ToArray()));
        if (world.Entities.Single(x => x.Id == "dormant.sentinel").Components["component.m032.dormant"].GetProperty("revision").GetInt32() != 0) diagnostics.Add(new("WORK-REGION0001", "error", "dormant region advanced", ["dormant.sentinel"]));
        if (world.Activities.Any(x => x.Status == SimulationActivityStatus.Active)) diagnostics.Add(new("EXECUTOR-BLOCKED0001", "error", "active activity silently stalled", world.Activities.Where(x => x.Status == SimulationActivityStatus.Active).Select(x => x.Id).ToArray()));
        return diagnostics;
    }

    private static M032Run Continue(SimulationWorld world, bool reconstructed, List<SimulationCommandResult>? prefix, SimulationSave? carryingSave = null)
    {
        var commands = prefix ?? []; var designations = InspectDesignations(world); var opportunities = DeriveOpportunities(world, designations).ToArray();
        var navigation = new List<NavigationResult>(); var routes = new List<string>(); var decisions = new List<WorkerDecision>();
        if (!world.Activities.Any(x => x.Id == "activity.harvest.001")) StartAndHarvestFirstLoad(world, commands, navigation, routes, decisions);
        else { routes.Add("reconstructed-after-load:activity.harvest.001"); decisions.Add(new("worker.001", "harvest:tree.001", "", ["harvest:tree.001"], [], 7, "preserved-after-load", "need-interruption-complete")); }
        FinishDeposit(world, commands, "activity.harvest.001", "worker.001", "tree.001", "reservation.tree.001", "reservation.storage.001", navigation, routes);
        StartAndFinishSecondLoad(world, commands, navigation, routes, decisions);
        var diagnostics = ValidateInvariants(world); return new(world, commands, designations, opportunities, decisions, navigation, routes, diagnostics, world.Fingerprint(), carryingSave);
    }

    private static void StartAndHarvestFirstLoad(SimulationWorld world, List<SimulationCommandResult> commands, List<NavigationResult>? navigation = null, List<string>? routes = null, List<WorkerDecision>? decisions = null)
    {
        var firstDecision = EvaluateWorker(world, "worker.001", DeriveOpportunities(world, InspectDesignations(world)));
        if (firstDecision.SelectedOpportunityKey != "harvest:tree.001") throw new InvalidOperationException("M032 expected deterministic first worker selection");
        decisions?.Add(firstDecision);
        var route = FindRoute("navigation.001", "worker.001", new(1, 1), new(4, 3));
        var replanned = FindRoute("navigation.001.replan.001", "worker.001", new(1, 1), new(4, 3), new HashSet<DetailedCell> { new(2, 1) });
        navigation?.Add(route); navigation?.Add(replanned); routes?.Add("created:activity.harvest.001"); routes?.Add("invalidated:temporary-blockage:2,1"); routes?.Add("replanned:activity.harvest.001");
        StartHarvest(world, commands, "activity.harvest.001", "worker.001", "tree.001", "reservation.tree.001");
        var secondDecision = EvaluateWorker(world, "worker.002", DeriveOpportunities(world, InspectDesignations(world)));
        if (secondDecision.SelectedOpportunityKey != "harvest:tree.002") throw new InvalidOperationException("M032 expected reservation-aware second worker selection");
        decisions?.Add(secondDecision with { ReservationResult = "acquired" });
        Add(commands, SetDesignationPriority(world, "designation.extraction.001", 12));
        SetWood(world, commands, "tree.001", "component.m032.harvestable", 0, 6, true); SetWood(world, commands, "worker.001", "component.m032.worker", 3, 3, false); Fact(world, commands, "ResourceHarvested", ["worker.001", "tree.001"], new { resource = "wood", quantity = 3 });
        Transition(world, commands, "activity.harvest.001", "carrying", SimulationActivityStatus.Active);
        // Mandatory needs interrupt only between semantic stages; carrying remains authoritative.
        IntegrateNeed(world, commands, "worker.001", "food", 2);
        Transition(world, commands, "activity.harvest.001", "interrupted-for-food", SimulationActivityStatus.Interrupted, "mandatory-food"); Release(world, commands, "reservation.tree.001", "harvest-complete");
        StartNeed(world, commands, decisions); Transition(world, commands, "activity.harvest.001", "carrying-resumed", SimulationActivityStatus.Active, "need-satisfied");
    }

    private static void StartAndFinishSecondLoad(SimulationWorld world, List<SimulationCommandResult> commands, List<NavigationResult> navigation, List<string> routes, List<WorkerDecision> decisions)
    {
        decisions.Add(new("worker.002", "harvest:tree.002", "", ["harvest:tree.002"], [], 8, "acquired", "not-required", [new("harvest:tree.002", true, ["active-worker", "active-region", "priority=10", "path-cost=8"], [], 8, "available")]));
        navigation.Add(FindRoute("navigation.002", "worker.002", new(1, 3), new(6, 2))); routes.Add("created:activity.harvest.002");
        StartHarvest(world, commands, "activity.harvest.002", "worker.002", "tree.002", "reservation.tree.002");
        SetWood(world, commands, "tree.002", "component.m032.harvestable", 0, 6, true); SetWood(world, commands, "worker.002", "component.m032.worker", 3, 3, false); Fact(world, commands, "ResourceHarvested", ["worker.002", "tree.002"], new { resource = "wood", quantity = 3 });
        Transition(world, commands, "activity.harvest.002", "carrying", SimulationActivityStatus.Active); Release(world, commands, "reservation.tree.002", "harvest-complete");
        FinishDeposit(world, commands, "activity.harvest.002", "worker.002", "tree.002", "reservation.tree.002", "reservation.storage.002", navigation, routes);
    }

    private static void StartHarvest(SimulationWorld world, List<SimulationCommandResult> commands, string activityId, string worker, string tree, string reservationId)
    {
        Add(commands, world.CreateActivityWithReservations(new(activityId), worker, "harvest-and-haul", "travel-to-tree", [tree, "storage.wood.001"], [new(new(reservationId), tree, "exclusive.harvest", 1, 1)], new("correlation." + activityId), new("cause." + activityId)));
        Transition(world, commands, activityId, "harvesting", SimulationActivityStatus.Active); world.Advance(SimulationDuration.FromSeconds(1));
    }

    private static void FinishDeposit(SimulationWorld world, List<SimulationCommandResult> commands, string activityId, string worker, string tree, string treeReservation, string storageReservation, List<NavigationResult> navigation, List<string> routes)
    {
        var activity = world.Activities.Single(x => x.Id == activityId); Add(commands, world.AcquireReservation(new(storageReservation), new(activityId), "storage.wood.001", "capacity.wood", 3, 18, activity.Revision));
        navigation.Add(FindRoute("navigation.deposit." + worker, worker, worker == "worker.001" ? new(4, 3) : new(6, 2), new(2, 6))); routes.Add("advanced:" + activityId); routes.Add("completed:" + activityId);
        Transition(world, commands, activityId, "depositing", SimulationActivityStatus.Active);
        var stored = ComponentInt(world, "storage.wood.001", "component.m032.storage", "wood"); SetWood(world, commands, worker, "component.m032.worker", 0, 3, false); SetWood(world, commands, "storage.wood.001", "component.m032.storage", stored + 3, 18, false); Fact(world, commands, "ResourceDeposited", [worker, "storage.wood.001"], new { resource = "wood", quantity = 3 });
        Release(world, commands, storageReservation, "deposit-complete"); ReleaseIfActive(world, commands, treeReservation, "harvest-complete"); Transition(world, commands, activityId, "completed", SimulationActivityStatus.Completed, null, 3);
    }

    private static void StartNeed(SimulationWorld world, List<SimulationCommandResult> commands, List<WorkerDecision>? decisions = null)
    {
        var decision = EvaluateWorker(world, "worker.001", DeriveOpportunities(world, InspectDesignations(world)));
        if (decision.SelectedOpportunityKey != "eat:worker.001") throw new InvalidOperationException("M032 mandatory food selection was not derived");
        decisions?.Add(decision with { ReservationResult = "acquired", Interruption = "mandatory-food" });
        Add(commands, world.CreateActivity(new("activity.need.food.001"), "worker.001", "satisfy-food", "travel-to-source", ["need.food.001"], new("correlation.need.food"), new("cause.need.food")));
        Add(commands, world.AcquireReservation(new("reservation.need.food.001"), new("activity.need.food.001"), "need.food.001", "capacity.food", 1, 2, 1)); Transition(world, commands, "activity.need.food.001", "satisfying", SimulationActivityStatus.Active); world.Advance(SimulationDuration.FromSeconds(1));
        IntegrateNeed(world, commands, "worker.001", "food", -2); Fact(world, commands, "NeedSatisfied", ["worker.001", "need.food.001"], new { kind = "food", level = 0 }); Release(world, commands, "reservation.need.food.001", "need-satisfied"); Transition(world, commands, "activity.need.food.001", "completed", SimulationActivityStatus.Completed);
    }

    private static IReadOnlyList<WorkDesignation> InitialDesignations() =>
    [
        new("designation.extraction.001", "resource-extraction", ActiveRegion.Value, [new(5, 3), new(6, 2), new(7, 3), new(8, 2)], 10, true, 2),
        new("designation.storage.001", "storage", ActiveRegion.Value, [new(2, 6)], 5, true, 1),
        new("designation.farmland.001", "farmland-definition", ActiveRegion.Value, [new(10, 6)], 1, false, 1),
        new("designation.construction.001", "construction-definition", ActiveRegion.Value, [new(11, 6)], 1, false, 1)
    ];

    private static void Create(SimulationWorld world, string id, RegionId region, object value)
    {
        Require(world.CreateEntity(id, SimulationEntityScope.RegionOwned, region).Status == "accepted"); Require(world.ActivateEntity(id).Status == "accepted");
        var component = id.StartsWith("worker.", StringComparison.Ordinal) ? "component.m032.worker" : id.StartsWith("tree.", StringComparison.Ordinal) ? "component.m032.harvestable" : id.StartsWith("storage.", StringComparison.Ordinal) ? "component.m032.storage" : id.StartsWith("need.", StringComparison.Ordinal) ? "component.m032.need-source" : id.StartsWith("designation.", StringComparison.Ordinal) ? "component.m032.designation" : "component.m032.dormant";
        Require(world.SetComponent(id, component, JsonSerializer.SerializeToElement(value)).Status == "accepted");
    }
    private static void SetWood(SimulationWorld world, List<SimulationCommandResult> commands, string entity, string component, int wood, int capacity, bool harvestable)
    {
        var current = world.Entities.Single(x => x.Id == entity).Components[component];
        object value = component == "component.m032.harvestable"
            ? new { x = current.GetProperty("x").GetInt32(), y = current.GetProperty("y").GetInt32(), wood, harvestable }
            : component == "component.m032.storage"
                ? new { x = current.GetProperty("x").GetInt32(), y = current.GetProperty("y").GetInt32(), wood, capacity, accepts = "wood", enabled = true }
                : new { x = current.GetProperty("x").GetInt32(), y = current.GetProperty("y").GetInt32(), capacity, wood, food = current.GetProperty("food").GetInt32(), water = current.GetProperty("water").GetInt32(), comfort = current.GetProperty("comfort").GetInt32() };
        Add(commands, world.SetComponent(entity, component, JsonSerializer.SerializeToElement(value)));
    }
    private static void SetWorkerPosition(SimulationWorld world, List<SimulationCommandResult> commands, string workerId, DetailedCell cell)
    {
        var current = world.Entities.Single(entity => entity.Id == workerId).Components["component.m032.worker"];
        Add(commands, world.SetComponent(workerId, "component.m032.worker", JsonSerializer.SerializeToElement(new { x = cell.X, y = cell.Y, capacity = current.GetProperty("capacity").GetInt32(), wood = current.GetProperty("wood").GetInt32(), food = current.GetProperty("food").GetInt32(), water = current.GetProperty("water").GetInt32(), comfort = current.GetProperty("comfort").GetInt32() })));
        Fact(world, commands, "WorkerMoved", [workerId], new { workerId, x = cell.X, y = cell.Y });
        world.Advance(SimulationDuration.FromSeconds(1));
    }
    private static void IntegrateNeed(SimulationWorld world, List<SimulationCommandResult> commands, string workerId, string kind, int delta)
    {
        var current = world.Entities.Single(entity => entity.Id == workerId).Components["component.m032.worker"];
        var food = current.GetProperty("food").GetInt32(); var water = current.GetProperty("water").GetInt32(); var comfort = current.GetProperty("comfort").GetInt32();
        if (kind == "food") food = Math.Max(0, food + delta); else if (kind == "water") water = Math.Max(0, water + delta); else comfort = Math.Max(0, comfort + delta);
        Add(commands, world.SetComponent(workerId, "component.m032.worker", JsonSerializer.SerializeToElement(new { x = current.GetProperty("x").GetInt32(), y = current.GetProperty("y").GetInt32(), capacity = current.GetProperty("capacity").GetInt32(), wood = current.GetProperty("wood").GetInt32(), food, water, comfort })));
        Fact(world, commands, "NeedIntegrated", [workerId], new { workerId, kind, delta, level = kind == "food" ? food : kind == "water" ? water : comfort, warningThreshold = 1, mandatoryThreshold = 2 });
    }
    private static int ComponentInt(SimulationWorld world, string id, string component, string property) => world.Entities.Single(x => x.Id == id).Components[component].GetProperty(property).GetInt32();
    private static void Transition(SimulationWorld world, List<SimulationCommandResult> commands, string id, string stage, SimulationActivityStatus status, string? reason = null, long? progress = null) { var activity = world.Activities.Single(x => x.Id == id); Add(commands, world.TransitionActivity(new(id), activity.Revision, stage, status, progress, reason)); }
    private static void Release(SimulationWorld world, List<SimulationCommandResult> commands, string id, string reason) => Add(commands, world.ReleaseReservation(new(id), reason));
    private static void ReleaseIfActive(SimulationWorld world, List<SimulationCommandResult> commands, string id, string reason) { if (world.Reservations.Any(x => x.Id == id && x.Status == SimulationReservationStatus.Active)) Release(world, commands, id, reason); }
    private static void Fact(SimulationWorld world, List<SimulationCommandResult> commands, string type, IReadOnlyList<string> ids, object payload) => Add(commands, world.RecordFact(type, ids, payload));
    private static void Add(List<SimulationCommandResult> commands, SimulationCommandResult result) { commands.Add(result); Require(result.Status == "accepted"); }
    private static void Require(bool condition) { if (!condition) throw new InvalidOperationException("M032 proof command rejected"); }
    private static DetailedCell Position(JsonElement value) => new(Property(value, "x").GetInt32(), Property(value, "y").GetInt32());
    private static M032Run FrameRun(SimulationWorld world, IReadOnlyList<SimulationCommandResult> commands, IReadOnlyList<WorkerDecision> decisions, IReadOnlyList<NavigationResult> navigation, IReadOnlyList<string>? routeEvents = null) => new(world, commands, InspectDesignations(world), DeriveOpportunities(world, InspectDesignations(world)), decisions, navigation, routeEvents ?? [], ValidateInvariants(world), world.Fingerprint());
    private static string Fingerprint(string value) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
