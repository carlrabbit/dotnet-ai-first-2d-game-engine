using System.Text.Json;
using Agentic2D.Tools;
using Agentic2D.UI;

namespace Agentic2D.Tests.Unit;

public sealed class M053RealWorkbenchUiTests
{
    private static readonly UiViewport[] DesktopMatrix =
    [
        new(1280, 720), new(1920, 1080), new(2560, 1440, 1.25),
        new(3840, 2160, 2), new(3440, 1440, 1.5), new(2560, 1440, 1, 1.25)
    ];

    [Test]
    public async Task ActualReviewAssetAndPreviewCompositionsResolveAtDesktopTargets()
    {
        var question = "Status: PENDING was previously drawn over navigation. " + string.Join(' ', Enumerable.Repeat("The complete subjective review question must remain readable and separate from its status and navigation controls.", 5));
        var review = M053WorkbenchScreens.Review(new("M053", "review.m053.sample", question, 1, 4, "pending", "saving", "Decision is being persisted", true, false, false, true, false));
        var assets = M053WorkbenchScreens.AssetWorkbench(new("session.m053", 7, Enumerable.Range(1, 30).Select(i => "candidate.with.a.long.identity." + i).ToArray(), "candidate.with.a.long.identity.1", "", "Current provider session"));
        var previews = new[] { "image", "animation", "audio", "unknown" }.Select(CreatePreview).ToArray();

        foreach (var viewport in DesktopMatrix)
        {
            foreach (var screen in new[] { review, assets }.Concat(previews))
            {
                var first = UiLayoutEngine.Resolve(screen, viewport);
                var second = UiLayoutEngine.Resolve(screen, viewport);
                await Assert.That(first.Fingerprint).IsEqualTo(second.Fingerprint);
                await Assert.That(first.Diagnostics.Where(diagnostic => diagnostic.Severity == "error").Select(diagnostic => diagnostic.Code + ":" + diagnostic.ElementId)).IsEmpty();
            }
        }

        var reviewLayout = UiLayoutEngine.Resolve(review, new UiViewport(1920, 1080));
        var status = reviewLayout.Elements.Single(element => element.Id == "review.status");
        var questionItem = reviewLayout.Elements.Single(element => element.Id == "review.question");
        var navigation = reviewLayout.Elements.Where(element => element.Id is "review.previous" or "review.ordinal" or "review.next").ToArray();
        await Assert.That(navigation.Any(element => status.LogicalBounds.Intersects(element.LogicalBounds))).IsFalse();
        await Assert.That(status.LogicalBounds.Intersects(questionItem.LogicalBounds)).IsFalse();
    }

    [Test]
    public async Task AssetWorkbenchDraftClipboardClearAndSubmitKeepExplicitCommandBoundary()
    {
        var screen = M053WorkbenchScreens.AssetWorkbench(new("session.m053", 12, ["candidate.a", "candidate.b"], "candidate.a", "", "Ready"));
        var session = new UiSession(screen, new UiViewport(1920, 1080));
        var field = session.Layout.Elements.Single(element => element.Id == "asset-workbench.command");
        var x = field.PhysicalBounds.X + 4; var y = field.PhysicalBounds.Y + 4;
        session.Dispatch(new UiInputEvent("pointer-down", x, y)); session.Dispatch(new UiInputEvent("pointer-up", x, y));
        session.Dispatch(new UiInputEvent("text", Text: "12"));
        await Assert.That(session.DrainActions()).IsEmpty();
        await Assert.That(session.UpdateTextDraft("asset-workbench.command", "12 pasted")).IsTrue();
        await Assert.That(session.DrainActions()).IsEmpty();
        await Assert.That(session.UpdateTextDraft("asset-workbench.command", string.Empty)).IsTrue();
        await Assert.That(session.Snapshot().Drafts["asset-workbench.command"]).IsEqualTo(string.Empty);
        session.UpdateTextDraft("asset-workbench.command", "choice 2");
        session.Dispatch(new UiInputEvent("key", Key: "Enter"));
        var request = session.DrainActions().Single();
        await Assert.That(request.ActionId).IsEqualTo("asset-workbench.submit");
        await Assert.That(request.SourceId).IsEqualTo("asset-workbench.command");
        await Assert.That(request.Value).IsEqualTo("choice 2");
    }

    [Test]
    public async Task AssetWorkbenchResizeRecompositionRetainsDraftFocusAndScrollButCancelsPointerCapture()
    {
        var candidates = Enumerable.Range(1, 30).Select(i => "candidate.resize." + i).ToArray();
        var projection = new M053WorkbenchScreens.AssetProjection("session.resize", 4, candidates, candidates[0], "", "Ready");
        var session = new UiSession(M053WorkbenchScreens.AssetWorkbench(projection), new UiViewport(1280, 720));

        var candidateViewport = session.Layout.Elements.Single(element => element.Id == "asset-workbench.candidate-scroll").PhysicalBounds;
        session.Dispatch(new UiInputEvent("scroll", candidateViewport.X + 4, candidateViewport.Y + 4, Delta: 120));
        var scrollBefore = session.Snapshot().ScrollOffsets["asset-workbench.candidate-scroll"];
        await Assert.That(scrollBefore > 0).IsTrue();

        var command = session.Layout.Elements.Single(element => element.Id == "asset-workbench.command");
        var x = command.PhysicalBounds.X + command.PhysicalBounds.Width / 2;
        var y = command.PhysicalBounds.Y + command.PhysicalBounds.Height / 2;
        session.Dispatch(new UiInputEvent("pointer-down", x, y));
        session.Dispatch(new UiInputEvent("pointer-up", x, y));
        session.UpdateTextDraft("asset-workbench.command", "choice 27");
        await Assert.That(session.FocusedId).IsEqualTo("asset-workbench.command");

        command = session.Layout.Elements.Single(element => element.Id == "asset-workbench.command");
        x = command.PhysicalBounds.X + command.PhysicalBounds.Width / 2;
        y = command.PhysicalBounds.Y + command.PhysicalBounds.Height / 2;
        session.Dispatch(new UiInputEvent("pointer-down", x, y));
        await Assert.That(session.CapturedId).IsEqualTo("asset-workbench.command");

        session.Recompose(M053WorkbenchScreens.AssetWorkbench(projection with { Feedback = "Resized" }), new UiViewport(1600, 900));
        session.Dispatch(new UiInputEvent("pointer-up", x, y));
        var snapshot = session.Snapshot();
        await Assert.That(snapshot.FocusedId).IsEqualTo("asset-workbench.command");
        await Assert.That(snapshot.Drafts["asset-workbench.command"]).IsEqualTo("choice 27");
        await Assert.That(snapshot.ScrollOffsets["asset-workbench.candidate-scroll"]).IsEqualTo(scrollBefore);
        await Assert.That(snapshot.CapturedId).IsNull();
        await Assert.That(session.DrainActions()).IsEmpty();
    }

