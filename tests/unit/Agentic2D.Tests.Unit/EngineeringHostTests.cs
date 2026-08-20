using System.Text.Json;
using Agentic2D.Engineering;

namespace Agentic2D.Tests.Unit;

public sealed class EngineeringHostTests
{
    [Test]
    public async Task PlanIsMachineReadableAndDeclaresBoundedShards()
    {
        var host = new EngineeringHost(RepositoryRoot());
        using var plan = JsonDocument.Parse(host.SerializePlan(host.GetSuite("m019-smoke")));
        await Assert.That(plan.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.engineering.validation-plan.v1");
        await Assert.That(plan.RootElement.GetProperty("requiredShards").GetArrayLength()).IsEqualTo(5);
    }

    [Test]
    public async Task AtomicReceiptWriteLeavesOnlyFinalReceipt()
    {
        var directory = Path.Combine(Path.GetTempPath(), "agentic2d-engineering-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "receipt.json");
        var receipt = new ValidationReceipt("agentic2d.engineering.validation-receipt.v1", "suite", "shard", "passed", "suite", "repository", "command", "input", "result", "command", [], [], new CompletionMetadata(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, "linux-bash"), []);
        ReceiptStore.WriteAtomic(path, receipt, new JsonSerializerOptions { WriteIndented = true });
        await Assert.That(File.Exists(path)).IsTrue();
        await Assert.That(Directory.EnumerateFiles(directory, "*.tmp").Any()).IsFalse();
        await Assert.That(ReceiptStore.TryRead(path, new JsonSerializerOptions(), out var read, out _)).IsTrue();
        await Assert.That(read!.Status).IsEqualTo("passed");
    }

    [Test]
    public async Task MilestoneScopedListShowsCanonicalIdAndEphemeralAlias()
    {
        var host = new EngineeringHost(RepositoryRoot());
        var output = new StringWriter();

        await Assert.That(host.ListReviews("M027", output)).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("1\thistorical\tM027\treview.m027.authoring-contracts-review-evidence-and-v060-migration\t");
    }

    [Test]
    public async Task UnfilteredListShowsReviewsForEveryMilestoneWithoutAliases()
    {
        var host = new EngineeringHost(RepositoryRoot());
        var output = new StringWriter();

        await Assert.That(host.ListReviews(null, output)).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("1\thistorical\tM022\tmigration-guide-v050");
        await Assert.That(output.ToString()).Contains("historical\tM027\treview.m027.authoring-contracts-review-evidence-and-v060-migration\t");
    }

    [Test]
    public async Task ReviewListAcceptsAnOmittedMilestoneSelector()
    {
        var output = new StringWriter();
        var code = await EngineeringCli.RunAsync(["review", "list"], RepositoryRoot(), output, new StringWriter());

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(output.ToString()).Contains("M027\treview.m027.authoring-contracts-review-evidence-and-v060-migration");
    }

    [Test]
    public async Task ReviewCheckStillRequiresAMilestoneSelector()
    {
        var diagnostics = new StringWriter();
        var code = await EngineeringCli.RunAsync(["review", "check"], RepositoryRoot(), new StringWriter(), diagnostics);

        await Assert.That(code).IsEqualTo(1);
        await Assert.That(diagnostics.ToString()).Contains("review commands require --milestone <id>");
    }

    [Test]
    public async Task RecordAcceptsNumericAliasButWritesCanonicalId()
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-engineering-review-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, ".review", "pending"));
        Directory.CreateDirectory(Path.Combine(root, "docs", "milestones"));
        await File.WriteAllTextAsync(Path.Combine(root, "docs", "milestones", "M027.md"), "# M027");
        var request = """
            {
              "schema":"agentic2d.engineering.review-request.v2",
              "id":"review.m027.alias-test",
              "owningMilestone":"M027",
              "owningMilestonePath":"docs/milestones/M027.md",
              "subject":"Alias test",
              "classes":["migration"],
              "level":"required",
              "status":"pending",
              "reviewerRole":"reviewer",
              "requiredEvidence":[],
              "acceptanceCriteria":[],
              "acceptableCompletionDecisions":["approved"],
              "waiverPolicy":"none"
            }
            """;
        await File.WriteAllTextAsync(Path.Combine(root, ".review", "pending", "review.m027.alias-test.json"), request);

        await EngineeringCli.RunAsync(["review", "list", "--milestone", "M027"], root, new StringWriter(), new StringWriter());
        var code = await EngineeringCli.RunAsync(["review", "record", "1", "approved", "--notes", "alias test"], root, new StringWriter(), new StringWriter());

        await Assert.That(code).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(root, ".review", "records", "review.m027.alias-test.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(root, ".review", "records", "1.json"))).IsFalse();
    }

    private static string RepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "dotnet-ai-first-2d-game-engine.slnx")) && Directory.Exists(Path.Combine(directory, ".review"))) return directory;
            directory = Directory.GetParent(directory)?.FullName;
        }

        throw new InvalidOperationException("repository root was not found");
    }
}
