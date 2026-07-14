using System.Text.Json;
using Agentic2D.Input;
using Agentic2D.ScenarioRunner;
using Agentic2D.Validation;

namespace Agentic2D.Tools;

internal static class M016InputCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length >= 3 && args[0] == "content" && args[1] == "validate" && args[2] is "input-maps" or "input-sequences") return await ContentAsync(args, output, error);
        if (args.Length >= 3 && args[0] == "input" && args[1] == "inspect") return await InspectAsync(args, output, error);
        if (args.Length >= 2 && args[0] == "input" && args[1] == "replay") return await ReplayAsync(args, output, error);
        return -1;
    }
    private static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private static async Task<int> ContentAsync(string[] args, TextWriter output, TextWriter error)
    {
        var target = args[2]; var dir = Option(args, "--output"); if (dir is null) { await error.WriteLineAsync("missing required --output <directory>"); return 2; }
        var root = ContentTargetResolver.FindRepositoryRoot(); var diagnostics = new List<object>();
        if (target == "input-maps") { var map = M016InputScenarioExecutor.LoadMap(); diagnostics.AddRange(InputMapValidator.Validate(map).Diagnostics); }
        else { foreach (var p in Directory.EnumerateFiles(Path.Combine(root, "game/input/sequences"), "*.json").Order(StringComparer.Ordinal)) { using var d = JsonDocument.Parse(File.ReadAllText(p)); if (!d.RootElement.TryGetProperty("schema", out _) || !d.RootElement.TryGetProperty("id", out _)) diagnostics.Add(new { id = "INPUTSEQ0001", severity = "error", message = "Input sequence schema or ID is missing.", target = p }); } }
        var failed = diagnostics.Any(x => JsonSerializer.Serialize(x).Contains("\"error\"", StringComparison.Ordinal)); Directory.CreateDirectory(dir); await File.WriteAllTextAsync(Path.Combine(dir, "result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.content-validation.result.v1", command = "content validate", scope = target, status = failed ? "failed" : "passed", exitCode = failed ? 1 : 0 }, new JsonSerializerOptions { WriteIndented = true })); await File.WriteAllTextAsync(Path.Combine(dir, "diagnostics.json"), JsonSerializer.Serialize(new { diagnostics }, new JsonSerializerOptions { WriteIndented = true })); await File.WriteAllTextAsync(Path.Combine(dir, "validated-items.json"), JsonSerializer.Serialize(new { schema = "agentic2d.content-validation.items.v1", items = Array.Empty<object>() }, new JsonSerializerOptions { WriteIndented = true })); await output.WriteLineAsync($"content validate: {(failed ? "failed" : "passed")}; result: {Path.Combine(dir, "result.json")}"); return failed ? 1 : 0;
    }
    private static async Task<int> InspectAsync(string[] args, TextWriter output, TextWriter error)
    {
        var dir = Option(args, "--output"); if (dir is null) { await error.WriteLineAsync("missing required --output <directory>"); return 2; }
        var scenario = SyntheticScenario("input.runtime-approach-and-interact-smoke", 3); var execution = M016InputScenarioExecutor.Execute(scenario, true); await M016InputArtifactWriter.WriteAsync(dir, execution, M016InputScenarioExecutor.LoadMap()); await output.WriteLineAsync("input inspect: passed; output: " + dir); return 0;
    }
    private static ScenarioSource SyntheticScenario(string id, int ticks) => new() { Id = id, Category = "smoke", Runtime = new ScenarioRuntimeSource(ticks, "spatial.continuous-kinematic-2d", "map.interaction-smoke", 16), Assertions = [new ScenarioAssertionSource("assert.input", "eventOccurred", EventType: "interaction.started")] };
    private static async Task<int> ReplayAsync(string[] args, TextWriter output, TextWriter error)
    {
        var scenarioId = Option(args, "--scenario") ?? "input.runtime-approach-and-interact-smoke"; var recordingPath = Option(args, "--recording"); var dir = Option(args, "--output"); if (recordingPath is null || dir is null) { await error.WriteLineAsync("input replay requires --recording and --output"); return 2; }
        var scenario = SyntheticScenario(scenarioId, 3);
        var recording = JsonSerializer.Deserialize<InputRecording>(File.ReadAllText(recordingPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        var execution = M016InputScenarioExecutor.Execute(scenario, suppliedFrames: recording.Frames);
        var compatibility = SemanticReplay.CheckCompatibility(execution.Recording.Compatibility, recording);
        var evidenceMismatches = SemanticReplay.CompareEvidence(recording.Evidence, execution.Recording.Evidence);
        var mismatches = compatibility.Mismatches.Concat(evidenceMismatches).Distinct(StringComparer.Ordinal).ToArray();
        var categories = new[] { "consumed-input-frames", "behavior-intents", "movement-resolutions", "interaction-resolutions", "commands", "events", "final-component-state", "assertions", "render-projection" };
        var result = new InputReplayResult(mismatches.Length == 0, mismatches, categories);
        await M016InputArtifactWriter.WriteAsync(dir, execution, M016InputScenarioExecutor.LoadMap(), result);
        await output.WriteLineAsync("input replay: " + (result.Accepted ? "passed" : "failed") + "; output: " + dir);
        return result.Accepted ? 0 : 1;
    }
}
