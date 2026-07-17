using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic2D.Validation;

public sealed class MapContentValidator
{
    public const string MapsScope = "maps";
    public const string MapSchema = "agentic2d.map.v1";
    public const string SmokeMapId = "map.smoke";
    public const string SmokeMapPath = "game/maps/smoke/map-smoke.map.json";
    public const string ContinuousSmokeMapId = "map.continuous-smoke";
    public const string ContinuousSmokeMapPath = "game/maps/smoke/map-continuous-smoke.map.json";
    public const string InteractionSmokeMapId = "map.interaction-smoke";
    public const string InteractionSmokeMapPath = "game/maps/smoke/map-interaction-smoke.map.json";

    public MapValidationItem ValidateFile(string path)
    {
        var relativePath = ContentTargetResolver.ToRepositoryRelativePath(path);
        JsonDocument document;
        MapContentSource? map;

        try
        {
            using var stream = File.OpenRead(path);
            document = JsonDocument.Parse(stream);
            map = document.Deserialize<MapContentSource>(ContentValidationJson.Options);
        }
        catch (JsonException exception)
        {
            var diagnostic = MapDiagnostic.InvalidField(relativePath, "json", $"Map JSON is malformed: {exception.Message}");
            return MapValidationItem.Failed(relativePath, Path.GetFileNameWithoutExtension(path), [diagnostic]);
        }
        catch (IOException exception)
        {
            var diagnostic = MapDiagnostic.InvalidField(relativePath, "$", $"Could not read map file: {exception.Message}");
            return MapValidationItem.Error(relativePath, Path.GetFileNameWithoutExtension(path), [diagnostic]);
        }

        using (document)
        {
            if (map is null)
            {
                var diagnostic = MapDiagnostic.InvalidField(relativePath, "$", "Map JSON must contain an object.");
                return MapValidationItem.Failed(relativePath, Path.GetFileNameWithoutExtension(path), [diagnostic]);
            }

            var diagnostics = ValidateMap(map, relativePath, Path.Combine(Path.GetDirectoryName(path)!, "..", "assets"));
            var status = diagnostics.Any(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error)
                ? ContentValidationStatus.Failed
                : ContentValidationStatus.Passed;
            var id = string.IsNullOrWhiteSpace(map.Id) ? Path.GetFileNameWithoutExtension(path) : map.Id;
            return new MapValidationItem(map, relativePath, id, status, diagnostics);
        }
    }

    public IReadOnlyList<ContentValidationDiagnostic> ValidateMap(MapContentSource map, string target, string? localAssetRoot = null)
    {
        var diagnostics = new List<ContentValidationDiagnostic>();

        RequireString(map.Schema, target, "schema", diagnostics);
        RequireString(map.Id, target, "id", diagnostics);
        RequireString(map.Title, target, "title", diagnostics);

        if (!StringComparer.Ordinal.Equals(map.Schema, MapSchema))
        {
            diagnostics.Add(MapDiagnostic.InvalidField(target, "schema", "Map schema must be agentic2d.map.v1."));
        }

        if (!IsStableId(map.Id))
        {
            diagnostics.Add(MapDiagnostic.InvalidStableId(target, "id", map.Id));
        }

        if (map.Width <= 0)
        {
            diagnostics.Add(MapDiagnostic.InvalidDimensions(target, "width", "Map width must be a positive integer."));
        }

        if (map.Height <= 0)
        {
            diagnostics.Add(MapDiagnostic.InvalidDimensions(target, "height", "Map height must be a positive integer."));
        }

        if (map.TileSize is null)
        {
            diagnostics.Add(MapDiagnostic.InvalidField(target, "tileSize", "Missing required field: tileSize."));
        }
        else
        {
            if (map.TileSize.Width <= 0)
            {
                diagnostics.Add(MapDiagnostic.InvalidDimensions(target, "tileSize.width", "tileSize.width must be a positive integer."));
            }

            if (map.TileSize.Height <= 0)
            {
                diagnostics.Add(MapDiagnostic.InvalidDimensions(target, "tileSize.height", "tileSize.height must be a positive integer."));
            }
        }

        var resolvedAssets = ResolveAssetRefs(map.AssetRefs, target, diagnostics, localAssetRoot, map.GeometryOnly);
        ValidateLayers(map, target, resolvedAssets, diagnostics, map.GeometryOnly);
        ValidateMarkers(map, target, diagnostics);
        ValidateObjects(map, target, diagnostics);
        ValidateEntitySpawns(map, target, diagnostics);

        return diagnostics.OrderBy(static diagnostic => diagnostic.Id, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Target, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.Field, StringComparer.Ordinal)
            .ThenBy(static diagnostic => diagnostic.ItemId, StringComparer.Ordinal)
            .ToArray();
    }

