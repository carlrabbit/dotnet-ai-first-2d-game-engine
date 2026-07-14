using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Agentic2D.Validation;

public sealed class AssetMetadataValidator
{
    public const string AssetsScope = "assets";
    public const string AssetSchema = "agentic2d.asset-metadata.v1";
    public const string TileAtlasKind = "tile-atlas";
    public const string SmokeAssetId = "asset.tile-atlas-smoke";
    public const string SmokeAssetPath = "game/assets/metadata/tile-atlas-smoke.asset.json";
    public const string RenderSmokeAssetId = "asset.render-atlas-smoke";
    public const string RenderSmokeAssetPath = "game/assets/metadata/render-atlas-smoke.asset.json";

    private static readonly Regex AssetIdPattern = new("^[a-z0-9]+([.-][a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private static readonly HashSet<string> HighImpactSemantics = new(StringComparer.Ordinal)
    {
        "walkable",
        "blocked",
        "collision",
        "navigation-cost",
        "damage",
        "interactable",
        "progression-blocker",
        "spawnable",
    };

    public AssetValidationItem ValidateFile(string path)
    {
        var relativePath = ContentTargetResolver.ToRepositoryRelativePath(path);
        return ValidateJsonCore(relativePath, path, () => File.OpenRead(path));
    }

    public AssetValidationItem ValidateJson(string displayPath, string json)
    {
        return ValidateJsonCore(displayPath, displayPath, () => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)));
    }

    private AssetValidationItem ValidateJsonCore(string relativePath, string idFallbackPath, Func<Stream> openStream)
    {
        JsonDocument document;
        AssetMetadataSource? metadata;

        try
        {
            using var stream = openStream();
            document = JsonDocument.Parse(stream);
            metadata = document.Deserialize<AssetMetadataSource>(ContentValidationJson.Options);
        }
        catch (JsonException exception)
        {
            var diagnostic = AssetDiagnostic.InvalidSchemaValue(relativePath, "json", $"Asset metadata JSON is malformed: {exception.Message}");
            return AssetValidationItem.Failed(relativePath, Path.GetFileNameWithoutExtension(idFallbackPath), [diagnostic]);
        }
        catch (IOException exception)
        {
            var diagnostic = AssetDiagnostic.InvalidSourceReference(relativePath, "$", $"Could not read asset metadata file: {exception.Message}");
            return AssetValidationItem.Error(relativePath, Path.GetFileNameWithoutExtension(idFallbackPath), [diagnostic]);
        }

        using (document)
        {
            if (metadata is null)
            {
                var diagnostic = AssetDiagnostic.MissingRequiredField(relativePath, "$", "Asset metadata JSON must contain an object.");
                return AssetValidationItem.Failed(relativePath, Path.GetFileNameWithoutExtension(idFallbackPath), [diagnostic]);
            }

            var diagnostics = new List<ContentValidationDiagnostic>();
            ValidateContract(document.RootElement, metadata, relativePath, diagnostics);
            var status = diagnostics.Any(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error)
                ? ContentValidationStatus.Failed
                : ContentValidationStatus.Passed;
            var id = string.IsNullOrWhiteSpace(metadata.Id) ? Path.GetFileNameWithoutExtension(idFallbackPath) : metadata.Id;
            return new AssetValidationItem(metadata, relativePath, id, status, diagnostics);
        }
    }

    public static void ValidateGridAgainstImage(
        AssetMetadataSource metadata,
        PngImageInfo imageInfo,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (metadata.TileAtlas is null)
        {
            return;
        }

        var expectedWidth = metadata.TileAtlas.TileWidth * metadata.TileAtlas.Columns;
        var expectedHeight = metadata.TileAtlas.TileHeight * metadata.TileAtlas.Rows;
        if (expectedWidth != imageInfo.Width || expectedHeight != imageInfo.Height)
        {
            diagnostics.Add(AssetDiagnostic.InvalidTileGrid(
                target,
                "tileAtlas",
                $"Declared tile grid is {expectedWidth}x{expectedHeight}, but PNG dimensions are {imageInfo.Width}x{imageInfo.Height}."));
        }
    }

    private static void ValidateContract(
        JsonElement root,
        AssetMetadataSource metadata,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        foreach (var field in AssetRequiredFields.TopLevel)
        {
            if (!root.TryGetProperty(field, out _))
            {
                diagnostics.Add(AssetDiagnostic.MissingRequiredField(target, field, $"Missing required field: {field}"));
            }
        }

        RequireString(metadata.Schema, target, "schema", diagnostics);
        RequireString(metadata.Id, target, "id", diagnostics);
        RequireString(metadata.Kind, target, "kind", diagnostics);
        RequireString(metadata.Title, target, "title", diagnostics);
        RequireString(metadata.Purpose, target, "purpose", diagnostics);

        if (!StringComparer.Ordinal.Equals(metadata.Schema, AssetSchema))
        {
            diagnostics.Add(AssetDiagnostic.InvalidSchemaValue(target, "schema", "Asset metadata schema must be agentic2d.asset-metadata.v1."));
        }

        if (!string.IsNullOrWhiteSpace(metadata.Id) && !AssetIdPattern.IsMatch(metadata.Id))
        {
            diagnostics.Add(AssetDiagnostic.InvalidStableId(target, "id", metadata.Id, "Asset ID must use lowercase dotted segments."));
        }

        if (!StringComparer.Ordinal.Equals(metadata.Kind, TileAtlasKind))
        {
            diagnostics.Add(AssetDiagnostic.UnsupportedKind(target, "kind", metadata.Kind ?? string.Empty));
        }

        ValidateSource(metadata.Source, target, diagnostics);
        ValidateTileAtlas(metadata.TileAtlas, target, diagnostics);
        ValidateTiles(metadata, target, diagnostics);
        ValidateSemantics(metadata, target, diagnostics);
        ValidateProvenance(metadata.Provenance, target, diagnostics);
        ValidateHumanReview(metadata.HumanReview, target, diagnostics);
    }

    private static void ValidateSource(
        AssetSourceReference? source,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (source is null)
        {
            return;
        }

        RequireString(source.Path, target, "source.path", diagnostics);
        RequireString(source.MediaType, target, "source.mediaType", diagnostics);

        if (!StringComparer.Ordinal.Equals(source.MediaType, "image/png"))
        {
            diagnostics.Add(AssetDiagnostic.UnsupportedMediaType(target, "source.mediaType", source.MediaType ?? string.Empty));
        }

        if (string.IsNullOrWhiteSpace(source.Path))
        {
            return;
        }

        if (Path.IsPathRooted(source.Path) || source.Path.Split(['/', '\\']).Contains("..", StringComparer.Ordinal))
        {
            diagnostics.Add(AssetDiagnostic.InvalidSourceReference(target, "source.path", "source.path must be repository-relative and must not escape the repository."));
            return;
        }

        var resolvedPath = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), source.Path);
        if (!File.Exists(resolvedPath))
        {
            diagnostics.Add(AssetDiagnostic.InvalidSourceReference(target, "source.path", $"Asset source file was not found: {source.Path}"));
        }
    }

    private static void ValidateTileAtlas(
        TileAtlasDeclaration? tileAtlas,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (tileAtlas is null)
        {
            return;
        }

        if (tileAtlas.TileWidth <= 0)
        {
            diagnostics.Add(AssetDiagnostic.InvalidTileGrid(target, "tileAtlas.tileWidth", "tileAtlas.tileWidth must be a positive integer."));
        }

        if (tileAtlas.TileHeight <= 0)
        {
            diagnostics.Add(AssetDiagnostic.InvalidTileGrid(target, "tileAtlas.tileHeight", "tileAtlas.tileHeight must be a positive integer."));
        }

        if (tileAtlas.Columns <= 0)
        {
            diagnostics.Add(AssetDiagnostic.InvalidTileGrid(target, "tileAtlas.columns", "tileAtlas.columns must be a positive integer."));
        }

        if (tileAtlas.Rows <= 0)
        {
            diagnostics.Add(AssetDiagnostic.InvalidTileGrid(target, "tileAtlas.rows", "tileAtlas.rows must be a positive integer."));
        }
    }

    private static void ValidateTiles(
        AssetMetadataSource metadata,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (metadata.Tiles.Count == 0)
        {
            diagnostics.Add(AssetDiagnostic.MissingRequiredField(target, "tiles", "tiles must contain at least one tile entry."));
            return;
        }

        var tileIds = new HashSet<string>(StringComparer.Ordinal);
        var coordinates = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tile in metadata.Tiles)
        {
            var tileId = tile.Id ?? string.Empty;
            if (RequireStableTileId(tile.Id, target, "tiles[].id", diagnostics) && !tileIds.Add(tileId))
            {
                diagnostics.Add(AssetDiagnostic.DuplicateTile(target, "tiles[].id", tileId));
            }

            if (metadata.TileAtlas is not null)
            {
                if (tile.X < 0 || tile.X >= metadata.TileAtlas.Columns)
                {
                    diagnostics.Add(AssetDiagnostic.InvalidTileGrid(target, "tiles[].x", $"Tile x coordinate is outside the declared atlas columns: {tile.X}", tile.Id));
                }

                if (tile.Y < 0 || tile.Y >= metadata.TileAtlas.Rows)
                {
                    diagnostics.Add(AssetDiagnostic.InvalidTileGrid(target, "tiles[].y", $"Tile y coordinate is outside the declared atlas rows: {tile.Y}", tile.Id));
                }
            }

            var coordinate = $"{tile.X},{tile.Y}";
            if (!coordinates.Add(coordinate))
            {
                diagnostics.Add(AssetDiagnostic.DuplicateTile(target, "tiles[].x,y", tileId, $"Duplicate tile coordinate: {coordinate}"));
            }
        }
    }

    private static void ValidateSemantics(
        AssetMetadataSource metadata,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (metadata.Semantics is null)
        {
            return;
        }

        var approvedScopes = metadata.HumanReview?.Approvals
            .Select(static approval => approval.Scope)
            .Where(static scope => !string.IsNullOrWhiteSpace(scope))
            .Select(static scope => scope!)
            .ToHashSet(StringComparer.Ordinal) ?? [];

        if (metadata.Semantics.PhysicalBehaviorsApproved.Count > 0 && approvedScopes.Count == 0)
        {
            diagnostics.Add(AssetDiagnostic.SemanticApprovalViolation(target, "semantics.physicalBehaviorsApproved", metadata.Id ?? target));
        }

        foreach (var behavior in metadata.Semantics.PhysicalBehaviorsApproved)
        {
            if (!HighImpactSemantics.Contains(behavior))
            {
                diagnostics.Add(AssetDiagnostic.SemanticApprovalViolation(target, "semantics.physicalBehaviorsApproved", behavior, $"Unknown or unsupported approved physical behavior: {behavior}"));
            }
        }

        foreach (var tile in metadata.Tiles)
        {
            if (tile.PhysicalBehaviorsApproved.Count == 0)
            {
                continue;
            }

            if (!approvedScopes.Contains(tile.Id ?? string.Empty))
            {
                diagnostics.Add(AssetDiagnostic.SemanticApprovalViolation(target, "tiles[].physicalBehaviorsApproved", tile.Id ?? string.Empty));
            }

            foreach (var behavior in tile.PhysicalBehaviorsApproved)
            {
                if (!HighImpactSemantics.Contains(behavior))
                {
                    diagnostics.Add(AssetDiagnostic.SemanticApprovalViolation(target, "tiles[].physicalBehaviorsApproved", behavior, $"Unknown or unsupported approved physical behavior: {behavior}"));
                }
            }
        }
    }

    private static void ValidateProvenance(
        AssetProvenance? provenance,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (provenance is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(provenance.SourceKind))
        {
            diagnostics.Add(AssetDiagnostic.InvalidProvenance(target, "provenance.sourceKind", "provenance.sourceKind must be present."));
        }

        if (string.IsNullOrWhiteSpace(provenance.CreatedBy))
        {
            diagnostics.Add(AssetDiagnostic.InvalidProvenance(target, "provenance.createdBy", "provenance.createdBy must be present."));
        }
    }

    private static void ValidateHumanReview(
        AssetHumanReview? humanReview,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (humanReview is null)
        {
            return;
        }

        if (!humanReview.RequiredForApprovedPhysicalBehaviors)
        {
            diagnostics.Add(AssetDiagnostic.SemanticApprovalViolation(
                target,
                "humanReview.requiredForApprovedPhysicalBehaviors",
                "Approved physical behaviors must remain review-gated."));
        }

        foreach (var approval in humanReview.Approvals)
        {
            if (string.IsNullOrWhiteSpace(approval.Id)
                || string.IsNullOrWhiteSpace(approval.ApprovedBy)
                || string.IsNullOrWhiteSpace(approval.Scope)
                || string.IsNullOrWhiteSpace(approval.ApprovedAt))
            {
                diagnostics.Add(AssetDiagnostic.SemanticApprovalViolation(
                    target,
                    "humanReview.approvals[]",
                    approval.Id ?? string.Empty,
                    "Human review approvals must include id, approvedBy, scope, and approvedAt."));
            }
        }
    }

    private static void RequireString(
        string? value,
        string target,
        string field,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(AssetDiagnostic.MissingRequiredField(target, field, $"Missing required field: {field}"));
        }
    }

    private static bool RequireStableTileId(
        string? value,
        string target,
        string field,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(AssetDiagnostic.MissingRequiredField(target, field, $"Missing required stable tile ID field: {field}"));
            return false;
        }

        if (!AssetIdPattern.IsMatch(value))
        {
            diagnostics.Add(AssetDiagnostic.InvalidStableId(target, field, value, "Tile ID must use lowercase dotted segments."));
            return false;
        }

        return true;
    }
}

