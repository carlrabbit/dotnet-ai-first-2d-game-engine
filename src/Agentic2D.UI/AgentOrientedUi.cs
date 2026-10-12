using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agentic2D.UI;

public enum AgentUiKind { Screen, Row, Column, Grid, Overlay, Margin, Scroll, ContentSlot, Text, Title, Separator, Image, Progress, Busy, Button, Toggle, Slider, TextField, Select, List, Modal }
public enum AgentUiAxis { Horizontal, Vertical }
public enum AgentUiAlignment { Start, Center, End, Stretch }
public enum AgentUiRole { Generic, Heading, Button, Checkbox, Slider, TextField, List, Dialog, Status, Image }
public enum AgentUiOperationStatus { Idle, Working, Succeeded, Failed, Cancelled }

public readonly record struct UiLength(double Value, bool IsGrow = false)
{
    public static UiLength Auto => new(0);
    public static UiLength Fixed(double value) => new(value);
    public static UiLength Grow(double weight = 1) => new(weight, true);
}
public sealed record UiSizeSpec(UiLength Width, UiLength Height, double MinWidth = 0, double MinHeight = 0, double? MaxWidth = null, double? MaxHeight = null)
{
    public static UiSizeSpec Auto { get; } = new(UiLength.Auto, UiLength.Auto);
    public static UiSizeSpec Fill { get; } = new(UiLength.Grow(), UiLength.Grow());
    public static UiSizeSpec Fixed(double width, double height) => new(UiLength.Fixed(width), UiLength.Fixed(height));
}
public readonly record struct UiInsets(double Left, double Top, double Right, double Bottom)
{
    public static UiInsets All(double value) => new(value, value, value, value);
    public static UiInsets Zero => default;
}
public sealed record UiTheme(string Id, string FontId, double FontSize, double BodyLineHeight, double Spacing, double ControlHeight, double FocusWidth, string Background = "#111827", string Surface = "#1f2937", string Foreground = "#f9fafb", string Accent = "#2563eb", string Error = "#b91c1c");
public sealed record UiFontMetrics(string FontId, string PolicyId, double AverageAdvance, double LineHeight, double? MaximumAdvance = null)
{
    /// <summary>Glyph advance in font design units. Missing Unicode scalars use the declared fallback advance.</summary>
    public IReadOnlyDictionary<int, double> GlyphAdvances { get; init; } = RaylibDefaultGlyphMetrics.Advances;
    public int FallbackCodePoint { get; init; } = 63;
    public double DesignUnitsPerEm { get; init; } = 10;
    /// <summary>Raylib DrawText's default-font spacing step is integer fontSize / 10.</summary>
    public double SpacingDivisor { get; init; } = 10;

    public double Measure(string text, double fontSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!double.IsFinite(fontSize) || fontSize <= 0) throw new ArgumentOutOfRangeException(nameof(fontSize));
        var maximum = 0d;
        var line = 0d;
        var glyphCount = 0;
        foreach (var rune in text.EnumerateRunes())
        {
            if (rune.Value == '\n') { maximum = Math.Max(maximum, line + Math.Max(0, glyphCount - 1) * Spacing(fontSize)); line = 0; glyphCount = 0; continue; }
            if (rune.Value == '\r') continue;
            var advance = GlyphAdvances.TryGetValue(rune.Value, out var known)
                ? known
                : GlyphAdvances.TryGetValue(FallbackCodePoint, out var fallback) ? fallback : AverageAdvance * DesignUnitsPerEm;
            line += advance * fontSize / DesignUnitsPerEm;
            glyphCount++;
        }
        return Math.Round(Math.Max(maximum, line + Math.Max(0, glyphCount - 1) * Spacing(fontSize)), 3, MidpointRounding.AwayFromZero);
    }
    public double Spacing(double fontSize) => SpacingDivisor > 0 ? Math.Floor(fontSize / SpacingDivisor) : 0;
    public IReadOnlyList<string> Wrap(string text, double width, double fontSize)
    {
        ArgumentNullException.ThrowIfNull(text);
        if (!double.IsFinite(width) || width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        text = NormalizeForFont(text);
        var lines = new List<string>();
        foreach (var paragraph in text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n').Split('\n'))
        {
            var line = string.Empty;
            foreach (var word in paragraph.Split(' '))
            {
                var next = line.Length == 0 ? word : line + " " + word;
                if (line.Length > 0 && Measure(next, fontSize) > width) { lines.Add(line); line = word; }
                else line = next;
                if (Measure(line, fontSize) > width)
                {
                    var segment = string.Empty;
                    foreach (var rune in line.EnumerateRunes())
                    {
                        var candidate = segment + rune;
                        if (segment.Length > 0 && Measure(candidate, fontSize) > width) { lines.Add(segment); segment = rune.ToString(); }
                        else segment = candidate;
                    }
                    line = segment;
                }
            }
            lines.Add(line);
        }
        return lines;
    }

    public string NormalizeForFont(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var output = new StringBuilder(text.Length);
        foreach (var rune in text.EnumerateRunes())
            output.Append(rune.Value is '\n' or '\r' || GlyphAdvances.ContainsKey(rune.Value) ? rune.ToString() : Rune.GetRuneAt(char.ConvertFromUtf32(FallbackCodePoint), 0).ToString());
        return output.ToString();
    }
}
public sealed record UiGridTrack(UiLength Length, double Minimum = 0, double? Maximum = null);
public sealed record UiOperationProjection(string Id, AgentUiOperationStatus Status, string? Message = null, string? RetryActionId = null);
public sealed record UiActionId
{
    public string Value { get; }
    public UiActionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("A stable action ID is required.", nameof(value));
        Value = value;
    }
}

public sealed record UiElementDefinition(
    string Id, AgentUiKind Kind, string? Text = null, string? ActionId = null, UiSizeSpec? Size = null,
    IReadOnlyList<UiElementDefinition>? Children = null, bool Visible = true, bool Enabled = true,
    bool Essential = false, bool IntentionalOverlay = false, bool ScrollClips = false,
    UiInsets? Padding = null, UiInsets? Margin = null, double Gap = 0,
    AgentUiAxis Axis = AgentUiAxis.Vertical, AgentUiAlignment Alignment = AgentUiAlignment.Stretch,
    IReadOnlyList<UiGridTrack>? Columns = null, IReadOnlyList<UiGridTrack>? Rows = null,
    int Column = 0, int Row = 0, int ColumnSpan = 1, int RowSpan = 1,
    string? ContentReference = null, double? Value = null, IReadOnlyList<string>? Options = null,
    UiOperationProjection? Operation = null, AgentUiRole? Role = null, string? AccessibleName = null,
    double FontSize = 0, int MaxLines = 0)
{
    public IReadOnlyList<UiElementDefinition> Items => Children ?? Array.Empty<UiElementDefinition>();
}

public sealed record UiDefinition(string Id, UiElementDefinition Root, UiTheme Theme, UiFontMetrics FontMetrics, bool ReducedMotion = false)
{
    public static UiDefinition Normalize(string id, UiElementDefinition root, UiTheme? theme = null, UiFontMetrics? metrics = null, bool reducedMotion = false)
    {
        ArgumentNullException.ThrowIfNull(root);
        var definition = new UiDefinition(id, Freeze(root), theme ?? Ui.Theme.Default, metrics ?? Ui.Theme.DefaultMetrics, reducedMotion);
        UiDefinitionValidator.Validate(definition);
        return definition;
    }
    private static UiElementDefinition Freeze(UiElementDefinition element) => element with
    {
        Children = Array.AsReadOnly(element.Items.Select(Freeze).ToArray()),
        Options = element.Options is null ? null : Array.AsReadOnly(element.Options.ToArray()),
        Columns = element.Columns is null ? null : Array.AsReadOnly(element.Columns.ToArray()),
        Rows = element.Rows is null ? null : Array.AsReadOnly(element.Rows.ToArray())
    };
}

