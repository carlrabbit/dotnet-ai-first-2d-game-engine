using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Tests.Unit;

public sealed class SimulationFoundationTests
{
    [Test]
    public async Task SimulationFoundationWoodWorkflowIsDeterministicAcrossFreshLoad()
    {
        var direct = M031WoodWorkflow.Direct();
        var roundtrip = M031WoodWorkflow.RoundTrip(out var save);

        await Assert.That(direct.Fingerprint).IsEqualTo(roundtrip.Fingerprint);
        await Assert.That(direct.Diagnostics).IsEmpty();
        await Assert.That(save.Schema).IsEqualTo(SimulationWorld.SaveSchema);
        await Assert.That(direct.World.Activities.Single().Status).IsEqualTo(SimulationActivityStatus.Completed);
        await Assert.That(direct.World.Reservations.All(x => x.Status != SimulationReservationStatus.Active)).IsTrue();
        await Assert.That(direct.World.Entities.Single(x => x.Id == "worker.001").RegionId).IsEqualTo("region.settlement");
    }

    [Test]
    public async Task SimulationWorldRegistrationLifecycleAndRegionQueriesAreDeterministic()
    {
        var first = NewWorld(registerInReverse: false);
        var second = NewWorld(registerInReverse: true);
        await Assert.That(first.RegistrationFingerprint).IsEqualTo(second.RegistrationFingerprint);
        await Assert.That(first.QueryRegion(new("region.a")).Select(x => x.Id)).IsEquivalentTo(["entity.a", "entity.b"]);
        var transferred = first.TransferRegion("entity.a", new("region.b"));
        await Assert.That(transferred.Status).IsEqualTo("accepted");
        await Assert.That(first.QueryRegion(new("region.a")).Select(x => x.Id)).IsEquivalentTo(["entity.b"]);
        await Assert.That(first.QueryRegion(new("region.b")).Select(x => x.Id)).IsEquivalentTo(["entity.a"]);
        await Assert.That(first.DestroyEntity("entity.a").Status).IsEqualTo("accepted");
        await Assert.That(first.CreateEntity("entity.a", SimulationEntityScope.RegionOwned, new("region.b")).Status).IsEqualTo("rejected");
    }

    [Test]
    public async Task SimulationTimeRejectsNegativeDurationsAndStaleActivityDoesNotMutate()
    {
        await Assert.That(() => new SimulationDuration(-1)).Throws<ArgumentOutOfRangeException>();
        var world = M031WoodWorkflow.CreateInitial();
        var created = world.CreateActivity(new("activity.test"), "worker.001", "test", "planned", ["tree.001"], new("c"), new("cause"));
        var before = world.Activities.Single();
        var stale = world.TransitionActivity(new("activity.test"), before.Revision + 1, "bad", SimulationActivityStatus.Active);
        await Assert.That(created.Status).IsEqualTo("accepted");
        await Assert.That(stale.Status).IsEqualTo("rejected");
        await Assert.That(world.Activities.Single().Stage).IsEqualTo("planned");
    }

    [Test]
    public async Task SimulationReservationsResolveCapacityAndReleaseIdempotently()
    {
        var world = M031WoodWorkflow.CreateInitial();
        world.CreateActivity(new("activity.one"), "worker.001", "test", "planned", ["tree.001"], new("c1"), new("cause1"));
        world.CreateActivity(new("activity.two"), "worker.001", "test", "planned", ["tree.001"], new("c2"), new("cause2"));
        var one = world.AcquireReservation(new("reservation.one"), new("activity.one"), "tree.001", "exclusive", 1, 1, 1);
        var two = world.AcquireReservation(new("reservation.two"), new("activity.two"), "tree.001", "exclusive", 1, 1, 1);
        var released = world.ReleaseReservation(new("reservation.one"), "done");
        var idempotent = world.ReleaseReservation(new("reservation.one"), "again");
        await Assert.That(one.Status).IsEqualTo("accepted");
        await Assert.That(two.Status).IsEqualTo("rejected");
        await Assert.That(released.Status).IsEqualTo("accepted");
        await Assert.That(idempotent.Status).IsEqualTo("accepted");
    }

    [Test]
    public async Task SimulationPersistenceRejectsUnknownComponentWithoutMutatingDestination()
    {
        var world = M031WoodWorkflow.CreateInitial();
        var save = world.Capture();
        var alteredEntity = save.Entities.Single(x => x.Id == "tree.001") with { Components = new SortedDictionary<string, JsonElement>(save.Entities.Single(x => x.Id == "tree.001").Components, StringComparer.Ordinal) { ["component.unknown"] = JsonSerializer.SerializeToElement(new { value = 1 }) } };
        var malformed = save with { Entities = save.Entities.Select(x => x.Id == "tree.001" ? alteredEntity : x).ToArray() };
        var loaded = SimulationWorld.Load(malformed, SimulationFoundationComposition.AddM031WoodWorkflowProofComponents());
        await Assert.That(loaded.Success).IsFalse();
        await Assert.That(loaded.World).IsNull();
        await Assert.That(loaded.Diagnostics.Select(x => x.Code)).Contains("SIMPERSIST0002");
    }

    private static SimulationWorld NewWorld(bool registerInReverse)
    {
        var world = new SimulationWorld(new("world.test"));
        var registrations = new[] { new SimulationComponentRegistration("component.a", 1, PersistenceClassification.AuthoritativePersistent, "test"), new SimulationComponentRegistration("component.b", 1, PersistenceClassification.DerivedRebuildable, "test") };
        foreach (var registration in registerInReverse ? registrations.Reverse() : registrations) world.RegisterComponent(registration);
        world.CreateRegion(new("region.a"), "A"); world.CreateRegion(new("region.b"), "B");
        world.CreateEntity("entity.b", SimulationEntityScope.RegionOwned, new("region.a")); world.ActivateEntity("entity.b"); world.CreateEntity("entity.a", SimulationEntityScope.RegionOwned, new("region.a")); world.ActivateEntity("entity.a");
        return world;
    }
}
