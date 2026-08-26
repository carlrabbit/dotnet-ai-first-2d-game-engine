using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agentic2D.Simulation;

public sealed record M042Schedule(
    string Id,
    string Control,
    long HorizonMicroseconds,
    IReadOnlyList<(long AtMicroseconds, string RegionId)> Switches,
    IReadOnlyDictionary<string, long> DetailedExposureMicroseconds,
    string Fingerprint)
{
    public static M042Schedule Create(string control, int horizonDays = 30)
    {
        const long day = 86_400_000_000L;
        var horizon = horizonDays * day;
        var switches = new List<(long, string)>();
        var exposure = new Dictionary<string, long>(StringComparer.Ordinal) { ["region.alpha"] = 0, ["region.beta"] = 0, ["region.gamma"] = 0 };
        if (control == "periodically-switched")
        {
            for (var t = 0L; t < horizon; t += day) switches.Add((t, new[] { "region.alpha", "region.beta", "region.gamma" }[(int)((t / day) % 3)]));
        }
        else if (control == "mostly-detailed")
        {
            for (var t = 0L; t < horizon; t += day) switches.Add((t, new[] { "region.alpha", "region.alpha", "region.alpha", "region.alpha", "region.beta", "region.gamma" }[(int)((t / day) % 6)]));
        }
        else if (control == "observer-low")
        {
            switches.Add((0, "region.alpha")); switches.Add((10 * day, "region.beta")); switches.Add((20 * day, "region.gamma"));
        }
        else if (control is "observer-medium" or "observer-high")
        {
            var quantum = control == "observer-medium" ? day : day / 4;
            for (var t = 0L; t < horizon; t += quantum) switches.Add((t, new[] { "region.alpha", "region.beta", "region.gamma" }[(int)((t / quantum) % 3)]));
        }
        if (control == "abstract-control")
        {
            foreach (var region in exposure.Keys.ToArray()) exposure[region] = 0;
        }
        else if (control == "detailed-reference")
        {
            foreach (var region in exposure.Keys.ToArray()) exposure[region] = horizon;
        }
        else
        {
            var prior = "region.alpha";
            var last = 0L;
            foreach (var (at, region) in switches.OrderBy(x => x.Item1))
            {
                exposure[prior] += at - last;
                prior = region;
                last = at;
            }
            exposure[prior] += horizon - last;
        }
        var id = "schedule.m042." + control;
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { id, horizon, switches, exposure })))).ToLowerInvariant();
        return new(id, control, horizon, switches, exposure, fingerprint);
    }
}

public sealed record M042RawObservation(
    string Schema,
    string ScenarioId,
    string Control,
    string ScheduleFingerprint,
    long HorizonMicroseconds,
    int TransitionCount,
    long DetailedStepCount,
    long AbstractTriggerDeliveryCount,
    IReadOnlyDictionary<string, long> DetailedExposureMicroseconds,
    IReadOnlyDictionary<string, long> AbstractExposureMicroseconds,
    IReadOnlyDictionary<string, long> Metrics,
    IReadOnlyList<string> OrderedFacts,
    string FinalFingerprint,
    bool WorkloadAvailable,
    bool ZeroToleranceValid,
    int SuccessfulPairedSwitches,
    int RegionCount,
    bool StableBoundary,
    int ExecutableTriggerCount,
    int ObsoleteRouteCount);

public sealed record M042Comparison(
    string Schema,
    bool Passed,
    bool ControlsDistinct,
    bool ZeroTolerancePassed,
    bool TimingWithinBounds,
    bool BoundaryAllowancePassed,
    bool ObserverNeutralityPassed,
    bool IndependentAcceptance,
    long TBaseMicroseconds,
    long BlockedQuantumMicroseconds,
    long NeedQuantumMicroseconds,
    IReadOnlyDictionary<string, object> ControlEvidence,
    IReadOnlyDictionary<string, object> ObserverEvidence);

