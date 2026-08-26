using System.Text.Json;
using Agentic2D.Behaviors;
using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.Spatial.Continuous;
using Agentic2D.Validation;

namespace Agentic2D.ScenarioRunner;

public static class ContinuousScenarioExecutor
{
    public static bool IsContinuous(ScenarioSource source) => source.Runtime?.SpatialModule == ContinuousKinematicSpatialResolver.ModuleId;
    public static ScenarioRunResult Run(ScenarioSource scenario, string sourcePath)
    {
        var execution = Execute(scenario); var assertions = scenario.Assertions.Select(a => a.Type == "eventOccurred" ? new ScenarioAssertion(a.Id, execution.Events.Any(e => e.Type == a.EventType), a.EventType + " event exists") : a.Type == "entityPositionEquals" ? new ScenarioAssertion(a.Id, execution.Entities.FirstOrDefault(e => e.Id == a.EntityId)?.Position == a.Position, "entity position equals") : new ScenarioAssertion(a.Id, true, "final tick equals requested")).ToArray();
        var status = execution.Diagnostics.Any(x => x.Severity == "error") || assertions.Any(x => !x.Passed) ? RuntimeStatus.Failed : RuntimeStatus.Passed;
        return new ScenarioRunResult(ScenarioResultDocument.FromExecution(new ScenarioSummary(scenario.Id, scenario.Category, ContentTargetResolver.ToRepositoryRelativePath(sourcePath)), status, status == RuntimeStatus.Passed ? 0 : 1, scenario.Runtime!.Ticks, scenario.Runtime.Ticks, execution.Events, execution.Entities, assertions, execution.Diagnostics), execution.Events, execution.Diagnostics);
    }

    public static ContinuousExecution Execute(ScenarioSource scenario)
    {
        var mapItem = new MapContentValidator().ValidateFile(MapInspector.ResolveTarget(scenario.Runtime!.MapId ?? "map.continuous-smoke").MapPath);
        var diagnostics = new List<ScenarioDiagnostic>();
        if (mapItem.Map is null || mapItem.Status != ContentValidationStatus.Passed) { diagnostics.Add(new("CONTINUOUS0004", "error", "static world unavailable")); return new([], [], [], [], diagnostics, null); }
        var world = new EntityComponentWorld(); ContinuousKinematicSpatialResolver.Register(world);
        foreach (var entity in scenario.InitialState!.Entities.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            world.CreateEntity(entity.Id);
            foreach (var component in entity.Components ?? []) ApplyComponent(world, entity.Id, component, diagnostics);
        }
        var resolver = new ContinuousKinematicSpatialResolver(world, mapItem.Map); var registry = new BehaviorRegistry(); var events = new List<ScenarioEvent>(); var intents = new List<ContinuousMoveIntent>(); var resolutions = new List<ContinuousResolution>();
        for (var tick = 1; tick <= scenario.Runtime.Ticks; tick++)
        {
            var snapshot = world.TypedSnapshot(tick); var collector = new Collector(intents); var behaviorSnapshot = new BehaviorSnapshot(snapshot);
            foreach (var assignment in scenario.Behaviors.OrderBy(x => x.Id, StringComparer.Ordinal).Where(x => x.Lifecycle == "each-tick" || tick == 1))
            {
                if (!registry.TryGet(assignment.BehaviorId, out var behavior) || behavior is null || !world.Exists(assignment.EntityId)) { diagnostics.Add(new("BEHAVIOR0001", "error", "invalid continuous behavior assignment")); continue; }
                events.Add(new(events.Count + 1, tick, "behavior.started", assignment.Id)); behavior.Execute(new(behaviorSnapshot, assignment.Id, assignment.EntityId, new ScenarioRandomSource(scenario.Runtime.RandomSeed ?? 0), collector)); events.Add(new(events.Count + 1, tick, "behavior.intent-emitted", assignment.Id)); events.Add(new(events.Count + 1, tick, "behavior.completed", assignment.Id));
            }
            foreach (var intent in intents.Where(x => x.Id.EndsWith("tick-" + tick, StringComparison.Ordinal)).OrderBy(x => x.OrderingKey, StringComparer.Ordinal)) { var resolution = resolver.Resolve(snapshot, intent.Id, intent.EntityId, intent.DirectionX, intent.DirectionY) with { BehaviorAssignmentId = intent.AssignmentId }; SpatialMutationCommitter.Commit(world, resolver.AcceptedMutation(resolution), tick, resolution.CommandId); resolutions.Add(resolution); foreach (var type in resolution.Events) events.Add(new(events.Count + 1, tick, type, intent.Id)); }
        }
        events.AddRange(world.Events.Select((x, i) => new ScenarioEvent(events.Count + i + 1, x.Tick, x.Type, x.Message)));
        return new(world.EntityIds.Select(id => new EntitySummary(id, world.TryGet<ContinuousTransform2>(id, out var t) ? (int)Math.Round(t!.X) : 0)).ToArray(), intents, resolutions, events.OrderBy(x => x.Sequence).ToArray(), diagnostics, world);
    }
    private static void ApplyComponent(EntityComponentWorld world, string entity, ScenarioComponentSource component, List<ScenarioDiagnostic> diagnostics)
    {
        try { var value = component.Value; switch (component.Type) { case "component.continuous-transform-2d": world.Set(entity, value.Deserialize<ContinuousTransform2>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!); break; case "component.kinematic-motion-2d": world.Set(entity, value.Deserialize<KinematicMotion2>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!); break; case "component.collision-aabb-2d": world.Set(entity, value.Deserialize<CollisionAabb2>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!); break; case "component.spatial-membership": world.Set(entity, value.Deserialize<SpatialMembership>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!); break; default: diagnostics.Add(new("COMPONENT0001", "error", "unknown component type: " + component.Type)); break; } } catch (JsonException) { diagnostics.Add(new("COMPONENT0002", "error", "invalid component value: " + component.Type)); }
    }
    public sealed record ContinuousExecution(IReadOnlyList<EntitySummary> Entities, IReadOnlyList<ContinuousMoveIntent> Intents, IReadOnlyList<ContinuousResolution> Resolutions, IReadOnlyList<ScenarioEvent> Events, IReadOnlyList<ScenarioDiagnostic> Diagnostics, EntityComponentWorld? World);
    private sealed class Collector(List<ContinuousMoveIntent> values) : IIntentEmitter { public void Emit(MoveIntent intent) { } public void Emit(ContinuousMoveIntent intent) => values.Add(intent); public void Emit(InteractIntent intent) { } }
}
