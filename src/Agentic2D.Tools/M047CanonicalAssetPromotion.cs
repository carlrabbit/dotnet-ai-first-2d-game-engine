using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Buffers.Binary;
using System.IO.Compression;

namespace Agentic2D.Tools;

/// <summary>Canonical, path-independent asset candidate and generation primitives for M047.</summary>
public static class M047CanonicalAssetPromotion
{
    public const string CandidateSchema = "agentic2d.canonical-asset-candidate.v2";
    public const string DecisionSchema = "agentic2d.asset-review-decision.v2";
    public const string RecipeSchema = "agentic2d.asset-processing-recipe.v2";
    public const string ManifestSchema = "agentic2d.asset-promotion-manifest.v2";

    public sealed record Selection(string Type, int X = 0, int Y = 0, int Width = 0, int Height = 0, int StartFrame = 0, int EndFrame = 0, int StartSampleFrame = 0, int EndSampleFrame = 0)
    {
        public string Canonical() => $"{Type}|{X}|{Y}|{Width}|{Height}|{StartFrame}|{EndFrame}|{StartSampleFrame}|{EndSampleFrame}";
    }

    public sealed record Variant(string Id, string Kind, string? SourceRelativePath, Selection Selection, string Fingerprint);
    public sealed record Candidate(string CampaignId, string CandidateId, string SourceId, string SourceRelativePath, string SourceFingerprint, string MediaKind, Selection Selection, string PresentationRole, string ProposalFingerprint, IReadOnlyList<Variant> Variants, string Fingerprint)
    {
        public static Candidate Create(string campaignId, string candidateId, string sourceId, string relativePath, byte[] bytes, string mediaKind, Selection selection, string role, string proposal, IReadOnlyList<Variant>? variants = null)
        {
            var source = Hash(bytes); var chosen = variants ?? []; var semantic = string.Join("\n", CandidateSchema, campaignId, candidateId, sourceId, Normalize(relativePath), source, mediaKind, selection.Canonical(), role, proposal, string.Join(";", chosen.OrderBy(x => x.Id, StringComparer.Ordinal).Select(x => x.Id + "=" + x.Fingerprint)));
            return new(CampaignId: campaignId, CandidateId: candidateId, SourceId: sourceId, SourceRelativePath: Normalize(relativePath), SourceFingerprint: source, MediaKind: mediaKind, Selection: selection, PresentationRole: role, ProposalFingerprint: proposal, Variants: chosen, Fingerprint: Hash(Encoding.UTF8.GetBytes(semantic)));
        }
    }

    public sealed record Correction(string Type, JsonElement Parameters);
    public sealed record Decision(string Id, string CampaignId, string CandidateId, string CandidateFingerprint, string? SelectedVariantId, string? SelectedVariantFingerprint, IReadOnlyList<Correction> Corrections, string ConsequenceResponse, int Sequence, string? Supersedes)
    {
        public bool IsApproval => ConsequenceResponse is "confirm" or "presentation-only" && Corrections.All(x => SupportedCorrections.Contains(x.Type));
    }

    public sealed record Recipe(string Id, string CandidateFingerprint, string? SelectedVariantId, IReadOnlyList<Correction> Operations, IReadOnlyList<string> InputFingerprints, string ExpectedOutputFingerprint, string Fingerprint);
    public sealed record Provenance(string ApprovedId, string CandidateId, string CandidateFingerprint, string SourceId, string SourceRelativePath, string SourceFingerprint, string? SelectedVariantId, string? SelectedVariantFingerprint, string DecisionId, string RecipeId, string InputHash, string OutputHash);

    public static readonly IReadOnlySet<string> SupportedCorrections = new HashSet<string>(StringComparer.Ordinal)
    {
        "copy-source", "crop-image-region", "preserve-padding", "trim-transparent-padding-to-alpha-bounds", "scale-image-nearest-integer", "set-pivot-or-anchor-metadata", "order-animation-frames", "audio-copy", "audio-trim-sample-frames"
    };