public static class Ui
{
    public static class Theme
    {
        public static UiTheme Default { get; } = new("theme.desktop", "font.default", 18, 25, 12, 48, 3);
        public static UiFontMetrics DefaultMetrics { get; } = new("font.default", "raylib-6.0-default-font-v1", .56, 25, 1);
    }
    public static UiElementDefinition Screen(string id, params UiElementDefinition[] children) => Node(id, AgentUiKind.Screen, children);
    public static UiElementDefinition Row(string id, params UiElementDefinition[] children) => Node(id, AgentUiKind.Row, children) with { Axis = AgentUiAxis.Horizontal, Gap = Theme.Default.Spacing };
    public static UiElementDefinition Column(string id, params UiElementDefinition[] children) => Node(id, AgentUiKind.Column, children) with { Gap = Theme.Default.Spacing };
    public static UiElementDefinition Grid(string id, IReadOnlyList<UiGridTrack> columns, IReadOnlyList<UiGridTrack> rows, params UiElementDefinition[] children) => Node(id, AgentUiKind.Grid, children) with { Columns = columns, Rows = rows };
    public static UiElementDefinition Overlay(string id, params UiElementDefinition[] children) => Node(id, AgentUiKind.Overlay, children);
    public static UiElementDefinition Margin(string id, UiInsets padding, UiElementDefinition child) => Node(id, AgentUiKind.Margin, [child]) with { Padding = padding };
    public static UiElementDefinition Scroll(string id, UiElementDefinition child) => Node(id, AgentUiKind.Scroll, [child]) with { ScrollClips = true };
    public static UiElementDefinition ContentSlot(string id, string contentReference, UiSizeSpec? size = null) => new(id, AgentUiKind.ContentSlot, ContentReference: contentReference, Size: size ?? UiSizeSpec.Fill, Role: AgentUiRole.Image);
    public static UiElementDefinition Text(string id, string text, UiSizeSpec? size = null) => new(id, AgentUiKind.Text, Text: text, Size: size ?? UiSizeSpec.Auto, Role: AgentUiRole.Generic);
    public static UiElementDefinition Title(string id, string text) => new(id, AgentUiKind.Title, Text: text, Size: UiSizeSpec.Auto, Role: AgentUiRole.Heading, FontSize: 1.5);
    public static UiElementDefinition Separator(string id) => new(id, AgentUiKind.Separator, Size: new(UiLength.Grow(), UiLength.Fixed(1)));
    public static UiElementDefinition Image(string id, string assetReference, string alternative) => new(id, AgentUiKind.Image, ContentReference: assetReference, AccessibleName: alternative, Size: new(UiLength.Grow(), UiLength.Grow()), Role: AgentUiRole.Image);
    public static UiElementDefinition Progress(string id, double value, string label) => new(id, AgentUiKind.Progress, Text: label, Value: value, Size: new(UiLength.Grow(), UiLength.Fixed(28)), Role: AgentUiRole.Status);
    public static UiElementDefinition Busy(string id, string label, UiOperationProjection operation) => new(id, AgentUiKind.Busy, Text: label, Operation: operation, Role: AgentUiRole.Status);
    public static UiElementDefinition Button(string id, string text, string actionId, bool enabled = true) => new(id, AgentUiKind.Button, Text: text, ActionId: actionId, Enabled: enabled, Size: new(UiLength.Grow(), UiLength.Fixed(48)), Role: AgentUiRole.Button, AccessibleName: text);
    public static UiElementDefinition Toggle(string id, string text, string actionId, bool value) => new(id, AgentUiKind.Toggle, Text: text, ActionId: actionId, Value: value ? 1 : 0, Size: new(UiLength.Grow(), UiLength.Fixed(48)), Role: AgentUiRole.Checkbox, AccessibleName: text);
    public static UiElementDefinition Slider(string id, string label, string actionId, double value) => new(id, AgentUiKind.Slider, Text: label, ActionId: actionId, Value: value, Size: new(UiLength.Grow(), UiLength.Fixed(56)), Role: AgentUiRole.Slider, AccessibleName: label);
    public static UiElementDefinition TextField(string id, string label, string actionId, string value = "") => new(id, AgentUiKind.TextField, Text: value, ActionId: actionId, AccessibleName: label, Size: new(UiLength.Grow(), UiLength.Fixed(48)), Role: AgentUiRole.TextField);
    public static UiElementDefinition Select(string id, string label, string actionId, IReadOnlyList<string> options, int selected = 0) => new(id, AgentUiKind.Select, Text: label, ActionId: actionId, Options: options, Value: selected, Size: new(UiLength.Grow(), UiLength.Fixed(48)), Role: AgentUiRole.Button, AccessibleName: label);
    public static UiElementDefinition List(string id, IReadOnlyList<string> items, int selected = 0, string? actionId = null) => new(id, AgentUiKind.List, ActionId: actionId, Options: items, Value: selected, Size: new(UiLength.Grow(), UiLength.Fixed(Math.Max(120, items.Count * Theme.Default.ControlHeight))), Role: AgentUiRole.List);
    public static UiElementDefinition Modal(string id, string title, UiElementDefinition content, string? cancelActionId = null) => Node(id, AgentUiKind.Modal, [Title(id + ".title", title), content]) with { ActionId = cancelActionId, IntentionalOverlay = true, Role = AgentUiRole.Dialog, AccessibleName = title };
    private static UiElementDefinition Node(string id, AgentUiKind kind, IReadOnlyList<UiElementDefinition> children) => new(id, kind, Children: children, Size: UiSizeSpec.Fill);
}

public sealed record UiDiagnostic(string Code, string Severity, string ElementId, string Message, double? Measured = null, double? Available = null);
public sealed record UiBounds(double X, double Y, double Width, double Height)
{
    public bool Contains(double x, double y) => x >= X && y >= Y && x < X + Width && y < Y + Height;
    public bool Intersects(UiBounds other) => X < other.X + other.Width && X + Width > other.X && Y < other.Y + other.Height && Y + Height > other.Y;
}
public sealed record UiLayoutItem(string Id, AgentUiKind Kind, UiBounds LogicalBounds, UiBounds PhysicalBounds, UiBounds VisibleBounds, int Layer, bool Visible, bool Enabled, bool Focusable, string? ActionId, string? Text, string? ContentReference, string? Role, string? AccessibleName, double? Value, IReadOnlyList<string>? Options, UiOperationProjection? Operation,
    UiBounds? ContentBounds = null, UiBounds? ClipBounds = null, UiBounds? ModalPanelBounds = null, int Order = 0);
public sealed record UiViewport(double PhysicalWidth, double PhysicalHeight, double UiScale = 1, double OsDpiScale = 1)
{
    public double LogicalWidth => PhysicalWidth / UiScale / OsDpiScale;
    public double LogicalHeight => PhysicalHeight / UiScale / OsDpiScale;
    public (double X, double Y) ToLogical(double physicalX, double physicalY) => (physicalX / UiScale / OsDpiScale, physicalY / UiScale / OsDpiScale);
}
public sealed record UiLayoutResult(string Schema, string DefinitionId, UiViewport Viewport, IReadOnlyList<UiLayoutItem> Elements, IReadOnlyList<UiDiagnostic> Diagnostics, string Fingerprint)
{
    public bool IsValid => Diagnostics.All(x => x.Severity != "error");
}

