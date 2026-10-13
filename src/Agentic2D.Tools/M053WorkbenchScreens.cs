using Agentic2D.UI;

namespace Agentic2D.Tools;

/// <summary>Renderer-independent application projections for the real M053 workbenches.</summary>
public static class M053WorkbenchScreens
{
    public sealed record ReviewProjection(string Milestone, string ItemId, string Question, int Ordinal, int Count,
        string Status, string OperationStatus, string Feedback, bool CanDecide, bool CanRetry, bool CanClose,
        bool CanLaunchPreview, bool CanLaunchStress, bool FinalPage = false, bool Resetting = false,
        IReadOnlyList<ReviewDecisionSummary>? DecisionSummary = null, int PendingCount = 0, bool Closing = false);

    public sealed record ReviewDecisionSummary(int Ordinal, string Decision, string Persistence, string Error = "");

    public sealed record AssetProjection(string SessionId, int AliasGeneration, IReadOnlyList<string> CandidateIds,
        string? ActiveCandidateId, string Draft, string Feedback);

    public sealed record PreviewProjection(PreviewSurface Surface, string MediaReference, string MediaStatus,
        string PlaybackStatus, int FrameIndex, int FrameCount, bool Playing, string AudioDiagnostic);

    public static UiDefinition Review(ReviewProjection state)
    {
        var experiences = new List<UiElementDefinition>
        {
            Ui.Text("review.experience", state.FinalPage ? "Ordered review decision summary" : "Review the actual experience for this question. Preview is optional; decisions remain available.")
        };
        if (state.CanLaunchPreview) experiences.Add(Ui.Button("review.preview", "Launch actual experience", "review.preview", !state.FinalPage && !state.Resetting && !state.Closing));
        if (state.CanLaunchStress) experiences.Add(Ui.Button("review.stress", "Launch UI stress screen", "review.stress", !state.FinalPage && !state.Resetting && !state.Closing));
        var children = new List<UiElementDefinition>
        {
            Ui.Title("review.title", "Simple Review Workbench"),
            Ui.Text("review.milestone", state.Milestone),
            Ui.Row("review.navigation",
                Ui.Button("review.previous", "Previous", "review.previous", !state.FinalPage && !state.Resetting && !state.Closing),
                Ui.Text("review.ordinal", state.FinalPage ? "Review complete" : $"Question {state.Ordinal} / {state.Count}"),
                Ui.Button("review.next", "Next", "review.next", !state.FinalPage && !state.Resetting && !state.Closing)),
            Ui.Text("review.status", state.Closing && state.PendingCount > 0 ? $"Finishing review saves — {state.PendingCount} pending" : state.FinalPage ? "Final status" : "Status: " + state.Status.ToUpperInvariant()),
            Ui.Scroll("review.question-scroll", Ui.Column("review.question-content",
                Ui.Title("review.question", state.FinalPage ? "Review pass complete" : state.Question))),
            Ui.Scroll("review.experience-scroll", Ui.Column("review.experience-content", experiences.ToArray())),
            Ui.Busy("review.operation", "Decision persistence", new UiOperationProjection("review.persistence",
                state.Resetting ? AgentUiOperationStatus.Working : state.OperationStatus switch
                {
                    "failed" => AgentUiOperationStatus.Failed,
                    "saved" => AgentUiOperationStatus.Succeeded,
                    "saving" or "retrying" => AgentUiOperationStatus.Working,
                    _ => AgentUiOperationStatus.Idle
                }, state.Feedback, state.CanRetry ? "review.retry" : null)),
            Ui.Text("review.pending-count", $"Pending operations: {state.PendingCount}"),
            Ui.Text("review.feedback", state.Feedback),
            Ui.Button("review.retry", "Retry failed saves", "review.retry", state.CanRetry && !state.Resetting)
        };
        if (state.FinalPage)
        {
            var rows = (state.DecisionSummary ?? []).Select(summary =>
            {
                var decision = summary.Decision is "Accepted" or "Rejected" ? summary.Decision : throw new InvalidDataException($"Invalid final review decision '{summary.Decision}'.");
                var persistence = string.IsNullOrWhiteSpace(summary.Error) ? summary.Persistence : summary.Persistence + ": " + summary.Error;
                return Ui.Text($"review.summary.item.{summary.Ordinal}", $"Item {summary.Ordinal} — {decision} — {persistence}");
            }).ToArray();
            children.Add(Ui.Scroll("review.final-summary-scroll", Ui.Column("review.final-summary", rows)));
            children.Add(Ui.Button("review.close", "Close", "review.close", state.CanClose));
        }
        else
        {
            children.Add(Ui.Row("review.decisions",
                Ui.Button("review.restart", state.Resetting ? "Restarting…" : "Restart", "review.restart", !state.Resetting && !state.Closing),
                Ui.Button("review.reject", "Reject", "review.reject", state.CanDecide && !state.Resetting && !state.Closing),
                Ui.Button("review.accept", "Accept", "review.accept", state.CanDecide && !state.Resetting && !state.Closing)));
        }
        return UiDefinition.Normalize("m053.review", Ui.Screen("review.screen",
            Ui.Margin("review.gutter", UiInsets.All(28), Ui.Column("review.content", children.ToArray()) with { Gap = 12 })));
    }

