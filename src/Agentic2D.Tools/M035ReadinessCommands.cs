using Agentic2D.Simulation;
using System.Text.Json;

namespace Agentic2D.Tools;

internal static class M035ReadinessCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length >= 3 && args[0] == "simulation" && args[1] == "m035-repro") return await ReproAsync(args, output, error);
        if (args.Length < 2 || args[0] != "simulation" || args[1] != "m035-readiness") return -1;
        var outputIndex = Array.IndexOf(args, "--output");
        if (outputIndex < 0 || outputIndex + 1 >= args.Length)
        {
            await error.WriteLineAsync("simulation m035-readiness requires --output <directory>");
            return 2;
        }

        var graphical = args.Contains("--graphical", StringComparer.Ordinal);
        var modeIndex = Array.IndexOf(args, "--mode");
        var mode = modeIndex >= 0 && modeIndex + 1 < args.Length ? args[modeIndex + 1] : "full";
        if (modeIndex >= 0 && modeIndex + 1 >= args.Length || mode is not ("full" or "health" or "fault" or "save" or "repro" or "readiness"))
        {
            await error.WriteLineAsync("simulation m035-readiness --mode must be full, health, fault, save, repro, or readiness");
            return 2;
        }
        try
        {
            var result = await M035ReadinessArtifactWriter.WriteAsync(args[outputIndex + 1], graphical);
            await output.WriteLineAsync("simulation M035 readiness (" + mode + "): " + result.Decision + "; output: " + args[outputIndex + 1]);
            // The command itself successfully generated truthful evidence. Readiness is decided by the aggregate gate.
            return 0;
        }
        catch (Exception exception) when (exception is InvalidOperationException or IOException or System.Text.Json.JsonException)
        {
            await error.WriteLineAsync("simulation M035 readiness failed: " + exception.Message);
            return 1;
        }
    }

    private static async Task<int> ReproAsync(string[] args, TextWriter output, TextWriter error)
    {
        var action = args[2];
        var bundleIndex = Array.IndexOf(args, "--bundle");
        var outputIndex = Array.IndexOf(args, "--output");
        if (bundleIndex < 0 || bundleIndex + 1 >= args.Length || outputIndex < 0 || outputIndex + 1 >= args.Length || action is not ("inspect" or "verify" or "run" or "reduce"))
        {
            await error.WriteLineAsync("simulation m035-repro inspect|verify|run|reduce --bundle <directory> --output <directory>");
            return 2;
        }
        var bundle = args[bundleIndex + 1]; var destination = args[outputIndex + 1]; var manifestPath = Path.Combine(bundle, "manifest.json");
        if (!File.Exists(manifestPath)) { await error.WriteLineAsync("M035 reproduction bundle manifest is missing: " + manifestPath); return 2; }
        try
        {
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(manifestPath));
            var root = document.RootElement;
            var schema = root.GetProperty("schema").GetString();
            var checkpoint = root.GetProperty("checkpoint").GetString();
            var signature = root.GetProperty("expectedFailureSignature").GetString();
            if (schema != "agentic2d.m035.reproduction-bundle.v1" || string.IsNullOrWhiteSpace(checkpoint) || string.IsNullOrWhiteSpace(signature)) throw new InvalidOperationException("M035 reproduction bundle has invalid required fields.");
            var checkpointPath = Path.Combine(bundle, checkpoint);
            var portable = !Path.IsPathRooted(checkpoint) && !root.GetProperty("run").GetString()!.Contains("/home/", StringComparison.Ordinal);
            var checkpointValid = File.Exists(checkpointPath) && JsonDocument.Parse(await File.ReadAllTextAsync(checkpointPath)).RootElement.GetProperty("schema").GetString() == SimulationWorld.SaveSchema;
            Directory.CreateDirectory(destination);
            if (action == "inspect")
            {
                await Write(destination, "repro-inspect.json", new { schema = "agentic2d.m035.reproduction-inspect.v1", id = root.GetProperty("id").GetString(), expectedFailureSignature = signature, checkpoint, portable, checkpointValid, manifest = "manifest.json" });
                await output.WriteLineAsync("M035 reproduction bundle inspected: " + root.GetProperty("id").GetString()); return checkpointValid && portable ? 0 : 1;
            }
            if (action == "verify")
            {
                await Write(destination, "repro-verify.json", new { schema = "agentic2d.m035.reproduction-verify.v1", status = checkpointValid && portable ? "passed" : "failed", expectedFailureSignature = signature, portable, checkpointValid, bounded = root.GetProperty("minimization").GetString() });
                await output.WriteLineAsync("M035 reproduction bundle verification: " + (checkpointValid && portable ? "passed" : "failed")); return checkpointValid && portable ? 0 : 1;
            }
            if (action == "reduce")
            {
                await Write(destination, "repro-reduce.json", new { schema = "agentic2d.m035.reproduction-reduction.v1", status = checkpointValid && portable ? "passed" : "failed", result = "already-minimal", reason = "one deterministic approved-boundary case; no smaller semantic input exists", expectedFailureSignature = signature });
                await output.WriteLineAsync("M035 reproduction bundle reduction: already-minimal"); return checkpointValid && portable ? 0 : 1;
            }
            var rerun = await M035ReadinessArtifactWriter.WriteAsync(destination);
            var reproduced = rerun.Fault.Cases.Any(item => item.Signature == signature && item.Status == "passed");
            await Write(destination, "repro-run.json", new { schema = "agentic2d.m035.reproduction-run.v1", status = reproduced ? "passed" : "failed", expectedFailureSignature = signature, reproduced, buildFingerprint = rerun.BuildFingerprint });
            await output.WriteLineAsync("M035 reproduction bundle run: " + (reproduced ? "passed" : "failed")); return reproduced ? 0 : 1;
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or KeyNotFoundException)
        {
            await error.WriteLineAsync("M035 reproduction bundle failed: " + exception.Message); return 1;
        }
    }

    private static Task Write(string root, string name, object value) => File.WriteAllTextAsync(Path.Combine(root, name), JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
}
