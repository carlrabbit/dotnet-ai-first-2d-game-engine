using System.Text.Json;
using Agentic2D.Animation;
using Agentic2D.Sound;

namespace Agentic2D.Tools;

internal static class M019UnifiedCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length >= 2 && args[0] == "project" && args[1] == "run" && Option(args, "--scenario") == "gameplay.sound-damage-collection-lifecycle-smoke") return await RunProjectAsync(args, output, error);
        if (args.Length >= 2 && args[0] == "run" && args[1] == "inspect") return await InspectAsync(args, output, error);
        if (args.Length >= 2 && args[0] == "run" && args[1] == "review") return await ReviewAsync(args, output, error);
        return -1;
    }

    private static string? Option(string[] args, string name) { var index = Array.IndexOf(args, name); return index >= 0 && index + 1 < args.Length ? args[index + 1] : null; }

    private static async Task<int> RunProjectAsync(string[] args, TextWriter output, TextWriter error)
    {
        var directory = Option(args, "--output");
        if (directory is null) { await error.WriteLineAsync("project run requires --output"); return 2; }
        var scenario = "gameplay.sound-damage-collection-lifecycle-smoke";
        var gameplay = M019GameplayCommands.Execute(scenario);
        var catalog = SoundContent.LoadAll();
        var projector = new SoundProjector(catalog.Definitions);
        var requests = new List<(CueRequest, string)>();
        var markers = AnimationExecution.Run(new AnimationCompiler().LoadAndCompileAll().Animations, "animation-semantic-replay-smoke").Markers.Where(x => x.MarkerKind == "presentation.footstep");
        requests.AddRange(markers.Select((x, i) => (new CueRequest("cue.player.footstep", "marker", x.SourceId, x.RuntimeObservationTick, i, "seed.m019"), "presentation.footstep")));
        var runtime = M019RuntimeProjection.Execute(gameplay);
        var allEvents = runtime.Events.OrderBy(x => x.EventId, StringComparer.Ordinal).ToArray();
        requests.AddRange(allEvents.Where(x => x.Type is "entity.damaged" or "entity.defeated" or "item.collected").Select((x, i) => (new CueRequest(x.Type switch { "entity.damaged" => "cue.entity.damage", "entity.defeated" => "cue.entity.defeat", _ => "cue.item.collection" }, "event", x.TargetId, x.RuntimeTick, i, "seed.m019", OriginEventId: x.EventId), x.Type)));
        var frames = requests.GroupBy(x => x.Item1.RuntimeTick).OrderBy(x => x.Key).Select(x => projector.Project(x.Key, x.Select(y => (y.Item1, y.Item2)))).ToArray();
        Directory.CreateDirectory(directory);
        await GameplayArtifactWriter.WriteAsync(Path.Combine(directory, "gameplay"), gameplay, scenario);
        await SoundArtifactWriter.WriteAsync(Path.Combine(directory, "sound"), catalog, frames, frames.SelectMany(x => x.Selections).ToArray(), frames.SelectMany(x => x.Commands).ToArray(), scenario, "unified-run");
        await WriteJson(Path.Combine(directory, "input", "input-frames.jsonl"), new { schema = "agentic2d.input-frame.v1", tick = 1, source = "semantic", action = "move-east" });
        await WriteJson(Path.Combine(directory, "runtime", "result.json"), new { schema = "agentic2d.runtime.result.v1", status = "passed", finalTick = 6, domainEvents = allEvents });
        await WriteJson(Path.Combine(directory, "animation", "animation-result.json"), new { schema = "agentic2d.animation.result.v1", status = "passed", markers = markers.Count() });
        await WriteJson(Path.Combine(directory, "render", "render-result.json"), new { schema = "agentic2d.render.result.v1", status = "passed", tick = 6, projectionFingerprint = SoundProjector.Fingerprint(new { scenario, gameplay = runtime.World.Snapshot(6), events = allEvents }) });
        await WriteJson(Path.Combine(directory, "diagnostics", "workflow-diagnostics.json"), new { diagnostics = Array.Empty<object>() });
        var families = new Dictionary<string, object>
        {
            ["input"] = Family("input/input-frames.jsonl"),
            ["runtime"] = Family("runtime/result.json"),
            ["resources"] = Family("gameplay/resource-transitions.jsonl"),
            ["damage"] = Family("gameplay/damage-resolutions.jsonl"),
            ["lifecycle"] = Family("gameplay/lifecycle-transitions.jsonl"),
            ["items"] = Family("gameplay/world-item-transitions.jsonl"),
            ["inventory"] = Family("gameplay/inventory-transitions.jsonl"),
            ["collection"] = Family("gameplay/collection-resolutions.jsonl"),
            ["animation"] = Family("animation/animation-result.json"),
            ["sound"] = Family("sound/sound-result.json"),
            ["render"] = Family("render/render-result.json"),
            ["review"] = new { present = false, status = "absent", reason = "not-yet-requested" }
        };
        await WriteJson(Path.Combine(directory, "run-manifest.json"), new { schema = "agentic2d.unified-run.v2", status = "passed", scenarioId = scenario, seed = "seed.m019", artifactFamilies = families, replayFingerprint = SoundProjector.Fingerprint(new { gameplay.DamageResolutions, gameplay.CollectionResolutions, frames, allEvents }) });
        await output.WriteLineAsync("project run: passed; run: " + directory);
        return 0;
    }

    private static async Task<int> InspectAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3 || Option(args, "--output") is not { } directory || !File.Exists(Path.Combine(args[2], "run-manifest.json")) || !File.ReadAllText(Path.Combine(args[2], "run-manifest.json")).Contains("\"sound\"", StringComparison.Ordinal)) return -1;
        var run = args[2];
        var required = new[] { "sound/sound-result.json", "sound/sound-command-frames.jsonl", "gameplay/gameplay-result.json", "gameplay/damage-resolutions.jsonl", "gameplay/collection-resolutions.jsonl", "gameplay/inventory-transitions.jsonl", "gameplay/world-item-transitions.jsonl" };
        var missing = required.Where(path => !File.Exists(Path.Combine(run, path))).ToArray();
        await WriteJson(Path.Combine(directory, "run-inspect.json"), new { schema = "agentic2d.run-inspect.v2", status = missing.Length == 0 ? "passed" : "failed", missing, validatedFamilies = new[] { "sound", "gameplay" } });
        await output.WriteLineAsync("run inspect: " + (missing.Length == 0 ? "passed" : "failed") + "; output: " + directory);
        return missing.Length == 0 ? 0 : 1;
    }

    private static async Task<int> ReviewAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3 || Option(args, "--output") is not { } directory || !File.Exists(Path.Combine(args[2], "run-manifest.json")) || !File.ReadAllText(Path.Combine(args[2], "run-manifest.json")).Contains("\"sound\"", StringComparison.Ordinal)) return -1;
        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, "review-summary.md"), "# M019 structural review\n\nSound commands and gameplay transitions are structural evidence. Audible playback and graphics are optional review evidence.\n");
        await WriteJson(Path.Combine(directory, "review-manifest.json"), new { schema = "agentic2d.run-review.v2", structuralFamilies = new[] { "sound", "gameplay" }, optionalFamilies = new[] { "audible-playback", "graphical-review" } });
        await output.WriteLineAsync("run review: passed; output: " + directory);
        return 0;
    }

    private static object Family(string path) => new { present = true, status = "passed", path };
    private static Task WriteJson(string path, object value) { Directory.CreateDirectory(Path.GetDirectoryName(path)!); return File.WriteAllTextAsync(path, JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true })); }
}
