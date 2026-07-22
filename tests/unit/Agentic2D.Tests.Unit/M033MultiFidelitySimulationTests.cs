using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Tests.Unit;

public sealed class M033MultiFidelitySimulationTests
{
    [Test]
    public async Task DiscreteEventEqualTimeOrderingIsStable()
    {
        var world = new SimulationWorld(new("world.scheduler"));
        var queue = new DiscreteEventScheduler();
        queue.Schedule(Request("trigger.third", 10, 2));
        queue.Schedule(Request("trigger.first", 10, 1));
        queue.Schedule(Request("trigger.second", 10, 1));
        var delivered = new List<string>();
        var result = queue.AdvanceTo(world, new(10), trigger => { delivered.Add(trigger.Id); return new(ScheduledTriggerStatus.Completed, null, "accepted"); });
        await Assert.That(result.SafetyStopped).IsFalse();
        await Assert.That(delivered).IsEquivalentTo(["trigger.first", "trigger.second", "trigger.third"]);
        await Assert.That(world.Clock.Now).IsEqualTo(new SimulationInstant(10));
    }

    [Test]
    public async Task ScheduledTriggerCancellationAndStaleDeliveryCannotMutate()
    {
        var world = new SimulationWorld(new("world.trigger"));
        var queue = new DiscreteEventScheduler();
        queue.Schedule(Request("trigger.cancel", 1, 1));
        queue.Schedule(Request("trigger.stale", 2, 1));
        await Assert.That(queue.Cancel("trigger.cancel", "activity-cancelled")).IsTrue();
        var mutations = 0;
        queue.AdvanceTo(world, new(2), trigger => { if (trigger.Id == "trigger.stale") return new(ScheduledTriggerStatus.Stale, null, "revision-mismatch"); mutations++; return new(ScheduledTriggerStatus.Completed, null, "accepted"); });
        await Assert.That(mutations).IsEqualTo(0);
        await Assert.That(queue.Inspect().Select(trigger => trigger.Status)).Contains(ScheduledTriggerStatus.Cancelled);
        await Assert.That(queue.Inspect().Select(trigger => trigger.Status)).Contains(ScheduledTriggerStatus.Stale);
    }

    [Test]
    public async Task AbstractTravelUsesCoarseGraphNotDetailedNavigation()
    {
        var route = M033MultiFidelitySimulation.PlanAbstractTravel("worker.alpha.001", "housing", "forest", [new("edge.housing-forest", "housing", "forest", 7)], 2, true);
        await Assert.That(route.Cost).IsEqualTo(14);
        await Assert.That(route.EdgeIds).IsEquivalentTo(["edge.housing-forest"]);
        await Assert.That(() => M033MultiFidelitySimulation.PlanAbstractTravel("worker.alpha.001", "housing", "water", [], 2, false)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task RegionFidelityAndRegionReconciliationTransitionsHaveOneOwnerAndRollback()
    {
        var world = new SimulationWorld(new("world.fidelity"));
        world.CreateRegion(new("region.a"), "a"); world.CreateRegion(new("region.b"), "b");
        var coordinator = new RegionFidelityCoordinator(world, new DiscreteEventScheduler(), [new("region.a", RegionFidelity.Detailed, "detailed", 1, RegionTransitionStatus.Stable, 0), new("region.b", RegionFidelity.Abstract, "abstract", 1, RegionTransitionStatus.Stable, 0)]);
        var failed = coordinator.SwitchDetailed("region.b", forceInvalidMaterialization: true);
        await Assert.That(failed.Status).IsEqualTo("failed");
        await Assert.That(coordinator.Regions.Single(region => region.RegionId == "region.a").Fidelity).IsEqualTo(RegionFidelity.Detailed);
        var committed = coordinator.SwitchDetailed("region.b");
        await Assert.That(committed.Status).IsEqualTo("committed");
        await Assert.That(coordinator.Regions.Count(region => region.Fidelity == RegionFidelity.Detailed)).IsEqualTo(1);
    }

    [Test]
    public async Task AbstractActivityFamiliesAndThirtyDayRunAreCommandBacked()
    {
        var run = M033MultiFidelitySimulation.RunThirtyDays();
        await Assert.That(run.Diagnostics).IsEmpty();
        await Assert.That(run.Days).IsEqualTo(30);
        await Assert.That(run.World.Activities.Select(activity => activity.Kind)).Contains("travel");
        await Assert.That(run.World.Activities.Select(activity => activity.Kind)).Contains("harvest");
        await Assert.That(run.World.Activities.Select(activity => activity.Kind)).Contains("eat");
        await Assert.That(run.World.Activities.All(activity => activity.Status == SimulationActivityStatus.Completed)).IsTrue();
    }

    [Test]
    public async Task MultiFidelityPersistenceRestoresQueueAndStableOwnership()
    {
        var run = M033MultiFidelitySimulation.RunThirtyDays();
        var restored = M033MultiFidelitySimulation.ContinueFromSave(run.Coordinator.Capture());
        await Assert.That(restored.Diagnostics).IsEmpty();
        await Assert.That(restored.Scheduler.Inspect().Count).IsEqualTo(run.Scheduler.Inspect().Count);
        await Assert.That(restored.Coordinator.Regions.Count(region => region.Fidelity == RegionFidelity.Detailed)).IsEqualTo(1);
    }

    [Test]
    public async Task MultiFidelityEquivalenceIsDeterministicAndObserverNeutralInBoundedProof()
    {
        var first = M033MultiFidelitySimulation.RunThirtyDays();
        var second = M033MultiFidelitySimulation.RunThirtyDays();
        await Assert.That(first.Fingerprint).IsEqualTo(second.Fingerprint);
        await Assert.That(first.World.Entities.Where(entity => entity.Id.StartsWith("resource.", StringComparison.Ordinal)).All(entity => entity.Components["component.m033.resource"].GetProperty("sourceWood").GetInt32() + entity.Components["component.m033.resource"].GetProperty("storedWood").GetInt32() == 60)).IsTrue();
    }

    [Test]
    public async Task StandaloneSimulationHostWritesStructuralArtifacts()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m033-" + Guid.NewGuid().ToString("N"));
        try
        {
            await M033ArtifactWriter.WriteAsync(root);
            await Assert.That(File.Exists(Path.Combine(root, "m033-manifest.json"))).IsTrue();
            await Assert.That(File.Exists(Path.Combine(root, "review-pack", "review-manifest.json"))).IsTrue();
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static ScheduledTriggerRequest Request(string id, long due, int priority) => new(id, new(due), priority, "region.test", null, null, "test", null, 1, "correlation", "cause", JsonSerializer.SerializeToElement(new { }));
}
