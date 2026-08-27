using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.Validation;

namespace Agentic2D.Spatial.Continuous;

public sealed record ContinuousTransform2(double X, double Y);
public sealed record KinematicMotion2(double MaxSpeed);
public sealed record CollisionAabb2(double HalfWidth, double HalfHeight);
public sealed record SpatialMembership(string WorldId, string SpatialModuleId);
public sealed record ContinuousCollisionCandidate(string SourceKind, string SourceId, string MapId, double MinX, double MinY, double MaxX, double MaxY);
public sealed record ContinuousAxisResolution(double Requested, double Applied, bool Constrained, string? ConstraintSourceId);
public sealed record ContinuousResolution(string IntentId, string EntityId, double RequestedX, double RequestedY, double AppliedX, double AppliedY, string Outcome, ContinuousTransform2 Result, IReadOnlyList<string> Candidates, string? CommandId, IReadOnlyList<string> Events, IReadOnlyList<string> Diagnostics)
{
    public string? BehaviorAssignmentId { get; init; }
    public ContinuousTransform2? InitialTransform { get; init; }
    public CollisionAabb2? CollisionShape { get; init; }
    public IReadOnlyList<ContinuousCollisionCandidate> CollisionCandidateDetails { get; init; } = [];
    public ContinuousAxisResolution XAxis { get; init; } = new(0, 0, false, null);
    public ContinuousAxisResolution YAxis { get; init; } = new(0, 0, false, null);
}

