using System.Security.Cryptography;
using System.Text;

namespace Agentic2D.UI;

public sealed record PreparedPresentationState(int HealthCurrent, int HealthMaximum, int InventoryDistinctCount, IReadOnlyDictionary<string, int> ItemCounts, bool InteractionPresent, bool InteractionEnabled, string? InteractionTextId, string? InteractionReasonId, bool NotificationPresent, string? NotificationTextId, string SaveLastStatus, IReadOnlyDictionary<string, string> DoorStates, IReadOnlyDictionary<string, string> SwitchStates);
public sealed record UiValue(string Kind, object? Value);
public sealed record UiElement(string Id, string Type, string? Binding, string Anchor, int OffsetX, int OffsetY, int Width, int Height, int Padding, int Spacing, int Layer, IReadOnlyList<UiElement> Children, string? TextResourceId = null, string? FontId = null);
public sealed record UiLayoutRecord(string ElementId, string Type, int X, int Y, int Width, int Height, int Layer, bool Visible, string Fingerprint);
public sealed record TextResource(string Id, string DefaultValue, IReadOnlyList<string> Tags, string Provenance);
public sealed record FontResource(string Id, string RawAssetReference, string MetricsPolicy, string GlyphSetPolicy, string Provenance);
public sealed record TextCommand(string Id, string ElementId, string TextResourceId, string Value, string FontId, int X, int Y, int Width, int Height, int Layer, string Fingerprint);

public static class SemanticBindings
{
    public static UiValue Resolve(string binding, PreparedPresentationState state)
    {
        return binding switch
        {
            "player.health.current" => new("integer", state.HealthCurrent),
            "player.health.maximum" => new("integer", state.HealthMaximum),
            "player.health.normalized" => new("scalar", state.HealthMaximum == 0 ? 0d : Math.Round(state.HealthCurrent / (double)state.HealthMaximum, 4, MidpointRounding.AwayFromZero)),
            "player.inventory.distinct-count" => new("integer", state.InventoryDistinctCount),
            "interaction.current.present" => new("boolean", state.InteractionPresent),
            "interaction.current.enabled" => new("boolean", state.InteractionEnabled),
            "interaction.current.text-id" => new("text-resource-id", state.InteractionTextId),
            "interaction.current.reason-id" => new("reason-id", state.InteractionReasonId),
            "notification.current.present" => new("boolean", state.NotificationPresent),
            "notification.current.text-id" => new("text-resource-id", state.NotificationTextId),
            "save.last-status" => new("status-id", state.SaveLastStatus),
            _ when binding.StartsWith("player.inventory.item-count:", StringComparison.Ordinal) => new("integer", state.ItemCounts.TryGetValue(ParameterizedId(binding, "player.inventory.item-count:"), out var amount) ? amount : 0),
            _ when binding.StartsWith("door:", StringComparison.Ordinal) => new("state-id", LookupState(binding, "door:", ".state", state.DoorStates)),
            _ when binding.StartsWith("switch:", StringComparison.Ordinal) => new("state-id", LookupState(binding, "switch:", ".state", state.SwitchStates)),
            _ => throw new InvalidOperationException("UI0211: unknown semantic binding " + binding)
        };
    }
    public static bool IsKnown(string binding) { try { _ = Resolve(binding, Empty); return true; } catch (InvalidOperationException) { return false; } }
    private static string ParameterizedId(string binding, string prefix) { var id = binding[prefix.Length..]; if (!id.StartsWith("item.", StringComparison.Ordinal) || id.Contains('.', StringComparison.Ordinal) && id.Split('.').Length < 2) throw new InvalidOperationException("UI0212: malformed parameterized binding " + binding); return id; }
    private static string LookupState(string binding, string prefix, string suffix, IReadOnlyDictionary<string, string> states) { if (!binding.EndsWith(suffix, StringComparison.Ordinal)) throw new InvalidOperationException("UI0212: malformed parameterized binding " + binding); var id = binding[prefix.Length..^suffix.Length]; if (!id.StartsWith(prefix == "door:" ? "door." : "switch.", StringComparison.Ordinal)) throw new InvalidOperationException("UI0212: malformed parameterized binding " + binding); return states.TryGetValue(id, out var value) ? value : "unknown"; }
    private static readonly PreparedPresentationState Empty = new(0, 1, 0, new Dictionary<string, int>(), false, false, null, null, false, null, "none", new Dictionary<string, string>(), new Dictionary<string, string>());
}

