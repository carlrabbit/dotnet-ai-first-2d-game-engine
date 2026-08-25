using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic2D.Engineering;

public static class EngineeringCli
{
    public static async Task<int> RunAsync(string[] args, string root, TextWriter stdout, TextWriter stderr)
    {
        var host = new EngineeringHost(root);
        try
        {
            if (args.Length == 0)
            {
                return Usage(stderr);
            }

            return args[0] switch
            {
                "suite" => await RunSuiteAsync(host, args[1..], stdout, stderr),
                "review" => await RunReviewAsync(host, args[1..], stdout, stderr),
                "performance" => await PerformanceCli.RunAsync(args[1..], root, stdout, stderr),
                _ => Usage(stderr)
            };
        }
        catch (EngineeringException exception)
        {
            await stderr.WriteLineAsync($"error: {exception.Message}");
            return 1;
        }
    }

    private static async Task<int> RunSuiteAsync(EngineeringHost host, string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length < 1)
        {
            return Usage(stderr);
        }

        var suite = host.GetSuite(args[0]);
        var operation = args.Skip(1).ToArray();
        if (operation.Length == 0)
        {
            return await host.RunAllAsync(suite, stdout, stderr);
        }

        if (operation.SequenceEqual(["--list"]))
        {
            foreach (var shard in suite.Shards)
            {
                await stdout.WriteLineAsync($"{shard.Id}\t{shard.Command}\t{shard.Description}");
            }

            return 0;
        }

        if (operation.SequenceEqual(["--plan-json"]))
        {
            await stdout.WriteLineAsync(host.SerializePlan(suite));
            return 0;
        }

        if (operation.Length == 2 && operation[0] == "--shard")
        {
            return await host.RunShardAsync(suite, operation[1], stdout, stderr);
        }

        if (operation.SequenceEqual(["--verify"]))
        {
            return host.Verify(suite, stderr) ? 0 : 1;
        }

        if (suite.Id == "m036-smoke" && operation.SequenceEqual(["--emit-blocked"]))
        {
            M036EngineeringSuite.EmitBlockedEvidence(host.Root);
            await stdout.WriteLineAsync("m036-smoke: blocked external-platform evidence written");
            return 0;
        }

        return Usage(stderr);
    }

    private static async Task<int> RunReviewAsync(EngineeringHost host, string[] args, TextWriter stdout, TextWriter stderr)
    {
        if (args.Length == 0)
        {
            return Usage(stderr);
        }

        return args[0] switch
        {
            "list" => host.ListReviewsFromArguments(args[1..], stdout),
            "show" when args.Length == 2 => host.ShowReview(args[1], stdout),
            "run" when args.Length == 2 => await host.RunSimpleReviewAsync(args[1], stdout, stderr),
            "run" when args.Length == 3 && args[1] == "--milestone" => await host.RunMilestoneReviewAsync(args[2], stdout, stderr),
            "reset" when args.Length == 3 && args[1] == "--milestone" => await host.ResetReviewsAsync(args[2], stdout),
            "check" => host.CheckReviews(ReviewMilestone(args[1..]), stderr) ? 0 : 1,
            "migration-report" => await host.WriteReviewMigrationReportAsync(ReviewMilestone(args[1..]), stdout),
            "request" => await host.CreateReviewRequestAsync(args[1..], stdout),
            "record" => await host.RecordReviewAsync(args[1..], stdout),
            "reopen" => await host.ReopenReviewAsync(args[1..], stdout),
            _ => Usage(stderr)
        };
    }

    private static int Usage(TextWriter stderr)
    {
        stderr.WriteLine("usage: engineering suite <id> [--list|--plan-json|--shard <id>|--verify] | engineering review list [--milestone <id>] [--state <active|historical>] [--status <status>] | engineering review show <review-id-or-alias> | engineering review run <review-id-or-alias> | engineering review run --milestone <id> | engineering review reset --milestone <id> | engineering review request --milestone <id> ... | engineering review record <review-id-or-alias> <decision> ... | engineering review reopen <review-id-or-alias> --reason <reason> [--correct-record] | engineering review check --milestone <id> | engineering performance <smoke|capture|compare|report>");
        return 2;
    }

    private static string ReviewMilestone(string[] args) => args.Length == 2 && args[0] == "--milestone" && !string.IsNullOrWhiteSpace(args[1])
        ? args[1]
        : throw new EngineeringException("review commands require --milestone <id>");

}

public sealed class EngineeringHost
{
    private const string PlanSchema = "agentic2d.engineering.validation-plan.v1";
    private const string ReceiptSchema = "agentic2d.engineering.validation-receipt.v1";
    private const string ReviewRequestSchema = "agentic2d.engineering.review-request.v2";
    private const string ReviewRecordSchema = "agentic2d.engineering.review-record.v2";
    private const string AliasMapPath = "artifacts/review/session/aliases.json";
    private readonly string root;
    private readonly IReadOnlyDictionary<string, ValidationSuite> suites;
    private readonly JsonSerializerOptions json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

    public EngineeringHost(string root)
    {
        this.root = Path.GetFullPath(root);
        suites = BuildSuites().ToDictionary(suite => suite.Id, StringComparer.Ordinal);
    }

    public string CurrentReviewStatus(string milestone, string reviewId) => ReadReviews()
        .Where(review => review.OwningMilestone == milestone && review.Id == reviewId)
        .Select(review => review.Status)
        .FirstOrDefault() ?? "missing";

    public bool TryGetSimpleReview(string id, out ReviewState? review, out string error)
        => TryGetSimpleReview(id, out review, out error, requireGraphicsPrerequisite: true);

    public bool TryGetSimpleReview(string id, out ReviewState? review, out string error, bool requireGraphicsPrerequisite)
    {
        review = ReadReviews().FirstOrDefault(candidate => candidate.Id == id);
        if (review is null) { error = $"review '{id}' is not active"; return false; }
        if (!IsMilestoneActive(review.OwningMilestone)) { error = "review belongs to a completed milestone"; return false; }
        if (!M038ReviewPolicy.IsSimple(review, out error)) return false;
        if (!M038ReviewPolicy.IsM038(review)) { error = "review is not registered to a simple current experience"; return false; }
        if (requireGraphicsPrerequisite)
        {
            var graphicsPath = Absolute("artifacts/validation/m038-smoke/active-platform-graphics.json");
            if (!File.Exists(graphicsPath) || !File.ReadAllText(graphicsPath).Contains("\"status\": \"passed\"", StringComparison.Ordinal)) { error = "current active-platform graphics prerequisite is missing or failed"; return false; }
        }
        return true;
    }

    public IReadOnlyList<ReviewRunItem> GetOpenSimpleReviews(string milestone, out string error)
        => GetOpenSimpleReviews(milestone, out error, requireGraphicsPrerequisite: true);

    public IReadOnlyList<ReviewRunItem> GetOpenSimpleReviews(string milestone, out string error, bool requireGraphicsPrerequisite)
    {
        var reviews = ReadReviews().Where(candidate => candidate.OwningMilestone == milestone && (candidate.Level is "required" or "blocking") && (candidate.Status is "pending" or "changes-requested")).OrderBy(candidate => candidate.Id, StringComparer.Ordinal).ToArray();
        var items = new List<ReviewRunItem>();
        foreach (var review in reviews)
        {
            if (!TryGetSimpleReview(review.Id, out var current, out error, requireGraphicsPrerequisite)) return [];
            items.Add(new ReviewRunItem(current!.Id, current.Subject, current.Status));
        }

        error = string.Empty;
        return items;
    }

