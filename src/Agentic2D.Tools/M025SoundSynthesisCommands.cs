using System.Text.Json;
using Agentic2D.Sound;

namespace Agentic2D.Tools;

internal static class M025SoundSynthesisCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length >= 2 && args[0] == "sound" && args[1] == "synthesize") return await Synthesize(args, output, error);
        if (args.Length >= 3 && args[0] == "sound" && args[1] == "synthesis" && args[2] is "validate" or "inspect") return await InspectOrValidate(args, output, error);
        return -1;
    }

    private static async Task<int> Synthesize(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3 || Option(args, "--output") is not { } destination) return await Usage(error, "sound synthesize requires <definition-or-directory> --output <directory>");
        var definitions = Load(args[2]); if (definitions.Count == 0) return await Usage(error, "no synthesis definitions were found");
        var artifacts = definitions.Select(x => OfflineSoundSynthesis.Synthesize(x, destination)).ToArray();
        await Write(destination, artifacts); var passed = artifacts.All(x => x.Diagnostics.All(d => d.Severity != "error")); await output.WriteLineAsync("sound synthesize: " + (passed ? "passed" : "failed") + "; output: " + destination); return passed ? 0 : 1;
    }
    private static async Task<int> InspectOrValidate(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 4 || Option(args, "--output") is not { } destination) return await Usage(error, "sound synthesis validate|inspect requires <definition-or-directory> --output <directory>");
        var definitions = Load(args[3]); var artifacts = definitions.Select(x => new SoundSynthesisArtifact("agentic2d.sound-synthesis-artifact.v1", x.Id, x.OutputAssetId, x.OutputPath, OfflineSoundSynthesis.Fingerprint(x), OfflineSoundSynthesis.ImplementationVersion, 0, x.Segments.FirstOrDefault()?.SampleRate ?? 0, x.Segments.Sum(s => s.DurationSeconds), 0, 0, "", OfflineSoundSynthesis.Validate(x, args[3]))).ToArray();
        await Write(destination, artifacts); var passed = definitions.Count > 0 && artifacts.All(x => x.Diagnostics.All(d => d.Severity != "error")); await output.WriteLineAsync("sound synthesis " + args[2] + ": " + (passed ? "passed" : "failed") + "; output: " + destination); return passed ? 0 : 1;
    }
    private static List<SoundSynthesisDefinition> Load(string target)
    {
        var files = Directory.Exists(target) ? Directory.EnumerateFiles(target, "*.json", SearchOption.AllDirectories) : File.Exists(target) ? [target] : [];
        return files.OrderBy(x => x, StringComparer.Ordinal).Select(path => JsonSerializer.Deserialize<SoundSynthesisDefinition>(File.ReadAllText(path), OfflineSoundSynthesis.Json) ?? new SoundSynthesisDefinition()).ToList();
    }
    private static async Task Write(string destination, IReadOnlyList<SoundSynthesisArtifact> artifacts)
    {
        Directory.CreateDirectory(destination); await File.WriteAllTextAsync(Path.Combine(destination, "sound-synthesis-result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.sound-synthesis-result.v1", status = artifacts.All(a => a.Diagnostics.All(d => d.Severity != "error")) ? "passed" : "failed", artifacts }, OfflineSoundSynthesis.Json)); await File.WriteAllTextAsync(Path.Combine(destination, "sound-synthesis-inventory.json"), JsonSerializer.Serialize(artifacts, OfflineSoundSynthesis.Json));
    }
    private static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private static Task<int> Usage(TextWriter error, string text) { error.WriteLine(text); return Task.FromResult(2); }
}
