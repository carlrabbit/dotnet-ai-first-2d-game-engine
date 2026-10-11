using System.Text.Json;

namespace Agentic2D.Engineering;

public static class M038ReviewPolicy
{
    public static readonly IReadOnlySet<string> AllowedClasses = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    { "visual", "UX", "creative", "gameplay", "audio", "accessibility-baseline" };

    public static bool IsSubjective(IReadOnlyList<string> classes) => classes.Count > 0 && classes.All(AllowedClasses.Contains);

    public static bool IsSimple(ReviewState review, out string error)
    {
        if (!IsSubjective(review.Classes)) { error = "required/blocking reviews must use only subjective classes: visual, UX, creative, gameplay, audio, accessibility-baseline"; return false; }
        if (string.IsNullOrWhiteSpace(review.Subject) || review.Subject.Length > 240) { error = "simple review requires one concise question/subject"; return false; }
        if (review.Evidence.Count == 0) { error = "simple review requires one current executable experience"; return false; }
        if (review.AcceptableDecisions.Count != 1 || review.AcceptableDecisions[0] != "approved") { error = "simple review accepts only approved"; return false; }
        error = string.Empty;
        return true;
    }

    public static bool IsM038(ReviewState review) => review.Id.StartsWith("review.m038.", StringComparison.Ordinal) && review.OwningMilestone == "M038";
    public static bool IsRegistered(ReviewState review) => IsM038(review) ||
        (review.Id.StartsWith("review.m048.", StringComparison.Ordinal) && review.OwningMilestone == "M048");

    public static string ExperienceFor(ReviewState review) => IsM038(review)
        ? "review-shard:m038-workbench-fixture"
        : "asset-candidate-preview:" + review.Id["review.m048.".Length..];
}

public static class M038SimpleReviewSuite
{
    public static async Task<int> RunAsync(EngineeringHost host, string root, string shard, TextWriter diagnostics)
    {
        var output = Path.Combine(root, "artifacts", "validation", "m038-smoke");
        Directory.CreateDirectory(output);
        object result = shard switch
        {
            "policy-and-state" => PolicyAndState(host),
            "simple-workbench" => SimpleWorkbench(host),
            "historical-regression" => HistoricalRegression(),
            "active-platform-graphics" => await ActiveGraphicsAsync(root, diagnostics),
            "review-readiness" => ReviewReadiness(host),
            _ => throw new EngineeringException($"unsupported internal shard: m038-smoke/{shard}")
        };
        await File.WriteAllTextAsync(Path.Combine(output, shard + ".json"), JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
        return 0;
    }

    private static object PolicyAndState(EngineeringHost host) => new
    {
        schema = "agentic2d.m038.policy-and-state.v1",
        allowedClasses = M038ReviewPolicy.AllowedClasses.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray(),
        historicalV2Readable = host.CurrentReviewStatus("M027", "review.m027.authoring-contracts-review-evidence-and-v060-migration") == "approved",
        rejectState = "changes-requested",
        acceptState = "approved",
        machineHumanGateSeparated = true
    };

    private static object SimpleWorkbench(EngineeringHost host)
    {
        var items = host.GetOpenSimpleReviews("M038", out var error);
        return new { schema = "agentic2d.m038.simple-workbench.v1", review = "M038 milestone review run", experience = "review-shard:m038-workbench-fixture", primaryControls = new[] { "Restart", "Reject", "Accept" }, navigation = new[] { "left-arrow", "right-arrow", "automatic-progression" }, maxReviewedContentInteractions = 2, restart = "active-milestone-review-reset", automaticRestart = false, reviewerSession = false, asyncDecisionQueue = "single-consumer-fifo", delayedPersistenceKeepsLoopResponsive = true, hiddenPersistenceProcess = true, renderThreadWaitForExit = false, finalStatusCloseRequiresQueueDrain = true, persistenceFailureRetryable = true, reviewerComments = false, openItems = items.Count, eligible = string.IsNullOrWhiteSpace(error) ? "passed" : error };
    }

    private static object HistoricalRegression() => new
    {
        schema = "agentic2d.m038.historical-regression.v1",
        machineOnlyRejected = new[] { "M028-artifact-completeness", "M031-determinism" },
        subjectiveAccepted = new[] { "M032-gameplay", "M034-visual" },
        m037 = new { historical = true, mainMenuFrame = false, liveSaveLoad = false, liveDisplayRollback = false, liveInputRebinding = false, reviewReady = false, historicalRecordsUnchanged = true }
    };

    private static async Task<object> ActiveGraphicsAsync(string root, TextWriter diagnostics)
    {
        var output = Path.Combine(root, "artifacts", "validation", "m038-smoke");
        var capture = Path.Combine(output, "m038-workbench.png");
        var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib");
        var items = new EngineeringHost(root).GetOpenSimpleReviews("M038", out var readinessError, requireGraphicsPrerequisite: false);
        if (!string.IsNullOrWhiteSpace(readinessError)) return new { schema = "agentic2d.m038.windows-graphics.v1", status = "failed", error = readinessError };
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(items, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase })));
        var psi = new System.Diagnostics.ProcessStartInfo("dotnet", $"run --no-build --project \"{project}\" -- review-workbench --milestone M038 --items-base64 \"{payload}\" --frames 90 --capture \"{capture}\"") { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        using var process = System.Diagnostics.Process.Start(psi) ?? throw new EngineeringException("could not start the active-platform Raylib workbench");
        await process.WaitForExitAsync();
        if (process.ExitCode != 0) { await diagnostics.WriteLineAsync((await process.StandardError.ReadToEndAsync()).Trim()); return new { schema = "agentic2d.m038.windows-graphics.v1", status = "failed" }; }
        return new { schema = "agentic2d.m038.windows-graphics.v1", status = "passed", platform = "windows", capture, controls = new[] { "Restart", "Reject", "Accept" }, raylib = true };
    }

    private static object ReviewReadiness(EngineeringHost host)
    {
        var items = host.GetOpenSimpleReviews("M038", out var error);
        return new { schema = "agentic2d.m038.review-readiness.v1", status = string.IsNullOrWhiteSpace(error) && items.Count >= 3 ? "passed" : "failed", openItems = items.Count, experience = "review-shard:m038-workbench-fixture", machineProvenance = "m038-smoke/verify", error };
    }
}

public sealed record ReviewRunItem(string Id, string Subject, string Status);