    private static Dictionary<string, AssetMetadataSource> ResolveAssetRefs(
        IReadOnlyList<MapAssetRefSource> assetRefs,
        string target,
        List<ContentValidationDiagnostic> diagnostics, string? localAssetRoot, bool geometryOnly)
    {
        if (assetRefs.Count == 0 && !geometryOnly)
        {
            diagnostics.Add(MapDiagnostic.InvalidField(target, "assetRefs", "assetRefs must contain at least one asset reference."));
            return new Dictionary<string, AssetMetadataSource>(StringComparer.Ordinal);
        }

        var assetIds = new HashSet<string>(StringComparer.Ordinal);
        var resolved = new Dictionary<string, AssetMetadataSource>(StringComparer.Ordinal);

        foreach (var assetRef in assetRefs)
        {
            if (!IsStableId(assetRef.AssetId))
            {
                diagnostics.Add(MapDiagnostic.InvalidStableId(target, "assetRefs[].assetId", assetRef.AssetId));
                continue;
            }

            if (!assetIds.Add(assetRef.AssetId))
            {
                diagnostics.Add(MapDiagnostic.DuplicateIdentity(target, "assetRefs[].assetId", assetRef.AssetId));
                continue;
            }

            var local = localAssetRoot is null || !Directory.Exists(localAssetRoot) ? null : Directory.EnumerateFiles(localAssetRoot, "*.json", SearchOption.AllDirectories).FirstOrDefault(path => JsonDocument.Parse(File.ReadAllText(path)).RootElement.TryGetProperty("id", out var id) && id.GetString() == assetRef.AssetId);
            var assetResolution = local is null ? AssetMetadataLocator.ResolveById(assetRef.AssetId) : null;
            if (local is null && !assetResolution!.IsSuccess)
            {
                diagnostics.AddRange(assetResolution.Diagnostics.Select(diagnostic => MapDiagnostic.MissingAsset(target, assetRef.AssetId, diagnostic.Message)));
                continue;
            }

            var assetValidation = new AssetMetadataValidator().ValidateFile(local ?? assetResolution!.MetadataPath);
            diagnostics.AddRange(assetValidation.Diagnostics.Select(diagnostic => diagnostic.Id == "ASSET0005"
                ? MapDiagnostic.ReviewGateUnsatisfied(target, assetRef.AssetId, diagnostic.Message)
                : diagnostic));

            if (assetValidation.Metadata is not null)
            {
                resolved[assetRef.AssetId] = assetValidation.Metadata;
            }
        }

        return resolved;
    }

