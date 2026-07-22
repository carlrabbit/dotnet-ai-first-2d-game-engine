using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic2D.Simulation;

namespace Agentic2D.Tools;

internal static class M033SimulationCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 2 || args[0] != "simulation") return -1;
        if (args.Length == 6 && args[1] == "m033-multi-region-child" && args[2] == "--save" && args[4] == "--output") return await ContinueInFreshProcess(args[3], args[5], output, error);
        string? destination = null;
        if (args[1] == "m033-multi-region") destination = Option(args, "--output");
        if (args[1] == "run" && args.Length >= 3 && args[2] == M033MultiFidelitySimulation.ScenarioId)
        {
            destination = Option(args, "--output");
            var mode = Option(args, "--mode");
            var until = Option(args, "--until");
            if (mode != "abstract" || string.IsNullOrWhiteSpace(until))
            {
                await error.WriteLineAsync("simulation run requires --until <duration> --mode abstract --output <directory>");
                return 2;
            }
        }
        if (destination is null) return -1;
        try
        {
            var run = await M033ArtifactWriter.WriteAsync(destination);
            if (run.Diagnostics.Count != 0) throw new InvalidOperationException(string.Join(", ", run.Diagnostics.Select(diagnostic => diagnostic.Code)));
            var childDirectory = Path.Combine(destination, "fresh-process");
            var exitCode = await StartFreshProcessAsync(Path.Combine(destination, "mixed-fidelity-save.json"), childDirectory);
            using var continuation = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(childDirectory, "continuation.json")));
            if (exitCode != 0 || continuation.RootElement.GetProperty("status").GetString() != "passed") throw new InvalidOperationException("fresh-process continuation failed");
            await File.WriteAllTextAsync(Path.Combine(destination, "fresh-process.json"), JsonSerializer.Serialize(new { schema = "agentic2d.m033.fresh-process-proof.v1", status = "passed", processExitCode = exitCode, queueCount = continuation.RootElement.GetProperty("queueCount").GetInt32() }, Json));
            await output.WriteLineAsync("simulation M033 multi-region: passed; output: " + destination);
            return 0;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or System.Text.Json.JsonException)
        {
            await error.WriteLineAsync("simulation M033 multi-region failed: " + exception.Message);
            return 1;
        }
    }

    private static async Task<int> ContinueInFreshProcess(string savePath, string outputPath, TextWriter output, TextWriter error)
    {
        try
        {
            var options = new JsonSerializerOptions(Json) { PropertyNameCaseInsensitive = true };
            var save = JsonSerializer.Deserialize<MultiFidelitySave>(await File.ReadAllTextAsync(savePath), options) ?? throw new InvalidOperationException("save is malformed");
            var run = M033MultiFidelitySimulation.ContinueFromSave(save);
            Directory.CreateDirectory(outputPath);
            await File.WriteAllTextAsync(Path.Combine(outputPath, "continuation.json"), JsonSerializer.Serialize(new { schema = "agentic2d.m033.fresh-process-continuation.v1", status = run.Diagnostics.Count == 0 ? "passed" : "failed", queueCount = run.Scheduler.Inspect().Count, diagnostics = run.Diagnostics }, Json));
            await output.WriteLineAsync("simulation M033 child: passed");
            return run.Diagnostics.Count == 0 ? 0 : 1;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or JsonException)
        {
            await error.WriteLineAsync("simulation M033 child failed: " + exception.Message);
            return 1;
        }
    }

    private static async Task<int> StartFreshProcessAsync(string savePath, string outputPath)
    {
        var processPath = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";
        var assembly = Path.Combine(AppContext.BaseDirectory, "Agentic2D.Tools.dll");
        using var process = Process.Start(new ProcessStartInfo(processPath, [assembly, "simulation", "m033-multi-region-child", "--save", savePath, "--output", outputPath]) { UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true }) ?? throw new InvalidOperationException("cannot start fresh M033 simulation process");
        await process.StandardOutput.ReadToEndAsync(); var errors = await process.StandardError.ReadToEndAsync(); await process.WaitForExitAsync();
        if (process.ExitCode != 0) throw new InvalidOperationException("fresh M033 simulation process failed: " + errors);
        return process.ExitCode;
    }

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length && !string.IsNullOrWhiteSpace(args[index + 1]) ? args[index + 1] : null;
    }

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } };
}
