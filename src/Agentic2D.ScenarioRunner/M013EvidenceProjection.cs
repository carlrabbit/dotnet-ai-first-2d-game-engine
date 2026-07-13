using System.Text.Json;
using System.Text.Json.Nodes;
using Agentic2D.Engine;
using Agentic2D.Spatial.Continuous;

namespace Agentic2D.ScenarioRunner;

internal static class M013EvidenceProjection
{
    public static string ComponentMutationsJsonl(EntityComponentWorld? world)
    {
        if (world is null) return string.Empty;
        var records = world.Mutations.Where(mutation => mutation.ComponentTypeId is not null)
            .Select((mutation, index) => new ComponentMutationEvidence(
                "agentic2d.entity-component-mutation.v1", index + 1, mutation.Tick, mutation.CommandId,
                mutation.CommandId, mutation.EntityId, mutation.ComponentTypeId!, MutationKind(mutation), mutation.Status,
                Parse(mutation.PreviousValue), Parse(mutation.ResultingValue), mutation.Events, mutation.Diagnostics));
        return JsonLines(records);
    }

    public static string ContinuousResolutionsJsonl(ContinuousScenarioExecutor.ContinuousExecution? execution)
    {
        if (execution?.World is null) return string.Empty;
        var mutations = execution.World.Mutations.ToDictionary(mutation => mutation.CommandId, StringComparer.Ordinal);
        var records = execution.Resolutions.Select((resolution, index) =>
        {
            mutations.TryGetValue(resolution.CommandId ?? string.Empty, out var mutation);
            var eventIds = resolution.Events.Concat(mutation?.Events ?? []).Distinct(StringComparer.Ordinal).ToArray();
            var diagnosticIds = resolution.Diagnostics.Concat(mutation?.Diagnostics ?? []).Distinct(StringComparer.Ordinal).ToArray();
            return new ContinuousResolutionEvidence(
                "agentic2d.continuous-spatial-resolution.v1", index + 1, ResolveTick(resolution, mutation), resolution.IntentId,
                resolution.BehaviorAssignmentId, resolution.EntityId, ContinuousKinematicSpatialResolver.ModuleId,
                new VectorEvidence(resolution.RequestedX == 0 && resolution.RequestedY == 0 ? 0 : resolution.RequestedX == 0 ? 0 : resolution.RequestedX / Math.Max(Math.Abs(resolution.RequestedX), Math.Abs(resolution.RequestedY)), resolution.RequestedY == 0 ? 0 : resolution.RequestedY / Math.Max(Math.Abs(resolution.RequestedX), Math.Abs(resolution.RequestedY))),
                new VectorEvidence(resolution.RequestedX, resolution.RequestedY), resolution.InitialTransform is null ? null : new TransformEvidence(resolution.InitialTransform.X, resolution.InitialTransform.Y),
                resolution.CollisionShape is null ? null : new CollisionShapeEvidence("aabb", resolution.CollisionShape.HalfWidth, resolution.CollisionShape.HalfHeight),
                resolution.CollisionCandidateDetails.Select(candidate => new CollisionCandidateEvidence(candidate.SourceKind, candidate.SourceId, candidate.MapId, new BoundsEvidence(candidate.MinX, candidate.MinY, candidate.MaxX, candidate.MaxY))).ToArray(),
                new AxisEvidence(resolution.XAxis.Requested, resolution.XAxis.Applied, resolution.XAxis.Constrained, resolution.XAxis.ConstraintSourceId),
                new AxisEvidence(resolution.YAxis.Requested, resolution.YAxis.Applied, resolution.YAxis.Constrained, resolution.YAxis.ConstraintSourceId),
                resolution.Outcome, new VectorEvidence(resolution.AppliedX, resolution.AppliedY), new TransformEvidence(resolution.Result.X, resolution.Result.Y),
                resolution.CommandId, mutation is null ? null : mutation.CommandId, eventIds, diagnosticIds);
        });
        return JsonLines(records);
    }

    private static int ResolveTick(ContinuousResolution resolution, EntityComponentMutation? mutation) => mutation?.Tick ?? 0;
    private static string MutationKind(EntityComponentMutation mutation) => mutation.MutationKind switch { "add" => "component-added", "update" => "component-updated", "remove" => "component-removed", _ => mutation.MutationKind };
    private static JsonNode? Parse(string? value) => value is null ? null : JsonNode.Parse(value);
    private static string JsonLines<T>(IEnumerable<T> values)
    {
        var lines = values.Select(value => JsonSerializer.Serialize(value)).ToArray();
        return lines.Length == 0 ? string.Empty : string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }

    private sealed record ComponentMutationEvidence(string Schema, int Sequence, int Tick, string CommandId, string CommandInstanceId, string EntityId, string ComponentTypeId, string MutationKind, string Status, JsonNode? PreviousValue, JsonNode? ResultingValue, IReadOnlyList<string> EventIds, IReadOnlyList<string> DiagnosticIds);
    private sealed record ContinuousResolutionEvidence(string Schema, int Sequence, int Tick, string IntentId, string? BehaviorAssignmentId, string EntityId, string ModuleId, VectorEvidence RequestedDirection, VectorEvidence RequestedDisplacement, TransformEvidence? InitialTransform, CollisionShapeEvidence? CollisionShape, IReadOnlyList<CollisionCandidateEvidence> CollisionCandidates, AxisEvidence XAxis, AxisEvidence YAxis, string Outcome, VectorEvidence AppliedDisplacement, TransformEvidence ResultingTransform, string? MutationCommandId, string? MutationRecordCommandInstanceId, IReadOnlyList<string> EventIds, IReadOnlyList<string> DiagnosticIds);
    private sealed record VectorEvidence(double X, double Y);
    private sealed record TransformEvidence(double X, double Y);
    private sealed record CollisionShapeEvidence(string Kind, double HalfWidth, double HalfHeight);
    private sealed record BoundsEvidence(double MinX, double MinY, double MaxX, double MaxY);
    private sealed record CollisionCandidateEvidence(string SourceKind, string SourceId, string MapId, BoundsEvidence Bounds);
    private sealed record AxisEvidence(double Requested, double Applied, bool Constrained, string? ConstraintSourceId);
}
