namespace Agentic2D.UI;

public static class M052UiScreens
{
    public static UiDefinition ProductShell(IReadOnlyList<MenuItem> menu)
    {
        ArgumentNullException.ThrowIfNull(menu);
        var actions = menu.Select(item => Ui.Button("menu." + item.Id, item.Label, "shell." + item.Id, item.Available)).ToArray();
        return UiDefinition.Normalize("screen.product-shell", Ui.Screen("product-shell",
            Ui.Margin("shell-gutter", new UiInsets(72, 54, 72, 48), Ui.Column("shell-content",
                Ui.Title("shell.title", "AGENTIC2D"),
                Ui.Text("shell.subtitle", "Endless settlement · Player shell"),
                Ui.Separator("shell.rule"),
                Ui.Column("shell.menu", actions),
                Ui.Text("shell.help", "Use pointer or Up/Down, then Enter. Escape closes.")))));
    }

    public static UiDefinition ReviewStress(UiOperationProjection operation, bool showModal = false)
    {
        var actions = Ui.Scroll("stress.controls-scroll", Ui.Column("stress.actions",
            Ui.Toggle("stress.toggle", "Show operation details", "stress.toggle", true),
            Ui.Slider("stress.slider", "Review intensity", "stress.intensity", .4),
            Ui.TextField("stress.notes", "Reviewer note", "stress.note", "Ready to inspect"),
            Ui.Select("stress.select", "Candidate", "stress.candidate", ["Processed atlas", "Source image", "Animation sequence"]),
            Ui.List("stress.list", ["Long description and source context", "Operation projection", "Scroll clipping", "Failure and retry feedback"]),
            OperationAction(operation)));
        var layout = Ui.Column("stress.layout",
            Ui.Title("stress.title", "Review Surface Stress Screen"),
            Ui.Text("stress.long-copy", "This synthetic screen exercises readable long-form content, deterministic wrapping, persistent interaction state, and application supplied operation status. It is not connected to a review record and never writes decisions or changes canonical persistence."),
            Ui.Row("stress.body",
                Ui.Scroll("stress.scroll", Ui.Column("stress.scroll-content", Ui.Text("stress.scroll-copy", string.Join(" ", Enumerable.Repeat("Scrollable context remains clipped to its own viewport and its hit bounds follow the same geometry as rendering.", 8))), Ui.List("stress.list-large", Enumerable.Range(1, 12).Select(i => $"Candidate {i:00} comparison").ToArray()))),
                Ui.ContentSlot("stress.media", "synthetic.preview.processed-image", new(UiLength.Grow(2), UiLength.Grow()))),
            actions,
            Ui.Busy("stress.operation", "Preview operation", operation),
            Ui.Progress("stress.progress", operation.Status == AgentUiOperationStatus.Working ? .65 : operation.Status == AgentUiOperationStatus.Succeeded ? 1 : .15, operation.Message ?? operation.Status.ToString()),
            Ui.Button("stress.dialog", "Show details", "stress.dialog")) with
        { Gap = 8 };
        var content = Ui.Margin("stress.gutter", UiInsets.All(32), layout);
        var modal = Ui.Modal("stress.modal", "Operation details", Ui.Column("stress.modal.content", Ui.Text("stress.modal.message", "Failure and retry state comes from the application projection. This modal is synthetic; no review state is changed."), Ui.Button("stress.modal.close", "Close", "stress.modal.close")), "stress.modal.close") with { Visible = showModal };
        var root = Ui.Screen("review-stress", Ui.Overlay("stress.stack", content, modal));
        return UiDefinition.Normalize("screen.review-stress", root);
    }

    private static UiElementDefinition OperationAction(UiOperationProjection operation) => operation.Status switch
    {
        AgentUiOperationStatus.Working => Ui.Button("stress.operation.cancel", "Cancel preview", "stress.cancel"),
        AgentUiOperationStatus.Failed or AgentUiOperationStatus.Cancelled => Ui.Button("stress.retry", "Retry preview", operation.RetryActionId ?? "stress.retry", enabled: operation.RetryActionId is not null),
        AgentUiOperationStatus.Succeeded => Ui.Button("stress.operation.done", "Preview complete", "stress.start", enabled: false),
        _ => Ui.Button("stress.operation.start", "Run preview", "stress.start")
    };
}

public sealed record UiDrawCommand(string Kind, string ElementId, UiBounds Bounds, UiBounds ClipBounds, UiBounds VisibleBounds, UiBounds ContentBounds, UiBounds? ModalPanelBounds, string? Text, string? ContentReference, string? Role, bool Focused, bool Enabled, double? Value, int Layer, string Variant, double Pulse, double FontSize = 18, double LineHeight = 25, double LetterSpacing = 0);

public interface IAgentUiRenderer
{
    void Render(IReadOnlyList<UiDrawCommand> commands, UiTheme theme, UiViewport viewport, double presentationTime, bool reducedMotion);
}

public static class UiDrawProjection
{
    public static IReadOnlyList<UiDrawCommand> Create(UiSession session)
    {
        var snapshot = session.Snapshot();
        var samples = session.PresentationSamples().ToDictionary(x => x.ElementId, StringComparer.Ordinal);
        var scale = session.Layout.Viewport.UiScale * session.Layout.Viewport.OsDpiScale;
        return session.Layout.Elements.Where(item => item.Visible && item.VisibleBounds.Width > 0 && item.VisibleBounds.Height > 0)
            .OrderBy(item => item.Layer).ThenBy(item => item.Order)
            .Select(item =>
            {
                var element = Find(session.Definition.Root, item.Id);
                var fontSize = session.Definition.Theme.FontSize * (element?.FontSize > 0 ? element.FontSize : 1);
                var contentBounds = item.ContentBounds ?? item.LogicalBounds;
                var displayText = item.Kind switch
                {
                    AgentUiKind.List when item.Options is { Count: > 0 } list => string.Join("\n", list.Select((option, index) => (index == (int)(item.Value ?? 0) ? "› " : "  ") + option)),
                    AgentUiKind.Select when item.Options is { Count: > 0 } options => options[Math.Clamp((int)(item.Value ?? 0), 0, options.Count - 1)],
                    AgentUiKind.Toggle => (item.Value ?? 0) == 0 ? "□ " + item.Text : "☑ " + item.Text,
                    _ => item.Text
                };
                var normalized = displayText is null ? null : session.Definition.FontMetrics.NormalizeForFont(displayText);
                var wrapped = normalized is null ? null : item.Kind == AgentUiKind.TextField ? normalized : string.Join("\n", session.Definition.FontMetrics.Wrap(normalized, Math.Max(1, contentBounds.Width), fontSize));
                return new UiDrawCommand(item.Kind.ToString(), item.Id, item.PhysicalBounds, item.ClipBounds ?? item.PhysicalBounds, item.VisibleBounds, contentBounds, item.ModalPanelBounds, wrapped, item.ContentReference, item.Role, item.Id == snapshot.FocusedId, item.Enabled, item.Value, item.Layer, samples[item.Id].Variant, samples[item.Id].Pulse, fontSize * scale, session.Definition.Theme.BodyLineHeight * (element?.FontSize > 0 ? element.FontSize : 1) * scale, session.Definition.FontMetrics.Spacing(fontSize) * scale);
            }).ToArray();

        static UiElementDefinition? Find(UiElementDefinition element, string id)
        {
            if (element.Id == id) return element;
            foreach (var child in element.Items) if (Find(child, id) is { } found) return found;
            return null;
        }
    }
}
