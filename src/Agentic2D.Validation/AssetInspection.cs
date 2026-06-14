using System.Buffers.Binary;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic2D.Validation;

public sealed class AssetInspector
{
    public AssetInspectionRun Inspect(string target)
    {
        var resolution = ResolveTarget(target);
        if (!resolution.IsSuccess)
        {
            return AssetInspectionRun.FromDiagnostics(target, ContentValidationStatus.Failed, 2, null, null, null, resolution.Diagnostics);
        }

        var validator = new AssetMetadataValidator();
        var validationItem = validator.ValidateFile(resolution.MetadataPath);
        var diagnostics = validationItem.Diagnostics.ToList();
        PngImageInfo? imageInfo = null;

        if (validationItem.Metadata?.Source?.Path is { Length: > 0 } sourcePath
            && validationItem.Metadata.Source.MediaType == "image/png"
            && !Path.IsPathRooted(sourcePath)
            && !sourcePath.Split(['/', '\\']).Contains("..", StringComparer.Ordinal))
        {
            var resolvedSourcePath = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), sourcePath);
            if (File.Exists(resolvedSourcePath))
            {
                var pngResult = PngHeaderReader.TryRead(resolvedSourcePath, validationItem.Id);
                if (pngResult.ImageInfo is not null)
                {
                    imageInfo = pngResult.ImageInfo;
                    AssetMetadataValidator.ValidateGridAgainstImage(validationItem.Metadata, imageInfo, validationItem.Id, diagnostics);
                }

                diagnostics.AddRange(pngResult.Diagnostics);
            }
        }

        var status = diagnostics.Any(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error)
            ? ContentValidationStatus.Failed
            : ContentValidationStatus.Passed;
        var exitCode = status == ContentValidationStatus.Passed ? 0 : 1;

        return AssetInspectionRun.FromDiagnostics(
            target,
            status,
            exitCode,
            validationItem.Metadata,
            validationItem.Path,
            imageInfo,
            diagnostics);
    }

    private static AssetTargetResolution ResolveTarget(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Asset inspection target must not be empty.")]);
        }

        if (StringComparer.Ordinal.Equals(target, AssetMetadataValidator.SmokeAssetId))
        {
            var path = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), AssetMetadataValidator.SmokeAssetPath);
            return File.Exists(path)
                ? AssetTargetResolution.Success(path)
                : AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, $"Asset metadata file was not found: {AssetMetadataValidator.SmokeAssetPath}")]);
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
}

public static class AssetInspectionArtifactWriter
{
    public static async Task<int> WriteAsync(string outputDirectory, AssetInspectionRun run)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "result.json"),
                JsonSerializer.Serialize(run.Result, ContentValidationJson.Options));

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "diagnostics.json"),
                JsonSerializer.Serialize(run.DiagnosticsDocument, ContentValidationJson.Options));

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "asset-summary.json"),
                JsonSerializer.Serialize(run.AssetSummary, ContentValidationJson.Options));

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "tiles.json"),
                JsonSerializer.Serialize(run.Tiles, ContentValidationJson.Options));

            return run.Result.ExitCode;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"failed to write asset inspection artifacts: {exception.Message}", exception);
        }
    }
}

public sealed record AssetInspectionRun(
    AssetInspectionResultDocument Result,
    AssetInspectionDiagnosticsDocument DiagnosticsDocument,
    AssetSummaryDocument AssetSummary,
    AssetTilesDocument Tiles)
{
    public static AssetInspectionRun FromDiagnostics(
        string target,
        string status,
        int exitCode,
        AssetMetadataSource? metadata,
        string? metadataPath,
        PngImageInfo? imageInfo,
        IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        var tiles = metadata?.Tiles ?? [];
        var visualLabelCount = (metadata?.Semantics?.VisualLabelsProposed.Count ?? 0)
            + tiles.Sum(static tile => tile.VisualLabelsProposed.Count);
        var physicalBehaviorCount = (metadata?.Semantics?.PhysicalBehaviorsApproved.Count ?? 0)
            + tiles.Sum(static tile => tile.PhysicalBehaviorsApproved.Count);
        var artifacts = new[]
        {
            new ContentArtifactReference("diagnostics.json", "diagnostics"),
            new ContentArtifactReference("asset-summary.json", "asset-summary"),
            new ContentArtifactReference("tiles.json", "tile-summary"),
        };

        var result = new AssetInspectionResultDocument(
            "agentic2d.asset-inspection.result.v1",
            "asset inspect",
            target,
            status,
            exitCode,
            new AssetInspectionSummary(
                metadata is null ? 0 : 1,
                tiles.Count,
                diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error),
                diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Warning),
                visualLabelCount,
                physicalBehaviorCount,
                physicalBehaviorCount),
            diagnostics,
            artifacts);

        var assetSummary = AssetSummaryDocument.From(metadata, metadataPath, imageInfo);
        var tileSummary = AssetTilesDocument.From(metadata);

        return new AssetInspectionRun(
            result,
            new AssetInspectionDiagnosticsDocument("agentic2d.asset-inspection.diagnostics.v1", diagnostics),
            assetSummary,
            tileSummary);
    }
}

