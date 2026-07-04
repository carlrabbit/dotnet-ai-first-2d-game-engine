using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic2D.Validation;

public sealed class AssetPerceiver
{
    public AssetPerceptionRun Perceive(string target)
    {
        var resolution = AssetMetadataLocator.ResolveTarget(target);
        if (!resolution.IsSuccess)
        {
            var resolutionDiagnostics = resolution.Diagnostics
                .Select(static diagnostic => new ContentValidationDiagnostic("PERCEPTION0001", diagnostic.Severity, diagnostic.Message, diagnostic.Target, diagnostic.Field, diagnostic.ItemId))
                .ToArray();
            return AssetPerceptionRun.From(target, null, string.Empty, [], [], ContentValidationStatus.Failed, 1, resolutionDiagnostics);
        }

        var validation = new AssetMetadataValidator().ValidateFile(resolution.MetadataPath);
        var diagnostics = validation.Diagnostics.ToList();
        var metadata = validation.Metadata;
        if (metadata?.Kind != AssetMetadataValidator.TileAtlasKind || metadata.Source?.MediaType != "image/png")
        {
            diagnostics.Add(new ContentValidationDiagnostic("PERCEPTION0002", ContentDiagnosticSeverity.Error, "Asset perception supports tile-atlas PNG assets only.", validation.Id));
            return AssetPerceptionRun.From(target, metadata, validation.Path, [], [], ContentValidationStatus.Failed, 1, diagnostics);
        }

        if (string.IsNullOrWhiteSpace(metadata.Source.Path))
        {
            diagnostics.Add(new ContentValidationDiagnostic("PERCEPTION0001", ContentDiagnosticSeverity.Error, "Asset source path is missing.", validation.Id, "source.path"));
            return AssetPerceptionRun.From(target, metadata, validation.Path, [], [], ContentValidationStatus.Failed, 1, diagnostics);
        }

        var sourcePath = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), metadata.Source.Path);
        var decode = PngPixelDecoder.TryDecode(sourcePath, validation.Id);
        diagnostics.AddRange(decode.Diagnostics);

        var features = new List<AssetTileFeature>();
        if (decode.Image is not null && metadata.TileAtlas is not null)
        {
            foreach (var tile in metadata.Tiles.OrderBy(static item => item.Y).ThenBy(static item => item.X).ThenBy(static item => item.Id, StringComparer.Ordinal))
            {
                var tileResult = BuildTileFeature(metadata, tile, decode.Image, validation.Id);
                if (tileResult.Feature is null)
                {
                    diagnostics.AddRange(tileResult.Diagnostics);
                    continue;
                }

                diagnostics.AddRange(tileResult.Diagnostics);
                features.Add(tileResult.Feature);
            }

            AssignDuplicateGroups(features);
        }

        var proposals = features.SelectMany(BuildSemanticProposals).ToArray();
        var status = diagnostics.Any(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error)
            ? ContentValidationStatus.Failed
            : ContentValidationStatus.Passed;
        var exitCode = status == ContentValidationStatus.Passed ? 0 : 1;
        return AssetPerceptionRun.From(target, metadata, validation.Path, features, proposals, status, exitCode, diagnostics);
    }

    private static AssetTileFeatureResult BuildTileFeature(AssetMetadataSource metadata, AssetTileSource tile, DecodedPngImage image, string target)
    {
        var diagnostics = new List<ContentValidationDiagnostic>();
        var atlas = metadata.TileAtlas!;
        var startX = tile.X * atlas.TileWidth;
        var startY = tile.Y * atlas.TileHeight;
        if (startX < 0 || startY < 0 || startX + atlas.TileWidth > image.Width || startY + atlas.TileHeight > image.Height)
        {
            diagnostics.Add(new ContentValidationDiagnostic("PERCEPTION0004", ContentDiagnosticSeverity.Error, "Tile atlas coordinate extends outside the decoded PNG bounds.", target, "tiles", tile.Id));
            return new AssetTileFeatureResult(null, diagnostics);
        }

        var pixelBuffer = new byte[atlas.TileWidth * atlas.TileHeight * 4];
        var writeIndex = 0;
        var alphaPixels = 0;
        var occupiedLeft = atlas.TileWidth;
        var occupiedTop = atlas.TileHeight;
        var occupiedRight = -1;
        var occupiedBottom = -1;
        long sumR = 0;
        long sumG = 0;
        long sumB = 0;
        long sumA = 0;
        var colors = new Dictionary<uint, int>();

        for (var y = 0; y < atlas.TileHeight; y++)
        {
            for (var x = 0; x < atlas.TileWidth; x++)
            {
                var pixel = image.GetPixel(startX + x, startY + y);
                pixelBuffer[writeIndex++] = pixel.R;
                pixelBuffer[writeIndex++] = pixel.G;
                pixelBuffer[writeIndex++] = pixel.B;
                pixelBuffer[writeIndex++] = pixel.A;

                sumR += pixel.R;
                sumG += pixel.G;
                sumB += pixel.B;
                sumA += pixel.A;

                var packed = ((uint)pixel.R << 24) | ((uint)pixel.G << 16) | ((uint)pixel.B << 8) | pixel.A;
                colors.TryGetValue(packed, out var currentCount);
                colors[packed] = currentCount + 1;

                if (pixel.A > 0)
                {
                    alphaPixels++;
                    occupiedLeft = Math.Min(occupiedLeft, x);
                    occupiedTop = Math.Min(occupiedTop, y);
                    occupiedRight = Math.Max(occupiedRight, x);
                    occupiedBottom = Math.Max(occupiedBottom, y);
                }
            }
        }

        var totalPixels = atlas.TileWidth * atlas.TileHeight;
        var average = new PixelColor(
            (byte)(sumR / totalPixels),
            (byte)(sumG / totalPixels),
            (byte)(sumB / totalPixels),
            (byte)(sumA / totalPixels));
        var dominant = colors.OrderByDescending(static item => item.Value)
            .ThenBy(static item => item.Key)
            .Select(static item => PixelColor.FromPacked(item.Key))
            .First();
        var fingerprint = AssetFingerprint.FromBytes(pixelBuffer);
        var bounds = alphaPixels == 0
            ? null
            : new AssetOccupiedBounds(occupiedLeft, occupiedTop, occupiedRight, occupiedBottom);

        return new AssetTileFeatureResult(
            new AssetTileFeature(
                tile.Id ?? string.Empty,
                tile.X,
                tile.Y,
                atlas.TileWidth,
                atlas.TileHeight,
                Math.Round(alphaPixels / (double)totalPixels, 6, MidpointRounding.AwayFromZero),
                totalPixels - alphaPixels,
                bounds,
                average.ToHex(),
                dominant.ToHex(),
                fingerprint,
                string.Empty,
                0),
            diagnostics);
    }

    private static void AssignDuplicateGroups(IReadOnlyList<AssetTileFeature> features)
    {
        var groups = features.GroupBy(static feature => feature.FeatureFingerprint, StringComparer.Ordinal)
            .OrderBy(static group => group.Key, StringComparer.Ordinal)
            .ToArray();
        for (var index = 0; index < groups.Length; index++)
        {
            var groupId = $"duplicate-group.{index + 1:000}";
            foreach (var feature in groups[index])
            {
                feature.DuplicateGroupId = groupId;
                feature.DuplicateGroupSize = groups[index].Count();
            }
        }
    }

    private static IReadOnlyList<AssetSemanticProposal> BuildSemanticProposals(AssetTileFeature feature)
    {
        var average = PixelColor.ParseHex(feature.RepresentativeAverageColor);
        var vocabulary = new[]
        {
            new SemanticPrototype("grass", new PixelColor(44, 160, 44, 255)),
            new SemanticPrototype("stone", new PixelColor(128, 128, 128, 255)),
            new SemanticPrototype("water", new PixelColor(30, 90, 220, 255)),
            new SemanticPrototype("flower", new PixelColor(230, 120, 170, 255)),
        };

        var best = vocabulary
            .Select(candidate => new
            {
                candidate.Label,
                Distance = Math.Sqrt(Math.Pow(average.R - candidate.Color.R, 2) + Math.Pow(average.G - candidate.Color.G, 2) + Math.Pow(average.B - candidate.Color.B, 2)),
            })
            .OrderBy(static candidate => candidate.Distance)
            .ThenBy(static candidate => candidate.Label, StringComparer.Ordinal)
            .First();

        var normalizedScore = Math.Round(Math.Max(0d, 1d - (best.Distance / 441.6729559300637d)), 4, MidpointRounding.AwayFromZero);
        if (normalizedScore < 0.55d || feature.AlphaCoverage <= 0d)
        {
            return [];
        }

        return
        [
            new AssetSemanticProposal(
                $"proposal.{feature.Id}.visual-label",
                feature.Id,
                "visual-label",
                best.Label,
                "proposed",
                "deterministic-color-heuristic",
                normalizedScore,
                [new ContentArtifactReference($"tile-features.json#{feature.Id}", "tile-feature")])
        ];
    }
}

