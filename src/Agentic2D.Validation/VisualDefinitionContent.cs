using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace Agentic2D.Validation;

public sealed class VisualDefinitionValidator
{
    public const string VisualsScope = "visuals", Schema = "agentic2d.visual-definition.v1"; public static readonly string[] Layers = ["background", "ground", "entities", "foreground", "debug", "ui"];
    public VisualDefinitionValidationItem ValidateFile(string path) { var rel = ContentTargetResolver.ToRepositoryRelativePath(path); try { var d = JsonSerializer.Deserialize<VisualDefinitionSource>(File.ReadAllText(path), ContentValidationJson.Options); var errors = Validate(d!, rel); return new(d, rel, errors.Count == 0 ? ContentValidationStatus.Passed : ContentValidationStatus.Failed, errors); } catch (Exception e) when (e is IOException or JsonException) { return new(null, rel, ContentValidationStatus.Failed, [D(rel, "VISUAL0001", "json", e.Message)]); } }
    public IReadOnlyList<ContentValidationDiagnostic> Validate(VisualDefinitionSource d, string target) { var r = new List<ContentValidationDiagnostic>(); if (d is null || d.Schema != Schema || !Stable(d.Id) || !d.Id.StartsWith("visual-definition.")) r.Add(D(target, "VISUAL0001", "schema", "Invalid visual definition schema or ID.")); if (d?.Parts.Count == 0) r.Add(D(target, "VISUAL0001", "parts", "Visual definition needs parts.")); var ids = new HashSet<string>(); foreach (var p in d?.Parts ?? []) { if (!Stable(p.Id) || !ids.Add(p.Id)) r.Add(D(target, "VISUAL0002", "parts[].id", "Duplicate/invalid part ID.", p.Id)); var hasAsset = Stable(p.AssetId) && Stable(p.RegionId); var hasGeometry = p.Geometry is not null; if (hasAsset == hasGeometry) r.Add(D(target, "GEOMETRY0001", "parts", "A visual part must use exactly one source kind: asset-region or geometry.", p.Id)); if (!hasAsset && !hasGeometry) r.Add(D(target, "VISUAL0003", "parts", "Asset and region IDs are required for asset-region parts.", p.Id)); if (hasGeometry) ValidateGeometry(p.Geometry!, target, p.Id, r); if (!new[] { "top-left", "center", "bottom-center" }.Contains(p.Anchor) || !Layers.Contains(p.Layer) || !new[] { "fixed", "y" }.Contains(p.SortMode)) r.Add(D(target, "VISUAL0004", "parts", "Unsupported anchor/layer/sort mode.", p.Id)); if (!double.IsFinite(p.Offset.X) || !double.IsFinite(p.Offset.Y) || !double.IsFinite(p.WorldSize.Width) || !double.IsFinite(p.WorldSize.Height) || p.WorldSize.Width <= 0 || p.WorldSize.Height <= 0) r.Add(D(target, "VISUAL0005", "parts", "Invalid finite world geometry.", p.Id)); } return r.OrderBy(x => x.Id, StringComparer.Ordinal).ThenBy(x => x.ItemId, StringComparer.Ordinal).ToArray(); }
    private static void ValidateGeometry(GeometryVisualSource x, string target, string part, List<ContentValidationDiagnostic> diagnostics)
    {
        if (!new[] { "circle", "rectangle", "triangle", "diamond", "regular-polygon", "ring", "line" }.Contains(x.Kind)) diagnostics.Add(D(target, "GEOMETRY0002", "parts[].geometry.kind", "Unsupported geometry kind.", part));
        if ((x.Kind is not ("ring" or "line") && !Color(x.Fill)) || (x.Fill is not null && !Color(x.Fill)) || (x.Outline is not null && !Color(x.Outline))) diagnostics.Add(D(target, "GEOMETRY0003", "parts[].geometry", "Geometry colors must be byte RGBA values.", part));
        if (!double.IsFinite(x.Opacity) || x.Opacity < 0 || x.Opacity > 1 || !double.IsFinite(x.Rotation) || !double.IsFinite(x.OutlineWidth) || x.OutlineWidth < 0) diagnostics.Add(D(target, "GEOMETRY0004", "parts[].geometry", "Geometry opacity, rotation, or outline width is invalid.", part));
        if (x.Kind == "regular-polygon" && (x.PolygonSides is < 3 or > 12)) diagnostics.Add(D(target, "GEOMETRY0005", "parts[].geometry.polygonSides", "Regular polygons require 3 through 12 sides.", part));
        if (x.Kind == "ring" && (!double.IsFinite(x.RingInnerRatio) || x.RingInnerRatio <= 0 || x.RingInnerRatio >= 1)) diagnostics.Add(D(target, "GEOMETRY0006", "parts[].geometry.ringInnerRatio", "Rings require an inner ratio strictly between zero and one.", part));
        if (x.Kind == "line" && (x.LineEnd is null || !double.IsFinite(x.LineEnd.X) || !double.IsFinite(x.LineEnd.Y))) diagnostics.Add(D(target, "GEOMETRY0007", "parts[].geometry.lineEnd", "Lines require a finite end point.", part));
    }
    private static bool Color(VisualColor? x) => x is not null && x.R is >= 0 and <= 255 && x.G is >= 0 and <= 255 && x.B is >= 0 and <= 255 && x.A is >= 0 and <= 255;
    static bool Stable(string? x) => !string.IsNullOrWhiteSpace(x) && x.All(c => char.IsLetterOrDigit(c) || c is '.' or '-'); static ContentValidationDiagnostic D(string t, string id, string f, string m, string? i = null) => new(id, ContentDiagnosticSeverity.Error, m, t, f, i);
}
public sealed class VisualDefinitionCatalog
{
    public VisualDefinitionCatalog(IReadOnlyDictionary<string, VisualDefinitionSource> d, string r) { Definitions = d; Revision = r; }
    public IReadOnlyDictionary<string, VisualDefinitionSource> Definitions { get; }
    public string Revision { get; }
    public bool TryGet(string id, out VisualDefinitionSource? d) => Definitions.TryGetValue(id, out d);
    public static VisualDefinitionCatalog LoadAll(out IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        var root = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game", "visuals");
        return LoadFiles(Directory.Exists(root) ? Directory.EnumerateFiles(root, "*.json", SearchOption.AllDirectories) : [], out diagnostics);
    }
    public static VisualDefinitionCatalog LoadFiles(IEnumerable<string> paths, out IReadOnlyList<ContentValidationDiagnostic> diagnostics) { var v = new VisualDefinitionValidator(); var a = new List<ContentValidationDiagnostic>(); var d = new Dictionary<string, VisualDefinitionSource>(StringComparer.Ordinal); foreach (var p in paths.OrderBy(x => x, StringComparer.Ordinal)) { var x = v.ValidateFile(p); a.AddRange(x.Diagnostics); if (x.Definition is not null && x.Status == ContentValidationStatus.Passed && !d.TryAdd(x.Definition.Id, x.Definition)) a.Add(new("VISUAL0002", ContentDiagnosticSeverity.Error, "Duplicate visual ID.", x.Path, "id", x.Definition.Id)); } diagnostics = a; var raw = string.Join("|", d.OrderBy(x => x.Key).Select(x => x.Key + JsonSerializer.Serialize(x.Value))); return new(d, "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw))).ToLowerInvariant()); }
}
public sealed record VisualDefinitionValidationItem(VisualDefinitionSource? Definition, string Path, string Status, IReadOnlyList<ContentValidationDiagnostic> Diagnostics);
public sealed class VisualDefinitionSource { [JsonPropertyName("schema")] public string Schema { get; init; } = ""; [JsonPropertyName("id")] public string Id { get; init; } = ""; [JsonPropertyName("parts")] public IReadOnlyList<VisualPartSource> Parts { get; init; } = []; }
public sealed record VisualPartSource([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("assetId")] string? AssetId, [property: JsonPropertyName("regionId")] string? RegionId, [property: JsonPropertyName("anchor")] string Anchor, [property: JsonPropertyName("offset")] VisualPoint Offset, [property: JsonPropertyName("worldSize")] VisualSize WorldSize, [property: JsonPropertyName("layer")] string Layer, [property: JsonPropertyName("order")] int Order, [property: JsonPropertyName("sortMode")] string SortMode, [property: JsonPropertyName("tint")] VisualColor Tint)
{ [JsonPropertyName("geometry")] public GeometryVisualSource? Geometry { get; init; } }
public sealed record VisualPoint([property: JsonPropertyName("x")] double X, [property: JsonPropertyName("y")] double Y); public sealed record VisualSize([property: JsonPropertyName("width")] double Width, [property: JsonPropertyName("height")] double Height); public sealed record VisualColor([property: JsonPropertyName("r")] int R, [property: JsonPropertyName("g")] int G, [property: JsonPropertyName("b")] int B, [property: JsonPropertyName("a")] int A);
public sealed record GeometryVisualSource
{
    [JsonPropertyName("kind")] public string Kind { get; init; } = "";
    [JsonPropertyName("fill")] public VisualColor? Fill { get; init; }
    [JsonPropertyName("outline")] public VisualColor? Outline { get; init; }
    [JsonPropertyName("outlineWidth")] public double OutlineWidth { get; init; }
    [JsonPropertyName("opacity")] public double Opacity { get; init; } = 1;
    [JsonPropertyName("rotation")] public double Rotation { get; init; }
    [JsonPropertyName("polygonSides")] public int PolygonSides { get; init; }
    [JsonPropertyName("ringInnerRatio")] public double RingInnerRatio { get; init; }
    [JsonPropertyName("lineEnd")] public VisualPoint? LineEnd { get; init; }
}