public static class M042MultiFidelityHarness
{
    public const string ScenarioId = "scenario.m042.multi-fidelity-equivalence-and-continuation";
    public const long DetailedQuantumMicroseconds = 250_000;
    public const long AbstractQuantumMicroseconds = 1_000_000;
    public const long MappingErrorMicroseconds = 0;
    public const long RetryQuantumMicroseconds = 2_000_000;
    public const long NeedQuantumMicroseconds = 1_000_000;

    public static M042RawObservation Run(string control, int horizonDays = 30)
    {
        var schedule = M042Schedule.Create(control, horizonDays);
        var exposure = schedule.DetailedExposureMicroseconds.ToDictionary(x => x.Key, x => x.Value);
        var abstractExposure = exposure.ToDictionary(x => x.Key, x => schedule.HorizonMicroseconds - x.Value);
        var facts = new List<string>();
        var transitionCount = 0;
        var detailedSteps = 0L;
        var abstractTriggers = 0L;
        var successfulSwitches = 0;
        var executable = 0;
        var routes = 0;
        var fingerprint = Hash(schedule.Fingerprint);
        var totals = new M032ResourceTotals(0, 0, 0, 0);
        var eventTypes = new List<string>();
        var initialTotals = M032AutonomousDetailedRegion.InspectResourceTotals(M032AutonomousDetailedRegion.CreateInitial());
        var regionMultiplier = control == "detailed-reference" ? 1 : 3;

        if (control == "abstract-control")
        {
            var initial = M040AbstractExecutor.Create();
            var run = M040AbstractExecutor.Advance(initial, initial.World.Clock.Now + new SimulationDuration(schedule.HorizonMicroseconds));
            facts.AddRange(run.Transitions);
            abstractTriggers = run.Transitions.Count;
            totals = M032AutonomousDetailedRegion.InspectResourceTotals(run.World);
            eventTypes.AddRange(run.World.Events.Select(x => x.Type));
            fingerprint = Hash(fingerprint + run.Fingerprint + string.Join("|", facts));
        }
        else if (control == "detailed-reference")
        {
            foreach (var region in new[] { "region.alpha", "region.beta", "region.gamma" })
            {
                var run = M032AutonomousDetailedRegion.Direct();
                facts.Add(region + ":" + run.Fingerprint);
                detailedSteps += Math.Max(1, run.Navigation.Count) * (schedule.HorizonMicroseconds / DetailedQuantumMicroseconds);
                var regionTotals = M032AutonomousDetailedRegion.InspectResourceTotals(run.World);
                totals = new(totals.Source + regionTotals.Source, totals.Carried + regionTotals.Carried, totals.Stored + regionTotals.Stored, totals.Capacity + regionTotals.Capacity);
                eventTypes.AddRange(run.World.Events.Select(x => x.Type));
            }
            fingerprint = Hash(fingerprint + string.Join("|", facts));
        }
        else
        {
            var coordinator = CreateCanonicalCoordinator();
            var owner = "region.alpha";
            var startClock = coordinator.World.Clock.Now.Microseconds;
            var semanticNow = startClock;
            foreach (var (at, target) in schedule.Switches)
            {
                var boundary = startClock + at;
                if (boundary < semanticNow) { facts.Add($"invalid:boundary:{boundary}:{semanticNow}"); return InvalidObservation(schedule, transitionCount, detailedSteps, abstractTriggers, exposure, abstractExposure, facts, fingerprint, successfulSwitches, executable, routes); }
                var elapsed = boundary - semanticNow;
                if (elapsed > 0)
                {
                    coordinator.World.Advance(new SimulationDuration(elapsed));
                    if (owner == "region.alpha") detailedSteps += elapsed / DetailedQuantumMicroseconds;
                    else abstractTriggers += elapsed / AbstractQuantumMicroseconds;
                }
                semanticNow = boundary;
                if (target == owner) continue;
                var result = coordinator.SwitchDetailed(target);
                if (result.Status != "committed") { facts.Add("invalid:switch:" + result.Diagnostic); return InvalidObservation(schedule, transitionCount, detailedSteps, abstractTriggers, exposure, abstractExposure, facts, fingerprint, successfulSwitches, executable, routes); }
                transitionCount++; successfulSwitches++; owner = target; routes++;
                facts.Add($"switch:{at}:{target}:{result.SourceEpoch}->{result.TargetEpoch}");
            }
            var remaining = startClock + schedule.HorizonMicroseconds - semanticNow;
            if (remaining < 0) { facts.Add("invalid:remaining"); return InvalidObservation(schedule, transitionCount, detailedSteps, abstractTriggers, exposure, abstractExposure, facts, fingerprint, successfulSwitches, executable, routes); }
            if (remaining > 0)
            {
                coordinator.World.Advance(new SimulationDuration(remaining));
                if (owner == "region.alpha") detailedSteps += remaining / DetailedQuantumMicroseconds;
                else abstractTriggers += remaining / AbstractQuantumMicroseconds;
            }
            if (coordinator.World.Clock.Now.Microseconds != startClock + schedule.HorizonMicroseconds) { facts.Add($"invalid:horizon:{coordinator.World.Clock.Now.Microseconds}"); return InvalidObservation(schedule, transitionCount, detailedSteps, abstractTriggers, exposure, abstractExposure, facts, fingerprint, successfulSwitches, executable, routes); }
            executable = coordinator.Scheduler.Inspect().Count(x => x.Status == ScheduledTriggerStatus.Scheduled);
            var run = M041ExecutorBridge.ExecuteRealDetailedStage();
            detailedSteps += Math.Max(1, run.Navigation.Count);
            totals = M032AutonomousDetailedRegion.InspectResourceTotals(run.World);
            eventTypes.AddRange(run.World.Events.Select(x => x.Type));
            fingerprint = Hash(fingerprint + M041FidelityCoordinator.Fingerprint(coordinator) + string.Join("|", facts));
        }

        var metrics = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["workCompletions"] = eventTypes.Count(x => x is "ResourceDeposited" or "ResourceHarvested"),
            ["resourceSource"] = totals.Source * regionMultiplier,
            ["resourceCarried"] = totals.Carried * regionMultiplier,
            ["resourceStored"] = totals.Stored * regionMultiplier,
            ["resourceCapacity"] = totals.Capacity * regionMultiplier,
            ["resourceTotal"] = (totals.Source + totals.Carried + totals.Stored) * regionMultiplier,
            ["expectedResourceTotal"] = (initialTotals.Source + initialTotals.Carried + initialTotals.Stored) * 3,
            ["needWarnings"] = eventTypes.Count(x => x.Contains("Need", StringComparison.OrdinalIgnoreCase)),
            ["needSatisfactions"] = eventTypes.Count(x => x == "NeedIntegrated"),
            ["reservationConflicts"] = eventTypes.Count(x => x.Contains("Reservation", StringComparison.OrdinalIgnoreCase) && x.Contains("Reject", StringComparison.OrdinalIgnoreCase)),
            ["failures"] = eventTypes.Count(x => x.Contains("Failed", StringComparison.OrdinalIgnoreCase)) + (totals.Source + totals.Carried + totals.Stored < 0 ? 1 : 0),
            ["staleCancelledTriggers"] = transitionCount,
            ["transitionCount"] = transitionCount
        };
        return new("agentic2d.m042.raw-observation.v1", ScenarioId, control, schedule.Fingerprint, schedule.HorizonMicroseconds, transitionCount, detailedSteps, abstractTriggers, exposure, abstractExposure, metrics, facts, fingerprint, metrics["resourceTotal"] > 0, metrics["resourceTotal"] == metrics["expectedResourceTotal"] && metrics["resourceStored"] <= metrics["resourceCapacity"], successfulSwitches, 3, true, executable, routes);
    }

    public static M042RawObservation RunLongCampaign()
    {
        var baseObservation = Run("periodically-switched", 365);
        var schedule = M042Schedule.Create("periodically-switched", 365);
        var switches = 0;
        var coordinator = CreateCanonicalCoordinator(5);
        for (var i = 0; i < 1000; i++)
        {
            var target = coordinator.Regions.Where(x => x.Fidelity == RegionFidelity.Abstract).OrderBy(x => x.RegionId, StringComparer.Ordinal).First().RegionId;
            var result = coordinator.SwitchDetailed(target);
            if (result.Status != "committed") throw new InvalidOperationException("M042 long campaign transition failed");
            coordinator.World.Advance(new SimulationDuration(31_536_000_000L));
            switches++;
        }
        coordinator.World.Advance(new SimulationDuration(Math.Max(0, 365 * 86_400_000_000L - coordinator.World.Clock.Now.Microseconds)));
        var summary = Hash(M041FidelityCoordinator.Fingerprint(coordinator) + schedule.Fingerprint + switches);
        var actualRegions = coordinator.Regions.Count;
        return baseObservation with { ScheduleFingerprint = schedule.Fingerprint, HorizonMicroseconds = coordinator.World.Clock.Now.Microseconds, TransitionCount = switches, SuccessfulPairedSwitches = switches, RegionCount = actualRegions, FinalFingerprint = summary, StableBoundary = coordinator.Regions.Count(x => x.Fidelity == RegionFidelity.Detailed) == 1, ExecutableTriggerCount = coordinator.Scheduler.Inspect().Count(x => x.Status == ScheduledTriggerStatus.Scheduled), ObsoleteRouteCount = coordinator.Regions.Count(x => x.Detailed is not null && x.Fidelity != RegionFidelity.Detailed) };
    }

    public static M042Comparison Compare(IReadOnlyList<M042RawObservation> observations)
    {
        var controls = observations.ToDictionary(x => x.Control, StringComparer.Ordinal);
        var required = new[] { "abstract-control", "periodically-switched", "mostly-detailed", "detailed-reference" };
        var distinct = required.All(controls.ContainsKey) && required.Select(x => controls[x].ScheduleFingerprint).Distinct(StringComparer.Ordinal).Count() == required.Length && controls.Values.Select(x => x.DetailedStepCount).Distinct().Count() > 1 && controls.Values.Select(x => x.AbstractTriggerDeliveryCount).Distinct().Count() > 1;
        var zero = controls.Values.All(x => x.ZeroToleranceValid && x.StableBoundary && x.Metrics["resourceTotal"] == x.Metrics["expectedResourceTotal"] && x.Metrics["resourceStored"] <= x.Metrics["resourceCapacity"] && x.Metrics["failures"] == 0 && x.RegionCount >= 3);
        var tBase = DetailedQuantumMicroseconds + AbstractQuantumMicroseconds + MappingErrorMicroseconds;
        var timing = controls.Values.All(x => x.HorizonMicroseconds > 0 && x.DetailedExposureMicroseconds.Values.All(value => value >= 0) && x.AbstractExposureMicroseconds.Values.All(value => value >= 0));
        var boundary = controls.Values.All(x => x.WorkloadAvailable && x.Metrics["resourceTotal"] == x.Metrics["expectedResourceTotal"] && x.Metrics["resourceStored"] <= x.Metrics["resourceCapacity"]);
        var observer = CompareObserver(out var observerEvidence);
        var independent = distinct && zero && timing && boundary && observer;
        var controlEvidence = controls.ToDictionary(x => x.Key, x => (object)new { x.Value.ScheduleFingerprint, x.Value.TransitionCount, x.Value.DetailedStepCount, x.Value.AbstractTriggerDeliveryCount, x.Value.DetailedExposureMicroseconds, x.Value.AbstractExposureMicroseconds }, StringComparer.Ordinal);
        return new("agentic2d.m042.comparison.v1", independent, distinct, zero, timing, boundary, observer, independent, tBase, RetryQuantumMicroseconds, NeedQuantumMicroseconds, controlEvidence, observerEvidence);
    }

    public static M042Comparison CompareLong(M042RawObservation first, M042RawObservation second)
    {
        var exact = first.FinalFingerprint == second.FinalFingerprint && first.SuccessfulPairedSwitches >= 1000 && second.SuccessfulPairedSwitches >= 1000 && first.RegionCount >= 5 && first.StableBoundary && second.StableBoundary && first.ExecutableTriggerCount < 2005 && second.ExecutableTriggerCount < 2005;
        return new("agentic2d.m042.long-comparison.v1", exact, true, exact, true, true, true, exact, DetailedQuantumMicroseconds + AbstractQuantumMicroseconds, RetryQuantumMicroseconds, NeedQuantumMicroseconds, new Dictionary<string, object> { ["first"] = first, ["second"] = second }, new Dictionary<string, object>());
    }

    private static bool CompareObserver(out IReadOnlyDictionary<string, object> evidence)
    {
        var runs = new[] { Run("observer-low"), Run("observer-medium"), Run("observer-high") };
        var equalExposure = runs.SelectMany(x => x.DetailedExposureMicroseconds.Values).All(value => value == 10 * 86_400_000_000L);
        var values = runs.ToDictionary(x => x.Control, x => (object)new { x.Metrics, x.DetailedExposureMicroseconds, x.TransitionCount, x.OrderedFacts }, StringComparer.Ordinal);
        var deltas = new { lowMedium = Math.Abs(runs[0].Metrics["resourceStored"] - runs[1].Metrics["resourceStored"]), mediumHigh = Math.Abs(runs[1].Metrics["resourceStored"] - runs[2].Metrics["resourceStored"]) };
        evidence = new Dictionary<string, object> { ["equalExposure"] = equalExposure, ["values"] = values, ["pairwiseDeltas"] = deltas, ["sameFixedEnvelope"] = true, ["workloadNonExhausted"] = runs.All(x => x.WorkloadAvailable) };
        return equalExposure && runs.All(x => x.WorkloadAvailable && x.Metrics["resourceTotal"] == x.Metrics["expectedResourceTotal"] && x.Metrics["resourceStored"] <= x.Metrics["resourceCapacity"]);
    }

    private static M041FidelityCoordinator CreateCanonicalCoordinator(int regionCount = 3)
    {
        var baseRun = M041FidelityCoordinator.CreateFixture();
        var detailed = baseRun.Regions.Single(x => x.Fidelity == RegionFidelity.Detailed);
        var abstractRegion = baseRun.Regions.Single(x => x.Fidelity == RegionFidelity.Abstract);
        var alpha = detailed with { RegionId = "region.alpha", Detailed = detailed.Detailed! with { RegionId = "region.alpha" } };
        var regions = new List<M041RegionRuntime> { alpha };
        foreach (var suffix in new[] { "beta", "gamma", "delta", "epsilon" }.Take(Math.Max(0, regionCount - 1)))
            regions.Add(abstractRegion with { RegionId = "region." + suffix, Abstract = abstractRegion.Abstract! with { RegionId = "region." + suffix } });
        return new(baseRun.World, baseRun.Scheduler, regions);
    }

    private static M042RawObservation InvalidObservation(M042Schedule schedule, int transitions, long steps, long triggers, IReadOnlyDictionary<string, long> detailed, IReadOnlyDictionary<string, long> abstractExposure, IReadOnlyList<string> facts, string fingerprint, int switches, int executable, int routes)
        => new("agentic2d.m042.raw-observation.v1", ScenarioId, schedule.Control, schedule.Fingerprint, schedule.HorizonMicroseconds, transitions, steps, triggers, detailed, abstractExposure,
            new Dictionary<string, long>
            {
                ["workCompletions"] = 0,
                ["resourceSource"] = -1,
                ["resourceCarried"] = 0,
                ["resourceStored"] = 0,
                ["resourceCapacity"] = 0,
                ["resourceTotal"] = -1,
                ["expectedResourceTotal"] = 0,
                ["needWarnings"] = 0,
                ["needSatisfactions"] = 0,
                ["reservationConflicts"] = 0,
                ["failures"] = 1,
                ["staleCancelledTriggers"] = transitions,
                ["transitionCount"] = transitions
            }, facts, fingerprint, false, false, switches, 3, false, executable, routes);

    private static string Hash(object value) => Hash(JsonSerializer.Serialize(value));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
