using System.Text.Json;
using Agentic2D.Engineering;

namespace Agentic2D.Tests.Unit;

public sealed class ReviewCommandTests
{
    [Test]
    public async Task RequestCreatesOnlyAnOwnedCanonicalPendingReview()
    {
        var root = await CreateActiveM027RootAsync();
        File.Delete(Path.Combine(root, ".review", "pending", ReviewId + ".json"));

        var code = await EngineeringCli.RunAsync(["review", "request", "--milestone", "M027", "--id", ReviewId, "--subject", "M027 review", "--class", "migration", "--level", "blocking", "--criteria", "Review usability", "--evidence", "artifacts/review/M027", "--waiver-policy", "No implicit waiver."], root, new StringWriter(), new StringWriter());

        var request = await File.ReadAllTextAsync(Path.Combine(root, ".review", "pending", ReviewId + ".json"));
        await Assert.That(code).IsEqualTo(0);
        await Assert.That(request).Contains("\"owningMilestone\": \"M027\"");
        await Assert.That(request).Contains("\"status\": \"pending\"");
        await Assert.That(request).DoesNotContain("\"decision\": \"approved\"");
    }

    [Test]
    public async Task ShowByCanonicalIdDisplaysActiveRequestFields()
    {
        var root = await CreateActiveM027RootAsync();
        var output = new StringWriter();

        var code = await EngineeringCli.RunAsync(["review", "show", ReviewId], root, output, new StringWriter());

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("Canonical review ID: " + ReviewId);
        await Assert.That(output.ToString()).Contains("Owning milestone: M027");
        await Assert.That(output.ToString()).Contains("State: active");
        await Assert.That(output.ToString()).Contains("Required evidence: artifacts/review/M027");
        await Assert.That(output.ToString()).Contains("Acceptance criteria: Review usability");
    }

    [Test]
    public async Task ShowByCurrentAliasResolvesTheListedReview()
    {
        var root = await CreateActiveM027RootAsync();
        await EngineeringCli.RunAsync(["review", "list", "--milestone", "M027"], root, new StringWriter(), new StringWriter());
        var output = new StringWriter();

        var code = await EngineeringCli.RunAsync(["review", "show", "1"], root, output, new StringWriter());

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("Canonical review ID: " + ReviewId);
    }

    [Test]
    public async Task HistoricalShowDoesNotEvaluateCurrentRepositoryFingerprint()
    {
        var root = await CreateActiveM027RootAsync();
        await WriteHistoricalM025RecordAsync(root);
        var output = new StringWriter();

        var code = await EngineeringCli.RunAsync(["review", "show", HistoricalId], root, output, new StringWriter());

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("State: historical");
        await Assert.That(output.ToString()).Contains("Provenance fingerprint: obsolete-fingerprint");
    }

    [Test]
    public async Task StaleAliasesAreRejectedForShowAndRecord()
    {
        var root = await CreateActiveM027RootAsync();
        await EngineeringCli.RunAsync(["review", "list", "--milestone", "M027"], root, new StringWriter(), new StringWriter());
        await EngineeringCli.RunAsync(["review", "record", ReviewId, "changes-requested", "--notes", "needs revision"], root, new StringWriter(), new StringWriter());
        var showError = new StringWriter();
        var recordError = new StringWriter();

        var showCode = await EngineeringCli.RunAsync(["review", "show", "1"], root, new StringWriter(), showError);
        var recordCode = await EngineeringCli.RunAsync(["review", "record", "1", "approved"], root, new StringWriter(), recordError);

        await Assert.That(showCode).IsEqualTo(1);
        await Assert.That(recordCode).IsEqualTo(1);
        await Assert.That(showError.ToString()).Contains("Review alias is stale or unknown. Run ./eng/review-list.sh again.");
        await Assert.That(recordError.ToString()).Contains("Review alias is stale or unknown. Run ./eng/review-list.sh again.");
    }

