using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.Sound;

namespace Agentic2D.Tools;

/// <summary>Explicit, author-declared linkage validation. It intentionally does not change cue projection.</summary>
internal static class M026SoundLinkageCommands
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 4 || args[0] != "sound" || args[1] != "linkage" || args[2] is not ("inspect" or "validate")) return -1;
        var destination = Option(args, "--output");
        if (destination is null) { await error.WriteLineAsync("sound linkage inspect|validate requires <project-or-workspace> --output <directory>"); return 2; }
        var project = Path.GetFullPath(args[3]);
        if (File.Exists(project) && Path.GetFileName(project) == "agentic2d.project.json") project = Path.GetDirectoryName(project)!;
        var diagnostics = new List<LinkageDiagnostic>(); var entries = new List<LinkageEntry>();
        var source = Path.Combine(project, "game-content", "generated-sound-linkage.json");
        if (!File.Exists(source)) source = Path.Combine(project, "game-content", "sounds", "generated-sound-linkage.json");
        if (!File.Exists(source)) diagnostics.Add(new("SOUNDLINK0001", "error", "Explicit generated-sound linkage manifest is missing.", source, "generated-sound-linkage.json"));
        else
        {
            using var document = JsonDocument.Parse(File.ReadAllText(source));
            if (!document.RootElement.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array) diagnostics.Add(new("SOUNDLINK0002", "error", "Linkage manifest requires links[].", source, "links"));
            else foreach (var link in links.EnumerateArray()) ValidateLink(project, source, link, diagnostics, entries);
            var claims = links.EnumerateArray().Select(x => x.TryGetProperty("outputPath", out var path) ? path.GetString() : null).Where(x => x is not null).GroupBy(x => x!, StringComparer.Ordinal);
            foreach (var claim in claims.Where(x => x.Count() > 1)) diagnostics.Add(new("SOUNDLINK0010", "error", "Incompatible multiple claims reference one generated output.", source, "links[].outputPath"));
        }
        var synthRoot = Path.Combine(project, "game-content", "sound-synthesis");
        if (Directory.Exists(synthRoot)) foreach (var synth in Directory.EnumerateFiles(synthRoot, "*.json", SearchOption.AllDirectories))
            {
                using var synthesisDocument = JsonDocument.Parse(File.ReadAllText(synth));
                var definitions = synthesisDocument.RootElement.ValueKind == JsonValueKind.Array ? synthesisDocument.RootElement.EnumerateArray() : new[] { synthesisDocument.RootElement }.AsEnumerable();
                foreach (var definition in definitions)
                {
                    var id = definition.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                    if (!string.IsNullOrWhiteSpace(id) && !entries.Any(x => x.SynthesisId == id)) diagnostics.Add(new("SOUNDLINK0009", "error", "Synthesis definition has no explicit linkage claim (orphan source).", synth, "id"));
                }
            }
        var generatedRoot = Path.Combine(project, "game-content", "generated", "sounds");
        if (Directory.Exists(generatedRoot)) foreach (var generatedWav in Directory.EnumerateFiles(generatedRoot, "*.wav", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(project, generatedWav).Replace('\\', '/');
                if (!entries.Any(x => string.Equals(x.OutputPath, relative, StringComparison.Ordinal))) diagnostics.Add(new("SOUNDLINK0012", "error", "Generated WAV has no explicit linkage claim (orphan generated output).", generatedWav, "outputPath"));
            }
        foreach (var duplicate in entries.GroupBy(x => x.SynthesisId, StringComparer.Ordinal).Where(x => x.Count() > 1)) diagnostics.Add(new("SOUNDLINK0013", "error", "One synthesis definition has incompatible multiple linkage claims.", source, "links[].synthesisId"));
        foreach (var duplicate in entries.GroupBy(x => x.SoundDefinitionId + "|" + x.VariantId, StringComparer.Ordinal).Where(x => x.Count() > 1)) diagnostics.Add(new("SOUNDLINK0014", "error", "One ordinary sound variant has incompatible multiple generated-output claims.", source, "links[].soundDefinitionId"));
        var status = diagnostics.Any(x => x.Severity == "error") ? "failed" : "passed";
        Directory.CreateDirectory(destination);
        var report = new { schema = "agentic2d.generated-sound-linkage-report.v1", status, project, links = entries, diagnostics, fingerprint = Hash(new { entries, diagnostics }) };
        await File.WriteAllTextAsync(Path.Combine(destination, "generated-sound-linkage-report.json"), JsonSerializer.Serialize(report, Json));
        await File.WriteAllTextAsync(Path.Combine(destination, "generated-sound-linkage-report.md"), "# Generated Sound Linkage Report\n\nStatus: `" + status + "`\n\n| Synthesis | Output | Ordinary sound definition | Variant | Status |\n|---|---|---|---|---|\n" + string.Join("\n", entries.Select(x => "| `" + x.SynthesisId + "` | `" + x.OutputPath + "` | `" + x.SoundDefinitionId + "` | `" + x.VariantId + "` | linked |")) + "\n\n## Diagnostics\n\n" + (diagnostics.Count == 0 ? "None." : string.Join("\n", diagnostics.Select(x => "- `" + x.Id + "` (" + x.Severity + "): " + x.Message + " — `" + x.Target + "` / `" + x.Field + "`"))) + "\n");
        await output.WriteLineAsync("sound linkage " + args[2] + ": " + status + "; output: " + destination);
        return status == "passed" ? 0 : 1;
    }
    private static void ValidateLink(string project, string manifest, JsonElement link, List<LinkageDiagnostic> diagnostics, List<LinkageEntry> entries)
    {
        string Get(string name) => link.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";
        var synthesisId = Get("synthesisId"); var outputPath = Get("outputPath"); var soundDefinitionId = Get("soundDefinitionId"); var variantId = Get("variantId");
        var synth = Directory.Exists(Path.Combine(project, "game-content", "sound-synthesis")) ? Directory.EnumerateFiles(Path.Combine(project, "game-content", "sound-synthesis"), "*.json", SearchOption.AllDirectories).FirstOrDefault(path => File.ReadAllText(path).Contains("\"id\":\"" + synthesisId + "\"", StringComparison.Ordinal)) : null;
        if (synth is null) diagnostics.Add(new("SOUNDLINK0003", "error", "Linked synthesis definition is missing.", manifest, "synthesisId"));
        var output = Path.Combine(project, outputPath.Replace('/', Path.DirectorySeparatorChar)); if (!File.Exists(output)) diagnostics.Add(new("SOUNDLINK0004", "error", "Linked generated WAV is missing.", manifest, "outputPath"));
        var provenance = Path.ChangeExtension(output, ".provenance.json"); if (!File.Exists(provenance)) provenance = Path.Combine(project, Path.GetFileNameWithoutExtension(output) + ".provenance.json");
        if (!File.Exists(provenance)) diagnostics.Add(new("SOUNDLINK0005", "error", "Generated WAV provenance is missing.", manifest, "outputPath"));
        var sounds = Directory.Exists(Path.Combine(project, "game-content", "sounds")) ? Directory.EnumerateFiles(Path.Combine(project, "game-content", "sounds"), "*.json", SearchOption.AllDirectories) : [];
        var sound = sounds.FirstOrDefault(path => File.ReadAllText(path).Contains("\"id\":\"" + soundDefinitionId + "\"", StringComparison.Ordinal));
        if (sound is null) diagnostics.Add(new("SOUNDLINK0006", "error", "Linked ordinary sound definition is missing.", manifest, "soundDefinitionId"));
        else if (!HasMappedVariant(sound, variantId, outputPath)) diagnostics.Add(new("SOUNDLINK0007", "error", "Ordinary sound definition maps the declared variant to a different generated output.", sound, "variants[].asset"));
        if (File.Exists(provenance) && synth is not null)
        {
            using var provenanceDocument = JsonDocument.Parse(File.ReadAllText(provenance));
            var expected = OfflineSoundSynthesis.Fingerprint(JsonSerializer.Deserialize<SoundSynthesisDefinition>(File.ReadAllText(synth), OfflineSoundSynthesis.Json)!);
            if (!Property(provenanceDocument.RootElement, "definitionFingerprint", "DefinitionFingerprint", out var fingerprint) || fingerprint.GetString() != expected) diagnostics.Add(new("SOUNDLINK0008", "error", "Generated provenance has a stale definition fingerprint.", provenance, "definitionFingerprint"));
            if (File.Exists(output) && Property(provenanceDocument.RootElement, "outputSha256", "OutputSha256", out var hash) && hash.GetString() != FileHash(output)) diagnostics.Add(new("SOUNDLINK0011", "error", "Generated WAV hash drifted from provenance.", output, "outputSha256"));
        }
        entries.Add(new LinkageEntry(synthesisId, outputPath, Path.GetRelativePath(project, provenance).Replace('\\', '/'), soundDefinitionId, variantId, "ordinary-sound-definition", "synthesis-definition"));
    }
    private static bool HasMappedVariant(string path, string variantId, string outputPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        if (!document.RootElement.TryGetProperty("variants", out var variants) || variants.ValueKind != JsonValueKind.Array) return false;
        return variants.EnumerateArray().Any(variant =>
            (!variant.TryGetProperty("id", out var id) || string.IsNullOrWhiteSpace(variantId) || id.GetString() == variantId) &&
            variant.TryGetProperty("asset", out var asset) && asset.GetString() == outputPath);
    }
    private static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private static bool Property(JsonElement element, string camel, string pascal, out JsonElement value) => element.TryGetProperty(camel, out value) || element.TryGetProperty(pascal, out value);
    private static string FileHash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static string Hash(object value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Json)))).ToLowerInvariant();
    private sealed record LinkageDiagnostic(string Id, string Severity, string Message, string Target, string Field);
    private sealed record LinkageEntry(string SynthesisId, string OutputPath, string ProvenancePath, string SoundDefinitionId, string VariantId, string RuntimeAuthority, string GenerationAuthority);
}