    private static void ValidateLayers(
        MapContentSource map,
        string target,
        IReadOnlyDictionary<string, AssetMetadataSource> resolvedAssets,
        List<ContentValidationDiagnostic> diagnostics, bool geometryOnly)
    {
        if (map.Layers.Count == 0 && !geometryOnly)
        {
            diagnostics.Add(MapDiagnostic.InvalidField(target, "layers", "layers must contain at least one layer."));
            return;
        }

        var layerIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var layer in map.Layers)
        {
            if (!IsStableId(layer.Id))
            {
                diagnostics.Add(MapDiagnostic.InvalidStableId(target, "layers[].id", layer.Id));
                continue;
            }

            if (!layerIds.Add(layer.Id))
            {
                diagnostics.Add(MapDiagnostic.DuplicateIdentity(target, "layers[].id", layer.Id));
            }

            if (!StringComparer.Ordinal.Equals(layer.Kind, "tile"))
            {
                diagnostics.Add(MapDiagnostic.UnsupportedLayerKind(target, layer.Id, layer.Kind));
                continue;
            }

            var occupied = new HashSet<string>(StringComparer.Ordinal);
            foreach (var cell in layer.Cells)
            {
                if (cell.X < 0 || cell.X >= map.Width)
                {
                    diagnostics.Add(MapDiagnostic.InvalidDimensions(target, $"layers.{layer.Id}.cells[].x", $"Layer {layer.Id} cell x is outside map bounds: {cell.X}", $"{layer.Id}:{cell.X},{cell.Y}"));
                }

                if (cell.Y < 0 || cell.Y >= map.Height)
                {
                    diagnostics.Add(MapDiagnostic.InvalidDimensions(target, $"layers.{layer.Id}.cells[].y", $"Layer {layer.Id} cell y is outside map bounds: {cell.Y}", $"{layer.Id}:{cell.X},{cell.Y}"));
                }

                var coordinateKey = $"{layer.Id}:{cell.X},{cell.Y}";
                if (!occupied.Add(coordinateKey))
                {
                    diagnostics.Add(MapDiagnostic.DuplicateIdentity(target, $"layers.{layer.Id}.cells", coordinateKey));
                }

                if (!resolvedAssets.TryGetValue(cell.AssetId, out var asset))
                {
                    diagnostics.Add(MapDiagnostic.MissingAsset(target, cell.AssetId, $"Layer {layer.Id} references unknown asset: {cell.AssetId}"));
                    continue;
                }

                if (asset.Tiles.All(tile => !StringComparer.Ordinal.Equals(tile.Id, cell.TileId)))
                {
                    diagnostics.Add(MapDiagnostic.MissingTile(target, cell.TileId, $"Layer {layer.Id} references unknown tile {cell.TileId} in asset {cell.AssetId}."));
                }
            }
        }
    }

    private static void ValidateObjects(MapContentSource map, string target, List<ContentValidationDiagnostic> diagnostics)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in map.Objects)
        {
            if (!IsStableId(item.Id) || !ids.Add(item.Id)) diagnostics.Add(MapDiagnostic.DuplicateIdentity(target, "objects[].id", item.Id));
            if (item.Kind != "static-obstacle" || item.Bounds.Kind != "aabb" || !double.IsFinite(item.Position.X) || !double.IsFinite(item.Position.Y) || !double.IsFinite(item.Bounds.HalfWidth) || !double.IsFinite(item.Bounds.HalfHeight) || item.Bounds.HalfWidth <= 0 || item.Bounds.HalfHeight <= 0) diagnostics.Add(MapDiagnostic.InvalidField(target, "objects", "Static objects require finite AABB position and positive half-extents."));
            if (item.AssetId is not null && map.AssetRefs.All(a => a.AssetId != item.AssetId)) diagnostics.Add(MapDiagnostic.MissingAsset(target, item.AssetId, "Object asset reference is not declared."));
        }
    }

    private static void ValidateEntitySpawns(MapContentSource map, string target, List<ContentValidationDiagnostic> diagnostics)
    {
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var entities = new HashSet<string>(StringComparer.Ordinal);
        foreach (var spawn in map.EntitySpawns)
        {
            if (!IsStableId(spawn.Id) || !ids.Add(spawn.Id) || !IsStableId(spawn.EntityId) || !entities.Add(spawn.EntityId) || !IsStableId(spawn.DefinitionId))
                diagnostics.Add(MapDiagnostic.InvalidField(target, "entitySpawns", "Entity spawns require unique stable spawn, entity, and definition IDs."));
            if (spawn.Overrides.GroupBy(x => x.ComponentType, StringComparer.Ordinal).Any(x => x.Count() != 1))
                diagnostics.Add(MapDiagnostic.InvalidField(target, "entitySpawns.overrides", "Spawn overrides replace complete values and cannot repeat a component type."));
        }
    }

    private static void ValidateMarkers(MapContentSource map, string target, List<ContentValidationDiagnostic> diagnostics)
    {
        var markerIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var marker in map.Markers)
        {
            if (!IsStableId(marker.Id))
            {
                diagnostics.Add(MapDiagnostic.InvalidStableId(target, "markers[].id", marker.Id));
                continue;
            }

            if (!markerIds.Add(marker.Id))
            {
                diagnostics.Add(MapDiagnostic.DuplicateIdentity(target, "markers[].id", marker.Id));
            }

            if (marker.X < 0 || marker.X >= map.Width)
            {
                diagnostics.Add(MapDiagnostic.InvalidDimensions(target, "markers[].x", $"Marker {marker.Id} x is outside map bounds: {marker.X}", marker.Id));
            }

            if (marker.Y < 0 || marker.Y >= map.Height)
            {
                diagnostics.Add(MapDiagnostic.InvalidDimensions(target, "markers[].y", $"Marker {marker.Id} y is outside map bounds: {marker.Y}", marker.Id));
            }
        }
    }

    private static void RequireString(string? value, string target, string field, List<ContentValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(MapDiagnostic.InvalidField(target, field, $"Missing required field: {field}."));
        }
    }

    private static bool IsStableId(string? value)
    {
        return !string.IsNullOrWhiteSpace(value)
            && value.All(static character => char.IsLetterOrDigit(character) || character is '.' or '-' or '_');
    }
}

