using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.Validation;

namespace Agentic2D.Spatial.Continuous;

public sealed record ContinuousTransform2(double X, double Y);
public sealed record KinematicMotion2(double VelocityX, double VelocityY, double MaxSpeed);
public sealed record CollisionAabb2(double HalfWidth, double HalfHeight);
public sealed record SpatialMembership(string WorldId, string SpatialModuleId);
public sealed record ContinuousResolution(string IntentId, string EntityId, double RequestedX, double RequestedY, double AppliedX, double AppliedY, string Outcome, ContinuousTransform2 Result, IReadOnlyList<string> Candidates, string? CommandId, IReadOnlyList<string> Events, IReadOnlyList<string> Diagnostics) { public string? BehaviorAssignmentId { get; init; } public ContinuousTransform2? InitialTransform { get; init; } public CollisionAabb2? CollisionShape { get; init; } public IReadOnlyList<ContinuousCollisionCandidate> CollisionCandidateDetails { get; init; } = []; public ContinuousAxisResolution XAxis { get; init; } = new(0, 0, false, null); public ContinuousAxisResolution YAxis { get; init; } = new(0, 0, false, null); }
public sealed record ContinuousCollisionCandidate(string SourceKind, string SourceId, string MapId, double MinX, double MinY, double MaxX, double MaxY);
public sealed record ContinuousAxisResolution(double Requested, double Applied, bool Constrained, string? ConstraintSourceId);

