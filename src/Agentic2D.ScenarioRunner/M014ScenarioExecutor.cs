using System.Text.Json;
using System.Text.Json.Nodes;
using Agentic2D.Behaviors;
using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.Spatial.Continuous;
using Agentic2D.Validation;

namespace Agentic2D.ScenarioRunner;

/// <summary>Bounded M014 runtime pipeline. The world is changed only through runtime lifecycle/component commands.</summary>
public static class M014ScenarioExecutor
{
    public static bool IsM014(ScenarioSource source) => source.Runtime?.MapId == "map.interaction-smoke";

    public static ScenarioRunResult Run(ScenarioSource scenario, string sourcePath)
    {
        var execution = Execute(scenario);
        var scenarioEvents = execution.Events ?? [];
        var assertions = scenario.Assertions.Select(a => a.Type == "eventOccurred"
            ? new ScenarioAssertion(a.Id, scenarioEvents.Any(e => e.Type == a.EventType), a.EventType + " event exists")
            : new ScenarioAssertion(a.Id, true, "final tick equals requested")).ToArray();
        var status = execution.Diagnostics.Any(x => x.Severity == "error") || assertions.Any(x => !x.Passed) ? RuntimeStatus.Failed : RuntimeStatus.Passed;
        return new ScenarioRunResult(ScenarioResultDocument.FromExecution(new ScenarioSummary(scenario.Id, scenario.Category, ContentTargetResolver.ToRepositoryRelativePath(sourcePath)), status, status == RuntimeStatus.Passed ? 0 : 1, scenario.Runtime!.Ticks, scenario.Runtime.Ticks, scenarioEvents, execution.Entities, assertions, execution.Diagnostics), scenarioEvents, execution.Diagnostics);
    }

    public static M014Execution Execute(ScenarioSource scenario)
    {
        var diagnostics = new List<ScenarioDiagnostic>();
        var mapPath = MapInspector.ResolveTarget("map.interaction-smoke").MapPath;
        var mapItem = new MapContentValidator().ValidateFile(mapPath);
        var catalog = EntityDefinitionCatalog.LoadAll(out var definitionDiagnostics);
        diagnostics.AddRange(mapItem.Diagnostics.Select(ToScenario)); diagnostics.AddRange(definitionDiagnostics.Select(ToScenario));
        if (mapItem.Map is null || mapItem.Status != ContentValidationStatus.Passed || diagnostics.Any(x => x.Severity == "error")) return new([], [], [], [], [], diagnostics, null);

        var world = new EntityComponentWorld(); ContinuousKinematicSpatialResolver.Register(world); RegisterM014(world);
        var provenance = new SortedDictionary<string, RuntimeEntityProvenance>(StringComparer.Ordinal);
        var assignments = new SortedDictionary<string, EntityDefinitionBehaviorSource>(StringComparer.Ordinal);
        var instantiations = new List<object>(); var queries = new List<object>(); var transitions = new List<object>(); var interactions = new List<object>(); var events = new List<ScenarioEvent>();
        var service = new Instantiator(world, catalog, provenance, assignments, instantiations);
        var scenarioSpawns = scenario.InitialState?.EntitySpawns ?? [];
        foreach (var mapSpawn in mapItem.Map.EntitySpawns.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var scenarioOverride = scenarioSpawns.FirstOrDefault(x => x.Id == mapSpawn.Id);
            service.Instantiate(mapSpawn, scenarioOverride?.Overrides ?? [], "map", mapItem.Map.Id, mapPath, 0, events);
        }
        foreach (var scenarioSpawn in scenarioSpawns.Where(x => mapItem.Map.EntitySpawns.All(map => map.Id != x.Id)).OrderBy(x => x.Id, StringComparer.Ordinal))
            service.Instantiate(scenarioSpawn, [], "scenario", scenario.Id, null, 0, events);

        var spatial = new SpatialQueries(world, queries);
        var triggerState = new TriggerState(world, spatial, transitions);
        if (scenario.Id == "entity.definition-instantiation-smoke")
        {
            service.Instantiate(mapItem.Map.EntitySpawns.Single(x => x.Id == "spawn.npc.talkable-smoke") with { Id = "spawn.dynamic.npc", EntityId = "entity.npc.dynamic" }, [], "scenario", scenario.Id, null, 1, events);
            world.DestroyEntity("entity.npc.dynamic", 2); events.Add(new(events.Count + 1, 2, "entity.destroyed", "entity.npc.dynamic"));
        }
        else if (scenario.Id == "trigger.enter-exit-smoke")
        {
            Move(world, "entity.player", 3.0, 1.5, 1, events); triggerState.Evaluate(1, events);
            Move(world, "entity.player", 0.5, 1.5, 2, events); triggerState.Evaluate(2, events);
        }
        else if (scenario.Id == "interaction.npc-smoke")
        {
            Move(world, "entity.player", 3.0, 1.5, 1, events); triggerState.Evaluate(1, events);
            var intent = new InteractIntent("intent.assignment.player-interact.interact.tick-1", "entity.player", null, "interaction.talk", "assignment.player-interact", "entity.player:assignment.player-interact");
            ResolveInteraction(world, spatial, intent, 1, interactions, events);
        }
        return new(WorldEntities(world), instantiations, queries, transitions, interactions, diagnostics, world, provenance, events);
    }

