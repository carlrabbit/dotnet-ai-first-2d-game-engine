using System.Text.Json;
using Agentic2D.Engine;
using Agentic2D.Spatial.Continuous;
using Agentic2D.Validation;

namespace Agentic2D.Tests.Unit;

public sealed class ContinuousResolverOutcomeTests
{
    [Test]
    public async Task DiagonalMovementSlidesAlongVerticalStaticObstacleWithLinkedMutation()
    {
        var first = ExecuteSlide();
        var second = ExecuteSlide();

        await Assert.That(first.Resolution.Outcome).IsEqualTo("slid");
        await Assert.That(first.Resolution.XAxis.Constrained).IsTrue();
        await Assert.That(first.Resolution.YAxis.Applied).IsGreaterThan(0d);
        await Assert.That(first.Resolution.AppliedX).IsNotEqualTo(first.Resolution.RequestedX);
        await Assert.That(first.Resolution.Result.X).IsGreaterThan(0.5d);
        await Assert.That(first.Resolution.Result.Y).IsGreaterThan(0.5d);
        await Assert.That(first.Resolution.XAxis.ConstraintSourceId).IsEqualTo("object.slide-wall");
        await Assert.That(first.Resolution.CollisionCandidateDetails.Single(item => item.SourceId == "object.slide-wall").SourceKind).IsEqualTo("static-map-object");
        await Assert.That(first.Resolution.CommandId).IsNotNull();
        await Assert.That(first.ApplyResult.Accepted).IsTrue();
        await Assert.That(first.World.Mutations.Single(item => item.CommandId == first.Resolution.CommandId).Status).IsEqualTo("accepted");
        await Assert.That(first.Resolution.Events).Contains("spatial.continuous-movement-slid");
        await Assert.That(first.Resolution.Events).Contains("entity.continuous-transform-changed");
        await Assert.That(first.Resolution.Diagnostics).IsEmpty();
        await Assert.That(first.Resolution.Result.X + 0.25d).IsLessThanOrEqualTo(1d);
        await Assert.That(JsonSerializer.Serialize(first.Resolution)).IsEqualTo(JsonSerializer.Serialize(second.Resolution));
    }

    [Test]
    public async Task EastwardMovementIntoAdjacentStaticObstacleIsBlockedWithoutMutation()
    {
        var first = ExecuteBlocked();
        var second = ExecuteBlocked();

        await Assert.That(first.Resolution.Outcome).IsEqualTo("blocked");
        await Assert.That(first.Resolution.AppliedX).IsEqualTo(0d);
        await Assert.That(first.Resolution.AppliedY).IsEqualTo(0d);
        await Assert.That(first.Resolution.Result).IsEqualTo(new ContinuousTransform2(0.5d, 0.5d));
        await Assert.That(first.Resolution.XAxis.Constrained).IsTrue();
        await Assert.That(first.Resolution.XAxis.ConstraintSourceId).IsEqualTo("object.block-wall");
        await Assert.That(first.Resolution.CollisionCandidateDetails.Single(item => item.SourceId == "object.block-wall").SourceKind).IsEqualTo("static-map-object");
        await Assert.That(first.Resolution.CommandId).IsNull();
        await Assert.That(first.ApplyResult.Accepted).IsFalse();
        await Assert.That(first.World.Mutations.Any(item => item.ComponentTypeId == "component.continuous-transform-2d" && item.MutationKind == "update")).IsFalse();
        await Assert.That(first.Resolution.Events).Contains("spatial.continuous-movement-blocked");
        await Assert.That(first.Resolution.Events).DoesNotContain("entity.continuous-transform-changed");
        await Assert.That(first.Resolution.Diagnostics).IsEmpty();
        await Assert.That(first.Resolution.Result.X + 0.25d).IsLessThanOrEqualTo(0.75d);
        await Assert.That(JsonSerializer.Serialize(first.Resolution)).IsEqualTo(JsonSerializer.Serialize(second.Resolution));
    }

    private static Execution ExecuteSlide() => Execute("object.slide-wall", new MapObjectPosition(1.25d, 0.5d), new MapObjectBounds("aabb", 0.25d, 0.25d), Math.Sqrt(1.25d), 1d, 0.5d);
    private static Execution ExecuteBlocked() => Execute("object.block-wall", new MapObjectPosition(1d, 0.5d), new MapObjectBounds("aabb", 0.25d, 0.25d), 0.5d, 1d, 0d);

    private static Execution Execute(string objectId, MapObjectPosition position, MapObjectBounds bounds, double speed, double directionX, double directionY)
    {
        var map = new MapContentSource { Id = "map.continuous-outcome-test", Width = 5, Height = 5, Objects = [new MapObjectSource(objectId, "static-obstacle", null, position, bounds)] };
        var world = new EntityComponentWorld();
        ContinuousKinematicSpatialResolver.Register(world);
        world.CreateEntity("entity.player");
        world.Set("entity.player", new ContinuousTransform2(0.5d, 0.5d));
        world.Set("entity.player", new KinematicMotion2(speed));
        world.Set("entity.player", new CollisionAabb2(0.25d, 0.25d));
        world.Set("entity.player", new SpatialMembership(map.Id, ContinuousKinematicSpatialResolver.ModuleId));
        var resolver = new ContinuousKinematicSpatialResolver(world, map);
        var resolution = resolver.Resolve("intent.outcome-test", "entity.player", directionX, directionY) with { BehaviorAssignmentId = "assignment.outcome-test" };
        var commit = SpatialMutationCommitter.Commit(world, resolver.AcceptedMutation(resolution), 1, resolution.CommandId);
        return new Execution(world, resolution, new EntityComponentResult(commit.Accepted, commit.Status, resolution.CommandId));
    }

    private sealed record Execution(EntityComponentWorld World, ContinuousResolution Resolution, EntityComponentResult ApplyResult);
}