public static class AssetPerceptionArtifactWriter
{
    public static async Task<int> WriteAsync(string outputDirectory, AssetPerceptionRun run)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(run.Result, ContentValidationJson.Options));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "diagnostics.json"), JsonSerializer.Serialize(run.DiagnosticsDocument, ContentValidationJson.Options));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "tile-features.json"), JsonSerializer.Serialize(run.TileFeatures, ContentValidationJson.Options));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "semantic-proposals.json"), JsonSerializer.Serialize(run.SemanticProposals, ContentValidationJson.Options));
            return run.Result.ExitCode;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"failed to write asset perception artifacts: {exception.Message}", exception);
        }
    }
}

public sealed record AssetPerceptionRun(
    AssetPerceptionResultDocument Result,
    ContentDiagnosticsDocument DiagnosticsDocument,
    AssetTileFeaturesDocument TileFeatures,
    AssetSemanticProposalsDocument SemanticProposals)
{
    public static AssetPerceptionRun From(
        string target,
        AssetMetadataSource? metadata,
        string metadataPath,
        IReadOnlyList<AssetTileFeature> features,
        IReadOnlyList<AssetSemanticProposal> proposals,
        string status,
        int exitCode,
        IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        var artifacts = new[]
        {
            new ContentArtifactReference("diagnostics.json", "diagnostics"),
            new ContentArtifactReference("tile-features.json", "tile-features"),
            new ContentArtifactReference("semantic-proposals.json", "semantic-proposals"),
        };

        return new AssetPerceptionRun(
            new AssetPerceptionResultDocument(
                "agentic2d.asset-perception.result.v1",
                "asset perceive",
                target,
                metadata?.Id ?? string.Empty,
                status,
                exitCode,
                new AssetPerceptionSummary(
                    features.Count,
                    features.Select(static feature => feature.DuplicateGroupId).Distinct(StringComparer.Ordinal).Count(),
                    proposals.Count,
                    diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error),
                    diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Warning)),
                diagnostics,
                artifacts),
            new ContentDiagnosticsDocument("agentic2d.asset-perception.diagnostics.v1", diagnostics),
            new AssetTileFeaturesDocument("agentic2d.asset-perception.tile-features.v1", metadata?.Id ?? string.Empty, metadataPath, metadata?.Source?.Path ?? string.Empty, features),
            new AssetSemanticProposalsDocument("agentic2d.asset-perception.semantic-proposals.v1", metadata?.Id ?? string.Empty, proposals));
    }
}