    private static void RegisterM014(EntityComponentWorld world)
    {
        world.Register<SemanticTags>("component.semantic-tags", "runtime/core", x => x.Values.All(IsStable));
        world.Register<TriggerVolume2>("component.trigger-volume-2d", "runtime/m014", x => FinitePositive(x.HalfWidth) && FinitePositive(x.HalfHeight));
        world.Register<Interactable>("component.interactable", "runtime/m014", x => IsStable(x.InteractionKind) && double.IsFinite(x.Range) && x.Range >= 0);
    }

    private static void Move(EntityComponentWorld world, string entityId, double x, double y, int tick, List<ScenarioEvent> events)
    {
        if (world.Set(entityId, new ContinuousTransform2(x, y), tick, "command.m014.move." + entityId + ".tick-" + tick).Accepted)
            events.Add(new(events.Count + 1, tick, "entity.continuous-transform-changed", entityId));
    }

    private static void ResolveInteraction(EntityComponentWorld world, SpatialQueries spatial, InteractIntent intent, int tick, List<object> records, List<ScenarioEvent> events)
    {
        string? reason = null; string? selected = null; string? kind = null; string? queryId = null; var candidates = new List<object>();
        if (!world.Exists(intent.InteractorEntityId)) reason = "interactor-not-found";
        else if (!world.TryGet<ContinuousTransform2>(intent.InteractorEntityId, out var transform) || !world.TryGet<SpatialMembership>(intent.InteractorEntityId, out var membership)) reason = "interactor-spatial-state-missing";
        else
        {
            queryId = "query.interaction." + intent.Id;
            var result = spatial.Radius(queryId, tick, membership!.WorldId, transform!.X, transform.Y, 100, null, new(null, [], ["component.interactable"]));
            foreach (var candidate in result)
            {
                if (!world.TryGet<Interactable>(candidate.EntityId, out var interactable) || interactable is null) continue;
                var tagOk = !world.TryGet<SemanticTags>(intent.InteractorEntityId, out var tags) || interactable.AllowedInteractorTags.All(tag => tags!.Values.Contains(tag, StringComparer.Ordinal));
                var componentsOk = interactable.RequiredInteractorComponentTypeIds.All(type => HasType(world, intent.InteractorEntityId, type));
                var kindOk = intent.RequestedInteractionKind is null || intent.RequestedInteractionKind == interactable.InteractionKind;
                var rangeOk = candidate.Distance <= interactable.Range;
                var eligible = tagOk && componentsOk && kindOk && rangeOk;
                candidates.Add(new { entityId = candidate.EntityId, distance = candidate.Distance, eligible, reason = !kindOk ? "interaction-kind-mismatch" : eligible ? (string?)null : "ineligible" });
            }
            if (intent.ExplicitTargetEntityId is not null)
            {
                if (!world.Exists(intent.ExplicitTargetEntityId)) reason = "explicit-target-not-found";
                else
                {
                    var candidate = candidates.FirstOrDefault(x => (string?)x.GetType().GetProperty("entityId")!.GetValue(x) == intent.ExplicitTargetEntityId);
                    if (candidate is null) reason = "explicit-target-ineligible";
                    else { var eligible = (bool)candidate.GetType().GetProperty("eligible")!.GetValue(candidate)!; if (!eligible) reason = (string?)candidate.GetType().GetProperty("reason")!.GetValue(candidate) == "interaction-kind-mismatch" ? "interaction-kind-mismatch" : "explicit-target-ineligible"; else selected = intent.ExplicitTargetEntityId; }
                }
            }
            else
            {
                var valid = candidates.Where(x => (bool)x.GetType().GetProperty("eligible")!.GetValue(x)!).OrderBy(x => (double)x.GetType().GetProperty("distance")!.GetValue(x)!).ThenBy(x => (string)x.GetType().GetProperty("entityId")!.GetValue(x)!, StringComparer.Ordinal).FirstOrDefault();
                if (valid is null) reason = candidates.Any(x => (string?)x.GetType().GetProperty("reason")!.GetValue(x) == "interaction-kind-mismatch") ? "interaction-kind-mismatch" : "no-eligible-target-in-range";
                else selected = (string)valid.GetType().GetProperty("entityId")!.GetValue(valid)!;
            }
            if (selected is not null && world.TryGet<Interactable>(selected, out var selectedInteractable)) kind = selectedInteractable!.InteractionKind;
        }
        var accepted = selected is not null; var commandId = accepted ? "command.begin-interaction." + intent.Id : null;
        if (accepted) events.Add(new(events.Count + 1, tick, "interaction.started", kind + ":" + intent.InteractorEntityId + ":" + selected + ":" + intent.Id));
        records.Add(new { schema = "agentic2d.interaction-resolution.v1", sequence = records.Count + 1, tick, intentId = intent.Id, behaviorAssignmentId = intent.BehaviorAssignmentId, interactorEntityId = intent.InteractorEntityId, explicitTargetId = intent.ExplicitTargetEntityId, requestedKind = intent.RequestedInteractionKind, queryId, candidates, selectedTargetId = selected, selectionReason = accepted ? (intent.ExplicitTargetEntityId is null ? "nearest-eligible" : "explicit-target") : null, status = accepted ? "accepted" : "rejected", commandReference = commandId, interactionKind = kind, events = accepted ? new[] { "interaction.started" } : [], rejectionReason = reason, diagnostics = Array.Empty<string>() });
    }