public sealed class ContinuousKinematicSpatialResolver
{
    public const string ModuleId = "spatial.continuous-kinematic-2d";
    private const double Epsilon = 1e-9;
    private readonly IRuntimeSnapshotView snapshot;
    private readonly MapContentSource map;
    public ContinuousKinematicSpatialResolver(EntityComponentWorld world, MapContentSource map) : this(world.TypedSnapshot(0), map) { }
    public ContinuousKinematicSpatialResolver(IRuntimeSnapshotView snapshot, MapContentSource map) { this.snapshot = snapshot; this.map = map; }
    public static void Register(EntityComponentWorld world)
    {
        world.Register<ContinuousTransform2>("component.continuous-transform-2d", ModuleId, x => Finite(x.X) && Finite(x.Y));
        world.Register<KinematicMotion2>("component.kinematic-motion-2d", ModuleId, x => Finite(x.MaxSpeed) && x.MaxSpeed >= 0);
        world.Register<CollisionAabb2>("component.collision-aabb-2d", ModuleId, x => Finite(x.HalfWidth) && Finite(x.HalfHeight) && x.HalfWidth > 0 && x.HalfHeight > 0);
        world.Register<SpatialMembership>("component.spatial-membership", "runtime/core", x => !string.IsNullOrWhiteSpace(x.WorldId) && x.SpatialModuleId == ModuleId);
    }
    public ContinuousResolution Resolve(string intentId, string entityId, double directionX, double directionY) => Resolve(snapshot, intentId, entityId, directionX, directionY);
    public ContinuousResolution Resolve(IRuntimeSnapshotView snapshot, string intentId, string entityId, double directionX, double directionY)
    {
        if (!Finite(directionX) || !Finite(directionY)) return Reject(intentId, entityId, "CONTINUOUS0002");
        if (!snapshot.TryGetByTypeId(entityId, "component.continuous-transform-2d", out ContinuousTransform2? transform) || !snapshot.TryGetByTypeId(entityId, "component.kinematic-motion-2d", out KinematicMotion2? motion) || !snapshot.TryGetByTypeId(entityId, "component.collision-aabb-2d", out CollisionAabb2? body) || !snapshot.TryGetByTypeId(entityId, "component.spatial-membership", out SpatialMembership? membership)) return Reject(intentId, entityId, "CONTINUOUS0001");
        if (membership!.SpatialModuleId != ModuleId || membership.WorldId != map.Id) return Reject(intentId, entityId, "CONTINUOUS0003");
        var currentTransform = transform!;
        var currentMotion = motion!;
        var currentBody = body!;
        var candidates = StaticAabbs().ToArray();
        if (Penetrates(currentTransform, currentBody, candidates)) return Reject(intentId, entityId, "CONTINUOUS0005", currentTransform, currentBody, candidates);
        var length = Math.Sqrt(directionX * directionX + directionY * directionY);
        var requestedX = length > Epsilon ? directionX / length * currentMotion.MaxSpeed : 0d;
        var requestedY = length > Epsilon ? directionY / length * currentMotion.MaxSpeed : 0d;
        var x = ResolveAxis(currentTransform.X, currentTransform.Y, currentBody, requestedX, true, candidates);
        var y = ResolveAxis(currentTransform.X + x.Applied, currentTransform.Y, currentBody, requestedY, false, candidates);
        var outcome = Classify(requestedX, requestedY, x.Applied, y.Applied);
        var result = new ContinuousTransform2(Normalize(currentTransform.X + x.Applied), Normalize(currentTransform.Y + y.Applied));
        var details = candidates.Select(c => new ContinuousCollisionCandidate(c.Bounds ? "map-bounds" : c.Id.StartsWith("cell:", StringComparison.Ordinal) ? "blocked-map-cell" : "static-map-object", c.Id, map.Id, Normalize(c.MinX), Normalize(c.MinY), Normalize(c.MaxX), Normalize(c.MaxY))).ToArray();
        var commandId = outcome is "accepted" or "slid" or "clipped" ? "command." + intentId : null;
        var events = outcome == "no-op" ? Array.Empty<string>() : outcome == "blocked" ? ["spatial.continuous-movement-blocked"] : new[] { "spatial.continuous-movement-" + outcome, "entity.continuous-transform-changed" };
        return new ContinuousResolution(intentId, entityId, Normalize(requestedX), Normalize(requestedY), Normalize(x.Applied), Normalize(y.Applied), outcome, result, candidates.Select(c => c.Id).ToArray(), commandId, events, []) { InitialTransform = currentTransform, CollisionShape = currentBody, CollisionCandidateDetails = details, XAxis = new(Normalize(requestedX), Normalize(x.Applied), x.Constrained, x.Source), YAxis = new(Normalize(requestedY), Normalize(y.Applied), y.Constrained, y.Source) };
    }
    public EntityComponentBatchMutation? AcceptedMutation(ContinuousResolution resolution) => resolution.CommandId is null ? null : new(resolution.EntityId, "component.continuous-transform-2d", resolution.Result);
    private static ContinuousResolution Reject(string intent, string entity, string diagnostic, ContinuousTransform2? transform = null, CollisionAabb2? body = null, IReadOnlyList<Aabb>? candidates = null) => new(intent, entity, 0, 0, 0, 0, "rejected", transform ?? new(0, 0), candidates?.Select(x => x.Id).ToArray() ?? [], null, ["spatial.continuous-movement-rejected"], [diagnostic]) { InitialTransform = transform, CollisionShape = body, CollisionCandidateDetails = candidates?.Select(c => new ContinuousCollisionCandidate(c.Bounds ? "map-bounds" : "static-map-object", c.Id, "", c.MinX, c.MinY, c.MaxX, c.MaxY)).ToArray() ?? [] };
    private IEnumerable<Aabb> StaticAabbs()
    {
        yield return new("map.bounds", 0, 0, map.Width, map.Height, true);
        foreach (var cell in map.CellOverrides.Where(x => x.PhysicalBehavior == "blocked").OrderBy(x => x.Y).ThenBy(x => x.X)) yield return new($"cell:{cell.X},{cell.Y}", cell.X, cell.Y, cell.X + 1, cell.Y + 1, false);
        foreach (var o in map.Objects.OrderBy(x => x.Id, StringComparer.Ordinal)) yield return new(o.Id, o.Position.X - o.Bounds.HalfWidth, o.Position.Y - o.Bounds.HalfHeight, o.Position.X + o.Bounds.HalfWidth, o.Position.Y + o.Bounds.HalfHeight, false);
    }
    private static bool Penetrates(ContinuousTransform2 p, CollisionAabb2 b, IReadOnlyList<Aabb> candidates) => candidates.Any(c => c.Bounds ? p.X - b.HalfWidth < c.MinX - Epsilon || p.X + b.HalfWidth > c.MaxX + Epsilon || p.Y - b.HalfHeight < c.MinY - Epsilon || p.Y + b.HalfHeight > c.MaxY + Epsilon : p.X + b.HalfWidth > c.MinX + Epsilon && p.X - b.HalfWidth < c.MaxX - Epsilon && p.Y + b.HalfHeight > c.MinY + Epsilon && p.Y - b.HalfHeight < c.MaxY - Epsilon);
    private static AxisResult ResolveAxis(double x, double y, CollisionAabb2 body, double requested, bool axisX, IReadOnlyList<Aabb> candidates)
    {
        if (Math.Abs(requested) <= Epsilon) return new(0, false, null);
        var current = axisX ? x : y; var extent = axisX ? body.HalfWidth : body.HalfHeight; var desired = current + requested;
        var lower = extent; var upper = (axisX ? candidates.First(c => c.Bounds).MaxX : candidates.First(c => c.Bounds).MaxY) - extent;
        var limits = new List<(double value, string id)> { (lower, "map.bounds"), (upper, "map.bounds") };
        foreach (var c in candidates.Where(c => !c.Bounds))
        {
            var other = axisX ? y : x; var otherExtent = axisX ? body.HalfHeight : body.HalfWidth;
            if (other + otherExtent <= (axisX ? c.MinY : c.MinX) + Epsilon || other - otherExtent >= (axisX ? c.MaxY : c.MaxX) - Epsilon) continue;
            if (requested > 0 && current + extent <= (axisX ? c.MinX : c.MinY) + Epsilon) limits.Add(((axisX ? c.MinX : c.MinY) - extent, c.Id));
            if (requested < 0 && current - extent >= (axisX ? c.MaxX : c.MaxY) - Epsilon) limits.Add(((axisX ? c.MaxX : c.MaxY) + extent, c.Id));
        }
        var applicable = requested > 0 ? limits.Where(l => l.value >= current - Epsilon).OrderBy(l => l.value).ThenBy(l => l.id, StringComparer.Ordinal) : limits.Where(l => l.value <= current + Epsilon).OrderByDescending(l => l.value).ThenBy(l => l.id, StringComparer.Ordinal);
        var boundary = applicable.First().value;
        var appliedPosition = requested > 0 ? Math.Min(desired, boundary) : Math.Max(desired, boundary);
        var applied = appliedPosition - current; var constrained = Math.Abs(applied - requested) > Epsilon; var source = constrained ? limits.Where(l => Math.Abs(l.value - appliedPosition) <= Epsilon).OrderBy(l => l.id, StringComparer.Ordinal).Select(l => l.id).FirstOrDefault() : null;
        return new(applied, constrained, source);
    }
    private static string Classify(double rx, double ry, double ax, double ay) { if (Math.Abs(rx) <= Epsilon && Math.Abs(ry) <= Epsilon) return "no-op"; if (Math.Abs(ax) <= Epsilon && Math.Abs(ay) <= Epsilon) return "blocked"; var cx = Math.Abs(ax - rx) > Epsilon; var cy = Math.Abs(ay - ry) > Epsilon; if (!cx && !cy) return "accepted"; if (Math.Abs(rx) > Epsilon && Math.Abs(ry) > Epsilon && cx ^ cy) return "slid"; return "clipped"; }
    private static bool Finite(double value) => double.IsFinite(value);
    private static double Normalize(double value) => value == 0d ? 0d : value;
    private sealed record Aabb(string Id, double MinX, double MinY, double MaxX, double MaxY, bool Bounds);
    private sealed record AxisResult(double Applied, bool Constrained, string? Source);
}
