using Agentic2D.Engine;
using Agentic2D.Gameplay;
using Agentic2D.Sound;

namespace Agentic2D.Tests.Unit;

public sealed class M019GameplayRuntimeTests
{
    [Test]
    public async Task DamageIsCappedDefeatsOnceAndSuppressesNormalBehavior()
    {
        var runtime = new M019GameplayRuntime();
        runtime.CreateEntity("entity.target", 0, new ResourceHealth(GameplayIds.Health, 3, 0, 3, 0));
        var first = runtime.ApplyDamage(new DamageIntent("damage.1", "entity.player", "entity.target", "damage.generic", 99, 1, "correlation.1", "test"));
        var second = runtime.ApplyDamage(new DamageIntent("damage.2", "entity.player", "entity.target", "damage.generic", 1, 2, "correlation.2", "test"));

        await Assert.That(first.AppliedAmount).IsEqualTo(3);
        await Assert.That(second.RejectionReason).IsEqualTo("already-defeated");
        await Assert.That(runtime.Events.Count(x => x.Type == "entity.defeated")).IsEqualTo(1);
        await Assert.That(runtime.World.TryGet<RuntimeLifecycle>("entity.target", out var lifecycle) && !lifecycle!.NormalBehaviorEnabled).IsTrue();
    }

    [Test]
    public async Task CollectionCommitsInventoryAndEntityRemovalTogether()
    {
        var runtime = new M019GameplayRuntime();
        runtime.RegisterItem(new ItemDefinitionSource { Schema = "agentic2d.item-definition.v1", Id = "item.collectible-crystal", Stackable = true, MaximumStack = 10, VisualDefinitionId = "visual-definition.player.basic", DefaultCollectionCue = "cue.item.collection" });
        runtime.CreateEntity("entity.player", 0, inventory: new Inventory("inventory.player", 1, [], 0));
        runtime.CreateEntity("entity.item", 0, worldItem: new WorldItem("item.collectible-crystal", 2, 0));
        var resolution = runtime.Collect(new CollectItemIntent("collect.1", "entity.player", "entity.item", "item.collectible-crystal", 1, "correlation.collection.1", "test"));

        await Assert.That(resolution.Status).IsEqualTo("accepted");
        await Assert.That(runtime.World.Exists("entity.item")).IsFalse();
        await Assert.That(runtime.World.TryGet<Inventory>("entity.player", out var inventory) && inventory!.Entries.Single().Quantity == 2).IsTrue();
        await Assert.That(runtime.Events.Select(x => x.Type)).Contains("item.collected");
    }

    [Test]
    public async Task SoundVariantsAndCommandOrderingAreStable()
    {
        var catalog = SoundContent.LoadAll();
        var projector = new SoundProjector(catalog.Definitions);
        var first = projector.Project(2, [
            (new CueRequest("cue.entity.damage", "event", "entity.b", 2, 1, "seed"), "entity.damaged"),
            (new CueRequest("cue.player.footstep", "marker", "entity.a", 2, 0, "seed"), "presentation.footstep")]);
        var second = new SoundProjector(catalog.Definitions).Project(2, [
            (new CueRequest("cue.entity.damage", "event", "entity.b", 2, 1, "seed"), "entity.damaged"),
            (new CueRequest("cue.player.footstep", "marker", "entity.a", 2, 0, "seed"), "presentation.footstep")]);

        await Assert.That(first.Fingerprint).IsEqualTo(second.Fingerprint);
        await Assert.That(first.Selections[0].CueId).IsEqualTo("cue.player.footstep");
    }
}
