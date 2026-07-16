using Agentic2D.Persistence;

namespace Agentic2D.Tests.Unit;

public sealed class M020PersistenceTests
{
    [Test]
    public async Task CanonicalCaptureOrdersRecordsAndRoundTripsWithoutTransientState()
    {
        var runtime = PersistentWorldRuntime.CreateInitial();
        runtime.AdvanceTo(3);
        runtime.CollectCrystal("collect.crystal", "test.collect");
        var service = new CanonicalSaveService();
        var first = service.Capture(runtime, CanonicalSaveService.DefaultIdentity("save.roundtrip"));
        var loaded = service.Load(first, CanonicalSaveService.DefaultIdentity("save.roundtrip"));

        await Assert.That(loaded.Success).IsTrue();
        var plan = loaded.LoadPlan as PersistenceLoadPlan;
        await Assert.That(plan).IsNotNull();
        await Assert.That(plan!.Steps.Select(x => x.ContributorId)).IsEquivalentTo(PersistenceContributorRegistry.RequiredIds);
        var second = service.Capture(loaded.Runtime!, CanonicalSaveService.DefaultIdentity("save.roundtrip"));
        await Assert.That(first.Canonical).IsEqualTo(second.Canonical);
        await Assert.That(first.Snapshot.RemovedEntities).Contains(PersistentIds.Crystal);
        await Assert.That(first.Canonical.Contains("soundCommands", StringComparison.Ordinal)).IsFalse();
        await Assert.That(first.Snapshot.Entities.Select(x => x.Id)).IsEquivalentTo(first.Snapshot.Entities.Select(x => x.Id).Order(StringComparer.Ordinal));
        await Assert.That(first.Manifest.Contributors.Select(x => x.Id)).IsEquivalentTo(first.Manifest.Contributors.Select(x => x.Id).Order(StringComparer.Ordinal));
    }

    [Test]
    public async Task ContributorCompatibilityRejectsMissingAndUnknownRequiredButAllowsSafeOptional()
    {
        var service = new CanonicalSaveService();
        var save = service.Capture(PersistentWorldRuntime.CreateInitial(), CanonicalSaveService.DefaultIdentity("save.compat"));
        var missing = save with { Manifest = save.Manifest with { Contributors = save.Manifest.Contributors.Skip(1).ToArray() } };
        var unknownRequired = save with { Manifest = save.Manifest with { Contributors = save.Manifest.Contributors.Append(new("persistence.unknown", 1, true, false, "x")).ToArray() } };
        var optional = save with { Manifest = save.Manifest with { Contributors = save.Manifest.Contributors.Append(new("persistence.optional", 1, false, true, "x")).ToArray() } };

        var badFingerprint = save with { Manifest = save.Manifest with { Contributors = save.Manifest.Contributors.Select((x, index) => index == 0 ? x with { Fingerprint = "sha256:invalid" } : x).ToArray() } };
        await Assert.That(service.Load(missing, CanonicalSaveService.DefaultIdentity("save.compat")).Success).IsFalse();
        await Assert.That(service.Load(unknownRequired, CanonicalSaveService.DefaultIdentity("save.compat")).Diagnostics.Any(x => x.Contains("unknown", StringComparison.Ordinal))).IsTrue();
        await Assert.That(service.Load(optional, CanonicalSaveService.DefaultIdentity("save.compat")).Success).IsTrue();
        await Assert.That(service.Load(badFingerprint, CanonicalSaveService.DefaultIdentity("save.compat")).Diagnostics.Any(x => x.Contains("fingerprint", StringComparison.Ordinal))).IsTrue();
    }
    [Test]
    public async Task FlagsConditionsSwitchesAndDoorsCommitAtTheRuntimeBoundary()
    {
        var runtime = PersistentWorldRuntime.CreateInitial();
        runtime.AdvanceTo(1);
        var noOp = runtime.SetFlag(PersistentIds.VaultAccess, "false", "flag.noop", "test.1");
        var blocked = runtime.OpenDoor("door.blocked", "test.2");
        runtime.CollectCrystal("collect.crystal", "test.3");
        runtime.AdvanceTo(2);
        var switched = runtime.ActivateSwitch("switch.activate", "test.4");
        var repeated = runtime.ActivateSwitch("switch.repeat", "test.5");
        var opened = runtime.OpenDoor("door.open", "test.6");

        await Assert.That(noOp.Status).IsEqualTo("accepted");
        await Assert.That(runtime.FlagTransitions[0].NoOp).IsTrue();
        await Assert.That(blocked.RejectionReason).IsEqualTo("condition-failed");
        await Assert.That(switched.Status).IsEqualTo("accepted");
        await Assert.That(repeated.RejectionReason).IsEqualTo("already-activated");
        await Assert.That(opened.Status).IsEqualTo("accepted");
        await Assert.That(runtime.Doors[PersistentIds.Door].CollisionEnabled).IsFalse();
        await Assert.That(runtime.Invalidations.Select(x => x.Projection)).Contains("spatial");
        await Assert.That(runtime.Events.Select(x => x.Type)).Contains("door.opened");
    }

    [Test]
    public async Task BoundedConditionCompositionIsInspectableAndSideEffectFree()
    {
        var runtime = PersistentWorldRuntime.CreateInitial();
        var condition = new NotCondition(new AnyCondition([new FlagEqualsCondition(PersistentIds.VaultPower, "true"), new LifecycleEqualsCondition(PersistentIds.Player, "defeated")]));
        var evidence = condition.Evaluate(runtime);

        await Assert.That(evidence.Result).IsTrue();
        await Assert.That(evidence.Children.Single().Kind).IsEqualTo("any");
        await Assert.That(runtime.Events).IsEmpty();
    }

    [Test]
    public async Task ClosedEnumFlagsAcceptOnlyAuthoredValuesAndPersistTheirState()
    {
        var runtime = PersistentWorldRuntime.CreateInitial();
        runtime.RegisterFlag(new FlagDefinition("flag.test.mode", "enum", ["idle", "armed"]), "idle");
        var accepted = runtime.SetFlag("flag.test.mode", "armed", "enum.accept", "test.enum.1");
        var rejected = runtime.SetFlag("flag.test.mode", "other", "enum.reject", "test.enum.2");

        await Assert.That(accepted.Status).IsEqualTo("accepted");
        await Assert.That(rejected.RejectionReason).IsEqualTo("invalid-flag-value");
        await Assert.That(runtime.Snapshot().Flags.Single(x => x.Id == "flag.test.mode").Value).IsEqualTo("armed");
    }
}