public sealed class ContinuousKinematicSpatialResolver
{
    public const string ModuleId = "spatial.continuous-kinematic-2d";
    private const double Epsilon = 0.000000001;
    private readonly EntityComponentWorld world;
    private readonly MapContentSource map;
    public ContinuousKinematicSpatialResolver(EntityComponentWorld world, MapContentSource map) { this.world = world; this.map = map; }
    public static void Register(EntityComponentWorld world)
    {
        world.Register<ContinuousTransform2>("component.continuous-transform-2d", ModuleId, x => Finite(x.X) && Finite(x.Y));
        world.Register<KinematicMotion2>("component.kinematic-motion-2d", ModuleId, x => Finite(x.VelocityX) && Finite(x.VelocityY) && Finite(x.MaxSpeed) && x.MaxSpeed >= 0);
        world.Register<CollisionAabb2>("component.collision-aabb-2d", ModuleId, x => Finite(x.HalfWidth) && Finite(x.HalfHeight) && x.HalfWidth > 0 && x.HalfHeight > 0);
        world.Register<SpatialMembership>("component.spatial-membership", "runtime/core", x => !string.IsNullOrWhiteSpace(x.WorldId) && x.SpatialModuleId == ModuleId);
    }
    public ContinuousResolution Resolve(string intentId, string entityId, double directionX, double directionY)
    {
        if (!world.TryGet<ContinuousTransform2>(entityId, out var transform) || !world.TryGet<KinematicMotion2>(entityId, out var motion) || !world.TryGet<CollisionAabb2>(entityId, out var body) || !world.TryGet<SpatialMembership>(entityId, out var membership)) return Fail(intentId, entityId, "CONTINUOUS0001");
        if (membership!.SpatialModuleId != ModuleId || membership!.WorldId != map.Id) return Fail(intentId, entityId, "CONTINUOUS0003");
        var length = Math.Sqrt(directionX * directionX + directionY * directionY);
        var requestedX = length > Epsilon ? directionX / length * motion!.MaxSpeed : 0d; var requestedY = length > Epsilon ? directionY / length * motion!.MaxSpeed : 0d;
        var candidates = StaticAabbs().ToArray();
        var x = ResolveAxis(transform!.X, transform!.Y, body!, requestedX, true, candidates);
        var y = ResolveAxis(transform!.X + x, transform!.Y, body!, requestedY, false, candidates);
        var outcome = Classify(requestedX, requestedY, x, y);
        var result = new ContinuousTransform2(Normalize(transform!.X + x), Normalize(transform!.Y + y));
        var xConstrained = Math.Abs(x - requestedX) > Epsilon; var yConstrained = Math.Abs(y - requestedY) > Epsilon;
        var xSource = xConstrained ? (x + transform!.X < body!.HalfWidth + Epsilon || x + transform.X > map.Width - body!.HalfWidth - Epsilon ? "map.bounds" : candidates.FirstOrDefault(c => !c.Bounds)?.Id) : null;
        var ySource = yConstrained ? (y + transform!.Y < body!.HalfHeight + Epsilon || y + transform.Y > map.Height - body!.HalfHeight - Epsilon ? "map.bounds" : candidates.FirstOrDefault(c => !c.Bounds)?.Id) : null;
        var commandId = outcome == "blocked" ? null : "command." + intentId;
        return new ContinuousResolution(intentId, entityId, Normalize(requestedX), Normalize(requestedY), Normalize(x), Normalize(y), outcome, result, candidates.Select(c => c.Id).ToArray(), commandId, outcome == "blocked" ? ["spatial.continuous-movement-blocked"] : ["spatial.continuous-movement-" + outcome, "entity.continuous-transform-changed"], []) { InitialTransform = transform, CollisionShape = body, CollisionCandidateDetails = candidates.Select(c => new ContinuousCollisionCandidate(c.Bounds ? "map-bounds" : c.Id.StartsWith("cell:", StringComparison.Ordinal) ? "blocked-map-cell" : "static-map-object", c.Id, map.Id, Normalize(c.MinX), Normalize(c.MinY), Normalize(c.MaxX), Normalize(c.MaxY))).ToArray(), XAxis = new ContinuousAxisResolution(Normalize(requestedX), Normalize(x), xConstrained, xSource), YAxis = new ContinuousAxisResolution(Normalize(requestedY), Normalize(y), yConstrained, ySource) };
    }
    public EntityComponentResult Apply(ContinuousResolution resolution, int tick) => resolution.CommandId is null ? new(false, "blocked", null) : world.Set(resolution.EntityId, resolution.Result, tick, resolution.CommandId);
    private ContinuousResolution Fail(string intent, string entity, string diagnostic) => new(intent, entity, 0, 0, 0, 0, "blocked", new(0, 0), [], null, ["spatial.continuous-movement-blocked"], [diagnostic]);
    private IEnumerable<Aabb> StaticAabbs()
    {
        yield return new("map.bounds", 0, 0, map.Width, map.Height, true);
        foreach (var cell in map.CellOverrides.Where(x => x.PhysicalBehavior == "blocked").OrderBy(x => x.Y).ThenBy(x => x.X)) yield return new($"cell:{cell.X},{cell.Y}", cell.X, cell.Y, cell.X + 1, cell.Y + 1, false);
        foreach (var o in map.Objects.OrderBy(x => x.Id, StringComparer.Ordinal)) yield return new(o.Id, o.Position.X - o.Bounds.HalfWidth, o.Position.Y - o.Bounds.HalfHeight, o.Position.X + o.Bounds.HalfWidth, o.Position.Y + o.Bounds.HalfHeight, false);
    }
    private static double ResolveAxis(double x, double y, CollisionAabb2 body, double requested, bool axisX, IReadOnlyList<Aabb> candidates)
    {
        var target = axisX ? x + requested : y + requested;
        var clamped = axisX ? Math.Clamp(target, body.HalfWidth, candidates.First(c => c.Bounds).MaxX - body.HalfWidth) : Math.Clamp(target, body.HalfHeight, candidates.First(c => c.Bounds).MaxY - body.HalfHeight);
        foreach (var candidate in candidates.Where(c => !c.Bounds))
        {
            var other = axisX ? y : x; var otherHalf = axisX ? body.HalfHeight : body.HalfWidth;
            var min = axisX ? candidate.MinY : candidate.MinX; var max = axisX ? candidate.MaxY : candidate.MaxX;
            if (other + otherHalf <= min || other - otherHalf >= max) continue;
            if (requested > 0 && (axisX ? x : y) + (axisX ? body.HalfWidth : body.HalfHeight) <= (axisX ? candidate.MinX : candidate.MinY) + Epsilon) clamped = Math.Min(clamped, (axisX ? candidate.MinX : candidate.MinY) - (axisX ? body.HalfWidth : body.HalfHeight));
            if (requested < 0 && (axisX ? x : y) - (axisX ? body.HalfWidth : body.HalfHeight) >= (axisX ? candidate.MaxX : candidate.MaxY) - Epsilon) clamped = Math.Max(clamped, (axisX ? candidate.MaxX : candidate.MaxY) + (axisX ? body.HalfWidth : body.HalfHeight));
        }
        return clamped - (axisX ? x : y);
    }
    private static string Classify(double rx, double ry, double ax, double ay) => Math.Abs(ax) < Epsilon && Math.Abs(ay) < Epsilon ? "blocked" : Math.Abs(ax - rx) < Epsilon && Math.Abs(ay - ry) < Epsilon ? "accepted" : Math.Abs(ax - rx) > Epsilon && Math.Abs(ay - ry) > Epsilon ? "blocked" : Math.Abs(ax - rx) > Epsilon && Math.Abs(ay) > Epsilon || Math.Abs(ay - ry) > Epsilon && Math.Abs(ax) > Epsilon ? "slid" : "clipped";
    private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    private static double Normalize(double value) => value == 0d ? 0d : value;
    private sealed record Aabb(string Id, double MinX, double MinY, double MaxX, double MaxY, bool Bounds);
}