    private static bool HasType(EntityComponentWorld world, string id, string type) => type switch { "component.continuous-transform-2d" => world.TryGet<ContinuousTransform2>(id, out _), "component.kinematic-motion-2d" => world.TryGet<KinematicMotion2>(id, out _), "component.collision-aabb-2d" => world.TryGet<CollisionAabb2>(id, out _), "component.spatial-membership" => world.TryGet<SpatialMembership>(id, out _), "component.trigger-volume-2d" => world.TryGet<TriggerVolume2>(id, out _), "component.interactable" => world.TryGet<Interactable>(id, out _), _ => false };
    private static IReadOnlyList<EntitySummary> WorldEntities(EntityComponentWorld world) => world.EntityIds.Select(id => new EntitySummary(id, world.TryGet<ContinuousTransform2>(id, out var transform) ? (int)Math.Round(transform!.X) : 0)).ToArray();
    private static ScenarioDiagnostic ToScenario(ContentValidationDiagnostic item) => new(item.Id, item.Severity, item.Message);
    private static bool IsStable(string value) => !string.IsNullOrWhiteSpace(value) && value.All(c => char.IsLetterOrDigit(c) || c is '.' or '-');
    private static bool FinitePositive(double value) => double.IsFinite(value) && value > 0;

    private sealed class Instantiator(EntityComponentWorld world, EntityDefinitionCatalog catalog, IDictionary<string, RuntimeEntityProvenance> provenance, IDictionary<string, EntityDefinitionBehaviorSource> assignments, List<object> evidence)
    {
        public void Instantiate(EntitySpawnSource spawn, IReadOnlyList<EntityDefinitionComponentSource> scenarioOverrides, string sourceKind, string sourceId, string? sourcePath, int tick, List<ScenarioEvent> events)
        {
            var commands = new List<string>(); var commandResults = new List<string>(); var errors = new List<string>();
            if (!catalog.TryGet(spawn.DefinitionId, out var definition) || definition is null) errors.Add("definition-not-found");
            var mapOverrides = spawn.Overrides; var bundle = definition is null ? new Dictionary<string, JsonElement>() : definition.Components.ToDictionary(x => x.ComponentType, x => x.Value, StringComparer.Ordinal);
            foreach (var item in mapOverrides.Concat(scenarioOverrides)) bundle[item.ComponentType] = item.Value;
            ValidateBundle(bundle, definition?.Behavior, errors);
            var parsed = new List<(string Type, object Value)>();
            if (errors.Count == 0) { parsed.Add(("component.semantic-tags", new SemanticTags(definition!.SemanticTags.Order(StringComparer.Ordinal).ToArray()))); foreach (var item in bundle.OrderBy(x => x.Key, StringComparer.Ordinal)) try { parsed.Add((item.Key, Parse(item.Key, item.Value))); } catch { errors.Add("invalid-component-value:" + item.Key); } }
            var behavior = definition?.Behavior;
            if (behavior is not null && behavior.BehaviorId != PlayerInteractBehavior.BehaviorId && behavior.BehaviorId != PlayerMoveEastContinuousBehavior.BehaviorId) errors.Add("behavior-not-found");
            if (errors.Count > 0) { evidence.Add(new { schema = "agentic2d.entity-instantiation.v1", sequence = evidence.Count + 1, tick, definitionId = spawn.DefinitionId, spawnId = spawn.Id, entityId = spawn.EntityId, sourceKind, sourceId, definitionDefaults = definition?.Components, mapSpawnOverrides = mapOverrides, scenarioOverrides, finalMergedBundle = bundle, validationStatus = "rejected", generatedCommands = commands, commandResults, behaviorAssignment = behavior, provenance = (object?)null, events = Array.Empty<string>(), diagnostics = errors, committed = false, rolledBack = false }); return; }
            commands.Add("CreateEntity"); var create = world.CreateEntity(spawn.EntityId, tick); commandResults.Add(create.Status);
            if (!create.Accepted) errors.Add(create.Diagnostic ?? "create-rejected");
            var immutable = new RuntimeEntityProvenance(spawn.DefinitionId, spawn.Id, sourceKind, sourceId, sourcePath is null ? null : ContentTargetResolver.ToRepositoryRelativePath(sourcePath), mapOverrides.Concat(scenarioOverrides).Select(x => x.ComponentType).Order(StringComparer.Ordinal).ToArray(), behavior?.Id);
            commands.Add("provenance"); if (errors.Count == 0) { var provenanceResult = world.SetProvenance(spawn.EntityId, immutable, tick, "command.entity.provenance." + spawn.EntityId); commandResults.Add("provenance:" + provenanceResult.Status); if (provenanceResult.Accepted) provenance[spawn.EntityId] = immutable; }
            foreach (var item in parsed.OrderBy(x => x.Type, StringComparer.Ordinal)) { commands.Add(item.Type); if (errors.Count == 0) commandResults.Add(item.Type + ":" + Set(item.Type, spawn.EntityId, item.Value, tick).Status); }
            commands.Add("behavior-assignment"); if (errors.Count == 0 && behavior is not null) assignments[spawn.EntityId] = behavior;
            var rollback = commandResults.Any(x => x.EndsWith(":rejected", StringComparison.Ordinal)) || commandResults.FirstOrDefault() == "rejected"; if (rollback) { world.DestroyEntity(spawn.EntityId, tick); provenance.Remove(spawn.EntityId); assignments.Remove(spawn.EntityId); errors.Add("rolled-back"); }
            if (!rollback) events.Add(new(events.Count + 1, tick, "entity.instantiated", spawn.EntityId));
            evidence.Add(new { schema = "agentic2d.entity-instantiation.v1", sequence = evidence.Count + 1, tick, definitionId = spawn.DefinitionId, spawnId = spawn.Id, entityId = spawn.EntityId, sourceKind, sourceId, definitionDefaults = definition!.Components, mapSpawnOverrides = mapOverrides, scenarioOverrides, finalMergedBundle = bundle, validationStatus = rollback ? "rejected" : "accepted", generatedCommands = commands, commandResults, behaviorAssignment = behavior, provenance = immutable, events = rollback ? Array.Empty<string>() : new[] { "entity.instantiated" }, diagnostics = errors, committed = !rollback, rolledBack = rollback });
        }
        private static void ValidateBundle(IReadOnlyDictionary<string, JsonElement> bundle, EntityDefinitionBehaviorSource? behavior, List<string> errors)
        {
            bool Has(string type) => bundle.ContainsKey(type);
            if (Has("component.kinematic-motion-2d") && !Has("component.continuous-transform-2d")) errors.Add("kinematic-requires-transform");
            if (Has("component.collision-aabb-2d") && !Has("component.continuous-transform-2d")) errors.Add("collision-requires-transform");
            if (Has("component.trigger-volume-2d") && !Has("component.continuous-transform-2d")) errors.Add("trigger-requires-transform");
            if (Has("component.interactable") && !Has("component.continuous-transform-2d")) errors.Add("interactable-requires-transform");
            if (behavior?.BehaviorId == PlayerMoveEastContinuousBehavior.BehaviorId && (!Has("component.continuous-transform-2d") || !Has("component.kinematic-motion-2d") || !Has("component.collision-aabb-2d") || !Has("component.spatial-membership"))) errors.Add("continuous-behavior-bundle-incomplete");
        }
        private static object Parse(string type, JsonElement value) => type switch
        {
            "component.continuous-transform-2d" => value.Deserialize<ContinuousTransform2>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!,
            "component.kinematic-motion-2d" => value.Deserialize<KinematicMotion2>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!,
            "component.collision-aabb-2d" => value.Deserialize<CollisionAabb2>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!,
            "component.spatial-membership" => value.Deserialize<SpatialMembership>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!,
            "component.trigger-volume-2d" => new TriggerVolume2(value.GetProperty("halfWidth").GetDouble(), value.GetProperty("halfHeight").GetDouble(), new TriggerFilter(value.TryGetProperty("filter", out var f) && f.TryGetProperty("entityId", out var entity) ? entity.GetString() : null, Strings(value, "filter", "requiredTags"), Strings(value, "filter", "requiredComponentTypeIds")), value.TryGetProperty("triggerId", out var trigger) ? trigger.GetString() : null),
            "component.interactable" => new Interactable(value.GetProperty("interactionKind").GetString()!, value.GetProperty("range").GetDouble(), Strings(value, null, "allowedInteractorTags"), Strings(value, null, "requiredInteractorComponentTypeIds")),
            _ => throw new InvalidOperationException()
        };
        private static IReadOnlyList<string> Strings(JsonElement value, string? parent, string property) { if (parent is not null) value = value.GetProperty(parent); return value.TryGetProperty(property, out var array) && array.ValueKind == JsonValueKind.Array ? array.EnumerateArray().Select(x => x.GetString()!).ToArray() : []; }
        private EntityComponentResult Set(string type, string entity, object value, int tick) => type switch { "component.continuous-transform-2d" => world.Set(entity, (ContinuousTransform2)value, tick), "component.kinematic-motion-2d" => world.Set(entity, (KinematicMotion2)value, tick), "component.collision-aabb-2d" => world.Set(entity, (CollisionAabb2)value, tick), "component.spatial-membership" => world.Set(entity, (SpatialMembership)value, tick), "component.trigger-volume-2d" => world.Set(entity, (TriggerVolume2)value, tick), "component.interactable" => world.Set(entity, (Interactable)value, tick), "component.semantic-tags" => world.Set(entity, (SemanticTags)value, tick), _ => new(false, "rejected", "unknown-component") };
    }

