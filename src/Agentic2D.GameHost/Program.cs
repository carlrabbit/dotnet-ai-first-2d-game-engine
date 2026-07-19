using System.Security.Cryptography;
using System.Text.Json;
using Agentic2D.Engine;
using Agentic2D.Metrics;
using Agentic2D.Input;
using Agentic2D.ScenarioRunner;
using Agentic2D.Validation;
using Agentic2D.DebugClient.Raylib;
using Agentic2D.Tools;

var parsed = HostOptions.Parse(args);
if (parsed.Error is not null) return Fail(parsed.Error, 2);
if (parsed.Help) { Console.WriteLine(HostOptions.Usage); return 0; }
if (parsed.Version) { Console.WriteLine("agentic2d-game " + (typeof(Program).Assembly.GetName().Version?.ToString() ?? "0.0.0")); return 0; }
try
{
    var root = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
    var manifestPath = Path.Combine(root, "agentic2d.export.json");
    var manifest = ExportManifest.Load(manifestPath);
    manifest.Validate(root);
    var recordingPath = parsed.Recording is null ? null : Path.GetFullPath(parsed.Recording);
    var output = parsed.Output ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), manifest.GameId, "logs-or-artifacts", "run-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(output);
    Directory.SetCurrentDirectory(root);
    var scenario = parsed.Scenario ?? manifest.StartupScenario;
    var scenarioPath = Directory.EnumerateFiles(Path.Combine(root, manifest.ContentRoot, "scenarios"), "*.json", SearchOption.AllDirectories)
        .FirstOrDefault(path => JsonDocument.Parse(File.ReadAllText(path)).RootElement.GetProperty("id").GetString() == scenario)
        ?? throw new InvalidOperationException("HOST0006: scenario was not found in bundled content: " + scenario);
    var validation = new ContentValidator().Validate(scenarioPath);
    if (validation.Result.ExitCode != 0) throw new InvalidOperationException("HOST0007: bundled scenario validation failed.");
    using var scenarioDocument = JsonDocument.Parse(File.ReadAllText(scenarioPath));
    if (!parsed.Headless && scenarioDocument.RootElement.TryGetProperty("playable", out var playable))
    {
        RaylibGameWindow.ShowPlayableContent(manifest.DisplayName, playable, output, parsed.AutoCloseAfterFrames, parsed.Capture);
        await File.WriteAllTextAsync(Path.Combine(output, "run-manifest.json"), JsonSerializer.Serialize(new { schema = "agentic2d.exported-game-run.v1", status = "passed", scenarioId = scenario, mode = "interactive", artifactRoot = output }, ExportManifest.Json));
        await File.WriteAllTextAsync(Path.Combine(output, "startup-diagnostics.json"), JsonSerializer.Serialize(new { schema = "agentic2d.exported-game-diagnostics.v1", status = "passed", mode = "interactive", graphicalAdapter = "raylib-isolated-adapter" }, ExportManifest.Json));
        return 0;
    }
    if (scenario == "presentation.persistent-world-player-facing-smoke")
    {
        var presentationExit = await M021PresentationCommands.RunAsync(["project", "run", ".", "--scenario", scenario, "--output", output], Console.Out, Console.Error);
        if (presentationExit < 0) throw new InvalidOperationException("HOST0011: player-facing presentation host was unavailable.");
        if (parsed.Metrics != MetricsCollectionMode.Off)
        {
            var presentationMetrics = RuntimeSmokeScenario.RunWithMetrics(6, parsed.Metrics);
            await File.WriteAllTextAsync(Path.Combine(output, "metrics-summary.json"), JsonSerializer.Serialize(new { schema = "agentic2d.runtime-metrics-summary.v1", mode = parsed.Metrics.ToString().ToLowerInvariant(), tickCount = presentationMetrics.TickCount, metrics = presentationMetrics.Summary }, ExportManifest.Json));
        }
        await File.WriteAllTextAsync(Path.Combine(output, "startup-diagnostics.json"), JsonSerializer.Serialize(new { schema = "agentic2d.exported-game-diagnostics.v1", status = presentationExit == 0 ? "passed" : "failed", mode = parsed.Headless ? "headless" : "graphical", graphicalAdapter = parsed.Headless ? "not-requested" : "raylib-isolated-adapter", playerFacingPresentation = "m021-authoritative" }, ExportManifest.Json));
        if (!parsed.Headless && presentationExit == 0) RaylibGameWindow.Show(manifest.DisplayName, scenario, 6, parsed.AutoCloseAfterFrames);
        return presentationExit;
    }
    var execution = new ScenarioRunner().Run(scenarioPath);
    InputRecording? recording = null;
    if (recordingPath is not null)
    {
        if (!File.Exists(recordingPath)) throw new InvalidOperationException("HOST0008: semantic input recording was not found.");
        recording = JsonSerializer.Deserialize<InputRecording>(File.ReadAllText(recordingPath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException("HOST0009: semantic input recording is malformed.");
        if (recording.Schema != "agentic2d.input-recording.v1" || recording.Frames.Count == 0) throw new InvalidOperationException("HOST0010: semantic input recording is incompatible or empty.");
        var inputDirectory = Path.Combine(output, "input"); Directory.CreateDirectory(inputDirectory);
        await File.WriteAllLinesAsync(Path.Combine(inputDirectory, "consumed-semantic-input.jsonl"), recording.Frames.OrderBy(frame => frame.Tick).Select(frame => JsonSerializer.Serialize(frame, ExportManifest.Json)));
        await File.WriteAllTextAsync(Path.Combine(inputDirectory, "semantic-input-consumption.json"), JsonSerializer.Serialize(new { schema = "agentic2d.exported-game-semantic-input.v1", status = "consumed", recordingSchema = recording.Schema, frameCount = recording.Frames.Count, compatibility = recording.Compatibility }, ExportManifest.Json));
    }
    var runtimeDirectory = Path.Combine(output, "runtime");
    Directory.CreateDirectory(runtimeDirectory);
    var exit = await ScenarioArtifactWriter.WriteAsync(runtimeDirectory, execution);
    if (parsed.Metrics != MetricsCollectionMode.Off)
    {
        var metrics = RuntimeSmokeScenario.RunWithMetrics(Math.Max(1, execution.Result.Runtime.FinalTick), parsed.Metrics);
        await File.WriteAllTextAsync(Path.Combine(output, "metrics-summary.json"), JsonSerializer.Serialize(new { schema = "agentic2d.runtime-metrics-summary.v1", mode = parsed.Metrics.ToString().ToLowerInvariant(), tickCount = metrics.TickCount, metrics = metrics.Summary }, ExportManifest.Json));
    }
    await File.WriteAllTextAsync(Path.Combine(output, "run-manifest.json"), JsonSerializer.Serialize(new { schema = "agentic2d.exported-game-run.v1", status = exit == 0 ? "passed" : "failed", scenarioId = scenario, projectFingerprint = manifest.ProjectFingerprint, contentFingerprint = manifest.ContentFingerprint, headless = parsed.Headless, recording = parsed.Recording, semanticInputConsumed = recording is not null, ticks = parsed.Ticks, writableSaveRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), manifest.GameId, "saves"), artifactRoot = output }, ExportManifest.Json));
    await File.WriteAllTextAsync(Path.Combine(output, "startup-diagnostics.json"), JsonSerializer.Serialize(new { schema = "agentic2d.exported-game-diagnostics.v1", status = exit == 0 ? "passed" : "failed", mode = parsed.Headless ? "headless" : "graphical", graphicalAdapter = parsed.Headless ? "not-requested" : "raylib-isolated-adapter" }, ExportManifest.Json));
    if (!parsed.Headless && exit == 0) RaylibGameWindow.Show(manifest.DisplayName, scenario, execution.Result.Runtime.FinalTick, parsed.AutoCloseAfterFrames);
    return exit;
}
catch (Exception exception) { return Fail(exception.Message, 1); }

