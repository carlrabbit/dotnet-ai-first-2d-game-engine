using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Tools;

internal static class M040SimulationCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3 || args[0] != "simulation" || args[1] != "m040-abstract") return -1;
        var mode = args[2];
        var destination = Option(args, "--output") ?? throw new ArgumentException("simulation m040-abstract requires --output");
        Directory.CreateDirectory(destination);
        var json = new JsonSerializerOptions { WriteIndented = true };
        if (mode == "producer")
        {
            var initial = M040AbstractExecutor.Create();
            var checkpoint = M040AbstractExecutor.Advance(initial, initial.World.Clock.Now + SimulationDuration.FromSeconds(10));
            var target = checkpoint.World.Clock.Now + SimulationDuration.FromSeconds(40);
            var uninterrupted = M040AbstractExecutor.Advance(M040AbstractExecutor.Create(), target);
            await File.WriteAllTextAsync(Path.Combine(destination, "checkpoint.json"), JsonSerializer.Serialize(M040AbstractExecutor.Capture(checkpoint), json));
            await File.WriteAllTextAsync(Path.Combine(destination, "target.json"), JsonSerializer.Serialize(new { targetMicroseconds = target.Microseconds, uninterruptedFingerprint = uninterrupted.Fingerprint, checkpointFingerprint = checkpoint.Fingerprint, checkpointInstant = checkpoint.World.Clock.Now.Microseconds }, json));
            await File.WriteAllTextAsync(Path.Combine(destination, "producer.json"), JsonSerializer.Serialize(new { status = "passed", processId = Environment.ProcessId, checkpoint = checkpoint.World.Clock.Now.Microseconds, target = target.Microseconds, queueCount = checkpoint.Scheduler.PendingCount }, json));
            await output.WriteLineAsync("simulation m040-abstract producer: passed");
            return 0;
        }
        if (mode == "consumer")
        {
            var savePath = Path.Combine(destination, "checkpoint.json"); var targetPath = Path.Combine(destination, "target.json");
            var save = JsonSerializer.Deserialize<M040AbstractSave>(await File.ReadAllTextAsync(savePath)) ?? throw new InvalidOperationException("M040 consumer checkpoint missing");
            var target = JsonDocument.Parse(await File.ReadAllTextAsync(targetPath)).RootElement.GetProperty("targetMicroseconds").GetInt64();
            var run = M040AbstractExecutor.Advance(M040AbstractExecutor.Restore(save), new SimulationInstant(target));
            var expected = JsonDocument.Parse(await File.ReadAllTextAsync(targetPath)).RootElement.GetProperty("uninterruptedFingerprint").GetString();
            var passed = run.World.Clock.Now.Microseconds == target && run.Fingerprint == expected && run.Transitions.Contains("deposit-complete:inventory-to-storage", StringComparer.Ordinal) && run.World.Reservations.All(x => x.Status != SimulationReservationStatus.Active);
            await File.WriteAllTextAsync(Path.Combine(destination, "consumer.json"), JsonSerializer.Serialize(new { status = passed ? "passed" : "failed", processId = Environment.ProcessId, checkpoint = save.Continuation.TargetMicroseconds, target, resumedFingerprint = run.Fingerprint, uninterruptedFingerprint = expected, advancedBeyondCheckpoint = target > save.Continuation.TargetMicroseconds, transitions = run.Transitions, storageWood = run.World.TryGetComponent<M032StorageComponent>("storage.wood.001", "component.m032.storage", out var storage) ? storage!.Wood : -1 }, json));
            await output.WriteLineAsync($"simulation m040-abstract consumer: {(passed ? "passed" : "failed")}");
            return passed ? 0 : 1;
        }
        await error.WriteLineAsync("unknown m040 abstract mode");
        return 2;
    }

    private static string? Option(string[] args, string name) => args.SkipWhile(x => x != name).Skip(1).FirstOrDefault();
}
