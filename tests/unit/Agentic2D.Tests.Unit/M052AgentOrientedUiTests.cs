using Agentic2D.UI;

namespace Agentic2D.Tests.Unit;

public sealed class M052AgentOrientedUiTests
{
    [Test]
    public async Task NormalizationFreezesDefinitionsAndRejectsInvalidIdentity()
    {
        var source = new List<UiElementDefinition> { Ui.Button("save", "Save", "save") };
        var normalized = UiDefinition.Normalize("consumer", Ui.Screen("root", source.ToArray()));
        source.Add(Ui.Button("cancel", "Cancel", "cancel"));
        await Assert.That(normalized.Root.Items).Count().IsEqualTo(1);
        await Assert.That(() => UiDefinition.Normalize("duplicate", Ui.Screen("root", Ui.Text("same", "one"), Ui.Text("same", "two")))).Throws<ArgumentException>();
        await Assert.That(() => UiDefinition.Normalize("invalid", Ui.Screen("root", Ui.Text("bad", "bad", new UiSizeSpec(UiLength.Fixed(double.NaN), UiLength.Auto))))).Throws<ArgumentException>();
    }

    [Test]
    public async Task LayoutAndPointerTransformRemainDeterministicAcrossUiScale()
    {
        var screen = UiDefinition.Normalize("screen", Ui.Screen("root", Ui.Column("actions", Ui.Button("save", "Save", "save"))));
        var first = UiLayoutEngine.Resolve(screen, new UiViewport(3840, 2160, 2));
        var second = UiLayoutEngine.Resolve(screen, new UiViewport(3840, 2160, 2));
        await Assert.That(first.Fingerprint).IsEqualTo(second.Fingerprint);
        await Assert.That(first.Viewport.ToLogical(800, 400)).IsEqualTo((400d, 200d));
        var session = new UiSession(screen, new UiViewport(3840, 2160, 2));
        var button = session.Layout.Elements.Single(x => x.Id == "save");
        var x = button.PhysicalBounds.X + button.PhysicalBounds.Width / 2; var y = button.PhysicalBounds.Y + button.PhysicalBounds.Height / 2;
        session.Dispatch(new UiInputEvent("pointer-down", x, y)); session.Dispatch(new UiInputEvent("pointer-up", x, y));
        await Assert.That(session.DrainActions().Single().ActionId).IsEqualTo("save");
    }

    [Test]
    public async Task SessionRetainsDraftAndFocusWithoutDuplicateActionsOnRecompose()
    {
        var definition = UiDefinition.Normalize("first", Ui.Screen("root", Ui.Column("controls", Ui.TextField("name", "Name", "name.changed"), Ui.Button("save", "Save", "save"))));
        var session = new UiSession(definition, new UiViewport(1920, 1080));
        var field = session.Layout.Elements.Single(x => x.Id == "name"); var x = field.PhysicalBounds.X + 2; var y = field.PhysicalBounds.Y + 2;
        session.Dispatch(new UiInputEvent("pointer-down", x, y)); session.Dispatch(new UiInputEvent("pointer-up", x, y)); session.Dispatch(new UiInputEvent("text", Text: "Maple!")); session.Dispatch(new UiInputEvent("key", Key: "Backspace"));
        var next = UiDefinition.Normalize("second", Ui.Screen("root", Ui.Column("controls", Ui.TextField("name", "Changed label", "name.changed"), Ui.Button("save", "Save", "save"))));
        session.Recompose(next, new UiViewport(1920, 1080));
        await Assert.That(session.FocusedId).IsEqualTo("name");
        await Assert.That(session.Snapshot().Drafts["name"]).IsEqualTo("Maple");
        var button = session.Layout.Elements.Single(x => x.Id == "save"); x = button.PhysicalBounds.X + 2; y = button.PhysicalBounds.Y + 2;
        session.Dispatch(new UiInputEvent("pointer-down", x, y)); session.Dispatch(new UiInputEvent("pointer-up", x, y));
        await Assert.That(session.DrainActions().Select(a => a.ActionId)).IsEquivalentTo(["save"]);
    }

    [Test]
    public async Task PresentationTimeIsInjectedAndReducedMotionIsStable()
    {
        var animated = UiDefinition.Normalize("animated", Ui.Screen("root", Ui.Busy("busy", "Saving", new("save", AgentUiOperationStatus.Working))), reducedMotion: false);
        var reduced = animated with { ReducedMotion = true };
        var animatedSession = new UiSession(animated, new UiViewport(1920, 1080)); var reducedSession = new UiSession(reduced, new UiViewport(1920, 1080));
        animatedSession.Dispatch(new UiInputEvent("time", Delta: .5)); reducedSession.Dispatch(new UiInputEvent("time", Delta: .5));
        await Assert.That(animatedSession.PresentationSamples().Single(x => x.ElementId == "busy").Pulse).IsNotEqualTo(reducedSession.PresentationSamples().Single(x => x.ElementId == "busy").Pulse);
        await Assert.That(reducedSession.PresentationSamples().Single(x => x.ElementId == "busy").Pulse).IsEqualTo(1d);
    }
}
