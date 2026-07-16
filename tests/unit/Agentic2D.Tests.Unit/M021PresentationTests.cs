using Agentic2D.Presentation;
using Agentic2D.UI;

namespace Agentic2D.Tests.Unit;

public sealed class M021PresentationTests
{
    [Test]
    public async Task EffectRequestsAndInstancesUseEventIdentityAndRuntimeTicks()
    {
        var projector = new EffectProjector();
        var events = new[] { new PresentationEvent("event.damage.1", "entity.damaged", 4, "hazard", "player", "2,3", "post-commit") };
        var request = projector.Requests(events, "seed").Single();
        var definition = new EffectDefinition("effect.damage-feedback", 3, "world", null, null, true, "test");
        var first = projector.Instances([request], new Dictionary<string, EffectDefinition> { [definition.Id] = definition }, 6).Single();
        var second = projector.Instances([request], new Dictionary<string, EffectDefinition> { [definition.Id] = definition }, 6).Single();

        await Assert.That(request.SourceEventOrOperationId).IsEqualTo("event.damage.1");
        await Assert.That(first.InstanceId).IsEqualTo(second.InstanceId);
        await Assert.That(first.CurrentAge).IsEqualTo(2);
        await Assert.That(first.State).IsEqualTo("active");
    }

    [Test]
    public async Task ParticleSpawnsAndSamplesAreReplayStable()
    {
        var emitter = new ParticleEmitterDefinition("emitter.test", "visual.test", "part.test", 2, 1, 4, [-1d, -1d], [1d, 1d], [-1d, -1d], [1d, 1d], [.5d, 1d], [0d, 360d], [-1d, 1d], [1, 1, 1, 1], [255, 255, 255, 255], "linear-inverse", "linear-inverse", "effects");
        var effect = new EffectInstance("instance.test", "effect.collection-burst", "request.test", "event.collection.test", 2, 4, "seed", 0, "active", [], "fingerprint");
        var first = ParticleProjector.Spawn(emitter, effect, 2, "0,0", "scenario");
        var second = ParticleProjector.Spawn(emitter, effect, 2, "0,0", "scenario");

        await Assert.That(first).IsEquivalentTo(second);
        await Assert.That(ParticleProjector.Sample(first, 3, "linear-inverse", "linear-inverse")).IsEquivalentTo(ParticleProjector.Sample(second, 3, "linear-inverse", "linear-inverse"));
    }

    [Test]
    public async Task BindingsAreFiniteAndPreparedStateOnly()
    {
        var state = new PreparedPresentationState(5, 10, 1, new Dictionary<string, int> { ["item.crystal"] = 2 }, true, false, "text.prompt.locked-door", "condition.locked", false, null, "created", new Dictionary<string, string> { ["door.vault"] = "closed" }, new Dictionary<string, string>());
        await Assert.That(SemanticBindings.Resolve("player.health.normalized", state).Value).IsEqualTo(.5d);
        await Assert.That(SemanticBindings.Resolve("player.inventory.item-count:item.crystal", state).Value).IsEqualTo(2);
        await Assert.That(() => SemanticBindings.Resolve("player.health.current.untrusted", state)).Throws<InvalidOperationException>();
    }
}