public static class UiDefinitionValidator
{
    public static void Validate(UiDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (string.IsNullOrWhiteSpace(definition.Id)) throw new ArgumentException("UI definition ID is required.");
        var ids = new HashSet<string>(StringComparer.Ordinal);
        void Visit(UiElementDefinition node, string path)
        {
            if (node is null) throw new ArgumentException($"{path}: null element is not allowed.");
            if (string.IsNullOrWhiteSpace(node.Id) || node.Id.Any(char.IsControl)) throw new ArgumentException($"{path}: element ID is invalid.");
            if (!ids.Add(node.Id)) throw new ArgumentException($"{path}: duplicate element ID '{node.Id}'.");
            var size = node.Size ?? UiSizeSpec.Auto;
            foreach (var n in new[] { size.Width.Value, size.Height.Value, size.MinWidth, size.MinHeight, size.MaxWidth ?? 0, size.MaxHeight ?? 0, node.Gap, node.FontSize }) if (!double.IsFinite(n) || n < 0) throw new ArgumentException($"{path}: sizing values must be finite and nonnegative.");
            if ((size.MaxWidth is { } mw && mw < size.MinWidth) || (size.MaxHeight is { } mh && mh < size.MinHeight)) throw new ArgumentException($"{path}: maximum size is below minimum.");
            if (size.Width.IsGrow && size.Width.Value == 0 || size.Height.IsGrow && size.Height.Value == 0) throw new ArgumentException($"{path}: grow weight must be positive.");
            if (node.Column < 0 || node.Row < 0 || node.ColumnSpan < 1 || node.RowSpan < 1) throw new ArgumentException($"{path}: grid references must be nonnegative with positive spans.");
            if (node.Kind is AgentUiKind.Button or AgentUiKind.Toggle or AgentUiKind.Slider or AgentUiKind.TextField or AgentUiKind.Select && string.IsNullOrWhiteSpace(node.ActionId)) throw new ArgumentException($"{path}: interactive control requires action identity.");
            if (node.ActionId is { } action && (string.IsNullOrWhiteSpace(action) || action.Any(char.IsControl))) throw new ArgumentException($"{path}: invalid action ID.");
            if (node.Value is { } value && !double.IsFinite(value)) throw new ArgumentException($"{path}: control value must be finite.");
            if (node.Kind == AgentUiKind.Progress && node.Value is < 0 or > 1) throw new ArgumentException($"{path}: progress value must be between 0 and 1.");
            if (node.Kind == AgentUiKind.Slider && node.Value is < 0 or > 1) throw new ArgumentException($"{path}: slider value must be between 0 and 1.");
            if (node.Kind == AgentUiKind.Grid && (node.Columns is null || node.Rows is null || node.Columns.Count == 0 || node.Rows.Count == 0)) throw new ArgumentException($"{path}: grid requires column and row tracks.");
            if (node.Columns?.Any(x => !ValidTrack(x)) == true || node.Rows?.Any(x => !ValidTrack(x)) == true) throw new ArgumentException($"{path}: grid tracks must be finite and valid.");
            if (node.Kind == AgentUiKind.TextField && node.Text is null) throw new ArgumentException($"{path}: text field needs a draft value.");
            if (node.Kind == AgentUiKind.Select && (node.Options is null || node.Options.Count == 0 || node.Value.GetValueOrDefault() < 0 || node.Value.GetValueOrDefault() >= node.Options.Count)) throw new ArgumentException($"{path}: select options and selected index are invalid.");
            if ((node.Kind is AgentUiKind.Screen or AgentUiKind.Row or AgentUiKind.Column or AgentUiKind.Grid or AgentUiKind.Overlay or AgentUiKind.Margin or AgentUiKind.Scroll or AgentUiKind.Modal) && node.Items.Count == 0) throw new ArgumentException($"{path}: structural element requires children.");
            if (node.Padding is { } padding && new[] { padding.Left, padding.Top, padding.Right, padding.Bottom }.Any(x => !double.IsFinite(x) || x < 0)) throw new ArgumentException($"{path}: padding must be finite and nonnegative.");
            if (node.Margin is { } margin && new[] { margin.Left, margin.Top, margin.Right, margin.Bottom }.Any(x => !double.IsFinite(x) || x < 0)) throw new ArgumentException($"{path}: margin must be finite and nonnegative.");
            for (var i = 0; i < node.Items.Count; i++) Visit(node.Items[i], path + ".children[" + i + "]");
        }
        Visit(definition.Root, "root");
        if (definition.Root.Kind != AgentUiKind.Screen) throw new ArgumentException("The root element must be Ui.Screen.");
        if (definition.Theme.FontId != definition.FontMetrics.FontId) throw new ArgumentException("Theme font and deterministic metric font identities must match.");
        if (!double.IsFinite(definition.FontMetrics.AverageAdvance) || definition.FontMetrics.AverageAdvance <= 0 || !double.IsFinite(definition.FontMetrics.LineHeight) || definition.FontMetrics.LineHeight <= 0 || !double.IsFinite(definition.FontMetrics.DesignUnitsPerEm) || definition.FontMetrics.DesignUnitsPerEm <= 0 || !double.IsFinite(definition.FontMetrics.SpacingDivisor) || definition.FontMetrics.SpacingDivisor < 0) throw new ArgumentException("Font metrics must be finite and positive.");
        if (!definition.FontMetrics.GlyphAdvances.TryGetValue(definition.FontMetrics.FallbackCodePoint, out var fallbackAdvance) || !double.IsFinite(fallbackAdvance) || fallbackAdvance <= 0 || definition.FontMetrics.GlyphAdvances.Any(glyph => glyph.Key < 0 || glyph.Key > 0x10ffff || glyph.Key is >= 0xd800 and <= 0xdfff || !double.IsFinite(glyph.Value) || glyph.Value < 0)) throw new ArgumentException("Font glyph advances and fallback must be valid Unicode scalar metrics.");
    }
    private static bool ValidTrack(UiGridTrack t) => double.IsFinite(t.Length.Value) && t.Length.Value >= 0 && double.IsFinite(t.Minimum) && t.Minimum >= 0 && (t.Maximum is null || double.IsFinite(t.Maximum.Value) && t.Maximum >= t.Minimum) && (!t.Length.IsGrow || t.Length.Value > 0);
}

