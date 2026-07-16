using System.Text.Json;
using Agentic2D.Animation;
using Agentic2D.Sound;

namespace Agentic2D.Tools;

internal static class M019SoundCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length >= 3 && args[0] == "content" && args[1] == "validate" && args[2] == "sounds") return await ValidateAsync(args, output, error);
        if (args.Length >= 2 && args[0] == "sound" && args[1] == "inspect") return await InspectAsync(args, output, error);
        if (args.Length >= 2 && args[0] == "sound" && args[1] == "project") return await ProjectAsync(args, output, error);
        return -1;
    }

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static async Task<int> ValidateAsync(string[] args, TextWriter output, TextWriter error)
    {
        var directory = Option(args, "--output");
        if (directory is null) { await error.WriteLineAsync("missing required --output <directory>"); return 2; }
        var catalog = SoundContent.LoadAll();
        await WriteContentArtifacts(directory, catalog);
        await output.WriteLineAsync($"content validate: {(catalog.Passed ? "passed" : "failed")}; result: {Path.Combine(directory, "result.json")}");
        return catalog.Passed ? 0 : 1;
    }

    private static async Task<int> InspectAsync(string[] args, TextWriter output, TextWriter error)
    {
        var target = args.Length > 2 ? args[2] : null;
        var directory = Option(args, "--output");
        if (target is null || directory is null) { await error.WriteLineAsync("sound inspect requires <sound-id-or-path> and --output"); return 2; }
        var catalog = SoundContent.LoadAll();
        var definition = catalog.Definitions.SingleOrDefault(x => x.Id == target || target.EndsWith(x.Id + ".json", StringComparison.Ordinal));
        if (definition is null || !catalog.Passed) { await error.WriteLineAsync("sound definition was not found or failed validation"); return 1; }
        await SoundArtifactWriter.WriteAsync(directory, catalog, [], [], [], "inspect", definition.Id);
        await output.WriteLineAsync("sound inspect: passed; output: " + directory);
        return 0;
    }

    private static async Task<int> ProjectAsync(string[] args, TextWriter output, TextWriter error)
    {
        var scenario = Option(args, "--scenario");
        var directory = Option(args, "--output");
        if (scenario is null || directory is null || Option(args, "--project") is null) { await error.WriteLineAsync("sound project requires --project, --scenario, and --output"); return 2; }
        var catalog = SoundContent.LoadAll();
        if (!catalog.Passed) { await error.WriteLineAsync("sound content validation failed"); return 1; }
        var projector = new SoundProjector(catalog.Definitions);
        var frames = new List<SoundCommandFrame>();
        if (scenario is "sound.marker-cue-smoke" or "gameplay.sound-damage-collection-lifecycle-smoke")
        {
            var markers = AnimationMarkers();
            frames.Add(projector.Project(1, markers.Select((x, index) => (new CueRequest("cue.player.footstep", "marker", x.SourceId, x.RuntimeObservationTick, index, "seed.m019"), "presentation.footstep"))));
        }
        if (scenario == "sound.loop-ownership-smoke")
        {
            frames.Add(projector.Project(1, [], [new SoundCommand("loop.001", "StartLoop", 1, "cue.ambient.loop-smoke", LoopInstanceKey: "loop.smoke")]));
            frames.Add(projector.Project(2, [], [new SoundCommand("loop.002", "StartLoop", 2, "cue.ambient.loop-smoke", LoopInstanceKey: "loop.smoke")]));
            frames.Add(projector.Project(3, [], [new SoundCommand("loop.003", "ReplaceLoop", 3, "cue.ambient.loop-smoke", LoopInstanceKey: "loop.smoke")]));
            frames.Add(projector.Project(4, [], [new SoundCommand("loop.004", "StopLoop", 4, LoopInstanceKey: "loop.smoke")]));
            frames.Add(projector.Project(5, [], [new SoundCommand("loop.005", "StopLoop", 5, LoopInstanceKey: "loop.smoke")]));
        }
        if (frames.Count == 0) frames.Add(projector.Project(0, []));
        await SoundArtifactWriter.WriteAsync(directory, catalog, frames, frames.SelectMany(x => x.Selections).ToArray(), frames.SelectMany(x => x.Commands).ToArray(), scenario, "sound-project");
        await output.WriteLineAsync("sound project: passed; output: " + directory);
        return 0;
    }

    private static IReadOnlyList<PresentationMarkerOccurrence> AnimationMarkers()
    {
        var definitions = new AnimationCompiler().LoadAndCompileAll();
        return AnimationExecution.Run(definitions.Animations, "animation-semantic-replay-smoke").Markers.Where(x => x.MarkerKind == "presentation.footstep").ToArray();
    }

    private static async Task WriteContentArtifacts(string directory, SoundCatalog catalog)
    {
        Directory.CreateDirectory(directory);
        var json = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(Path.Combine(directory, "result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.content-validation.result.v1", command = "content validate", scope = "sounds", status = catalog.Passed ? "passed" : "failed", exitCode = catalog.Passed ? 0 : 1 }, json));
        await File.WriteAllTextAsync(Path.Combine(directory, "diagnostics.json"), JsonSerializer.Serialize(new { diagnostics = catalog.Diagnostics }, json));
        await File.WriteAllTextAsync(Path.Combine(directory, "validated-items.json"), JsonSerializer.Serialize(new { items = catalog.Definitions.Select(x => new { id = x.Id, status = "passed" }) }, json));
    }
}

internal static class SoundArtifactWriter
{
    public static async Task WriteAsync(string directory, SoundCatalog catalog, IReadOnlyList<SoundCommandFrame> frames, IReadOnlyList<SoundCueSelection> selections, IReadOnlyList<SoundCommand> commands, string source, string target)
    {
        Directory.CreateDirectory(directory);
        var json = new JsonSerializerOptions { WriteIndented = true };
        var fingerprint = SoundProjector.Fingerprint(new { catalog.Definitions, frames, selections, commands, source, target });
        await File.WriteAllTextAsync(Path.Combine(directory, "sound-result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.sound.result.v1", status = catalog.Passed ? "passed" : "failed", source, target, fingerprint }, json));
        await File.WriteAllTextAsync(Path.Combine(directory, "sound-definitions.json"), JsonSerializer.Serialize(catalog.Definitions, json));
        await Lines(Path.Combine(directory, "sound-cue-selections.jsonl"), selections);
        await Lines(Path.Combine(directory, "sound-commands.jsonl"), commands);
        await Lines(Path.Combine(directory, "sound-command-frames.jsonl"), frames);
        await Lines(Path.Combine(directory, "sound-playback-state.jsonl"), frames.Select(x => new { x.RuntimeTick, x.LoopState, x.Fingerprint }));
        await File.WriteAllTextAsync(Path.Combine(directory, "sound-diagnostics.json"), JsonSerializer.Serialize(new { schema = "agentic2d.sound.diagnostics.v1", diagnostics = catalog.Diagnostics }, json));
    }

    private static Task Lines<T>(string path, IEnumerable<T> values) => File.WriteAllTextAsync(path, string.Join(Environment.NewLine, values.Select(x => JsonSerializer.Serialize(x))));
}