public static class AssetMetadataLocator
{
    public static AssetTargetResolution ResolveTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Asset target must not be empty.")]);
        }

        if (StringComparer.Ordinal.Equals(target, AssetMetadataValidator.SmokeAssetId) || StringComparer.Ordinal.Equals(target, AssetMetadataValidator.RenderSmokeAssetId))
        {
            var knownPath = StringComparer.Ordinal.Equals(target, AssetMetadataValidator.RenderSmokeAssetId) ? AssetMetadataValidator.RenderSmokeAssetPath : AssetMetadataValidator.SmokeAssetPath;
            var path = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), knownPath);
            return File.Exists(path)
                ? AssetTargetResolution.Success(path)
                : AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, $"Asset metadata file was not found: {knownPath}")]);
        }

        if (!target.EndsWith(".asset.json", StringComparison.OrdinalIgnoreCase))
        {
            return AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Unsupported asset target. Expected asset.tile-atlas-smoke or a repository-relative .asset.json path.")]);
        }

        if (Path.IsPathRooted(target) || target.Split(['/', '\\']).Contains("..", StringComparer.Ordinal))
        {
            return AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Asset metadata path must be repository-relative and must not escape the repository.")]);
        }

        var resolvedPath = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), target);
        return File.Exists(resolvedPath)
            ? AssetTargetResolution.Success(resolvedPath)
            : AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, $"Asset metadata file was not found: {target}")]);
    }

    public static AssetTargetResolution ResolveById(string assetId)
    {
        if (StringComparer.Ordinal.Equals(assetId, AssetMetadataValidator.SmokeAssetId))
        {
            return ResolveTarget(assetId);
        }

        var assetMetadataRoot = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game", "assets", "metadata");
        if (!Directory.Exists(assetMetadataRoot))
        {
            return AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(assetId, "Asset metadata directory was not found: game/assets/metadata")]);
        }

        foreach (var candidate in Directory.EnumerateFiles(assetMetadataRoot, "*.asset.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal))
        {
            var validation = new AssetMetadataValidator().ValidateFile(candidate);
            if (StringComparer.Ordinal.Equals(validation.Id, assetId))
            {
                return AssetTargetResolution.Success(candidate);
            }
        }

        return AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(assetId, $"Asset metadata file was not found for ID: {assetId}")]);
    }
}

