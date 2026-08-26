using System.Text.Json;
using Agentic2D.Persistence;
using Agentic2D.Simulation;

namespace Agentic2D.Tools;

internal static class M043ProductSaveCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args is ["save", "create", ..]) return await Create(args, output, error);
        if (args is ["save", "inspect", ..]) return await Inspect(args, output, error);
        if (args is ["save", "validate", ..]) return await Validate(args, output, error);
        if (args is ["save", "compare", ..]) return await Compare(args, output, error);
        if (args is ["save", "recover", ..]) return await Recover(args, output, error);
        if (args is ["save", "continue", ..]) return await Continue(args, output, error);
        if (args is ["save", "migrate", ..]) { await error.WriteLineAsync("SAVE0439: no supported migration path; current durable format is agentic2d.game-save.v1"); return 1; }
        return -1;
    }

    private static async Task<int> Create(string[] args, TextWriter output, TextWriter error)
    {
        var destination = Option(args, "--output"); var id = Option(args, "--save-id") ?? "save.product"; var project = Option(args, "--project") ?? "project.product";
        if (destination is null) { await error.WriteLineAsync("save create requires --output"); return 2; }
        var world = M032AutonomousDetailedRegion.CreateInitial(); var entries = Content(world, project); var service = new CanonicalRuntimePersistenceService(); var envelope = service.Capture(world, id, project, "world.standard", "config.product", entries); var path = Path.Combine(destination, "canonical-save.json"); service.WriteAtomic(path, envelope, project, "world.standard", "config.product", entries); await output.WriteLineAsync("save create: passed; output: " + path); return 0;
    }

    private static async Task<int> Inspect(string[] args, TextWriter output, TextWriter error)
    {
        var path = args.ElementAtOrDefault(2); var destination = Option(args, "--output"); if (path is null || destination is null || !File.Exists(path)) { await error.WriteLineAsync("save inspect requires an existing save path and --output"); return 2; }
        try { var envelope = JsonSerializer.Deserialize<CanonicalGameSaveEnvelope>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }); if (envelope is null) return 1; Directory.CreateDirectory(destination); await File.WriteAllTextAsync(Path.Combine(destination, "save-inspection.json"), JsonSerializer.Serialize(new { schema = "agentic2d.save-inspection.v2", envelope.Schema, envelope.SaveId, envelope.WorldId, envelope.WorldPayloadSchema, envelope.CanonicalSaveFingerprint }, new JsonSerializerOptions { WriteIndented = true })); await output.WriteLineAsync("save inspect: passed; output: " + destination); return 0; } catch (Exception exception) { await error.WriteLineAsync("save inspect: " + exception.Message); return 1; }
    }

    private static async Task<int> Validate(string[] args, TextWriter output, TextWriter error)
    {
        var path = args.ElementAtOrDefault(2); var destination = Option(args, "--output"); var project = Option(args, "--project") ?? "project.product"; if (path is null || destination is null || !File.Exists(path)) { await error.WriteLineAsync("save validate requires save path and --output"); return 2; }
        var world = M032AutonomousDetailedRegion.CreateInitial(); var result = new CanonicalRuntimePersistenceService().ValidateFile(path, project, "world.standard", "config.product", Content(world, project)); Directory.CreateDirectory(destination); await File.WriteAllTextAsync(Path.Combine(destination, "save-validation.json"), JsonSerializer.Serialize(new { schema = "agentic2d.save-validation.v2", status = result.Success ? "passed" : "failed", diagnostics = result.Diagnostics }, new JsonSerializerOptions { WriteIndented = true })); await output.WriteLineAsync("save validate: " + (result.Success ? "passed" : "failed")); return result.Success ? 0 : 1;
    }

    private static async Task<int> Compare(string[] args, TextWriter output, TextWriter error) { var a = args.ElementAtOrDefault(2); var b = args.ElementAtOrDefault(3); var destination = Option(args, "--output"); if (a is null || b is null || destination is null) { await error.WriteLineAsync("save compare requires two save paths and --output"); return 2; } var equal = File.Exists(a) && File.Exists(b) && File.ReadAllText(a) == File.ReadAllText(b); Directory.CreateDirectory(destination); await File.WriteAllTextAsync(Path.Combine(destination, "save-comparison.json"), JsonSerializer.Serialize(new { schema = "agentic2d.save-comparison.v1", equal }, new JsonSerializerOptions { WriteIndented = true })); await output.WriteLineAsync("save compare: passed"); return 0; }
    private static async Task<int> Recover(string[] args, TextWriter output, TextWriter error) { var path = args.ElementAtOrDefault(2); var previous = Option(args, "--previous-good"); var destination = Option(args, "--output"); if (path is null || previous is null || destination is null) { await error.WriteLineAsync("save recover requires save path, --previous-good, and --output"); return 2; } var world = M032AutonomousDetailedRegion.CreateInitial(); var result = new CanonicalRuntimePersistenceService().Recover(path, previous, M032AutonomousDetailedRegion.Registrations(), "project.product", "world.standard", "config.product", Content(world, "project.product")); Directory.CreateDirectory(destination); await File.WriteAllTextAsync(Path.Combine(destination, "save-recovery.json"), JsonSerializer.Serialize(new { schema = "agentic2d.save-recovery.v1", status = result.Success ? "passed" : "failed", diagnostics = result.Diagnostics }, new JsonSerializerOptions { WriteIndented = true })); await output.WriteLineAsync("save recover: " + (result.Success ? "passed" : "failed")); return result.Success ? 0 : 1; }
    private static async Task<int> Continue(string[] args, TextWriter output, TextWriter error)
    {
        var path = args.ElementAtOrDefault(2); var destination = Option(args, "--output"); var project = Option(args, "--project") ?? "project.product";
        if (path is null || destination is null) { await error.WriteLineAsync("save continue requires a save path and --output"); return 2; }
        var initial = M032AutonomousDetailedRegion.CreateInitial(); var entries = Content(initial, project); var service = new CanonicalRuntimePersistenceService(); var loaded = service.LoadFresh(path, M032AutonomousDetailedRegion.Registrations(), project, "world.standard", "config.product", entries);
        if (!loaded.Success || loaded.World is null) { await error.WriteLineAsync("save continue: " + string.Join("; ", loaded.Diagnostics)); return 1; }
        var before = loaded.World.Clock.Now.Microseconds; loaded.World.Advance(new SimulationDuration(1_000_000)); var envelope = service.Capture(loaded.World, loaded.Envelope!.SaveId + ".continued", project, "world.standard", "config.product", Content(loaded.World, project)); Directory.CreateDirectory(destination); service.WriteAtomic(Path.Combine(destination, "continued-canonical-save.json"), envelope, project, "world.standard", "config.product", Content(loaded.World, project)); await File.WriteAllTextAsync(Path.Combine(destination, "save-continue.json"), JsonSerializer.Serialize(new { schema = "agentic2d.save-continue.v1", status = "passed", advanced = loaded.World.Clock.Now.Microseconds > before, sourceFingerprint = loaded.Envelope.CanonicalSaveFingerprint, continuedFingerprint = envelope.CanonicalSaveFingerprint }, new JsonSerializerOptions { WriteIndented = true })); await output.WriteLineAsync("save continue: passed"); return 0;
    }
    private static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
    private static IReadOnlyList<SemanticContentEntry> Content(SimulationWorld world, string project) => CanonicalRuntimePersistenceService.ResolveSemanticContent(world, project, "world.standard", "config.product");
}
