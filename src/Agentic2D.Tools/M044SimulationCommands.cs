using System.Text.Json;
using Agentic2D.Persistence;
using Agentic2D.Simulation;

namespace Agentic2D.Tools;

internal static class M044SimulationCommands
{
    private const string ScheduleId = "schedule.m044.canonical-resume.v1";
    private const string ScheduleFingerprint = "schedule-fingerprint.m044.v1";
    private const string ProjectId = "project.m044";
    private const string ConfigId = "world.standard";
    private const string ConfigFingerprint = "config.m044.standard.v1";
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length < 3 || args[0] != "simulation" || args[1] != "m044-continuation") return -1;
        var mode = args[2]; var checkpoint = Option(args, "--checkpoint") ?? throw new ArgumentException("m044-continuation requires --checkpoint"); var destination = Option(args, "--output") ?? throw new ArgumentException("m044-continuation requires --output"); var schedule = Option(args, "--schedule") ?? ScheduleFingerprint; Directory.CreateDirectory(destination);
        if (schedule != ScheduleFingerprint) { await error.WriteLineAsync("M044-SCHEDULE0001: external schedule fingerprint mismatch"); return 1; }
        if (mode == "control") return await Control(checkpoint, destination, output);
        if (mode == "producer") return await Producer(checkpoint, destination, output);
        if (mode == "consumer") return await Consumer(checkpoint, destination, output);
        if (mode == "recovery-producer") return await RecoveryProducer(checkpoint, destination, output);
        await error.WriteLineAsync("unknown m044 continuation mode"); return 2;
    }

    private static async Task<int> Control(string checkpoint, string destination, TextWriter output)
    {
        var world = CreateWorld(checkpoint); Prepare(world, checkpoint); var before = world.SequenceForM044(); var fixture = Fixture(world, checkpoint); Continue(world, checkpoint); var final = Capture(world); await File.WriteAllTextAsync(Path.Combine(destination, checkpoint + ".control.json"), JsonSerializer.Serialize(new { processId = Environment.ProcessId, scheduleId = ScheduleId, scheduleFingerprint = ScheduleFingerprint, checkpoint, checkpointMicroseconds = before.clock, fixture, preCheckpointEventIds = before.eventIds, finalFingerprint = final.CanonicalSaveFingerprint, finalSequence = world.SequenceForM044().sequence, postCheckpointEventIds = world.Events.Select(x => x.Id).Where(x => !before.eventIds.Contains(x, StringComparer.Ordinal)).ToArray() }, Json)); await output.WriteLineAsync("m044 control: " + checkpoint); return 0;
    }

    private static async Task<int> Producer(string checkpoint, string destination, TextWriter output)
    {
        var coordinator = CreateCoordinator(checkpoint); var world = coordinator?.World ?? CreateWorld(checkpoint); Prepare(world, checkpoint); var before = world.SequenceForM044(); var fixture = Fixture(world, checkpoint); var fidelityPath = Path.Combine(destination, checkpoint + ".m041-save.json"); if (coordinator is not null) await File.WriteAllTextAsync(fidelityPath, JsonSerializer.Serialize(coordinator.Capture(), Json)); var envelope = Capture(world); var path = Path.Combine(destination, checkpoint + ".canonical-save.json"); new CanonicalRuntimePersistenceService().WriteAtomic(path, envelope, ProjectId, ConfigId, ConfigFingerprint, Content(world)); await File.WriteAllTextAsync(Path.Combine(destination, checkpoint + ".producer.json"), JsonSerializer.Serialize(new { processId = Environment.ProcessId, scheduleId = ScheduleId, scheduleFingerprint = ScheduleFingerprint, checkpoint, checkpointMicroseconds = before.clock, fixture, fidelityPath = coordinator is null ? null : fidelityPath, savePath = path, saveFingerprint = envelope.CanonicalSaveFingerprint, preCheckpointEventIds = before.eventIds }, Json)); await output.WriteLineAsync("m044 producer: " + checkpoint); return 0;
    }

    private static async Task<int> RecoveryProducer(string checkpoint, string destination, TextWriter output)
    {
        var world = CreateWorld(checkpoint); Prepare(world, checkpoint); var before = world.SequenceForM044();
        var service = new CanonicalRuntimePersistenceService(); var entries = Content(world);
        var envelope = Capture(world); var path = Path.Combine(destination, checkpoint + ".canonical-save.json");
        var previous = Path.Combine(destination, checkpoint + ".previous-good.json");
        service.WriteAtomic(previous, envelope, ProjectId, ConfigId, ConfigFingerprint, entries);
        service.WriteAtomic(path, envelope, ProjectId, ConfigId, ConfigFingerprint, entries);
        await File.WriteAllTextAsync(path, "{corrupted");
        var recovered = service.Recover(path, previous, M032AutonomousDetailedRegion.Registrations(), ProjectId, ConfigId, ConfigFingerprint, Content(CreateWorld(checkpoint)));
        if (!recovered.Success || recovered.Envelope is null) { await output.WriteLineAsync("m044 recovery producer: recovery failed"); return 1; }
        await File.WriteAllTextAsync(Path.Combine(destination, checkpoint + ".producer.json"), JsonSerializer.Serialize(new { processId = Environment.ProcessId, scheduleId = ScheduleId, scheduleFingerprint = ScheduleFingerprint, checkpoint, checkpointMicroseconds = before.clock, fixture = Fixture(recovered.World!, checkpoint), savePath = path, previousGoodPath = previous, saveFingerprint = recovered.Envelope.CanonicalSaveFingerprint, preCheckpointEventIds = before.eventIds, corruptionDetected = true, recoveryValidated = true }, Json));
        await output.WriteLineAsync("m044 recovery producer: " + checkpoint); return 0;
    }

    private static async Task<int> Consumer(string checkpoint, string destination, TextWriter output)
    {
        var producerPath = Path.Combine(destination, checkpoint + ".producer.json"); var producer = JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(producerPath), Json); var savePath = producer.GetProperty("savePath").GetString()!; var service = new CanonicalRuntimePersistenceService(); var loaded = service.LoadFresh(savePath, M032AutonomousDetailedRegion.Registrations(), ProjectId, ConfigId, ConfigFingerprint, Content(CreateWorld(checkpoint))); if (!loaded.Success || loaded.World is null) { await output.WriteLineAsync("m044 consumer: failed load: " + string.Join("; ", loaded.Diagnostics)); return 1; }
        var fidelityRestored = false; var fidelityPath = producer.TryGetProperty("fidelityPath", out var fidelityProperty) && fidelityProperty.ValueKind != JsonValueKind.Null ? fidelityProperty.GetString() : null; if (fidelityPath is not null) { var saved = JsonSerializer.Deserialize<M041Save>(await File.ReadAllTextAsync(fidelityPath), Json) ?? throw new InvalidOperationException("M044 fidelity continuation save is malformed"); var restored = M041FidelityCoordinator.Restore(saved); if (restored.World.Fingerprint() != loaded.World.Fingerprint()) throw new InvalidOperationException("M044 fidelity continuation differs from canonical world"); loaded = loaded with { World = restored.World }; fidelityRestored = true; }
        var before = loaded.World.SequenceForM044(); var fixture = Fixture(loaded.World, checkpoint); var roundtrip = Capture(loaded.World); var roundtripPath = Path.Combine(destination, checkpoint + ".roundtrip-save.json"); service.WriteAtomic(roundtripPath, roundtrip, ProjectId, ConfigId, ConfigFingerprint, Content(loaded.World)); Continue(loaded.World, checkpoint); var final = Capture(loaded.World); var preIds = producer.GetProperty("preCheckpointEventIds").EnumerateArray().Select(x => x.GetString()!).ToHashSet(StringComparer.Ordinal); var postIds = loaded.World.Events.Select(x => x.Id).Where(x => !preIds.Contains(x)).ToArray(); await File.WriteAllTextAsync(Path.Combine(destination, checkpoint + ".consumer.json"), JsonSerializer.Serialize(new { processId = Environment.ProcessId, scheduleId = producer.GetProperty("scheduleId").GetString(), scheduleFingerprint = producer.GetProperty("scheduleFingerprint").GetString(), checkpoint, fixture, fidelityRestored, consumerAdvanced = loaded.World.Clock.Now.Microseconds > producer.GetProperty("checkpointMicroseconds").GetInt64(), finalFingerprint = final.CanonicalSaveFingerprint, roundtripFingerprint = roundtrip.CanonicalSaveFingerprint, roundtripPath, finalSequence = loaded.World.SequenceForM044().sequence, postCheckpointEventIds = postIds, noDuplicatePostIds = postIds.Distinct(StringComparer.Ordinal).Count() == postIds.Length, noSequenceReset = loaded.World.SequenceForM044().sequence > before.sequence }, Json)); await output.WriteLineAsync("m044 consumer: " + checkpoint); return 0;
    }

    private static SimulationWorld CreateWorld(string checkpoint)
    {
        if (checkpoint is "abstract-travel" or "abstract-carrying" or "mandatory-need-interruption")
        {
            var initial = M040AbstractExecutor.Create();
            return M040AbstractExecutor.Advance(initial, initial.World.Clock.Now + new SimulationDuration(1_000_000)).World;
        }
        if (checkpoint is "immediately-after-materialization" or "immediately-after-abstraction" or "equal-time-trigger-and-switch-boundary")
        {
            var coordinator = M041FidelityCoordinator.CreateFixture(); coordinator.SwitchDetailed("region.forest.dormant"); return coordinator.World;
        }
        return M032AutonomousDetailedRegion.CreateInitial();
    }
    private static M041FidelityCoordinator? CreateCoordinator(string checkpoint) => checkpoint is "immediately-after-materialization" or "immediately-after-abstraction" or "equal-time-trigger-and-switch-boundary" ? CreateAndSwitch() : null;
    private static M041FidelityCoordinator CreateAndSwitch() { var coordinator = M041FidelityCoordinator.CreateFixture(); coordinator.SwitchDetailed("region.forest.dormant"); return coordinator; }
    private static void Prepare(SimulationWorld world, string checkpoint)
    {
        if (checkpoint == "typed-world-active-reservation")
        {
            var result = world.CreateActivityWithReservations(new ActivityId("activity.m044.reservation"), "worker.001", "harvest-and-haul", "travel-to-source", ["tree.001"], [new(new ReservationId("reservation.m044.active"), "tree.001", "exclusive.harvest", 1, null)], new CorrelationId("correlation.m044.reservation"), new CausationId("cause.m044.fixture"));
            if (result.Status != "accepted") throw new InvalidOperationException("M044 fixture reservation was not accepted: " + string.Join("; ", result.Diagnostics.Select(x => x.Code)));
        }
        if (checkpoint == "destroyed-entity-tombstone") { world.DestroyEntity("tree.001"); }
        else if (checkpoint is "immediately-after-materialization" or "immediately-after-abstraction" or "equal-time-trigger-and-switch-boundary") { world.Advance(new SimulationDuration(5_000_000)); }
        else { world.Advance(new SimulationDuration(10_000_000)); }
    }
    private static object Fixture(SimulationWorld world, string checkpoint) => new
    {
        checkpoint,
        activeReservationCount = world.Reservations.Count(x => x.Status == SimulationReservationStatus.Active),
        tombstonePresent = world.Capture().Tombstones.Contains("tree.001", StringComparer.Ordinal),
        executorFixture = checkpoint is "abstract-travel" or "abstract-carrying" or "mandatory-need-interruption" ? "m040-abstract" : checkpoint is "immediately-after-materialization" or "immediately-after-abstraction" or "equal-time-trigger-and-switch-boundary" ? "m041-fidelity" : "m032-detailed"
    };
    private static void Continue(SimulationWorld world, string checkpoint) { world.Advance(new SimulationDuration(10_000_000)); var id = "entity.m044.after." + checkpoint; if (!world.Entities.Any(x => x.Id == id)) world.CreateEntity(id, SimulationEntityScope.WorldScoped); }
    private static CanonicalGameSaveEnvelope Capture(SimulationWorld world) { var service = new CanonicalRuntimePersistenceService(); return service.Capture(world, "save.m044." + CurrentCheckpoint(world), ProjectId, ConfigId, ConfigFingerprint, Content(world)); }
    private static string CurrentCheckpoint(SimulationWorld world) => world.Clock.Now.Microseconds <= 5_000_000 ? "boundary" : "continuation";
    private static IReadOnlyList<SemanticContentEntry> Content(SimulationWorld world) => CanonicalRuntimePersistenceService.ResolveSemanticContent(world, ProjectId, ConfigId, ConfigFingerprint);
    private static string? Option(string[] args, string name) { var i = Array.IndexOf(args, name); return i >= 0 && i + 1 < args.Length ? args[i + 1] : null; }
}

internal static class M044WorldObservations
{
    public static (long clock, long sequence, IReadOnlyList<string> eventIds) SequenceForM044(this SimulationWorld world) => (world.Clock.Now.Microseconds, world.Events.Count == 0 ? 0 : world.Events.Max(x => x.Sequence), world.Events.Select(x => x.Id).ToArray());
}