public static class UiLayoutEngine
{
    public static UiLayoutResult Resolve(UiDefinition definition, UiViewport viewport, IReadOnlyDictionary<string, double>? scrollOffsets = null)
    {
        UiDefinitionValidator.Validate(definition);
        if (!double.IsFinite(viewport.PhysicalWidth) || !double.IsFinite(viewport.PhysicalHeight) || viewport.PhysicalWidth <= 0 || viewport.PhysicalHeight <= 0 || !double.IsFinite(viewport.UiScale) || viewport.UiScale <= 0 || !double.IsFinite(viewport.OsDpiScale) || viewport.OsDpiScale <= 0) throw new ArgumentException("Viewport dimensions and scales must be finite and positive.");
        var diagnostics = new List<UiDiagnostic>(); var items = new List<UiLayoutItem>(); var logicalWidth = viewport.LogicalWidth; var logicalHeight = viewport.LogicalHeight; var scale = viewport.UiScale * viewport.OsDpiScale;
        Layout(definition.Root, new(0, 0, logicalWidth, logicalHeight), new(0, 0, logicalWidth, logicalHeight), 0, true, 0);
        var siblings = items.Where(x => x.Visible && x.Kind is AgentUiKind.Button or AgentUiKind.Toggle or AgentUiKind.Slider or AgentUiKind.TextField or AgentUiKind.Select or AgentUiKind.List).ToArray();
        for (var i = 0; i < siblings.Length; i++) for (var j = i + 1; j < siblings.Length; j++) if (siblings[i].LogicalBounds.Intersects(siblings[j].LogicalBounds) && siblings[i].Layer == siblings[j].Layer && !IsIntentional(siblings[i].Id, siblings[j].Id, definition.Root)) diagnostics.Add(new("UI2001", "error", siblings[j].Id, $"Focusable controls overlap '{siblings[i].Id}'."));
        foreach (var item in items.Where(x => x.Visible && x.VisibleBounds.Width > 0 &&
                     (x.Focusable || FindNode(definition.Root, x.Id) is { } node && IsEssential(node)) &&
                     !IsWithinKind(definition.Root, x.Id, AgentUiKind.Scroll)))
            if (item.VisibleBounds.Width + .01 < item.PhysicalBounds.Width || item.VisibleBounds.Height + .01 < item.PhysicalBounds.Height)
                diagnostics.Add(new("UI2008", "error", item.Id, "Essential content or an interactive control is clipped outside its declared panel or viewport.", item.PhysicalBounds.Width * item.PhysicalBounds.Height, item.VisibleBounds.Width * item.VisibleBounds.Height));
        if (logicalWidth < 1280 || logicalHeight < 720) diagnostics.Add(new("UI2002", "error", definition.Root.Id, "Viewport is below the supported desktop minimum of 1280×720.", logicalWidth, 1280));
        var normalized = JsonSerializer.Serialize(new { definition, viewport, elements = items, diagnostics }); var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))).ToLowerInvariant();
        return new("agentic2d.ui.layout.v1", definition.Id, viewport, items.OrderBy(x => x.Layer).ThenBy(x => x.Order).ToArray(), diagnostics.OrderBy(x => x.Code, StringComparer.Ordinal).ThenBy(x => x.ElementId, StringComparer.Ordinal).ToArray(), fingerprint);

        void Layout(UiElementDefinition node, UiBounds bounds, UiBounds clip, int layer, bool inheritedVisible, double scrollTranslationY)
        {
            var visible = inheritedVisible && node.Visible; var padding = node.Padding ?? UiInsets.Zero; var margin = node.Margin ?? UiInsets.Zero;
            var x = bounds.X + margin.Left; var y = bounds.Y + margin.Top; var w = Math.Max(0, bounds.Width - margin.Left - margin.Right); var h = Math.Max(0, bounds.Height - margin.Top - margin.Bottom);
            var logical = new UiBounds(x, y, w, h); var visibleBounds = Intersect(logical, clip);
            var focusable = node.Kind is AgentUiKind.Button or AgentUiKind.Toggle or AgentUiKind.Slider or AgentUiKind.TextField or AgentUiKind.Select or AgentUiKind.List;
            var contentBounds = new UiBounds(x, y + scrollTranslationY, w, h);
            var modalPanel = node.Kind == AgentUiKind.Modal ? ModalPanel(logical) : null;
            var px = x * scale; var py = y * scale;
            items.Add(new(node.Id, node.Kind, logical, Scale(logical, scale), Scale(visibleBounds, scale), layer, visible, node.Enabled, focusable, node.ActionId, node.Text, node.ContentReference, (node.Role ?? DefaultRole(node.Kind)).ToString(), node.AccessibleName ?? node.Text ?? node.ContentReference, node.Value, node.Options, node.Operation,
                contentBounds, Scale(clip, scale), modalPanel is null ? null : Scale(modalPanel, scale), items.Count));
            if (!visible) return;
            var content = modalPanel ?? new UiBounds(x + padding.Left, y + padding.Top, Math.Max(0, w - padding.Left - padding.Right), Math.Max(0, h - padding.Top - padding.Bottom));
            var childClip = node.Kind == AgentUiKind.Scroll ? Intersect(clip, content) : clip;
            if (node.Kind == AgentUiKind.Modal) childClip = Intersect(childClip, content);
            var displayText = node.Kind switch
            {
                AgentUiKind.List when node.Options is { Count: > 0 } list => string.Join("\n", list.Select((option, index) => (index == (int)(node.Value ?? 0) ? "› " : "  ") + option)),
                AgentUiKind.Select when node.Options is { Count: > 0 } options => options[Math.Clamp((int)(node.Value ?? 0), 0, options.Count - 1)],
                _ => node.Text
            };
            if (displayText is { } text && (node.Kind is AgentUiKind.Text or AgentUiKind.Title or AgentUiKind.List or AgentUiKind.Select or AgentUiKind.Button or AgentUiKind.Toggle or AgentUiKind.TextField))
            {
                var fontSize = EffectiveFontSize(node, definition);
                var lineHeight = EffectiveLineHeight(node, definition);
                var lines = Wrap(text, content.Width, fontSize, definition.FontMetrics);
                var measured = lines.Count * lineHeight;
                var widthOverflow = lines.Any(line => definition.FontMetrics.Measure(line, fontSize) > content.Width + .01);
                var heightOverflow = measured > content.Height + .01 || node.MaxLines > 0 && lines.Count > node.MaxLines;
                if ((widthOverflow || heightOverflow) && IsEssential(node)) diagnostics.Add(new("UI2003", "error", node.Id, "Essential text exceeds its available width or height policy.", Math.Max(widthOverflow ? lines.Max(line => definition.FontMetrics.Measure(line, fontSize)) : 0, measured), Math.Min(content.Height, node.MaxLines > 0 ? node.MaxLines * lineHeight : content.Height)));
                else if (widthOverflow || heightOverflow) diagnostics.Add(new("UI2003", "warning", node.Id, "Text exceeds available width or height policy.", measured, content.Height));
            }
            if (node.Items.Count == 0) return;
            var childLayer = layer + (node.Kind is AgentUiKind.Overlay or AgentUiKind.Modal ? 1 : 0);
            switch (node.Kind)
            {
                case AgentUiKind.Row:
                case AgentUiKind.Column:
                case AgentUiKind.Screen:
                case AgentUiKind.Margin:
                case AgentUiKind.Modal:
                    var horizontal = node.Kind == AgentUiKind.Row || node.Axis == AgentUiAxis.Horizontal; var total = horizontal ? content.Width : content.Height; var gapTotal = node.Gap * Math.Max(0, node.Items.Count - 1);
                    var allocations = AllocateStack(node, horizontal, content, definition, diagnostics);
                    var cursor = horizontal ? content.X : content.Y;
                    for (var childIndex = 0; childIndex < node.Items.Count; childIndex++)
                    {
                        var child = node.Items[childIndex]; var size = child.Size ?? UiSizeSpec.Auto; var childMain = allocations[childIndex];
                        var crossExtent = horizontal ? content.Height : content.Width; var crossMin = horizontal ? size.MinHeight : size.MinWidth; var crossMax = horizontal ? size.MaxHeight : size.MaxWidth;
                        var crossLength = horizontal ? size.Height : size.Width;
                        var preferredCross = crossLength.IsGrow ? crossExtent : crossLength.Value > 0 ? crossLength.Value : child.Alignment == AgentUiAlignment.Stretch ? crossExtent : Intrinsic(child, !horizontal, definition, crossExtent);
                        var cross = Math.Min(crossExtent, Math.Min(Math.Max(preferredCross, crossMin), crossMax ?? double.MaxValue));
                        if (crossExtent + .01 < crossMin || cross + .01 < crossMin) diagnostics.Add(new("UI2006", "error", child.Id, "Minimum cross-axis size constraint cannot be satisfied.", crossMin, cross));
                        if (preferredCross > crossExtent + .01) diagnostics.Add(new("UI2004", "error", child.Id, "Preferred cross-axis size exceeds its allocated container extent.", preferredCross, crossExtent));
                        var offset = child.Alignment switch { AgentUiAlignment.Center => (crossExtent - cross) / 2, AgentUiAlignment.End => crossExtent - cross, _ => 0 };
                        var childBounds = horizontal ? new UiBounds(cursor, content.Y + offset, childMain, cross) : new UiBounds(content.X + offset, cursor, cross, childMain);
                        Layout(child, childBounds, childClip, childLayer, visible, scrollTranslationY); cursor += childMain + node.Gap;
                    }
                    var used = allocations.Sum() + gapTotal;
                    if (used > total + .01) diagnostics.Add(new("UI2004", "error", node.Id, "Allocated children exceed the available container size.", used, total));
                    break;
                case AgentUiKind.Scroll:
                    var scrollOffset = Math.Max(0, scrollOffsets?.GetValueOrDefault(node.Id) ?? 0);
                    foreach (var child in node.Items)
                    {
                        var extent = ContentExtent(child, content.Width, definition);
                        var height = Math.Max(content.Height, extent);
                        Layout(child, new UiBounds(content.X, content.Y - scrollOffset, content.Width, height), childClip, childLayer, visible, scrollTranslationY + scrollOffset);
                    }
                    break;
                case AgentUiKind.Overlay:
                    for (var index = 0; index < node.Items.Count; index++) Layout(node.Items[index], content, childClip, childLayer + index * 10, visible, scrollTranslationY);
                    break;
                case AgentUiKind.Grid:
                    var columns = ResolveTracks(node.Columns!, content.Width, node.Id, "columns", diagnostics); var rows = ResolveTracks(node.Rows!, content.Height, node.Id, "rows", diagnostics);
                    for (var index = 0; index < node.Items.Count; index++)
                    {
                        var child = node.Items[index];
                        if (child.Column + child.ColumnSpan > columns.Length || child.Row + child.RowSpan > rows.Length) { diagnostics.Add(new("UI2005", "error", child.Id, "Grid child references an unavailable track.")); continue; }
                        var cellX = content.X + columns.Take(child.Column).Sum(); var cellY = content.Y + rows.Take(child.Row).Sum();
                        var cellWidth = columns.Skip(child.Column).Take(child.ColumnSpan).Sum(); var cellHeight = rows.Skip(child.Row).Take(child.RowSpan).Sum();
                        var constraint = child.Size ?? UiSizeSpec.Auto;
                        var requestedWidth = constraint.Width.IsGrow ? cellWidth : constraint.Width.Value > 0 ? constraint.Width.Value : Intrinsic(child, true, definition, cellHeight);
                        var requestedHeight = constraint.Height.IsGrow ? cellHeight : constraint.Height.Value > 0 ? constraint.Height.Value : Intrinsic(child, false, definition, cellWidth);
                        var childWidth = Math.Min(cellWidth, Math.Min(requestedWidth, constraint.MaxWidth ?? double.MaxValue));
                        var childHeight = Math.Min(cellHeight, Math.Min(requestedHeight, constraint.MaxHeight ?? double.MaxValue));
                        if (!constraint.Width.IsGrow && constraint.Width.Value > 0 && (constraint.Width.Value < constraint.MinWidth || constraint.MaxWidth is { } maxWidth && constraint.Width.Value > maxWidth)) diagnostics.Add(new("UI2006", "error", child.Id, "Grid child preferred width conflicts with its minimum/maximum constraint.", constraint.Width.Value, childWidth));
                        if (!constraint.Height.IsGrow && constraint.Height.Value > 0 && (constraint.Height.Value < constraint.MinHeight || constraint.MaxHeight is { } maxHeight && constraint.Height.Value > maxHeight)) diagnostics.Add(new("UI2006", "error", child.Id, "Grid child preferred height conflicts with its minimum/maximum constraint.", constraint.Height.Value, childHeight));
                        if (cellWidth + .01 < constraint.MinWidth || childWidth + .01 < constraint.MinWidth) diagnostics.Add(new("UI2006", "error", child.Id, "Grid cell cannot satisfy the child's minimum width.", constraint.MinWidth, childWidth));
                        if (cellHeight + .01 < constraint.MinHeight || childHeight + .01 < constraint.MinHeight) diagnostics.Add(new("UI2006", "error", child.Id, "Grid cell cannot satisfy the child's minimum height.", constraint.MinHeight, childHeight));
                        if (requestedWidth > cellWidth + .01 || requestedHeight > cellHeight + .01) diagnostics.Add(new("UI2004", "error", child.Id, "Grid child preferred extent exceeds its allocated cell.", Math.Max(requestedWidth, requestedHeight), Math.Max(cellWidth, cellHeight)));
                        var xOffset = child.Alignment switch { AgentUiAlignment.Center => (cellWidth - childWidth) / 2, AgentUiAlignment.End => cellWidth - childWidth, _ => 0 };
                        var yOffset = child.Alignment switch { AgentUiAlignment.Center => (cellHeight - childHeight) / 2, AgentUiAlignment.End => cellHeight - childHeight, _ => 0 };
                        Layout(child, new(cellX + xOffset, cellY + yOffset, childWidth, childHeight), childClip, childLayer, visible, scrollTranslationY);
                    }
                    break;
                default:
                    foreach (var child in node.Items) Layout(child, content, childClip, childLayer, visible, scrollTranslationY);
                    break;
            }
        }

        UiBounds ModalPanel(UiBounds bounds)
        {
            var width = Math.Min(bounds.Width - 48, 720);
            var height = Math.Min(bounds.Height - 48, 560);
            width = Math.Max(0, width); height = Math.Max(0, height);
            return new(bounds.X + (bounds.Width - width) / 2, bounds.Y + (bounds.Height - height) / 2, width, height);
        }

        static UiElementDefinition? FindNode(UiElementDefinition node, string id)
        {
            if (node.Id == id) return node;
            foreach (var child in node.Items) if (FindNode(child, id) is { } found) return found;
            return null;
        }
    }
    private static AgentUiRole DefaultRole(AgentUiKind kind) => kind switch { AgentUiKind.Button => AgentUiRole.Button, AgentUiKind.Title => AgentUiRole.Heading, AgentUiKind.TextField => AgentUiRole.TextField, AgentUiKind.List => AgentUiRole.List, AgentUiKind.Modal => AgentUiRole.Dialog, _ => AgentUiRole.Generic };
    private static double Intrinsic(UiElementDefinition node, bool horizontal, UiDefinition definition, double availableCross = double.MaxValue) => node.Kind switch
    {
        AgentUiKind.Text or AgentUiKind.Title => horizontal ? definition.FontMetrics.Measure(node.Text ?? string.Empty, EffectiveFontSize(node, definition)) : Wrap(node.Text ?? string.Empty, availableCross, EffectiveFontSize(node, definition), definition.FontMetrics).Count * EffectiveLineHeight(node, definition),
        AgentUiKind.List => horizontal ? node.Options?.Select(option => definition.FontMetrics.Measure(option, definition.Theme.FontSize)).DefaultIfEmpty(0).Max() + 40 ?? 40 : Math.Max(definition.Theme.ControlHeight, (node.Options?.Count ?? 1) * definition.Theme.ControlHeight),
        AgentUiKind.Button or AgentUiKind.Toggle or AgentUiKind.Slider or AgentUiKind.Select or AgentUiKind.TextField => horizontal ? definition.FontMetrics.Measure(node.Text ?? string.Empty, definition.Theme.FontSize) + 40 : definition.Theme.ControlHeight,
        AgentUiKind.Separator => 1,
        _ => 0
    };
    private static double[] ResolveTracks(IReadOnlyList<UiGridTrack> tracks, double available, string elementId, string axis, List<UiDiagnostic> diagnostics)
    {
        for (var index = 0; index < tracks.Count; index++)
        {
            var track = tracks[index];
            if (!track.Length.IsGrow && track.Length.Value > 0 && (track.Length.Value < track.Minimum || track.Maximum is { } max && track.Length.Value > max))
                diagnostics.Add(new("UI2006", "error", elementId, $"Grid {axis} track {index} preferred size conflicts with its minimum/maximum constraints.", track.Length.Value, Math.Clamp(track.Length.Value, track.Minimum, track.Maximum ?? double.MaxValue)));
        }
        var sizes = tracks.Select(track => track.Length.IsGrow ? track.Minimum : Math.Clamp(track.Length.Value > 0 ? track.Length.Value : track.Minimum, track.Minimum, track.Maximum ?? double.MaxValue)).ToArray();
        var grow = tracks.Select((track, index) => (track, index)).Where(x => x.track.Length.IsGrow).ToArray();
        var remaining = available - sizes.Sum();
        var active = grow.Where(x => x.track.Maximum is null || sizes[x.index] < x.track.Maximum.Value).ToList();
        while (remaining > .001 && active.Count > 0)
        {
            var weight = active.Sum(x => x.track.Length.Value);
            var spent = 0d;
            foreach (var item in active.ToArray())
            {
                var requested = remaining * item.track.Length.Value / weight;
                var grant = Math.Min(requested, (item.track.Maximum ?? double.MaxValue) - sizes[item.index]);
                sizes[item.index] += grant; spent += grant;
                if (item.track.Maximum is { } max && sizes[item.index] >= max - .001) active.Remove(item);
            }
            if (spent <= .001) break;
            remaining -= spent;
        }
        var overflow = sizes.Sum() - available;
        if (overflow > .01) diagnostics.Add(new("UI2004", "error", elementId, $"Grid {axis} exceed the available extent.", sizes.Sum(), available));
        return sizes;
    }
    private static double[] AllocateStack(UiElementDefinition container, bool horizontal, UiBounds content, UiDefinition definition, List<UiDiagnostic> diagnostics)
    {
        var total = horizontal ? content.Width : content.Height;
        var sizes = new double[container.Items.Count];
        var grow = new List<int>();
        for (var index = 0; index < container.Items.Count; index++)
        {
            var child = container.Items[index]; var size = child.Size ?? UiSizeSpec.Auto;
            var length = horizontal ? size.Width : size.Height; var min = horizontal ? size.MinWidth : size.MinHeight; var max = horizontal ? size.MaxWidth : size.MaxHeight;
            var intrinsic = Intrinsic(child, horizontal, definition, horizontal ? content.Height : content.Width);
            if (!length.IsGrow && length.Value > 0 && (length.Value < min || max is { } preferredMax && length.Value > preferredMax))
                diagnostics.Add(new("UI2006", "error", child.Id, "Preferred stack size conflicts with its minimum/maximum constraints.", length.Value, Math.Clamp(length.Value, min, max ?? double.MaxValue)));
            sizes[index] = length.IsGrow ? min : Math.Clamp(length.Value > 0 ? length.Value : intrinsic, min, max ?? double.MaxValue);
            if (length.IsGrow) grow.Add(index);
        }
        var gaps = container.Gap * Math.Max(0, container.Items.Count - 1);
        var fixedAndMinimum = sizes.Sum() + gaps;
        if (fixedAndMinimum > total + .01)
            diagnostics.Add(new("UI2004", "error", container.Id, "Children's required extents exceed the available container size.", fixedAndMinimum, total));
        var remaining = Math.Max(0, total - fixedAndMinimum);
        var active = grow.Where(i => (horizontal ? container.Items[i].Size?.MaxWidth : container.Items[i].Size?.MaxHeight) is null || sizes[i] < (horizontal ? container.Items[i].Size!.MaxWidth : container.Items[i].Size!.MaxHeight)).ToList();
        while (remaining > .001 && active.Count > 0)
        {
            var weight = active.Sum(i => (horizontal ? container.Items[i].Size!.Width : container.Items[i].Size!.Height).Value);
            var spent = 0d;
            foreach (var i in active.ToArray())
            {
                var childSize = container.Items[i].Size!; var max = horizontal ? childSize.MaxWidth : childSize.MaxHeight;
                var share = remaining * (horizontal ? childSize.Width : childSize.Height).Value / weight;
                var grant = Math.Min(share, max is { } limit ? Math.Max(0, limit - sizes[i]) : double.MaxValue);
                sizes[i] += grant; spent += grant;
                if (max is { } bound && sizes[i] >= bound - .001) active.Remove(i);
            }
            if (spent <= .001) break;
            remaining -= spent;
        }
        for (var i = 0; i < sizes.Length; i++)
        {
            var size = container.Items[i].Size ?? UiSizeSpec.Auto; var min = horizontal ? size.MinWidth : size.MinHeight;
            if (sizes[i] + .01 < min) diagnostics.Add(new("UI2006", "error", container.Items[i].Id, "Minimum size constraint cannot be satisfied.", min, sizes[i]));
        }
        return sizes;
    }
    private static double ContentExtent(UiElementDefinition node, double availableWidth, UiDefinition definition)
    {
        if (node.Kind is AgentUiKind.Column or AgentUiKind.Screen)
            return node.Items.Sum(child =>
            {
                var size = child.Size ?? UiSizeSpec.Auto;
                var height = size.Height.IsGrow ? Math.Max(size.MinHeight, Intrinsic(child, false, definition, availableWidth)) : size.Height.Value > 0 ? size.Height.Value : Intrinsic(child, false, definition, availableWidth);
                return Math.Max(size.MinHeight, Math.Min(height, size.MaxHeight ?? double.MaxValue));
            }) + node.Gap * Math.Max(0, node.Items.Count - 1);
        if (node.Kind == AgentUiKind.Row) return node.Items.Select(child => ContentExtent(child, availableWidth, definition)).DefaultIfEmpty(0).Max();
        return Math.Max(node.Size?.Height.Value ?? 0, Intrinsic(node, false, definition, availableWidth));
    }
    private static bool IsEssential(UiElementDefinition node) => node.Essential || node.Kind is AgentUiKind.Button or AgentUiKind.Toggle or AgentUiKind.Slider or AgentUiKind.TextField or AgentUiKind.Select or AgentUiKind.List or AgentUiKind.ContentSlot or AgentUiKind.Image or AgentUiKind.Progress or AgentUiKind.Busy or AgentUiKind.Text or AgentUiKind.Title;
    private static double EffectiveFontSize(UiElementDefinition node, UiDefinition definition) => node.FontSize > 0 ? node.FontSize * definition.Theme.FontSize : definition.Theme.FontSize;
    private static double EffectiveLineHeight(UiElementDefinition node, UiDefinition definition) => definition.Theme.BodyLineHeight * (node.FontSize > 0 ? node.FontSize : 1);
    private static UiBounds Scale(UiBounds bounds, double scale) => new(bounds.X * scale, bounds.Y * scale, bounds.Width * scale, bounds.Height * scale);
    private static IReadOnlyList<string> Wrap(string text, double width, double size, UiFontMetrics metrics) => width <= 0 ? Array.Empty<string>() : metrics.Wrap(text, width, size);
    private static bool IsWithinKind(UiElementDefinition root, string id, AgentUiKind kind)
    {
        bool Search(UiElementDefinition node, bool inherited)
        {
            var nested = inherited || node.Kind == kind;
            if (node.Id == id) return nested;
            return node.Items.Any(child => Search(child, nested));
        }
        return Search(root, false);
    }
    private static bool IsIntentional(string a, string b, UiElementDefinition root) => HasIntentionalAncestor(root, a, false) || HasIntentionalAncestor(root, b, false);
    private static bool HasIntentionalAncestor(UiElementDefinition node, string id, bool inherited)
    {
        var intentional = inherited || node.IntentionalOverlay || node.Kind == AgentUiKind.Overlay || node.Kind == AgentUiKind.Scroll;
        if (node.Id == id) return intentional;
        foreach (var child in node.Items) if (HasIntentionalAncestor(child, id, intentional)) return true;
        return false;
    }
    private static UiBounds Intersect(UiBounds a, UiBounds b) { var x = Math.Max(a.X, b.X); var y = Math.Max(a.Y, b.Y); return new(x, y, Math.Max(0, Math.Min(a.X + a.Width, b.X + b.Width) - x), Math.Max(0, Math.Min(a.Y + a.Height, b.Y + b.Height) - y)); }
}