public sealed class MapInspector
{
    public MapInspectionRun Inspect(string target)
    {
        var resolution = ResolveTarget(target);
        if (!resolution.IsSuccess)
        {
            return MapInspectionRun.From(target, null, string.Empty, ContentValidationStatus.Failed, 1, resolution.Diagnostics);
        }

        var validation = new MapContentValidator().ValidateFile(resolution.MapPath);
        var exitCode = validation.Status == ContentValidationStatus.Passed ? 0 : 1;

        return MapInspectionRun.From(target, validation.Map, validation.Path, validation.Status, exitCode, validation.Diagnostics);
    }

    public static MapTargetResolution ResolveTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return MapTargetResolution.Failure([MapDiagnostic.InvalidField(target, "target", "Map target must not be empty.")]);
        }

        var candidate = StringComparer.Ordinal.Equals(target, MapContentValidator.SmokeMapId)
            ? MapContentValidator.SmokeMapPath
            : StringComparer.Ordinal.Equals(target, MapContentValidator.ContinuousSmokeMapId)
                ? MapContentValidator.ContinuousSmokeMapPath
                : StringComparer.Ordinal.Equals(target, MapContentValidator.InteractionSmokeMapId)
                    ? MapContentValidator.InteractionSmokeMapPath
                    : target;

        if (!candidate.EndsWith(".map.json", StringComparison.OrdinalIgnoreCase))
        {
            return MapTargetResolution.Failure([MapDiagnostic.InvalidField(target, "target", "Unsupported map target. Expected map.smoke or a repository-relative .map.json path.")]);
        }

        if (Path.IsPathRooted(candidate) || candidate.Split(['/', '\\']).Contains("..", StringComparer.Ordinal))
        {
            return MapTargetResolution.Failure([MapDiagnostic.InvalidField(target, "target", "Map path must be repository-relative and must not escape the repository.")]);
        }

        var resolvedPath = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), candidate);
        return File.Exists(resolvedPath)
            ? MapTargetResolution.Success(resolvedPath)
            : MapTargetResolution.Failure([MapDiagnostic.InvalidField(target, "target", $"Map file was not found: {candidate}")]);
    }
}