    public async Task<int> RunSimpleReviewAsync(string idOrAlias, TextWriter stdout, TextWriter stderr)
    {
        var id = ResolveReviewTarget(idOrAlias);
        if (!TryGetSimpleReview(id, out var review, out var error)) throw new EngineeringException($"review-run: {error}");
        var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib");
        var arguments = $"run --no-build --project \"{project}\" -- review-workbench --review-id \"{review!.Id}\" --question \"{review.Subject.Replace("\"", "'")}\"";
        var exit = await ProcessRunner.RunAsync(root, "dotnet " + arguments, stdout, stderr);
        return exit;
    }

    public async Task<int> RunMilestoneReviewAsync(string milestone, TextWriter stdout, TextWriter stderr)
    {
        if (!IsMilestoneActive(milestone)) throw new EngineeringException($"review-run: milestone '{milestone}' is not active");
        var items = GetOpenSimpleReviews(milestone, out var error);
        if (!string.IsNullOrWhiteSpace(error)) throw new EngineeringException($"review-run: {error}");
        if (items.Count == 0) throw new EngineeringException($"review-run: milestone '{milestone}' has no open simple blocking reviews");
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(items, json)));
        var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib");
        var arguments = $"run --no-build --project \"{project}\" -- review-workbench --milestone \"{milestone}\" --items-base64 \"{payload}\"";
        return await ProcessRunner.RunAsync(root, "dotnet " + arguments, stdout, stderr);
    }

    public async Task<int> ResetReviewsAsync(string milestone, TextWriter stdout)
    {
        if (!IsMilestoneActive(milestone)) throw new EngineeringException($"review reset: milestone '{milestone}' is not active");
        var reviews = ReadReviews().Where(candidate => candidate.OwningMilestone == milestone && (candidate.Level is "required" or "blocking") && M038ReviewPolicy.IsM038(candidate)).ToArray();
        foreach (var review in reviews)
        {
            var reset = review with { Schema = ReviewRequestSchema, Status = "pending", Decision = string.Empty, Conditions = [], ReviewedRevision = string.Empty, ReviewedFingerprint = string.Empty, CompletedAt = null, Path = Path.Combine(".review", "pending", review.Id + ".json") };
            WriteReview(reset, reset.Path);
        }

        await stdout.WriteLineAsync($"review reset: {reviews.Length} active {milestone} review items reopened");
        return 0;
    }

    public ValidationSuite GetSuite(string id) => suites.TryGetValue(id, out var suite)
        ? suite
        : throw new EngineeringException($"unknown validation suite: {id}");

    public string SerializePlan(ValidationSuite suite)
    {
        EnsurePlatformState(suite);
        var repository = Fingerprints.Repository(root);
        var suiteFingerprint = Fingerprints.Suite(suite, root);
        var plan = new ValidationPlan(
            PlanSchema,
            suite.Id,
            suite.ExecutionMode,
            suiteFingerprint,
            repository,
            suite.Shards.Select(shard => new PlanShard(
                shard.Id,
                shard.Description,
                shard.Command,
                ReceiptPath(suite, shard),
                shard.DependsOn,
                shard.Evidence)).ToArray(),
            suite.Id is "m037-smoke" or "m039-smoke" ? $"pwsh ./eng/suite.ps1 {suite.Id} --verify" : $"./eng/{suite.Id}.sh --verify",
            suite.Shards.SelectMany(shard => shard.Evidence).Distinct(StringComparer.Ordinal).ToArray());
        var serialized = JsonSerializer.Serialize(plan, json);
        if (suite.Id is "m033-smoke" or "m034-smoke" or "m035-smoke" or "m039-smoke")
        {
            var planPath = Absolute(Path.Combine("artifacts", "validation", suite.Id, "plan.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(planPath)!);
            File.WriteAllText(planPath, serialized);
        }
        return serialized;
    }

    public async Task<int> RunAllAsync(ValidationSuite suite, TextWriter stdout, TextWriter stderr)
    {
        foreach (var shard in suite.Shards)
        {
            var result = await RunShardAsync(suite, shard.Id, stdout, stderr);
            if (result != 0)
            {
                return result;
            }
        }

        return Verify(suite, stderr) ? 0 : 1;
    }

    public async Task<int> RunShardAsync(ValidationSuite suite, string shardId, TextWriter stdout, TextWriter stderr)
    {
        EnsurePlatformState(suite);
        var shard = suite.Shards.SingleOrDefault(candidate => candidate.Id == shardId)
            ?? throw new EngineeringException($"unknown shard '{shardId}' for suite '{suite.Id}'");
        var receiptPath = Absolute(ReceiptPath(suite, shard));
        ReceiptStore.Invalidate(receiptPath);
        foreach (var dependency in shard.DependsOn)
        {
            if (!HasCurrentReceipt(suite, dependency, stderr))
            {
                await stderr.WriteLineAsync($"error: shard '{shardId}' requires a current receipt for '{dependency}'");
                return 1;
            }
        }

        var repositoryFingerprint = Fingerprints.Repository(root);
        var suiteFingerprint = Fingerprints.Suite(suite, root);
        var commandFingerprint = Fingerprints.Command(shard);
        var inputFingerprint = Fingerprints.Input(root, shard);
        var started = DateTimeOffset.UtcNow;
        await stdout.WriteLineAsync($"{suite.Id}/{shard.Id}: running {shard.Command}");
        var exitCode = shard.IsInternal
            ? await RunInternalShardAsync(suite, shard, stderr)
            : await ProcessRunner.RunAsync(root, shard.Command, stdout, stderr);
        if (exitCode != 0)
        {
            await stderr.WriteLineAsync($"error: {suite.Id}/{shard.Id} failed with exit code {exitCode}; no passing receipt was written");
            return exitCode;
        }

        var artifacts = ValidateEvidence(shard);
        var receipt = new ValidationReceipt(
            ReceiptSchema,
            suite.Id,
            shard.Id,
            "passed",
            suiteFingerprint,
            repositoryFingerprint,
            commandFingerprint,
            inputFingerprint,
            Fingerprints.Result(artifacts, root),
            shard.Command,
            shard.Evidence,
            artifacts,
            new CompletionMetadata(started, DateTimeOffset.UtcNow, EngineeringEnvironment.Current.Launcher, EngineeringEnvironment.Current),
            []);
        ReceiptStore.WriteAtomic(receiptPath, receipt, json);
        await stdout.WriteLineAsync($"{suite.Id}/{shard.Id}: passed; receipt {ReceiptPath(suite, shard)}");
        return 0;
    }

    public bool Verify(ValidationSuite suite, TextWriter diagnostics)
    {
        EnsurePlatformState(suite);
        var expectedSuite = Fingerprints.Suite(suite, root);
        var expectedRepository = Fingerprints.Repository(root);
        var success = true;
        foreach (var shard in suite.Shards)
        {
            var receiptPath = Absolute(ReceiptPath(suite, shard));
            if (!ReceiptStore.TryRead(receiptPath, json, out var receipt, out var error))
            {
                diagnostics.WriteLine($"error: {suite.Id}/{shard.Id}: {error}");
                success = false;
                continue;
            }

            var mismatches = new List<string>();
            if (receipt!.Schema != ReceiptSchema) mismatches.Add("schema");
            if (receipt.SuiteId != suite.Id) mismatches.Add("suite identity");
            if (receipt.ShardId != shard.Id) mismatches.Add("shard identity");
            if (receipt.Status != "passed") mismatches.Add("status");
            if (receipt.SuiteFingerprint != expectedSuite) mismatches.Add("suite fingerprint");
            if (receipt.RepositoryFingerprint != expectedRepository) mismatches.Add("repository fingerprint");
            if (receipt.CommandFingerprint != Fingerprints.Command(shard)) mismatches.Add("command fingerprint");
            if (receipt.InputFingerprint != Fingerprints.Input(root, shard)) mismatches.Add("input fingerprint");
            var artifacts = ValidateEvidence(shard, throwOnMissing: false);
            if (artifacts.Count != shard.Evidence.Count || receipt.ResultFingerprint != Fingerprints.Result(artifacts, root)) mismatches.Add("result/evidence fingerprint");
            if (mismatches.Count > 0)
            {
                diagnostics.WriteLine($"error: {suite.Id}/{shard.Id}: invalid receipt ({string.Join(", ", mismatches)})");
                success = false;
            }
        }

        if (success)
        {
            diagnostics.WriteLine($"{suite.Id}: verification passed ({suite.Shards.Count} current receipts)");
        }

        if (suite.Id == "m036-smoke")
        {
            var audit = Absolute("artifacts/engineering/M036/m036-completion-audit.json");
            var comparison = Absolute("artifacts/engineering/M036/platform-comparison.json");
            if (!File.Exists(audit) || !File.ReadAllText(audit).Contains("\"terminalOutcome\": \"COMPLETE\"", StringComparison.Ordinal))
            {
                diagnostics.WriteLine("error: m036-smoke: completion audit is not COMPLETE");
                success = false;
            }
            if (!File.Exists(comparison) || !File.ReadAllText(comparison).Contains("\"status\": \"passed\"", StringComparison.Ordinal))
            {
                diagnostics.WriteLine("error: m036-smoke: platform semantic comparison is not passed");
                success = false;
            }
            foreach (var platform in new[] { "linux", "windows" })
            {
                var report = Absolute($"artifacts/engineering/M036/platform/{platform}/platform-verification.json");
                var graphics = Absolute($"artifacts/engineering/M036/platform/{platform}/graphics-development.json");
                if (!File.Exists(report) || !File.ReadAllText(report).Contains("\"status\": \"passed\"", StringComparison.Ordinal) || !File.Exists(graphics) || !File.ReadAllText(graphics).Contains("\"status\": \"passed\"", StringComparison.Ordinal))
                {
                    diagnostics.WriteLine($"error: m036-smoke: {platform} platform and graphics proofs are not passed");
                    success = false;
                }
            }
        }

        if (suite.Id == "m037-smoke")
        {
            var state = PlatformVerificationState.Load(root);
            foreach (var platform in state.SupportedDevelopmentPlatforms)
            {
                var structural = Absolute($"artifacts/application/M037/platform/{platform}/structural-report.json");
                var graphical = Absolute($"artifacts/application/M037/platform/{platform}/graphical-report.json");
                var expected = state.IsActive(platform) ? "passed" : "deferred-inactive-platform";
                foreach (var report in new[] { structural, graphical })
                {
                    if (!File.Exists(report) || !File.ReadAllText(report).Contains($"\"status\": \"{expected}\"", StringComparison.Ordinal))
                    {
                        diagnostics.WriteLine($"error: m037-smoke: {platform} report is not {expected}");
                        success = false;
                    }
                }
            }
            var audit = Absolute("artifacts/application/M037/m037-completion-audit.json");
            var reviewStatus = CurrentReviewStatus("M037", "review.m037.product-shell-ui-saves-settings-and-input");
            var expectedOutcome = reviewStatus == "approved" ? "COMPLETE" : "AWAITING HUMAN REVIEW";
            if (!File.Exists(audit) || !File.ReadAllText(audit).Contains($"\"terminalOutcome\": \"{expectedOutcome}\"", StringComparison.Ordinal))
            {
                diagnostics.WriteLine($"error: m037-smoke: completion audit is not {expectedOutcome}");
                success = false;
            }
            if (reviewStatus != "approved")
            {
                diagnostics.WriteLine("error: m037-smoke: blocking M037 human review is not approved");
                success = false;
            }
        }

        if (suite.Id == "m039-smoke" && success)
        {
            foreach (var shard in suite.Shards)
            {
                var evidence = Absolute(shard.Evidence.Single());
                using var document = JsonDocument.Parse(File.ReadAllText(evidence));
                if (document.RootElement.GetProperty("status").GetString() != "passed") { diagnostics.WriteLine($"error: m039-smoke/{shard.Id}: observation status is not passed"); success = false; }
                if (shard.Id == "fresh-process-equivalence" && (!document.RootElement.GetProperty("evidence").GetProperty("separateOsProcesses").GetBoolean() || document.RootElement.GetProperty("evidence").GetProperty("launches").GetArrayLength() != 2)) { diagnostics.WriteLine("error: m039-smoke/fresh-process-equivalence: distinct process provenance missing"); success = false; }
            }
            var path = Absolute("artifacts/validation/m039-smoke/verify.json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new { schema = "agentic2d.m039.verification.v1", suite = "m039-smoke", status = success ? "passed" : "failed", currentReceipts = suite.Shards.Count, freshProcess = success }, json));
        }

        if (suite.Id == "m038-smoke" && success)
        {
            var readiness = Absolute("artifacts/validation/m038-smoke/review-readiness.json");
            if (!File.Exists(readiness) || !File.ReadAllText(readiness).Contains("\"status\": \"passed\"", StringComparison.Ordinal)) { diagnostics.WriteLine("error: m038-smoke: review readiness is not passed"); success = false; }
        }

        if (suite.Id is "m031-smoke" or "m032-smoke" or "m033-smoke" or "m034-smoke" or "m035-smoke")
        {
            var path = Absolute(Path.Combine("artifacts", "validation", suite.Id, "verify.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var schema = suite.Id switch
            {
                "m031-smoke" => "agentic2d.simulation-foundation-verification.v1",
                "m032-smoke" => "agentic2d.autonomous-detailed-region-verification.v1",
                "m033-smoke" => "agentic2d.m033.verification.v1",
                "m034-smoke" => "agentic2d.m034.verification.v1",
                _ => "agentic2d.m035.verification.v1"
            };
            if (suite.Id == "m033-smoke")
            {
                var graphical = Absolute("artifacts/simulation/M033/graphical-evidence/environment.json");
                if (!File.Exists(graphical) || !File.ReadAllText(graphical).Contains("\"status\": \"passed\"", StringComparison.Ordinal))
                {
                    diagnostics.WriteLine("error: m033-smoke/graphical-switch-proof: graphics-capable proof is not passed");
                    success = false;
                }
            }
            if (suite.Id == "m034-smoke")
            {
                var graphical = Absolute("artifacts/simulation/M034/graphical-evidence/environment.json");
                if (!File.Exists(graphical) || !File.ReadAllText(graphical).Contains("\"status\": \"passed\"", StringComparison.Ordinal))
                {
                    diagnostics.WriteLine("error: m034-smoke/graphical-play-proof: graphics-capable proof is not passed");
                    success = false;
                }
            }
            if (suite.Id == "m035-smoke")
            {
                var graphical = Absolute("artifacts/readiness/M035/graphical-soak-report.json");
                if (!File.Exists(graphical) || !File.ReadAllText(graphical).Contains("\"status\": \"passed\"", StringComparison.Ordinal))
                {
                    diagnostics.WriteLine("error: m035-smoke/graphical-4-hour-soak: graphics-capable four-hour proof is not passed");
                    success = false;
                }
                var session = Absolute("artifacts/readiness/M035/graphical-soak/session.json");
                if (!File.Exists(session) || !File.ReadAllText(session).Contains("\"status\": \"passed\"", StringComparison.Ordinal) || !File.ReadAllText(session).Contains("\"earlyTermination\": false", StringComparison.Ordinal))
                {
                    diagnostics.WriteLine("error: m035-smoke/graphical-4-hour-soak: validated Raylib session evidence is absent, incomplete, or early-terminated");
                    success = false;
                }
                var readiness = Absolute("artifacts/readiness/M035/readiness-report.json");
                if (!File.Exists(readiness) || (!File.ReadAllText(readiness).Contains("\"decision\": \"ready\"", StringComparison.Ordinal) && !File.ReadAllText(readiness).Contains("\"decision\": \"ready-with-declared-limitations\"", StringComparison.Ordinal)))
                {
                    diagnostics.WriteLine("error: m035-smoke/readiness-report: readiness decision is not an allowed completion decision");
                    success = false;
                }
                foreach (var campaign in new[] { "population-entity", "pathfinding-work", "abstract-queue", "fidelity-transition", "persistence-cycle", "infrastructure-shortage", "headless-365-day" })
                {
                    var campaignVerify = Absolute(Path.Combine("artifacts", "readiness", "M035", "campaigns", campaign, "verify.json"));
                    if (!File.Exists(campaignVerify) || !File.ReadAllText(campaignVerify).Contains("\"status\": \"passed\"", StringComparison.Ordinal))
                    {
                        diagnostics.WriteLine($"error: m035-smoke campaign '{campaign}' is not verified");
                        success = false;
                    }
                }
                if (!CheckReviews("M035", diagnostics)) success = false;
            }
            File.WriteAllText(path, JsonSerializer.Serialize(new { schema, suite = suite.Id, status = success ? "passed" : "failed", receiptCount = suite.Shards.Count }, json));
        }

        return success;
    }

    public int ListReviewsFromArguments(string[] args, TextWriter stdout) => ListReviews(ParseListOptions(args), stdout);

    public int ListReviews(string? milestone, TextWriter stdout) => ListReviews(new ReviewListOptions(milestone, null, null), stdout);

    private int ListReviews(ReviewListOptions options, TextWriter stdout)
    {
        var reviews = FilterReviews(options).ToArray();
        WriteAliasMap(options, reviews);
        for (var index = 0; index < reviews.Length; index++)
        {
            var review = reviews[index];
            stdout.WriteLine($"{index + 1}\t{ReviewStateKind(review)}\t{review.OwningMilestone}\t{review.Id}\t{review.Status}\t{review.Level}\t{review.Subject}\t{review.Path}");
        }

        return 0;
    }

    public int ShowReview(string idOrAlias, TextWriter stdout)
    {
        var id = ResolveReviewTarget(idOrAlias);
        var entries = ReadReviewFiles().Where(review => review.Id == id).ToArray();
        if (entries.Length == 0) throw new EngineeringException($"unknown review '{id}'");

        var review = SelectCurrentReview(entries);
        var request = entries.FirstOrDefault(entry => entry.Path.StartsWith(".review/pending/", StringComparison.Ordinal));
        var records = entries.Where(entry => entry.Path.StartsWith(".review/records/", StringComparison.Ordinal)).OrderBy(entry => entry.CompletedAt).ToArray();
        stdout.WriteLine($"Canonical review ID: {review.Id}");
        stdout.WriteLine($"Owning milestone: {review.OwningMilestone}");
        stdout.WriteLine($"Owning milestone path: {review.OwningMilestonePath}");
        stdout.WriteLine($"State: {ReviewStateKind(review)}");
        stdout.WriteLine($"Status: {review.Status}");
        stdout.WriteLine($"Subject: {review.Subject}");
        stdout.WriteLine($"Classes: {string.Join(", ", review.Classes)}");
        stdout.WriteLine($"Applicability: {review.Level}");
        stdout.WriteLine($"Reviewer role: {review.ReviewerRole}");
        stdout.WriteLine($"Waiver policy: {review.WaiverPolicy}");
        stdout.WriteLine($"Current decision: {DisplayValue(review.Decision)}");
        stdout.WriteLine($"Required evidence: {DisplayList(review.Evidence)}");
        stdout.WriteLine($"Acceptance criteria: {DisplayList(review.AcceptanceCriteria)}");
        stdout.WriteLine($"Decision history: {DisplayHistory(review.DecisionHistory)}");
        stdout.WriteLine($"Provenance revision: {DisplayValue(review.ReviewedRevision)}");
        stdout.WriteLine($"Provenance fingerprint: {DisplayValue(review.ReviewedFingerprint)}");
        stdout.WriteLine($"Request path: {request?.Path ?? "none"}");
        stdout.WriteLine($"Record paths: {DisplayList(records.Select(record => record.Path))}");
        if (!string.IsNullOrWhiteSpace(review.CorrectsReviewId)) stdout.WriteLine($"Corrects review: {review.CorrectsReviewId}");
        return 0;
    }

    public bool CheckReviews(string milestone, TextWriter diagnostics)
    {
        var reviews = ReadReviews().Where(review => review.OwningMilestone == milestone).ToArray();
        var success = true;
        foreach (var review in reviews.Where(review => review.Level is "required" or "blocking"))
        {
            if (review.Status != "approved")
            {
                diagnostics.WriteLine($"error: required review '{review.Id}' is {review.Status}");
                success = false;
                continue;
            }

            if (review.Evidence.Count == 0 || review.Evidence.Any(path => !File.Exists(Absolute(path)) && !Directory.Exists(Absolute(path))))
            {
                diagnostics.WriteLine($"error: required review '{review.Id}' has missing evidence");
                success = false;
            }
        }

        if (!reviews.Any(review => review.Level is "required" or "blocking"))
        {
            diagnostics.WriteLine($"error: no required or blocking review exists for milestone '{milestone}'");
            return false;
        }

        if (success)
        {
            diagnostics.WriteLine($"review-check: passed for {milestone}");
        }

        return success;
    }

    public async Task<int> CreateReviewRequestAsync(string[] args, TextWriter stdout)
    {
        var options = ParseOptions(args);
        var id = Required(options, "--id");
        var milestone = Required(options, "--milestone");
        ValidateCanonicalReviewId(id);
        if (ReadReviewFiles().Any(review => review.Id == id)) throw new EngineeringException($"review ID already exists: {id}");
        var review = new ReviewState(
            ReviewRequestSchema,
            id,
            milestone,
            options.GetValueOrDefault("--milestone-path", $"docs/milestones/{milestone}.md"),
            Required(options, "--subject"),
            Split(Required(options, "--class")),
            Required(options, "--level"),
            options.GetValueOrDefault("--reviewer", "human reviewer"),
            "pending",
            Split(options.GetValueOrDefault("--evidence", string.Empty)),
            Split(options.GetValueOrDefault("--criteria", string.Empty)),
            Split(options.GetValueOrDefault("--acceptable", "approved")),
            options.GetValueOrDefault("--waiver-policy", "No implicit waiver."),
            string.Empty,
            string.Empty,
            string.Empty,
            [],
            null,
            [],
            string.Empty,
            Path.Combine(".review", "pending", id + ".json"));
        WriteReview(review, Path.Combine(".review", "pending", id + ".json"));
        await stdout.WriteLineAsync($"review request created: {review.Path}");
        return 0;
    }

    public async Task<int> RecordReviewAsync(string[] args, TextWriter stdout)
    {
        if (args.Length < 2) throw new EngineeringException("review record requires <review-id-or-alias> <decision>");
        var id = ResolveReviewTarget(args[0]);
        var decision = args[1];
        var options = ParseOptions(args[2..]);
        ValidateDecision(decision);
        var pendingPath = Path.Combine(".review", "pending", id + ".json");
        if (!TryReadReview(Absolute(pendingPath), out var pending, out var error))
        {
            throw new EngineeringException(string.IsNullOrWhiteSpace(error) ? $"review '{id}' is not an active request" : error);
        }

        if (!IsMilestoneActive(pending!.OwningMilestone))
        {
            throw new EngineeringException($"review '{id}' belongs to completed milestone {pending.OwningMilestone}; create a future milestone review or use explicit record correction");
        }

        if (IsFinalDecision(decision) && !pending.AcceptableDecisions.Contains(decision, StringComparer.Ordinal))
        {
            throw new EngineeringException($"review decision '{decision}' is not an acceptable completion decision for '{id}'");
        }

        var evidence = options.TryGetValue("--evidence", out var suppliedEvidence) ? Split(suppliedEvidence) : pending!.Evidence;
        var reviewer = options.GetValueOrDefault("--reviewer", pending.ReviewerRole);
        var conditions = Split(options.GetValueOrDefault("--conditions", string.Empty));
        var history = pending.DecisionHistory.Append(new ReviewDecision(
            decision,
            reviewer,
            options.GetValueOrDefault("--notes", string.Empty),
            evidence,
            RepositoryRevision(),
            Fingerprints.Review(root),
            DateTimeOffset.UtcNow,
            false)).ToArray();
        if (!IsFinalDecision(decision))
        {
            WriteReview(pending with
            {
                Status = decision,
                ReviewerRole = reviewer,
                Evidence = evidence,
                Decision = decision,
                Conditions = conditions,
                DecisionHistory = history
            }, pendingPath);
            await stdout.WriteLineAsync($"review request updated: {pendingPath}");
            return 0;
        }

        var recordPath = NextRecordPath(id);
        var record = pending with
        {
            Schema = ReviewRecordSchema,
            Status = decision,
            ReviewerRole = reviewer,
            Evidence = evidence,
            Decision = decision,
            Conditions = conditions,
            ReviewedRevision = RepositoryRevision(),
            ReviewedFingerprint = Fingerprints.Review(root),
            CompletedAt = DateTimeOffset.UtcNow,
            DecisionHistory = history,
            Path = recordPath
        };
        WriteReview(record, recordPath);
        File.Delete(Absolute(pendingPath));
        await stdout.WriteLineAsync($"review record written: {recordPath}");
        return 0;
    }

    public async Task<int> ReopenReviewAsync(string[] args, TextWriter stdout)
    {
        if (args.Length < 1) throw new EngineeringException("review reopen requires <review-id-or-alias> --reason <reason>");
        var id = ResolveReviewTarget(args[0]);
        var (correctRecord, options) = ParseReopenOptions(args[1..]);
        var reason = Required(options, "--reason");
        var entries = ReadReviewFiles().Where(review => review.Id == id).ToArray();
        if (entries.Length == 0) throw new EngineeringException($"unknown review '{id}'");

        var current = SelectCurrentReview(entries);
        var reviewer = options.GetValueOrDefault("--reviewer", current.ReviewerRole);
        var history = current.DecisionHistory.Append(new ReviewDecision("reopened", reviewer, reason, current.Evidence, RepositoryRevision(), Fingerprints.Review(root), DateTimeOffset.UtcNow, correctRecord)).ToArray();
        var pending = entries.FirstOrDefault(entry => entry.Path.StartsWith(".review/pending/", StringComparison.Ordinal));
        if (IsMilestoneActive(current.OwningMilestone))
        {
            var reopened = current with
            {
                Schema = ReviewRequestSchema,
                Status = "pending",
                Decision = string.Empty,
                CompletedAt = null,
                DecisionHistory = history,
                Path = Path.Combine(".review", "pending", id + ".json")
            };
            WriteReview(reopened, reopened.Path);
            await stdout.WriteLineAsync($"review reopened: {reopened.Path}");
            return 0;
        }

        if (!correctRecord)
        {
            throw new EngineeringException($"review '{id}' is historical. Later repository changes do not reopen it; create a review for a future milestone or use --correct-record --reason <reason> to correct an erroneous record");
        }

        var correctionId = id + ".correction." + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(System.Globalization.CultureInfo.InvariantCulture);
        var correction = current with
        {
            Schema = ReviewRequestSchema,
            Id = correctionId,
            Status = "pending",
            Decision = string.Empty,
            ReviewedRevision = string.Empty,
            ReviewedFingerprint = string.Empty,
            CompletedAt = null,
            DecisionHistory = history,
            CorrectsReviewId = id,
            Path = Path.Combine(".review", "pending", correctionId + ".json")
        };
        WriteReview(correction, correction.Path);
        await stdout.WriteLineAsync($"review correction opened: {correction.Path}");
        return 0;
    }

    public async Task<int> WriteReviewMigrationReportAsync(string milestone, TextWriter stdout)
    {
        var entries = new List<object>();
        foreach (var directory in new[] { ".review/pending", ".review/records", ".review/closed" })
        {
            var absolute = Absolute(directory);
            if (!Directory.Exists(absolute)) continue;
            foreach (var path in Directory.EnumerateFiles(absolute, "*", SearchOption.TopDirectoryOnly).OrderBy(path => path, StringComparer.Ordinal))
            {
                var relative = Path.GetRelativePath(root, path).Replace('\\', '/');
                if (path.EndsWith(".json", StringComparison.Ordinal) && TryReadReview(path, out var review, out _))
                {
                    var classification = directory == ".review/records" ? "historical-completed" : review!.OwningMilestone == milestone ? "active-owned" : "unfinished-focused-work";
                    entries.Add(new { path = relative, id = review!.Id, owningMilestone = review.OwningMilestone, status = review.Status, classification, rationale = classification == "historical-completed" ? "Completed record retained as immutable historical evidence." : "Pending request is explicitly owned by its milestone." });
                }
                else if (directory == ".review/closed")
                {
                    entries.Add(new { path = relative, id = Path.GetFileNameWithoutExtension(path), owningMilestone = "historical", status = "closed", classification = "historical-completed", rationale = "Legacy request retained beside its immutable completed record." });
                }
                else if (path.EndsWith(".json", StringComparison.Ordinal))
                {
                    entries.Add(new { path = relative, id = Path.GetFileNameWithoutExtension(path), owningMilestone = "", status = "invalid", classification = "invalid", rationale = "Review JSON could not be parsed as a supported review record." });
                }
            }
        }

        var ordered = entries.OrderBy(entry => JsonSerializer.Serialize(entry), StringComparer.Ordinal).ToArray();
        var output = Absolute(Path.Combine("artifacts", "review-migration", milestone));
        Directory.CreateDirectory(output);
        var report = new { schema = "agentic2d.review-migration-report.v1", milestone, generatedBy = "agentic2d-engineering", entries = ordered, fingerprint = "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ordered)))).ToLowerInvariant() };
        await File.WriteAllTextAsync(Path.Combine(output, "review-migration-report.json"), JsonSerializer.Serialize(report, json));
        var markdown = "# Review Migration Report\n\nMilestone: `" + milestone + "`\n\n| Review source | Classification | Ownership | Status |\n|---|---|---|---|\n" + string.Join("\n", ordered.Select(entry => { using var document = JsonDocument.Parse(JsonSerializer.Serialize(entry)); var item = document.RootElement; return "| `" + item.GetProperty("path").GetString() + "` | `" + item.GetProperty("classification").GetString() + "` | `" + item.GetProperty("owningMilestone").GetString() + "` | `" + item.GetProperty("status").GetString() + "` |"; })) + "\n\nNo approval was created by this report. Completed records are historical and do not stale from later commits.\n";
        await File.WriteAllTextAsync(Path.Combine(output, "review-migration-report.md"), markdown);
        await stdout.WriteLineAsync($"review migration report written: artifacts/review-migration/{milestone}");
        return 0;
    }

    private async Task<int> RunInternalShardAsync(ValidationSuite suite, ValidationShard shard, TextWriter diagnostics)
    {
        if (suite.Id == "m037-smoke")
        {
            return await M037ProductShellSuite.RunAsync(this, root, shard.Id, diagnostics);
        }
        if (suite.Id == "m039-smoke")
        {
            return await M039SimulationClosureSuite.RunAsync(root, shard.Id, diagnostics);
        }
        if (suite.Id == "m038-smoke") return await M038SimpleReviewSuite.RunAsync(this, root, shard.Id, diagnostics);
        if (suite.Id == "m036-smoke")
        {
            return await M036EngineeringSuite.RunAsync(root, shard.Id, diagnostics);
        }

        throw new EngineeringException($"unsupported internal shard: {suite.Id}/{shard.Id}");
    }

    public string Root => root;

    private bool HasCurrentReceipt(ValidationSuite suite, string shardId, TextWriter diagnostics)
    {
        var shard = suite.Shards.Single(candidate => candidate.Id == shardId);
        var path = Absolute(ReceiptPath(suite, shard));
        if (!ReceiptStore.TryRead(path, json, out var receipt, out _))
        {
            return false;
        }

        return receipt!.Status == "passed"
            && receipt.RepositoryFingerprint == Fingerprints.Repository(root)
            && receipt.SuiteFingerprint == Fingerprints.Suite(suite, root)
            && receipt.CommandFingerprint == Fingerprints.Command(shard)
            && receipt.InputFingerprint == Fingerprints.Input(root, shard);
    }

    private IReadOnlyList<ArtifactFingerprint> ValidateEvidence(ValidationShard shard, bool throwOnMissing = true)
    {
        var artifacts = new List<ArtifactFingerprint>();
        foreach (var relative in shard.Evidence)
        {
            var absolute = Absolute(relative);
            if (!File.Exists(absolute) && !Directory.Exists(absolute))
            {
                if (throwOnMissing)
                {
                    throw new EngineeringException($"required shard evidence was not produced: {relative}");
                }

                continue;
            }

            artifacts.Add(new ArtifactFingerprint(relative, Fingerprints.Path(absolute)));
        }

        return artifacts;
    }

    private IEnumerable<ReviewState> ReadReviews() => ReadReviewFiles()
        .GroupBy(review => review.Id, StringComparer.Ordinal)
        .Select(SelectCurrentReview)
        .OrderBy(review => review.OwningMilestone, StringComparer.Ordinal)
        .ThenBy(review => review.Id, StringComparer.Ordinal);

    private IEnumerable<ReviewState> ReadReviewFiles()
    {
        foreach (var directory in new[] { ".review/pending", ".review/records", ".review/closed" })
        {
            var absolute = Absolute(directory);
            if (!Directory.Exists(absolute)) continue;
            foreach (var path in Directory.EnumerateFiles(absolute, "*.json").OrderBy(path => path, StringComparer.Ordinal))
            {
                if (TryReadReview(path, out var review, out _)) yield return review!;
            }
        }
    }

    private bool TryReadReview(string path, out ReviewState? review, out string error)
    {
        review = null;
        error = string.Empty;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var item = document.RootElement;
            var schema = String(item, "schema");
            review = schema switch
            {
                "agentic2d.engineering.review-request.v2" or "agentic2d.engineering.review-record.v2" => ReadV2(item, schema),
                "agentic2d.engineering.review.v1" => ReadV1(item),
                _ => null
            };
            if (review is null || string.IsNullOrWhiteSpace(review.Id) || review.Evidence is null || string.IsNullOrWhiteSpace(review.OwningMilestone))
            {
                error = $"malformed review record: {path}";
                return false;
            }

            review = review with { Path = Path.GetRelativePath(root, path).Replace('\\', '/') };
            return true;
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            error = $"cannot read review record '{path}': {exception.Message}";
            return false;
        }
    }

    private void WriteReview(ReviewState review, string relativePath)
    {
        var path = Absolute(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        ReceiptStore.WriteAtomic(path, review, json);
    }

    private string ReceiptPath(ValidationSuite suite, ValidationShard shard) => (suite.Id is "m031-smoke" or "m033-smoke" or "m034-smoke"
        ? Path.Combine("artifacts", "validation", suite.Id, "receipts", shard.Id + ".json")
        : suite.Id == "m038-smoke"
        ? Path.Combine("artifacts", "validation", suite.Id, "receipts", shard.Id + ".json")
        : Path.Combine("artifacts", "validation", suite.Id, shard.Id + ".json")).Replace('\\', '/');
    private string Absolute(string relative) => Path.Combine(root, relative);

    private void EnsurePlatformState(ValidationSuite suite)
    {
        if (suite.Id is "m037-smoke" or "m038-smoke") _ = PlatformVerificationState.Load(root);
    }

    private static IReadOnlyDictionary<string, string> ParseOptions(string[] args)
    {
        if (args.Length % 2 != 0) throw new EngineeringException("review options must be --name value pairs");
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (var index = 0; index < args.Length; index += 2)
        {
            if (!args[index].StartsWith("--", StringComparison.Ordinal)) throw new EngineeringException($"invalid option: {args[index]}");
            options.Add(args[index], args[index + 1]);
        }

        return options;
    }

    private static string Required(IReadOnlyDictionary<string, string> options, string key) => options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
        ? value
        : throw new EngineeringException($"missing required {key} <value>");
    private static IReadOnlyList<string> Split(string value) => value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private ReviewListOptions ParseListOptions(string[] args)
    {
        var options = ParseOptions(args);
        foreach (var key in options.Keys)
        {
            if (key is not ("--milestone" or "--state" or "--status")) throw new EngineeringException($"unsupported review list filter: {key}");
        }

        var state = options.GetValueOrDefault("--state");
        if (state is not null and not ("active" or "historical")) throw new EngineeringException("review list --state must be active or historical");
        return new ReviewListOptions(options.GetValueOrDefault("--milestone"), state, options.GetValueOrDefault("--status"));
    }

    private IEnumerable<ReviewState> FilterReviews(ReviewListOptions options) => ReadReviews()
        .Where(review => options.Milestone is null || review.OwningMilestone == options.Milestone)
        .Where(review => options.State is null || ReviewStateKind(review) == options.State)
        .Where(review => options.Status is null || review.Status == options.Status);

    private void WriteAliasMap(ReviewListOptions options, IReadOnlyList<ReviewState> reviews)
    {
        var map = new ReviewAliasMap(
            "agentic2d.engineering.review-alias-map.v1",
            AliasContextFingerprint(options, reviews),
            options,
            reviews.Select((review, index) => new ReviewAlias(index + 1, review.Id)).ToArray(),
            DateTimeOffset.UtcNow);
        ReceiptStore.WriteAtomic(Absolute(AliasMapPath), map, json);
    }

    private string ResolveReviewTarget(string idOrAlias)
    {
        if (!int.TryParse(idOrAlias, out var alias) || alias < 1) return idOrAlias;
        var path = Absolute(AliasMapPath);
        if (!File.Exists(path)) throw StaleAlias();
        try
        {
            var map = JsonSerializer.Deserialize<ReviewAliasMap>(File.ReadAllText(path), json);
            if (map is null || map.Schema != "agentic2d.engineering.review-alias-map.v1") throw StaleAlias();
            var reviews = FilterReviews(map.Options).ToArray();
            if (map.ContextFingerprint != AliasContextFingerprint(map.Options, reviews)) throw StaleAlias();
            return map.Aliases.SingleOrDefault(item => item.Alias == alias)?.ReviewId ?? throw StaleAlias();
        }
        catch (JsonException)
        {
            throw StaleAlias();
        }
    }

    private static EngineeringException StaleAlias() => new("Review alias is stale or unknown. Run ./eng/review-list.sh again.");

    private static string AliasContextFingerprint(ReviewListOptions options, IReadOnlyList<ReviewState> reviews) =>
        "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { options, reviews })))).ToLowerInvariant();

    private static void ValidateCanonicalReviewId(string id)
    {
        if (!id.StartsWith("review.", StringComparison.Ordinal) || id.Any(character => !(char.IsLower(character) || char.IsDigit(character) || character is '.' or '-')))
        {
            throw new EngineeringException("review ID must be a canonical lowercase review.<name> identifier");
        }
    }

    private static void ValidateDecision(string decision)
    {
        if (decision is not ("approved" or "changes-requested" or "rejected" or "waived" or "superseded"))
        {
            throw new EngineeringException($"unsupported review decision: {decision}");
        }
    }

    private static bool IsFinalDecision(string decision) => decision is "approved" or "rejected" or "waived" or "superseded";

    private bool IsMilestoneActive(string milestone)
    {
        var directory = Absolute(Path.Combine("docs", "milestones"));
        if (!Directory.Exists(directory)) return ReadReviewFiles().Any(review => review.OwningMilestone == milestone && review.Path.StartsWith(".review/pending/", StringComparison.Ordinal));
        var latest = Directory.EnumerateFiles(directory, "*.md", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileNameWithoutExtension)
            .Select(ParseMilestoneId)
            .OrderByDescending(value => value.Sequence)
            .FirstOrDefault();
        return latest.Sequence >= 0 && latest.Id == milestone;
    }

    private static (string Id, int Sequence) ParseMilestoneId(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return (string.Empty, -1);
        var digits = name.StartsWith("MILESTONE-", StringComparison.OrdinalIgnoreCase) ? name.AsSpan("MILESTONE-".Length) : name.StartsWith('M') ? name.AsSpan(1) : ReadOnlySpan<char>.Empty;
        var length = 0;
        while (length < digits.Length && char.IsDigit(digits[length])) length++;
        return length > 0 && int.TryParse(digits[..length], out var sequence)
            ? ("M" + sequence.ToString("D3", System.Globalization.CultureInfo.InvariantCulture), sequence)
            : (string.Empty, -1);
    }

    private string ReviewStateKind(ReviewState review) => !IsFinalDecision(review.Status) || IsMilestoneActive(review.OwningMilestone)
        ? "active"
        : "historical";

    private ReviewState SelectCurrentReview(IEnumerable<ReviewState> entries) => entries
        .OrderByDescending(review => review.Path.StartsWith(".review/pending/", StringComparison.Ordinal))
        .ThenByDescending(review => review.CompletedAt)
        .First();

    private string NextRecordPath(string id)
    {
        var basePath = Path.Combine(".review", "records", id + ".json");
        if (!File.Exists(Absolute(basePath))) return basePath;
        var revision = 2;
        while (File.Exists(Absolute(Path.Combine(".review", "records", id + ".revision-" + revision.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".json")))) revision++;
        return Path.Combine(".review", "records", id + ".revision-" + revision.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".json");
    }

    private static (bool CorrectRecord, IReadOnlyDictionary<string, string> Options) ParseReopenOptions(string[] args)
    {
        var correctRecord = args.Contains("--correct-record", StringComparer.Ordinal);
        return (correctRecord, ParseOptions(args.Where(argument => argument != "--correct-record").ToArray()));
    }

    private static string DisplayValue(string value) => string.IsNullOrWhiteSpace(value) ? "none" : value;
    private static string DisplayList(IEnumerable<string> values) => string.Join("; ", values.DefaultIfEmpty("none"));
    private static string DisplayHistory(IEnumerable<ReviewDecision> decisions) => string.Join("; ", decisions.Select(decision => $"{decision.RecordedAt:O} {decision.Decision} by {decision.Reviewer}: {DisplayValue(decision.Notes)}").DefaultIfEmpty("none"));

    private string RepositoryRevision()
    {
        var head = Path.Combine(root, ".git", "HEAD");
        return File.Exists(head) ? File.ReadAllText(head).Trim() : "unavailable";
    }

    private static ReviewState ReadV2(JsonElement item, string schema) => new(
        schema, String(item, "id"), String(item, "owningMilestone"), String(item, "owningMilestonePath"), String(item, "subject"),
        Strings(item, "classes"), String(item, "level"), String(item, "reviewerRole"), String(item, "status"),
        Strings(item, "requiredEvidence", "evidence"), Strings(item, "acceptanceCriteria"), Strings(item, "acceptableCompletionDecisions", "acceptableDecisions"),
        String(item, "waiverPolicy"), String(item, "decision"), String(item, "reviewedRevision"), String(item, "reviewedFingerprint"),
        Strings(item, "conditions"), DateTimeOffset.TryParse(String(item, "completedAt"), out var completedAt) ? completedAt : null,
        Decisions(item), String(item, "correctsReviewId"), String(item, "path"));

    private static ReviewState? ReadV1(JsonElement item)
    {
        var source = String(item, "source");
        return string.IsNullOrWhiteSpace(source) ? null : new ReviewState(
            String(item, "schema"), String(item, "id"), source, $"docs/milestones/{source}.md", String(item, "subject"),
            [String(item, "class")], String(item, "level"), String(item, "reviewerRole"), String(item, "status"), Strings(item, "evidence"),
            [], ["approved", "waived"], "Historical migrated record.", String(item, "decision"), "legacy", String(item, "reviewedFingerprint"), [], null,
            [new ReviewDecision(String(item, "decision"), String(item, "reviewerRole"), string.Empty, Strings(item, "evidence"), "legacy", String(item, "reviewedFingerprint"), DateTimeOffset.MinValue, false)], string.Empty, String(item, "path"));
    }

    private static string String(JsonElement item, string property) => item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;
    private static IReadOnlyList<string> Strings(JsonElement item, params string[] properties)
    {
        foreach (var property in properties)
        {
            if (item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Array)
                return value.EnumerateArray().Where(value => value.ValueKind == JsonValueKind.String).Select(value => value.GetString() ?? string.Empty).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        }
        return [];
    }

    private static IReadOnlyList<ReviewDecision> Decisions(JsonElement item)
    {
        if (item.TryGetProperty("decisionHistory", out var value) && value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray().Where(decision => decision.ValueKind == JsonValueKind.Object).Select(decision => new ReviewDecision(
                String(decision, "decision"), String(decision, "reviewer"), String(decision, "notes"), Strings(decision, "evidence"), String(decision, "revision"), String(decision, "fingerprint"),
                DateTimeOffset.TryParse(String(decision, "recordedAt"), out var recordedAt) ? recordedAt : DateTimeOffset.MinValue, decision.TryGetProperty("recordCorrection", out var correction) && correction.ValueKind == JsonValueKind.True)).ToArray();
        }

        var decisionValue = String(item, "decision");
        return string.IsNullOrWhiteSpace(decisionValue) ? [] : [new ReviewDecision(decisionValue, String(item, "reviewerRole"), string.Empty, Strings(item, "requiredEvidence", "evidence"), String(item, "reviewedRevision"), String(item, "reviewedFingerprint"), DateTimeOffset.TryParse(String(item, "completedAt"), out var completedAt) ? completedAt : DateTimeOffset.MinValue, false)];
    }

    private static IReadOnlyList<ValidationSuite> BuildSuites() =>
    [
        new("m019-smoke", "resumable-sharded",
        [
            Shard("sound", "Sound content, cue, and loop ownership.", "./eng/sound-content-smoke.sh && ./eng/sound-marker-cue-smoke.sh && ./eng/sound-loop-ownership-smoke.sh", ["artifacts/smoke/sound-content/result.json", "artifacts/smoke/sound-marker-cue/sound-command-frames.jsonl", "artifacts/smoke/sound-loops/sound-playback-state.jsonl"]),
            Shard("damage-lifecycle", "Gameplay damage and defeat lifecycle.", "./eng/gameplay-damage-resource-smoke.sh && ./eng/gameplay-defeat-lifecycle-smoke.sh", ["artifacts/smoke/gameplay-damage/damage-resolutions.jsonl", "artifacts/smoke/gameplay-defeat/lifecycle-transitions.jsonl"]),
            Shard("inventory-collection", "Inventory collection atomicity.", "./eng/gameplay-collection-atomicity-smoke.sh", ["artifacts/smoke/gameplay-collection/collection-resolutions.jsonl", "artifacts/smoke/gameplay-collection/inventory-transitions.jsonl"]),
            Shard("integrated", "Integrated gameplay journey.", "./eng/gameplay-integrated-smoke.sh", ["artifacts/smoke/m019-integrated/sound/sound-result.json", "artifacts/smoke/m019-integrated/gameplay/gameplay-result.json"]),
            Shard("replay", "Gameplay deterministic replay.", "./eng/gameplay-replay-smoke.sh", ["artifacts/smoke/m019-replay-first/runtime/result.json", "artifacts/smoke/m019-replay-second/runtime/result.json"])
        ]),
        new("m020-smoke", "resumable-sharded",
        [
            Shard("save-roundtrip", "Canonical save/load round trip.", "./eng/save-canonical-roundtrip-smoke.sh", ["artifacts/smoke/m020-save-roundtrip/save/save-equivalence.json"]),
            Shard("incompatibility", "Incompatible save rejection.", "./eng/save-incompatibility-smoke.sh", ["artifacts/smoke/m020-save-incompatibility/validation/save-validation.json"]),
            Shard("flags-conditions", "Flag and condition validation.", "./eng/state-flag-condition-smoke.sh", ["artifacts/smoke/m020-flags/validated-items.json"]),
            Shard("switches", "Stateful switch evidence.", "./eng/state-switch-activation-smoke.sh", ["artifacts/smoke/m020-switch/persistent-world/switch-transitions.jsonl"]),
            Shard("doors", "Door collision and projection invalidation.", "./eng/state-door-collision-smoke.sh", ["artifacts/smoke/m020-door/persistent-world/projection-invalidations.jsonl", "artifacts/smoke/m020-door/persistent-world/door-transitions.jsonl"]),
            Shard("integrated-resume", "Persistent-world integrated resume.", "./eng/save-resume-equivalence-smoke.sh && ./eng/persistent-world-integrated-smoke.sh", ["artifacts/smoke/m020-resume-equivalence/persistent-world/persistent-world-result.json", "artifacts/smoke/m020-persistent-world/save/save-snapshot.json"]),
            Shard("review", "Persistent-world structured review.", "./eng/persistent-world-review-smoke.sh", ["artifacts/smoke/m020-persistent-world-review/review/review-summary.md"])
        ]),
        new("m021-smoke", "resumable-sharded",
        [
            Shard("effects", "Presentation effects.", "./eng/presentation-effect-smoke.sh", ["artifacts/smoke/m021-effects/presentation/effect-instances.jsonl"]),
            Shard("particles", "Deterministic particles.", "./eng/presentation-particle-smoke.sh", ["artifacts/smoke/m021-particles/particle-samples.jsonl"]),
            Shard("camera", "Camera state and requests.", "./eng/presentation-camera-smoke.sh", ["artifacts/smoke/m021-camera/camera-states.jsonl"]),
            Shard("ui-text", "UI layout and text commands.", "./eng/presentation-ui-text-smoke.sh", ["artifacts/smoke/m021-ui-text/presentation/ui-layout.jsonl", "artifacts/smoke/m021-ui-text/presentation/text-commands.jsonl"]),
            Shard("interaction-surfaces", "Prompts and notifications.", "./eng/presentation-interaction-surface-smoke.sh", ["artifacts/smoke/m021-interaction-surface/interaction-prompts.jsonl", "artifacts/smoke/m021-interaction-surface/notifications.jsonl"]),
            Shard("integrated", "Player-facing presentation journey.", "./eng/presentation-integrated-smoke.sh", ["artifacts/smoke/m021-integrated/presentation/player-facing-presentation-result.json"]),
            Shard("replay", "Presentation replay equivalence.", "./eng/presentation-replay-smoke.sh", ["artifacts/smoke/m021-replay-a/presentation-composition.jsonl", "artifacts/smoke/m021-replay-b/presentation-composition.jsonl"]),
            Shard("post-load", "Post-load transient reconstruction policy.", "./eng/presentation-post-load-smoke.sh", ["artifacts/smoke/m021-resumed/presentation/player-facing-presentation-result.json"], ["integrated"]),
            Shard("review", "Presentation review pack.", "./eng/presentation-review-smoke.sh", ["artifacts/smoke/m021-integrated/review/review-summary.md"], ["integrated"])
        ]),
        new("m023-smoke", "resumable-sharded",
        [
            Shard("metrics-contracts", "Finite metric vocabulary and bounded collector tests.", "./eng/test-filter.sh Metrics", ["src/Agentic2D.Metrics/RuntimeMetrics.cs", "tests/unit/Agentic2D.Tests.Unit/MetricsTests.cs"]),
            Shard("runtime-instrumentation", "Runtime metrics leave semantic output unchanged.", "./eng/test-filter.sh Metrics", ["src/Agentic2D.Engine/MinimalRuntime.cs", "tests/unit/Agentic2D.Tests.Unit/MetricsTests.cs"]),
            Shard("metrics-artifacts", "Summary and bounded per-tick artifacts.", "./eng/metrics-artifacts-smoke.sh", ["artifacts/smoke/m023-metrics/metrics-summary.json", "artifacts/smoke/m023-metrics/metrics-ticks.jsonl"]),
            Shard("comparative-workloads", "Reference workload capture.", "./eng/perf-smoke.sh", ["artifacts/performance/smoke/performance-capture.json"]),
            Shard("performance-report", "Advisory comparison and report generation.", "./eng/perf-report-smoke.sh", ["artifacts/performance/m023/performance-report.json", "artifacts/performance/m023/performance-report.md"]),
            Shard("integrated", "Direct build, test, and product integration checks.", "./eng/build.sh && ./eng/test.sh && ./eng/cli-smoke.sh && ./eng/product-validate.sh", ["artifacts/cli/runtime-smoke/result.json", "artifacts/cli/validate/result.json"])
        ]),
        new("m026-smoke", "resumable-sharded",
        [
            Shard("geometry-diagnostics", "Geometry inspection, headless preview, comparison, and all-shape evidence.", "./eng/geometry-diagnostics-smoke.sh", ["artifacts/geometry/M026/signal-passage/geometry-inspection.json", "artifacts/geometry/M026/tic-tac-toe/geometry-projection-comparison.json", "artifacts/geometry/M026/all-supported-shapes/geometry-inspection.json"]),
            Shard("sound-linkage", "Explicit synthesis/WAV/provenance/sound linkage for both consumers.", "./eng/generated-sound-linkage-smoke.sh", ["artifacts/sound-linkage/M026/signal-passage/generated-sound-linkage-report.json", "artifacts/sound-linkage/M026/tic-tac-toe/generated-sound-linkage-report.json"]),
            Shard("scaled-performance", "Small-workload timing policy and scaled real workloads.", "./eng/perf-smoke.sh && ./eng/scaled-performance-smoke.sh && ./eng/m026-performance-report.sh", ["artifacts/performance/M026/performance-report.json", "artifacts/performance/M026/performance-report.md"]),
            Shard("tic-tac-toe-core", "Autonomous rounds, deterministic choice, takeover, rejection, win/draw, and reset.", "./eng/tic-tac-toe-smoke.sh", ["consumers/autonomous-tic-tac-toe/artifacts/runs/deterministic-random-choice/tic-tac-toe-result.json", "consumers/autonomous-tic-tac-toe/artifacts/runs/draw/tic-tac-toe-result.json"]),
            Shard("tic-tac-toe-presentation", "Board structural presentation and cue evidence.", "./eng/tic-tac-toe-smoke.sh", ["consumers/autonomous-tic-tac-toe/artifacts/runs/presentation-smoke/tic-tac-toe-presentation.json", "consumers/autonomous-tic-tac-toe/artifacts/geometry/geometry-preview.json"]),
            Shard("tic-tac-toe-persistence", "Save during deterministic AI thinking excludes transient feedback.", "./eng/tic-tac-toe-smoke.sh", ["consumers/autonomous-tic-tac-toe/artifacts/runs/save-during-thinking/tic-tac-toe-save.json"]),
            Shard("workspace-isolation", "Relocated external-style tic-tac-toe workspace validates and runs.", "./eng/tic-tac-toe-isolation.sh", ["artifacts/tic-tac-toe/isolation/workspace-validation.json", "artifacts/tic-tac-toe/isolation/run-manifest.json"]),
            Shard("linux-export", "Consumer Linux export, actual consumer launch, playable launcher publication, and development/export equivalence.", "./eng/tic-tac-toe-export.sh", ["artifacts/tic-tac-toe/export/game/agentic2d.export.json", "artifacts/tic-tac-toe/export/run/run-manifest.json", "artifacts/tic-tac-toe/export/playable-linux-x64/AutonomousTicTacToe.Playable", "artifacts/tic-tac-toe/export/development-export-equivalence.json"]),
            Shard("consumer-boundary-report", "Evidence-based cross-consumer boundary decisions.", "test -f artifacts/consumer-boundaries/M026/consumer-boundary-decision-report.json && test -f artifacts/consumer-boundaries/M026/consumer-boundary-decision-report.md", ["artifacts/consumer-boundaries/M026/consumer-boundary-decision-report.json", "artifacts/consumer-boundaries/M026/consumer-boundary-decision-report.md"]),
            Shard("human-review", "Historical M026 visual, UX, and artifact-quality review is present and approved.", "./eng/review-check.sh --milestone M026", [".review/records/review.m026.geometry-diagnostics-and-autonomous-tic-tac-toe.json"]),
            Shard("integrated", "Provider gate plus both consumer journeys.", "./eng/check.sh && ./eng/cli-smoke.sh && ./eng/product-validate.sh && ./eng/signal-passage-smoke.sh && ./eng/tic-tac-toe-smoke.sh", ["consumers/signal-passage/artifacts/journey/complete-journey.json", "consumers/autonomous-tic-tac-toe/artifacts/runs/ai-vs-ai-smoke/tic-tac-toe-result.json"])
        ]),
        new("m027-smoke", "resumable-sharded",
        [
            Shard("review-migration", "All six review commands, alias lifecycle, state transitions, historical safeguards, migration inventory, and documentation consistency.", "./eng/review-migration-smoke.sh", ["artifacts/review-migration/M027/review-migration-report.json", "artifacts/review-migration/M027/review-migration-report.md"]),
            Shard("geometry-contracts", "Stable geometry schemas, diagnostics, and both consumer packs.", "./eng/geometry-diagnostics-smoke.sh && ./eng/geometry-review-pack-smoke.sh", ["artifacts/geometry/M027/signal-passage/manifest.json", "artifacts/geometry/M027/tic-tac-toe/manifest.json"]),
            Shard("sound-linkage-contracts", "Stable generated-sound linkage and provenance across both consumers.", "./eng/generated-sound-linkage-smoke.sh && ./eng/generated-sound-review-pack-smoke.sh", ["artifacts/sound-linkage/M027/signal-passage/manifest.json", "artifacts/sound-linkage/M027/tic-tac-toe/manifest.json"]),
            Shard("review-packs", "Bounded durable consumer-authoring review pack.", "./eng/consumer-authoring-review-pack-smoke.sh", ["artifacts/review/M027/review-pack/manifest.json", "artifacts/review/M027/review-pack/index.md"], ["review-migration", "geometry-contracts", "sound-linkage-contracts"]),
            Shard("scenario-persistence-diagnostics", "Scenario assertion and persistence comparison diagnostics.", "./eng/scenario-diagnostics-smoke.sh && ./eng/persistence-diagnostics-smoke.sh", ["artifacts/review/M027/scenarios/scenario-diagnostics.json", "artifacts/review/M027/persistence/persistence-diagnostics.json"]),
            Shard("signal-passage", "Signal Passage compatibility fixture.", "./eng/signal-passage-smoke.sh", ["consumers/signal-passage/artifacts/journey/complete-journey.json"]),
            Shard("tic-tac-toe", "Autonomous Tic-Tac-Toe compatibility fixture and persistence.", "./eng/tic-tac-toe-smoke.sh", ["consumers/autonomous-tic-tac-toe/artifacts/runs/save-during-thinking/tic-tac-toe-save.json"]),
            Shard("performance-regression", "Unchanged scaled-performance policy.", "./eng/perf-smoke.sh && ./eng/scaled-performance-smoke.sh", ["artifacts/performance/smoke/performance-capture.json"]),
            Shard("documentation", "Active M027 contracts and command documentation are present.", "test -f docs/specs/geometry-authoring-diagnostics-contract.md && test -f docs/specs/generated-sound-linkage-contract.md && test -f docs/artifacts/consumer-authoring-review-pack-artifact-contract.md", ["docs/specs/geometry-authoring-diagnostics-contract.md", "docs/specs/generated-sound-linkage-contract.md", "docs/artifacts/consumer-authoring-review-pack-artifact-contract.md"]),
            Shard("human-review", "Blocking M027 review is approved by a human.", "./eng/review-check.sh --milestone M027", [".review/records/review.m027.authoring-contracts-review-evidence-and-v060-migration.json"]),
            Shard("integrated", "Provider build, test, and product validation.", "./eng/check.sh && ./eng/cli-smoke.sh && ./eng/product-validate.sh", ["artifacts/cli/runtime-smoke/result.json", "artifacts/cli/validate/result.json"])
        ]),
        new("m028-smoke", "resumable-sharded",
        [
            Shard("m011-audit-generalization", "Evidence-backed M011 capability audit and generated unknown-library discovery acceptance.", "./eng/m028-generalization-smoke.sh", ["artifacts/assets/M028/m011-capability-audit.json", "artifacts/assets/M028/generalization/unknown-library-acceptance.json", "artifacts/assets/M028/generalization/metamorphic-test-results.json"]),
            Shard("asset-home", "Local asset-home resolution and safe stale cleanup.", "./eng/asset-home-smoke.sh", ["artifacts/assets/M028/home/asset-home.json"]),
            Shard("source-registry", "Path-independent source registry and refresh pointer.", "./eng/asset-source-registry-smoke.sh", ["artifacts/assets/M028/registry/refresh/source-profile.json"], ["m011-audit-generalization"]),
            Shard("image-discovery", "Deterministic PNG grid, regions, duplicates, and animation proposals.", "./eng/asset-source-profile-smoke.sh", ["artifacts/assets/M028/discovery/profile/image-observations.jsonl"], ["m011-audit-generalization"]),
            Shard("audio-discovery", "Bounded WAV observations.", "./eng/m028-provider-smoke.sh audio", ["artifacts/assets/M028/audio/profile/audio-observations.jsonl"], ["m011-audit-generalization"]),
            Shard("annotations-cleanup", "Generated cleanup retains reusable annotations.", "./eng/asset-source-annotation-smoke.sh && ./eng/asset-source-cleanup-smoke.sh", ["artifacts/assets/M028/annotations/list/annotations.json"]),
            Shard("campaign-reuse", "Campaign proposals stay separate from shared discovery.", "./eng/asset-campaign-smoke.sh", ["artifacts/assets/M028/campaign/campaign/propose/proposal-summary.json"]),
            Shard("batch-proposals", "Bounded batch inventory, proposals, validation, and review evidence.", "./eng/asset-batch-smoke.sh", ["artifacts/assets/M028/batch/batch/review/asset-review-pack/manifest.json"]),
            Shard("headless-review-pack", "Copyable headless image/audio evidence and M029 readiness.", "./eng/asset-discovery-review-pack-smoke.sh", ["artifacts/assets/M028/review-pack/review/asset-review-pack/manifest.json"]),
            Shard("m011-regression", "M011 asset inspection/perception/review/curation remain valid.", "./eng/m011-smoke.sh", ["artifacts/review/m011/review-manifest.json"]),
            Shard("documentation", "M028 authority and command documentation are present.", "./eng/m028-documentation-smoke.sh", ["docs/specs/shared-asset-home-and-source-registry-contract.md", "docs/specs/asset-campaign-and-batch-contract.md", "artifacts/assets/M028/documentation/diff-summary.md"]),
            Shard("human-review", "Blocking M028 review is approved by a human.", "./eng/review-check.sh --milestone M028", [".review/records/review.m028.shared-asset-library-discovery-and-campaign-foundation.json"]),
            Shard("integrated", "Provider build, product validation, and CLI integration.", "./eng/build.sh && ./eng/cli-smoke.sh && ./eng/product-validate.sh", ["artifacts/cli/runtime-smoke/result.json", "artifacts/cli/validate/result.json"])
        ]),
        new("m029-smoke", "resumable-sharded",
        [
            Shard("session-aliases", "Persistent sessions, regenerated aliases, and safe stale-alias behavior.", "./eng/asset-workbench-session-smoke.sh && ./eng/asset-workbench-alias-smoke.sh", ["artifacts/assets/M029/aliases/aliases.json"]),
            Shard("rdp-text-input", "Editable text stream, explicit submission, correction, paste, composition, focus recovery, and invalid-input behavior.", "./eng/test-filter.sh AssetWorkbenchInput && ./eng/asset-workbench-input-smoke.sh && ./eng/asset-workbench-rdp-input-smoke.sh", ["artifacts/assets/M029/input/rdp/input-state.json"]),
            Shard("mouse-touch-input", "Mouse/touch visible choice selection works without raw key events.", "./eng/asset-workbench-mouse-input-smoke.sh", ["artifacts/assets/M029/input/mouse/input-result.json"]),
            Shard("input-equivalence", "Mouse, text, and headless input share canonical actions.", "./eng/asset-workbench-smoke.sh equivalence", ["artifacts/assets/M029/input/mouse/input-result.json"]),
            Shard("guided-decisions", "Canonical decision history and bounded guided review.", "./eng/test-filter.sh AssetWorkbenchDecision && ./eng/asset-workbench-decision-smoke.sh", ["artifacts/assets/M029/decisions/review-decisions.jsonl"]),
            Shard("consequence-confirmation", "High-impact implications require explicit confirmation or presentation-only approval.", "./eng/asset-workbench-consequence-smoke.sh", ["artifacts/assets/M029/decisions/review-decisions.jsonl"]),
            Shard("preview-ipc", "Versioned persistent actual-engine preview protocol evidence.", "./eng/test-filter.sh AssetPreviewIpc && ./eng/asset-preview-ipc-smoke.sh", ["artifacts/assets/M029/preview/preview-ipc.json"]),
            Shard("preview-recovery", "Preview restart preserves input and decisions.", "./eng/asset-preview-recovery-smoke.sh", ["artifacts/assets/M029/recovery/input-result.json"]),
            Shard("graphical-preview", "Preview visual controls, overlays, and malformed-candidate diagnostic scene.", "./eng/asset-preview-graphical-smoke.sh", ["artifacts/assets/M029/preview/malformed/preview-scene.json"]),
            Shard("audio-preview", "Manual audio projection, A/B, and safe no-device evidence.", "./eng/asset-preview-audio-smoke.sh", ["artifacts/assets/M029/preview/preview-scene.json"]),
            Shard("promotion", "Deterministic staged promotion and approved-definition validation.", "./eng/test-filter.sh AssetPromotion && ./eng/asset-promotion-smoke.sh", ["artifacts/assets/M029/promotion/workspace/promotion-manifest.json"]),
            Shard("affected-rebuild", "Affected rebuild limits changes to dependencies.", "./eng/asset-affected-rebuild-smoke.sh", ["artifacts/assets/M029/promotion/rebuild/affected-rebuild.json"]),
            Shard("workbench-review-pack", "M029 bounded review pack and M030 readiness handoff.", "./eng/asset-workbench-review-pack-smoke.sh", ["artifacts/assets/M029/workbench/asset-workbench-review-pack/manifest.json", "artifacts/assets/M029/m030-readiness.json"]),
            Shard("m028-regression", "M028 provider remains valid historical foundation.", "./eng/m028-provider-smoke.sh review-pack", ["artifacts/assets/M028/review-pack/review/asset-review-pack/manifest.json"]),
            Shard("documentation", "M029 active authority and command documentation are indexed.", "test -f docs/specs/asset-workbench-input-contract.md && test -f docs/specs/approved-asset-and-deterministic-promotion-contract.md && test -f docs/artifacts/asset-workbench-session-and-promotion-review-pack-contract.md", ["docs/specs/asset-workbench-input-contract.md", "docs/specs/approved-asset-and-deterministic-promotion-contract.md"]),
            Shard("human-review", "Blocking M029 review is approved by a human.", "./eng/review-check.sh --milestone M029", [".review/records/review.m029.choice-driven-workbench-preview-and-promotion.json"]),
            Shard("integrated", "Provider build, workbench flow, and product validation.", "./eng/build.sh && ./eng/asset-workbench-smoke.sh integrated && ./eng/product-validate.sh", ["artifacts/assets/M029/workbench/asset-workbench-review-pack/manifest.json", "artifacts/cli/validate/result.json"])
        ]),
        new("m031-smoke", "resumable-sharded",
        [
            Shard("documentation", "M031 active specifications, scenario, artifact contract, and command indexes are present.", "test -f docs/specs/simulation-world-and-semantic-foundation-contract.md && test -f docs/scenarios/m031-headless-wood-workflow.md && test -f docs/artifacts/simulation-foundation-artifact-contract.md", ["docs/specs/simulation-world-and-semantic-foundation-contract.md", "docs/artifacts/simulation-foundation-artifact-contract.md"]),
            Shard("component-registration", "Deterministic game-defined component registration.", "./eng/test-filter.sh SimulationFoundation", ["tests/unit/Agentic2D.Tests.Unit/SimulationFoundationTests.cs"]),
            Shard("entity-lifecycle", "Validated lifecycle and identity tombstones.", "./eng/test-filter.sh SimulationWorld", ["src/Agentic2D.Simulation/SimulationFoundation.cs"]),
            Shard("region-partition", "One-world deterministic region partition evidence.", "./eng/simulation-world-smoke.sh", ["artifacts/simulation/M031/world-after.json"]),
            Shard("simulation-time-ordering", "Semantic clock and deterministic ordering.", "./eng/simulation-time-smoke.sh", ["artifacts/simulation/M031/world-after.json"]),
            Shard("commands-domain-events", "Atomic commands and factual post-commit events.", "./eng/simulation-command-event-smoke.sh", ["artifacts/simulation/M031/command-results.jsonl", "artifacts/simulation/M031/domain-events.jsonl"]),
            Shard("activities-reservations", "Explicit stage/revision and reservation semantics.", "./eng/simulation-activity-reservation-smoke.sh", ["artifacts/simulation/M031/activities.json", "artifacts/simulation/M031/reservations.json"]),
            Shard("persistence-roundtrip", "Canonical transactional fresh-process persistence.", "./eng/simulation-persistence-smoke.sh", ["artifacts/simulation/M031/persistence-report.json", "artifacts/simulation/M031/fingerprints.json"]),
            Shard("inspection-artifacts", "Structured semantic inspection and artifact contract evidence.", "./eng/simulation-inspection-smoke.sh", ["artifacts/simulation/M031/invariants.json", "artifacts/simulation/M031/performance-baseline.json"]),
            Shard("wood-workflow", "Bounded two-region harvest/deposit proof.", "./eng/m031-wood-workflow-smoke.sh", ["artifacts/simulation/M031/wood-workflow/comparison.json", "artifacts/simulation/M031/review-pack/review-manifest.json"]),
            Shard("runtime-regression", "Existing runtime/entity/behavior/spatial/inspection/persistence evidence remains structurally valid.", "test -s artifacts/runtime/entity-runtime-smoke/result.json && test -s artifacts/runtime/continuous-kinematic-tree-collision-smoke/continuous-resolutions.jsonl && test -s artifacts/runtime/inspect/result.json && test -s artifacts/scenarios/runtime-smoke/result.json && jq -e '.status == \"passed\"' artifacts/review/M027/persistence/persistence-diagnostics.json >/dev/null", ["artifacts/runtime/entity-runtime-smoke/result.json", "artifacts/runtime/continuous-kinematic-tree-collision-smoke/continuous-resolutions.jsonl", "artifacts/runtime/inspect/result.json", "artifacts/scenarios/runtime-smoke/result.json", "artifacts/review/M027/persistence/persistence-diagnostics.json"]),
            Shard("asset-train-regression", "Implemented M028 and M029 provider surfaces remain available.", "./eng/m028-provider-smoke.sh review-pack && ./eng/asset-workbench-smoke.sh integrated", ["artifacts/assets/M028/review-pack/review/asset-review-pack/manifest.json", "artifacts/assets/M029/workbench/asset-workbench-review-pack/manifest.json"]),
            Shard("human-review", "Blocking M031 review is approved by a human.", "./eng/review-check.sh --milestone M031", [".review/records/review.m031.simulation-world-and-semantic-foundation.json"]),
            Shard("integrated", "M031 build, focused proof, and product integration.", "./eng/build.sh && ./eng/test-filter.sh SimulationFoundation && ./eng/m031-wood-workflow-smoke.sh", ["artifacts/simulation/M031/foundation-manifest.json"])
        ]),
        new("m032-smoke", "resumable-sharded",
        [
            Shard("documentation", "M032 contracts, scenario, and artifact authority are present.", "test -f docs/specs/autonomous-work-and-detailed-logistics-contract.md && test -f docs/specs/detailed-grid-navigation-and-activity-execution-contract.md && test -f docs/artifacts/autonomous-detailed-region-artifact-contract.md", ["docs/specs/autonomous-work-and-detailed-logistics-contract.md", "docs/artifacts/autonomous-detailed-region-artifact-contract.md"]),
            Shard("autonomous-work", "Derived opportunities and deterministic worker selection.", "./eng/test-filter.sh AutonomousWork && ./eng/test-filter.sh WorkOpportunity && ./eng/test-filter.sh WorkerSelection && ./eng/designation-work-smoke.sh && ./eng/worker-selection-smoke.sh", ["artifacts/simulation/M032/work-opportunities.json", "artifacts/simulation/M032/worker-decisions.jsonl"]),
            Shard("detailed-navigation", "Four-directional deterministic detailed routes and replanning.", "./eng/test-filter.sh DetailedGridNavigation && ./eng/detailed-grid-navigation-smoke.sh", ["artifacts/simulation/M032/navigation-results.jsonl", "artifacts/simulation/M032/route-events.jsonl"]),
            Shard("execution-logistics", "Command-backed activities and conserved finite storage.", "./eng/test-filter.sh DetailedActivityExecution && ./eng/test-filter.sh Logistics && ./eng/detailed-activity-execution-smoke.sh && ./eng/logistics-conservation-smoke.sh", ["artifacts/simulation/M032/logistics-ledger.json", "artifacts/simulation/M032/activities.json"]),
            Shard("needs-persistence-projection", "Need interruption, carrying save/load, and headless projection.", "./eng/test-filter.sh BasicNeeds && ./eng/test-filter.sh DetailedRegionPersistence && ./eng/test-filter.sh DetailedRegionProjection && ./eng/basic-needs-interruption-smoke.sh && ./eng/detailed-region-persistence-smoke.sh && ./eng/detailed-region-projection-smoke.sh", ["artifacts/simulation/M032/needs.json", "artifacts/simulation/M032/persistence-report.json", "artifacts/simulation/M032/structural-frames/post-load.json"]),
            Shard("forest-logistics", "End-to-end forest logistics proof and review pack.", "./eng/m032-forest-logistics-smoke.sh", ["artifacts/simulation/M032/forest-logistics/comparison.json", "artifacts/simulation/M032/review-pack/review-manifest.json"]),
            Shard("graphics", "Graphics smoke reports a classified result.", "./eng/m032-detailed-region-graphics-smoke.sh", ["artifacts/simulation/M032/graphical-evidence/environment.json"]),
            Shard("human-review", "Blocking M032 review is approved by a human.", "./eng/review-check.sh --milestone M032", [".review/records/review.m032.autonomous-detailed-region-work-and-logistics.json"]),
            Shard("integrated", "M032 build and complete bounded proof.", "./eng/build.sh && ./eng/m032-forest-logistics-smoke.sh", ["artifacts/simulation/M032/m032-manifest.json"])
        ]),
        new("m033-smoke", "resumable-sharded",
        [
            Shard("documentation", "M033 authority, scenario, artifact contract, and direct indexes are present.", "test -f docs/specs/discrete-event-simulation-contract.md && test -f docs/specs/abstract-activity-and-travel-contract.md && test -f docs/specs/region-fidelity-and-reconciliation-contract.md && test -f docs/specs/multi-fidelity-equivalence-contract.md && test -f docs/artifacts/multi-fidelity-simulation-artifact-contract.md", ["docs/specs/discrete-event-simulation-contract.md", "docs/artifacts/multi-fidelity-simulation-artifact-contract.md"]),
            Shard("scheduler-ordering", "Deterministic equal-time event ordering and bounded advancement.", "./eng/test-filter.sh DiscreteEvent && ./eng/discrete-event-scheduler-smoke.sh", ["artifacts/simulation/M033/queue-inspection.json"]),
            Shard("trigger-invalidation", "Cancellation, stale revisions, and duplicate-delivery safety.", "./eng/test-filter.sh ScheduledTrigger && ./eng/discrete-event-scheduler-smoke.sh", ["artifacts/simulation/M033/trigger-outcomes.jsonl"]),
            Shard("standalone-host", "Headless accelerated simulation host and structural receipt.", "./eng/test-filter.sh StandaloneSimulationHost && ./eng/standalone-simulation-smoke.sh", ["artifacts/simulation/M033/long-horizon-report.json"]),
            Shard("abstract-work-logistics", "Command-backed abstract activity and logistics families.", "./eng/test-filter.sh AbstractActivity && ./eng/abstract-activity-smoke.sh", ["artifacts/simulation/M033/invariants.json"]),
            Shard("abstract-needs", "Lazy semantic need threshold duration evidence.", "./eng/abstract-needs-smoke.sh", ["artifacts/simulation/M033/duration-models.json"]),
            Shard("abstract-travel", "Coarse graph planning independent of detailed paths.", "./eng/test-filter.sh AbstractTravel && ./eng/abstract-travel-smoke.sh", ["artifacts/simulation/M033/abstract-routes.jsonl"]),
            Shard("fidelity-ownership", "Exactly one detailed region and explicit executor owners.", "./eng/test-filter.sh RegionFidelity && ./eng/region-fidelity-smoke.sh", ["artifacts/simulation/M033/executor-ownership.json"]),
            Shard("abstract-to-detailed", "Deterministic materialization and route reconstruction evidence.", "./eng/test-filter.sh RegionReconciliation && ./eng/region-reconciliation-smoke.sh", ["artifacts/simulation/M033/materialization-mappings.jsonl"]),
            Shard("detailed-to-abstract", "Deterministic abstraction and next-trigger evidence.", "./eng/region-reconciliation-smoke.sh", ["artifacts/simulation/M033/abstraction-mappings.jsonl"]),
            Shard("transition-rollback", "Failed materialization restores a prior stable owner.", "./eng/test-filter.sh RegionReconciliation", ["tests/unit/Agentic2D.Tests.Unit/M033MultiFidelitySimulationTests.cs"]),
            Shard("mixed-fidelity-persistence", "Stable mixed-fidelity queue and ownership restore.", "./eng/test-filter.sh MultiFidelityPersistence && ./eng/multi-fidelity-persistence-smoke.sh", ["artifacts/simulation/M033/persistence-report.json"]),
            Shard("equivalence-conservation", "Independent conservation and declared bounded equivalence.", "./eng/test-filter.sh MultiFidelityEquivalence && ./eng/multi-fidelity-equivalence-smoke.sh", ["artifacts/simulation/M033/conservation-ledger.json", "artifacts/simulation/M033/equivalence-report.json"]),
            Shard("observer-neutrality", "Control-run productivity and safety comparison.", "./eng/multi-fidelity-equivalence-smoke.sh", ["artifacts/simulation/M033/observer-neutrality-report.json"]),
            Shard("long-horizon", "Thirty-day repeated-switch standalone proof.", "./eng/m033-multi-region-smoke.sh", ["artifacts/simulation/M033/long-horizon-report.json"]),
            Shard("graphical-switch-proof", "Graphics-capable capture or explicit classified skip.", "./eng/m033-region-switch-graphics-smoke.sh", ["artifacts/simulation/M033/graphical-evidence/environment.json"]),
            Shard("m031-m032-regression", "M031/M032 aggregate receipts remain current.", "./eng/m031-smoke.sh --verify && ./eng/m032-smoke.sh --verify", ["artifacts/validation/m031-smoke/verify.json", "artifacts/validation/m032-smoke/verify.json"]),
            Shard("engine-regression", "Current provider build and focused simulation tests.", "./eng/build.sh && ./eng/test-filter.sh SimulationFoundation", ["src/Agentic2D.Simulation/M033MultiFidelitySimulation.cs"]),
            Shard("asset-train-regression", "Earlier asset provider artifacts remain available.", "test -s artifacts/assets/M028/review-pack/review/asset-review-pack/manifest.json && test -s artifacts/assets/M029/workbench/asset-workbench-review-pack/manifest.json", ["artifacts/assets/M028/review-pack/review/asset-review-pack/manifest.json", "artifacts/assets/M029/workbench/asset-workbench-review-pack/manifest.json"]),
            Shard("human-review", "Blocking M033 review is approved by a human.", "./eng/review-check.sh --milestone M033", ["artifacts/simulation/M033/review-pack/review-manifest.json"]),
            Shard("integrated", "M033 build and complete structural proof.", "./eng/build.sh && ./eng/m033-multi-region-smoke.sh", ["artifacts/simulation/M033/m033-manifest.json", "artifacts/simulation/M033/review-pack/review-manifest.json"])
        ]),
        new("m034-smoke", "resumable-sharded",
        [
            Shard("documentation", "M034 authority, scenario, artifact contract, and direct indexes are present.", "test -f docs/specs/construction-and-infrastructure-lifecycle-contract.md && test -f docs/specs/environmental-resource-and-flow-contract.md && test -f docs/specs/settlement-operations-surface-contract.md && test -f docs/artifacts/settlement-infrastructure-and-operations-artifact-contract.md", ["docs/specs/construction-and-infrastructure-lifecycle-contract.md", "docs/artifacts/settlement-infrastructure-and-operations-artifact-contract.md"]),
            Shard("construction-plans", "Validated planning and cancellation conservation.", "./eng/test-filter.sh ConstructionPlan && ./eng/construction-lifecycle-smoke.sh", ["artifacts/simulation/M034/construction-plans.json"]),
            Shard("construction-execution", "Delivery, activity-backed work, and completion.", "./eng/test-filter.sh InfrastructureLifecycle && ./eng/construction-lifecycle-smoke.sh", ["artifacts/simulation/M034/structures.json"]),
            Shard("water-flow", "Conserved collector, storage, hauling, and consumption.", "./eng/test-filter.sh EnvironmentalResource && ./eng/test-filter.sh WaterFlow && ./eng/water-infrastructure-smoke.sh", ["artifacts/simulation/M034/water-flow.json"]),
            Shard("food-farming", "Farm preparation through harvest and storage.", "./eng/test-filter.sh CropProduction && ./eng/farm-production-smoke.sh", ["artifacts/simulation/M034/farm-production.json"]),
            Shard("comfort-capacity", "Finite comfort infrastructure capacity.", "./eng/test-filter.sh ComfortInfrastructure && ./eng/comfort-capacity-smoke.sh", ["artifacts/simulation/M034/comfort-capacity.json"]),
            Shard("wear-maintenance", "Deterministic wear, failure, and repair.", "./eng/test-filter.sh Maintenance && ./eng/maintenance-failure-smoke.sh", ["artifacts/simulation/M034/maintenance.json"]),
            Shard("roads-travel-modifiers", "Shared detailed/abstract road cost authority.", "./eng/road-travel-modifier-smoke.sh", ["artifacts/simulation/M034/roads.json"]),
            Shard("policies-alerts", "Reserve policy and causal deterministic alerts.", "./eng/test-filter.sh SettlementAlert && ./eng/settlement-alert-smoke.sh", ["artifacts/simulation/M034/alerts.jsonl"]),
            Shard("operations-projection", "Read-only world and region operations dashboard.", "./eng/test-filter.sh OperationsProjection && ./eng/operations-surface-smoke.sh", ["artifacts/simulation/M034/world-dashboard.json"]),
            Shard("operations-input", "Explicit command journal for planning, policies, switching, save/load.", "./eng/operations-surface-smoke.sh", ["artifacts/simulation/M034/operations-commands.jsonl"]),
            Shard("mixed-fidelity-infrastructure", "Infrastructure remains semantic across fidelity switching.", "./eng/m034-settlement-smoke.sh", ["artifacts/simulation/M034/mixed-fidelity-report.json"]),
            Shard("persistence-resume", "Fresh-process infrastructure continuation evidence.", "./eng/test-filter.sh InfrastructurePersistence && ./eng/infrastructure-persistence-smoke.sh", ["artifacts/simulation/M034/persistence-report.json"]),
            Shard("shortage-recovery", "Water, storage, and maintenance recovery proof.", "./eng/m034-settlement-smoke.sh", ["artifacts/simulation/M034/shortage-recovery-report.json"]),
            Shard("sustained-fourteen-day", "Fourteen post-stabilization days with declared reserves.", "./eng/m034-settlement-smoke.sh", ["artifacts/simulation/M034/sustained-run-report.json"]),
            Shard("graphical-play-proof", "Graphics-capable M034 operations proof.", "./eng/m034-settlement-graphics-smoke.sh", ["artifacts/simulation/M034/graphical-evidence/environment.json"]),
            Shard("m031-m033-regression", "M031 through M033 aggregate receipts remain current.", "./eng/m031-smoke.sh --verify && ./eng/m032-smoke.sh --verify && ./eng/m033-smoke.sh --verify", ["artifacts/validation/m031-smoke/verify.json", "artifacts/validation/m033-smoke/verify.json"]),
            Shard("engine-regression", "Provider build and focused M034 test suite.", "./eng/build.sh && ./eng/test-filter.sh M034SettlementInfrastructure", ["src/Agentic2D.Simulation/M034SettlementInfrastructure.cs"]),
            Shard("asset-train-regression", "Earlier asset provider artifacts remain available.", "test -s artifacts/assets/M028/review-pack/review/asset-review-pack/manifest.json && test -s artifacts/assets/M029/workbench/asset-workbench-review-pack/manifest.json", ["artifacts/assets/M028/review-pack/review/asset-review-pack/manifest.json", "artifacts/assets/M029/workbench/asset-workbench-review-pack/manifest.json"]),
            Shard("human-review", "Blocking M034 review is approved by a human.", "./eng/review-check.sh --milestone M034", ["artifacts/simulation/M034/review-pack/review-manifest.json"]),
            Shard("integrated", "M034 build and full structural settlement proof.", "./eng/build.sh && ./eng/m034-settlement-smoke.sh", ["artifacts/simulation/M034/m034-manifest.json", "artifacts/simulation/M034/review-pack/review-manifest.json"])
        ]),
        new("m035-smoke", "resumable-sharded",
        [
            Shard("documentation", "M035 authority, runbook, campaign, artifact contract, and indexes are present.", "test -f docs/specs/internal-testing-scale-and-performance-contract.md && test -f docs/specs/runtime-health-and-diagnostics-contract.md && test -f docs/specs/stress-soak-and-fault-campaign-contract.md && test -f docs/specs/save-compatibility-and-recovery-contract.md && test -f docs/specs/reproduction-and-internal-testing-contract.md && test -f docs/artifacts/heavy-internal-testing-readiness-artifact-contract.md", ["docs/specs/internal-testing-scale-and-performance-contract.md", "docs/artifacts/heavy-internal-testing-readiness-artifact-contract.md"]),
            Shard("scale-fixtures", "Five-region supported-scale fixture and capacity envelope.", "./eng/m035-probe.sh campaign", ["artifacts/readiness/M035/support-envelope.json"]),
            Shard("performance-baselines", "Versioned explicit baseline provenance and metric definitions.", "./eng/test-filter.sh PerformanceBudget && ./eng/performance-budget-smoke.sh", ["artifacts/readiness/M035/performance-baseline.json", "artifacts/readiness/M035/performance-budgets.json"]),
            Shard("performance-regression", "Comparable same-machine comparison with thresholds.", "./eng/performance-budget-smoke.sh", ["artifacts/readiness/M035/performance-comparison.json"]),
            Shard("runtime-invariants", "Bounded observer-only invariant monitoring.", "./eng/test-filter.sh RuntimeHealth && ./eng/runtime-health-smoke.sh", ["artifacts/readiness/M035/runtime-health-summary.json", "artifacts/readiness/M035/invariant-violations.jsonl"]),
            Shard("deadlock-livelock-starvation", "Actionable deadlock, livelock, and starvation diagnostics.", "./eng/test-filter.sh DeadlockDetection && ./eng/deadlock-detection-smoke.sh", ["artifacts/readiness/M035/deadlock-livelock-report.json"]),
            Shard("queue-reservation-health", "Queue ordering and activity/reservation health remain bounded.", "./eng/runtime-health-smoke.sh", ["artifacts/readiness/M035/runtime-health-summary.json"]),
            Shard("fault-command-persistence", "Test-only deterministic command and persistence faults.", "./eng/test-filter.sh FaultInjection && ./eng/fault-injection-smoke.sh", ["artifacts/readiness/M035/fault-campaign-report.json"]),
            Shard("fault-transition-execution", "Transition, delivery, routing, projection, and graphical fault boundaries.", "./eng/fault-injection-smoke.sh", ["artifacts/readiness/M035/fault-campaign-report.json"]),
            Shard("save-compatibility-matrix", "Explicit current/prior/forward save compatibility policy.", "./eng/test-filter.sh SaveCompatibility && ./eng/save-compatibility-smoke.sh", ["artifacts/readiness/M035/save-compatibility-matrix.json", "artifacts/readiness/M035/reference-save-manifest.json"]),
            Shard("save-corruption-recovery", "Atomic previous-good recovery and corruption diagnostics.", "./eng/test-filter.sh SaveRecovery && ./eng/save-recovery-smoke.sh", ["artifacts/readiness/M035/save-recovery-report.json"]),
            Shard("reproduction-bundles", "Portable bounded fault reproduction coverage.", "./eng/test-filter.sh ReproductionBundle && ./eng/reproduction-bundle-smoke.sh", ["artifacts/readiness/M035/reproduction-bundle-index.json"]),
            Shard("tester-session-workflow", "Tester session manifest and operational evidence.", "./eng/test-filter.sh InternalTestSession && ./eng/internal-test-session-smoke.sh", ["artifacts/readiness/M035/tester-session-index.json"]),
            Shard("population-entity-stress", "Population/entity scale campaign is nested and verified.", "./eng/m035-probe.sh campaign", ["artifacts/readiness/M035/campaigns/population-entity/verify.json"]),
            Shard("pathfinding-work-stress", "Pathfinding and work contention campaign is nested and verified.", "./eng/m035-probe.sh campaign", ["artifacts/readiness/M035/campaigns/pathfinding-work/verify.json"]),
            Shard("abstract-queue-stress", "Abstract queue/stale-trigger campaign is nested and verified.", "./eng/m035-probe.sh campaign", ["artifacts/readiness/M035/campaigns/abstract-queue/verify.json"]),
            Shard("fidelity-transition-churn", "One-thousand transition campaign is nested and verified.", "./eng/m035-probe.sh campaign", ["artifacts/readiness/M035/campaigns/fidelity-transition/verify.json"]),
            Shard("persistence-cycle-campaign", "Two-hundred-fifty save/load campaign is nested and verified.", "./eng/m035-probe.sh campaign", ["artifacts/readiness/M035/campaigns/persistence-cycle/verify.json"]),
            Shard("infrastructure-shortage-campaign", "Shortage, maintenance, and recovery campaign is nested and verified.", "./eng/m035-probe.sh campaign", ["artifacts/readiness/M035/campaigns/infrastructure-shortage/verify.json"]),
            Shard("headless-365-day-soak", "365-day headless soak is nested and verified.", "./eng/m035-probe.sh campaign", ["artifacts/readiness/M035/headless-soak-report.json", "artifacts/readiness/M035/campaigns/headless-365-day/verify.json"]),
            Shard("graphical-4-hour-soak", "Four-hour graphical soak records passed, failed, or explicit graphics skip.", "./eng/m035-graphical-soak-smoke.sh", ["artifacts/readiness/M035/graphical-soak-report.json"]),
            Shard("memory-throughput-trends", "Memory, queue, journal, artifact, projection, and throughput trend evidence.", "./eng/m035-probe.sh campaign", ["artifacts/readiness/M035/memory-throughput-trends.json"]),
            Shard("m031-m034-regression", "M031 through M034 aggregate receipts remain current.", "./eng/m031-smoke.sh --verify && ./eng/m032-smoke.sh --verify && ./eng/m033-smoke.sh --verify && ./eng/m034-smoke.sh --verify", ["artifacts/validation/m031-smoke/verify.json", "artifacts/validation/m034-smoke/verify.json"]),
            Shard("engine-regression", "Provider build and focused M035 readiness tests.", "./eng/build.sh && ./eng/test-filter.sh M035", ["src/Agentic2D.Simulation/M035InternalTestingReadiness.cs", "tests/unit/Agentic2D.Tests.Unit/M035InternalTestingReadinessTests.cs"]),
            Shard("asset-train-regression", "Existing asset provider evidence remains available.", "test -s artifacts/assets/M028/review-pack/review/asset-review-pack/manifest.json && test -s artifacts/assets/M029/workbench/asset-workbench-review-pack/manifest.json", ["artifacts/assets/M028/review-pack/review/asset-review-pack/manifest.json", "artifacts/assets/M029/workbench/asset-workbench-review-pack/manifest.json"]),
            Shard("readiness-report", "Readiness report and review pack are complete candidate evidence.", "./eng/test-filter.sh ReadinessGate && ./eng/m035-readiness-smoke.sh", ["artifacts/readiness/M035/readiness-report.json", "artifacts/readiness/M035/review-pack/review-manifest.json"]),
            Shard("human-review", "Blocking M035 readiness review is approved by a human.", "./eng/review-check.sh --milestone M035", ["artifacts/readiness/M035/review-pack/review-manifest.json"]),
            Shard("integrated", "M035 structural campaign, build, and readiness evidence.", "./eng/build.sh && ./eng/m035-probe.sh campaign && ./eng/m035-readiness-smoke.sh", ["artifacts/readiness/M035/m035-manifest.json", "artifacts/readiness/M035/readiness-report.json"])
        ]),
        new("m039-smoke", "resumable-sharded",
        [
            Shard("typed-component-authority", "Typed component authority, registration determinism, and lifecycle identity.", "internal:m039", ["artifacts/simulation/M039/typed-component-authority.json"], isInternal: true),
            Shard("semantic-command-atomicity", "Atomic semantic mutation, rollback observation, and event causality.", "internal:m039", ["artifacts/simulation/M039/semantic-command-atomicity.json"], isInternal: true),
            Shard("activities-and-reservations", "Activity transition authority and reservation invariants.", "internal:m039", ["artifacts/simulation/M039/activities-and-reservations.json"], isInternal: true),
            Shard("persistence-classification", "Executable classification policy and v2 compatibility boundary.", "internal:m039", ["artifacts/simulation/M039/persistence-classification.json"], isInternal: true),
            Shard("fresh-process-equivalence", "Separate producer and consumer OS process provenance.", "internal:m039", ["artifacts/simulation/M039/fresh-process-equivalence.json"], isInternal: true),
            Shard("current-consumer-regression", "M031, M032, and M033 bounded consumer regression.", "internal:m039", ["artifacts/simulation/M039/current-consumer-regression.json"], isInternal: true),
            Shard("evidence-integrity", "Observation-derived evidence and current receipt boundary.", "internal:m039", ["artifacts/simulation/M039/evidence-integrity.json"], isInternal: true)
        ]),
        new("m037-smoke", "resumable-sharded",
        [
            Shard("authority-normalization", "Active platform authority is consistent.", "internal:m037", ["artifacts/application/M037/authority-normalization-report.json"], isInternal: true),
            Shard("ui-tree-layout", "Retained controls and deterministic layout projection.", "internal:m037", ["artifacts/application/M037/ui-control-catalog.json", "artifacts/application/M037/ui-layout-cases.json"], isInternal: true),
            Shard("ui-focus-modal-text", "Focus, modal, pointer, and text-entry isolation.", "internal:m037", ["artifacts/application/M037/ui-focus-input-cases.json"], isInternal: true),
            Shard("application-foundation", "Explicit application lifecycle states.", "internal:m037", ["artifacts/application/M037/application-state-transitions.json"], isInternal: true),
            Shard("player-diagnostics-isolation", "Player and diagnostics dependency separation.", "internal:m037", ["artifacts/application/M037/client-dependency-report.json"], isInternal: true),
            Shard("main-pause-navigation", "Required player menus contain no diagnostics-only entries.", "internal:m037", ["artifacts/application/M037/main-menu-projection.json", "artifacts/application/M037/pause-menu-projection.json"], isInternal: true),
            Shard("new-game-tutorial-entry", "World configurations, seeds, titles, and tutorial entry.", "internal:m037", ["artifacts/application/M037/new-game-cases.json", "artifacts/application/M037/world-configuration-validation.json"], isInternal: true),
            Shard("save-catalog-naming", "Catalog metadata and locked autosave/manual naming.", "internal:m037", ["artifacts/application/M037/save-catalog.json", "artifacts/application/M037/save-naming-cases.json"], isInternal: true),
            Shard("manual-save-lifecycle", "Manual save and browser lifecycle operations.", "internal:m037", ["artifacts/application/M037/save-catalog.json"], isInternal: true),
            Shard("autosave-scheduling-retention", "Injected wall-clock scheduling and per-world retention.", "internal:m037", ["artifacts/application/M037/autosave-schedule-cases.json", "artifacts/application/M037/autosave-retention-cases.json"], isInternal: true),
            Shard("settings-validation-recovery", "Versioned settings validation and startup recovery.", "internal:m037", ["artifacts/application/M037/settings-validation-report.json", "artifacts/application/M037/safe-mode-report.json"], isInternal: true),
            Shard("display-preview-rollback", "Timed display preview and rollback state.", "internal:m037", ["artifacts/application/M037/display-preview-rollback-report.json"], isInternal: true),
            Shard("input-registry-defaults", "Explicit software-defined action registry.", "internal:m037", ["artifacts/application/M037/input-action-registry.json"], isInternal: true),
            Shard("input-rebinding-conflicts", "Capture, conflict, reset, and fallback behavior.", "internal:m037", ["artifacts/application/M037/input-binding-cases.json"], isInternal: true),
            Shard("input-context-isolation", "Context priority and text-entry suppression.", "internal:m037", ["artifacts/application/M037/input-context-cases.json"], isInternal: true),
            Shard("world-load-unload-resource-lifecycle", "Repeated world replacement disposes ownership.", "internal:m037", ["artifacts/application/M037/world-lifecycle-resource-report.json"], isInternal: true),
            Shard("linux-player-shell-structural", "Linux structural proof is recorded as inactive-platform debt during the Windows epoch.", "internal:m037", ["artifacts/application/M037/platform/linux/structural-report.json"], isInternal: true),
            Shard("headless-structural-proof", "Windows structural product-shell proof.", "internal:m037", ["artifacts/application/M037/platform/windows/structural-report.json"], isInternal: true),
            Shard("linux-player-shell-graphics", "Linux graphics proof is explicit and honest.", "internal:m037", ["artifacts/application/M037/platform/linux/graphical-report.json"], isInternal: true),
            Shard("windows-player-shell-graphics", "Windows graphics startup/navigation proof.", "internal:m037", ["artifacts/application/M037/platform/windows/graphical-report.json"], isInternal: true),
            Shard("affected-current-regression", "Current regression report and preserved M035/M036 boundaries.", "internal:m037", ["artifacts/application/M037/current-regression-report.json"], isInternal: true),
            Shard("review-pack", "Bounded review pack linked to structural evidence.", "internal:m037", ["artifacts/application/M037/review-pack/review-manifest.json", "artifacts/application/M037/review-pack/evidence-index.json"], isInternal: true),
            Shard("human-review", "Blocking M037 review is approved by the repository user.", "pwsh ./eng/review-check.ps1 --milestone M037", ["artifacts/application/M037/review-pack/review-manifest.json"]),
            Shard("integrated", "Integrated structural proof and completion audit candidate.", "internal:m037", ["artifacts/application/M037/m037-completion-audit.json", "artifacts/application/M037/diagnostics.json"], ["review-pack"], true),
            Shard("completion-audit", "Completion audit derives its terminal outcome from current review state and active-platform proof.", "internal:m037", ["artifacts/application/M037/m037-completion-audit.json", "artifacts/application/M037/diagnostics.json"], ["integrated"], true)
        ]),
        new("m038-smoke", "resumable-sharded",
        [
            Shard("policy-and-state", "Subjective-only applicability, v2 compatibility, and separate machine/human gates.", "internal:m038", ["artifacts/validation/m038-smoke/policy-and-state.json"], isInternal: true),
            Shard("simple-workbench", "Bounded executable review shape and exactly three primary controls.", "internal:m038", ["artifacts/validation/m038-smoke/simple-workbench.json"], isInternal: true),
            Shard("historical-regression", "Fixed M028/M031 negatives, M032/M034 positives, and M037 insufficient-experience regression.", "internal:m038", ["artifacts/validation/m038-smoke/historical-regression.json"], isInternal: true),
            Shard("active-platform-graphics", "Real active-Windows Raylib workbench draw and cleanup smoke.", "internal:m038", ["artifacts/validation/m038-smoke/active-platform-graphics.json", "artifacts/validation/m038-smoke/m038-workbench.png"], isInternal: true),
            Shard("review-readiness", "Fast current prerequisite and executable review registration.", "internal:m038", ["artifacts/validation/m038-smoke/review-readiness.json"], ["policy-and-state", "simple-workbench", "historical-regression", "active-platform-graphics"], true)
        ]),
        new("m036-smoke", "resumable-sharded",
        [
            Shard("guide-profile-v072", "Guide profile metadata and effective 0.7.2 values.", "internal:guide-profile-v072", ["artifacts/engineering/M036/guide-profile-migration-report.json"], isInternal: true),
            Shard("localized-execution-contract", "Localized ready and completion-audit execution semantics.", "internal:localized-execution-contract", ["artifacts/engineering/M036/guide-profile-migration-report.json"], ["guide-profile-v072"], true),
            Shard("engineering-host-portability", "Platform-neutral host, process, temp, and receipt semantics.", "internal:engineering-host-portability", ["artifacts/engineering/M036/receipt-environment-report.json"], ["localized-execution-contract"], true),
            Shard("launcher-inventory", "Evidence-backed inventory of every tracked Bash launcher.", "internal:launcher-inventory", ["artifacts/engineering/M036/launcher-inventory.json"], ["engineering-host-portability"], true),
            Shard("historical-shell-cleanup", "No deleted historical launcher remains referenced by active truth.", "internal:historical-shell-cleanup", ["artifacts/engineering/M036/launcher-cleanup-report.json"], ["launcher-inventory"], true),
            Shard("git-line-endings-and-paths", "Git normalization and portable durable paths.", "internal:git-line-endings-and-paths", ["artifacts/engineering/M036/git-normalization-report.json", "artifacts/engineering/M036/path-portability-report.json"], ["historical-shell-cleanup"], true),
            Shard("asset-home-platform-defaults", "Explicit override and native per-user defaults.", "internal:asset-home-platform-defaults", ["artifacts/engineering/M036/asset-home-platform-report.json"], ["git-line-endings-and-paths"], true),
            Shard("linux-core", "Linux Class A engineering proof.", "internal:linux-core", ["artifacts/engineering/M036/platform/linux/platform-verification.json"], ["asset-home-platform-defaults"], true),
            Shard("windows-core", "Windows Class A engineering proof under PowerShell 7.", "internal:windows-core", ["artifacts/engineering/M036/platform/windows/platform-verification.json"], ["asset-home-platform-defaults"], true),
            Shard("linux-graphics", "Linux Raylib development startup proof.", "internal:linux-graphics", ["artifacts/engineering/M036/platform/linux/graphics-development.json"], ["linux-core"], true),
            Shard("windows-graphics", "Windows Raylib development startup proof.", "internal:windows-graphics", ["artifacts/engineering/M036/platform/windows/graphics-development.json"], ["windows-core"], true),
            Shard("platform-semantic-comparison", "Cross-platform comparison with declared host differences only.", "internal:platform-semantic-comparison", ["artifacts/engineering/M036/platform-comparison.json"], ["linux-graphics", "windows-graphics"], true),
            Shard("current-regression", "Current regression and representative headless proof.", "internal:current-regression", ["src/Agentic2D.Engineering/EngineeringHost.cs"], ["platform-semantic-comparison"], true),
            Shard("documentation", "M036 current authority and launcher surface are indexed.", "internal:documentation", ["docs/engineering/cross-platform-development-and-launcher-policy.md", "docs/engineering/command-contract.md"], ["platform-semantic-comparison"], true),
            Shard("integrated", "M036 completion audit after all platform evidence.", "internal:integrated", ["artifacts/engineering/M036/m036-completion-audit.json", "artifacts/engineering/M036/diagnostics.json"], ["current-regression", "documentation"], true)
        ])
    ];

    private static ValidationShard Shard(string id, string description, string command, IReadOnlyList<string> evidence, IReadOnlyList<string>? dependsOn = null, bool isInternal = false) =>
        new(id, description, command, evidence, dependsOn ?? [], isInternal);
}

public static class Fingerprints
{
    public static string Repository(string root) => HashLines(EnumerateRelevantFiles(root, includeReview: false).Select(path => $"{Relative(root, path)}:{Path(path)}"));
    public static string Review(string root) => HashLines(EnumerateRelevantFiles(root, includeReview: false).Select(path => $"{Relative(root, path)}:{Path(path)}"));
    public static string Suite(ValidationSuite suite, string root) => Hash(JsonSerializer.Serialize(suite) + "\nplatform-epoch:" + PlatformEpochFingerprint(root));
    public static string Command(ValidationShard shard) => Hash($"{shard.Id}\n{shard.Command}\n{string.Join("\n", shard.Evidence)}\n{string.Join("\n", shard.DependsOn)}");
    public static string Input(string root, ValidationShard shard) => HashLines(shard.Evidence.Where(path => !path.StartsWith("artifacts/", StringComparison.Ordinal)).Select(path => $"{path}:{Path(System.IO.Path.Combine(root, path))}").Append("platform-epoch:" + PlatformEpochFingerprint(root)));
    public static string Result(IReadOnlyList<ArtifactFingerprint> artifacts, string root) => HashLines(artifacts.OrderBy(artifact => artifact.Path, StringComparer.Ordinal).Select(artifact => $"{artifact.Path}:{artifact.Fingerprint}"));
    public static string Path(string path)
    {
        if (File.Exists(path)) return Hash(File.ReadAllBytes(path));
        if (Directory.Exists(path)) return HashLines(Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).OrderBy(value => value, StringComparer.Ordinal).Select(file => $"{Relative(path, file)}:{Hash(File.ReadAllBytes(file))}"));
        return "missing";
    }

    private static IEnumerable<string> EnumerateRelevantFiles(string root, bool includeReview) => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
        .Where(path => !IsExcluded(Relative(root, path), includeReview))
        .OrderBy(path => path, StringComparer.Ordinal);
    private static bool IsExcluded(string path, bool includeReview) =>
        path.StartsWith(".git/", StringComparison.Ordinal) || path.StartsWith("artifacts/", StringComparison.Ordinal) || path.Contains("/artifacts/", StringComparison.Ordinal) ||
        path.Contains("/bin/", StringComparison.Ordinal) || path.Contains("/obj/", StringComparison.Ordinal) ||
        path.StartsWith(".guide-sync/", StringComparison.Ordinal) || (!includeReview && path.StartsWith(".review/", StringComparison.Ordinal)) ||
        path.EndsWith(".zip", StringComparison.Ordinal) || path.StartsWith("game/assets/generated/", StringComparison.Ordinal) || path.Contains("/game-content/generated/", StringComparison.Ordinal);
    private static string Relative(string root, string path) => System.IO.Path.GetRelativePath(root, path).Replace('\\', '/');
    private static string HashLines(IEnumerable<string> lines) => Hash(string.Join("\n", lines));
    private static string Hash(string value) => Hash(Encoding.UTF8.GetBytes(value));
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static string PlatformEpochFingerprint(string root) => Path(System.IO.Path.Combine(root, "eng", "platform-verification.json"));
}


public static class ReceiptStore
{
    public static void Invalidate(string path)
    {
        if (File.Exists(path)) File.Delete(path);
    }

    public static void WriteAtomic<T>(string path, T value, JsonSerializerOptions json)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var temporary = Path.Combine(Path.GetDirectoryName(path)!, "." + Path.GetFileName(path) + "." + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.WriteThrough))
            using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
            {
                JsonSerializer.Serialize(writer, value, json);
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            IOException? lastIo = null;
            UnauthorizedAccessException? lastAccess = null;
            for (var attempt = 0; attempt < 20; attempt++)
            {
                try
                {
                    File.Move(temporary, path, overwrite: true);
                    lastIo = null;
                    lastAccess = null;
                    break;
                }
                catch (IOException exception)
                {
                    lastIo = exception;
                    Thread.Sleep(10);
                }
                catch (UnauthorizedAccessException exception)
                {
                    lastAccess = exception;
                    Thread.Sleep(10);
                }
            }

            if (lastAccess is not null) throw lastAccess;
            if (lastIo is not null) throw lastIo;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static bool TryRead(string path, JsonSerializerOptions json, out ValidationReceipt? receipt, out string error)
    {
        receipt = null;
        error = string.Empty;
        try
        {
            if (!File.Exists(path))
            {
                error = "receipt is missing";
                return false;
            }

            receipt = JsonSerializer.Deserialize<ValidationReceipt>(File.ReadAllText(path), json);
            if (receipt is null)
            {
                error = "receipt is malformed";
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            error = "receipt is malformed";
            return false;
        }
    }
}

public static class ProcessRunner
{
    public static async Task<int> RunAsync(string workingDirectory, string command, TextWriter stdout, TextWriter stderr)
    {
        var start = BuildStartInfo(command, workingDirectory);
        using var process = Process.Start(start) ?? throw new EngineeringException($"unable to start shard command: {command}");
        var outTask = PumpAsync(process.StandardOutput, stdout);
        var errTask = PumpAsync(process.StandardError, stderr);
        await Task.WhenAll(process.WaitForExitAsync(), outTask, errTask);
        return process.ExitCode;
    }

    public static ProcessStartInfo BuildStartInfo(string command, string workingDirectory)
    {
        var start = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("pwsh", ["-NoLogo", "-NoProfile", "-NonInteractive", "-Command", command])
            : new ProcessStartInfo("bash", ["-lc", command]);
        start.WorkingDirectory = workingDirectory;
        start.RedirectStandardOutput = true;
        start.RedirectStandardError = true;
        start.UseShellExecute = false;
        return start;
    }

    private static async Task PumpAsync(StreamReader reader, TextWriter writer)
    {
        while (await reader.ReadLineAsync() is { } line) await writer.WriteLineAsync(line);
    }
}

public sealed class EngineeringException(string message) : Exception(message);

public sealed record ValidationSuite(string Id, string ExecutionMode, IReadOnlyList<ValidationShard> Shards);
public sealed record ValidationShard(string Id, string Description, string Command, IReadOnlyList<string> Evidence, IReadOnlyList<string> DependsOn, bool IsInternal);
public sealed record ValidationPlan(string Schema, string SuiteId, string ExecutionMode, string SuiteFingerprint, string RepositoryFingerprint, IReadOnlyList<PlanShard> RequiredShards, string VerifierCommand, IReadOnlyList<string> ArtifactPaths);
public sealed record PlanShard(string Id, string Description, string Command, string ReceiptPath, IReadOnlyList<string> DependsOn, IReadOnlyList<string> Evidence);
public sealed record ArtifactFingerprint(string Path, string Fingerprint);
public sealed record CompletionMetadata(DateTimeOffset StartedAtUtc, DateTimeOffset CompletedAtUtc, string Platform, EngineeringEnvironment? Environment = null);
public sealed record ValidationReceipt(string Schema, string SuiteId, string ShardId, string Status, string SuiteFingerprint, string RepositoryFingerprint, string CommandFingerprint, string InputFingerprint, string ResultFingerprint, string Command, IReadOnlyList<string> EvidencePaths, IReadOnlyList<ArtifactFingerprint> Artifacts, CompletionMetadata Completion, IReadOnlyList<string> Diagnostics);
public sealed record ReviewListOptions(string? Milestone, string? State, string? Status);
public sealed record ReviewAlias(int Alias, string ReviewId);
public sealed record ReviewAliasMap(string Schema, string ContextFingerprint, ReviewListOptions Options, IReadOnlyList<ReviewAlias> Aliases, DateTimeOffset GeneratedAt);
public sealed record ReviewDecision(string Decision, string Reviewer, string Notes, IReadOnlyList<string> Evidence, string Revision, string Fingerprint, DateTimeOffset RecordedAt, bool RecordCorrection);
public sealed record ReviewState(
    string Schema,
    string Id,
    string OwningMilestone,
    string OwningMilestonePath,
    string Subject,
    IReadOnlyList<string> Classes,
    string Level,
    string ReviewerRole,
    string Status,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<string> AcceptanceCriteria,
    IReadOnlyList<string> AcceptableDecisions,
    string WaiverPolicy,
    string Decision,
    string ReviewedRevision,
    string ReviewedFingerprint,
    IReadOnlyList<string> Conditions,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ReviewDecision> DecisionHistory,
    string CorrectsReviewId,
    string Path);
