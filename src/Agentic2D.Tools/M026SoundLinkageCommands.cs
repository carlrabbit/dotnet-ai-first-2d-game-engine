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
        if (args.Length < 4 || args[0] != "sound" || args[1] != "linkage" || args[2] is not ("inspect" or "validate" or "review-pack")) return -1;
        var destination = Option(args, "--output");
        if (destination is null) { await error.WriteLineAsync("sound linkage inspect|validate|review-pack requires <project-or-workspace> --output <directory>"); return 2; }
        var project = Path.GetFullPath(args[3]);
        if (File.Exists(project) && Path.GetFileName(project) == "agentic2d.project.json") project = Path.GetDirectoryName(project)!;
        var diagnostics = new List<LinkageDiagnostic>(); var entries = new List<LinkageEntry>();
        var source = Path.Combine(project, "game-content", "generated-sound-linkage.json");
        if (!File.Exists(source)) source = Path.Combine(project, "game-content", "sounds", "generated-sound-linkage.json");
        if (!File.Exists(source)) diagnostics.Add(new("SNDL010", "error", "Explicit generated-sound linkage manifest is missing or unsupported.", Relative(project, source), "schema"));
        else
        {
            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(source));
                if (!document.RootElement.TryGetProperty("schema", out var schema) || schema.GetString() != "agentic2d.generated-sound-linkage.v1") diagnostics.Add(new("SNDL010", "error", "Linkage manifest schema/version is unsupported.", Relative(project, source), "schema"));
                if (!document.RootElement.TryGetProperty("links", out var links) || links.ValueKind != JsonValueKind.Array) diagnostics.Add(new("SNDL010", "error", "Linkage manifest requires links[].", Relative(project, source), "links"));
                else
                {
                    foreach (var link in links.EnumerateArray()) ValidateLink(project, source, link, diagnostics, entries);
                    var claims = links.EnumerateArray().Select(x => x.TryGetProperty("outputPath", out var path) ? path.GetString() : null).Where(x => x is not null).GroupBy(x => x!, StringComparer.Ordinal);
                    foreach (var claim in claims.Where(x => x.Count() > 1)) diagnostics.Add(new("SNDL005", "error", "Incompatible multiple claims reference one generated output.", Relative(project, source), "links[].outputPath"));
                }
            }
            catch (JsonException)
            {
                diagnostics.Add(new("SNDL010", "error", "Linkage manifest is malformed or uses an unsupported schema.", Relative(project, source), "schema"));
            }
        }
        var synthRoot = Path.Combine(project, "game-content", "sound-synthesis");
        if (Directory.Exists(synthRoot)) foreach (var synth in Directory.EnumerateFiles(synthRoot, "*.json", SearchOption.AllDirectories))
        {
            using var synthesisDocument = JsonDocument.Parse(File.ReadAllText(synth));
            var definitions = synthesisDocument.RootElement.ValueKind == JsonValueKind.Array ? synthesisDocument.RootElement.EnumerateArray() : new[] { synthesisDocument.RootElement }.AsEnumerable();
            foreach (var definition in definitions)
            {
                var id = definition.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
                if (!string.IsNullOrWhiteSpace(id) && !entries.Any(x => x.SynthesisId == id)) diagnostics.Add(new("SNDL004", "warning", "Synthesis definition has no ordinary sound linkage claim.", Relative(project, synth), "id"));
            }
        }
        var generatedRoot = Path.Combine(project, "game-content", "generated", "sounds");
        if (Directory.Exists(generatedRoot)) foreach (var generatedWav in Directory.EnumerateFiles(generatedRoot, "*.wav", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(project, generatedWav).Replace('\\', '/');
            if (!entries.Any(x => string.Equals(x.OutputPath, relative, StringComparison.Ordinal))) diagnostics.Add(new("SNDL004", "error", "Generated output has no ordinary sound definition linkage.", Relative(project, generatedWav), "outputPath"));
        }
        foreach (var duplicate in entries.GroupBy(x => x.SynthesisId, StringComparer.Ordinal).Where(x => x.Count() > 1)) diagnostics.Add(new("SNDL005", "error", "One synthesis definition has incompatible multiple linkage claims.", Relative(project, source), "links[].synthesisId"));
        foreach (var duplicate in entries.GroupBy(x => x.SoundDefinitionId + "|" + x.VariantId, StringComparer.Ordinal).Where(x => x.Count() > 1)) diagnostics.Add(new("SNDL005", "error", "One ordinary sound variant has incompatible multiple generated-output claims.", Relative(project, source), "links[].soundDefinitionId"));
        var exportDirectory = Option(args, "--export");
        if (exportDirectory is not null) ValidateExportInclusion(Path.GetFullPath(exportDirectory), entries, diagnostics);
        var status = diagnostics.Any(x => x.Severity == "error") ? "failed" : "passed";
        Directory.CreateDirectory(destination);
        var normalizedDiagnostics = diagnostics.OrderBy(diagnostic => diagnostic.Id, StringComparer.Ordinal).ThenBy(diagnostic => diagnostic.Target, StringComparer.Ordinal).Select(diagnostic => new { code = diagnostic.Id, severity = diagnostic.Severity, message = diagnostic.Message, sourcePath = diagnostic.Target, fieldPath = diagnostic.Field, remediation = Remediation(diagnostic.Id) }).ToArray();
        var report = new { schema = "agentic2d.generated-sound-linkage-report.v1", status, project = Path.GetFileName(project), exportStatus = exportDirectory is null ? "not-inspected" : "inspected", links = entries.OrderBy(entry => entry.SynthesisId, StringComparer.Ordinal), diagnostics = normalizedDiagnostics, fingerprint = Hash(new { entries, normalizedDiagnostics }) };
        await File.WriteAllTextAsync(Path.Combine(destination, "generated-sound-linkage-report.json"), JsonSerializer.Serialize(report, Json));
        await File.WriteAllTextAsync(Path.Combine(destination, "generated-sound-linkage-report.md"), "# Generated Sound Linkage Report\n\nStatus: `" + status + "`\n\n| Synthesis | Output | Ordinary sound definition | Variant | Status |\n|---|---|---|---|---|\n" + string.Join("\n", entries.OrderBy(x => x.SynthesisId, StringComparer.Ordinal).Select(x => "| `" + x.SynthesisId + "` | `" + x.OutputPath + "` | `" + x.SoundDefinitionId + "` | `" + x.VariantId + "` | linked |")) + "\n\n## Diagnostics\n\n" + (normalizedDiagnostics.Length == 0 ? "None." : string.Join("\n", normalizedDiagnostics.Select(x => "- `" + x.code + "` (" + x.severity + "): " + x.message + " — `" + x.sourcePath + "` / `" + x.fieldPath + "`"))) + "\n");
        await File.WriteAllTextAsync(Path.Combine(destination, "generated-sound-linkage.json"), JsonSerializer.Serialize(new { schema = "agentic2d.generated-sound-linkage.v1", links = entries.OrderBy(entry => entry.SynthesisId, StringComparer.Ordinal), fingerprint = Hash(entries) }, Json));
        await File.WriteAllTextAsync(Path.Combine(destination, "generated-sound-provenance.json"), JsonSerializer.Serialize(new { schema = "agentic2d.generated-sound-provenance.v1", entries = entries.OrderBy(entry => entry.SynthesisId, StringComparer.Ordinal).Select(entry => new { entry.SynthesisId, entry.OutputPath, entry.ProvenancePath, outputSha256 = OutputHash(project, entry.OutputPath) }) }, Json));
        if (args[2] == "review-pack") await WriteReviewPackAsync(destination, status);
        await output.WriteLineAsync("sound linkage " + args[2] + ": " + status + "; output: " + destination);
        return status == "passed" ? 0 : 1;
    }
    private static void ValidateLink(string project, string manifest, JsonElement link, List<LinkageDiagnostic> diagnostics, List<LinkageEntry> entries)
    {
        string Get(string name) => link.TryGetProperty(name, out var value) ? value.GetString() ?? "" : "";
        var synthesisId = Get("synthesisId"); var outputPath = Get("outputPath"); var soundDefinitionId = Get("soundDefinitionId"); var variantId = Get("variantId");
        var synth = Directory.Exists(Path.Combine(project, "game-content", "sound-synthesis")) ? Directory.EnumerateFiles(Path.Combine(project, "game-content", "sound-synthesis"), "*.json", SearchOption.AllDirectories).FirstOrDefault(path => File.ReadAllText(path).Contains("\"id\":\"" + synthesisId + "\"", StringComparison.Ordinal)) : null;
        if (synth is null) diagnostics.Add(new("SNDL007", "error", "Linked synthesis definition is missing.", Relative(project, manifest), "links[].synthesisId"));
        var output = Path.Combine(project, outputPath.Replace('/', Path.DirectorySeparatorChar)); if (!File.Exists(output)) diagnostics.Add(new("SNDL001", "error", "Linked generated WAV is missing.", Relative(project, manifest), "links[].outputPath"));
        var provenance = Path.ChangeExtension(output, ".provenance.json"); if (!File.Exists(provenance)) provenance = Path.Combine(project, Path.GetFileNameWithoutExtension(output) + ".provenance.json");
        if (!File.Exists(provenance)) diagnostics.Add(new("SNDL008", "error", "Generated WAV provenance is missing or malformed.", Relative(project, manifest), "links[].outputPath"));
        var sounds = Directory.Exists(Path.Combine(project, "game-content", "sounds")) ? Directory.EnumerateFiles(Path.Combine(project, "game-content", "sounds"), "*.json", SearchOption.AllDirectories) : [];
        var sound = sounds.FirstOrDefault(path => File.ReadAllText(path).Contains("\"id\":\"" + soundDefinitionId + "\"", StringComparison.Ordinal));
        if (sound is null) diagnostics.Add(new("SNDL003", "error", "Linked ordinary sound definition is missing.", Relative(project, manifest), "links[].soundDefinitionId"));
        else if (!HasMappedVariant(sound, variantId, outputPath)) diagnostics.Add(new("SNDL003", "error", "Ordinary sound definition maps the declared variant to a different generated output.", Relative(project, sound), "variants[].asset"));
        if (File.Exists(provenance) && synth is not null)
        {
            try
            {
                using var provenanceDocument = JsonDocument.Parse(File.ReadAllText(provenance));
                var expected = OfflineSoundSynthesis.Fingerprint(JsonSerializer.Deserialize<SoundSynthesisDefinition>(File.ReadAllText(synth), OfflineSoundSynthesis.Json)!);
                if (!Property(provenanceDocument.RootElement, "definitionFingerprint", "DefinitionFingerprint", out var fingerprint)) diagnostics.Add(new("SNDL008", "error", "Generated provenance is missing definitionFingerprint.", Relative(project, provenance), "definitionFingerprint"));
                else if (fingerprint.GetString() != expected) diagnostics.Add(new("SNDL006", "error", "Generated provenance has a stale synthesis-definition fingerprint.", Relative(project, provenance), "definitionFingerprint"));
                if (!Property(provenanceDocument.RootElement, "outputSha256", "OutputSha256", out var hash)) diagnostics.Add(new("SNDL008", "error", "Generated provenance is missing outputSha256.", Relative(project, provenance), "outputSha256"));
                else if (File.Exists(output) && hash.GetString() != FileHash(output)) diagnostics.Add(new("SNDL002", "error", "Generated WAV hash differs from provenance.", Relative(project, output), "outputSha256"));
            }
            catch (JsonException)
            {
                diagnostics.Add(new("SNDL008", "error", "Generated provenance is malformed.", Relative(project, provenance), "provenance"));
            }
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
    private static void ValidateExportInclusion(string exportDirectory, IReadOnlyList<LinkageEntry> entries, List<LinkageDiagnostic> diagnostics)
    {
        if (!Directory.Exists(exportDirectory))
        {
            diagnostics.Add(new("SNDL009", "error", "Requested export directory is unavailable for linked-output validation.", exportDirectory, "--export"));
            return;
        }

        foreach (var entry in entries)
        {
            var exportRelative = entry.OutputPath.StartsWith("game-content/", StringComparison.Ordinal) ? "game/" + entry.OutputPath["game-content/".Length..] : entry.OutputPath;
            if (!File.Exists(Path.Combine(exportDirectory, exportRelative.Replace('/', Path.DirectorySeparatorChar))))
                diagnostics.Add(new("SNDL009", "error", "Export omitted a generated output linked by an included ordinary sound definition.", exportRelative, "links[].outputPath"));
        }
    }
    private static string Relative(string project, string path) => Path.GetRelativePath(project, path).Replace('\\', '/');
    private static string OutputHash(string project, string outputPath) { var path = Path.Combine(project, outputPath.Replace('/', Path.DirectorySeparatorChar)); return File.Exists(path) ? FileHash(path) : string.Empty; }
    private static string Remediation(string code) => code switch { "SNDL001" => "Regenerate the declared output from its synthesis definition.", "SNDL002" => "Regenerate the output and provenance together.", "SNDL003" => "Correct the ordinary sound definition reference.", "SNDL004" => "Add an explicit ordinary sound linkage or remove the orphan output.", "SNDL005" => "Keep one compatible linkage per identity.", "SNDL006" => "Regenerate after changing the synthesis definition.", "SNDL007" => "Reference an existing synthesis definition.", "SNDL008" => "Regenerate valid provenance with the output.", "SNDL009" => "Include the linked output in the export manifest.", _ => "Use the supported generated-sound linkage schema." };
    private static async Task WriteReviewPackAsync(string directory, string status)
    {
        var files = Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly).Where(File.Exists).Where(path => !Path.GetFileName(path).Equals("manifest.json", StringComparison.Ordinal) && !Path.GetFileName(path).Equals("index.md", StringComparison.Ordinal)).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        var members = files.Select(path => new { path = Path.GetFileName(path), size = new FileInfo(path).Length, sha256 = FileHash(path), required = true }).ToArray();
        await File.WriteAllTextAsync(Path.Combine(directory, "manifest.json"), JsonSerializer.Serialize(new { schema = "agentic2d.generated-sound-review-pack.v1", status, members, omissions = new[] { "No audio playback capture is required; cue inventory and hashes are structural review evidence." }, fingerprint = Hash(members) }, Json));
        await File.WriteAllTextAsync(Path.Combine(directory, "index.md"), "# Generated Sound Review Pack\n\nStatus: `" + status + "`\n\n" + string.Join("\n", members.Select(member => "- `" + member.path + "` — " + member.size + " bytes")) + "\n");
    }
    private static bool Property(JsonElement element, string camel, string pascal, out JsonElement value) => element.TryGetProperty(camel, out value) || element.TryGetProperty(pascal, out value);
    private static string FileHash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
    private static string Hash(object value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Json)))).ToLowerInvariant();
    private sealed record LinkageDiagnostic(string Id, string Severity, string Message, string Target, string Field);
    private sealed record LinkageEntry(string SynthesisId, string OutputPath, string ProvenancePath, string SoundDefinitionId, string VariantId, string RuntimeAuthority, string GenerationAuthority);
}