public sealed record AssetInspectionResultDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("summary")] AssetInspectionSummary Summary,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ContentArtifactReference> Artifacts);

public sealed record AssetInspectionSummary(
    [property: JsonPropertyName("assetsInspected")] int AssetsInspected,
    [property: JsonPropertyName("tilesDeclared")] int TilesDeclared,
    [property: JsonPropertyName("errors")] int Errors,
    [property: JsonPropertyName("warnings")] int Warnings,
    [property: JsonPropertyName("visualLabelsProposed")] int VisualLabelsProposed,
    [property: JsonPropertyName("physicalBehaviorsApproved")] int PhysicalBehaviorsApproved,
    [property: JsonPropertyName("reviewGatedFields")] int ReviewGatedFields);

public sealed record AssetInspectionDiagnosticsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics);

public sealed record AssetSummaryDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("asset")] AssetSummaryAsset Asset,
    [property: JsonPropertyName("image")] AssetSummaryImage Image,
    [property: JsonPropertyName("tileAtlas")] AssetSummaryTileAtlas TileAtlas,
    [property: JsonPropertyName("semantics")] AssetSummarySemantics Semantics)
{
    public static AssetSummaryDocument From(AssetMetadataSource? metadata, string? metadataPath, PngImageInfo? imageInfo)
    {
        var tiles = metadata?.Tiles ?? [];
        var visualLabels = (metadata?.Semantics?.VisualLabelsProposed ?? [])
            .Concat(tiles.SelectMany(static tile => tile.VisualLabelsProposed))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var physicalBehaviors = (metadata?.Semantics?.PhysicalBehaviorsApproved ?? [])
            .Concat(tiles.SelectMany(static tile => tile.PhysicalBehaviorsApproved))
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        return new AssetSummaryDocument(
            "agentic2d.asset-inspection.asset-summary.v1",
            new AssetSummaryAsset(
                metadata?.Id ?? string.Empty,
                metadata?.Kind ?? string.Empty,
                metadata?.Title ?? string.Empty,
                metadataPath ?? string.Empty,
                metadata?.Source?.Path ?? string.Empty,
                metadata?.Source?.MediaType ?? string.Empty),
            new AssetSummaryImage(imageInfo?.Width ?? 0, imageInfo?.Height ?? 0),
            new AssetSummaryTileAtlas(
                metadata?.TileAtlas?.TileWidth ?? 0,
                metadata?.TileAtlas?.TileHeight ?? 0,
                metadata?.TileAtlas?.Columns ?? 0,
                metadata?.TileAtlas?.Rows ?? 0,
                tiles.Count),
            new AssetSummarySemantics(
                visualLabels,
                physicalBehaviors,
                metadata?.HumanReview?.RequiredForApprovedPhysicalBehaviors ?? true));
    }
}

public sealed record AssetSummaryAsset(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("metadataPath")] string MetadataPath,
    [property: JsonPropertyName("sourcePath")] string SourcePath,
    [property: JsonPropertyName("mediaType")] string MediaType);

public sealed record AssetSummaryImage(
    [property: JsonPropertyName("width")] int Width,
    [property: JsonPropertyName("height")] int Height);