public static class MapInspectionArtifactWriter
{
    public static async Task<int> WriteAsync(string outputDirectory, MapInspectionRun run)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(run.Result, ContentValidationJson.Options));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "diagnostics.json"), JsonSerializer.Serialize(run.DiagnosticsDocument, ContentValidationJson.Options));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "map-summary.json"), JsonSerializer.Serialize(run.MapSummary, ContentValidationJson.Options));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "layers.json"), JsonSerializer.Serialize(run.LayersDocument, ContentValidationJson.Options));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "resolved-references.json"), JsonSerializer.Serialize(run.ResolvedReferences, ContentValidationJson.Options));
            return run.Result.ExitCode;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"failed to write map inspection artifacts: {exception.Message}", exception);
        }
    }
}

public sealed record MapInspectionRun(
    MapInspectionResultDocument Result,
    ContentDiagnosticsDocument DiagnosticsDocument,
    MapSummaryDocument MapSummary,
    MapLayersDocument LayersDocument,
    MapResolvedReferencesDocument ResolvedReferences)
{
    public static MapInspectionRun From(
        string target,
        MapContentSource? map,
        string mapPath,
        string status,
        int exitCode,
        IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        var artifacts = new[]
        {
            new ContentArtifactReference("diagnostics.json", "diagnostics"),
            new ContentArtifactReference("map-summary.json", "map-summary"),
            new ContentArtifactReference("layers.json", "layers"),
            new ContentArtifactReference("resolved-references.json", "resolved-references"),
        };

        var result = new MapInspectionResultDocument(
            "agentic2d.map-inspection.result.v1",
            "map inspect",
            target,
            map?.Id ?? string.Empty,
            status,
            exitCode,
            new MapInspectionSummary(
                map?.Layers.Count ?? 0,
                map?.Layers.Sum(static layer => layer.Cells.Count) ?? 0,
                map?.Markers.Count ?? 0,
                map?.AssetRefs.Count ?? 0,
                diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error),
                diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Warning)),
            diagnostics,
            artifacts);

        return new MapInspectionRun(
            result,
            new ContentDiagnosticsDocument("agentic2d.map-inspection.diagnostics.v1", diagnostics),
            MapSummaryDocument.From(map, mapPath),
            MapLayersDocument.From(map),
            MapResolvedReferencesDocument.From(map));
    }
}

public sealed record MapInspectionResultDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("mapId")] string MapId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("summary")] MapInspectionSummary Summary,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ContentArtifactReference> Artifacts);

public sealed record MapInspectionSummary(
    [property: JsonPropertyName("layers")] int Layers,
    [property: JsonPropertyName("cells")] int Cells,
    [property: JsonPropertyName("markers")] int Markers,
    [property: JsonPropertyName("assets")] int Assets,
    [property: JsonPropertyName("errors")] int Errors,
    [property: JsonPropertyName("warnings")] int Warnings);

public sealed record MapSummaryDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("map")] MapSummaryIdentity Map,
    [property: JsonPropertyName("tileSize")] MapTileSizeSource TileSize,
    [property: JsonPropertyName("counts")] MapSummaryCounts Counts,
    [property: JsonPropertyName("assetRefs")] IReadOnlyList<MapAssetRefSource> AssetRefs)
{
    public static MapSummaryDocument From(MapContentSource? map, string mapPath)
    {
        return new MapSummaryDocument(
            "agentic2d.map-inspection.summary.v1",
            new MapSummaryIdentity(map?.Id ?? string.Empty, mapPath, map?.Title ?? string.Empty, map?.Width ?? 0, map?.Height ?? 0),
            map?.TileSize ?? new MapTileSizeSource(0, 0),
            new MapSummaryCounts(map?.Layers.Count ?? 0, map?.Layers.Sum(static layer => layer.Cells.Count) ?? 0, map?.Markers.Count ?? 0),
            map?.AssetRefs.OrderBy(static item => item.AssetId, StringComparer.Ordinal).ToArray() ?? []);
    }
}

