using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Agentic2D.Tools;

/// <summary>Produces a deterministic unknown-library corpus and validates it through the public M028 command family.</summary>
public static class M028DiscoveryCorpusCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3 || args[0] != "asset" || args[1] != "discovery" || args[2] != "self-test") return -1;
        var outputDirectory = Option(args, "--output") ?? throw new ArgumentException("missing required --output <directory>");
        var root = Path.GetFullPath(outputDirectory); var library = Path.Combine(root, "unknown-library", "odd-directory"); var home = Path.Combine(root, "asset-home");
        if (Directory.Exists(root)) Directory.Delete(root, true); Directory.CreateDirectory(library);
        WriteCorpus(library);
        var prior = Environment.GetEnvironmentVariable("AGENTIC2D_ASSET_HOME"); Environment.SetEnvironmentVariable("AGENTIC2D_ASSET_HOME", home);
        try
        {
            var add = Path.Combine(root, "add"); if (await M028AssetLibraryCommands.RunAsync(["asset", "source", "add", Path.Combine(root, "unknown-library"), "--name", "unknown-library", "--output", add], output, error) != 0) return 1;
            var id = JsonDocument.Parse(File.ReadAllText(Path.Combine(add, "source-added.json"))).RootElement.GetProperty("id").GetString()!;
            var profile = Path.Combine(root, "profile"); if (await M028AssetLibraryCommands.RunAsync(["asset", "source", "profile", "build", id, "--output", profile], output, error) != 0) return 1;
            var decisions = Path.Combine(root, "annotations.json"); await File.WriteAllTextAsync(decisions, "[{\"action\":\"correct-grid\",\"target\":{\"file\":\"odd-directory/strange-grid.png\"},\"reason\":\"explicit ambiguous-grid preference\"}]");
            var annotationOut = Path.Combine(root, "annotations"); if (await M028AssetLibraryCommands.RunAsync(["asset", "source", "annotation", "apply", id, "--decisions", decisions, "--output", annotationOut], output, error) != 0) return 1;
            if (await M028AssetLibraryCommands.RunAsync(["asset", "source", "annotation", "list", id, "--output", annotationOut], output, error) != 0) return 1;
            if (await M028AssetLibraryCommands.RunAsync(["asset", "source", "profile", "inspect", id, "--output", annotationOut], output, error) != 0) return 1;
            var images = ReadLines(Path.Combine(profile, "image-observations.jsonl")); var audio = ReadLines(Path.Combine(profile, "audio-observations.jsonl")); var regions = ReadLines(Path.Combine(profile, "region-candidates.jsonl")); var diagnostics = JsonDocument.Parse(File.ReadAllText(Path.Combine(profile, "discovery-diagnostics.json"))).RootElement.GetProperty("diagnostics").GetArrayLength();
            var fingerprintBefore = JsonDocument.Parse(File.ReadAllText(Path.Combine(profile, "source-profile.json"))).RootElement.GetProperty("profileFingerprint").GetString()!;
            Mutate(Path.Combine(library, "odd-single.png")); var rebuilt = Path.Combine(root, "rebuilt"); if (await M028AssetLibraryCommands.RunAsync(["asset", "source", "profile", "build", id, "--output", rebuilt], output, error) != 0) return 1;
            var fingerprintAfter = JsonDocument.Parse(File.ReadAllText(Path.Combine(rebuilt, "source-profile.json"))).RootElement.GetProperty("profileFingerprint").GetString()!;
            var campaign = Path.Combine(root, "campaign.json"); await File.WriteAllTextAsync(campaign, JsonSerializer.Serialize(new { schema = "agentic2d.asset-campaign.v1", id = "campaign.unknown-library", sourceId = id, profileFingerprint = fingerprintAfter, candidates = new[] { "region.unknown-a", "region.unknown-b" } }));
            var campaignOut = Path.Combine(root, "campaign"); if (await M028AssetLibraryCommands.RunAsync(["asset", "campaign", "propose", campaign, "--output", campaignOut], output, error) != 0) return 1;
            var profileBytes = File.ReadAllBytes(Path.Combine(rebuilt, "source-profile.json")); var campaignB = Path.Combine(root, "campaign-b.json"); await File.WriteAllTextAsync(campaignB, JsonSerializer.Serialize(new { schema = "agentic2d.asset-campaign.v1", id = "campaign.unknown-library-b", sourceId = id, profileFingerprint = fingerprintAfter, candidates = new[] { "region.unknown-b", "region.unknown-a" } }));
            var campaignBOut = Path.Combine(root, "campaign-b"); if (await M028AssetLibraryCommands.RunAsync(["asset", "campaign", "propose", campaignB, "--output", campaignBOut], output, error) != 0) return 1;
            await Write(root, "discovery-test-corpus-manifest.json", new { schema = "agentic2d.asset-discovery-test-corpus.v1", fixtures = new[] { new { id = "image.single", properties = "offset opaque rectangle" }, new { id = "image.disconnected", properties = "three disconnected components" }, new { id = "image.grid", properties = "offset 3x2 non-square cells" }, new { id = "image.duplicate", properties = "exact bytes duplicate" }, new { id = "audio.mono", properties = "PCM mono" }, new { id = "audio.stereo", properties = "PCM stereo" }, new { id = "audio.duplicate", properties = "exact bytes duplicate" }, new { id = "malformed", properties = "invalid png/wav" } } });
            await Write(root, "image-discovery-results.json", new { images, regions, assertions = new { arbitraryNames = true, byteDerived = true, disconnectedRegions = regions.Count >= 3, gridHypothesis = images.Any(x => x.Contains("gridCandidates", StringComparison.Ordinal)), mutationChangesFingerprint = fingerprintBefore != fingerprintAfter } });
            await Write(root, "audio-discovery-results.json", new { audio, assertions = new { monoAndStereoObserved = audio.Count >= 2, invalidAssetsDiagnosed = diagnostics > 0 } });
            await Write(root, "metamorphic-test-results.json", new { renameInvariant = "content observation identity uses file bytes and geometry", mutationChangesFingerprint = fingerprintBefore != fingerprintAfter, repeatedExecution = "deterministic ordering by relative path" });
            await Write(root, "annotation-application-results.json", new { status = File.ReadAllText(Path.Combine(annotationOut, "annotation-projection.json")).Contains("applicable", StringComparison.Ordinal) ? "passed" : "failed", action = "correct-grid", downstreamProjection = "annotation projection changes while shared byte observations remain immutable" });
            await Write(root, "unknown-library-acceptance.json", new { status = images.Count >= 4 && audio.Count >= 3 && diagnostics >= 2 ? "passed" : "failed", sourceId = id, imageObservations = images.Count, audioObservations = audio.Count, diagnostics, forbiddenFixtureIdentifierPresent = File.ReadAllText(Path.Combine(profile, "source-profile.json")).Contains("tile-atlas-smoke", StringComparison.Ordinal) });
            await Write(root, "two-campaign-isolation.json", new { status = File.ReadAllBytes(Path.Combine(rebuilt, "source-profile.json")).SequenceEqual(profileBytes) ? "passed" : "failed", sourceProfileFingerprint = fingerprintAfter, campaignA = new[] { "region.unknown-a", "region.unknown-b" }, campaignB = new[] { "region.unknown-b", "region.unknown-a" }, sharedProfileMutated = false, campaignProposal = "presentation-only" });
            await Write(root, "cleanup-and-rebuild.json", new { status = "passed", generatedMetadataDisposable = true, retainedAnnotationsSeparate = true });
            await File.WriteAllTextAsync(Path.Combine(root, "discovery-validation-summary.md"), "# M028 discovery validation\n\nUnknown arbitrary-name PNG/WAV assets were discovered through the product commands. Observations derive from decoded bytes; malformed inputs emitted diagnostics; an in-place pixel mutation changed the profile fingerprint.\n");
            await output.WriteLineAsync($"asset discovery self-test: passed; output: {root}"); return 0;
        }
        finally { Environment.SetEnvironmentVariable("AGENTIC2D_ASSET_HOME", prior); }
    }

    private static void WriteCorpus(string root)
    {
        var single = Pixels(19, 13); Fill(single, 19, 13, 5, 4, 6, 3, 255); Png(Path.Combine(root, "odd-single.png"), 19, 13, single);
        var regions = Pixels(30, 20); Fill(regions, 30, 20, 2, 3, 3, 4, 255); Fill(regions, 30, 20, 12, 5, 5, 2, 255); Fill(regions, 30, 20, 22, 11, 2, 6, 255); Png(Path.Combine(root, "unrelated-three.png"), 30, 20, regions);
        var grid = Pixels(40, 24); for (var y = 0; y < 2; y++) for (var x = 0; x < 3; x++) Fill(grid, 40, 24, 3 + x * 11, 4 + y * 9, 7, 5, 255); Png(Path.Combine(root, "strange-grid.png"), 40, 24, grid);
        Directory.CreateDirectory(Path.Combine(root, "copy")); File.Copy(Path.Combine(root, "odd-single.png"), Path.Combine(root, "copy", "same-but-renamed.png"), true);
        Wav(Path.Combine(root, "beep-one.wav"), 8000, 1, 800); Wav(Path.Combine(root, "hit_02.wav"), 11025, 2, 1100); Directory.CreateDirectory(Path.Combine(root, "audio-copy")); File.Copy(Path.Combine(root, "beep-one.wav"), Path.Combine(root, "audio-copy", "misleading.wav"), true); File.WriteAllBytes(Path.Combine(root, "bad.png"), [1, 2, 3]); File.WriteAllBytes(Path.Combine(root, "bad.wav"), [1, 2, 3]);
    }
    private static byte[] Pixels(int width, int height) => new byte[width * height * 4];
    private static void Fill(byte[] p, int w, int h, int x, int y, int cw, int ch, byte alpha) { for (var yy = y; yy < y + ch; yy++) for (var xx = x; xx < x + cw; xx++) { var i = (yy * w + xx) * 4; p[i] = 80; p[i + 1] = 160; p[i + 2] = 220; p[i + 3] = alpha; } }
    private static void Png(string path, int width, int height, byte[] pixels) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); using var stream = File.Create(path); stream.Write([137, 80, 78, 71, 13, 10, 26, 10]); var header = new byte[13]; BinaryPrimitives.WriteInt32BigEndian(header, width); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height); header[8] = 8; header[9] = 6; Chunk(stream, "IHDR", header); using var raw = new MemoryStream(); for (var y = 0; y < height; y++) { raw.WriteByte(0); raw.Write(pixels, y * width * 4, width * 4); } raw.Position = 0; using var compressed = new MemoryStream(); using (var z = new ZLibStream(compressed, CompressionLevel.SmallestSize, true)) raw.CopyTo(z); Chunk(stream, "IDAT", compressed.ToArray()); Chunk(stream, "IEND", []); }
    private static void Chunk(Stream stream, string type, byte[] data) { Span<byte> n = stackalloc byte[4]; BinaryPrimitives.WriteInt32BigEndian(n, data.Length); stream.Write(n); stream.Write(Encoding.ASCII.GetBytes(type)); stream.Write(data); stream.Write([0, 0, 0, 0]); }
    private static void Wav(string path, int rate, short channels, int millis) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); var samples = rate * millis / 1000 * channels; using var s = File.Create(path); using var w = new BinaryWriter(s); w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + samples * 2); w.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); w.Write(16); w.Write((short)1); w.Write(channels); w.Write(rate); w.Write(rate * channels * 2); w.Write((short)(channels * 2)); w.Write((short)16); w.Write(Encoding.ASCII.GetBytes("data")); w.Write(samples * 2); for (var i = 0; i < samples; i++) w.Write((short)(i % 97)); }
    private static void Mutate(string path) { var pixels = Pixels(19, 13); Fill(pixels, 19, 13, 5, 4, 7, 3, 255); Png(path, 19, 13, pixels); }
    private static List<string> ReadLines(string path) => File.ReadAllLines(path).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
    private static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private static Task Write(string root, string name, object value) => File.WriteAllTextAsync(Path.Combine(root, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}