public sealed record AssetSummaryTileAtlas(
    [property: JsonPropertyName("tileWidth")] int TileWidth,
    [property: JsonPropertyName("tileHeight")] int TileHeight,
    [property: JsonPropertyName("columns")] int Columns,
    [property: JsonPropertyName("rows")] int Rows,
    [property: JsonPropertyName("declaredTileCount")] int DeclaredTileCount);

public sealed record AssetSummarySemantics(
    [property: JsonPropertyName("visualLabelsProposed")] IReadOnlyList<string> VisualLabelsProposed,
    [property: JsonPropertyName("physicalBehaviorsApproved")] IReadOnlyList<string> PhysicalBehaviorsApproved,
    [property: JsonPropertyName("reviewRequiredForApprovedPhysicalBehaviors")] bool ReviewRequiredForApprovedPhysicalBehaviors);

public sealed record AssetTilesDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("assetId")] string AssetId,
    [property: JsonPropertyName("tiles")] IReadOnlyList<AssetTileSummary> Tiles)
{
    public static AssetTilesDocument From(AssetMetadataSource? metadata)
    {
        return new AssetTilesDocument(
            "agentic2d.asset-inspection.tiles.v1",
            metadata?.Id ?? string.Empty,
            (metadata?.Tiles ?? [])
                .OrderBy(static tile => tile.Y)
                .ThenBy(static tile => tile.X)
                .ThenBy(static tile => tile.Id, StringComparer.Ordinal)
                .Select(static tile => new AssetTileSummary(
                    tile.Id ?? string.Empty,
                    tile.X,
                    tile.Y,
                    tile.VisualLabelsProposed,
                    tile.PhysicalBehaviorsApproved,
                    tile.PhysicalBehaviorsApproved.Count == 0 ? "not-required-for-proposals" : "approved-with-human-review"))
                .ToArray());
    }
}

public sealed record AssetTileSummary(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("visualLabelsProposed")] IReadOnlyList<string> VisualLabelsProposed,
    [property: JsonPropertyName("physicalBehaviorsApproved")] IReadOnlyList<string> PhysicalBehaviorsApproved,
    [property: JsonPropertyName("reviewStatus")] string ReviewStatus);

public sealed record PngImageInfo(int Width, int Height);

public static class PngHeaderReader
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static PngReadResult TryRead(string path, string target)
    {
        try
        {
            Span<byte> header = stackalloc byte[24];
            using var stream = File.OpenRead(path);
            if (stream.Read(header) != header.Length)
            {
                return PngReadResult.Failure([AssetDiagnostic.InvalidSourceReference(target, "source.path", "PNG file is too small to contain a valid header.")]);
            }

            if (!header[..8].SequenceEqual(Signature)
                || header[12] != (byte)'I'
                || header[13] != (byte)'H'
                || header[14] != (byte)'D'
                || header[15] != (byte)'R')
            {
                return PngReadResult.Failure([AssetDiagnostic.InvalidSourceReference(target, "source.path", "Source file is not a valid PNG with an IHDR chunk.")]);
            }

            var width = BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
            var height = BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
            if (width <= 0 || height <= 0)
            {
                return PngReadResult.Failure([AssetDiagnostic.InvalidSourceReference(target, "source.path", "PNG width and height must be positive.")]);
            }

            return new PngReadResult(new PngImageInfo(width, height), []);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return PngReadResult.Failure([AssetDiagnostic.InvalidSourceReference(target, "source.path", $"Could not inspect PNG source: {exception.Message}")]);
        }
    }
}

public sealed record PngReadResult(PngImageInfo? ImageInfo, IReadOnlyList<ContentValidationDiagnostic> Diagnostics)
{
    public static PngReadResult Failure(IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        return new PngReadResult(null, diagnostics);
    }
}

public sealed class AssetTargetResolution
{
    private AssetTargetResolution(string metadataPath, IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        MetadataPath = metadataPath;
        Diagnostics = diagnostics;
    }

    public bool IsSuccess => Diagnostics.Count == 0;

    public string MetadataPath { get; }

    public IReadOnlyList<ContentValidationDiagnostic> Diagnostics { get; }

    public static AssetTargetResolution Success(string metadataPath)
    {
        return new AssetTargetResolution(metadataPath, []);
    }

    public static AssetTargetResolution Failure(IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        return new AssetTargetResolution(string.Empty, diagnostics);
    }
}