public sealed record MapSummaryIdentity(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

public sealed record MapSummaryCounts(
    [property: JsonPropertyName("layers")] int Layers,
    [property: JsonPropertyName("cells")] int Cells,
    [property: JsonPropertyName("markers")] int Markers);

public sealed record MapLayersDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("mapId")] string MapId,
    [property: JsonPropertyName("layers")] IReadOnlyList<MapLayerInspection> Layers)
{
    public static MapLayersDocument From(MapContentSource? map)
    {
        return new MapLayersDocument(
            "agentic2d.map-inspection.layers.v1",
            map?.Id ?? string.Empty,
            map?.Layers.Select(layer => new MapLayerInspection(
                    layer.Id,
                    layer.Kind,
                    layer.Cells.OrderBy(static cell => cell.Y)
                        .ThenBy(static cell => cell.X)
                        .ThenBy(static cell => cell.AssetId, StringComparer.Ordinal)
                        .ThenBy(static cell => cell.TileId, StringComparer.Ordinal)
                        .ToArray()))
                .OrderBy(static layer => layer.Id, StringComparer.Ordinal)
                .ToArray() ?? []);
    }
}

public sealed record MapLayerInspection(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("cells")] IReadOnlyList<MapCellSource> Cells);

public sealed record MapResolvedReferencesDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("mapId")] string MapId,
    [property: JsonPropertyName("references")] IReadOnlyList<MapResolvedReference> References)
{
    public static MapResolvedReferencesDocument From(MapContentSource? map)
    {
        if (map is null)
        {
            return new MapResolvedReferencesDocument("agentic2d.map-inspection.resolved-references.v1", string.Empty, []);
        }

        var references = new List<MapResolvedReference>();
        foreach (var layer in map.Layers.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            foreach (var cell in layer.Cells.OrderBy(static item => item.Y).ThenBy(static item => item.X).ThenBy(static item => item.AssetId, StringComparer.Ordinal).ThenBy(static item => item.TileId, StringComparer.Ordinal))
            {
                references.Add(new MapResolvedReference(layer.Id, cell.X, cell.Y, cell.AssetId, cell.TileId));
            }
        }

        return new MapResolvedReferencesDocument("agentic2d.map-inspection.resolved-references.v1", map.Id, references);
    }
}

public sealed record MapResolvedReference(
    [property: JsonPropertyName("layerId")] string LayerId,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("assetId")] string AssetId,
    [property: JsonPropertyName("tileId")] string TileId);

public sealed record MapValidationItem(
    MapContentSource? Map,
    string Path,
    string Id,
    string Status,
    IReadOnlyList<ContentValidationDiagnostic> Diagnostics)
{
    public static MapValidationItem Failed(string path, string id, IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        return new MapValidationItem(null, path, id, ContentValidationStatus.Failed, diagnostics);
    }

    public static MapValidationItem Error(string path, string id, IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        return new MapValidationItem(null, path, id, ContentValidationStatus.Error, diagnostics);
    }
}

public sealed class MapTargetResolution
{
    private MapTargetResolution(string mapPath, IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        MapPath = mapPath;
        Diagnostics = diagnostics;
    }

    public bool IsSuccess => Diagnostics.Count == 0;

    public string MapPath { get; }

    public IReadOnlyList<ContentValidationDiagnostic> Diagnostics { get; }

    public static MapTargetResolution Success(string mapPath)
    {
        return new MapTargetResolution(mapPath, []);
    }

    public static MapTargetResolution Failure(IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        return new MapTargetResolution(string.Empty, diagnostics);
    }
}

public sealed class MapContentSource
{
    [JsonPropertyName("schema")]
    public string Schema { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("width")]
    public int Width { get; init; }

    [JsonPropertyName("height")]
    public int Height { get; init; }

    [JsonPropertyName("tileSize")]
    public MapTileSizeSource? TileSize { get; init; }

    [JsonPropertyName("assetRefs")]
    public IReadOnlyList<MapAssetRefSource> AssetRefs { get; init; } = [];

    [JsonPropertyName("geometryOnly")]
    public bool GeometryOnly { get; init; }