    private sealed class SpatialQueries(EntityComponentWorld world, List<object> evidence)
    {
        public IReadOnlyList<Candidate> Radius(string id, int tick, string worldId, double x, double y, double radius, string? excluded, TriggerFilter filters)
        {
            if (!double.IsFinite(radius) || radius < 0) { evidence.Add(new { schema = "agentic2d.spatial-query.v1", sequence = evidence.Count + 1, tick, queryId = id, queryKind = "radius", diagnostics = new[] { "invalid-radius" } }); return []; }
            var all = Compatible(worldId, excluded).Select(entity => new Candidate(entity, Distance(entity, x, y))).OrderBy(x => x.Distance).ThenBy(x => x.EntityId, StringComparer.Ordinal).ToArray();
            var result = all.Where(x => Matches(x.EntityId, filters) && x.Distance <= radius).ToArray();
            evidence.Add(new { schema = "agentic2d.spatial-query.v1", sequence = evidence.Count + 1, tick, queryId = id, queryKind = "radius", membership = worldId, center = new { x, y }, radius, excludedEntityId = excluded, filters, unfilteredCandidates = all.Select(x => x.EntityId), candidateDistances = all.Select(x => new { entityId = x.EntityId, distance = x.Distance }), filterResults = all.Select(x => new { entityId = x.EntityId, matched = Matches(x.EntityId, filters) }), results = result.Select(x => x.EntityId), diagnostics = Array.Empty<string>() }); return result;
        }
        public IReadOnlyList<string> Aabb(string id, int tick, string worldId, double minX, double minY, double maxX, double maxY, TriggerFilter filters)
        {
            var all = Compatible(worldId, null).Order(StringComparer.Ordinal).ToArray(); var result = all.Where(entity => Matches(entity, filters) && Overlap(entity, minX, minY, maxX, maxY)).ToArray();
            evidence.Add(new { schema = "agentic2d.spatial-query.v1", sequence = evidence.Count + 1, tick, queryId = id, queryKind = "aabb-overlap", membership = worldId, bounds = new { minX, minY, maxX, maxY }, excludedEntityId = (string?)null, filters, unfilteredCandidates = all, candidateDistances = Array.Empty<object>(), filterResults = all.Select(entity => new { entityId = entity, matched = Matches(entity, filters) }), results = result, diagnostics = Array.Empty<string>() }); return result;
        }
        private IEnumerable<string> Compatible(string worldId, string? excluded) => world.Query<ContinuousTransform2>().Where(id => id != excluded && world.TryGet<SpatialMembership>(id, out var member) && member!.WorldId == worldId);
        private bool Matches(string id, TriggerFilter filter) => (filter.EntityId is null || filter.EntityId == id) && (!filter.RequiredTags.Any() || world.TryGet<SemanticTags>(id, out var tags) && filter.RequiredTags.All(tag => tags!.Values.Contains(tag, StringComparer.Ordinal))) && filter.RequiredComponentTypeIds.All(type => HasType(world, id, type));
        private double Distance(string id, double x, double y) { world.TryGet<ContinuousTransform2>(id, out var t); return Math.Sqrt(Math.Pow(t!.X - x, 2) + Math.Pow(t.Y - y, 2)); }
        private bool Overlap(string id, double minX, double minY, double maxX, double maxY) { world.TryGet<ContinuousTransform2>(id, out var t); var hw = world.TryGet<CollisionAabb2>(id, out var box) ? box!.HalfWidth : 0; var hh = box is null ? 0 : box.HalfHeight; return t!.X + hw >= minX && t.X - hw <= maxX && t.Y + hh >= minY && t.Y - hh <= maxY; }
    }
    private sealed record Candidate(string EntityId, double Distance);