    public static string StableApprovedId(string campaignId, string candidateId, string kind, string role) => "approved-asset." + Hash(Encoding.UTF8.GetBytes(string.Join("\n", campaignId, candidateId, kind, role)));
    public static string RecipeFingerprint(string candidateFingerprint, string? variant, IReadOnlyList<Correction> operations, IReadOnlyList<string> inputs) => Hash(Encoding.UTF8.GetBytes(string.Join("\n", RecipeSchema, candidateFingerprint, variant ?? "", string.Join(";", operations.Select(CanonicalCorrection)), string.Join(";", inputs.Order(StringComparer.Ordinal)))));
    public static string CanonicalCorrection(Correction correction) => correction.Type + ":" + CanonicalJson(correction.Parameters);
    public static string CanonicalJson(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject().OrderBy(x => x.Name, StringComparer.Ordinal).Select(x => JsonSerializer.Serialize(x.Name) + ":" + CanonicalJson(x.Value))) + "}",
        JsonValueKind.Array => "[" + string.Join(",", value.EnumerateArray().Select(CanonicalJson)) + "]",
        _ => value.GetRawText()
    };
    public static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    public static string Normalize(string path) => path.Replace('\\', '/').TrimStart('/');

    public static Candidate Resolve(JsonElement campaign, string candidateId, string assetHome)
    {
        if (!campaign.TryGetProperty("candidates", out var candidates) || candidates.ValueKind != JsonValueKind.Array) throw new InvalidDataException("campaign has no structured candidates");
        var item = candidates.EnumerateArray().SingleOrDefault(x => x.ValueKind == JsonValueKind.Object && x.TryGetProperty("candidateId", out var id) && id.GetString() == candidateId);
        if (item.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException("candidate is not a structured promotion subject: " + candidateId);
        var sourceRelative = item.GetProperty("sourceRelativePath").GetString() ?? throw new InvalidDataException("candidate source path missing");
        if (!IsSafeRelative(sourceRelative)) throw new InvalidDataException("candidate source path must remain under the declared asset home");
        var full = Path.Combine(assetHome, sourceRelative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(full)) throw new FileNotFoundException("candidate source is unavailable", full);
        var bytes = File.ReadAllBytes(full); var selection = ParseSelection(item.GetProperty("selection"));
        var variants = item.TryGetProperty("variants", out var variantArray) && variantArray.ValueKind == JsonValueKind.Array
            ? variantArray.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object).Select(x =>
            {
                var variantPath = x.TryGetProperty("sourceRelativePath", out var path) ? path.GetString() : null;
                var variantSelection = x.TryGetProperty("selection", out var selected) ? ParseSelection(selected) : selection;
                var declaredFingerprint = x.GetProperty("variantFingerprint").GetString()!;
                if (variantPath is not null && !IsSafeRelative(variantPath)) throw new InvalidDataException("variant source path must remain under the declared asset home");
                var resolvedFingerprint = variantPath is null ? declaredFingerprint : Hash(Encoding.UTF8.GetBytes(string.Join("\n", declaredFingerprint, Normalize(variantPath), Hash(File.ReadAllBytes(Path.Combine(assetHome, variantPath.Replace('/', Path.DirectorySeparatorChar)))), variantSelection.Canonical())));
                return new Variant(x.GetProperty("variantId").GetString()!, x.GetProperty("kind").GetString() ?? "delta", variantPath, variantSelection, resolvedFingerprint);
            }).ToArray()
            : [];
        return Candidate.Create(campaign.GetProperty("id").GetString()!, candidateId, campaign.GetProperty("sourceId").GetString()!, sourceRelative, bytes, item.GetProperty("mediaKind").GetString()!, selection, item.GetProperty("presentationRole").GetString() ?? "default", item.GetProperty("proposalFingerprint").GetString() ?? "proposal.unknown", variants);
    }

    public static bool ValidateRecipe(Recipe recipe) => recipe.Operations.All(x => SupportedCorrections.Contains(x.Type)) && recipe.Fingerprint == RecipeFingerprint(recipe.CandidateFingerprint, recipe.SelectedVariantId, recipe.Operations, recipe.InputFingerprints);
    public static byte[] Materialize(byte[] source, string mediaKind, Selection selection, IReadOnlyList<Correction> operations)
    {
        var current = source;
        foreach (var operation in operations)
        {
            switch (operation.Type)
            {
                case "copy-source":
                case "preserve-padding":
                case "set-pivot-or-anchor-metadata":
                case "order-animation-frames":
                    break;
                case "crop-image-region": current = CropPng(current, operation.Parameters.TryGetProperty("width", out _) ? ParseSelection(operation.Parameters) : selection); break;
                case "trim-transparent-padding-to-alpha-bounds": current = TrimPng(current); break;
                case "scale-image-nearest-integer":
                    var factor = operation.Parameters.TryGetProperty("factor", out var f) && f.TryGetInt32(out var n) ? n : 1;
                    if (factor < 1 || factor > 16) throw new InvalidDataException("scale factor must be between 1 and 16");
                    current = ScalePng(current, factor); break;
                case "audio-copy": break;
                case "audio-trim-sample-frames": current = TrimWav(current, selection); break;
                default: throw new InvalidDataException("unsupported processing operation: " + operation.Type);
            }
        }
        return current;
    }
    public static bool ValidatePublishedGeneration(string root) => ValidatePublishedGeneration(root, null);
    public static bool ValidatePublishedGeneration(string root, string? sourceRegistryPath)
    {
        try
        {
            var manifestPath = Path.Combine(root, "promotion-manifest.json");
            var currentPath = Path.Combine(root, "current-generation.json");
            if (!File.Exists(manifestPath) || !File.Exists(currentPath)) return false;
            using var manifest = JsonDocument.Parse(File.ReadAllText(manifestPath));
            using var current = JsonDocument.Parse(File.ReadAllText(currentPath));
            if (manifest.RootElement.GetProperty("schema").GetString() != ManifestSchema || current.RootElement.GetProperty("manifest").GetString() != "promotion-manifest.json") return Fail(root, "schema");
            var manifestHash = Hash(Encoding.UTF8.GetBytes(File.ReadAllText(manifestPath)));
            if (current.RootElement.GetProperty("generation").GetString() != manifestHash) return Fail(root, "generation");
            var entries = manifest.RootElement.GetProperty("entries");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var definitionsPath = Path.Combine(root, "approved-definitions.json");
            if (!File.Exists(definitionsPath)) return Fail(root, "definitions-missing");
            using var definitions = JsonDocument.Parse(File.ReadAllText(definitionsPath));
            if (definitions.RootElement.ValueKind != JsonValueKind.Array || HasForbiddenOperationalData(definitions.RootElement)) return Fail(root, "definitions-operational-data");
            var definitionById = definitions.RootElement.EnumerateArray().Where(x => x.ValueKind == JsonValueKind.Object && x.TryGetProperty("id", out _)).ToDictionary(x => x.GetProperty("id").GetString()!, StringComparer.Ordinal);
            if (definitionById.Count != definitions.RootElement.GetArrayLength()) return Fail(root, "definitions-duplicate-id");
            foreach (var entry in entries.EnumerateArray())
            {
                if (!ids.Add(entry.GetProperty("id").GetString() ?? "") || !IsSafeRelative(entry.GetProperty("derivative").GetString() ?? "")) return Fail(root, "identity-or-path");
                var derivative = Path.Combine(root, entry.GetProperty("derivative").GetString()!.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(derivative)) return Fail(root, "derivative-missing");
                var outputHash = "sha256:" + Hash(File.ReadAllBytes(derivative));
                if (!outputHash.Equals(entry.GetProperty("outputFingerprint").GetString(), StringComparison.Ordinal)) return Fail(root, "output-hash");
                var id = entry.GetProperty("id").GetString()!;
                var entryInputFingerprint = entry.GetProperty("inputFingerprint").GetString();
                if (entryInputFingerprint is null || !entryInputFingerprint.StartsWith("sha256:", StringComparison.Ordinal) || entryInputFingerprint.Length == "sha256:".Length) return Fail(root, "input-fingerprint");
                if (!definitionById.TryGetValue(id, out var definition) || definition.GetProperty("derivative").GetString() != entry.GetProperty("derivative").GetString() || definition.GetProperty("inputFingerprint").GetString() != entryInputFingerprint || definition.GetProperty("outputFingerprint").GetString() != entry.GetProperty("outputFingerprint").GetString() || definition.GetProperty("candidateFingerprint").GetString() != entry.GetProperty("candidateFingerprint").GetString()) return Fail(root, "definition-manifest-mismatch");
                var recipe = entry.GetProperty("recipe");
                if (!recipe.GetProperty("schema").GetString()!.Equals(RecipeSchema, StringComparison.Ordinal) || !recipe.GetProperty("candidateFingerprint").GetString()!.Equals(entry.GetProperty("candidateFingerprint").GetString(), StringComparison.Ordinal)) return Fail(root, "recipe-subject");
                var operations = recipe.GetProperty("operations").EnumerateArray().Select(x => new Correction(x.GetProperty("type").GetString()!, x.GetProperty("parameters"))).ToArray();
                var inputs = recipe.GetProperty("inputFingerprints").EnumerateArray().Select(x => x.GetString()!).ToArray();
                if (inputs.Length != 1 || !("sha256:" + inputs[0]).Equals(entryInputFingerprint, StringComparison.Ordinal)) return Fail(root, "recipe-input-mismatch");
                var selectedVariant = recipe.TryGetProperty("selectedVariant", out var variant) ? variant.GetString() : null;
                if (recipe.GetProperty("fingerprint").GetString() != RecipeFingerprint(entry.GetProperty("candidateFingerprint").GetString()!, selectedVariant, operations, inputs)) return Fail(root, "recipe-fingerprint");
                if (recipe.GetProperty("expectedOutputFingerprint").GetString() != outputHash["sha256:".Length..]) return Fail(root, "recipe-output");
                var provenance = entry.GetProperty("provenance");
                if (!provenance.GetProperty("inputHash").GetString()!.Equals(entryInputFingerprint, StringComparison.Ordinal) || !provenance.GetProperty("sourceFingerprint").GetString()!.Equals(entryInputFingerprint, StringComparison.Ordinal) || !provenance.GetProperty("outputHash").GetString()!.Equals(outputHash, StringComparison.Ordinal) || provenance.GetProperty("candidateId").GetString() is null) return Fail(root, "provenance");
                if (sourceRegistryPath is not null)
                {
                    using var registry = JsonDocument.Parse(File.ReadAllText(sourceRegistryPath));
                    var sourceId = provenance.GetProperty("sourceId").GetString(); var relative = provenance.GetProperty("sourceRelativePath").GetString();
                    var source = registry.RootElement.GetProperty("sources").EnumerateArray().FirstOrDefault(x => x.GetProperty("id").GetString() == sourceId);
                    var sourceRoot = source.ValueKind == JsonValueKind.Object ? source.GetProperty("path").GetString() : null;
                    if (sourceRoot is null || relative is null || !IsSafeRelative(relative)) return Fail(root, "source-reference");
                    var sourcePath = Path.GetFullPath(Path.Combine(sourceRoot, relative.Replace('/', Path.DirectorySeparatorChar)));
                    if (!sourcePath.StartsWith(Path.GetFullPath(sourceRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || !File.Exists(sourcePath) || !("sha256:" + Hash(File.ReadAllBytes(sourcePath))).Equals(provenance.GetProperty("sourceFingerprint").GetString(), StringComparison.Ordinal)) return Fail(root, "source-hash");
                }
            }
            if (!ids.SetEquals(definitionById.Keys)) return Fail(root, "definitions-id-set-mismatch");
            return true;
        }
        catch (Exception exception) { return Fail(root, exception.GetType().Name + ":" + exception.Message); }
    }
    private static bool Fail(string root, string reason) { if (string.Equals(Environment.GetEnvironmentVariable("AGENTIC2D_M047_DEBUG_VALIDATION"), "1", StringComparison.Ordinal)) File.WriteAllText(Path.Combine(root, "validation-error.txt"), reason); return false; }
    private static bool HasForbiddenOperationalData(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => value.EnumerateObject().Any(property => IsForbiddenProperty(property) || HasForbiddenOperationalData(property.Value)),
        JsonValueKind.Array => value.EnumerateArray().Any(HasForbiddenOperationalData),
        _ => false
    };
    private static bool IsForbiddenProperty(JsonProperty property) => property.Name is "absoluteAssetHomePath" or "assetHome" or "sessionId" or "processId" or "inputState" or "previewOperationalState" or "aliases" || (property.Name.Contains("path", StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String && Path.IsPathRooted(property.Value.GetString() ?? ""));
    public static bool IsSafeRelative(string path) => !Path.IsPathRooted(path) && !path.Split('/', '\\').Any(x => x is ".." or "");
    private static Selection ParseSelection(JsonElement e) => new(e.GetProperty("type").GetString()!, GetInt(e, "x"), GetInt(e, "y"), GetInt(e, "width"), GetInt(e, "height"), GetInt(e, "startFrame"), GetInt(e, "endFrame"), GetInt(e, "startSampleFrame"), GetInt(e, "endSampleFrame"));
    private static int GetInt(JsonElement e, string name) => e.TryGetProperty(name, out var p) && p.TryGetInt32(out var n) ? n : 0;

    private static byte[] CropPng(byte[] bytes, Selection selection) { var image = DecodePng(bytes); var x = Math.Clamp(selection.X, 0, image.Width); var y = Math.Clamp(selection.Y, 0, image.Height); var w = selection.Width <= 0 ? image.Width - x : Math.Min(selection.Width, image.Width - x); var h = selection.Height <= 0 ? image.Height - y : Math.Min(selection.Height, image.Height - y); if (w <= 0 || h <= 0) throw new InvalidDataException("image crop is empty"); var pixels = new byte[w * h * 4]; for (var row = 0; row < h; row++) Buffer.BlockCopy(image.Pixels, ((y + row) * image.Width + x) * 4, pixels, row * w * 4, w * 4); return EncodePng(w, h, pixels); }
    private static byte[] TrimPng(byte[] bytes) { var image = DecodePng(bytes); var l = image.Width; var t = image.Height; var r = -1; var b = -1; for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) if (image.Pixels[(y * image.Width + x) * 4 + 3] != 0) { l = Math.Min(l, x); t = Math.Min(t, y); r = Math.Max(r, x); b = Math.Max(b, y); } return r < 0 ? EncodePng(1, 1, new byte[4]) : CropPng(bytes, new Selection("region", l, t, r - l + 1, b - t + 1)); }
    private static byte[] ScalePng(byte[] bytes, int factor) { var image = DecodePng(bytes); var pixels = new byte[image.Width * factor * image.Height * factor * 4]; for (var y = 0; y < image.Height; y++) for (var x = 0; x < image.Width; x++) for (var yy = 0; yy < factor; yy++) for (var xx = 0; xx < factor; xx++) Buffer.BlockCopy(image.Pixels, (y * image.Width + x) * 4, pixels, (((y * factor + yy) * image.Width * factor) + x * factor + xx) * 4, 4); return EncodePng(image.Width * factor, image.Height * factor, pixels); }
    private static byte[] TrimWav(byte[] bytes, Selection selection) { if (bytes.Length < 44 || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF" || Encoding.ASCII.GetString(bytes, 8, 4) != "WAVE") throw new InvalidDataException("invalid WAV"); var channels = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(22)); var bits = BinaryPrimitives.ReadInt16LittleEndian(bytes.AsSpan(34)); if (channels <= 0 || bits != 16) throw new InvalidDataException("audio trim supports PCM16 WAV only"); var data = 12; while (data + 8 <= bytes.Length && Encoding.ASCII.GetString(bytes, data, 4) != "data") { var chunkSize = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(data + 4)); if (chunkSize < 0) throw new InvalidDataException("invalid WAV chunk"); data += 8 + chunkSize + (chunkSize & 1); } if (data + 8 > bytes.Length) throw new InvalidDataException("WAV data chunk missing"); var originalSize = BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(data + 4)); var start = Math.Max(0, selection.StartSampleFrame) * channels * 2; var end = selection.EndSampleFrame > selection.StartSampleFrame ? selection.EndSampleFrame * channels * 2 : originalSize; var count = Math.Min(end, originalSize) - Math.Min(start, originalSize); if (count <= 0) throw new InvalidDataException("audio trim is empty"); var result = bytes[..(data + 8)].ToArray(); BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(data + 4), count); result = result.Concat(bytes.AsSpan(data + 8 + Math.Min(start, originalSize), count).ToArray()).ToArray(); BinaryPrimitives.WriteInt32LittleEndian(result.AsSpan(4), result.Length - 8); return result; }
    private sealed record Png(int Width, int Height, byte[] Pixels);
    private static Png DecodePng(byte[] bytes) { if (bytes.Length < 33 || !bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) throw new InvalidDataException("invalid PNG"); var at = 8; var idat = new MemoryStream(); int w = 0, h = 0; while (at + 8 <= bytes.Length) { var len = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(at)); var tag = Encoding.ASCII.GetString(bytes, at + 4, 4); if (tag == "IHDR") { w = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(at + 8)); h = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(at + 12)); if (bytes[at + 16] != 8 || bytes[at + 17] != 6 || bytes[at + 20] != 0) throw new InvalidDataException("PNG must be RGBA8 non-interlaced"); } else if (tag == "IDAT") idat.Write(bytes, at + 8, len); at += 12 + len; if (tag == "IEND") break; } idat.Position = 0; using var z = new ZLibStream(idat, CompressionMode.Decompress); using var raw = new MemoryStream(); z.CopyTo(raw); var pixels = new byte[w * h * 4]; var scan = raw.ToArray(); var stride = w * 4; for (var y = 0; y < h; y++) { if (scan[y * (stride + 1)] != 0) throw new InvalidDataException("PNG filter unsupported"); Buffer.BlockCopy(scan, y * (stride + 1) + 1, pixels, y * stride, stride); } return new(w, h, pixels); }
    private static byte[] EncodePng(int width, int height, byte[] pixels) { using var output = new MemoryStream(); output.Write(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }); var header = new byte[13]; BinaryPrimitives.WriteInt32BigEndian(header, width); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height); header[8] = 8; header[9] = 6; Chunk(output, "IHDR", header); using var raw = new MemoryStream(); for (var y = 0; y < height; y++) { raw.WriteByte(0); raw.Write(pixels, y * width * 4, width * 4); } raw.Position = 0; using var compressed = new MemoryStream(); using (var z = new ZLibStream(compressed, CompressionLevel.SmallestSize, true)) raw.CopyTo(z); Chunk(output, "IDAT", compressed.ToArray()); Chunk(output, "IEND", []); return output.ToArray(); }
    private static void Chunk(Stream stream, string name, byte[] data) { var n = Encoding.ASCII.GetBytes(name); var length = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(length, data.Length); stream.Write(length); stream.Write(n); stream.Write(data); var crc = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(n.Concat(data).ToArray())); stream.Write(crc); }
    private static uint Crc32(byte[] bytes) { uint crc = 0xffffffff; foreach (var b in bytes) { crc ^= b; for (var i = 0; i < 8; i++) crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1)); } return ~crc; }
}