    [JsonPropertyName("layers")]
    public IReadOnlyList<MapLayerSource> Layers { get; init; } = [];

    [JsonPropertyName("markers")]
    public IReadOnlyList<MapMarkerSource> Markers { get; init; } = [];

    [JsonPropertyName("cellOverrides")]
    public IReadOnlyList<MapCellOverrideSource> CellOverrides { get; init; } = [];

    [JsonPropertyName("objects")]
    public IReadOnlyList<MapObjectSource> Objects { get; init; } = [];

    [JsonPropertyName("entitySpawns")]
    public IReadOnlyList<EntitySpawnSource> EntitySpawns { get; init; } = [];
}

public sealed record MapTileSizeSource(
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

public sealed record MapAssetRefSource([property: JsonPropertyName("assetId")] string AssetId);

public sealed record MapLayerSource(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("cells")] IReadOnlyList<MapCellSource> Cells)
{
    public MapLayerSource()
        : this(string.Empty, string.Empty, [])
    {
    }
}

public sealed record MapCellSource(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("assetId")] string AssetId,
    [property: JsonPropertyName("tileId")] string TileId);

public sealed record MapMarkerSource(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y);

public sealed record MapCellOverrideSource(
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("physicalBehavior")] string PhysicalBehavior);

public sealed record MapObjectSource([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("kind")] string Kind, [property: JsonPropertyName("assetId")] string? AssetId, [property: JsonPropertyName("position")] MapObjectPosition Position, [property: JsonPropertyName("bounds")] MapObjectBounds Bounds, [property: JsonPropertyName("visualDefinitionId")] string? VisualDefinitionId = null);
public sealed record MapObjectPosition([property: JsonPropertyName("x")] double X, [property: JsonPropertyName("y")] double Y);
public sealed record MapObjectBounds([property: JsonPropertyName("kind")] string Kind, [property: JsonPropertyName("halfWidth")] double HalfWidth, [property: JsonPropertyName("halfHeight")] double HalfHeight);

public static class MapDiagnostic
{
    public static ContentValidationDiagnostic InvalidField(string target, string field, string message)
    {
        return new ContentValidationDiagnostic("MAP0001", ContentDiagnosticSeverity.Error, message, target, field);
    }

    public static ContentValidationDiagnostic InvalidStableId(string target, string field, string itemId)
    {
        return new ContentValidationDiagnostic("MAP0002", ContentDiagnosticSeverity.Error, $"Invalid stable ID: {itemId}", target, field, itemId);
    }

    public static ContentValidationDiagnostic InvalidDimensions(string target, string field, string message, string? itemId = null)
    {
        return new ContentValidationDiagnostic("MAP0003", ContentDiagnosticSeverity.Error, message, target, field, itemId);
    }

    public static ContentValidationDiagnostic DuplicateIdentity(string target, string field, string itemId)
    {
        return new ContentValidationDiagnostic("MAP0004", ContentDiagnosticSeverity.Error, $"Duplicate identity: {itemId}", target, field, itemId);
    }

    public static ContentValidationDiagnostic MissingAsset(string target, string itemId, string message)
    {
        return new ContentValidationDiagnostic("MAP0005", ContentDiagnosticSeverity.Error, message, target, null, itemId);
    }

    public static ContentValidationDiagnostic MissingTile(string target, string itemId, string message)
    {
        return new ContentValidationDiagnostic("MAP0006", ContentDiagnosticSeverity.Error, message, target, null, itemId);
    }

    public static ContentValidationDiagnostic UnsupportedLayerKind(string target, string itemId, string kind)
    {
        return new ContentValidationDiagnostic("MAP0007", ContentDiagnosticSeverity.Error, $"Unsupported layer kind: {kind}", target, "layers[].kind", itemId);
    }

    public static ContentValidationDiagnostic ReviewGateUnsatisfied(string target, string itemId, string message)
    {
        return new ContentValidationDiagnostic("MAP0008", ContentDiagnosticSeverity.Error, message, target, null, itemId);
    }
}