public sealed record AssetPerceptionResultDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("assetId")] string AssetId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("summary")] AssetPerceptionSummary Summary,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ContentArtifactReference> Artifacts);

public sealed record AssetPerceptionSummary(
    [property: JsonPropertyName("tilesObserved")] int TilesObserved,
    [property: JsonPropertyName("duplicateGroups")] int DuplicateGroups,
    [property: JsonPropertyName("proposals")] int Proposals,
    [property: JsonPropertyName("errors")] int Errors,
    [property: JsonPropertyName("warnings")] int Warnings);

public sealed record AssetTileFeaturesDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("assetId")] string AssetId,
    [property: JsonPropertyName("metadataPath")] string MetadataPath,
    [property: JsonPropertyName("sourcePath")] string SourcePath,
    [property: JsonPropertyName("tiles")] IReadOnlyList<AssetTileFeature> Tiles);

public sealed record AssetSemanticProposalsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("assetId")] string AssetId,
    [property: JsonPropertyName("proposals")] IReadOnlyList<AssetSemanticProposal> Proposals);

public sealed class AssetTileFeature
{
    public AssetTileFeature(
        string id,
        int x,
        int y,
        int width,
        int height,
        double alphaCoverage,
        int transparentPixels,
        AssetOccupiedBounds? occupiedBounds,
        string representativeAverageColor,
        string representativeDominantColor,
        string featureFingerprint,
        string duplicateGroupId,
        int duplicateGroupSize)
    {
        Id = id;
        X = x;
        Y = y;
        Width = width;
        Height = height;
        AlphaCoverage = alphaCoverage;
        TransparentPixels = transparentPixels;
        OccupiedBounds = occupiedBounds;
        RepresentativeAverageColor = representativeAverageColor;
        RepresentativeDominantColor = representativeDominantColor;
        FeatureFingerprint = featureFingerprint;
        DuplicateGroupId = duplicateGroupId;
        DuplicateGroupSize = duplicateGroupSize;
    }

    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("x")]
    public int X { get; }

    [JsonPropertyName("y")]
    public int Y { get; }

    [JsonPropertyName("width")]
    public int Width { get; }

    [JsonPropertyName("height")]
    public int Height { get; }

    [JsonPropertyName("alphaCoverage")]
    public double AlphaCoverage { get; }

    [JsonPropertyName("transparentPixels")]
    public int TransparentPixels { get; }

    [JsonPropertyName("occupiedBounds")]
    public AssetOccupiedBounds? OccupiedBounds { get; }

    [JsonPropertyName("representativeAverageColor")]
    public string RepresentativeAverageColor { get; }

    [JsonPropertyName("representativeDominantColor")]
    public string RepresentativeDominantColor { get; }

    [JsonPropertyName("featureFingerprint")]
    public string FeatureFingerprint { get; }

    [JsonPropertyName("duplicateGroupId")]
    public string DuplicateGroupId { get; set; }

    [JsonPropertyName("duplicateGroupSize")]
    public int DuplicateGroupSize { get; set; }
}

