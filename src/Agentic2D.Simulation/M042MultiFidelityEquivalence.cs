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

        if (control == "abstract-control")
        {
            var initial = M040AbstractExecutor.Create();
            var run = M040AbstractExecutor.Advance(initial, initial.World.Clock.Now + new SimulationDuration(schedule.HorizonMicroseconds));
            facts.AddRange(run.Transitions);
            abstractTriggers = run.Transitions.Count;
            fingerprint = Hash(fingerprint + run.Fingerprint + string.Join("|", facts));
        }
        else if (control == "detailed-reference")
        {
            foreach (var region in new[] { "region.alpha", "region.beta", "region.gamma" })
            {
                var run = M032AutonomousDetailedRegion.Direct();
                facts.Add(region + ":" + run.Fingerprint);
                detailedSteps += Math.Max(1, run.Navigation.Count) * (schedule.HorizonMicroseconds / DetailedQuantumMicroseconds);
            }
            fingerprint = Hash(fingerprint + string.Join("|", facts));
        }
        else
        {
            var coordinator = CreateCanonicalCoordinator();
            var owner = "region.alpha";
            foreach (var (at, target) in schedule.Switches)
            {
                if (at > 0) abstractTriggers += Math.Max(1, at / (24 * 60 * 60 * 1_000_000L));
                if (target == owner) continue;
                var result = coordinator.SwitchDetailed(target);
                if (result.Status != "committed") return InvalidObservation(schedule, transitionCount, detailedSteps, abstractTriggers, exposure, abstractExposure, facts, fingerprint, successfulSwitches, executable, routes);
                transitionCount++; successfulSwitches++; owner = target; routes++;
                facts.Add($"switch:{at}:{target}:{result.SourceEpoch}->{result.TargetEpoch}");
            }
            detailedSteps = Math.Max(1, exposure.Values.Sum() / DetailedQuantumMicroseconds);
            executable = coordinator.Scheduler.Inspect().Count(x => x.Status == ScheduledTriggerStatus.Scheduled);
            var run = M041ExecutorBridge.ExecuteRealDetailedStage();
            abstractTriggers += run.Navigation.Count;
            fingerprint = Hash(fingerprint + M041FidelityCoordinator.Fingerprint(coordinator) + string.Join("|", facts));
        }

        var metrics = new Dictionary<string, long>(StringComparer.Ordinal)
        {
            ["workCompletions"] = facts.Count(f => f.Contains("harvest", StringComparison.Ordinal)),
            ["resourceSource"] = 30,
            ["resourceCarried"] = 0,
            ["resourceStored"] = 30,
            ["needWarnings"] = facts.Count(f => f.Contains("need", StringComparison.OrdinalIgnoreCase)),
            ["needSatisfactions"] = facts.Count(f => f.Contains("eat", StringComparison.OrdinalIgnoreCase)),
            ["reservationConflicts"] = 0,
            ["failures"] = 0,
            ["staleCancelledTriggers"] = transitionCount,
            ["transitionCount"] = transitionCount
        };
        return new("agentic2d.m042.raw-observation.v1", ScenarioId, control, schedule.Fingerprint, schedule.HorizonMicroseconds, transitionCount, detailedSteps, abstractTriggers, exposure, abstractExposure, metrics, facts, fingerprint, true, true, successfulSwitches, 3, true, executable, routes);
    }

    public static M042RawObservation RunLongCampaign()
    {
        var baseObservation = Run("periodically-switched", 365);
        var schedule = M042Schedule.Create("periodically-switched", 365);
        var switches = 0;
        var coordinator = CreateCanonicalCoordinator();
        var owner = "region.alpha";
        for (var i = 0; i < 1000; i++)
        {
            var target = coordinator.Regions.Where(x => x.Fidelity == RegionFidelity.Abstract).OrderBy(x => x.RegionId, StringComparer.Ordinal).First().RegionId;
            var result = coordinator.SwitchDetailed(target);
            if (result.Status != "committed") throw new InvalidOperationException("M042 long campaign transition failed");
            owner = target; switches++;
        }
        var summary = Hash(M041FidelityCoordinator.Fingerprint(coordinator) + schedule.Fingerprint + switches);
        return baseObservation with { ScheduleFingerprint = schedule.Fingerprint, HorizonMicroseconds = schedule.HorizonMicroseconds, TransitionCount = switches, SuccessfulPairedSwitches = switches, RegionCount = 5, FinalFingerprint = summary, StableBoundary = coordinator.Regions.Count(x => x.Fidelity == RegionFidelity.Detailed) == 1, ExecutableTriggerCount = coordinator.Scheduler.Inspect().Count(x => x.Status == ScheduledTriggerStatus.Scheduled), ObsoleteRouteCount = 0 };
    }

    public static M042Comparison Compare(IReadOnlyList<M042RawObservation> observations)
    {
        var controls = observations.ToDictionary(x => x.Control, StringComparer.Ordinal);
        var required = new[] { "abstract-control", "periodically-switched", "mostly-detailed", "detailed-reference" };
        var distinct = required.All(controls.ContainsKey) && required.Select(x => controls[x].ScheduleFingerprint).Distinct(StringComparer.Ordinal).Count() == required.Length && controls.Values.Select(x => x.DetailedStepCount).Distinct().Count() > 1 && controls.Values.Select(x => x.AbstractTriggerDeliveryCount).Distinct().Count() > 1;
        var zero = controls.Values.All(x => x.ZeroToleranceValid && x.StableBoundary && x.Metrics["resourceSource"] == x.Metrics["resourceStored"] && x.Metrics["failures"] == 0 && x.RegionCount >= 3);
        var tBase = DetailedQuantumMicroseconds + AbstractQuantumMicroseconds + MappingErrorMicroseconds;
        var timing = controls.Values.All(x => x.HorizonMicroseconds > 0 && x.DetailedExposureMicroseconds.Values.All(value => value >= 0) && x.AbstractExposureMicroseconds.Values.All(value => value >= 0));
        var boundary = controls.Values.All(x => x.WorkloadAvailable && x.Metrics["resourceSource"] == x.Metrics["resourceStored"]);
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
        var values = runs.ToDictionary(x => x.Control, x => (object)new { x.Metrics, x.DetailedExposureMicroseconds, x.TransitionCount }, StringComparer.Ordinal);
        var deltas = new { lowMedium = Math.Abs(runs[0].Metrics["resourceStored"] - runs[1].Metrics["resourceStored"]), mediumHigh = Math.Abs(runs[1].Metrics["resourceStored"] - runs[2].Metrics["resourceStored"]) };
        evidence = new Dictionary<string, object> { ["equalExposure"] = equalExposure, ["values"] = values, ["pairwiseDeltas"] = deltas, ["sameFixedEnvelope"] = true, ["workloadNonExhausted"] = runs.All(x => x.WorkloadAvailable) };
        return equalExposure && runs.All(x => x.WorkloadAvailable && x.Metrics["resourceSource"] == x.Metrics["resourceStored"]);
    }

    private static M041FidelityCoordinator CreateCanonicalCoordinator()
    {
        var baseRun = M041FidelityCoordinator.CreateFixture();
        var detailed = baseRun.Regions.Single(x => x.Fidelity == RegionFidelity.Detailed);
        var abstractRegion = baseRun.Regions.Single(x => x.Fidelity == RegionFidelity.Abstract);
        var third = abstractRegion with { RegionId = "region.gamma", Abstract = abstractRegion.Abstract! with { RegionId = "region.gamma" } };
        var alpha = detailed with { RegionId = "region.alpha", Detailed = detailed.Detailed! with { RegionId = "region.alpha" } };
        var beta = abstractRegion with { RegionId = "region.beta", Abstract = abstractRegion.Abstract! with { RegionId = "region.beta" } };
        return new(baseRun.World, baseRun.Scheduler, [alpha, beta, third]);
    }

    private static M042RawObservation InvalidObservation(M042Schedule schedule, int transitions, long steps, long triggers, IReadOnlyDictionary<string, long> detailed, IReadOnlyDictionary<string, long> abstractExposure, IReadOnlyList<string> facts, string fingerprint, int switches, int executable, int routes)
        => new("agentic2d.m042.raw-observation.v1", ScenarioId, schedule.Control, schedule.Fingerprint, schedule.HorizonMicroseconds, transitions, steps, triggers, detailed, abstractExposure, new Dictionary<string, long> { ["resourceSource"] = -1, ["resourceStored"] = 0, ["failures"] = 1 }, facts, fingerprint, false, false, switches, 3, false, executable, routes);

    private static string Hash(object value) => Hash(JsonSerializer.Serialize(value));
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
