using Agentic2D.Simulation;

namespace Agentic2D.Tests.Unit;

public sealed class M035InternalTestingReadinessTests
{
    [Test]
    public async Task RuntimeHealthMonitorDetectsOwnerlessActivityWithoutRepairingState()
    {
        var monitor = new RuntimeHealthMonitor(RuntimeHealthMode.ContinuousBounded, 4);
        var snapshot = Snapshot(activities: [new("activity.001", null, false, false)]);
        var result = monitor.Observe(snapshot);
        await Assert.That(result.Diagnostics.Select(item => item.Code)).Contains("HEALTH-ACTIVITY-OWNERLESS");
        await Assert.That(snapshot.Activities.Single().ExecutorId).IsNull();
    }

    [Test]
    public async Task DeadlockDetectionClassifiesSameInstantAndRepeatedRouteLoops()
    {
        var monitor = new RuntimeHealthMonitor();
        var result = monitor.Observe(Snapshot(sameInstant: 2, replans: 4));
        await Assert.That(result.Diagnostics.Select(item => item.Code)).Contains("HEALTH-TRIGGER-SAME-INSTANT-LOOP");
        await Assert.That(result.Diagnostics.Select(item => item.Code)).Contains("HEALTH-ROUTE-REPLAN-LOOP");
    }

    [Test]
    public async Task RuntimeHealthDetectsCompleteM031ToM034InvariantTaxonomy()
    {
        var monitor = new RuntimeHealthMonitor();
        var snapshot = Snapshot() with
        {
            NoStaleTriggerAuthoritativeMutation = false,
            NoDuplicateSemanticCompletion = false,
            ResourceAndEnvironmentalConservation = false,
            StorageAndInfrastructureCapacityBounds = false,
            ConstructionCropAndConditionStateValid = false,
            NoHalfCommittedFidelityTransition = false,
            ReservationCycleOrLeak = true,
        };
        var codes = monitor.Observe(snapshot).Diagnostics.Select(item => item.Code);
        await Assert.That(codes).Contains("HEALTH-TRIGGER-STALE-MUTATION");
        await Assert.That(codes).Contains("HEALTH-DUPLICATE-COMPLETION");
        await Assert.That(codes).Contains("HEALTH-RESOURCE-CONSERVATION");
        await Assert.That(codes).Contains("HEALTH-CAPACITY-BOUND");
        await Assert.That(codes).Contains("HEALTH-INFRASTRUCTURE-STATE");
        await Assert.That(codes).Contains("HEALTH-FIDELITY-HALF-COMMIT");
        await Assert.That(codes).Contains("HEALTH-RESERVATION-CYCLE");
    }

    [Test]
    public async Task FaultInjectionIsDisabledUntilExplicitCompositionAndIsDeterministic()
    {
        var disabled = new DeterministicFaultInjector();
        var enabled = new DeterministicFaultInjector([new("fault.command", "command.commit", 3, "command-before-commit")]);
        await Assert.That(disabled.Enabled).IsFalse();
        await Assert.That(disabled.Check("command.commit", 3).Injected).IsFalse();
        await Assert.That(enabled.Check("command.commit", 3)).IsEqualTo(new FaultInjectionResult(true, "fault.command", "command-before-commit"));
    }

    [Test]
    public async Task SaveCompatibilityRejectsCorruptionAndMigratesSupportedPriorSchema()
    {
        var prior = M035SaveCompatibility.Create("reference-world", 1);
        var valid = M035SaveCompatibility.Validate(prior);
        var corrupt = M035SaveCompatibility.Validate(prior with { Checksum = "invalid" });
        await Assert.That(valid.Success && valid.Migrated?.Version == M035SaveCompatibility.CurrentVersion).IsTrue();
        await Assert.That(corrupt.Code).IsEqualTo("SAVE-CHECKSUM-MISMATCH");
    }

    [Test]
    public async Task SaveRecoveryRetainsPreviousGoodSave()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m035-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(root, "world.save.json");
            await M035SaveCompatibility.AtomicWriteAsync(path, M035SaveCompatibility.Create("first"));
            await M035SaveCompatibility.AtomicWriteAsync(path, M035SaveCompatibility.Create("second"));
            await File.WriteAllTextAsync(path, "corrupt");
            var recovery = await M035SaveCompatibility.RecoverAsync(path);
            await Assert.That(recovery.Success).IsTrue();
            await Assert.That(await File.ReadAllTextAsync(path)).Contains("first");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task PerformanceBudgetReadinessCampaignDeclaresMinimumScaleAndDoesNotTreatHeadlessSkipAsReady()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m035-" + Guid.NewGuid().ToString("N"));
        try
        {
            var result = await M035ReadinessArtifactWriter.WriteAsync(root);
            await Assert.That(result.Headless.Days).IsEqualTo(365);
            await Assert.That(result.Headless.Transitions).IsGreaterThanOrEqualTo(1000);
            await Assert.That(result.Headless.SaveLoadCycles).IsEqualTo(250);
            await Assert.That(result.Decision).IsEqualTo("not-ready");
            await Assert.That(File.Exists(Path.Combine(root, "readiness-report.json"))).IsTrue();
            var measurements = await File.ReadAllTextAsync(Path.Combine(root, "performance-measurements.json"));
            await Assert.That(measurements).Contains("work.selection");
            await Assert.That(measurements).Contains("awaiting-graphical-session");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task ReproductionBundleAndInternalTestSessionArePortableAndBounded()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m035-" + Guid.NewGuid().ToString("N"));
        try
        {
            await M035ReadinessArtifactWriter.WriteAsync(root);
            var bundle = await File.ReadAllTextAsync(Path.Combine(root, "reproduction-bundle-index.json"));
            var sessions = await File.ReadAllTextAsync(Path.Combine(root, "tester-session-index.json"));
            await Assert.That(bundle).Contains("sanitized");
            await Assert.That(sessions).Contains("continuous-bounded");
            await Assert.That(File.Exists(Path.Combine(root, "reproductions", "fault.command-before-commit", "manifest.json"))).IsTrue();
            var carrying = await File.ReadAllTextAsync(Path.Combine(root, "reference-saves", "active-carrying.save.json"));
            await Assert.That(carrying).Contains("activity.m035.save.active-carrying");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    [Test]
    public async Task ReadinessGateDoesNotAcceptSkippedGraphicalEvidence()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m035-" + Guid.NewGuid().ToString("N"));
        try
        {
            var result = await M035ReadinessArtifactWriter.WriteAsync(root);
            await Assert.That(result.Graphical.Status).IsEqualTo("skipped-not-graphics-capable");
            await Assert.That(result.Decision).IsEqualTo("not-ready");
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static RuntimeHealthSnapshot Snapshot(IReadOnlyList<RuntimeActivityHealth>? activities = null, int sameInstant = 0, int replans = 0) => new(
        10,
        ["entity.001"],
        new Dictionary<string, string> { ["entity.001"] = "region.001" },
        activities ?? [],
        true,
        1,
        true,
        true,
        sameInstant,
        replans,
        false,
        false,
        false,
        "activity.001",
        "state.001",
        3,
        ["command.001"]);
}