public sealed record AssetOccupiedBounds(
    [property: JsonPropertyName("left")] int Left,
    [property: JsonPropertyName("top")] int Top,
    [property: JsonPropertyName("right")] int Right,
    [property: JsonPropertyName("bottom")] int Bottom);

public sealed record AssetSemanticProposal(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("tileId")] string TileId,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("origin")] string Origin,
    [property: JsonPropertyName("score")] double Score,
    [property: JsonPropertyName("evidenceReferences")]
    IReadOnlyList<ContentArtifactReference> EvidenceReferences);

public sealed record AssetTileFeatureResult(AssetTileFeature? Feature, IReadOnlyList<ContentValidationDiagnostic> Diagnostics);

public sealed record DecodedPngImage(int Width, int Height, byte[] Pixels)
{
    public PixelColor GetPixel(int x, int y)
    {
        var offset = ((y * Width) + x) * 4;
        return new PixelColor(Pixels[offset], Pixels[offset + 1], Pixels[offset + 2], Pixels[offset + 3]);
    }
}

public sealed record PngDecodeResult(DecodedPngImage? Image, IReadOnlyList<ContentValidationDiagnostic> Diagnostics)
{
    public static PngDecodeResult Failure(IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        return new PngDecodeResult(null, diagnostics);
    }
}