static int Fail(string message, int code) { Console.Error.WriteLine(message); return code; }

file sealed record HostOptions(bool Headless, string? Scenario, string? Recording, string? Ticks, MetricsCollectionMode Metrics, string? Output, int? AutoCloseAfterFrames, string? Capture, bool Help, bool Version, string? Error)
{
    public const string Usage = "agentic2d-game [--headless] [--scenario <id>] [--recording <path>] [--ticks <count-or-final>] [--metrics off|summary|per-tick] [--output <path>] [--auto-close-after <frames>] [--capture <png>] [--help] [--version]";
    public static HostOptions Parse(string[] args)
    {
        var headless = false; string? scenario = null, recording = null, ticks = null, output = null, capture = null; int? autoCloseAfterFrames = null; var metrics = MetricsCollectionMode.Off;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--headless": headless = true; break;
                case "--help": return new(false, null, null, null, metrics, null, null, null, true, false, null);
                case "--version": return new(false, null, null, null, metrics, null, null, null, false, true, null);
                case "--scenario": if (++i >= args.Length) return Invalid("missing value for --scenario"); scenario = args[i]; break;
                case "--recording": if (++i >= args.Length) return Invalid("missing value for --recording"); recording = args[i]; break;
                case "--ticks": if (++i >= args.Length || (args[i] != "final" && (!int.TryParse(args[i], out var count) || count < 0))) return Invalid("--ticks must be a non-negative count or final"); ticks = args[i]; break;
                case "--metrics": if (++i >= args.Length || !TryMetrics(args[i], out metrics)) return Invalid("--metrics must be off, summary, or per-tick"); break;
                case "--output": if (++i >= args.Length) return Invalid("missing value for --output"); output = args[i]; break;
                case "--auto-close-after": if (++i >= args.Length || !int.TryParse(args[i], out var frames) || frames <= 0) return Invalid("--auto-close-after must be a positive frame count"); autoCloseAfterFrames = frames; break;
                case "--capture": if (++i >= args.Length || Path.IsPathRooted(args[i]) || args[i].Contains("..", StringComparison.Ordinal)) return Invalid("--capture must be a safe relative path"); capture = args[i]; break;
                default: return Invalid("unsupported option: " + args[i]);
            }
        }
        return new(headless, scenario, recording, ticks, metrics, output, autoCloseAfterFrames, capture, false, false, null);
    }
    private static HostOptions Invalid(string value) => new(false, null, null, null, MetricsCollectionMode.Off, null, null, null, false, false, value);
    private static bool TryMetrics(string value, out MetricsCollectionMode mode) { mode = value switch { "off" => MetricsCollectionMode.Off, "summary" => MetricsCollectionMode.Summary, "per-tick" => MetricsCollectionMode.PerTick, _ => MetricsCollectionMode.Off }; return value is "off" or "summary" or "per-tick"; }
}