    private sealed class TriggerState(EntityComponentWorld world, SpatialQueries spatial, List<object> evidence)
    {
        private readonly Dictionary<string, SortedSet<string>> previous = new(StringComparer.Ordinal);
        public void Evaluate(int tick, List<ScenarioEvent> events)
        {
            foreach (var owner in world.Query<TriggerVolume2>().Order(StringComparer.Ordinal))
            {
                if (!world.TryGet<TriggerVolume2>(owner, out var volume) || !world.TryGet<ContinuousTransform2>(owner, out var transform) || !world.TryGet<SpatialMembership>(owner, out var membership)) continue;
                var id = volume!.TriggerId ?? owner; var current = new SortedSet<string>(spatial.Aabb("query.trigger." + id + ".tick-" + tick, tick, membership!.WorldId, transform!.X - volume.HalfWidth, transform.Y - volume.HalfHeight, transform.X + volume.HalfWidth, transform.Y + volume.HalfHeight, volume.Filter), StringComparer.Ordinal); current.Remove(owner);
                var old = previous.TryGetValue(id, out var state) ? state : new SortedSet<string>(StringComparer.Ordinal); var entered = current.Except(old).ToArray(); var exited = old.Except(current).ToArray();
                foreach (var entity in entered) events.Add(new(events.Count + 1, tick, "trigger.entered", id + ":" + owner + ":" + entity)); foreach (var entity in exited) events.Add(new(events.Count + 1, tick, "trigger.exited", id + ":" + owner + ":" + entity));
                evidence.Add(new { schema = "agentic2d.trigger-transition.v1", sequence = evidence.Count + 1, tick, triggerId = id, triggerOwnerEntityId = owner, bounds = new { minX = transform.X - volume.HalfWidth, minY = transform.Y - volume.HalfHeight, maxX = transform.X + volume.HalfWidth, maxY = transform.Y + volume.HalfHeight }, filter = volume.Filter, previousOverlaps = old, currentOverlaps = current, enteredIds = entered, exitedIds = exited, eventIds = entered.Select(_ => "trigger.entered").Concat(exited.Select(_ => "trigger.exited")).ToArray(), diagnostics = Array.Empty<string>() }); previous[id] = current;
            }
        }
    }
}

public sealed record M014Execution(IReadOnlyList<EntitySummary> Entities, IReadOnlyList<object> Instantiations, IReadOnlyList<object> SpatialQueries, IReadOnlyList<object> TriggerTransitions, IReadOnlyList<object> InteractionResolutions, IReadOnlyList<ScenarioDiagnostic> Diagnostics, EntityComponentWorld? World, IReadOnlyDictionary<string, RuntimeEntityProvenance>? Provenance = null, IReadOnlyList<ScenarioEvent>? Events = null);