public static class PngPixelDecoder
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static PngDecodeResult TryDecode(string path, string target)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var reader = new BinaryReader(stream);
            var signature = reader.ReadBytes(8);
            if (!signature.SequenceEqual(Signature))
            {
                return PngDecodeResult.Failure([new ContentValidationDiagnostic("PERCEPTION0003", ContentDiagnosticSeverity.Error, "Source file is not a valid PNG.", target, "source.path")]);
            }

            var width = 0;
            var height = 0;
            var bitDepth = 0;
            var colorType = 0;
            var interlaceMethod = 0;
            using var compressed = new MemoryStream();

            while (stream.Position < stream.Length)
            {
                var length = BinaryPrimitives.ReadInt32BigEndian(reader.ReadBytesRequired(4));
                var chunkType = reader.ReadBytesRequired(4);
                var data = reader.ReadBytesRequired(length);
                _ = reader.ReadBytesRequired(4); // CRC
                var type = System.Text.Encoding.ASCII.GetString(chunkType);

                switch (type)
                {
                    case "IHDR":
                        width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
                        height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(4, 4));
                        bitDepth = data[8];
                        colorType = data[9];
                        interlaceMethod = data[12];
                        break;
                    case "IDAT":
                        compressed.Write(data, 0, data.Length);
                        break;
                    case "IEND":
                        goto Decode;
                }
            }

        Decode:
            if (width <= 0 || height <= 0 || bitDepth != 8 || colorType != 6 || interlaceMethod != 0)
            {
                return PngDecodeResult.Failure([new ContentValidationDiagnostic("PERCEPTION0003", ContentDiagnosticSeverity.Error, "PNG must be 8-bit RGBA and non-interlaced for deterministic perception.", target, "source.path")]);
            }

            compressed.Position = 0;
            using var inflated = new MemoryStream();
            using (var zlib = new ZLibStream(compressed, CompressionMode.Decompress, leaveOpen: true))
            {
                zlib.CopyTo(inflated);
            }

            var raw = inflated.ToArray();
            var bytesPerPixel = 4;
            var stride = width * bytesPerPixel;
            var expectedLength = (stride + 1) * height;
            if (raw.Length != expectedLength)
            {
                return PngDecodeResult.Failure([new ContentValidationDiagnostic("PERCEPTION0003", ContentDiagnosticSeverity.Error, "Decoded PNG scanline length does not match image dimensions.", target, "source.path")]);
            }

            var pixels = new byte[width * height * bytesPerPixel];
            var previous = new byte[stride];
            var current = new byte[stride];
            var rawOffset = 0;

            for (var y = 0; y < height; y++)
            {
                var filter = raw[rawOffset++];
                Array.Copy(raw, rawOffset, current, 0, stride);
                rawOffset += stride;

                ApplyFilter(filter, current, previous, bytesPerPixel);
                Buffer.BlockCopy(current, 0, pixels, y * stride, stride);
                Array.Copy(current, previous, stride);
            }

            return new PngDecodeResult(new DecodedPngImage(width, height, pixels), []);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException or EndOfStreamException)
        {
            return PngDecodeResult.Failure([new ContentValidationDiagnostic("PERCEPTION0003", ContentDiagnosticSeverity.Error, $"Could not decode PNG for deterministic perception: {exception.Message}", target, "source.path")]);
        }
    }

    private static void ApplyFilter(byte filter, byte[] current, byte[] previous, int bytesPerPixel)
    {
        switch (filter)
        {
            case 0:
                return;
            case 1:
                for (var index = 0; index < current.Length; index++)
                {
                    var left = index >= bytesPerPixel ? current[index - bytesPerPixel] : (byte)0;
                    current[index] = unchecked((byte)(current[index] + left));
                }

                return;
            case 2:
                for (var index = 0; index < current.Length; index++)
                {
                    current[index] = unchecked((byte)(current[index] + previous[index]));
                }

                return;
            case 3:
                for (var index = 0; index < current.Length; index++)
                {
                    var left = index >= bytesPerPixel ? current[index - bytesPerPixel] : (byte)0;
                    var up = previous[index];
                    current[index] = unchecked((byte)(current[index] + ((left + up) / 2)));
                }

                return;
            case 4:
                for (var index = 0; index < current.Length; index++)
                {
                    var left = index >= bytesPerPixel ? current[index - bytesPerPixel] : (byte)0;
                    var up = previous[index];
                    var upLeft = index >= bytesPerPixel ? previous[index - bytesPerPixel] : (byte)0;
                    current[index] = unchecked((byte)(current[index] + PaethPredictor(left, up, upLeft)));
                }

                return;
            default:
                throw new InvalidDataException($"Unsupported PNG filter type: {filter}");
        }
    }

    private static byte PaethPredictor(byte left, byte up, byte upLeft)
    {
        var p = left + up - upLeft;
        var pa = Math.Abs(p - left);
        var pb = Math.Abs(p - up);
        var pc = Math.Abs(p - upLeft);
        return pa <= pb && pa <= pc ? left : pb <= pc ? up : upLeft;
    }

    private static byte[] ReadBytesRequired(this BinaryReader reader, int count)
    {
        var bytes = reader.ReadBytes(count);
        if (bytes.Length != count)
        {
            throw new EndOfStreamException("Unexpected end of PNG stream.");
        }

        return bytes;
    }
}

public sealed record PixelColor(byte R, byte G, byte B, byte A)
{
    public string ToHex()
    {
        return $"#{R:x2}{G:x2}{B:x2}{A:x2}";
    }

    public static PixelColor FromPacked(uint packed)
    {
        return new PixelColor((byte)(packed >> 24), (byte)(packed >> 16), (byte)(packed >> 8), (byte)packed);
    }

    public static PixelColor ParseHex(string hex)
    {
        return new PixelColor(
            Convert.ToByte(hex[1..3], 16),
            Convert.ToByte(hex[3..5], 16),
            Convert.ToByte(hex[5..7], 16),
            Convert.ToByte(hex[7..9], 16));
    }
}

public sealed record SemanticPrototype(string Label, PixelColor Color);

public static class AssetFingerprint
{
    public static string FromBytes(byte[] bytes)
    {
        return $"sha256:{Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant()}";
    }
}
