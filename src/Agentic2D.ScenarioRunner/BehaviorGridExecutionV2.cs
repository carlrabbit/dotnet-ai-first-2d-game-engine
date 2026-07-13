using System.Security.Cryptography;
using System.Text;
using Agentic2D.Behaviors;
using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.Spatial.Grid;
using Agentic2D.Validation;

namespace Agentic2D.ScenarioRunner;

internal static class BehaviorGridExecutionV2
{
    public static BehaviorExecutionEvidence Execute(ScenarioSource scenario)
    {
        var diagnostics = new List<ScenarioDiagnostic>();
        if (!StringComparer.Ordinal.Equals(scenario.Runtime?.SpatialModule, GridSpatialResolver.ModuleId))
        {
            diagnostics.Add(new ScenarioDiagnostic("BEHAVIOR0002", "error", "Behavior scenario requires runtime.spatialModule spatial.grid."));
            return new([], [], [], [], [], diagnostics);
        }

        var mapResolution = MapInspector.ResolveTarget(scenario.Runtime.MapId ?? MapContentValidator.SmokeMapId);
        var mapItem = mapResolution.IsSuccess ? new MapContentValidator().ValidateFile(mapResolution.MapPath) : null;
        if (mapItem?.Map is null || mapItem.Status != ContentValidationStatus.Passed)
        {
            diagnostics.Add(new ScenarioDiagnostic("GRID0003", "error", "Behavior scenario map could not be resolved and validated."));
            return new([], [], [], [], [], diagnostics);
        }

        var world = new EntityComponentWorld();
        world.Register<GridPosition>("component.grid-position", "spatial.grid");
        foreach (var entity in scenario.InitialState!.Entities) { world.CreateEntity(entity.Id); world.Set(entity.Id, new GridPosition(entity.GridPosition?.X ?? entity.Position, entity.GridPosition?.Y ?? 0)); }
        var entityIds = world.EntityIds.ToHashSet(StringComparer.Ordinal);
        var resolver = new GridSpatialResolver(mapItem.Map, world);
        var registry = new BehaviorRegistry();
        var random = new ScenarioRandomSource(scenario.Runtime.RandomSeed ?? 0);
        var assignments = new List<BehaviorEvidenceAssignment>();
        var allIntents = new List<MoveIntent>();
        var resolutions = new List<SpatialResolutionEvidence>();
        var events = new List<ScenarioEvent>();

        for (var tick = 1; tick <= scenario.Runtime.Ticks; tick++)
        {
            var snapshot = new BehaviorSnapshot(tick, Fingerprint(tick, entityIds, resolver), entityIds);
            var phaseIntents = new List<MoveIntent>();
            var emitter = new IntentCollector(phaseIntents);
            foreach (var assignment in scenario.Behaviors.OrderBy(item => item.Id, StringComparer.Ordinal))
            {
                if (!IsScheduled(assignment.Lifecycle, tick))
                {
                    continue;
                }

                if (!ValidateAssignment(assignment, entityIds, registry, diagnostics))
                {
                    continue;
                }

                events.Add(new ScenarioEvent(events.Count + 1, tick, "behavior.started", assignment.Id));
                registry.TryGet(assignment.BehaviorId, out var behavior);
                behavior!.Execute(new BehaviorContext(snapshot, assignment.Id, assignment.EntityId, random, emitter));
                events.Add(new ScenarioEvent(events.Count + 1, tick, "behavior.intent-emitted", assignment.Id));
                events.Add(new ScenarioEvent(events.Count + 1, tick, "behavior.completed", assignment.Id));
                assignments.Add(new BehaviorEvidenceAssignment(assignment.Id, assignment.BehaviorId, assignment.EntityId, assignment.Lifecycle, tick, snapshot.Fingerprint, "completed"));
            }

            foreach (var intent in phaseIntents.OrderBy(item => item.OrderingKey, StringComparer.Ordinal).ThenBy(item => item.Id, StringComparer.Ordinal))
            {
                allIntents.Add(intent);
                var resolution = resolver.ResolveDetailed(intent);
                resolver.ApplyAccepted(resolution, tick);
                resolutions.Add(new SpatialResolutionEvidence(intent.Id, resolution.Resolution.ModuleId, resolution.Resolution.Accepted, resolution.Resolution.Reason, resolution.SemanticSource, resolution.SemanticValue, resolution.AssetId, resolution.TileId, resolution.Resolution.CommandId, resolution.Resolution.Events, resolution.Resolution.Diagnostics, resolution.Destination?.X, resolution.Destination?.Y));
                foreach (var eventType in resolution.Resolution.Events)
                {
                    events.Add(new ScenarioEvent(events.Count + 1, tick, eventType, intent.Id));
                }
            }
        }

        var entities = scenario.InitialState.Entities.OrderBy(entity => entity.Id, StringComparer.Ordinal)
            .Select(entity => new EntitySummary(entity.Id, resolver.QueryPosition(entity.Id)?.X ?? entity.Position)).ToArray();
        var gridPositions = scenario.InitialState.Entities.OrderBy(entity => entity.Id, StringComparer.Ordinal).Select(entity => new GridPositionEvidence(entity.Id, resolver.QueryPosition(entity.Id)!.X, resolver.QueryPosition(entity.Id)!.Y)).ToArray();
        return new(assignments, allIntents, resolutions, events, entities, diagnostics, gridPositions);
    }

    private static bool IsScheduled(string lifecycle, int tick) => lifecycle == "each-tick" || lifecycle == "once" && tick == 1;

    private static bool ValidateAssignment(ScenarioBehaviorAssignmentSource assignment, IReadOnlySet<string> entityIds, IBehaviorRegistry registry, List<ScenarioDiagnostic> diagnostics)
    {
        if (!entityIds.Contains(assignment.EntityId)) { diagnostics.Add(new ScenarioDiagnostic("BEHAVIOR0005", "error", "Assignment target entity is missing: " + assignment.EntityId)); return false; }
        if (!registry.TryGet(assignment.BehaviorId, out _)) { diagnostics.Add(new ScenarioDiagnostic("BEHAVIOR0001", "error", "Unknown behavior: " + assignment.BehaviorId)); return false; }
        if (assignment.Lifecycle is not "once" and not "each-tick") { diagnostics.Add(new ScenarioDiagnostic("BEHAVIOR0004", "error", "Unsupported lifecycle: " + assignment.Lifecycle)); return false; }
        return true;
    }

    private static string Fingerprint(int tick, IEnumerable<string> entityIds, GridSpatialResolver resolver)
    {
        var state = string.Join("|", entityIds.Order(StringComparer.Ordinal).Select(id => id + ":" + resolver.QueryPosition(id)?.X + "," + resolver.QueryPosition(id)?.Y));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(tick + "|" + state));
        return "sha256:" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed class IntentCollector(List<MoveIntent> intents) : IIntentEmitter
    {
        public void Emit(MoveIntent intent) => intents.Add(intent);
        public void Emit(ContinuousMoveIntent intent) { }
    }
}
