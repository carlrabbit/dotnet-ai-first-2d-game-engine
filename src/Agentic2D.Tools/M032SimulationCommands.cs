using System.Diagnostics;
using System.Text.Json;
using Agentic2D.Rendering;
using Agentic2D.Simulation;

namespace Agentic2D.Tools;

internal static class M032SimulationCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 6 && args[0] == "simulation" && args[1] == "detailed-forest-logistics-child" && args[2] == "--save" && args[4] == "--output") return await ContinueInFreshProcessAsync(args[3], args[5], output, error);
        if (args.Length < 2 || args[0] != "simulation" || args[1] != "detailed-forest-logistics") return -1;
        var option = Array.IndexOf(args, "--output");
        if (args.Length != 4 || option != 2 || string.IsNullOrWhiteSpace(args[3]))
        {
            await error.WriteLineAsync("simulation detailed-forest-logistics requires only --output <directory>");
            return 2;
        }
        try
        {
            await M032ArtifactWriter.WriteAsync(args[3]);
            await WriteStructuralFramesAsync(args[3]);
            var savePath = Path.Combine(args[3], "forest-logistics", "roundtrip", "save-while-carrying.json");
            var childDirectory = Path.Combine(args[3], "forest-logistics", "fresh-process");
            var exitCode = await StartFreshProcessAsync(savePath, childDirectory);
            using var child = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(childDirectory, "continuation.json")));
            using var comparison = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(args[3], "forest-logistics", "comparison.json")));
            var expected = comparison.RootElement.GetProperty("roundtripFingerprint").GetString();
            var actual = child.RootElement.GetProperty("fingerprint").GetString();
            if (exitCode != 0 || !string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException("fresh-process continuation fingerprint mismatch");
            await File.WriteAllTextAsync(Path.Combine(args[3], "forest-logistics", "fresh-process.json"), JsonSerializer.Serialize(new { schema = "agentic2d.m032.fresh-process-proof.v1", status = "passed", processExitCode = exitCode, fingerprint = actual }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            await output.WriteLineAsync("simulation detailed-forest-logistics: passed; output: " + args[3]);
            return 0;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or System.Text.Json.JsonException)
        {
            await error.WriteLineAsync("simulation detailed-forest-logistics failed: " + exception.Message);
            return 1;
        }
    }

    private static async Task<int> ContinueInFreshProcessAsync(string savePath, string outputPath, TextWriter output, TextWriter error)
    {
        try
        {
            var save = JsonSerializer.Deserialize<SimulationSave>(await File.ReadAllTextAsync(savePath), new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
            if (save is null) throw new InvalidOperationException("save is malformed");
            var continued = M032AutonomousDetailedRegion.ContinueFromSave(save);
            Directory.CreateDirectory(outputPath);
            await File.WriteAllTextAsync(Path.Combine(outputPath, "continuation.json"), JsonSerializer.Serialize(new { schema = "agentic2d.m032.fresh-process-continuation.v1", status = continued.Diagnostics.Count == 0 ? "passed" : "failed", fingerprint = continued.Fingerprint, diagnostics = continued.Diagnostics }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            await output.WriteLineAsync("simulation detailed-forest-logistics child: passed");
            return continued.Diagnostics.Count == 0 ? 0 : 1;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or JsonException)
        {
            await error.WriteLineAsync("simulation detailed-forest-logistics child failed: " + exception.Message);
            return 1;
        }
    }

    private static async Task<int> StartFreshProcessAsync(string savePath, string outputPath)
    {
        var processPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        var assembly = Path.Combine(AppContext.BaseDirectory, "Agentic2D.Tools.dll");
        using var process = Process.Start(new ProcessStartInfo(processPath, [assembly, "simulation", "detailed-forest-logistics-child", "--save", savePath, "--output", outputPath]) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }) ?? throw new InvalidOperationException("cannot start fresh simulation process");
        await process.StandardOutput.ReadToEndAsync(); var errors = await process.StandardError.ReadToEndAsync(); await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException("fresh simulation process failed: " + errors);
        return process.ExitCode;
    }

    private static async Task WriteStructuralFramesAsync(string root)
    {
        var runs = M032AutonomousDetailedRegion.CreateEvidenceStates();
        var frames = new[] { ("initial", "paused / designation inspection"), ("movement", "walking / route overlay"), ("interruption", "mandatory food interruption"), ("post-load", "route reconstructed after load") };
        var directory = Path.Combine(root, "structural-frames"); Directory.CreateDirectory(directory);
        foreach (var (id, overlay) in frames)
        {
            var frame = M032DetailedRegionProjection.Project(runs[id], id, overlay);
            await File.WriteAllTextAsync(Path.Combine(directory, id + ".json"), JsonSerializer.Serialize(new { schema = "agentic2d.m032.structural-frame.v1", id, overlay, projection = "read-only", simulationSave = runs[id].World.Capture(), decisions = runs[id].Decisions, routeEvents = runs[id].RouteEvents, frame }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
