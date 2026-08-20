using Agentic2D.Engineering;

namespace Agentic2D.Tests.Unit;

public sealed class PlatformVerificationTests
{
    [Test]
    public async Task WindowsEpochIsValidAndLinuxDebtTargetsInactivePlatform()
    {
        var state = WindowsState();
        PlatformVerificationState.Validate(state);
        await Assert.That(state.ActivePlatform).IsEqualTo("windows");
        await Assert.That(PlatformVerificationPolicy.EvidenceStatus(state, "linux", false)).IsEqualTo("deferred-inactive-platform");
    }

    [Test]
    public async Task ActiveWindowsGraphicsCannotPassWithoutExecution()
    {
        await Assert.That(PlatformVerificationPolicy.EvidenceStatus(WindowsState(), "windows", false)).IsEqualTo("pending-active-platform-proof");
        await Assert.That(PlatformVerificationPolicy.EvidenceStatus(WindowsState(), "windows", true)).IsEqualTo("passed");
    }

    [Test]
    public async Task InvalidEpochAndDeferredActivePlatformAreRejected()
    {
        await Assert.That(() => PlatformVerificationState.Validate(WindowsState() with { ActiveEpoch = new("", "", "M036", "invalid") })).Throws<EngineeringException>();
        await Assert.That(() => PlatformVerificationState.Validate(WindowsState() with { DeferredVerification = [new("windows", "M037", ["native"], null)] })).Throws<EngineeringException>();
    }

    [Test]
    public async Task ReviewDrivesCompletionWhileLinuxDebtRemains()
    {
        await Assert.That(PlatformVerificationPolicy.CompletionOutcome("pending", true)).IsEqualTo("AWAITING HUMAN REVIEW");
        await Assert.That(PlatformVerificationPolicy.CompletionOutcome("approved", true)).IsEqualTo("COMPLETE");
        await Assert.That(PlatformVerificationPolicy.CompletionOutcome("approved", false)).IsEqualTo("BLOCKED");
    }

    [Test]
    public async Task EpochFingerprintChangesWhenAuthoredStateChanges()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-platform-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "eng"));
        try
        {
            var first = "{\"schema\":\"agentic2d.engineering.platform-verification.v1\",\"supportedDevelopmentPlatforms\":[\"linux\",\"windows\"],\"activeEpoch\":{\"id\":\"windows-m036\",\"platform\":\"windows\",\"startedAtMilestone\":\"M036\",\"reason\":\"test\"},\"deferredVerification\":[]}";
            var second = first.Replace("windows-m036", "linux-m038", StringComparison.Ordinal).Replace("\"platform\":\"windows\"", "\"platform\":\"linux\"", StringComparison.Ordinal);
            File.WriteAllText(Path.Combine(root, "eng", "platform-verification.json"), first);
            var before = Fingerprints.Input(root, new ValidationShard("platform", "", "", ["eng/platform-verification.json"], [], true));
            File.WriteAllText(Path.Combine(root, "eng", "platform-verification.json"), second);
            var after = Fingerprints.Input(root, new ValidationShard("platform", "", "", ["eng/platform-verification.json"], [], true));
            await Assert.That(after).IsNotEqualTo(before);
        }
        finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
    }

    private static PlatformVerificationState WindowsState() => new(
        "agentic2d.engineering.platform-verification.v1",
        ["linux", "windows"],
        new("windows-m036", "windows", "M036", "test"),
        [new("linux", "M037", ["graphics"], "future-catch-up")]);
}
