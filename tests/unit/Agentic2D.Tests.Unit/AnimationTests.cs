using Agentic2D.Animation;

namespace Agentic2D.Tests.Unit;

public sealed class AnimationTests
{
    private static readonly CompiledAnimation Animation = new(
        "agentic2d.compiled-animation.v1", "animation-definition.test", "visual-definition.test",
        [new("clip.once", 3, "once", [new("track.opacity", "part.test", "visual.opacity", "scalar", "linear", [new("key.0", 0, 0d, null), new("key.2", 2, 1d, null)])], [new("marker.zero", 0, "presentation.debug", new Dictionary<string, System.Text.Json.JsonElement>()), new("marker.two", 2, "presentation.effect", new Dictionary<string, System.Text.Json.JsonElement>())])], "test");

    [Test]
    public async Task OnceClipCompletesAndHoldsItsFinalTick()
    {
        var sample = new AnimationSampler().Sample(Animation, new AnimationSelection("base", "clip.once", "key.1", "test", 10), 14);
        await Assert.That(sample.Playback.Status).IsEqualTo("completed");
        await Assert.That(sample.Playback.LocalTick).IsEqualTo(2);
        await Assert.That(sample.Patches.Single().Scalar).IsEqualTo(1d);
    }

    [Test]
    public async Task SelectionKeyPreservesStartAndNewKeyRestarts()
    {
        var selections = new AnimationSelections();
        selections.SelectBaseClip("clip.once", "key.a", "first", 2);
        selections.SelectBaseClip("clip.once", "key.a", "same", 5);
        await Assert.That(selections.Base!.StartedAtRuntimeTick).IsEqualTo(2);
        selections.RestartBaseClip("clip.once", "key.b", "restart", 5);
        await Assert.That(selections.Base!.StartedAtRuntimeTick).IsEqualTo(5);
    }

    [Test]
    public async Task TickZeroAndMultiTickMarkersAreAllObserved()
    {
        var selection = new AnimationSelection("overlay", "clip.once", "key.1", "test", 0);
        var markers = new AnimationSampler().Markers(Animation, selection, null, 2, "source.test");
        await Assert.That(markers.Select(x => x.MarkerId)).IsEquivalentTo(["marker.zero", "marker.two"]);
    }
}
