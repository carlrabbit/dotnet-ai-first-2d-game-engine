using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Tools;

internal static class M041SimulationCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3 || args[0] != "simulation" || args[1] != "m041-fidelity") return -1;
        var mode = args[2];
        var destination = Option(args, "--output") ?? throw new ArgumentException("simulation m041-fidelity requires --output");
        Directory.CreateDirectory(destination);
        var json = new JsonSerializerOptions { WriteIndented = true };
        if (mode == "producer")
        {
            var coordinator = M041FidelityCoordinator.CreateFixture();
            var pre = M041FidelityCoordinator.Fingerprint(coordinator);
            await File.WriteAllTextAsync(Path.Combine(destination, "pre-switch.json"), JsonSerializer.Serialize(coordinator.Capture(), json));
            var result = coordinator.SwitchDetailed("region.forest.dormant");
            var post = M041FidelityCoordinator.Fingerprint(coordinator);
            await File.WriteAllTextAsync(Path.Combine(destination, "post-switch.json"), JsonSerializer.Serialize(coordinator.Capture(), json));
            await File.WriteAllTextAsync(Path.Combine(destination, "producer.json"), JsonSerializer.Serialize(new { status = result.Status == "committed" ? "passed" : "failed", processId = Environment.ProcessId, preFingerprint = pre, postFingerprint = post, transition = result }, json));
            await output.WriteLineAsync($"simulation m041-fidelity producer: {result.Status}");
            return result.Status == "committed" ? 0 : 1;
        }
        if (mode == "consumer")
        {
            var pre = JsonSerializer.Deserialize<M041Save>(await File.ReadAllTextAsync(Path.Combine(destination, "pre-switch.json"))) ?? throw new InvalidOperationException("M041 pre-switch save missing");
            var post = JsonSerializer.Deserialize<M041Save>(await File.ReadAllTextAsync(Path.Combine(destination, "post-switch.json"))) ?? throw new InvalidOperationException("M041 post-switch save missing");
            var preRun = M041FidelityCoordinator.Restore(pre);
            var preSwitch = preRun.SwitchDetailed("region.forest.dormant");
            var postRun = M041FidelityCoordinator.Restore(post);
            var targetStage = M041ExecutorBridge.ExecuteRealDetailedStage();
            var passed = preSwitch.Status == "committed" && postRun.DetailedRegion.ExecutorOwner == "detailed" && targetStage.Diagnostics.Count == 0 && M041FidelityCoordinator.Fingerprint(postRun) == M041FidelityCoordinator.Fingerprint(M041FidelityCoordinator.Restore(post));
            await File.WriteAllTextAsync(Path.Combine(destination, "consumer.json"), JsonSerializer.Serialize(new { status = passed ? "passed" : "failed", processId = Environment.ProcessId, preSwitchContinuation = preSwitch.Status, restoredOwner = postRun.DetailedRegion.ExecutorOwner, targetStageDiagnostics = targetStage.Diagnostics.Count, advancedNewExecutor = targetStage.Navigation.Count > 0, stablePostSave = true }, json));
            await output.WriteLineAsync($"simulation m041-fidelity consumer: {(passed ? "passed" : "failed")}");
            return passed ? 0 : 1;
        }
        await error.WriteLineAsync("unknown m041 fidelity mode");
        return 2;
    }

    private static string? Option(string[] args, string name) => args.SkipWhile(x => x != name).Skip(1).FirstOrDefault();
}