public sealed record AssetValidationItem(
    AssetMetadataSource? Metadata,
    string Path,
    string Id,
    string Status,
    IReadOnlyList<ContentValidationDiagnostic> Diagnostics)
{
    public static AssetValidationItem Failed(string path, string id, IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        return new AssetValidationItem(null, path, id, ContentValidationStatus.Failed, diagnostics);
    }

    public static AssetValidationItem Error(string path, string id, IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        return new AssetValidationItem(null, path, id, ContentValidationStatus.Error, diagnostics);
    }
}

public sealed record AssetMetadataSource
{
    [JsonPropertyName("schema")]
    public string? Schema { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("kind")]
    public string? Kind { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("purpose")]
    public string? Purpose { get; init; }

    [JsonPropertyName("source")]
    public AssetSourceReference? Source { get; init; }

    [JsonPropertyName("tileAtlas")]
    public TileAtlasDeclaration? TileAtlas { get; init; }

    [JsonPropertyName("tiles")]
    public IReadOnlyList<AssetTileSource> Tiles { get; init; } = [];

    [JsonPropertyName("provenance")]
    public AssetProvenance? Provenance { get; init; }

    [JsonPropertyName("semantics")]
    public AssetSemantics? Semantics { get; init; }

    [JsonPropertyName("humanReview")]
    public AssetHumanReview? HumanReview { get; init; }
}

