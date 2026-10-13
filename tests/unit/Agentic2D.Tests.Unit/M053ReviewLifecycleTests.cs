using System.Collections.Concurrent;
using System.Text.Json;
using Agentic2D.Engineering;
using Agentic2D.Tools;
using Agentic2D.UI;

namespace Agentic2D.Tests.Unit;

public sealed class M053ReviewLifecycleTests
{
    [Test]
    public async Task ReviewReadinessRejectsEveryStaleSuiteFingerprintReceiptAndNativeArtifact()
    {
        var (root, suite, host) = await CreateVerifiedReadinessFixtureAsync();
        await Assert.That(host.HasCurrentSuccessfulVerification(suite, out _)).IsTrue();

        await File.AppendAllTextAsync(Path.Combine(root, "src", "relevant-source.cs"), "// changed");
        await Assert.That(host.HasCurrentSuccessfulVerification(suite, out _)).IsFalse();

        (root, suite, host) = await CreateVerifiedReadinessFixtureAsync();
        File.Delete(Path.Combine(root, "artifacts", "validation", suite.Id, "windows-native.json"));
        await Assert.That(host.HasCurrentSuccessfulVerification(suite, out _)).IsFalse();

        (root, suite, host) = await CreateVerifiedReadinessFixtureAsync();
        await File.WriteAllTextAsync(Path.Combine(root, "artifacts", "ui", "M053", "native", "review.png"), "changed native capture");
        await Assert.That(host.HasCurrentSuccessfulVerification(suite, out _)).IsFalse();
        File.Delete(Path.Combine(root, "artifacts", "ui", "M053", "native", "review.png"));
        await Assert.That(host.HasCurrentSuccessfulVerification(suite, out _)).IsFalse();

        (root, suite, host) = await CreateVerifiedReadinessFixtureAsync();
        await File.WriteAllTextAsync(Path.Combine(root, "src", "review-input.json"), "tampered input");
        await Assert.That(host.HasCurrentSuccessfulVerification(suite, out _)).IsFalse();

        (root, suite, host) = await CreateVerifiedReadinessFixtureAsync();
        var verificationPath = Path.Combine(root, "artifacts", "validation", suite.Id, "verify.json");
        var staleReport = await File.ReadAllTextAsync(verificationPath);
        await File.WriteAllTextAsync(verificationPath, staleReport.Replace("\"suiteFingerprint\":", "\"suiteFingerprint\": \"stale\", \"oldSuiteFingerprint\":", StringComparison.Ordinal));
        await Assert.That(host.HasCurrentSuccessfulVerification(suite, out _)).IsFalse();

        (root, suite, host) = await CreateVerifiedReadinessFixtureAsync();
        var nativeReceiptPath = Path.Combine(root, "artifacts", "validation", suite.Id, "windows-native.json");
        var receipt = JsonSerializer.Deserialize<ValidationReceipt>(await File.ReadAllTextAsync(nativeReceiptPath), JsonOptions)!;
        await File.WriteAllTextAsync(nativeReceiptPath, JsonSerializer.Serialize(receipt with { Status = "failed" }, JsonOptions));
        await Assert.That(host.HasCurrentSuccessfulVerification(suite, out _)).IsFalse();

        (root, suite, host) = await CreateVerifiedReadinessFixtureAsync();
        await File.WriteAllTextAsync(Path.Combine(root, "artifacts", "ui", "M053", "native", "review.png"), "new actual capture bytes");
        await WriteCurrentReceiptsAsync(root, suite);
        await Assert.That(host.Verify(suite, TextWriter.Null)).IsTrue();
        await Assert.That(host.HasCurrentSuccessfulVerification(suite, out _)).IsTrue();
    }

