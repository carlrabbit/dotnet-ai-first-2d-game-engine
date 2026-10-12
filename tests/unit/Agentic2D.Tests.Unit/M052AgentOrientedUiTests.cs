using Agentic2D.UI;
using Agentic2D.Tools;

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

    [Test]
    public async Task PointerTransformAndVisibleHitGeometryAgreeAtFractionalUiAndDpiScales()
    {
        var definition = UiDefinition.Normalize("scaled", Ui.Screen("root", Ui.Margin("gutter", UiInsets.All(40), Ui.Button("go", "Go", "go"))));
        foreach (var uiScale in new[] { 1d, 1.25, 1.5, 2d })
        {
            var session = new UiSession(definition, new UiViewport(3840, 2160, uiScale, 1.25));
            var button = session.Layout.Elements.Single(x => x.Id == "go");
            var pointX = button.PhysicalBounds.X + button.PhysicalBounds.Width / 2;
            var pointY = button.PhysicalBounds.Y + button.PhysicalBounds.Height / 2;
            var logical = session.Layout.Viewport.ToLogical(pointX, pointY);
            await Assert.That(button.VisibleBounds.Contains(pointX, pointY)).IsTrue();
            await Assert.That(button.LogicalBounds.Contains(logical.X, logical.Y)).IsTrue();
            session.Dispatch(new UiInputEvent("pointer-down", pointX, pointY));
            session.Dispatch(new UiInputEvent("pointer-up", pointX, pointY));
            await Assert.That(session.DrainActions().Single().SourceId).IsEqualTo("go");
        }
    }

    [Test]
    public async Task ModalPanelAndChildrenShareResolvedGeometryAndBlockOutsideClicks()
    {
        var modal = Ui.Modal("dialog", "Confirm", Ui.Column("dialog.content", Ui.Text("dialog.message", "Confirm this action."), Ui.Button("dialog.confirm", "Confirm", "confirm")), "dialog.cancel");
        var definition = UiDefinition.Normalize("modal", Ui.Screen("root", Ui.Overlay("stack", Ui.Button("underlay", "Behind", "underlay"), modal with { Visible = true })));
        var session = new UiSession(definition, new UiViewport(1920, 1080, 1.25, 1.25));
        await Assert.That(session.FocusedId).IsEqualTo("dialog.confirm");
        var modalItem = session.Layout.Elements.Single(x => x.Id == "dialog");
        var panel = modalItem.ModalPanelBounds!;
        var confirm = session.Layout.Elements.Single(x => x.Id == "dialog.confirm");
        await Assert.That(confirm.PhysicalBounds.X >= panel.X).IsTrue();
        await Assert.That(confirm.PhysicalBounds.X + confirm.PhysicalBounds.Width <= panel.X + panel.Width + .01).IsTrue();
        await Assert.That(confirm.PhysicalBounds.Y >= panel.Y).IsTrue();
        await Assert.That(confirm.PhysicalBounds.Y + confirm.PhysicalBounds.Height <= panel.Y + panel.Height + .01).IsTrue();
        var xOutside = 20d; var yOutside = 20d;
        session.Dispatch(new UiInputEvent("pointer-down", xOutside, yOutside)); session.Dispatch(new UiInputEvent("pointer-up", xOutside, yOutside));
        await Assert.That(session.DrainActions().Count).IsEqualTo(0);
        var projection = UiDrawProjection.Create(session).Single(x => x.ElementId == "dialog.confirm");
        await Assert.That(projection.Bounds).IsEqualTo(confirm.PhysicalBounds);
        await Assert.That(projection.ClipBounds).IsEqualTo(confirm.ClipBounds);
        session.Dispatch(new UiInputEvent("key", Key: "Escape"));
        await Assert.That(session.DrainActions().Single().ActionId).IsEqualTo("dialog.cancel");
        var closedDefinition = definition with { Root = definition.Root with { Children = [definition.Root.Items[0] with { Children = [definition.Root.Items[0].Items[0], modal with { Visible = false }] }] } };
        session.Recompose(closedDefinition, session.Layout.Viewport);
        await Assert.That(session.FocusedId).IsEqualTo("underlay");
    }

    [Test]
    public async Task ScrollUsesStableContentExtentAcrossBothDirectionsAndRecomposition()
    {
        var definition = UiDefinition.Normalize("scroll", Ui.Screen("root", Ui.Scroll("viewport", Ui.Column("content",
            Ui.Button("first", "First", "first"), Ui.Button("second", "Second", "second"), Ui.Button("third", "Third", "third"), Ui.Button("fourth", "Fourth", "fourth"), Ui.Button("fifth", "Fifth", "fifth"))) with
        { Size = UiSizeSpec.Fixed(400, 200) }));
        var session = new UiSession(definition, new UiViewport(1280, 720));
        var rangeBefore = session.Layout.Elements.Where(x => x.Id is "first" or "second" or "third" or "fourth" or "fifth").Max(x => (x.ContentBounds ?? x.LogicalBounds).Y + (x.ContentBounds ?? x.LogicalBounds).Height)
            - session.Layout.Elements.Single(x => x.Id == "viewport").LogicalBounds.Height;
        var viewport = session.Layout.Elements.Single(x => x.Id == "viewport").PhysicalBounds;
        session.Dispatch(new UiInputEvent("scroll", viewport.X + 4, viewport.Y + 4, Delta: 50));
        var down = session.Snapshot().ScrollOffsets["viewport"];
        var rangeAfter = session.Layout.Elements.Where(x => x.Id is "first" or "second" or "third" or "fourth" or "fifth").Max(x => (x.ContentBounds ?? x.LogicalBounds).Y + (x.ContentBounds ?? x.LogicalBounds).Height)
            - session.Layout.Elements.Single(x => x.Id == "viewport").LogicalBounds.Height;
        await Assert.That(down).IsEqualTo(50d);
        await Assert.That(rangeAfter).IsEqualTo(rangeBefore);
        session.Dispatch(new UiInputEvent("scroll", viewport.X + 4, viewport.Y + 4, Delta: -20));
        await Assert.That(session.Snapshot().ScrollOffsets["viewport"]).IsEqualTo(30d);
        session.Recompose(definition with { Id = "scroll.recomposed" }, new UiViewport(1280, 720));
        await Assert.That(session.Snapshot().ScrollOffsets["viewport"]).IsEqualTo(30d);
        var projected = UiDrawProjection.Create(session).Single(x => x.ElementId == "third");
        await Assert.That(projected.Bounds).IsEqualTo(session.Layout.Elements.Single(x => x.Id == "third").PhysicalBounds);
        await Assert.That(projected.ClipBounds).IsEqualTo(session.Layout.Elements.Single(x => x.Id == "third").ClipBounds);
    }

    [Test]
    public async Task ScreenReplacementSuppressesCapturedPointerAndQueuedActions()
    {
        var screen = UiDefinition.Normalize("first", Ui.Screen("root", Ui.Button("save", "Save", "save")));
        var session = new UiSession(screen, new UiViewport(1280, 720));
        var save = session.Layout.Elements.Single(x => x.Id == "save");
        var x = save.PhysicalBounds.X + save.PhysicalBounds.Width / 2; var y = save.PhysicalBounds.Y + save.PhysicalBounds.Height / 2;
        session.Dispatch(new UiInputEvent("pointer-down", x, y));
        var replacement = UiDefinition.Normalize("replacement", Ui.Screen("new-root", Ui.Button("other", "Other", "other")));
        session.Recompose(replacement, new UiViewport(1280, 720));
        session.Dispatch(new UiInputEvent("pointer-up", x, y));
        await Assert.That(session.CapturedId).IsNull();
        await Assert.That(session.DrainActions().Count).IsEqualTo(0);
    }

    [Test]
    public async Task ConstraintsAndEssentialContentCreateMechanicalErrors()
    {
        var wideA = Ui.Button("a", "A", "a") with { Size = new UiSizeSpec(UiLength.Fixed(900), UiLength.Fixed(48), MinWidth: 900) };
        var wideB = Ui.Button("b", "B", "b") with { Size = new UiSizeSpec(UiLength.Fixed(900), UiLength.Fixed(48), MinWidth: 900) };
        var row = UiDefinition.Normalize("row", Ui.Screen("root", Ui.Row("row", wideA, wideB)));
        var rowLayout = UiLayoutEngine.Resolve(row, new UiViewport(1280, 720));
        await Assert.That(rowLayout.Diagnostics.Any(x => x.Code == "UI2004")).IsTrue();
        var constrainedControl = Ui.Button("go", "Go", "go") with { Size = new UiSizeSpec(UiLength.Grow(), UiLength.Grow(), MinWidth: 900, MinHeight: 600) };
        var grid = UiDefinition.Normalize("grid", Ui.Screen("root", Ui.Grid("grid", [new(UiLength.Grow(), Minimum: 700), new(UiLength.Grow(), Minimum: 700)], [new(UiLength.Grow(), Minimum: 400), new(UiLength.Grow(), Minimum: 400)], constrainedControl)));
        var gridLayout = UiLayoutEngine.Resolve(grid, new UiViewport(1280, 720));
        await Assert.That(gridLayout.Diagnostics.Any(x => x.Code == "UI2004" || x.Code == "UI2006")).IsTrue();
        var fixedBeyondMaximum = Ui.Button("bounded", "Bounded", "bounded") with { Size = new UiSizeSpec(UiLength.Fixed(120), UiLength.Fixed(48), MaxWidth: 80) };
        var bounded = UiDefinition.Normalize("bounded-track", Ui.Screen("root", Ui.Grid("grid", [new(UiLength.Fixed(100), Maximum: 80)], [new(UiLength.Grow())], fixedBeyondMaximum)));
        var boundedLayout = UiLayoutEngine.Resolve(bounded, new UiViewport(1280, 720));
        await Assert.That(boundedLayout.Diagnostics.Any(x => x.Code == "UI2006" && x.ElementId is "grid" or "bounded")).IsTrue();
        var tooSmall = UiDefinition.Normalize("text", Ui.Screen("root", Ui.Text("required", "Critical information that cannot fit in this fixed panel.", UiSizeSpec.Fixed(40, 18))));
        var textLayout = UiLayoutEngine.Resolve(tooSmall, new UiViewport(1280, 720));
        await Assert.That(textLayout.Diagnostics.Any(x => x.Code == "UI2003" && x.Severity == "error" && x.ElementId == "required")).IsTrue();
    }

    [Test]
    public async Task FontMetricsUsePinnedRaylibGlyphAdvancesAndFallbackForUnicodeAndMultilineText()
    {
        var metrics = Ui.Theme.DefaultMetrics;
        await Assert.That(metrics.Measure("Mi", 10)).IsEqualTo(9d);
        await Assert.That(metrics.Measure("🚀", 10)).IsEqualTo(metrics.Measure("?", 10));
        await Assert.That(metrics.Measure("WW\ni", 10)).IsEqualTo(15d);
        var lines = metrics.Wrap("WWWW\ni🚀", 15, 10);
        await Assert.That(lines.Count).IsEqualTo(3);
        await Assert.That(lines[2]).IsEqualTo("i?");
    }

    [Test]
    public async Task EditedTextOverflowIsDiagnosedAndDrawProjectionKeepsControlClip()
    {
        var field = Ui.TextField("notes", "Notes", "notes.changed") with { Size = UiSizeSpec.Fixed(90, 48) };
        var definition = UiDefinition.Normalize("text-field-overflow", Ui.Screen("root", field));
        var session = new UiSession(definition, new UiViewport(1280, 720));
        session.Dispatch(new UiInputEvent("text", Text: "WWWWWWWW"));
        await Assert.That(session.Layout.Diagnostics.Any(x => x.Code == "UI2003" && x.Severity == "error" && x.ElementId == "notes")).IsTrue();
        var command = UiDrawProjection.Create(session).Single(x => x.ElementId == "notes");
        await Assert.That(command.Text).IsEqualTo("WWWWWWWW");
        await Assert.That(command.VisibleBounds).IsEqualTo(session.Layout.Elements.Single(x => x.Id == "notes").VisibleBounds);
        await Assert.That(command.ClipBounds).IsEqualTo(session.Layout.Elements.Single(x => x.Id == "notes").ClipBounds);
        await Assert.That(command.LetterSpacing).IsEqualTo(session.Definition.FontMetrics.Spacing(session.Definition.Theme.FontSize));
    }

    [Test]
    public async Task TextFieldSupportsRuneSafeCursorInsertionDeletionAndFocusTraversal()
    {
        var definition = UiDefinition.Normalize("text-edit", Ui.Screen("root", Ui.Column("controls", Ui.TextField("notes", "Notes", "notes.changed", "A🚀C"), Ui.Button("save", "Save", "save"))));
        var session = new UiSession(definition, new UiViewport(1280, 720));
        session.Dispatch(new UiInputEvent("key", Key: "Left"));
        session.Dispatch(new UiInputEvent("text", Text: "é"));
        session.Dispatch(new UiInputEvent("key", Key: "Delete"));
        await Assert.That(session.Snapshot().Drafts["notes"]).IsEqualTo("A🚀é");
        session.Dispatch(new UiInputEvent("key", Key: "Tab"));
        await Assert.That(session.FocusedId).IsEqualTo("save");
        session.Dispatch(new UiInputEvent("key", Key: "Tab"));
        await Assert.That(session.FocusedId).IsEqualTo("notes");
    }

    [Test]
    public async Task SyntheticApplicationOperationRejectsDuplicatesAndRequiresConfirmedCompletion()
    {
        var controller = new M052SyntheticReviewOperationController([AgentUiOperationStatus.Failed, AgentUiOperationStatus.Succeeded]);
        await Assert.That(controller.Start()).IsTrue();
        var firstAttempt = controller.Attempt;
        await Assert.That(controller.Projection.Status).IsEqualTo(AgentUiOperationStatus.Working);
        await Assert.That(controller.Start()).IsFalse();
        await Assert.That(controller.Advance(.34)).IsFalse();
        await Assert.That(controller.Projection.Status).IsEqualTo(AgentUiOperationStatus.Working);
        await Assert.That(controller.Advance(.02)).IsTrue();
        await Assert.That(controller.Projection.Status).IsEqualTo(AgentUiOperationStatus.Failed);
        await Assert.That(controller.Projection.RetryActionId).IsEqualTo("stress.retry");
        await Assert.That(controller.Retry()).IsTrue();
        await Assert.That(controller.Attempt).IsEqualTo(firstAttempt + 1);
        await Assert.That(controller.Retry()).IsFalse();
        await Assert.That(controller.Advance(.35)).IsTrue();
        await Assert.That(controller.Projection.Status).IsEqualTo(AgentUiOperationStatus.Succeeded);
        await Assert.That(controller.Retry()).IsFalse();

        var cancelled = new M052SyntheticReviewOperationController([AgentUiOperationStatus.Failed]);
        await Assert.That(cancelled.Start()).IsTrue();
        await Assert.That(cancelled.Cancel()).IsTrue();
        await Assert.That(cancelled.Projection.Status).IsEqualTo(AgentUiOperationStatus.Cancelled);
        await Assert.That(cancelled.Advance(1)).IsFalse();
        await Assert.That(cancelled.Retry()).IsTrue();
        await Assert.That(cancelled.Advance(.35)).IsTrue();
        await Assert.That(cancelled.Projection.Status).IsEqualTo(AgentUiOperationStatus.Failed);

        var exhausted = new M052SyntheticReviewOperationController([AgentUiOperationStatus.Failed]);
        await Assert.That(exhausted.Start()).IsTrue();
        await Assert.That(exhausted.Advance(.35)).IsTrue();
        await Assert.That(exhausted.CanRetry).IsFalse();
        await Assert.That(exhausted.Projection.RetryActionId).IsNull();
        await Assert.That(exhausted.Projection.Message).Contains("No retry remains");
        await Assert.That(exhausted.Retry()).IsFalse();
        var exhaustedUi = new UiSession(M052UiScreens.ReviewStress(exhausted.Projection), new UiViewport(1920, 1080));
        await Assert.That(exhaustedUi.Layout.Elements.Single(x => x.Id == "stress.retry").Enabled).IsFalse();
    }
}
