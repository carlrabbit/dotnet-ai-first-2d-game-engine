using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Tools;

internal static class M042SimulationCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3 || args[0] != "simulation" || args[1] != "m042-continuation") return -1;
        var mode = args[2];
        var checkpoint = Option(args, "--checkpoint") ?? throw new ArgumentException("m042-continuation requires --checkpoint");
        var destination = Option(args, "--output") ?? throw new ArgumentException("m042-continuation requires --output");
        Directory.CreateDirectory(destination);
        var schedule = M042Schedule.Create("periodically-switched");
        var path = Path.Combine(destination, checkpoint + ".json");
        if (mode == "producer")
        {
            var coordinator = M041FidelityCoordinator.CreateFixture();
            PrepareCheckpoint(coordinator, checkpoint);
            var save = coordinator.Capture();
            var savePath = Path.Combine(destination, checkpoint + ".save.json");
            await File.WriteAllTextAsync(savePath, JsonSerializer.Serialize(save, new JsonSerializerOptions { WriteIndented = true }));
            var expected = Continue(coordinator, checkpoint);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { schema = "agentic2d.m042.continuation-checkpoint.v1", checkpoint, scheduleId = schedule.Id, scheduleFingerprint = schedule.Fingerprint, targetFingerprint = expected, producerProcessId = Environment.ProcessId }, new JsonSerializerOptions { WriteIndented = true }));
            await output.WriteLineAsync($"m042-continuation producer: {checkpoint}");
            return 0;
        }
        if (mode == "consumer")
        {
            var document = JsonDocument.Parse(await File.ReadAllTextAsync(path)).RootElement;
            var scheduleValidated = document.GetProperty("scheduleFingerprint").GetString() == schedule.Fingerprint && document.GetProperty("scheduleId").GetString() == schedule.Id;
            var savePath = Path.Combine(destination, checkpoint + ".save.json");
            var save = JsonSerializer.Deserialize<M041Save>(await File.ReadAllTextAsync(savePath)) ?? throw new InvalidOperationException("M042 continuation save missing");
            var resumed = M041FidelityCoordinator.Restore(save);
            var before = M041FidelityCoordinator.Fingerprint(resumed);
            var actual = Continue(resumed, checkpoint);
            var consumerAdvanced = actual != before;
            var exact = document.GetProperty("targetFingerprint").GetString() == actual;
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { schema = "agentic2d.m042.continuation-result.v1", checkpoint, producerProcessId = document.GetProperty("producerProcessId").GetInt32(), consumerProcessId = Environment.ProcessId, scheduleValidated, consumerAdvanced, exactTargetEquality = exact, targetFingerprint = actual }, new JsonSerializerOptions { WriteIndented = true }));
            await output.WriteLineAsync($"m042-continuation consumer: {(scheduleValidated && consumerAdvanced && exact ? "passed" : "failed")}");
            return scheduleValidated && consumerAdvanced && exact ? 0 : 1;
        }
        await error.WriteLineAsync("unknown m042 continuation mode");
        return 2;
    }

    private static string? Option(string[] args, string name) => args.SkipWhile(x => x != name).Skip(1).FirstOrDefault();
    private static void PrepareCheckpoint(M041FidelityCoordinator coordinator, string checkpoint)
    {
        if (checkpoint is "immediately-after-materialization" or "immediately-after-abstraction" or "equal-time-trigger-and-switch-boundary")
        {
            if (coordinator.SwitchDetailed("region.forest.dormant").Status != "committed") throw new InvalidOperationException("M042 checkpoint preparation failed");
        }
    }
    private static string Continue(M041FidelityCoordinator coordinator, string checkpoint)
    {
        if (checkpoint == "immediately-after-materialization")
        {
            if (coordinator.SwitchDetailed("region.forest.active").Status != "committed") throw new InvalidOperationException("M042 materialization continuation failed");
        }
        else if (checkpoint == "immediately-after-abstraction")
        {
            if (coordinator.SwitchDetailed("region.forest.active").Status != "committed") throw new InvalidOperationException("M042 abstraction continuation failed");
        }
        else
        {
            coordinator.World.Advance(new SimulationDuration(1_000_000));
        }
        return M041FidelityCoordinator.Fingerprint(coordinator);
    }
}