    [Test]
    public async Task FinalPageRendersOrderedDecisionsAndActualPersistenceState()
    {
        var projection = new M053WorkbenchScreens.ReviewProjection("M053", "review.m053.final", "not shown on final page", 3, 3,
            "pending", "failed", "review.m053.two: disk unavailable", false, true, false, false, false, FinalPage: true,
            DecisionSummary:
            [
                new(1, "Accepted", "Saved"),
                new(2, "Rejected", "Failed", "disk unavailable"),
                new(3, "Accepted", "Saving (1 pending)")
            ], PendingCount: 1);
        var layout = UiLayoutEngine.Resolve(M053WorkbenchScreens.Review(projection), new UiViewport(1920, 1080));
        var rows = layout.Elements.Where(item => item.Id.StartsWith("review.summary.item.", StringComparison.Ordinal)).OrderBy(item => item.Id, StringComparer.Ordinal).ToArray();
        await Assert.That(string.Join("\n", rows.Select(item => item.Text))).IsEqualTo(string.Join("\n", [
            "Item 1 — Accepted — Saved",
            "Item 2 — Rejected — Failed: disk unavailable",
            "Item 3 — Accepted — Saving (1 pending)"]));
        await Assert.That(layout.Elements.Single(item => item.Id == "review.question").Text).IsEqualTo("Review pass complete");
        await Assert.That(layout.Elements.Single(item => item.Id == "review.close").Enabled).IsFalse();
        await Assert.That(layout.Elements.Single(item => item.Id == "review.retry").Enabled).IsTrue();
        await Assert.That(layout.Elements.Single(item => item.Id == "review.pending-count").Text).IsEqualTo("Pending operations: 1");

        var complete = projection with
        {
            OperationStatus = "saved",
            Feedback = "All decision writes persisted.",
            CanRetry = false,
            CanClose = true,
            PendingCount = 0,
            DecisionSummary = [new(1, "Accepted", "Saved"), new(2, "Rejected", "Saved"), new(3, "Accepted", "Saved")]
        };
        var completeLayout = UiLayoutEngine.Resolve(M053WorkbenchScreens.Review(complete), new UiViewport(1920, 1080));
        await Assert.That(completeLayout.Elements.Single(item => item.Id == "review.close").Enabled).IsTrue();
        await Assert.That(completeLayout.Elements.Single(item => item.Id == "review.retry").Enabled).IsFalse();
    }

    [Test]
    public async Task DecisionQueueIsBoundedFifoAndShutdownWaitsForEveryAcceptedOperation()
    {
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var actualCalls = new ConcurrentQueue<string>();
        await using var queue = new ReviewDecisionQueue(3, async operation =>
        {
            actualCalls.Enqueue(operation.Id + ":started");
            if (operation.Id == "one") { firstEntered.TrySetResult(); await releaseFirst.Task; }
            actualCalls.Enqueue(operation.Id + ":persisted");
            return new(true, "canonical operation completed");
        });

        await Assert.That(queue.TryEnqueue(new("one", "approved"))).IsTrue();
        await firstEntered.Task;
        var close = new ReviewWorkbenchCloseLifecycle(); close.RequestClose();
        await Assert.That(close.ShouldExit(queue.PendingCount, queue.FailedOperations.Count, false)).IsFalse();
        await Assert.That(queue.TryEnqueue(new("two", "changes-requested"))).IsTrue();
        await Assert.That(queue.TryEnqueue(new("three", "approved"))).IsTrue();
        await Assert.That(queue.TryEnqueue(new("four", "approved"))).IsFalse();
        var idle = queue.WaitForIdleAsync();
        await Assert.That(idle.IsCompleted).IsFalse();
        releaseFirst.TrySetResult();
        await idle;
        await queue.CompleteAsync();

        await Assert.That(actualCalls.ToArray()).IsEquivalentTo([
            "one:started", "one:persisted", "two:started", "two:persisted", "three:started", "three:persisted"]);
        await Assert.That(queue.PendingCount).IsEqualTo(0);
        await Assert.That(close.ShouldExit(queue.PendingCount, queue.FailedOperations.Count, false)).IsTrue();
        await Assert.That(queue.TakeCompletions().Select(result => result.Id)).IsEquivalentTo(["one", "two", "three"]);
        await Assert.That(queue.TryEnqueue(new("one", "approved"))).IsFalse();
    }

