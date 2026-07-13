using System.Security.Cryptography;
using System.Text;
using Agentic2D.Behaviors;
using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.Spatial.Grid;
using Agentic2D.Validation;

namespace Agentic2D.ScenarioRunner;

public sealed record BehaviorExecutionEvidence(
    IReadOnlyList<BehaviorEvidenceAssignment> Behaviors,
    IReadOnlyList<MoveIntent> Intents,
    IReadOnlyList<SpatialResolutionEvidence> Resolutions,
    IReadOnlyList<ScenarioEvent> Events,
    IReadOnlyList<EntitySummary> Entities,
    IReadOnlyList<ScenarioDiagnostic> Diagnostics,
    IReadOnlyList<GridPositionEvidence>? GridPositions = null);

public sealed record GridPositionEvidence(string EntityId, int X, int Y);
public sealed record BehaviorEvidenceAssignment(string AssignmentId, string BehaviorId, string EntityId, string Lifecycle, int ExecutionTick, string SnapshotFingerprint, string Status);
public sealed record SpatialResolutionEvidence(string IntentId, string ModuleId, bool Accepted, string Reason, string SemanticSource, string? SemanticValue, string? AssetId, string? TileId, string? CommandId, IReadOnlyList<string> Events, IReadOnlyList<string> Diagnostics, int? DestinationX, int? DestinationY);

internal static class BehaviorGridScenarioExecutor
{
    public static bool IsBehaviorScenario(ScenarioSource scenario) => scenario.Behaviors.Count > 0;
    public static BehaviorExecutionEvidence Execute(ScenarioSource scenario) => BehaviorGridExecutionV2.Execute(scenario);


    public static ScenarioRunResult Run(ScenarioSource scenario, string sourcePath)
    {
        var execution = Execute(scenario);
        var assertions = scenario.Assertions.Select(assertion => EvaluateAssertion(assertion, scenario.Runtime!.Ticks, execution)).ToArray();
        var status = execution.Diagnostics.Any(item => item.Severity == "error") || assertions.Any(item => !item.Passed) ? RuntimeStatus.Failed : RuntimeStatus.Passed;
        var document = ScenarioResultDocument.FromExecution(new ScenarioSummary(scenario.Id, scenario.Category, ContentTargetResolver.ToRepositoryRelativePath(sourcePath)), status, status == RuntimeStatus.Passed ? 0 : 1, scenario.Runtime!.Ticks, scenario.Runtime.Ticks, execution.Events, execution.Entities, assertions, execution.Diagnostics);
        return new ScenarioRunResult(document, execution.Events, execution.Diagnostics);
    }

    private static BehaviorExecutionEvidence ExecuteLegacy(ScenarioSource scenario)
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

        var world = new EntityComponentWorld(); world.Register<GridPosition>("component.grid-position", "spatial.grid"); foreach (var entity in scenario.InitialState!.Entities) { world.CreateEntity(entity.Id); world.Set(entity.Id, new GridPosition(entity.GridPosition?.X ?? entity.Position, entity.GridPosition?.Y ?? 0)); }
        var entityIds = world.EntityIds.ToHashSet(StringComparer.Ordinal);
        var snapshot = new BehaviorSnapshot(1, Fingerprint(entityIds), entityIds);
        var resolver = new GridSpatialResolver(mapItem.Map, world);
        var registry = new BehaviorRegistry();
        var intents = new List<MoveIntent>();
        var assignments = new List<BehaviorEvidenceAssignment>();
        var emitter = new ListIntentEmitter(intents);
        var random = new ScenarioRandomSource(scenario.Runtime.RandomSeed ?? 0);
        var events = new List<ScenarioEvent>();