    [Test]
    public async Task PreviewModalityUsesStructuredKindAndOnlyItsOwnControls()
    {
        var image = CreateSurface("image", "review.id.audio-is-a-trap", "candidate.animation-is-a-trap");
        var animation = CreateSurface("animation", "review.id.image-is-a-trap", "candidate.audio-is-a-trap");
        var audio = CreateSurface("audio", "review.id.image-is-a-trap", "candidate.animation-is-a-trap");
        var unknown = CreateSurface("unsupported-kind", "review.id.image", "candidate.audio");
        await Assert.That(image.Kind).IsEqualTo(PreviewSurfaceKind.Image);
        await Assert.That(image.Controls).IsEquivalentTo(["Source", "Processed", "Isolated region", "Source context", "Nearest", "Smooth", "Overlays"]);
        await Assert.That(animation.Controls).IsEquivalentTo(["Play", "Pause", "Step", "Reset", "0.5x", "1x", "2x"]);
        await Assert.That(audio.Controls).IsEquivalentTo(["Play Raw", "Play Processed", "Stop"]);
        await Assert.That(unknown.Kind).IsEqualTo(PreviewSurfaceKind.Unsupported);
        await Assert.That(unknown.Controls).IsEmpty();
    }

    [Test]
    public async Task InvalidOverlapOversizeTextDisabledActionAndStalePointerAreRejected()
    {
        var overlap = UiDefinition.Normalize("broken.overlap", Ui.Screen("root", Ui.Grid("overlap",
            [new UiGridTrack(UiLength.Grow())], [new UiGridTrack(UiLength.Grow())],
            Ui.Button("previous", "Previous", "review.previous") with { Row = 0, Column = 0 },
            Ui.Button("accept", "Accept", "review.accept") with { Row = 0, Column = 0 })));
        var overlapLayout = UiLayoutEngine.Resolve(overlap, new UiViewport(1920, 1080));
        await Assert.That(overlapLayout.Diagnostics.Any(diagnostic => diagnostic.Code == "UI2001")).IsTrue();

        var overlong = UiDefinition.Normalize("broken.context", Ui.Screen("root",
            Ui.Text("oversized", string.Join(' ', Enumerable.Repeat("unbounded context", 300)), new UiSizeSpec(UiLength.Fixed(100), UiLength.Fixed(32)))));
        var overflowLayout = UiLayoutEngine.Resolve(overlong, new UiViewport(1280, 720));
        await Assert.That(overflowLayout.Diagnostics.Any(diagnostic => diagnostic.Code is "UI2003" or "UI2004")).IsTrue();

        var disabled = UiDefinition.Normalize("broken.disabled", Ui.Screen("root", Ui.Button("action", "Unavailable", "danger", enabled: false)));
        var session = new UiSession(disabled, new UiViewport(1920, 1080));
        var target = session.Layout.Elements.Single(element => element.Id == "action");
        var x = target.PhysicalBounds.X + 2; var y = target.PhysicalBounds.Y + 2;
        session.Dispatch(new UiInputEvent("pointer-down", x, y)); session.Dispatch(new UiInputEvent("pointer-up", x, y));
        await Assert.That(session.DrainActions()).IsEmpty();
        session.Dispatch(new UiInputEvent("pointer-down", target.PhysicalBounds.X + target.PhysicalBounds.Width + 2, y));
        session.Dispatch(new UiInputEvent("pointer-up", target.PhysicalBounds.X + target.PhysicalBounds.Width + 2, y));
        await Assert.That(session.DrainActions()).IsEmpty();
    }

    private static UiDefinition CreatePreview(string kind)
    {
        var surface = CreateSurface(kind, "review.fixture", "candidate.fixture");
        return M053WorkbenchScreens.AssetPreview(new(surface, "media.fixture", "Current media", "Paused", 0, 3, false, "Manual playback; device not attempted."));
    }

    private static PreviewSurface CreateSurface(string kind, string reviewId, string candidateId)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new
        {
            mediaKind = kind,
            reviewId,
            reviewContext = new
            {
                modality = "structured modality",
                candidateId,
                presentationRole = "candidate role",
                selectedVariant = "variant.1",
                corrections = new[] { "correction.one", "correction.two" },
                rawMeaning = "raw/base",
                processedMeaning = "processed/current draft",
                subjectiveQuestion = "Is this candidate clear for its intended purpose?",
                rawDurationSamples = 1200,
                processedDurationSamples = 900,
                sampleRate = 8000
            }
        }));
        return AssetPreviewSurfaceModel.FromBundle(document.RootElement);
    }
}
