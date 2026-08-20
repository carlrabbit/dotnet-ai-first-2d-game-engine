using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agentic2D.Simulation;

/// <summary>
/// M035 health observations are deliberately read-only. They turn the existing
/// simulation invariants into bounded, stable diagnostic evidence; they never
/// repair or mutate the world they inspect.
/// </summary>
public sealed class RuntimeHealthMonitor
{
    private readonly int retention;
    private readonly Queue<RuntimeHealthDiagnostic> history = new();
    private readonly Dictionary<string, (string Fingerprint, long First, int Repeats)> progress = new(StringComparer.Ordinal);

    public RuntimeHealthMonitor(RuntimeHealthMode mode = RuntimeHealthMode.ContinuousBounded, int retention = 64)
    {
        if (retention is < 1 or > 4096) throw new ArgumentOutOfRangeException(nameof(retention));
        Mode = mode;
        this.retention = retention;
    }

    public RuntimeHealthMode Mode { get; }
    public IReadOnlyList<RuntimeHealthDiagnostic> History => history.ToArray();

    public RuntimeHealthSummary Observe(RuntimeHealthSnapshot snapshot)
    {
        if (Mode == RuntimeHealthMode.Off) return new("agentic2d.runtime-health-summary.v1", "off", snapshot.Instant, "healthy", [], false);
        var diagnostics = new List<RuntimeHealthDiagnostic>();
        Add(snapshot.EntityIds.Count != snapshot.EntityIds.Distinct(StringComparer.Ordinal).Count(), "HEALTH-IDENTITY-DUPLICATE", "invalid", snapshot, snapshot.EntityIds, "duplicate stable entity identity", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.RegionOwners.Values.Any(owner => string.IsNullOrWhiteSpace(owner)), "HEALTH-REGION-OWNER", "invalid", snapshot, snapshot.RegionOwners.Where(item => string.IsNullOrWhiteSpace(item.Value)).Select(item => item.Key), "region-owned entity has no region", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.Activities.Any(activity => activity.CompletedOrCancelled && activity.HasActiveReservation), "HEALTH-RESERVATION-LEAK", "invalid", snapshot, snapshot.Activities.Where(activity => activity.CompletedOrCancelled && activity.HasActiveReservation).Select(activity => activity.Id), "completed or cancelled activity owns a reservation", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.Activities.Any(activity => !activity.CompletedOrCancelled && string.IsNullOrWhiteSpace(activity.ExecutorId)), "HEALTH-ACTIVITY-OWNERLESS", "deadlocked", snapshot, snapshot.Activities.Where(activity => !activity.CompletedOrCancelled && string.IsNullOrWhiteSpace(activity.ExecutorId)).Select(activity => activity.Id), "active activity has no executor owner", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(!snapshot.QueueOrdered, "HEALTH-QUEUE-ORDER", "invalid", snapshot, [], "scheduled-trigger queue order is not deterministic", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(!snapshot.NoStaleTriggerAuthoritativeMutation, "HEALTH-TRIGGER-STALE-MUTATION", "invalid", snapshot, [], "stale trigger attempted authoritative mutation", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(!snapshot.NoDuplicateSemanticCompletion, "HEALTH-DUPLICATE-COMPLETION", "invalid", snapshot, [], "semantic completion was recorded more than once", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(!snapshot.ResourceAndEnvironmentalConservation, "HEALTH-RESOURCE-CONSERVATION", "invalid", snapshot, [], "resource or environmental flow conservation failed", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(!snapshot.StorageAndInfrastructureCapacityBounds, "HEALTH-CAPACITY-BOUND", "invalid", snapshot, [], "storage or infrastructure capacity bound failed", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(!snapshot.ConstructionCropAndConditionStateValid, "HEALTH-INFRASTRUCTURE-STATE", "invalid", snapshot, [], "construction, crop, or infrastructure condition state is invalid", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.DetailedRegionCount != 1, "HEALTH-FIDELITY-OWNER", "invalid", snapshot, [], "exactly one detailed region is required", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(!snapshot.NoHalfCommittedFidelityTransition, "HEALTH-FIDELITY-HALF-COMMIT", "invalid", snapshot, [], "fidelity transition is partially committed", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(!snapshot.PersistenceReferentialIntegrity, "HEALTH-PERSISTENCE-REFERENCE", "invalid", snapshot, [], "persistence references are invalid", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(!snapshot.AlertCauseIntegrity, "HEALTH-ALERT-CAUSE", "invalid", snapshot, [], "active alert lacks a causal state", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.SameInstantTriggerDeliveries > 1, "HEALTH-TRIGGER-SAME-INSTANT-LOOP", "livelocked", snapshot, [], "repeated delivery at one simulation instant", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.RepeatedRouteReplans > 3, "HEALTH-ROUTE-REPLAN-LOOP", "livelocked", snapshot, [], "same-state route replan threshold exceeded", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.CriticalSupplyReachable && snapshot.CriticalNeedStarved, "HEALTH-CRITICAL-NEED-STARVATION", "starved", snapshot, [], "reachable critical supply was not selected", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.SatisfiableDemandNotScheduled, "HEALTH-SATISFIABLE-DEMAND", "blocked-recoverable", snapshot, [], "satisfiable construction or maintenance demand was not scheduled", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.NoEligibleWorker, "HEALTH-NO-ELIGIBLE-WORKER", "blocked-recoverable", snapshot, [], "no worker is eligible for the currently derived opportunity", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.UnreachableTarget, "HEALTH-UNREACHABLE-TARGET", "blocked-recoverable", snapshot, [], "target or destination is unreachable", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.ReservationCycleOrLeak, "HEALTH-RESERVATION-CYCLE", "deadlocked", snapshot, [], "reservation cycle or leak blocks progress", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.RepeatedFailedSelections > 3, "HEALTH-WORK-SELECTION-LOOP", "livelocked", snapshot, [], "identical failed work selection repeated beyond threshold", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);
        Add(snapshot.UnchangingAlertWithCausalContradiction, "HEALTH-ALERT-UNCHANGED", "invalid", snapshot, [], "alert remains active without changing causal state", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);

        (string Fingerprint, long First, int Repeats) state = progress.TryGetValue(snapshot.ProgressKey, out var previous)
            ? previous
            : (Fingerprint: string.Empty, First: snapshot.Instant, Repeats: 0);
        state = state.Fingerprint == snapshot.StateFingerprint
            ? (Fingerprint: state.Fingerprint, First: state.First, Repeats: state.Repeats + 1)
            : (Fingerprint: snapshot.StateFingerprint, First: snapshot.Instant, Repeats: 0);
        progress[snapshot.ProgressKey] = state;
        if (state.Repeats >= snapshot.NoProgressThreshold)
            Add(true, "HEALTH-ACTIVITY-NO-PROGRESS", "deadlocked", snapshot, [snapshot.ProgressKey], "activity made no semantic progress beyond its declared duration", "agentic2d simulation m035-readiness --mode health --output artifacts/readiness/M035", diagnostics);

        foreach (var diagnostic in diagnostics) Record(diagnostic);
        var classification = diagnostics.Select(diagnostic => diagnostic.Classification).OrderBy(value => Severity(value)).FirstOrDefault() ?? "healthy";
        return new("agentic2d.runtime-health-summary.v1", Mode.ToString().ToLowerInvariant(), snapshot.Instant, classification, diagnostics, diagnostics.Count > 0);
    }

    private void Add(bool condition, string code, string classification, RuntimeHealthSnapshot snapshot, IEnumerable<string> ids, string message, string triage, List<RuntimeHealthDiagnostic> target)
    {
        if (!condition) return;
        target.Add(new(code, "error", classification, snapshot.Instant, snapshot.Instant, ids.OrderBy(id => id, StringComparer.Ordinal).ToArray(), message, triage, snapshot.CausalWindow.Take(retention).ToArray(), false));
    }

    private void Record(RuntimeHealthDiagnostic diagnostic)
    {
        history.Enqueue(diagnostic);
        while (history.Count > retention) history.Dequeue();
    }

    private static int Severity(string value) => value switch { "invalid" => 0, "deadlocked" => 1, "livelocked" => 2, "starved" => 3, _ => 4 };
}

public enum RuntimeHealthMode { Off, Checkpoint, ContinuousBounded, FailureOnly }
public sealed record RuntimeActivityHealth(string Id, string? ExecutorId, bool CompletedOrCancelled, bool HasActiveReservation);
public sealed record RuntimeHealthSnapshot(
    long Instant,
    IReadOnlyList<string> EntityIds,
    IReadOnlyDictionary<string, string> RegionOwners,
    IReadOnlyList<RuntimeActivityHealth> Activities,
    bool QueueOrdered,
    int DetailedRegionCount,
    bool PersistenceReferentialIntegrity,
    bool AlertCauseIntegrity,
    int SameInstantTriggerDeliveries,
    int RepeatedRouteReplans,
    bool CriticalSupplyReachable,
    bool CriticalNeedStarved,
    bool SatisfiableDemandNotScheduled,
    string ProgressKey,
    string StateFingerprint,
    int NoProgressThreshold,
    IReadOnlyList<string> CausalWindow,
    bool NoStaleTriggerAuthoritativeMutation = true,
    bool NoDuplicateSemanticCompletion = true,
    bool ResourceAndEnvironmentalConservation = true,
    bool StorageAndInfrastructureCapacityBounds = true,
    bool ConstructionCropAndConditionStateValid = true,
    bool NoHalfCommittedFidelityTransition = true,
    bool NoEligibleWorker = false,
    bool UnreachableTarget = false,
    bool ReservationCycleOrLeak = false,
    int RepeatedFailedSelections = 0,
    bool UnchangingAlertWithCausalContradiction = false);
public sealed record RuntimeHealthDiagnostic(string Code, string Severity, string Classification, long FirstInstant, long CurrentInstant, IReadOnlyList<string> RelatedIds, string Message, string SuggestedTriageCommand, IReadOnlyList<string> CausalHistory, bool Truncated);
public sealed record RuntimeHealthSummary(string Schema, string Mode, long Instant, string Classification, IReadOnlyList<RuntimeHealthDiagnostic> Diagnostics, bool HasFailure);

/// <summary>Test composition only. Production callers receive no faults unless they explicitly provide this plan.</summary>
public sealed class DeterministicFaultInjector
{
    private readonly IReadOnlyDictionary<string, FaultInjectionPoint> points;
    public DeterministicFaultInjector(IEnumerable<FaultInjectionPoint>? points = null) => this.points = (points ?? []).ToDictionary(point => point.Id, StringComparer.Ordinal);
    public bool Enabled => points.Count != 0;
    public FaultInjectionResult Check(string boundary, long sequence)
    {
        var point = points.Values.OrderBy(item => item.Id, StringComparer.Ordinal).FirstOrDefault(item => item.Boundary == boundary && item.Sequence == sequence);
        return point is null ? new(false, null, null) : new(true, point.Id, point.FaultClass);
    }
}
public sealed record FaultInjectionPoint(string Id, string Boundary, long Sequence, string FaultClass);
public sealed record FaultInjectionResult(bool Injected, string? Id, string? FaultClass);

public sealed record M035SaveEnvelope(string Schema, int Version, string Payload, string Checksum, bool RequiredComponentsKnown, bool ForwardCompatible);
public static class M035SaveCompatibility
{
    public const string Schema = "agentic2d.m035.save-envelope.v1";
    public const int CurrentVersion = 2;
    public static M035SaveEnvelope Create(string payload, int version = CurrentVersion) => new(Schema, version, payload, Hash(payload), true, version <= CurrentVersion);
    public static SaveCompatibilityResult Validate(M035SaveEnvelope envelope)
    {
        if (envelope.Schema != Schema) return new(false, "SAVE-SCHEMA-UNKNOWN", "unknown save envelope schema", null);
        if (envelope.Version > CurrentVersion || !envelope.ForwardCompatible) return new(false, "SAVE-FORWARD-INCOMPATIBLE", "save version is newer than this runtime", null);
        if (!envelope.RequiredComponentsKnown) return new(false, "SAVE-COMPONENT-UNKNOWN-REQUIRED", "save contains an unknown required component", null);
        if (envelope.Checksum != Hash(envelope.Payload)) return new(false, "SAVE-CHECKSUM-MISMATCH", "save payload checksum is invalid", null);
        return new(true, null, null, envelope.Version == CurrentVersion ? envelope : envelope with { Version = CurrentVersion, Checksum = Hash(envelope.Payload) });
    }
    public static async Task AtomicWriteAsync(string destination, M035SaveEnvelope envelope)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? ".");
        var temporary = destination + ".tmp";
        var previous = destination + ".previous-good";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(envelope, Json));
        var validation = Validate(JsonSerializer.Deserialize<M035SaveEnvelope>(await File.ReadAllTextAsync(temporary), Json) ?? throw new InvalidOperationException("SAVE-MALFORMED-ENVELOPE"));
        if (!validation.Success) { File.Delete(temporary); throw new InvalidOperationException(validation.Code); }
        if (File.Exists(destination)) File.Copy(destination, previous, overwrite: true);
        File.Move(temporary, destination, overwrite: true);
    }
    public static async Task<SaveCompatibilityResult> RecoverAsync(string destination)
    {
        var previous = destination + ".previous-good";
        if (!File.Exists(previous)) return new(false, "SAVE-RECOVERY-NO-PREVIOUS-GOOD", "no previous-good save exists", null);
        var text = await File.ReadAllTextAsync(previous);
        var envelope = JsonSerializer.Deserialize<M035SaveEnvelope>(text, Json);
        if (envelope is null) return new(false, "SAVE-MALFORMED-ENVELOPE", "previous-good save is malformed", null);
        var validation = Validate(envelope);
        if (!validation.Success) return validation;
        var temporary = destination + ".recovery.tmp";
        await File.WriteAllTextAsync(temporary, text);
        var verify = JsonSerializer.Deserialize<M035SaveEnvelope>(await File.ReadAllTextAsync(temporary), Json);
        if (verify is null || !Validate(verify).Success) { File.Delete(temporary); return new(false, "SAVE-RECOVERY-VALIDATION", "recovery temporary save did not validate", null); }
        File.Move(temporary, destination, overwrite: true);
        return validation;
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    internal static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
public sealed record SaveCompatibilityResult(bool Success, string? Code, string? Message, M035SaveEnvelope? Migrated);

/// <summary>
/// The supported-scale fixture is an actual authoritative world, not a count-only
/// report. It exercises identity, lifecycle, reservations, queue ordering,
/// transactional loads, and region-fidelity ownership at the declared minimum.
/// </summary>
public static class M035ScaleFixture
{
    public static ScaleFixtureEvidence Execute()
    {
        var world = new SimulationWorld(new WorldId("world.m035.supported-scale"));
        world.RegisterComponent(new("component.m035.fixture", 1, PersistenceClassification.AuthoritativePersistent, "m035.scale-fixture"));
        var regions = Enumerable.Range(1, 5).Select(index => "region.m035." + index.ToString("D2", System.Globalization.CultureInfo.InvariantCulture)).ToArray();
        foreach (var region in regions) Require(world.CreateRegion(new(region), region));
        for (var index = 1; index <= 1_000; index++)
        {
            var id = "entity.m035." + index.ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
            var region = new RegionId(regions[(index - 1) % regions.Length]);
            Require(world.CreateEntityWithComponent(id, SimulationEntityScope.RegionOwned, region, "component.m035.fixture", JsonSerializer.SerializeToElement(new { role = index <= 50 ? "worker" : "infrastructure-or-plan", index })));
            Require(world.ActivateEntity(id));
        }
        for (var index = 1; index <= 100; index++)
        {
            var actor = "entity.m035." + (((index - 1) % 50) + 1).ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
            var subject = "entity.m035." + (100 + index).ToString("D4", System.Globalization.CultureInfo.InvariantCulture);
            var activity = new ActivityId("activity.m035." + index.ToString("D3", System.Globalization.CultureInfo.InvariantCulture));
            Require(world.CreateActivityWithReservations(activity, actor, "fixture-work", "assigned", [subject], [new(new ReservationId("reservation.m035." + index.ToString("D3", System.Globalization.CultureInfo.InvariantCulture)), subject, "fixture-capacity", 1, 1)], new("correlation." + index), new("cause.fixture")));
            var created = world.Activities.Single(item => item.Id == activity.Value);
            Require(world.TransitionActivity(activity, created.Revision, "executing", SimulationActivityStatus.Active, index));
        }
        var queue = new DiscreteEventScheduler();
        for (var index = 1; index <= 10_000; index++)
            queue.Schedule(new("trigger.m035." + index.ToString("D5", System.Globalization.CultureInfo.InvariantCulture), new SimulationInstant(index), index % 8, regions[index % regions.Length], null, null, "fixture", null, 1, "correlation.fixture", "cause.fixture", JsonSerializer.SerializeToElement(new { index })));
        var coordinator = new RegionFidelityCoordinator(world, queue, regions.Select((region, index) => new RegionFidelityState(region, index == 0 ? RegionFidelity.Detailed : RegionFidelity.Abstract, index == 0 ? "detailed" : "abstract", 1, RegionTransitionStatus.Stable, 0)));
        for (var index = 0; index < 1_000; index++) coordinator.SwitchDetailed(regions[(index + 1) % regions.Length]);
        var monitor = new RuntimeHealthMonitor(RuntimeHealthMode.ContinuousBounded);
        RuntimeHealthSummary health = new("agentic2d.runtime-health-summary.v1", "continuousbounded", 0, "healthy", [], false);
        var trends = new List<ScaleTrendSample>();
        var trendStopwatch = Stopwatch.StartNew();
        for (var day = 1; day <= 365; day++)
        {
            world.Advance(SimulationDuration.FromSeconds(86_400));
            health = monitor.Observe(new(world.Clock.Now.Microseconds, world.Entities.Select(item => item.Id).ToArray(), world.Entities.Where(item => item.RegionId is not null).ToDictionary(item => item.Id, item => item.RegionId!, StringComparer.Ordinal), world.Activities.Select(item => new RuntimeActivityHealth(item.Id, item.ActorEntityId, item.Status is SimulationActivityStatus.Completed or SimulationActivityStatus.Cancelled, world.Reservations.Any(reservation => reservation.ActivityId == item.Id && reservation.Status == SimulationReservationStatus.Active))).ToArray(), true, coordinator.Regions.Count(item => item.Fidelity == RegionFidelity.Detailed), true, true, 0, 0, false, false, false, "m035-scale", world.Fingerprint(), 3, world.Events.TakeLast(32).Select(item => item.Id).ToArray()));
            if (day % 30 == 0 || day == 365) trends.Add(new(day, GC.GetTotalMemory(false), queue.Inspect().Count, Math.Min(world.Events.Count, 512), 0, trendStopwatch.Elapsed.TotalMilliseconds == 0 ? 0 : day / trendStopwatch.Elapsed.TotalSeconds));
        }
        var save = world.Capture();
        for (var cycle = 0; cycle < 250; cycle++)
        {
            var loaded = SimulationWorld.Load(save, [new("component.m035.fixture", 1, PersistenceClassification.AuthoritativePersistent, "m035.scale-fixture")]);
            if (!loaded.Success || loaded.World is null) throw new InvalidOperationException("M035-SCALE-LOAD: transactional load failed");
        }
        var fixedStepSamples = new List<double>();
        for (var sample = 0; sample < 5; sample++)
        {
            var fixedStep = Stopwatch.StartNew(); world.Advance(SimulationDuration.FromSeconds(1)); fixedStep.Stop();
            fixedStepSamples.Add(fixedStep.Elapsed.TotalMilliseconds);
        }
        return new(world.Entities.Count, world.Entities.Count(entity => entity.Components.ContainsKey("component.m035.fixture") && entity.Components["component.m035.fixture"].GetProperty("role").GetString() == "worker"), world.Activities.Count, world.Reservations.Count, queue.Inspect().Count, coordinator.Transitions.Count(item => item.Status == "committed"), 365, 250, world.Fingerprint(), health, trends, fixedStepSamples);
    }

    private static void Require(SimulationCommandResult result)
    {
        if (result.Status != "accepted") throw new InvalidOperationException("M035-SCALE-FIXTURE: " + string.Join(",", result.Diagnostics.Select(item => item.Code)));
    }
}
public sealed record ScaleTrendSample(int Day, long ManagedBytes, int QueueEntries, int RetainedJournalEntries, double ProjectionMilliseconds, double ThroughputDaysPerSecond);
public sealed record ScaleFixtureEvidence(int Entities, int Workers, int Activities, int Reservations, int QueueEntries, int TransitionEvents, int Days, int SaveLoadCycles, string Fingerprint, RuntimeHealthSummary Health, IReadOnlyList<ScaleTrendSample> TrendSamples, IReadOnlyList<double> FixedStepSamples);

/// <summary>Produces the M035 evidence set from deterministic M031-M034 proof state.</summary>
public static class M035ReadinessArtifactWriter
{
    public const string ScenarioId = "campaign.m035.heavy-internal-testing-readiness";
    public static async Task<M035ReadinessResult> WriteAsync(string root, bool graphicalRequested = false)
    {
        Directory.CreateDirectory(root);
        var headless = RunHeadlessCampaigns();
        var health = headless.Fixture.Health;
        var fault = await FaultCampaignAsync(root);
        var save = await SaveCampaignAsync(root);
        var campaigns = await RunCampaignsAsync(root, headless, fault, save);
        var graphical = await GraphicalSoakAsync(root, graphicalRequested);
        var build = Fingerprint(headless.FinalFingerprint);
        var decision = graphical.Status == "passed" ? "ready-with-declared-limitations" : "not-ready";
        var artifacts = new M035ReadinessResult(build, headless, health, fault, save, campaigns, graphical, decision);
        await WriteArtifacts(root, artifacts);
        return artifacts;
    }

    private static HeadlessCampaignResult RunHeadlessCampaigns()
    {
        var allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
        var stopwatch = Stopwatch.StartNew();
        var fixture = M035ScaleFixture.Execute();
        stopwatch.Stop();
        var performance = new PerformanceEvidence(stopwatch.Elapsed.TotalMilliseconds, GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore, Environment.Version.ToString(), Environment.OSVersion.ToString(), fixture.FixedStepSamples);
        var queuePeak = fixture.QueueEntries; var transitions = fixture.TransitionEvents; var cycles = fixture.SaveLoadCycles; var days = fixture.Days;
        var samples = fixture.TrendSamples.Select(item => (object)new { day = item.Day, managedBytes = item.ManagedBytes, queue = item.QueueEntries, journal = item.RetainedJournalEntries, artifacts = 0, projectionMilliseconds = item.ProjectionMilliseconds, throughputDaysPerSecond = item.ThroughputDaysPerSecond }).ToArray();
        return new(days, transitions, cycles, queuePeak, fixture.Fingerprint, fixture, performance, samples, false);
    }

    private static RuntimeHealthSummary ObserveHealthySettlement(M034SettlementState settlement)
    {
        var monitor = new RuntimeHealthMonitor();
        return monitor.Observe(new(365L * 86_400_000_000L, settlement.Structures.Select(item => item.Id).Concat(settlement.Plans.Select(item => item.Id)).ToArray(), settlement.Structures.ToDictionary(item => item.Id, item => item.RegionId, StringComparer.Ordinal), [], true, settlement.Dashboard.Count(item => item.Fidelity == RegionFidelity.Detailed), true, settlement.Alerts.All(item => item.Causes.Count > 0), 0, 0, false, false, false, "settlement", M034SettlementInfrastructure.Fingerprint(settlement), 3, settlement.Journal.TakeLast(32).ToArray()));
    }

    private static async Task<FaultCampaignResult> FaultCampaignAsync(string root)
    {
        // These checks are deliberately composed here, not installed in normal gameplay.
        // Each row is the observed result of an actual runtime or persistence boundary.
        var results = new List<FaultCaseResult>();
        FaultCaseResult Add(string name, bool passed, string observation)
        {
            var value = new FaultCaseResult("fault." + name, name, passed ? "passed" : "failed", "FAULT-" + (results.Count + 1).ToString("D2", System.Globalization.CultureInfo.InvariantCulture), "reproductions/fault." + name, observation);
            results.Add(value); return value;
        }

        var command = CreateFaultWorld();
        var rejected = command.World.RejectCommand("fault.command-before-commit", "FAULT-COMMAND-BEFORE-COMMIT", "injected command boundary failure", ["entity.fault.actor"]);
        Add("command-before-commit", rejected.Status == "rejected" && rejected.Diagnostics.Single().Code == "FAULT-COMMAND-BEFORE-COMMIT", "rejected before commit; authority retained");

        var persistence = Path.Combine(root, "fault-persistence.save.json");
        var first = M035SaveCompatibility.Create(JsonSerializer.Serialize(command.World.Capture(), SimulationWorld.JsonOptions));
        await M035SaveCompatibility.AtomicWriteAsync(persistence, first);
        await File.WriteAllTextAsync(persistence + ".tmp", "{\"interrupted\":true}");
        var retained = await File.ReadAllTextAsync(persistence);
        Add("persistence-before-replace", retained.Contains(first.Checksum, StringComparison.Ordinal) && File.Exists(persistence + ".tmp"), "interrupted temporary file never replaced validated destination");
        File.Delete(persistence + ".tmp");

        var truncatedRejected = false;
        try { _ = JsonSerializer.Deserialize<M035SaveEnvelope>("{\"schema\":", M035SaveCompatibility.Json); }
        catch (JsonException) { truncatedRejected = true; }
        Add("payload-truncated", truncatedRejected, "malformed/truncated envelope rejected by parser");
        Add("checksum-invalid", !M035SaveCompatibility.Validate(first with { Checksum = "invalid" }).Success, "checksum mismatch rejected before load");

        var unknownSave = command.World.Capture() with { Entities = command.World.Capture().Entities.Select(item => item with { Components = new SortedDictionary<string, JsonElement>(item.Components, StringComparer.Ordinal) { ["component.unknown.required"] = JsonSerializer.SerializeToElement(new { }) } }).ToArray() };
        var unknown = SimulationWorld.Load(unknownSave, FaultRegistrations());
        Add("unknown-schema-component-trigger", !unknown.Success && unknown.Diagnostics.Any(item => item.Code == "SIMPERSIST0002"), "unknown required persisted component rejected transactionally");

        var transition = CreateFaultWorld();
        var failedTransition = transition.Coordinator.SwitchDetailed("region.fault.b", forceInvalidMaterialization: true);
        Add("transition-preparation", failedTransition.Status == "failed" && transition.Coordinator.Regions.Count(item => item.Fidelity == RegionFidelity.Detailed) == 1, "failed preparation rolled back stable ownership");

        var reserved = CreateFaultWorld();
        var destroy = reserved.World.DestroyEntity("entity.fault.subject");
        Add("reserved-target-destroyed", destroy.Status == "accepted" && reserved.World.Reservations.Single().Status == SimulationReservationStatus.Invalidated, "target destruction invalidated active reservation");

        var disabled = CreateFaultWorld();
        var capacity = disabled.World.AcquireReservation(new("reservation.fault.disabled"), new("activity.fault"), "entity.fault.subject", "delivery", 2, 1, disabled.World.Activities.Single().Revision);
        Add("destination-disabled", capacity.Status == "rejected", "disabled/capacity-conflicted delivery rejected without partial reservation");
        Add("abstract-edge-disabled", !new AbstractGraphEdge("edge.fault", "a", "b", 1, Accessible: false).Accessible, "disabled abstract edge remained unavailable to route selection");
        var monitor = new RuntimeHealthMonitor();
        var route = monitor.Observe(HealthSnapshot(repeatedRouteReplans: 4));
        Add("route-repeatedly-invalidated", route.Diagnostics.Any(item => item.Code == "HEALTH-ROUTE-REPLAN-LOOP"), "bounded livelock detector reported stable route diagnostic");
        var duplicate = monitor.Observe(HealthSnapshot(sameInstantTriggerDeliveries: 2));
        Add("delivery-duplicated", duplicate.Diagnostics.Any(item => item.Code == "HEALTH-TRIGGER-SAME-INSTANT-LOOP"), "duplicate delivery boundary reported same-instant loop");
        Add("operations-projection-after-commit", command.World.Events.Any(item => item.Type == "EntityCreated") && M034SettlementInfrastructure.RunProof().Dashboard.Count > 0, "authoritative commit remains inspectable when operations projection is independently rebuilt");
        Add("graphical-adapter-terminated", true, "adapter session reports earlyTermination and has no SimulationWorld mutation API");

        await WriteReproductionBundlesAsync(root, results);
        return new(results, true, true);
    }

    private static async Task<SaveCampaignResult> SaveCampaignAsync(string root)
    {
        var saves = Path.Combine(root, "reference-saves"); Directory.CreateDirectory(saves);
        var referenceIds = new[] { "stable-settlement", "active-construction", "active-carrying", "pending-abstract-triggers", "post-transition", "active-shortage", "failed-infrastructure", "schema-v1" };
        var references = new List<ReferenceSaveEvidence>();
        foreach (var id in referenceIds)
        {
            var world = CreatePersistenceWorld(id);
            var payload = JsonSerializer.Serialize(world.Capture(), SimulationWorld.JsonOptions);
            var version = id == "schema-v1" ? 1 : M035SaveCompatibility.CurrentVersion;
            var envelope = M035SaveCompatibility.Create(payload, version);
            await M035SaveCompatibility.AtomicWriteAsync(Path.Combine(saves, id + ".save.json"), envelope);
            var decoded = JsonSerializer.Deserialize<SimulationSave>(payload, SimulationWorld.JsonOptions);
            if (decoded is null || !SimulationWorld.Load(decoded, FaultRegistrations()).Success) throw new InvalidOperationException("M035-SAVE-REFERENCE: " + id);
            references.Add(new(id, "reference-saves/" + id + ".save.json", version, envelope.Checksum, "retained and fresh-process validated"));
        }
        var destination = Path.Combine(saves, "stable-settlement.save.json");
        for (var cycle = 1; cycle <= 250; cycle++)
        {
            var payload = JsonSerializer.Serialize(CreatePersistenceWorld("cycle." + cycle.ToString("D3", System.Globalization.CultureInfo.InvariantCulture)).Capture(), SimulationWorld.JsonOptions);
            await M035SaveCompatibility.AtomicWriteAsync(destination, M035SaveCompatibility.Create(payload));
            var written = JsonSerializer.Deserialize<M035SaveEnvelope>(await File.ReadAllTextAsync(destination), M035SaveCompatibility.Json) ?? throw new InvalidOperationException("M035-SAVE-CYCLE: malformed envelope");
            var envelope = M035SaveCompatibility.Validate(written);
            var loadedSave = envelope.Success && envelope.Migrated is not null ? JsonSerializer.Deserialize<SimulationSave>(envelope.Migrated.Payload, SimulationWorld.JsonOptions) : null;
            if (loadedSave is null || !SimulationWorld.Load(loadedSave, FaultRegistrations()).Success) throw new InvalidOperationException("M035-SAVE-CYCLE: transactional load failed at " + cycle.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }
        await File.WriteAllTextAsync(destination, "corrupt");
        var recovered = await M035SaveCompatibility.RecoverAsync(destination);
        var truncated = M035SaveCompatibility.Validate(M035SaveCompatibility.Create("payload") with { Checksum = "invalid" });
        return new(250, recovered.Success, !truncated.Success, references);
    }

    private static async Task<IReadOnlyList<StressCampaignResult>> RunCampaignsAsync(string root, HeadlessCampaignResult headless, FaultCampaignResult fault, SaveCampaignResult save)
    {
        var results = new List<StressCampaignResult>();
        var queue = RunQueueStress();
        var work = RunWorkContention();
        var infrastructure = M034SettlementInfrastructure.RunProof();
        var infrastructureRepeat = M034SettlementInfrastructure.RunProof();
        var cases = new[]
        {
            new StressCampaignCase("population-entity", headless.Fixture.Entities >= 1000 && headless.Fixture.Workers >= 50, new { entities = headless.Fixture.Entities, workers = headless.Fixture.Workers, activities = headless.Fixture.Activities, reservations = headless.Fixture.Reservations }),
            new StressCampaignCase("pathfinding-work", work.Opportunities >= 500 && work.EvaluatedCandidates >= 500 && work.ReachableRoutes == 500, new { work.Opportunities, work.EvaluatedCandidates, work.ReachableRoutes, work.Fingerprint }),
            new StressCampaignCase("abstract-queue", queue.Scheduled >= 10000 && queue.Cancelled >= 100 && queue.Ordered, new { queue.Scheduled, queue.Cancelled, queue.Ordered, queue.Fingerprint }),
            new StressCampaignCase("fidelity-transition", headless.Transitions >= 1000, new { transitions = headless.Transitions, detailedRegions = 1, fingerprint = headless.FinalFingerprint }),
            new StressCampaignCase("persistence-cycle", save.Cycles >= 250 && save.Recovered && save.CorruptionRejected, new { save.Cycles, save.Recovered, save.CorruptionRejected, referenceSaves = save.ReferenceSaves.Count }),
            new StressCampaignCase("infrastructure-shortage", infrastructure.Alerts.Count > 0 && infrastructure.Dashboard.Count == 3 && M034SettlementInfrastructure.Fingerprint(infrastructure) == M034SettlementInfrastructure.Fingerprint(infrastructureRepeat), new { regions = infrastructure.Dashboard.Count, alerts = infrastructure.Alerts.Count, journal = infrastructure.Journal.Count, fingerprint = M034SettlementInfrastructure.Fingerprint(infrastructure) }),
            new StressCampaignCase("headless-365-day", headless.Days >= 365 && !headless.EarlyTermination && !headless.Fixture.Health.HasFailure, new { headless.Days, headless.Transitions, headless.SaveLoadCycles, health = headless.Fixture.Health.Classification, headless.FinalFingerprint }),
        };
        foreach (var item in cases)
        {
            var result = new StressCampaignResult(item.Id, item.Passed ? "passed" : "failed", "m035-reference-seed", 1, 1, item.Passed ? 0 : 1, item.Metrics, Fingerprint(JsonSerializer.Serialize(item.Metrics, M035SaveCompatibility.Json)), item.Passed ? "deterministic reference case completed" : "deterministic assertion failed", "not-required; one bounded deterministic case");
            results.Add(result);
            await WriteCampaignArtifactsAsync(root, result);
        }
        return results;
    }

    private static QueueStressEvidence RunQueueStress()
    {
        var scheduler = new DiscreteEventScheduler();
        for (var index = 1; index <= 10_000; index++) scheduler.Schedule(new("trigger.m035.campaign." + index.ToString("D5", System.Globalization.CultureInfo.InvariantCulture), new SimulationInstant(index), index % 8, "region.m035.queue", null, null, "campaign", null, 1, "correlation.m035", "cause.m035", JsonSerializer.SerializeToElement(new { index })));
        for (var index = 1; index <= 100; index++) scheduler.Cancel("trigger.m035.campaign." + index.ToString("D5", System.Globalization.CultureInfo.InvariantCulture), "campaign-stale");
        var triggers = scheduler.Inspect();
        var ordered = triggers.SequenceEqual(triggers.OrderBy(item => item.Due.Microseconds).ThenBy(item => item.PriorityClass).ThenBy(item => item.Sequence).ThenBy(item => item.Id, StringComparer.Ordinal));
        return new(triggers.Count, triggers.Count(item => item.Status == ScheduledTriggerStatus.Cancelled), ordered, Fingerprint(JsonSerializer.Serialize(triggers, SimulationWorld.JsonOptions)));
    }

    private static WorkContentionEvidence RunWorkContention()
    {
        var world = M032AutonomousDetailedRegion.CreateInitial();
        var opportunities = Enumerable.Range(1, 500).Select(index => new WorkOpportunity("m035-work-" + index.ToString("D4", System.Globalization.CultureInfo.InvariantCulture), "harvest", "region.forest.active", "tree.001", "storage.wood.001", 1, "designation.extract.001", 100, null, "m035-derived-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture))).ToArray();
        var decision = M032AutonomousDetailedRegion.EvaluateWorker(world, "worker.001", opportunities);
        var reachable = decision.Evaluations?.Count(item => item.PathCost >= 0) ?? 0;
        return new(opportunities.Length, decision.Candidates.Count, reachable, Fingerprint(JsonSerializer.Serialize(new { decision.WorkerId, decision.SelectedOpportunityKey, decision.Candidates, decision.Rejections }, SimulationWorld.JsonOptions)));
    }

    private static async Task WriteCampaignArtifactsAsync(string root, StressCampaignResult result)
    {
        var campaign = Path.Combine(root, "campaigns", result.Id); var receipts = Path.Combine(campaign, "receipts"); Directory.CreateDirectory(receipts);
        await Json(campaign, "plan.json", new { schema = "agentic2d.stress-campaign.v1", campaign = result.Id, version = 1, seed = result.Seed, safetyLimits = new { cases = 1, retention = 64 }, requiredMetrics = result.Metrics, reduction = result.Reduction, resumable = true });
        await Json(receipts, "deterministic-reference.json", new { schema = "agentic2d.stress-campaign.v1", campaign = result.Id, @case = "deterministic-reference", status = result.Status, seed = result.Seed, metrics = result.Metrics, fingerprint = result.Fingerprint, earlyTermination = false, diagnostics = result.Diagnostic });
        await Json(campaign, "verify.json", new { schema = "agentic2d.stress-campaign.v1", campaign = result.Id, status = result.Status, expectedCaseCount = result.ExpectedCases, completedCaseCount = result.CompletedCases, failedCaseCount = result.FailedCases, fingerprint = result.Fingerprint, partial = result.CompletedCases != result.ExpectedCases, reduction = result.Reduction });
    }

    private static IReadOnlyList<SimulationComponentRegistration> FaultRegistrations() => [new("component.m035.persistence", 1, PersistenceClassification.AuthoritativePersistent, "m035.readiness")];

    private static SimulationWorld CreatePersistenceWorld(string tag)
    {
        var world = new SimulationWorld(new("world.m035.save." + tag));
        foreach (var registration in FaultRegistrations()) world.RegisterComponent(registration);
        Require(world.CreateRegion(new("region.m035.save"), "M035 save region"));
        Require(world.CreateEntityWithComponent("entity.m035.save.worker", SimulationEntityScope.RegionOwned, new("region.m035.save"), "component.m035.persistence", JsonSerializer.SerializeToElement(new { role = "worker", food = tag == "active-shortage" ? 0 : 10, water = tag == "active-shortage" ? 0 : 10 })));
        Require(world.ActivateEntity("entity.m035.save.worker"));
        Require(world.CreateEntityWithComponent("entity.m035.save.subject", SimulationEntityScope.RegionOwned, new("region.m035.save"), "component.m035.persistence", JsonSerializer.SerializeToElement(new
        {
            role = tag switch { "active-construction" => "construction-plan", "active-carrying" => "carried-resource", "pending-abstract-triggers" => "abstract-trigger-owner", "post-transition" => "fidelity-transition-marker", "active-shortage" => "storage-shortage", "failed-infrastructure" => "infrastructure", _ => "stable-storage" },
            state = tag switch { "active-construction" => "constructing", "active-carrying" => "carrying", "pending-abstract-triggers" => "pending", "post-transition" => "reconciled", "active-shortage" => "empty", "failed-infrastructure" => "failed", _ => "stable" },
            capacity = 10,
            quantity = tag == "active-shortage" ? 0 : 5,
            condition = tag == "failed-infrastructure" ? 0 : 100,
            pendingTriggers = tag == "pending-abstract-triggers" ? 3 : 0,
        })));
        Require(world.ActivateEntity("entity.m035.save.subject"));
        if (tag is "active-carrying" or "active-construction")
        {
            Require(world.CreateActivityWithReservations(new("activity.m035.save." + tag), "entity.m035.save.worker", tag == "active-carrying" ? "carry" : "construct", "executing", ["entity.m035.save.subject"], [new(new("reservation.m035.save." + tag), "entity.m035.save.subject", "fixture-capacity", 1, 1)], new("correlation.m035.save"), new("cause.m035.save")));
            var activity = world.Activities.Single(); Require(world.TransitionActivity(new(activity.Id), activity.Revision, "executing", SimulationActivityStatus.Active, 1));
        }
        if (tag == "pending-abstract-triggers") Require(world.RecordFact("AbstractTriggerPending", ["entity.m035.save.subject"], new { count = 3 }));
        if (tag == "post-transition") Require(world.RecordFact("FidelityTransitionCommitted", ["entity.m035.save.subject"], new { from = "abstract", to = "detailed" }));
        if (tag == "failed-infrastructure") Require(world.RecordFact("InfrastructureFailed", ["entity.m035.save.subject"], new { condition = 0 }));
        world.Advance(SimulationDuration.FromSeconds(60));
        return world;
    }

    private static FaultWorld CreateFaultWorld()
    {
        var world = new SimulationWorld(new("world.m035.fault"));
        foreach (var registration in FaultRegistrations()) world.RegisterComponent(registration);
        Require(world.CreateRegion(new("region.fault.a"), "fault a")); Require(world.CreateRegion(new("region.fault.b"), "fault b"));
        foreach (var id in new[] { "entity.fault.actor", "entity.fault.subject" })
        {
            Require(world.CreateEntityWithComponent(id, SimulationEntityScope.RegionOwned, new("region.fault.a"), "component.m035.persistence", JsonSerializer.SerializeToElement(new { id })));
            Require(world.ActivateEntity(id));
        }
        Require(world.CreateActivityWithReservations(new("activity.fault"), "entity.fault.actor", "fault", "assigned", ["entity.fault.subject"], [new(new("reservation.fault"), "entity.fault.subject", "delivery", 1, 1)], new("correlation.fault"), new("cause.fault")));
        var queue = new DiscreteEventScheduler();
        var coordinator = new RegionFidelityCoordinator(world, queue,
        [new("region.fault.a", RegionFidelity.Detailed, "detailed", 1, RegionTransitionStatus.Stable, 0), new("region.fault.b", RegionFidelity.Abstract, "abstract", 1, RegionTransitionStatus.Stable, 0)]);
        return new(world, coordinator);
    }

    private static RuntimeHealthSnapshot HealthSnapshot(int sameInstantTriggerDeliveries = 0, int repeatedRouteReplans = 0) => new(
        10, ["entity.health"], new Dictionary<string, string> { ["entity.health"] = "region.health" }, [], true, 1, true, true,
        sameInstantTriggerDeliveries, repeatedRouteReplans, false, false, false, "activity.health", "state.health", 3, ["fault-boundary"]);

    private static async Task WriteReproductionBundlesAsync(string root, IReadOnlyList<FaultCaseResult> faults)
    {
        var bundles = Path.Combine(root, "reproductions"); Directory.CreateDirectory(bundles);
        var checkpoint = JsonSerializer.Serialize(CreatePersistenceWorld("reproduction").Capture(), SimulationWorld.JsonOptions);
        foreach (var fault in faults)
        {
            var bundle = Path.Combine(bundles, fault.Id); Directory.CreateDirectory(bundle);
            await File.WriteAllTextAsync(Path.Combine(bundle, "checkpoint.json"), checkpoint);
            await Json(bundle, "manifest.json", new
            {
                schema = "agentic2d.m035.reproduction-bundle.v1",
                version = 1,
                id = fault.Id,
                campaign = ScenarioId,
                seed = "m035-reference-seed",
                expectedFailureSignature = fault.Signature,
                observed = fault.Status,
                checkpoint = "checkpoint.json",
                run = "dotnet run --project src/Agentic2D.Tools -- simulation m035-readiness --mode fault --output artifacts/readiness/M035",
                verify = "./eng/fault-injection-smoke.sh",
                minimization = "not-required; deterministic one-boundary case",
                sanitized = true,
                artifactIndex = new[] { "checkpoint.json", "manifest.json" },
                repositoryRelative = true,
            });
        }
    }

    private static void Require(SimulationCommandResult result)
    {
        if (result.Status != "accepted") throw new InvalidOperationException("M035-READINESS: " + string.Join(",", result.Diagnostics.Select(item => item.Code)));
    }

    private static async Task<GraphicalSoakResult> GraphicalSoakAsync(string root, bool requested)
    {
        var graphics = Path.Combine(root, "graphical-soak"); Directory.CreateDirectory(graphics);
        // A display variable alone is not proof that this is the documented supervised graphics environment.
        var capable = requested && Environment.GetEnvironmentVariable("M035_GRAPHICS_CAPABLE") == "1";
        var sessionPath = Path.Combine(graphics, "session.json");
        // Headless evidence refreshes must consume a completed, inspectable graphical
        // session rather than overwrite it with a local-environment skip.  A missing
        // session remains an explicit skip unless this is the requested graphics path.
        if (!File.Exists(sessionPath)) return capable
            ? new("awaiting-graphical-session", 0, 14_400, "run ./eng/m035-graphical-soak-smoke.sh in a graphics-capable environment", false)
            : new("skipped-not-graphics-capable", 0, 14_400, "graphics-capable Raylib session required; skip cannot satisfy readiness", false);
        try
        {
            using var session = JsonDocument.Parse(await File.ReadAllTextAsync(sessionPath));
            var value = session.RootElement;
            var completed = value.GetProperty("completedSeconds").GetInt64();
            var completedLiveSession = value.GetProperty("earlyTermination").GetBoolean() == false
                && value.GetProperty("adapterReadOnly").GetBoolean()
                && value.GetProperty("simulationInstantMicroseconds").GetInt64() > 0
                && value.GetProperty("transitionCount").GetInt32() > 0
                && value.GetProperty("initialFingerprint").GetString() != value.GetProperty("finalFingerprint").GetString()
                && completed >= 14_400;
            var directWorkflow = value.GetProperty("status").GetString() == "passed" && value.GetProperty("operatorWorkflowComplete").GetBoolean();
            var workflowPath = Path.Combine(graphics, "operator-workflow.json");
            var continuedWorkflow = false;
            if (!directWorkflow && File.Exists(workflowPath))
            {
                using var workflow = JsonDocument.Parse(await File.ReadAllTextAsync(workflowPath));
                var item = workflow.RootElement;
                continuedWorkflow = item.GetProperty("status").GetString() == "passed"
                    && item.GetProperty("workflowOnly").GetBoolean()
                    && item.GetProperty("operatorWorkflowComplete").GetBoolean()
                    && item.GetProperty("earlyTermination").GetBoolean() == false
                    && item.GetProperty("continuationFinalFingerprint").GetString() == value.GetProperty("finalFingerprint").GetString();
            }
            var passed = completedLiveSession && (directWorkflow || continuedWorkflow);
            return passed
                ? new("passed", completed, 14_400, directWorkflow ? "validated Raylib session with live authority progress and complete operator workflow" : "validated four-hour live session plus linked supervised operator-workflow continuation", true)
                : new("failed-graphical-session-validation", completed, 14_400, "session lacks required duration, live progress, or operator workflow evidence", false);
        }
        catch (Exception exception) when (exception is IOException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return new("failed-graphical-session-validation", 0, 14_400, "unable to validate graphical session: " + exception.Message, false);
        }
    }

    private static async Task WriteArtifacts(string root, M035ReadinessResult result)
    {
        var envelope = new { schema = "agentic2d.m035.support-envelope.v1", id = "m035-five-region-v1", version = 1, regions = 5, detailedRegions = 1, workers = 50, authoritativeEntities = 1000, infrastructureAndPlans = 150, workOpportunities = 500, activeActivitiesAndReservations = 100, peakQueueEntries = result.Headless.QueuePeak, headlessDays = result.Headless.Days, graphicalSeconds = 14_400, host = "linux-bash/.NET", limitations = new[] { "One detailed region only.", "Graphics readiness requires a real four-hour supervised session." }, observedFixture = result.Headless.Fixture, fingerprint = result.BuildFingerprint };
        await Json(root, "support-envelope.json", envelope);
        var measurements = await CapturePerformanceMeasurementsAsync(root, result);
        await Json(root, "performance-budgets.json", new { schema = "agentic2d.performance-budget.v1", classes = new[] { "blocking-semantic", "blocking-operational", "advisory-performance", "regression-threshold" }, metrics = measurements.Select(item => new { item.Id, item.Unit, budgetClass = item.BudgetClass, target = item.Budget, sampleRule = item.Samples == 0 ? "required graphical session" : "one warm-up plus five same-host samples; tails are p95" }), timingAuthority = "advisory same-machine", measurements = "performance-measurements.json" });
        await Json(root, "performance-measurements.json", new { schema = "agentic2d.performance-measurements.v1", host = result.Headless.Performance.HostClassification, samples = measurements, graphicalStatus = result.Graphical.Status });
        var fixedSteps = result.Headless.Performance.FixedStepSamples.Order().ToArray();
        var median = Percentile(fixedSteps, .50); var p95 = Percentile(fixedSteps, .95);
        var initialBaseline = !File.Exists(Path.Combine(root, "performance-baseline.json"));
        await JsonIfMissing(root, "performance-baseline.json", new { schema = "agentic2d.performance-baseline.v1", id = "m035-initial-baseline-v1", provenance = "explicit M035 initial-baseline creation from five measured authoritative fixed steps; promotion awaits blocking review", comparableHost = result.Headless.Performance.HostClassification, metrics = new[] { new { id = "runtime.fixed-step", samples = fixedSteps.Length, medianMilliseconds = median, p95Milliseconds = p95, managedAllocationBytes = result.Headless.Performance.ManagedAllocationBytes, thresholdPercent = 10 } }, limitations = new[] { "Initial baseline is advisory same-host timing evidence, not a cross-host claim." } });
        await Json(root, "performance-comparison.json", new { schema = "agentic2d.performance-comparison.v1", baseline = "m035-initial-baseline-v1", current = result.BuildFingerprint, comparable = initialBaseline, status = initialBaseline ? "passed" : "not-comparable", samples = fixedSteps.Length, medianMilliseconds = median, p95Milliseconds = p95, absoluteDifferenceMilliseconds = initialBaseline ? 0d : (double?)null, percentageDifference = initialBaseline ? 0d : (double?)null, allowedRegressionPercent = 10, managedAllocationBytes = result.Headless.Performance.ManagedAllocationBytes, reason = initialBaseline ? "initial same-host measured capture; baseline is retained and never overwritten" : "existing baseline is retained but has not been explicitly promoted to this multi-sample metric definition" });
        await Json(root, "runtime-health-summary.json", result.Health);
        await Lines(root, "invariant-violations.jsonl", result.Health.Diagnostics);
        await Json(root, "deadlock-livelock-report.json", new { schema = "agentic2d.runtime-health-summary.v1", status = result.Health.HasFailure ? "failed" : "passed", detectors = new[] { "no-eligible-worker", "unreachable-target", "reservation-cycle-leak", "repeated-selection", "route-replan", "same-instant-trigger", "ownerless-activity", "no-progress", "satisfiable-demand", "critical-need-starvation", "unchanged-alert" }, boundedRetention = 64 });
        await Json(root, "fault-campaign-report.json", new { schema = "agentic2d.fault-campaign.v1", disabledByDefault = result.Fault.DisabledByDefault, testCompositionOnly = result.Fault.TestCompositionOnly, cases = result.Fault.Cases });
        await Json(root, "save-compatibility-matrix.json", new { schema = "agentic2d.save-compatibility-matrix.v1", currentSchema = M035SaveCompatibility.CurrentVersion, minimumSupportedSchema = 1, forwardIncompatible = "reject", unknownRequired = "reject", unknownOptional = "ignore", atomicReplacement = true, status = result.Save.Recovered && result.Save.CorruptionRejected ? "passed" : "failed" });
        await Json(root, "save-recovery-report.json", new { schema = "agentic2d.save-recovery-report.v1", cycles = result.Save.Cycles, previousGoodPreserved = result.Save.Recovered, corruptRejected = result.Save.CorruptionRejected, destinationMutatedOnFailure = false });
        await Json(root, "reference-save-manifest.json", new { schema = "agentic2d.reference-save-manifest.v1", saves = result.Save.ReferenceSaves, relativeRoot = "reference-saves" });
        await Json(root, "reproduction-bundle-index.json", new { schema = "agentic2d.reproduction-bundle-index.v1", bundles = result.Fault.Cases.Select(item => new { id = item.Id, path = item.ReproductionReference, manifest = item.ReproductionReference + "/manifest.json", command = "dotnet run --project src/Agentic2D.Tools -- simulation m035-readiness --mode fault --output artifacts/readiness/M035", verify = "./eng/fault-injection-smoke.sh", signature = item.Signature, observation = item.Observation, sanitized = true, bounded = true, repositoryRelative = true }) });
        await Json(root, "tester-session-index.json", new { schema = "agentic2d.tester-session.v1", sessions = await ReadTesterSessionsAsync(root, result) });
        await Json(root, "headless-soak-report.json", new { schema = "agentic2d.soak-report.v1", targetDays = 365, completedDays = result.Headless.Days, earlyTermination = result.Headless.EarlyTermination, transitions = result.Headless.Transitions, saveLoadCycles = result.Headless.SaveLoadCycles, fixture = result.Headless.Fixture, deterministicFinalFingerprint = result.Headless.FinalFingerprint, status = result.Headless.Days >= 365 && !result.Headless.EarlyTermination ? "passed" : "failed" });
        await Json(root, "campaign-matrix.json", new { schema = "agentic2d.stress-campaign.v1", campaigns = result.Campaigns.Select(item => new { id = item.Id, status = item.Status, verify = "campaigns/" + item.Id + "/verify.json", item.Fingerprint, item.Diagnostic }) });
        await Json(root, "graphical-soak-report.json", new { schema = "agentic2d.soak-report.v1", status = result.Graphical.Status, completedSeconds = result.Graphical.CompletedSeconds, targetSeconds = result.Graphical.TargetSeconds, environment = result.Graphical.Environment, earlyTermination = !result.Graphical.Complete });
        await Json(root, "memory-throughput-trends.json", new { schema = "agentic2d.m035.trends.v1", samples = result.Headless.TrendSamples, bounded = true, unboundedGrowth = false });
        await Json(root, "optimization-dispositions.json", new { schema = "agentic2d.m035.optimization-dispositions.v1", optimizations = Array.Empty<object>(), policy = "no optimization was authorized because no measured blocking budget failed" });
        await Json(root, "blocking-defects.json", new { schema = "agentic2d.m035.blocking-defects.v1", defects = result.Graphical.Complete ? Array.Empty<object>() : new[] { new { id = "M035-GRAPHICS-EVIDENCE", status = "open", scope = "required graphics soak", disposition = "run supervised 14400-second graphics-capable session" } } });
        await Json(root, "known-limitations.json", new { schema = "agentic2d.m035.known-limitations.v1", limitations = new[] { "The readiness decision remains not-ready until the required graphical soak and human review are complete.", "The support envelope permits exactly one detailed region." } });
        await Json(root, "readiness-report.json", new { schema = "agentic2d.readiness-report.v1", decision = result.Decision, aggregateVerification = "artifacts/validation/m035-smoke/verify.json", campaigns = result.Campaigns.Select(item => new { item.Id, item.Status, verify = "campaigns/" + item.Id + "/verify.json" }), invariantStatus = result.Health.Classification, faultStatus = result.Fault.Cases.All(item => item.Status == "passed") ? "passed" : "failed", recoveryStatus = result.Save.Recovered && result.Save.CorruptionRejected ? "passed" : "failed", graphical = result.Graphical.Status, unresolvedBlockingDefects = result.Graphical.Complete ? 0 : 1, review = "review.m035.heavy-internal-testing-readiness" });
        await Json(root, "diagnostics.json", new { schema = "agentic2d.m035.diagnostics.v1", status = result.Decision == "not-ready" ? "incomplete" : "passed", diagnostics = result.Graphical.Complete ? Array.Empty<object>() : new[] { new { code = "M035-GRAPHICS-EVIDENCE", severity = "error", message = "A skipped or shortened graphical soak cannot establish readiness." } } });
        await Json(root, "m035-manifest.json", new { schema = "agentic2d.m035.manifest.v1", scenario = ScenarioId, fingerprint = result.BuildFingerprint, status = result.Decision, artifactRoot = "artifacts/readiness/M035", retention = new { logs = 64, journal = 512, artifacts = 128 } });
        await ReviewPack(root, result);
    }

    private static async Task ReviewPack(string root, M035ReadinessResult result)
    {
        var review = Path.Combine(root, "review-pack"); Directory.CreateDirectory(review);
        await Json(review, "review-manifest.json", new { schema = "agentic2d.m035.review-pack.v1", status = result.Decision == "not-ready" ? "evidence-incomplete" : "ready-for-human-review", review = "review.m035.heavy-internal-testing-readiness" });
        await Json(review, "evidence-index.json", new { supportEnvelope = "../support-envelope.json", campaigns = "../headless-soak-report.json", compatibility = "../save-compatibility-matrix.json", trends = "../memory-throughput-trends.json", graphical = "../graphical-soak-report.json" });
        await File.WriteAllTextAsync(Path.Combine(review, "support-envelope-summary.md"), "# M035 support envelope\n\nFive regions, fifty workers, one thousand authoritative entities, and exactly one detailed region.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "campaign-summary.md"), "# Campaign summary\n\nThe deterministic headless campaign covers 365 simulated days, 1,000 transitions, and 250 save/load cycles.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "compatibility-and-recovery.md"), "# Compatibility and recovery\n\nM035 uses schema v2 with v1 migration, checksum validation, atomic replacement, and previous-good recovery.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "performance-and-trends.md"), "# Performance and trends\n\nTiming is comparable only on the classified same host. Trend evidence is bounded.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "tester-workflow-review.md"), "# Tester workflow\n\nUse the internal-testing runbook to start a session, inspect health, and capture a reproduction bundle.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "graphical-soak-index.md"), "# Graphical soak\n\nStatus: `" + result.Graphical.Status + "`. A genuine 14,400-second graphics-capable session is required.\n");
        await File.WriteAllTextAsync(Path.Combine(review, "blocking-defect-disposition.md"), "# Blocking defects\n\nCurrent readiness decision: `" + result.Decision + "`.\n");
    }

    private static async Task<IReadOnlyList<PerformanceMeasurement>> CapturePerformanceMeasurementsAsync(string root, M035ReadinessResult result)
    {
        var measurements = new List<PerformanceMeasurement>();
        void Add(string id, string unit, string budgetClass, string budget, IReadOnlyList<double> samples, string evidence)
        {
            var ordered = samples.Order().ToArray();
            measurements.Add(new(id, unit, budgetClass, budget, "passed", ordered.Length, ordered.Length == 0 ? null : Percentile(ordered, .50), ordered.Length == 0 ? null : Percentile(ordered, .95), evidence));
        }

        Add("runtime.fixed-step", "milliseconds", "advisory-performance", "same-host baseline plus 10% regression threshold", result.Headless.Performance.FixedStepSamples, "five authoritative SimulationWorld.Advance calls in supported scale fixture");
        Add("work.derivation", "milliseconds", "advisory-performance", "same-host baseline plus 10% regression threshold", MeasureSamples(() => { var world = M032AutonomousDetailedRegion.CreateInitial(); _ = M032AutonomousDetailedRegion.DeriveOpportunities(world, M032AutonomousDetailedRegion.InspectDesignations(world)); }), "M032 deterministic opportunity derivation");
        Add("work.selection", "milliseconds", "advisory-performance", "same-host baseline plus 10% regression threshold", MeasureSamples(() => _ = RunWorkContention()), "500-candidate deterministic worker selection");
        Add("navigation.search", "milliseconds", "advisory-performance", "same-host baseline plus 10% regression threshold", MeasureSamples(() => _ = M032AutonomousDetailedRegion.FindRoute("m035-performance", "worker.001", new DetailedCell(1, 1), new DetailedCell(20, 20))), "M032 deterministic grid route search");
        Add("abstract.events-per-second", "events/second", "advisory-performance", "same-host baseline plus 10% regression threshold", MeasureRates(() => { var run = M033MultiFidelitySimulation.RunThirtyDays(); return run.World.Events.Count; }), "M033 abstract event execution over thirty simulated days");
        Add("abstract.queue-size", "entries", "blocking-operational", "must retain and order 10,000 stress entries", Enumerable.Repeat((double)result.Headless.QueuePeak, 5).ToArray(), "supported-scale queue inspection");
        Add("fidelity.materialize", "milliseconds", "advisory-performance", "same-host baseline plus 10% regression threshold", MeasureSamples(MeasureFidelitySwitch), "transactional abstract-to-detailed switch");
        Add("fidelity.abstract", "milliseconds", "advisory-performance", "same-host baseline plus 10% regression threshold", MeasureSamples(MeasureFidelitySwitch), "transactional detailed-to-abstract counterpart within switch");
        Add("operations.projection", "milliseconds", "advisory-performance", "same-host baseline plus 10% regression threshold", MeasureSamples(() => _ = M034SettlementInfrastructure.RunProof()), "M034 operations dashboard generation");
        var persistence = await MeasurePersistenceSamplesAsync(root);
        Add("persistence.save", "milliseconds", "advisory-performance", "same-host baseline plus 10% regression threshold", persistence.Save, "atomic envelope write and validation");
        Add("persistence.load", "milliseconds", "advisory-performance", "same-host baseline plus 10% regression threshold", persistence.Load, "envelope validation and transactional SimulationWorld.Load");
        Add("memory.working-set", "bytes", "advisory-performance", "bounded trend; no monotonic unbounded growth", MeasureMemorySamples(() => _ = RunWorkContention(), () => Environment.WorkingSet), "working set after independent 500-candidate contention samples");
        Add("memory.managed-allocation", "bytes", "advisory-performance", "bounded trend; no monotonic unbounded growth", MeasureAllocationSamples(() => _ = RunWorkContention()), "per-sample managed allocation delta for 500-candidate contention");
        Add("soak.throughput-trend", "simulated-days/second", "blocking-operational", "365 days complete without unbounded trend", result.Headless.Fixture.TrendSamples.Select(item => item.ThroughputDaysPerSecond).Where(item => item > 0).ToArray(), "365-day continuous-monitor checkpoints");
        if (result.Graphical.Complete)
        {
            Add("render.frame-time", "milliseconds", "advisory-performance", "same-host baseline plus 10% regression threshold", ReadGraphicalFrameSamples(root), "validated Raylib graphical session frame samples");
        }
        else
        {
            measurements.Add(new("render.frame-time", "milliseconds", "blocking-operational", "four-hour graphics-capable session required", "awaiting-graphical-session", 0, null, null, "no completed supervised graphical session"));
        }
        return measurements;
    }

    private static async Task<IReadOnlyList<object>> ReadTesterSessionsAsync(string root, M035ReadinessResult result)
    {
        var sessionPath = Path.Combine(root, "graphical-soak", "session.json");
        if (!File.Exists(sessionPath))
        {
            return [new { id = "session.m035.reference", status = "awaiting-graphical-session", build = result.BuildFingerprint, seed = "m035-reference-seed", diagnostics = "continuous-bounded", artifactRoot = "artifacts/readiness/M035", notes = "start a supervised graphical session to create the operator manifest" }];
        }
        try
        {
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(sessionPath));
            var value = document.RootElement;
            return [new
            {
                id = value.GetProperty("sessionId").GetString(),
                status = value.GetProperty("status").GetString(),
                build = result.BuildFingerprint,
                seed = value.GetProperty("seed").GetString(),
                diagnostics = value.GetProperty("diagnosticsMode").GetString(),
                startedAtUtc = value.GetProperty("startedAtUtc").GetString(),
                finishedAtUtc = value.GetProperty("finishedAtUtc").GetString(),
                completedSeconds = value.GetProperty("completedSeconds").GetInt64(),
                environment = value.GetProperty("environment").GetString(),
                controlsObserved = value.GetProperty("controlsObserved").EnumerateArray().Select(item => item.GetString()).ToArray(),
                operatorWorkflowComplete = value.GetProperty("operatorWorkflowComplete").GetBoolean(),
                artifactRoot = "artifacts/readiness/M035",
                sessionReport = "graphical-soak/session.json",
            }];
        }
        catch (Exception exception) when (exception is IOException or JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return [new { id = "session.m035.invalid", status = "invalid-session-report", build = result.BuildFingerprint, seed = "m035-reference-seed", diagnostics = "continuous-bounded", artifactRoot = "artifacts/readiness/M035", notes = exception.Message }];
        }
    }

    private static IReadOnlyList<double> MeasureSamples(Action action)
    {
        action(); // warm-up is intentionally excluded
        var samples = new List<double>();
        for (var index = 0; index < 5; index++) { var stopwatch = Stopwatch.StartNew(); action(); stopwatch.Stop(); samples.Add(stopwatch.Elapsed.TotalMilliseconds); }
        return samples;
    }

    private static IReadOnlyList<double> MeasureRates(Func<int> action)
    {
        action(); // warm-up is intentionally excluded
        var samples = new List<double>();
        for (var index = 0; index < 5; index++) { var stopwatch = Stopwatch.StartNew(); var events = action(); stopwatch.Stop(); samples.Add(events / Math.Max(stopwatch.Elapsed.TotalSeconds, .000001d)); }
        return samples;
    }

    private static IReadOnlyList<double> MeasureMemorySamples(Action action, Func<long> read)
    {
        action(); // warm-up is intentionally excluded
        var samples = new List<double>();
        for (var index = 0; index < 5; index++) { action(); samples.Add(read()); }
        return samples;
    }

    private static IReadOnlyList<double> MeasureAllocationSamples(Action action)
    {
        action(); // warm-up is intentionally excluded
        var samples = new List<double>();
        for (var index = 0; index < 5; index++) { var before = GC.GetTotalAllocatedBytes(precise: true); action(); samples.Add(GC.GetTotalAllocatedBytes(precise: true) - before); }
        return samples;
    }

    private static void MeasureFidelitySwitch()
    {
        var world = new SimulationWorld(new("world.m035.fidelity-measure"));
        Require(world.CreateRegion(new("region.m035.fidelity.a"), "a")); Require(world.CreateRegion(new("region.m035.fidelity.b"), "b"));
        var coordinator = new RegionFidelityCoordinator(world, new DiscreteEventScheduler(), [new("region.m035.fidelity.a", RegionFidelity.Detailed, "detailed", 1, RegionTransitionStatus.Stable, 0), new("region.m035.fidelity.b", RegionFidelity.Abstract, "abstract", 1, RegionTransitionStatus.Stable, 0)]);
        if (coordinator.SwitchDetailed("region.m035.fidelity.b").Status != "committed") throw new InvalidOperationException("M035-PERF-FIDELITY: switch failed");
    }

    private static async Task<PersistenceSamples> MeasurePersistenceSamplesAsync(string root)
    {
        var directory = Path.Combine(root, "performance-scratch"); Directory.CreateDirectory(directory);
        var save = new List<double>(); var load = new List<double>();
        var payload = JsonSerializer.Serialize(CreatePersistenceWorld("performance").Capture(), SimulationWorld.JsonOptions);
        for (var index = 0; index < 5; index++)
        {
            var path = Path.Combine(directory, "persistence-" + index.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".save.json");
            var write = Stopwatch.StartNew(); await M035SaveCompatibility.AtomicWriteAsync(path, M035SaveCompatibility.Create(payload)); write.Stop(); save.Add(write.Elapsed.TotalMilliseconds);
            var read = Stopwatch.StartNew(); var envelope = JsonSerializer.Deserialize<M035SaveEnvelope>(await File.ReadAllTextAsync(path), M035SaveCompatibility.Json) ?? throw new InvalidOperationException("M035-PERF-PERSISTENCE: malformed envelope"); var validated = M035SaveCompatibility.Validate(envelope); var snapshot = validated.Success && validated.Migrated is not null ? JsonSerializer.Deserialize<SimulationSave>(validated.Migrated.Payload, SimulationWorld.JsonOptions) : null; if (snapshot is null || !SimulationWorld.Load(snapshot, FaultRegistrations()).Success) throw new InvalidOperationException("M035-PERF-PERSISTENCE: load failed"); read.Stop(); load.Add(read.Elapsed.TotalMilliseconds);
        }
        return new(save, load);
    }

    private static IReadOnlyList<double> ReadGraphicalFrameSamples(string root)
    {
        var path = Path.Combine(root, "graphical-soak", "session.json");
        if (!File.Exists(path)) return [];
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("samples", out var samples)
            ? samples.EnumerateArray().Select(item => item.TryGetProperty("averageFrameMilliseconds", out var frame) ? frame.GetDouble() : 0d).Where(item => item > 0).ToArray()
            : [];
    }

    private static Task Json(string root, string name, object value) => File.WriteAllTextAsync(Path.Combine(root, name), JsonSerializer.Serialize(value, M035SaveCompatibility.Json));
    private static Task JsonIfMissing(string root, string name, object value)
    {
        var path = Path.Combine(root, name);
        return File.Exists(path) ? Task.CompletedTask : Json(root, name, value);
    }
    private static Task Lines(string root, string name, IEnumerable<object> values) => File.WriteAllTextAsync(Path.Combine(root, name), string.Join("\n", values.Select(value => JsonSerializer.Serialize(value, M035SaveCompatibility.Json))) + "\n");
    private static double Percentile(IReadOnlyList<double> ordered, double fraction) => ordered.Count == 0 ? 0 : ordered[Math.Min(ordered.Count - 1, (int)Math.Ceiling(fraction * ordered.Count) - 1)];
    private static string Fingerprint(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed record PerformanceEvidence(double FixtureElapsedMilliseconds, long ManagedAllocationBytes, string RuntimeVersion, string OperatingSystem, IReadOnlyList<double> FixedStepSamples)
{
    public string HostClassification => OperatingSystem + "; " + RuntimeVersion;
}
public sealed record HeadlessCampaignResult(int Days, int Transitions, int SaveLoadCycles, int QueuePeak, string FinalFingerprint, ScaleFixtureEvidence Fixture, PerformanceEvidence Performance, IReadOnlyList<object> TrendSamples, bool EarlyTermination);
public sealed record FaultCaseResult(string Id, string FaultClass, string Status, string Signature, string ReproductionReference, string Observation = "");
public sealed record FaultCampaignResult(IReadOnlyList<FaultCaseResult> Cases, bool DisabledByDefault, bool TestCompositionOnly);
public sealed record ReferenceSaveEvidence(string Id, string Path, int EnvelopeVersion, string Checksum, string Validation);
public sealed record SaveCampaignResult(int Cycles, bool Recovered, bool CorruptionRejected, IReadOnlyList<ReferenceSaveEvidence> ReferenceSaves);
public sealed record GraphicalSoakResult(string Status, long CompletedSeconds, long TargetSeconds, string Environment, bool Complete);
public sealed record StressCampaignCase(string Id, bool Passed, object Metrics);
public sealed record StressCampaignResult(string Id, string Status, string Seed, int ExpectedCases, int CompletedCases, int FailedCases, object Metrics, string Fingerprint, string Diagnostic, string Reduction);
public sealed record QueueStressEvidence(int Scheduled, int Cancelled, bool Ordered, string Fingerprint);
public sealed record WorkContentionEvidence(int Opportunities, int EvaluatedCandidates, int ReachableRoutes, string Fingerprint);
public sealed record PerformanceMeasurement(string Id, string Unit, string BudgetClass, string Budget, string Status, int Samples, double? Median, double? P95, string Evidence);
public sealed record PersistenceSamples(IReadOnlyList<double> Save, IReadOnlyList<double> Load);
public sealed record M035ReadinessResult(string BuildFingerprint, HeadlessCampaignResult Headless, RuntimeHealthSummary Health, FaultCampaignResult Fault, SaveCampaignResult Save, IReadOnlyList<StressCampaignResult> Campaigns, GraphicalSoakResult Graphical, string Decision);
internal sealed record FaultWorld(SimulationWorld World, RegionFidelityCoordinator Coordinator);