public sealed record AssetSourceReference(
    [property: JsonPropertyName("path")] string? Path,
    [property: JsonPropertyName("mediaType")] string? MediaType);

public sealed record TileAtlasDeclaration(
    [property: JsonPropertyName("tileWidth")] int TileWidth,
    [property: JsonPropertyName("tileHeight")] int TileHeight,
    [property: JsonPropertyName("columns")] int Columns,
    [property: JsonPropertyName("rows")] int Rows);

public sealed record AssetTileSource
{
    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("x")]
    public int X { get; init; }

    [JsonPropertyName("y")]
    public int Y { get; init; }

    [JsonPropertyName("visualLabelsProposed")]
    public IReadOnlyList<string> VisualLabelsProposed { get; init; } = [];

    [JsonPropertyName("physicalBehaviorsApproved")]
    public IReadOnlyList<string> PhysicalBehaviorsApproved { get; init; } = [];
}

public sealed record AssetProvenance(
    [property: JsonPropertyName("sourceKind")] string? SourceKind,
    [property: JsonPropertyName("createdBy")] string? CreatedBy,
    [property: JsonPropertyName("notes")] string? Notes);

public sealed record AssetSemantics
{
    [JsonPropertyName("visualLabelsProposed")]
    public IReadOnlyList<string> VisualLabelsProposed { get; init; } = [];