public static class UiProjection
{
    public static IReadOnlyList<UiLayoutRecord> Layout(UiElement root, PreparedPresentationState state, int viewportWidth, int viewportHeight)
    {
        var result = new List<UiLayoutRecord>(); LayoutInto(root, state, 0, 0, viewportWidth, viewportHeight, result); return result.OrderBy(x => x.Layer).ThenBy(x => x.ElementId, StringComparer.Ordinal).ToArray();
    }
    public static IReadOnlyList<TextCommand> TextCommands(IEnumerable<UiLayoutRecord> layout, IEnumerable<UiElement> elements, IReadOnlyDictionary<string, TextResource> text, IReadOnlyDictionary<string, FontResource> fonts)
    {
        var byId = Flatten(elements).ToDictionary(x => x.Id, StringComparer.Ordinal); var output = new List<TextCommand>();
        foreach (var record in layout.Where(x => x.Visible && x.Type == "text").OrderBy(x => x.Layer).ThenBy(x => x.ElementId, StringComparer.Ordinal))
        {
            var element = byId[record.ElementId]; if (element.TextResourceId is null || element.FontId is null || !text.TryGetValue(element.TextResourceId, out var resource) || !fonts.ContainsKey(element.FontId)) continue;
            var width = Measure(resource.DefaultValue); var id = "text-command." + element.Id; output.Add(new(id, element.Id, resource.Id, resource.DefaultValue, element.FontId, record.X, record.Y, width, 8, record.Layer, Hash(id + "|" + resource.Id + "|" + resource.DefaultValue + "|" + record.X + "|" + record.Y + "|" + width)));
        }
        return output;
    }
    public static int Measure(string value) => value.EnumerateRunes().Count() * 8;
    private static void LayoutInto(UiElement e, PreparedPresentationState state, int parentX, int parentY, int parentW, int parentH, List<UiLayoutRecord> target)
    {
        var visible = e.Binding is null || (SemanticBindings.Resolve(e.Binding, state).Value as bool? ?? true); var x = e.Anchor switch { "top-right" => parentX + parentW - e.Width - e.OffsetX, "bottom-left" => parentX + e.OffsetX, "bottom-right" => parentX + parentW - e.Width - e.OffsetX, _ => parentX + e.OffsetX }; var y = e.Anchor switch { "bottom-left" or "bottom-right" => parentY + parentH - e.Height - e.OffsetY, _ => parentY + e.OffsetY };
        target.Add(new(e.Id, e.Type, x, y, e.Width, e.Height, e.Layer, visible, Hash(e.Id + "|" + x + "|" + y + "|" + e.Width + "|" + e.Height + "|" + visible)));
        var cursor = 0; foreach (var child in e.Children)
        {
            var childX = x + e.Padding + (e.Type == "horizontal-stack" ? cursor : 0); var childY = y + e.Padding + (e.Type == "vertical-stack" ? cursor : 0); LayoutInto(child with { OffsetX = childX, OffsetY = childY, Anchor = "top-left" }, state, 0, 0, e.Width, e.Height, target); cursor += (e.Type == "horizontal-stack" ? child.Width : child.Height) + e.Spacing;
        }
    }
    private static IEnumerable<UiElement> Flatten(IEnumerable<UiElement> values) => values.SelectMany(x => new[] { x }.Concat(Flatten(x.Children)));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