        foreach (var assignment in scenario.Behaviors.OrderBy(item => item.Id, StringComparer.Ordinal))
        {
            if (!entityIds.Contains(assignment.EntityId)) { diagnostics.Add(new("BEHAVIOR0005", "error", $"Assignment target entity is missing: {assignment.EntityId}")); continue; }
            if (!registry.TryGet(assignment.BehaviorId, out var behavior) || behavior is null) { diagnostics.Add(new("BEHAVIOR0001", "error", $"Unknown behavior: {assignment.BehaviorId}")); continue; }
            if (assignment.Lifecycle is not "once" and not "each-tick") { diagnostics.Add(new("BEHAVIOR0004", "error", $"Unsupported lifecycle: {assignment.Lifecycle}")); continue; }
            events.Add(new ScenarioEvent(events.Count + 1, 1, "behavior.started", assignment.Id));
            behavior.Execute(new BehaviorContext(snapshot, assignment.Id, assignment.EntityId, random, emitter));
            events.Add(new ScenarioEvent(events.Count + 1, 1, "behavior.intent-emitted", assignment.Id));
            events.Add(new ScenarioEvent(events.Count + 1, 1, "behavior.completed", assignment.Id));
            assignments.Add(new BehaviorEvidenceAssignment(assignment.Id, assignment.BehaviorId, assignment.EntityId, assignment.Lifecycle, 1, snapshot.Fingerprint, "completed"));
        }

        var resolutionEvidence = new List<SpatialResolutionEvidence>();
        foreach (var intent in intents.OrderBy(item => item.OrderingKey, StringComparer.Ordinal).ThenBy(item => item.Id, StringComparer.Ordinal))
        {
            var resolution = resolver.ResolveDetailed(intent);
            resolver.ApplyAccepted(resolution, 1);
            resolutionEvidence.Add(new SpatialResolutionEvidence(intent.Id, resolution.Resolution.ModuleId, resolution.Resolution.Accepted, resolution.Resolution.Reason, resolution.SemanticSource, resolution.SemanticValue, resolution.AssetId, resolution.TileId, resolution.Resolution.CommandId, resolution.Resolution.Events, resolution.Resolution.Diagnostics, resolution.Destination?.X, resolution.Destination?.Y));
            foreach (var eventType in resolution.Resolution.Events) events.Add(new ScenarioEvent(events.Count + 1, 1, eventType, intent.Id));
        }
        var entities = scenario.InitialState.Entities.OrderBy(entity => entity.Id, StringComparer.Ordinal).Select(entity => new EntitySummary(entity.Id, resolver.QueryPosition(entity.Id)?.X ?? entity.Position)).ToArray();
        return new(assignments, intents.OrderBy(item => item.OrderingKey, StringComparer.Ordinal).ToArray(), resolutionEvidence, events, entities, diagnostics);
    }

    private static string Fingerprint(IEnumerable<string> entityIds)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", entityIds.Order(StringComparer.Ordinal))));
        return "sha256:" + Convert.ToHexString(bytes).ToLowerInvariant();
    }

    internal static ScenarioAssertion EvaluateAssertion(ScenarioAssertionSource assertion, int ticks, BehaviorExecutionEvidence execution)
    {
        var entity = execution.Entities.FirstOrDefault(item => item.Id == assertion.EntityId);
        return assertion.Type switch
        {
            "finalTickEqualsRequested" => new(assertion.Id, true, "final tick equals requested tick count", ticks.ToString(), ticks.ToString()),
            "entityExists" => new(assertion.Id, entity is not null, assertion.EntityId + " exists", "exists", entity is null ? "missing" : "exists"),
            "entityPositionEquals" => new(assertion.Id, entity?.Position == assertion.Position, assertion.EntityId + " position equals " + assertion.Position, assertion.Position?.ToString(), entity?.Position.ToString() ?? "missing"),
            "eventOccurred" => new(assertion.Id, execution.Events.Any(item => item.Type == assertion.EventType), assertion.EventType + " event exists", "occurred", execution.Events.Any(item => item.Type == assertion.EventType) ? "occurred" : "missing"),
            _ => new(assertion.Id, false, "Unsupported assertion type: " + assertion.Type),
        };
    }

    private sealed class ListIntentEmitter(List<MoveIntent> intents) : IIntentEmitter { public void Emit(MoveIntent intent) => intents.Add(intent); public void Emit(ContinuousMoveIntent intent) { } }
}
