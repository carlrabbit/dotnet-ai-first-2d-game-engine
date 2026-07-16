using System.Text.Json;

namespace Agentic2D.UI;

public static class AuthoredUiCatalog
{
    public static UiElement Load(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        if (root.GetProperty("schema").GetString() != "agentic2d.ui-document.v1" || root.GetProperty("id").GetString() != "ui.player-hud") throw new InvalidOperationException("UI0213: invalid UI document");
        var elements = root.GetProperty("elements").EnumerateArray().Select(x => x.Clone()).ToDictionary(x => x.GetProperty("id").GetString()!, StringComparer.Ordinal);
        UiElement Build(string id)
        {
            var item = elements[id]; var offset = item.GetProperty("offset");
            var children = item.GetProperty("children").EnumerateArray().Select(x => Build(x.GetString()!)).ToArray();
            var binding = item.TryGetProperty("binding", out var value) ? value.GetString() : null;
            if (binding is not null && !SemanticBindings.IsKnown(binding)) throw new InvalidOperationException("UI0211: unknown semantic binding " + binding);
            return new UiElement(id, item.GetProperty("type").GetString()!, binding, item.GetProperty("anchor").GetString()!, offset.GetProperty("x").GetInt32(), offset.GetProperty("y").GetInt32(), item.GetProperty("width").GetInt32(), item.GetProperty("height").GetInt32(), item.GetProperty("padding").GetInt32(), item.GetProperty("spacing").GetInt32(), item.GetProperty("layer").GetInt32(), children, item.TryGetProperty("textResourceId", out var text) ? text.GetString() : null, item.TryGetProperty("fontId", out var font) ? font.GetString() : null);
        }
        return Build(root.GetProperty("rootElementId").GetString()!);
    }

    public static IReadOnlyDictionary<string, TextResource> LoadText(string directory)
    {
        return Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal).Select(path =>
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path)); var item = doc.RootElement;
            if (item.GetProperty("schema").GetString() != "agentic2d.text-resource.v1") throw new InvalidOperationException("TEXT0211: invalid text schema");
            return new TextResource(item.GetProperty("id").GetString()!, item.GetProperty("defaultValue").GetString()!, item.GetProperty("tags").EnumerateArray().Select(x => x.GetString()!).Order(StringComparer.Ordinal).ToArray(), item.GetProperty("provenance").GetString()!);
        }).ToDictionary(x => x.Id, StringComparer.Ordinal);
    }

    public static IReadOnlyDictionary<string, FontResource> LoadFonts(string directory)
    {
        return Directory.EnumerateFiles(directory, "*.json").Order(StringComparer.Ordinal).Select(path =>
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path)); var item = doc.RootElement;
            if (item.GetProperty("schema").GetString() != "agentic2d.font-resource.v1") throw new InvalidOperationException("FONT0211: invalid font schema");
            return new FontResource(item.GetProperty("id").GetString()!, item.GetProperty("rawAssetReference").GetString()!, item.GetProperty("metricsPolicy").GetString()!, item.GetProperty("glyphSetPolicy").GetString()!, item.GetProperty("provenance").GetString()!);
        }).ToDictionary(x => x.Id, StringComparer.Ordinal);
    }
}