public sealed record UiActionRequest(string ActionId, string SourceId, object? Value);
public sealed record UiPresentationSample(string ElementId, string Variant, double Pulse);
public sealed record UiInputEvent(string Kind, double X = 0, double Y = 0, string? Key = null, string? Text = null, double Delta = 0);
public sealed record UiSessionSnapshot(string? FocusedId, string? CapturedId, string? HoveredId, IReadOnlyDictionary<string, string> Drafts, IReadOnlyDictionary<string, double> Values, IReadOnlyDictionary<string, double> ScrollOffsets, IReadOnlyList<UiActionRequest> Actions, IReadOnlyList<string> Trace, double PresentationTime);

public sealed class UiSession
{
    private UiDefinition definition;
    private UiLayoutResult layout;
    private UiViewport viewport;
    private readonly Dictionary<string, string> drafts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> textCursors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> values = new(StringComparer.Ordinal);
    private readonly Dictionary<string, double> scroll = new(StringComparer.Ordinal);
    private readonly List<UiActionRequest> actions = [];
    private readonly List<string> trace = [];
    private string? previousFocus;
    public string? FocusedId { get; private set; }
    public string? CapturedId { get; private set; }
    public string? HoveredId { get; private set; }
    public double PresentationTime { get; private set; }
    public UiDefinition Definition => definition;
    public UiLayoutResult Layout => layout with { Elements = ProjectElements() };
    public UiSession(UiDefinition definition, UiViewport viewport)
    {
        this.definition = definition; this.viewport = viewport; layout = UiLayoutEngine.Resolve(definition, viewport); Initialize(definition.Root); RefreshLayoutFromDrafts();
        FocusedId = FocusOrder(layout).FirstOrDefault()?.Id;
        if (FindVisibleModal(definition.Root) is not null)
        {
            previousFocus = FocusedId;
            FocusedId = FocusOrder(layout).FirstOrDefault(x => IsDescendantOf(definition.Root, x.Id, AgentUiKind.Modal))?.Id;
        }
    }
    public UiSessionSnapshot Snapshot() => new(FocusedId, CapturedId, HoveredId, new Dictionary<string, string>(drafts), new Dictionary<string, double>(values), new Dictionary<string, double>(scroll), actions.ToArray(), trace.ToArray(), PresentationTime);
    public IReadOnlyList<UiPresentationSample> PresentationSamples() => layout.Elements.Where(x => x.Visible).Select(item =>
    {
        var variant = !item.Enabled ? "disabled" : item.Id == CapturedId ? "pressed" : item.Id == FocusedId ? "focused" : item.Id == HoveredId ? "hovered" : item.Operation?.Status switch { AgentUiOperationStatus.Working => "busy", AgentUiOperationStatus.Failed => "error", AgentUiOperationStatus.Succeeded => "success", _ => "normal" };
        var pulse = definition.ReducedMotion ? 1 : .78 + .22 * Math.Sin(Math.Tau * (PresentationTime % 1));
        return new UiPresentationSample(item.Id, variant, Math.Round(pulse, 6, MidpointRounding.AwayFromZero));
    }).ToArray();
    public IReadOnlyList<UiActionRequest> DrainActions() { var result = actions.ToArray(); actions.Clear(); return result; }
    public void Recompose(UiDefinition next, UiViewport viewport)
    {
        definition = UiDefinition.Normalize(next.Id, next.Root, next.Theme, next.FontMetrics, next.ReducedMotion); this.viewport = viewport; layout = UiLayoutEngine.Resolve(definition, viewport, scroll); var valid = layout.Elements.Where(Eligible).ToDictionary(x => x.Id, StringComparer.Ordinal);
        actions.Clear(); HoveredId = null;
        foreach (var key in drafts.Keys.Where(x => !valid.TryGetValue(x, out var e) || e.Kind != AgentUiKind.TextField).ToArray()) drafts.Remove(key);
        foreach (var key in textCursors.Keys.Where(x => !valid.TryGetValue(x, out var e) || e.Kind != AgentUiKind.TextField).ToArray()) textCursors.Remove(key);
        foreach (var key in values.Keys.Where(x => !valid.ContainsKey(x)).ToArray()) values.Remove(key);
        foreach (var key in scroll.Keys.Where(x => !valid.TryGetValue(x, out var e) || e.Kind != AgentUiKind.Scroll).ToArray()) scroll.Remove(key);
        if (FocusedId is not null && (!valid.TryGetValue(FocusedId, out var focus) || !focus.Focusable || !focus.Enabled)) FocusedId = null;
        if (CapturedId is not null && (!valid.TryGetValue(CapturedId, out var capture) || !capture.Focusable || !capture.Enabled)) CapturedId = null;
        if (ContainsVisibleModal(next.Root)) { previousFocus ??= FocusedId; FocusedId = FocusOrder(layout).FirstOrDefault(x => IsDescendantOf(next.Root, x.Id, AgentUiKind.Modal))?.Id; CapturedId = null; }
        else if (previousFocus is not null && valid.ContainsKey(previousFocus)) { FocusedId = previousFocus; previousFocus = null; }
        ClampScrollOffsets();
        Initialize(next.Root); RefreshLayoutFromDrafts(); trace.Add("recompose:" + next.Id);
    }
    public bool Dispatch(UiInputEvent input)
    {
        if (input.Kind == "time") { if (!double.IsFinite(input.Delta) || input.Delta < 0) throw new ArgumentOutOfRangeException(nameof(input)); PresentationTime += input.Delta; trace.Add("time:" + PresentationTime.ToString("R", System.Globalization.CultureInfo.InvariantCulture)); return true; }
        var modal = FindVisibleModal(definition.Root);
        var items = ProjectElements().Where(x => x.Visible && x.Enabled && x.VisibleBounds.Width > 0 && x.VisibleBounds.Height > 0 && (modal is null || IsDescendantOf(definition.Root, x.Id, AgentUiKind.Modal))).ToArray();
        if (input.Kind == "pointer-move") { var logical = layout.Viewport.ToLogical(input.X, input.Y); HoveredId = Hit(items, input.X, input.Y, logical.X, logical.Y)?.Id; trace.Add("hover:" + (HoveredId ?? "none")); return true; }
        if (input.Kind == "pointer-down") { var p = layout.Viewport.ToLogical(input.X, input.Y); var hit = Hit(items, input.X, input.Y, p.X, p.Y); CapturedId = hit?.Focusable == true ? hit.Id : null; if (CapturedId is not null) { FocusedId = CapturedId; EnsureFocusVisible(); } trace.Add("press:" + (CapturedId ?? "none")); return true; }
        if (input.Kind == "pointer-up")
        {
            var p = layout.Viewport.ToLogical(input.X, input.Y); var hit = Hit(items, input.X, input.Y, p.X, p.Y);
            if (CapturedId is { } captured && hit?.Id == captured && Element(captured) is { } node)
            {
                var item = items.First(x => x.Id == captured);
                if (node.Kind == AgentUiKind.Slider) { var value = Math.Clamp((p.X - item.LogicalBounds.X) / Math.Max(1, item.LogicalBounds.Width), 0, 1); values[captured] = value; Emit(node, captured, value); }
                else if (node.Kind == AgentUiKind.List && node.Options is { Count: > 0 } options) { var selected = Math.Clamp((int)((p.Y - item.LogicalBounds.Y) / Math.Max(1, item.LogicalBounds.Height) * options.Count), 0, options.Count - 1); values[captured] = selected; RefreshLayoutFromDrafts(); if (node.ActionId is not null) Emit(node, captured, selected); }
                else if (node.Kind != AgentUiKind.TextField) Activate(item);
            }
            trace.Add("release:" + (CapturedId ?? "none")); CapturedId = null; return true;
        }
        if (input.Kind == "pointer-cancel") { trace.Add("capture-cancel:" + (CapturedId ?? "none")); CapturedId = null; return true; }
        if (input.Kind == "text" && FocusedId is { } focused && Element(focused)?.Kind == AgentUiKind.TextField)
        {
            var current = drafts.GetValueOrDefault(focused, Element(focused)?.Text ?? string.Empty).EnumerateRunes().ToArray();
            var insert = (input.Text ?? string.Empty).EnumerateRunes().ToArray(); var cursor = Math.Clamp(textCursors.GetValueOrDefault(focused, current.Length), 0, current.Length);
            drafts[focused] = string.Concat(current.Take(cursor).Concat(insert).Concat(current.Skip(cursor)).Select(rune => rune.ToString()));
            textCursors[focused] = cursor + insert.Length; RefreshLayoutFromDrafts(); trace.Add("text:" + focused); return true;
        }
        if (input.Kind == "scroll") { var p = layout.Viewport.ToLogical(input.X, input.Y); var target = items.Where(x => x.Kind == AgentUiKind.Scroll && x.VisibleBounds.Contains(input.X, input.Y) && x.LogicalBounds.Contains(p.X, p.Y)).OrderBy(x => x.Layer).ThenBy(x => x.Order).LastOrDefault(); if (target is not null) { var max = ScrollRange(target.Id); scroll[target.Id] = Math.Clamp(scroll.GetValueOrDefault(target.Id) + input.Delta, 0, max); RefreshLayoutFromDrafts(); trace.Add("scroll:" + target.Id); return true; } }
        if (input.Kind == "key" && input.Key is { } key)
        {
            if (key == "Escape" && modal is not null)
            {
                if (modal.ActionId is { } cancelAction) actions.Add(new(cancelAction, modal.Id, null));
                trace.Add("modal-cancel:" + modal.Id);
                CapturedId = null;
                return true;
            }
            if (FocusedId is { } controlId && Element(controlId) is { } control)
            {
                if (control.Kind == AgentUiKind.TextField && key is "Backspace" or "Delete")
                {
                    var current = drafts.GetValueOrDefault(controlId, control.Text ?? string.Empty);
                    var runes = current.EnumerateRunes().ToArray();
                    var cursor = Math.Clamp(textCursors.GetValueOrDefault(controlId, runes.Length), 0, runes.Length);
                    if (key == "Backspace" && cursor > 0) { drafts[controlId] = string.Concat(runes.Take(cursor - 1).Concat(runes.Skip(cursor)).Select(rune => rune.ToString())); textCursors[controlId] = cursor - 1; }
                    else if (key == "Delete" && cursor < runes.Length) { drafts[controlId] = string.Concat(runes.Take(cursor).Concat(runes.Skip(cursor + 1)).Select(rune => rune.ToString())); textCursors[controlId] = cursor; }
                    RefreshLayoutFromDrafts();
                    trace.Add("text-backspace:" + controlId);
                    return true;
                }
                if (control.Kind == AgentUiKind.TextField && key is "Left" or "Right")
                {
                    var length = drafts.GetValueOrDefault(controlId, control.Text ?? string.Empty).EnumerateRunes().Count();
                    textCursors[controlId] = Math.Clamp(textCursors.GetValueOrDefault(controlId, length) + (key == "Right" ? 1 : -1), 0, length);
                    trace.Add("text-cursor:" + controlId + ":" + textCursors[controlId]);
                    return true;
                }
                if (control.Kind == AgentUiKind.Slider && key is "Left" or "Right") { var next = Math.Clamp(values.GetValueOrDefault(controlId, control.Value ?? 0) + (key == "Right" ? .05 : -.05), 0, 1); values[controlId] = next; actions.Add(new(control.ActionId!, controlId, next)); trace.Add("value:" + controlId); return true; }
                if (control.Kind == AgentUiKind.Select && key is "Left" or "Right" && control.Options is { Count: > 0 } options) { var current = (int)values.GetValueOrDefault(controlId, control.Value ?? 0); var next = (current + (key == "Right" ? 1 : options.Count - 1)) % options.Count; values[controlId] = next; RefreshLayoutFromDrafts(); actions.Add(new(control.ActionId!, controlId, next)); trace.Add("selection:" + controlId); return true; }
                if (control.Kind == AgentUiKind.List && key is "Up" or "Down" && control.Options is { Count: > 0 } list) { var current = (int)values.GetValueOrDefault(controlId, control.Value ?? 0); values[controlId] = Math.Clamp(current + (key == "Down" ? 1 : -1), 0, list.Count - 1); RefreshLayoutFromDrafts(); trace.Add("selection:" + controlId); return true; }
            }
            if (key is "Tab" or "Down" or "Right" or "Up" or "Left") { var order = FocusOrder(layout).Where(x => modal is null || IsDescendantOf(definition.Root, x.Id, AgentUiKind.Modal)).ToArray(); if (order.Length > 0) { var index = Array.FindIndex(order, x => x.Id == FocusedId); var forward = key is "Tab" or "Down" or "Right"; var next = index < 0 ? (forward ? 0 : order.Length - 1) : (index + (forward ? 1 : order.Length - 1)) % order.Length; FocusedId = order[next].Id; EnsureFocusVisible(); trace.Add("focus:" + FocusedId); return true; } }
            if ((key is "Enter" or "Space") && FocusedId is { } id && ProjectElements().FirstOrDefault(x => x.Id == id) is { Enabled: true } focusedItem) { Activate(focusedItem); trace.Add("confirm:" + id); return true; }
            if (key == "Escape" && CapturedId is not null) { CapturedId = null; trace.Add("cancel-capture"); return true; }
        }
        trace.Add("ignored:" + input.Kind); return false;
    }
    public object ToEvidence()
    {
        var snapshot = Snapshot(); var elements = ProjectElements();
        var artifact = new { schema = "agentic2d.ui.evidence.v1", definitionId = definition.Id, theme = definition.Theme, metrics = definition.FontMetrics, viewport = layout.Viewport, semanticTree = elements.Select(x => new { x.Id, kind = x.Kind.ToString(), x.Role, x.AccessibleName, x.ActionId, x.Text, x.Value, x.Options, x.Operation }), layout = elements, focus = snapshot.FocusedId, capture = snapshot.CapturedId, hover = snapshot.HoveredId, drafts = snapshot.Drafts, values = snapshot.Values, scroll = snapshot.ScrollOffsets, actions = snapshot.Actions, trace = snapshot.Trace, presentationTime = snapshot.PresentationTime, diagnostics = layout.Diagnostics, layoutFingerprint = layout.Fingerprint };
        var canonical = JsonSerializer.Serialize(artifact);
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
        return new { schema = artifact.schema, DefinitionId = artifact.definitionId, Theme = artifact.theme, Metrics = artifact.metrics, Viewport = artifact.viewport, SemanticTree = artifact.semanticTree, Layout = artifact.layout, Focus = artifact.focus, Capture = artifact.capture, Hover = artifact.hover, Drafts = artifact.drafts, Values = artifact.values, Scroll = artifact.scroll, Actions = artifact.actions, Trace = artifact.trace, PresentationTime = artifact.presentationTime, Diagnostics = artifact.diagnostics, LayoutFingerprint = artifact.layoutFingerprint, fingerprint };
    }
    private void Initialize(UiElementDefinition node) { if (node.Kind == AgentUiKind.TextField) drafts.TryAdd(node.Id, node.Text ?? string.Empty); if (node.Kind is AgentUiKind.Toggle or AgentUiKind.Slider or AgentUiKind.Select or AgentUiKind.List) values.TryAdd(node.Id, node.Value ?? 0); foreach (var child in node.Items) Initialize(child); }
    private UiLayoutItem[] ProjectElements() => layout.Elements.Select(item => item with { Value = values.TryGetValue(item.Id, out var value) ? value : item.Value, Text = item.Kind == AgentUiKind.TextField && drafts.TryGetValue(item.Id, out var draft) ? draft : item.Text }).ToArray();
    private static UiLayoutItem? Hit(IReadOnlyList<UiLayoutItem> items, double physicalX, double physicalY, double logicalX, double logicalY) => items.Where(i => i.Focusable && i.VisibleBounds.Contains(physicalX, physicalY) && i.LogicalBounds.Contains(logicalX, logicalY)).OrderByDescending(i => i.Layer).ThenByDescending(i => i.Order).FirstOrDefault();
    private void Activate(UiLayoutItem element)
    {
        var node = Element(element.Id); if (node is null) return;
        object? value = values.GetValueOrDefault(element.Id, node.Value ?? 0);
        if (node.Kind == AgentUiKind.Toggle) values[element.Id] = (double)value == 0 ? 1 : 0;
        if (node.Kind == AgentUiKind.Select && node.Options is { Count: > 0 } options) values[element.Id] = ((int)(double)value + 1) % options.Count;
        if (node.Kind is AgentUiKind.Toggle or AgentUiKind.Select) RefreshLayoutFromDrafts();
        if (node.Kind == AgentUiKind.TextField) value = drafts.GetValueOrDefault(element.Id, node.Text ?? string.Empty);
        else if (node.Kind is AgentUiKind.Toggle or AgentUiKind.Slider or AgentUiKind.Select) value = values[element.Id];
        else value = null;
        Emit(node, element.Id, value);
    }
    private void Emit(UiElementDefinition node, string sourceId, object? value) { if (node.ActionId is null) return; actions.Add(new(node.ActionId, sourceId, value)); trace.Add("action:" + node.ActionId + ":" + sourceId); }
    private UiElementDefinition? Element(string id) { UiElementDefinition? Find(UiElementDefinition n) => n.Id == id ? n : n.Items.Select(Find).FirstOrDefault(x => x is not null); return Find(definition.Root); }
    private double ScrollRange(string id)
    {
        var node = Element(id); var viewportItem = layout.Elements.FirstOrDefault(x => x.Id == id); if (node is null || viewportItem is null) return 0;
        var descendants = new HashSet<string>(StringComparer.Ordinal); void Add(UiElementDefinition item) { foreach (var child in item.Items) { descendants.Add(child.Id); Add(child); } }
        Add(node);
        var bottom = layout.Elements.Where(x => descendants.Contains(x.Id) && x.Visible).Select(x => (x.ContentBounds ?? x.LogicalBounds).Y + (x.ContentBounds ?? x.LogicalBounds).Height).DefaultIfEmpty((viewportItem.ContentBounds ?? viewportItem.LogicalBounds).Y + (viewportItem.ContentBounds ?? viewportItem.LogicalBounds).Height).Max();
        var viewportBounds = viewportItem.ContentBounds ?? viewportItem.LogicalBounds;
        return Math.Max(0, bottom - (viewportBounds.Y + viewportBounds.Height));
    }
    private void ClampScrollOffsets()
    {
        foreach (var id in scroll.Keys.ToArray()) scroll[id] = Math.Clamp(scroll[id], 0, ScrollRange(id));
        RefreshLayoutFromDrafts();
    }
    private void RefreshLayoutFromDrafts()
    {
        UiElementDefinition ApplyDrafts(UiElementDefinition node)
        {
            var text = node.Text;
            if (node.Kind == AgentUiKind.TextField && drafts.TryGetValue(node.Id, out var draft)) text = draft;
            var value = values.TryGetValue(node.Id, out var currentValue) ? currentValue : node.Value;
            return node with { Text = text, Value = value, Children = node.Items.Select(ApplyDrafts).ToArray() };
        }
        var projected = UiDefinition.Normalize(definition.Id, ApplyDrafts(definition.Root), definition.Theme, definition.FontMetrics, definition.ReducedMotion);
        layout = UiLayoutEngine.Resolve(projected, viewport, scroll);
    }
    private void EnsureFocusVisible()
    {
        if (FocusedId is null) return;
        var chain = new List<UiElementDefinition>();
        bool Find(UiElementDefinition node)
        {
            chain.Add(node);
            if (node.Id == FocusedId) return true;
            foreach (var child in node.Items) if (Find(child)) return true;
            chain.RemoveAt(chain.Count - 1);
            return false;
        }
        if (!Find(definition.Root)) return;
        foreach (var id in chain.Where(x => x.Kind == AgentUiKind.Scroll).Select(x => x.Id))
        {
            var control = layout.Elements.FirstOrDefault(x => x.Id == FocusedId);
            var scrollItem = layout.Elements.FirstOrDefault(x => x.Id == id);
            if (control is null || scrollItem is null) continue;
            var controlBounds = control.ContentBounds ?? control.LogicalBounds;
            var viewportBounds = scrollItem.ContentBounds ?? scrollItem.LogicalBounds;
            var top = viewportBounds.Y; var bottom = top + viewportBounds.Height; var offset = scroll.GetValueOrDefault(id);
            if (controlBounds.Y - offset < top) offset = controlBounds.Y - top;
            else if (controlBounds.Y + controlBounds.Height - offset > bottom) offset = controlBounds.Y + controlBounds.Height - bottom;
            scroll[id] = Math.Clamp(offset, 0, ScrollRange(id));
            RefreshLayoutFromDrafts();
        }
    }
    private static IReadOnlyList<UiLayoutItem> FocusOrder(UiLayoutResult result) => result.Elements.Where(x => x.Visible && x.Enabled && x.Focusable && x.LogicalBounds.Width > 0 && x.LogicalBounds.Height > 0).OrderBy(x => x.LogicalBounds.Y).ThenBy(x => x.LogicalBounds.X).ThenBy(x => x.Order).ToArray();
    private static bool Eligible(UiLayoutItem x) => x.Visible && x.LogicalBounds.Width > 0 && x.LogicalBounds.Height > 0;
    private static bool ContainsVisibleModal(UiElementDefinition node) => node.Kind == AgentUiKind.Modal && node.Visible || node.Items.Any(ContainsVisibleModal);
    private static UiElementDefinition? FindVisibleModal(UiElementDefinition node) => node.Kind == AgentUiKind.Modal && node.Visible ? node : node.Items.Select(FindVisibleModal).FirstOrDefault(x => x is not null);
    private bool IsDescendantOf(UiElementDefinition root, string id, AgentUiKind ancestor) { var chain = new List<UiElementDefinition>(); bool Find(UiElementDefinition n) { chain.Add(n); if (n.Id == id) return true; foreach (var c in n.Items) if (Find(c)) return true; chain.RemoveAt(chain.Count - 1); return false; } return Find(root) && chain.Take(chain.Count - 1).Any(x => x.Kind == ancestor); }
}
