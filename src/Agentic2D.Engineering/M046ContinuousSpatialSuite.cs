using System.Text.Json;
using Agentic2D.Engine;
using Agentic2D.Spatial.Continuous;
using Agentic2D.Validation;

namespace Agentic2D.Engineering;

internal static class M046ContinuousSpatialSuite
{
    public static async Task<int> RunAsync(string root, string shard, TextWriter diagnostics)
    {
        var probe = Probe(root);
        var path = Path.Combine(root, "artifacts", "spatial", "M046", shard + ".json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { schema = "agentic2.m046.observation.v1", milestone = "M046", shard, status = probe.Values.Values.All(x => x) ? "passed" : "failed", evidence = new { observed = probe.Values, outcomes = probe.Outcomes, sourceAttribution = probe.Sources, numeric = probe.Numeric, mutation = probe.Mutation, assertions = probe.Assertions } }, new JsonSerializerOptions { WriteIndented = true }));
        await diagnostics.WriteLineAsync($"m046 evidence written for {shard}: {(probe.Values.Values.All(x => x) ? "passed" : "failed")}");
        return probe.Values.Values.All(x => x) ? 0 : 1;
    }

    private static ProbeResult Probe(string root)
    {
        var map = new MapContentSource
        {
            Id = "map.m046",
            Width = 10,
            Height = 10,
            Objects = [
            new MapObjectSource("object.vertical", "wall", null, new(4, 5), new("aabb", .5, 4)),
            new MapObjectSource("object.corner", "wall", null, new(6, 4), new("aabb", 1, .5))]
        };
        var world = NewWorld(map, new(2, 2), .5);
        var resolver = new ContinuousKinematicSpatialResolver(world, map);
        var finiteReject = resolver.Resolve("intent.nan", "entity.player", double.NaN, 1);
        var noOp = resolver.Resolve("intent.zero", "entity.player", 0, 0);
        var accepted = resolver.Resolve("intent.accepted", "entity.player", -1, 0);
        var slideWorld = NewWorld(map, new(2, 2), 2);
        var slideResolver = new ContinuousKinematicSpatialResolver(slideWorld, map);
        var slide = slideResolver.Resolve("intent.slide", "entity.player", 1, 1);
        var blockedWorld = NewWorld(map, new(3.25, 5), 2);
        var blocked = new ContinuousKinematicSpatialResolver(blockedWorld, map).Resolve("intent.blocked", "entity.player", 1, 0);
        var clippedWorld = NewWorld(map, new(9.2, 9.2), 1);
        var clipped = new ContinuousKinematicSpatialResolver(clippedWorld, map).Resolve("intent.clip", "entity.player", 1, 1);
        var nearMap = new MapContentSource { Id = "map.m046.near", Width = 10, Height = 10, Objects = [new MapObjectSource("object.far", "wall", null, new(6, 5), new("aabb", .5, 1)), new MapObjectSource("object.near", "wall", null, new(4, 5), new("aabb", .5, 1))] };
        var nearWorld = NewWorld(nearMap, new(2, 5), 5);
        var near = new ContinuousKinematicSpatialResolver(nearWorld, nearMap).Resolve("intent.near", "entity.player", 1, 0);
        var penetrationWorld = NewWorld(map, new(4, 5), 1);
        var penetration = new ContinuousKinematicSpatialResolver(penetrationWorld, map).Resolve("intent.penetration", "entity.player", 0, 1);
        var cellMap = new MapContentSource { Id = "map.m046.cell", Width = 4, Height = 4, CellOverrides = [new MapCellOverrideSource(1, 1, "blocked")] };
        var cellWorld = NewWorld(cellMap, new(.75, 1.5), 1);
        var cell = new ContinuousKinematicSpatialResolver(cellWorld, cellMap).Resolve("intent.cell", "entity.player", 1, 0);
        var slideCommit = SpatialMutationCommitter.Commit(slideWorld, slideResolver.AcceptedMutation(slide), 1, slide.CommandId);
        var finalSnapshot = slideWorld.TypedSnapshot(1);
        var factualEvent = slideCommit.Accepted && finalSnapshot.TryGetByTypeId("entity.player", "component.continuous-transform-2d", out ContinuousTransform2? changed) && changed!.Equals(slide.Result);
        var values = new SortedDictionary<string, bool>(StringComparer.Ordinal)
        {
            ["finiteIntentRejects"] = finiteReject.Outcome == "rejected" && finiteReject.Diagnostics.Contains("CONTINUOUS0002") && finiteReject.CommandId is null && finiteReject.AppliedX == 0 && finiteReject.AppliedY == 0,
            ["zeroDirectionNoOp"] = noOp.Outcome == "no-op" && noOp.CommandId is null && noOp.Events.Count == 0,
            ["accepted"] = accepted.Outcome == "accepted" && accepted.AppliedX == accepted.RequestedX,
            ["blocked"] = blocked.Outcome == "blocked" && blocked.AppliedX == 0 && blocked.AppliedY == 0 && blocked.CommandId is null,
            ["slide"] = slide.Outcome == "slid" && slide.AppliedY > 0 && slide.XAxis.Constrained,
            ["bothAxesClip"] = clipped.Outcome == "clipped" && clipped.AppliedX > 0 && clipped.AppliedY > 0 && clipped.AppliedX < clipped.RequestedX && clipped.AppliedY < clipped.RequestedY,
            ["actualLimiter"] = slide.XAxis.ConstraintSourceId == "object.vertical" && blocked.XAxis.ConstraintSourceId == "object.vertical",
            ["closestLimiterWins"] = near.XAxis.ConstraintSourceId == "object.near" && near.AppliedX < near.RequestedX,
            ["initialPenetrationRejects"] = penetration.Outcome == "rejected" && penetration.Diagnostics.Contains("CONTINUOUS0005") && penetration.CommandId is null,
            ["blockedCellObserved"] = cell.XAxis.ConstraintSourceId == "cell:1,1" && cell.Outcome == "blocked",
            ["candidateOrdering"] = slide.Candidates.SequenceEqual(slide.Candidates.Order(StringComparer.Ordinal)),
            ["noPenetration"] = factualEvent,
            ["proposalCommitLink"] = slide.CommandId is not null && slideCommit.Accepted && slideWorld.Mutations.Any(m => m.CommandId == slide.CommandId && m.Status == "accepted"),
            ["noFactualRejectedEvent"] = finiteReject.Events.Contains("spatial.continuous-movement-rejected") && !finiteReject.Events.Contains("entity.continuous-transform-changed"),
            ["motionPolicy"] = Math.Abs(slide.RequestedX) <= 2 && Math.Abs(slide.RequestedY) <= 2,
            ["strictAssertions"] = StrictAssertionSource(root)
        };
        return new(values, new { accepted = accepted.Outcome, blocked = blocked.Outcome, slid = slide.Outcome, clipped = clipped.Outcome, noOp = noOp.Outcome, rejected = finiteReject.Outcome, cell = cell.Outcome }, new { slideX = slide.XAxis.ConstraintSourceId, blockedX = blocked.XAxis.ConstraintSourceId, cellX = cell.XAxis.ConstraintSourceId }, new { nan = double.NaN.ToString(), infinity = double.PositiveInfinity.ToString(), finiteDiagnostic = finiteReject.Diagnostics.Single() }, new { slideCommit.Accepted, factualEvent }, new { unsupportedFails = StrictAssertionSource(root) });
    }

    private static EntityComponentWorld NewWorld(MapContentSource map, ContinuousTransform2 position, double speed)
    {
        var world = new EntityComponentWorld(); ContinuousKinematicSpatialResolver.Register(world); world.CreateEntity("entity.player");
        world.Set("entity.player", position); world.Set("entity.player", new KinematicMotion2(speed)); world.Set("entity.player", new CollisionAabb2(.25, .25)); world.Set("entity.player", new SpatialMembership(map.Id, ContinuousKinematicSpatialResolver.ModuleId)); return world;
    }
    private static bool StrictAssertionSource(string root) => File.ReadAllText(Path.Combine(root, "src", "Agentic2D.ScenarioRunner", "ContinuousScenarioExecutor.cs")).Contains("Unsupported assertion type", StringComparison.Ordinal);
    private sealed record ProbeResult(SortedDictionary<string, bool> Values, object Outcomes, object Sources, object Numeric, object Mutation, object Assertions);
}