    public static UiDefinition AssetWorkbench(AssetProjection state)
    {
        var candidates = state.CandidateIds.Select((candidate, index) => $"{index + 1}.  {candidate}").ToArray();
        var definition = Ui.Screen("asset-workbench.screen", Ui.Margin("asset-workbench.gutter", UiInsets.All(28),
            Ui.Column("asset-workbench.content",
                Ui.Title("asset-workbench.title", "Asset Workbench"),
                Ui.Text("asset-workbench.session", $"Session {state.SessionId} · alias generation {state.AliasGeneration}"),
                Ui.Text("asset-workbench.active", "Active candidate: " + (state.ActiveCandidateId ?? "none")),
                Ui.Text("asset-workbench.help", "Choose a visible current candidate or enter an explicit canonical command."),
                Ui.Scroll("asset-workbench.candidate-scroll", Ui.List("asset-workbench.candidates", candidates,
                    selected: Math.Max(0, Array.IndexOf(state.CandidateIds.ToArray(), state.ActiveCandidateId)), actionId: "asset-workbench.choose")),
                Ui.TextField("asset-workbench.command", "Command entry", "asset-workbench.submit", state.Draft),
                Ui.Row("asset-workbench.entry-actions",
                    Ui.Button("asset-workbench.submit-button", "Submit", "asset-workbench.submit"),
                    Ui.Button("asset-workbench.clear", "Clear", "asset-workbench.clear"),
                    Ui.Button("asset-workbench.paste", "Paste", "asset-workbench.paste")),
                Ui.Row("asset-workbench.decisions",
                    Ui.Button("asset-workbench.accept", "Accept", "asset-workbench.accept"),
                    Ui.Button("asset-workbench.reject", "Reject", "asset-workbench.reject"),
                    Ui.Button("asset-workbench.defer", "Defer", "asset-workbench.defer"),
                    Ui.Button("asset-workbench.presentation-only", "Presentation only", "asset-workbench.presentation-only")),
                Ui.Text("asset-workbench.feedback", state.Feedback))));
        return UiDefinition.Normalize("m053.asset-workbench", definition);
    }

    public static UiDefinition AssetPreview(PreviewProjection state)
    {
        var surface = state.Surface;
        var rows = new List<UiElementDefinition>
        {
            Ui.Title("asset-preview.title", surface.Kind == PreviewSurfaceKind.Unsupported ? "Unsupported preview" : surface.Kind + " candidate preview"),
            Ui.Text("asset-preview.candidate", "Candidate: " + surface.Context.CandidateId),
            Ui.Text("asset-preview.purpose", $"Purpose: {surface.Context.PresentationRole} · Variant: {surface.Context.SelectedVariant}"),
            Ui.Text("asset-preview.corrections", "Corrections: " + (surface.Context.Corrections.Count == 0 ? "none" : string.Join(", ", surface.Context.Corrections))),
            Ui.Text("asset-preview.raw", "Raw/base: " + surface.Context.RawMeaning),
            Ui.Text("asset-preview.processed", "Processed/current draft: " + surface.Context.ProcessedMeaning),
            Ui.Text("asset-preview.question", "Question: " + surface.Context.SubjectiveQuestion),
            Ui.Text("asset-preview.media-status", state.MediaStatus)
        };
        var controls = surface.Controls.Select((control, index) => Ui.Button("asset-preview.control." + index, control, "asset-preview.control." + control)).ToArray();
        UiElementDefinition body;
        if (surface.Kind == PreviewSurfaceKind.Unsupported)
        {
            body = Ui.Column("asset-preview.unsupported", rows.Append(Ui.Text("asset-preview.diagnostic", surface.Diagnostic)).ToArray());
        }
        else
        {
            var media = Ui.ContentSlot("asset-preview.media", state.MediaReference,
                new UiSizeSpec(UiLength.Grow(2), UiLength.Grow(3), MinWidth: 160, MinHeight: 120)) with
            { AccessibleName = surface.Kind switch { PreviewSurfaceKind.Image => "Current candidate image", PreviewSurfaceKind.Animation => "Current ordered animation frame", _ => "Audio candidate playback status" } };
            var status = surface.Kind switch
            {
                PreviewSurfaceKind.Animation => new[] { Ui.Text("asset-preview.frame", $"Frame {state.FrameIndex + 1} / {Math.Max(1, state.FrameCount)} · {(state.Playing ? "playing" : "paused")}"), Ui.Text("asset-preview.playback", state.PlaybackStatus) },
                PreviewSurfaceKind.Audio => new[] { Ui.Text("asset-preview.duration", $"Raw duration: {surface.Context.RawDurationSamples?.ToString() ?? "unknown"} samples at {surface.Context.SampleRate?.ToString() ?? "unknown"} Hz · Processed: {surface.Context.ProcessedDurationSamples?.ToString() ?? "unknown"} samples"), Ui.Text("asset-preview.audio-diagnostic", state.AudioDiagnostic), Ui.Text("asset-preview.playback", state.PlaybackStatus) },
                _ => new[] { Ui.Text("asset-preview.image-state", state.PlaybackStatus) }
            };
            body = Ui.Column("asset-preview.body",
                Ui.Scroll("asset-preview.context-scroll", Ui.Column("asset-preview.context", rows.ToArray())),
                Ui.Row("asset-preview.comparison", media, Ui.Scroll("asset-preview.controls-scroll", Ui.Column("asset-preview.controls", status.Concat(controls).ToArray()))));
        }
        return UiDefinition.Normalize("m053.asset-preview", Ui.Screen("asset-preview.screen",
            Ui.Margin("asset-preview.gutter", UiInsets.All(24), body)));
    }
}
