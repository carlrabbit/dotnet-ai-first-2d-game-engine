using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.Validation;

namespace Agentic2D.Tools;

/// <summary>Headless authoring evidence for the bounded geometric visual vocabulary.</summary>
internal static class M026GeometryCommands
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3 || args[0] != "geometry" || args[1] is not ("inspect" or "preview")) return -1;
        var destination = Option(args, "--output");
        if (destination is null) { await error.WriteLineAsync("geometry inspect|preview requires <project-or-definition> --output <directory>"); return 2; }
        var files = Resolve(args[2]).ToArray();
        var graphicalMetadata = Option(args, "--graphical-metadata");
        var validator = new VisualDefinitionValidator();
        var diagnostics = new List<object>();
        var parts = new List<object>();
        if (files.Length == 0) diagnostics.Add(new { id = "GEOMETRY0001", severity = "error", message = "No visual definition JSON files were resolved for inspection.", target = Path.GetFullPath(args[2]), field = "input", partId = (string?)null, definitionId = (string?)null, value = args[2], acceptedRange = "an existing visual-definition JSON file, project visual root, or directory", remediation = "Pass an existing authored visual definition or consumer project path." });
        foreach (var file in files)
        {
            var item = validator.ValidateFile(file);
            foreach (var d in item.Diagnostics) { var field = d.Field ?? "unknown"; diagnostics.Add(new { id = d.Id, severity = d.Severity, message = d.Message, target = d.Target, field, partId = d.ItemId, definitionId = item.Definition?.Id ?? DefinitionId(file), value = AuthoredValue(file, field, d.ItemId), acceptedRange = AcceptedRange(d.Id), remediation = "Correct the declared bounded geometry field; diagnostics never modify authored layout." }); }
            if (item.Definition is null) continue;
            foreach (var part in item.Definition.Parts.Where(x => x.Geometry is not null))
            {
                var geometry = part.Geometry!;
                var bounds = new { x = part.Offset.X - part.WorldSize.Width / 2, y = part.Offset.Y - part.WorldSize.Height / 2, width = part.WorldSize.Width, height = part.WorldSize.Height };
                var contrast = Contrast(geometry.Fill, new VisualColor(20, 31, 48, 255));
                if (contrast < 1.35) diagnostics.Add(new { id = "GEOMETRY0101", severity = "warning", message = "Geometry fill has bounded low contrast against the declared preview background; this is not accessibility or aesthetic certification.", target = file, field = "parts[].geometry.fill", partId = part.Id, remediation = "Choose a more distinct author-selected fill or preview background." });
                parts.Add(new { definitionId = item.Definition.Id, partId = part.Id, shapeKind = geometry.Kind, dimensions = part.WorldSize, bounds, fill = geometry.Fill, outline = geometry.Outline, outlineWidth = geometry.OutlineWidth, opacity = geometry.Opacity, anchor = part.Anchor, offset = part.Offset, rotation = geometry.Rotation, layer = part.Layer, order = part.Order, sortMode = part.SortMode, polygonSides = geometry.PolygonSides, ringInnerRatio = geometry.RingInnerRatio, lineEnd = geometry.LineEnd, provenance = new { sourcePath = file, sourceKind = "authored-geometry" }, fingerprint = Fingerprint(new { item.Definition.Id, part, geometry }) });
            }
        }
        var ordered = parts.OrderBy(x => JsonSerializer.Serialize(x), StringComparer.Ordinal).ToArray();
        object graphicalCapture = new { status = "conditional", owner = "Agentic2D.DebugClient.Raylib", reason = "A graphics-capable environment is required for PNG capture." };
        object projectionComparison = new { comparison = "structural-only", normalizedCommandCount = ordered.Length, graphicalCapture = "not-captured-in-headless-environment", conclusion = "Semantic commands are comparable to adapter capture metadata; pixels are not simulation authority." };
        if (graphicalMetadata is not null)
        {
            if (!File.Exists(graphicalMetadata)) diagnostics.Add(new { id = "GEOMETRY0201", severity = "error", message = "Requested graphical capture metadata is missing.", target = graphicalMetadata, field = "--graphical-metadata", partId = (string?)null, remediation = "Run the graphics-capable capture command before comparing it." });
            else
            {
                using var metadata = JsonDocument.Parse(File.ReadAllText(graphicalMetadata));
                var captured = metadata.RootElement.TryGetProperty("parts", out var metadataParts) && metadataParts.ValueKind == JsonValueKind.Array
                    ? metadataParts.EnumerateArray().Select(x => x.TryGetProperty("id", out var id) ? id.GetString() : null).Where(x => x is not null).Cast<string>().Order(StringComparer.Ordinal).ToArray() : [];
                var expected = ordered.Select(x => (string)x.GetType().GetProperty("partId")!.GetValue(x)!).Order(StringComparer.Ordinal).ToArray();
                var matches = captured.SequenceEqual(expected, StringComparer.Ordinal);
                if (!matches) diagnostics.Add(new { id = "GEOMETRY0202", severity = "error", message = "Graphical capture part inventory does not match the structural projection.", target = graphicalMetadata, field = "parts", partId = (string?)null, remediation = "Capture the same normalized visual definition used for inspection." });
                graphicalCapture = new { status = matches ? "captured" : "mismatch", owner = "Agentic2D.DebugClient.Raylib", metadataPath = Path.GetFullPath(graphicalMetadata), capturedPartIds = captured };
                projectionComparison = new { comparison = "structural-versus-graphical", normalizedCommandCount = ordered.Length, graphicalCapture = matches ? "matched" : "mismatch", conclusion = matches ? "The graphical adapter capture names every structurally projected geometry part; pixels remain non-authoritative." : "The capture inventory differs from the structural projection." };
            }
        }
        var status = diagnostics.Any(x => JsonSerializer.Serialize(x).Contains("\"severity\":\"error\"", StringComparison.Ordinal)) ? "failed" : "passed";
        Directory.CreateDirectory(destination);
        var inspection = new { schema = "agentic2d.geometry-inspection.v1", status, previewBackground = new { r = 20, g = 31, b = 48, a = 255 }, parts = ordered, supportedShapeKinds = new[] { "circle", "rectangle", "triangle", "diamond", "regular-polygon", "ring", "line" }, fingerprint = Fingerprint(ordered) };
        await Write(destination, "geometry-inspection.json", inspection);
        await Write(destination, "geometry-preview.json", new { schema = "agentic2d.geometry-preview.v1", status, mode = "headless-structural", background = inspection.previewBackground, commands = ordered.Select((x, i) => new { id = "geometry-command." + i.ToString("D3"), kind = "draw-geometry", part = x }), graphicalCapture });
        await Write(destination, "geometry-diagnostics.json", new { schema = "agentic2d.geometry-diagnostics.v1", status, diagnostics });
        await Write(destination, "geometry-projection-comparison.json", new { schema = "agentic2d.geometry-projection-comparison.v1", status, projectionComparison });
        await output.WriteLineAsync("geometry " + args[1] + ": " + status + "; output: " + destination);
        return status == "passed" ? 0 : 1;
    }

    private static IEnumerable<string> Resolve(string target)
    {
        target = Path.GetFullPath(target);
        if (File.Exists(target) && target.EndsWith(".json", StringComparison.Ordinal)) return new[] { target };
        if (File.Exists(Path.Combine(target, "agentic2d.project.json"))) return Directory.EnumerateFiles(Path.Combine(target, "game-content", "visuals"), "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal);
        if (Directory.Exists(target)) return Directory.EnumerateFiles(target, "*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal);
        return [];
    }
    private static double Contrast(VisualColor? foreground, VisualColor background)
    {
        if (foreground is null) return 21;
        static double Channel(int value) { var c = value / 255d; return c <= .03928 ? c / 12.92 : Math.Pow((c + .055) / 1.055, 2.4); }
        var a = .2126 * Channel(foreground.R) + .7152 * Channel(foreground.G) + .0722 * Channel(foreground.B);
        var b = .2126 * Channel(background.R) + .7152 * Channel(background.G) + .0722 * Channel(background.B);
        return (Math.Max(a, b) + .05) / (Math.Min(a, b) + .05);
    }
    private static string DefinitionId(string file)
    {
        try { using var document = JsonDocument.Parse(File.ReadAllText(file)); return document.RootElement.TryGetProperty("id", out var id) ? id.GetString() ?? "unavailable" : "unavailable"; }
        catch (JsonException) { return "unavailable"; }
    }
    private static string AuthoredValue(string file, string field, string? partId)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file)); var root = document.RootElement;
            if (field is "schema" or "id") return root.TryGetProperty(field, out var rootValue) ? rootValue.GetRawText() : "missing";
            if (!root.TryGetProperty("parts", out var parts) || parts.ValueKind != JsonValueKind.Array) return "missing";
            var part = parts.EnumerateArray().FirstOrDefault(x => partId is null || (x.TryGetProperty("id", out var id) && id.GetString() == partId));
            if (part.ValueKind == JsonValueKind.Undefined) return "missing";
            if (field.Contains("geometry", StringComparison.Ordinal) && part.TryGetProperty("geometry", out var geometry))
            {
                var property = field.EndsWith(".kind", StringComparison.Ordinal) ? "kind" : field.EndsWith("polygonSides", StringComparison.Ordinal) ? "polygonSides" : field.EndsWith("ringInnerRatio", StringComparison.Ordinal) ? "ringInnerRatio" : field.EndsWith("lineEnd", StringComparison.Ordinal) ? "lineEnd" : null;
                return property is not null && geometry.TryGetProperty(property, out var value) ? value.GetRawText() : geometry.GetRawText();
            }
            return field.Contains("world", StringComparison.Ordinal) && part.TryGetProperty("worldSize", out var size) ? size.GetRawText() : part.GetRawText();
        }
        catch (JsonException) { return "unavailable (invalid JSON)"; }
    }
    private static string AcceptedRange(string diagnosticId) => diagnosticId switch
    {
        "GEOMETRY0002" => "circle, rectangle, triangle, diamond, regular-polygon, ring, or line",
        "GEOMETRY0003" => "RGBA byte channels 0 through 255; fill required except for ring and line",
        "GEOMETRY0004" => "opacity 0 through 1; finite rotation; outlineWidth >= 0",
        "GEOMETRY0005" => "polygonSides 3 through 12",
        "GEOMETRY0006" => "ringInnerRatio strictly between 0 and 1",
        "GEOMETRY0007" => "finite lineEnd x and y",
        "VISUAL0004" => "anchor top-left, center, or bottom-center; declared layer; sortMode fixed or y",
        "VISUAL0005" => "finite offset and positive finite worldSize dimensions",
        _ => "see the declared visual-definition contract"
    };
    private static Task Write(string directory, string name, object value) => File.WriteAllTextAsync(Path.Combine(directory, name), JsonSerializer.Serialize(value, Json));
    private static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private static string Fingerprint(object value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Json)))).ToLowerInvariant();
}