    [JsonPropertyName("physicalBehaviorsApproved")]
    public IReadOnlyList<string> PhysicalBehaviorsApproved { get; init; } = [];
}

public sealed record AssetHumanReview
{
    [JsonPropertyName("requiredForApprovedPhysicalBehaviors")]
    public bool RequiredForApprovedPhysicalBehaviors { get; init; }

    [JsonPropertyName("approvals")]
    public IReadOnlyList<AssetHumanReviewApproval> Approvals { get; init; } = [];
}

public sealed record AssetHumanReviewApproval(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("approvedBy")] string? ApprovedBy,
    [property: JsonPropertyName("scope")] string? Scope,
    [property: JsonPropertyName("approvedAt")] string? ApprovedAt,
    [property: JsonPropertyName("reason")] string? Reason = null,
    [property: JsonPropertyName("decisionId")] string? DecisionId = null,
    [property: JsonPropertyName("sourceFingerprint")]
    string? SourceFingerprint = null);

public static class AssetDiagnostic
{
    public static ContentValidationDiagnostic MissingRequiredField(string target, string field, string message)
    {
        return new ContentValidationDiagnostic("ASSET0001", ContentDiagnosticSeverity.Error, message, target, field);
    }

    public static ContentValidationDiagnostic InvalidSourceReference(string target, string field, string message)
    {
        return new ContentValidationDiagnostic("ASSET0002", ContentDiagnosticSeverity.Error, message, target, field);
    }

    public static ContentValidationDiagnostic InvalidTileGrid(string target, string field, string message, string? itemId = null)
    {
        return new ContentValidationDiagnostic("ASSET0003", ContentDiagnosticSeverity.Error, message, target, field, itemId);
    }

    public static ContentValidationDiagnostic DuplicateTile(string target, string field, string itemId, string? message = null)
    {
        return new ContentValidationDiagnostic("ASSET0004", ContentDiagnosticSeverity.Error, message ?? $"Duplicate tile ID: {itemId}", target, field, itemId);
    }

    public static ContentValidationDiagnostic SemanticApprovalViolation(string target, string field, string itemId, string? message = null)
    {
        return new ContentValidationDiagnostic("ASSET0005", ContentDiagnosticSeverity.Error, message ?? "Approved physical/gameplay semantics require explicit human review evidence.", target, field, itemId);
    }

    public static ContentValidationDiagnostic InvalidProvenance(string target, string field, string message)
    {
        return new ContentValidationDiagnostic("ASSET0006", ContentDiagnosticSeverity.Error, message, target, field);
    }

    public static ContentValidationDiagnostic UnsupportedKind(string target, string field, string itemId)
    {
        return new ContentValidationDiagnostic("ASSET0007", ContentDiagnosticSeverity.Error, $"Unsupported asset kind: {itemId}", target, field, itemId);
    }

    public static ContentValidationDiagnostic UnsupportedMediaType(string target, string field, string itemId)
    {
        return new ContentValidationDiagnostic("ASSET0008", ContentDiagnosticSeverity.Error, $"Unsupported asset media type: {itemId}", target, field, itemId);
    }

    public static ContentValidationDiagnostic InvalidSchemaValue(string target, string field, string message)
    {
        return new ContentValidationDiagnostic("CONTENT0002", ContentDiagnosticSeverity.Error, message, target, field);
    }

    public static ContentValidationDiagnostic InvalidStableId(string target, string field, string itemId, string message)
    {
        return new ContentValidationDiagnostic("CONTENT0003", ContentDiagnosticSeverity.Error, message, target, field, itemId);
    }
}

internal static class AssetRequiredFields
{
    public static readonly string[] TopLevel =
    [
        "schema",
        "id",
        "kind",
        "title",
        "purpose",
        "source",
        "tileAtlas",
        "tiles",
        "provenance",
        "semantics",
        "humanReview",
    ];
}