    [Test]
    public async Task CloseRequestWaitsForSavesFailureAndResetThenExitsWithoutSecondRequest()
    {
        var lifecycle = new ReviewWorkbenchCloseLifecycle();
        await Assert.That(lifecycle.CanExit(0, 0, false)).IsTrue();
        await Assert.That(lifecycle.ShouldExit(0, 0, false)).IsFalse();

        lifecycle.RequestClose();
        await Assert.That(lifecycle.ShouldExit(2, 0, false)).IsFalse();
        await Assert.That(lifecycle.BlocksNewActions).IsTrue();
        lifecycle.RequestClose();
        await Assert.That(lifecycle.ShouldExit(0, 1, false)).IsFalse();
        await Assert.That(lifecycle.ShouldExit(1, 0, false)).IsFalse();
        await Assert.That(lifecycle.ShouldExit(0, 0, true)).IsFalse();
        await Assert.That(lifecycle.ShouldExit(0, 0, false)).IsTrue();
    }

    [Test]
    public async Task ClosingWithOnlyUndecidedReviewsExitsWithoutWritingAnyDecision()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m053-close-undecided", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".review", "pending"));
        Directory.CreateDirectory(Path.Combine(root, "docs", "milestones"));
        await File.WriteAllTextAsync(Path.Combine(root, "docs", "milestones", "MILESTONE-053-active.md"), "# Active M053");
        var pending = CreateReview("review.m053.close-undecided", "M053", "pending", ".review/pending/review.m053.close-undecided.json");
        await WriteReviewAsync(root, pending);
        var path = Path.Combine(root, pending.Path.Replace('/', Path.DirectorySeparatorChar));
        var before = await File.ReadAllBytesAsync(path);
        await using var queue = new ReviewDecisionQueue(1, _ => throw new InvalidOperationException("No write should be enqueued."));
        var close = new ReviewWorkbenchCloseLifecycle(); close.RequestClose(); close.RequestClose();

