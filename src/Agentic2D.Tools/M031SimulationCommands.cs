using System.Diagnostics;
using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Tools;

internal static class M031SimulationCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length == 6 && args[0] == "simulation" && args[1] == "wood-workflow-child" && args[2] == "--save" && args[4] == "--output") return await ContinueInFreshProcess(args[3], args[5], output, error);
        if (args.Length < 2 || args[0] != "simulation" || args[1] != "wood-workflow") return -1;
        var option = Array.IndexOf(args, "--output");
        if (option < 0 || option + 1 >= args.Length || string.IsNullOrWhiteSpace(args[option + 1]))
        {
            await error.WriteLineAsync("simulation wood-workflow requires --output <directory>");
            return 2;
        }

        if (args.Length != 4 || option != 2)
        {
            await error.WriteLineAsync("simulation wood-workflow accepts only --output <directory>");
            return 2;
        }

        try
        {
            await SimulationFoundationArtifactWriter.WriteWoodWorkflowAsync(args[3]);
            var savePath = Path.Combine(args[3], "wood-workflow", "roundtrip", "save.json");
            var childDirectory = Path.Combine(args[3], "wood-workflow", "fresh-process");
            var child = await StartFreshProcessAsync(savePath, childDirectory);
            using var childResult = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(childDirectory, "continuation.json")));
            using var comparison = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(args[3], "wood-workflow", "comparison.json")));
            var expected = comparison.RootElement.GetProperty("roundtripFingerprint").GetString();
            var actual = childResult.RootElement.GetProperty("fingerprint").GetString();
            if (child != 0 || !string.Equals(expected, actual, StringComparison.Ordinal)) throw new InvalidOperationException("fresh-process continuation fingerprint mismatch");
            await File.WriteAllTextAsync(Path.Combine(args[3], "wood-workflow", "fresh-process.json"), JsonSerializer.Serialize(new { schema = "agentic2d.simulation-fresh-process-proof.v1", status = "passed", processExitCode = child, fingerprint = actual }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            await output.WriteLineAsync("simulation wood-workflow: passed; output: " + args[3]);
            return 0;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or JsonException)
        {
            await error.WriteLineAsync("simulation wood-workflow failed: " + exception.Message);
            return 1;
        }
    }

    private static async Task<int> ContinueInFreshProcess(string savePath, string outputPath, TextWriter output, TextWriter error)
    {
        try
        {
            var save = JsonSerializer.Deserialize<SimulationSave>(await File.ReadAllTextAsync(savePath), new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } });
            if (save is null) throw new InvalidOperationException("save is malformed");
            var continued = M031WoodWorkflow.ContinueFromSave(save);
            Directory.CreateDirectory(outputPath);
            await File.WriteAllTextAsync(Path.Combine(outputPath, "continuation.json"), JsonSerializer.Serialize(new { schema = "agentic2d.simulation-fresh-process-continuation.v1", status = continued.Diagnostics.Count == 0 ? "passed" : "failed", processId = Environment.ProcessId, fingerprint = continued.Fingerprint, diagnostics = continued.Diagnostics }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            await output.WriteLineAsync("simulation wood-workflow child: passed");
            return continued.Diagnostics.Count == 0 ? 0 : 1;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or JsonException)
        {
            await error.WriteLineAsync("simulation wood-workflow child failed: " + exception.Message);
            return 1;
        }
    }

    private static async Task<int> StartFreshProcessAsync(string savePath, string outputPath)
    {
        var processPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        var assembly = Path.Combine(AppContext.BaseDirectory, "Agentic2D.Tools.dll");
        using var process = Process.Start(new ProcessStartInfo(processPath, [assembly, "simulation", "wood-workflow-child", "--save", savePath, "--output", outputPath]) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }) ?? throw new InvalidOperationException("cannot start fresh simulation process");
        await process.StandardOutput.ReadToEndAsync(); var errors = await process.StandardError.ReadToEndAsync(); await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException("fresh simulation process failed: " + errors);
        return process.ExitCode;
    }
}