    [Test]
    public async Task RecordByCanonicalIdWritesCanonicalDurableState()
    {
        var root = await CreateActiveM027RootAsync();

        var code = await EngineeringCli.RunAsync(["review", "record", ReviewId, "approved", "--reviewer", "reviewer", "--notes", "approved after review"], root, new StringWriter(), new StringWriter());

        var recordPath = Path.Combine(root, ".review", "records", ReviewId + ".json");
        await Assert.That(code).IsEqualTo(0);
        await Assert.That(File.Exists(recordPath)).IsTrue();
        await Assert.That(File.Exists(Path.Combine(root, ".review", "records", "1.json"))).IsFalse();
        await Assert.That(await File.ReadAllTextAsync(recordPath)).Contains("\"id\": \"" + ReviewId + "\"");
    }

    [Test]
    public async Task RecordByAliasWritesTheCanonicalIdAndNotTheAlias()
    {
        var root = await CreateActiveM027RootAsync();
        await EngineeringCli.RunAsync(["review", "list", "--milestone", "M027"], root, new StringWriter(), new StringWriter());

        var code = await EngineeringCli.RunAsync(["review", "record", "1", "approved", "--notes", "approved by alias"], root, new StringWriter(), new StringWriter());

        var recordPath = Path.Combine(root, ".review", "records", ReviewId + ".json");
        await Assert.That(code).IsEqualTo(0);
        await Assert.That(await File.ReadAllTextAsync(recordPath)).Contains("\"id\": \"" + ReviewId + "\"");
        await Assert.That(File.Exists(Path.Combine(root, ".review", "records", "1.json"))).IsFalse();
    }

