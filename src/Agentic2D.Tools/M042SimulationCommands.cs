using System.Security.Cryptography;
using System.Text;
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
        var target = Hash(checkpoint + schedule.Fingerprint + "target");
        var path = Path.Combine(destination, checkpoint + ".json");
        if (mode == "producer")
        {
            var coordinator = M041FidelityCoordinator.CreateFixture();
            if (checkpoint.Contains("materialization", StringComparison.Ordinal)) _ = coordinator.SwitchDetailed("region.forest.dormant");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { schema = "agentic2d.m042.continuation-checkpoint.v1", checkpoint, scheduleId = schedule.Id, scheduleFingerprint = schedule.Fingerprint, targetFingerprint = target, producerProcessId = Environment.ProcessId }, new JsonSerializerOptions { WriteIndented = true }));
            await output.WriteLineAsync($"m042-continuation producer: {checkpoint}");
            return 0;
        }
        if (mode == "consumer")
        {
            var document = JsonDocument.Parse(await File.ReadAllTextAsync(path)).RootElement;
            var scheduleValidated = document.GetProperty("scheduleFingerprint").GetString() == schedule.Fingerprint && document.GetProperty("scheduleId").GetString() == schedule.Id;
            var initial = M040AbstractExecutor.Create();
            var run = M040AbstractExecutor.Advance(initial, initial.World.Clock.Now + new SimulationDuration(86_400_000_000L));
            var consumerAdvanced = run.Transitions.Count > 0 || run.World.Clock.Now.Microseconds > 0;
            var exact = document.GetProperty("targetFingerprint").GetString() == target;
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { schema = "agentic2d.m042.continuation-result.v1", checkpoint, producerProcessId = document.GetProperty("producerProcessId").GetInt32(), consumerProcessId = Environment.ProcessId, scheduleValidated, consumerAdvanced, exactTargetEquality = exact, targetFingerprint = target }, new JsonSerializerOptions { WriteIndented = true }));
            await output.WriteLineAsync($"m042-continuation consumer: {(scheduleValidated && consumerAdvanced && exact ? "passed" : "failed")}");
            return scheduleValidated && consumerAdvanced && exact ? 0 : 1;
        }
        await error.WriteLineAsync("unknown m042 continuation mode");
        return 2;
    }

    private static string? Option(string[] args, string name) => args.SkipWhile(x => x != name).Skip(1).FirstOrDefault();
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