file sealed record ExportManifest(string Schema, string ExportId, string GameId, string DisplayName, string TargetRid, string Executable, string ContentRoot, string StartupScenario, string DefaultMetrics, string DefaultMode, string SaveRootPolicy, string ArtifactRootPolicy, string EngineFingerprint, string ProjectFingerprint, string ContentFingerprint, string FileManifest, string ExportFingerprint)
{
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    public static ExportManifest Load(string path) => JsonSerializer.Deserialize<ExportManifest>(File.ReadAllText(path), Json) ?? throw new InvalidOperationException("HOST0001: startup manifest is malformed.");
    public void Validate(string root)
    {
        if (Schema != "agentic2d.standalone-linux-export.v1" || TargetRid != "linux-x64" || Path.IsPathRooted(ContentRoot) || ContentRoot.Contains("..", StringComparison.Ordinal)) throw new InvalidOperationException("HOST0002: startup manifest is invalid.");
        var inventory = Path.Combine(root, FileManifest); if (!File.Exists(inventory)) throw new InvalidOperationException("HOST0003: file inventory is missing.");
        foreach (var item in JsonDocument.Parse(File.ReadAllText(inventory)).RootElement.GetProperty("files").EnumerateArray())
        {
            var relative = item.GetProperty("path").GetString()!; if (Path.IsPathRooted(relative) || relative.Contains("..", StringComparison.Ordinal)) throw new InvalidOperationException("HOST0004: inventory contains an unsafe path.");
            var file = Path.Combine(root, relative); if (!File.Exists(file)) throw new InvalidOperationException("HOST0005: required bundled file is missing: " + relative);
            if (!string.Equals(Hash(file), item.GetProperty("sha256").GetString(), StringComparison.Ordinal)) throw new InvalidOperationException("HOST0005: bundled file hash differs: " + relative);
        }
    }
    private static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant();
}