    [Test]
    public async Task ReopenActiveMilestoneReviewPreservesDecisionHistory()
    {
        var root = await CreateActiveM027RootAsync();
        await EngineeringCli.RunAsync(["review", "record", ReviewId, "changes-requested", "--notes", "needs clearer evidence"], root, new StringWriter(), new StringWriter());

        var code = await EngineeringCli.RunAsync(["review", "reopen", ReviewId, "--reason", "updated evidence is ready"], root, new StringWriter(), new StringWriter());

        var pendingPath = Path.Combine(root, ".review", "pending", ReviewId + ".json");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(pendingPath));
        await Assert.That(code).IsEqualTo(0);
        await Assert.That(document.RootElement.GetProperty("status").GetString()).IsEqualTo("pending");
        await Assert.That(document.RootElement.GetProperty("decisionHistory").GetArrayLength()).IsEqualTo(2);
    }

    [Test]
    public async Task HistoricalReviewCannotBeReopenedWithoutExplicitCorrection()
    {
        var root = await CreateActiveM027RootAsync();
        await WriteHistoricalM025RecordAsync(root);
        var diagnostics = new StringWriter();

        var code = await EngineeringCli.RunAsync(["review", "reopen", HistoricalId, "--reason", "repository changed"], root, new StringWriter(), diagnostics);

        await Assert.That(code).IsEqualTo(1);
        await Assert.That(diagnostics.ToString()).Contains("Later repository changes do not reopen it; create a review for a future milestone");
    }

    [Test]
    public async Task ExplicitHistoricalRecordCorrectionCreatesASeparateActiveRequest()
    {
        var root = await CreateActiveM027RootAsync();
        await WriteHistoricalM025RecordAsync(root);

        var code = await EngineeringCli.RunAsync(["review", "reopen", HistoricalId, "--correct-record", "--reason", "reviewer name was incorrect"], root, new StringWriter(), new StringWriter());

        var correction = Directory.EnumerateFiles(Path.Combine(root, ".review", "pending"), HistoricalId + ".correction.*.json").Single();
        await Assert.That(code).IsEqualTo(0);
        await Assert.That(await File.ReadAllTextAsync(correction)).Contains("\"correctsReviewId\": \"" + HistoricalId + "\"");
    }

    [Test]
    public async Task MilestoneScopedCheckIgnoresHistoricalMilestoneRecords()
    {
        var root = await CreateActiveM027RootAsync();
        await WriteHistoricalM025RecordAsync(root, status: "rejected");
        var diagnostics = new StringWriter();

        var code = await EngineeringCli.RunAsync(["review", "check", "--milestone", "M027"], root, new StringWriter(), diagnostics);

        await Assert.That(code).IsEqualTo(1);
        await Assert.That(diagnostics.ToString()).Contains(ReviewId);
        await Assert.That(diagnostics.ToString()).DoesNotContain(HistoricalId);
    }

    [Test]
    public async Task AliasMapIsEphemeralAndDurableReviewFilesNeverContainAliases()
    {
        var root = await CreateActiveM027RootAsync();
        await EngineeringCli.RunAsync(["review", "list", "--milestone", "M027"], root, new StringWriter(), new StringWriter());

        var aliasPath = Path.Combine(root, "artifacts", "review", "session", "aliases.json");
        var durableJson = string.Join("\n", Directory.EnumerateFiles(Path.Combine(root, ".review"), "*.json", SearchOption.AllDirectories).Select(File.ReadAllText));
        await Assert.That(File.Exists(aliasPath)).IsTrue();
        await Assert.That(durableJson).DoesNotContain("\"alias\"");
    }

    private static async Task<string> CreateActiveM027RootAsync()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-review-command-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".review", "pending"));
        Directory.CreateDirectory(Path.Combine(root, ".review", "records"));
        Directory.CreateDirectory(Path.Combine(root, "docs", "milestones"));
        await File.WriteAllTextAsync(Path.Combine(root, "docs", "milestones", "MILESTONE-027-active.md"), "# M027");
        await File.WriteAllTextAsync(Path.Combine(root, ".review", "pending", ReviewId + ".json"), ActiveRequest);
        return root;
    }

    private static async Task WriteHistoricalM025RecordAsync(string root, string status = "approved")
    {
        var json = HistoricalRecord.Replace("STATUS", status, StringComparison.Ordinal);
        await File.WriteAllTextAsync(Path.Combine(root, ".review", "records", HistoricalId + ".json"), json);
    }

    private const string ReviewId = "review.m027.authoring-contracts-review-evidence-and-v060-migration";
    private const string HistoricalId = "review.m025.historical-approval";
    private const string ActiveRequest = """
        {
          "schema":"agentic2d.engineering.review-request.v2",
          "id":"review.m027.authoring-contracts-review-evidence-and-v060-migration",
          "owningMilestone":"M027",
          "owningMilestonePath":"docs/milestones/MILESTONE-027-active.md",
          "subject":"M027 review",
          "classes":["migration","artifact-quality"],
          "level":"blocking",
          "status":"pending",
          "reviewerRole":"reviewer",
          "requiredEvidence":["artifacts/review/M027"],
          "acceptanceCriteria":["Review usability"],
          "acceptableCompletionDecisions":["approved","rejected","waived","superseded"],
          "waiverPolicy":"No implicit waiver."
        }
        """;
    private const string HistoricalRecord = """
        {
          "schema":"agentic2d.engineering.review-record.v2",
          "id":"review.m025.historical-approval",
          "owningMilestone":"M025",
          "owningMilestonePath":"docs/milestones/M025.md",
          "subject":"Historical M025 review",
          "classes":["migration"],
          "level":"required",
          "status":"STATUS",
          "reviewerRole":"reviewer",
          "requiredEvidence":["missing-but-historical"],
          "acceptanceCriteria":["Historical acceptance"],
          "acceptableCompletionDecisions":["approved","rejected"],
          "waiverPolicy":"Historical record.",
          "decision":"approved",
          "reviewedRevision":"old-revision",
          "reviewedFingerprint":"obsolete-fingerprint",
          "conditions":[],
          "completedAt":"2025-01-01T00:00:00Z"
        }
        """;
}