        await Assert.That(queue.PendingCount).IsEqualTo(0);
        await Assert.That(close.ShouldExit(queue.PendingCount, queue.FailedOperations.Count, false)).IsTrue();
        await queue.CompleteAsync();
        await Assert.That(Convert.ToHexString(await File.ReadAllBytesAsync(path))).IsEqualTo(Convert.ToHexString(before));
        using var record = JsonDocument.Parse(await File.ReadAllTextAsync(path));
        await Assert.That(record.RootElement.GetProperty("status").GetString()).IsEqualTo("pending");
        await Assert.That(record.RootElement.GetProperty("decisionHistory").GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task FailedDecisionRemainsFailedUntilRetryOperationActuallySucceeds()
    {
        var attempt = 0;
        var retryEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var releaseRetry = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var queue = new ReviewDecisionQueue(2, async operation =>
        {
            if (++attempt == 1) return new(false, "injected disk failure");
            retryEntered.TrySetResult(); await releaseRetry.Task; return new(true, "persisted");
        });
        var close = new ReviewWorkbenchCloseLifecycle();
        await Assert.That(queue.TryEnqueue(new("item.a", "approved"))).IsTrue();
        close.RequestClose();
        await queue.WaitForIdleAsync();
        var first = queue.TakeCompletions().Single();
        await Assert.That(first.Success).IsFalse();
        await Assert.That(first.Message).IsEqualTo("injected disk failure");
        await Assert.That(queue.FailedOperations.Select(item => item.Id)).IsEquivalentTo(["item.a"]);
        await Assert.That(close.ShouldExit(queue.PendingCount, queue.FailedOperations.Count, false)).IsFalse();

        await Assert.That(queue.RetryFailed()).IsEqualTo(1);
        await retryEntered.Task;
        await Assert.That(queue.RetryFailed()).IsEqualTo(0);
        await Assert.That(close.ShouldExit(queue.PendingCount, queue.FailedOperations.Count, false)).IsFalse();
        releaseRetry.TrySetResult();
        await queue.WaitForIdleAsync();
        var retry = queue.TakeCompletions().Single();
        await Assert.That(retry.IsRetry).IsTrue();
        await Assert.That(retry.Success).IsTrue();
        await Assert.That(queue.FailedOperations).IsEmpty();
        await Assert.That(close.ShouldExit(queue.PendingCount, queue.FailedOperations.Count, false)).IsTrue();
    }

    [Test]
    public async Task RepeatedRetryFailureStaysOpenAndDoesNotConvertFailedWriteToSuccess()
    {
        await using var queue = new ReviewDecisionQueue(1, _ => Task.FromResult(new ReviewDecisionOperationResult(false, "still offline")));
        var close = new ReviewWorkbenchCloseLifecycle();
        close.RequestClose();
        await Assert.That(queue.TryEnqueue(new("item.a", "changes-requested"))).IsTrue();
        await queue.WaitForIdleAsync();
        await Assert.That(queue.TakeCompletions().Single().Success).IsFalse();
        await Assert.That(queue.RetryFailed()).IsEqualTo(1);
        await queue.WaitForIdleAsync();
        var retry = queue.TakeCompletions().Single();
        await Assert.That(retry.Success).IsFalse();
        await Assert.That(retry.Message).IsEqualTo("still offline");
        await Assert.That(queue.RetryFailed()).IsEqualTo(1);
        await Assert.That(queue.RetryFailed()).IsEqualTo(0);
        await Assert.That(close.ShouldExit(queue.PendingCount, queue.FailedOperations.Count, false)).IsFalse();
    }

    [Test]
    public async Task RestartReopensActiveSetAndLeavesCompletedMilestoneApprovalByteForByteUnchanged()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m053-reset-tests", Guid.NewGuid().ToString("N"));
        var pendingDirectory = Path.Combine(root, ".review", "pending");
        var recordsDirectory = Path.Combine(root, ".review", "records");
        Directory.CreateDirectory(pendingDirectory); Directory.CreateDirectory(recordsDirectory);
        Directory.CreateDirectory(Path.Combine(root, "docs", "milestones"));
        await File.WriteAllTextAsync(Path.Combine(root, "docs", "milestones", "MILESTONE-053-active.md"), "# Active M053");

        var activeApproved = CreateReview("review.m053.initially-approved", "M053", "approved", ".review/records/review.m053.initially-approved.json");
        var activePending = CreateReview("review.m053.initially-pending", "M053", "pending", ".review/pending/review.m053.initially-pending.json");
        var activeChanges = CreateReview("review.m053.changes-requested", "M053", "changes-requested", ".review/pending/review.m053.changes-requested.json");
        var completed = CreateReview("review.m052.completed-history", "M052", "approved", ".review/records/review.m052.completed-history.json");
        await WriteReviewAsync(root, activeApproved);
        await WriteReviewAsync(root, activePending);
        await WriteReviewAsync(root, activeChanges);
        await WriteReviewAsync(root, completed);
        var completedPath = Path.Combine(root, completed.Path.Replace('/', Path.DirectorySeparatorChar));
        var completedBefore = await File.ReadAllBytesAsync(completedPath);

        var host = new EngineeringHost(root);
        await Assert.That(await host.ResetReviewsAsync("M053", TextWriter.Null)).IsEqualTo(0);
        foreach (var expectedId in new[] { activeApproved.Id, activePending.Id, activeChanges.Id })
        {
            var path = Path.Combine(pendingDirectory, expectedId + ".json");
            await Assert.That(File.Exists(path)).IsTrue();
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(path));
            await Assert.That(document.RootElement.GetProperty("status").GetString()).IsEqualTo("pending");
            await Assert.That(document.RootElement.GetProperty("decision").GetString()).IsEqualTo(string.Empty);
        }
        await Assert.That(await File.ReadAllBytesAsync(completedPath)).IsEquivalentTo(completedBefore);
        await Assert.That(File.Exists(Path.Combine(pendingDirectory, completed.Id + ".json"))).IsFalse();
    }

    [Test]
    public async Task CanonicalDecisionAdapterPersistsAcceptRejectAndDuplicateRetryWithoutDuplicateHistory()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m053-decide-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".review", "pending"));
        Directory.CreateDirectory(Path.Combine(root, "docs", "milestones"));
        await File.WriteAllTextAsync(Path.Combine(root, "docs", "milestones", "MILESTONE-053-active.md"), "# Active M053");
        await WriteReviewAsync(root, CreateReview("review.m053.decision.accept", "M053", "pending", ".review/pending/review.m053.decision.accept.json"));
        await WriteReviewAsync(root, CreateReview("review.m053.decision.reject", "M053", "pending", ".review/pending/review.m053.decision.reject.json"));
        var host = new EngineeringHost(root);

        await Assert.That(await host.DecideReviewAsync("review.m053.decision.accept", "approved", TextWriter.Null)).IsEqualTo(0);
        await Assert.That(await host.DecideReviewAsync("review.m053.decision.accept", "approved", TextWriter.Null)).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(root, ".review", "pending", "review.m053.decision.accept.json"))).IsFalse();
        using (var accepted = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, ".review", "records", "review.m053.decision.accept.json"))))
        {
            await Assert.That(accepted.RootElement.GetProperty("status").GetString()).IsEqualTo("approved");
            await Assert.That(accepted.RootElement.GetProperty("decisionHistory").GetArrayLength()).IsEqualTo(1);
        }

        await Assert.That(await host.DecideReviewAsync("review.m053.decision.reject", "changes-requested", TextWriter.Null)).IsEqualTo(0);
        await Assert.That(await host.DecideReviewAsync("review.m053.decision.reject", "changes-requested", TextWriter.Null)).IsEqualTo(0);
        using var rejected = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(root, ".review", "pending", "review.m053.decision.reject.json")));
        await Assert.That(rejected.RootElement.GetProperty("status").GetString()).IsEqualTo("changes-requested");
        await Assert.That(rejected.RootElement.GetProperty("decisionHistory").GetArrayLength()).IsEqualTo(1);
    }

    [Test]
    public async Task CloseDrainRetryUsesProductionReviewAdapterAndPersistsEveryAcceptedQueueJobOnce()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m053-close-adapter", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".review", "pending"));
        Directory.CreateDirectory(Path.Combine(root, "docs", "milestones"));
        await File.WriteAllTextAsync(Path.Combine(root, "docs", "milestones", "MILESTONE-053-active.md"), "# Active M053");
        foreach (var id in new[] { "review.m053.queue-first", "review.m053.queue-second" })
            await WriteReviewAsync(root, CreateReview(id, "M053", "pending", ".review/pending/" + id + ".json"));
        var host = new EngineeringHost(root);
        var secondAttempts = 0;
        await using var queue = new ReviewDecisionQueue(2, async operation =>
        {
            if (operation.Id == "review.m053.queue-second" && ++secondAttempts == 1)
                return new(false, "injected transient failure before canonical adapter");
            var exitCode = await host.DecideReviewAsync(operation.Id, operation.Decision, TextWriter.Null);
            return new(exitCode == 0, exitCode == 0 ? "canonical review persisted" : "canonical review command failed");
        });
        var close = new ReviewWorkbenchCloseLifecycle();
        await Assert.That(queue.TryEnqueue(new("review.m053.queue-first", "approved"))).IsTrue();
        await Assert.That(queue.TryEnqueue(new("review.m053.queue-second", "changes-requested"))).IsTrue();
        close.RequestClose();
        close.RequestClose();
        await queue.WaitForIdleAsync();
        var initial = queue.TakeCompletions().ToArray();
        await Assert.That(initial.Select(item => item.Id)).IsEquivalentTo(["review.m053.queue-first", "review.m053.queue-second"]);
        await Assert.That(initial[0].Success).IsTrue();
        await Assert.That(initial[1].Success).IsFalse();
        await Assert.That(close.ShouldExit(queue.PendingCount, queue.FailedOperations.Count, false)).IsFalse();
        await Assert.That(File.Exists(Path.Combine(root, ".review", "records", "review.m053.queue-first.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(root, ".review", "pending", "review.m053.queue-second.json"))).IsTrue();

        await Assert.That(queue.RetryFailed()).IsEqualTo(1);
        await queue.WaitForIdleAsync();
        var retry = queue.TakeCompletions().Single();
        await Assert.That(retry.Success).IsTrue();
        await Assert.That(retry.IsRetry).IsTrue();
        await Assert.That(close.ShouldExit(queue.PendingCount, queue.FailedOperations.Count, false)).IsTrue();
        await queue.CompleteAsync();

        foreach (var id in new[] { "review.m053.queue-first", "review.m053.queue-second" })
        {
            var recordPath = id.EndsWith("second", StringComparison.Ordinal)
                ? Path.Combine(root, ".review", "pending", id + ".json")
                : Path.Combine(root, ".review", "records", id + ".json");
            using var record = JsonDocument.Parse(await File.ReadAllTextAsync(recordPath));
            await Assert.That(record.RootElement.GetProperty("status").GetString()).IsEqualTo(id.EndsWith("second", StringComparison.Ordinal) ? "changes-requested" : "approved");
            await Assert.That(record.RootElement.GetProperty("decisionHistory").GetArrayLength()).IsEqualTo(1);
        }
        await Assert.That(secondAttempts).IsEqualTo(2);
    }

    [Test]
    public async Task ChangedPreviewMaterializationSubjectInvalidatesPriorAcknowledgement()
    {
        var acknowledged = new M048ActualCandidatePreview.Draft("candidate.current", "candidate-fingerprint.1", null, null, [],
            "recipe.fingerprint.1", "materialization.subject.1", "acknowledged", "materialization.subject.1");
        await Assert.That(acknowledged.IsPreviewCurrent).IsTrue();
        await Assert.That(M048ActualCandidatePreview.CanCommitApproval(acknowledged, "candidate-fingerprint.1", acknowledged.AcknowledgedMaterializationSubjectFingerprint)).IsTrue();

        var changed = acknowledged with { CandidateFingerprint = "candidate-fingerprint.2", MaterializationSubjectFingerprint = "materialization.subject.2" };
        await Assert.That(changed.IsPreviewCurrent).IsFalse();
        await Assert.That(M048ActualCandidatePreview.CanCommitApproval(changed, "candidate-fingerprint.2", acknowledged.AcknowledgedMaterializationSubjectFingerprint)).IsFalse();
        await Assert.That(() => M048ActualCandidatePreview.CommitApproval(changed, "campaign.current", "decision.current", "accept-proposal", "confirm", 1))
            .Throws<InvalidOperationException>();
    }

    [Test]
    public async Task AudioPreviewCannotClaimPlaybackWithoutReadyDeviceAndLoadedMedia()
    {
        await Assert.That(PreviewAudioPlaybackPolicy.CanStartPlayback(false, true)).IsFalse();
        await Assert.That(PreviewAudioPlaybackPolicy.CanStartPlayback(true, false)).IsFalse();
        await Assert.That(PreviewAudioPlaybackPolicy.CanStartPlayback(false, false)).IsFalse();
        await Assert.That(PreviewAudioPlaybackPolicy.CanStartPlayback(true, true)).IsTrue();
    }

    private static ReviewState CreateReview(string id, string milestone, string status, string path) => new(
        milestone == "M053" ? "agentic2d.engineering.review-request.v2" : "agentic2d.engineering.review-record.v2",
        id, milestone, $"docs/milestones/MILESTONE-{milestone[1..]}-active.md", "Is this review surface clear?", ["UX"], "required", "human reviewer", status,
        ["artifacts/ui/" + milestone + "/review.png"], ["Readable", "Usable"], ["approved"], "No implicit waiver.", status == "approved" ? "approved" : string.Empty,
        status == "approved" ? "old-revision" : string.Empty, status == "approved" ? "old-fingerprint" : string.Empty, [], status == "approved" ? DateTimeOffset.UtcNow : null, [], string.Empty, path);

    private static async Task WriteReviewAsync(string root, ReviewState review)
    {
        var path = Path.Combine(root, review.Path.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(review, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true }));
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true };

    private static async Task<(string Root, ValidationSuite Suite, EngineeringHost Host)> CreateVerifiedReadinessFixtureAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-m053-readiness-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "eng")); Directory.CreateDirectory(Path.Combine(root, "src"));
        Directory.CreateDirectory(Path.Combine(root, "artifacts", "ui", "M053", "native"));
        Directory.CreateDirectory(Path.Combine(root, "artifacts", "validation", "m053-workbenches"));
        await File.WriteAllTextAsync(Path.Combine(root, "eng", "platform-verification.json"), "{}");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "relevant-source.cs"), "class CurrentSource {}");
        await File.WriteAllTextAsync(Path.Combine(root, "src", "review-input.json"), "current input");
        await File.WriteAllTextAsync(Path.Combine(root, "artifacts", "ui", "M053", "headless.json"), "current headless evidence");
        await File.WriteAllTextAsync(Path.Combine(root, "artifacts", "ui", "M053", "native", "review.png"), "actual capture bytes");
        var suite = new ValidationSuite("m053-workbenches", "resumable-sharded",
        [
            new("headless-compositions", "headless", "internal:m053", ["src/review-input.json", "artifacts/ui/M053/headless.json"], [], true),
            new("windows-native", "native", "internal:m053", ["artifacts/ui/M053/native/review.png"], [], true)
        ]);
        await WriteCurrentReceiptsAsync(root, suite);
        await File.WriteAllTextAsync(Path.Combine(root, "artifacts", "validation", suite.Id, "verify.json"), VerificationReport(suite, root, "passed"));
        return (root, suite, new EngineeringHost(root));
    }

    private static async Task WriteCurrentReceiptsAsync(string root, ValidationSuite suite)
    {
        var suiteFingerprint = Fingerprints.Suite(suite, root);
        var repositoryFingerprint = Fingerprints.Repository(root);
        foreach (var shard in suite.Shards)
        {
            var artifacts = shard.Evidence.Select(path => new ArtifactFingerprint(path, Fingerprints.Path(Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))))).ToArray();
            var receipt = new ValidationReceipt("agentic2d.engineering.validation-receipt.v1", suite.Id, shard.Id, "passed", suiteFingerprint,
                repositoryFingerprint, Fingerprints.Command(shard), Fingerprints.Input(root, shard), Fingerprints.Result(artifacts, root), shard.Command,
                shard.Evidence, artifacts, new CompletionMetadata(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "windows-pwsh-7"), []);
            var path = Path.Combine(root, "artifacts", "validation", suite.Id, shard.Id + ".json");
            ReceiptStore.WriteAtomic(path, receipt, JsonOptions);
        }
    }

    private static string VerificationReport(ValidationSuite suite, string root, string status) => JsonSerializer.Serialize(new
    {
        schema = "agentic2d.m053.verification.v1",
        suite = suite.Id,
        status,
        suiteFingerprint = Fingerprints.Suite(suite, root),
        repositoryFingerprint = Fingerprints.Repository(root),
        currentReceipts = suite.Shards.Count,
        verifiedReceipts = status == "passed" ? suite.Shards.Count : 0
    }, JsonOptions);
}
