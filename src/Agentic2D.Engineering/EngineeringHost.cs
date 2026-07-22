using System.Diagnostics;
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
        stderr.WriteLine("usage: engineering suite <id> [--list|--plan-json|--shard <id>|--verify] | engineering review list [--milestone <id>] [--state <active|historical>] [--status <status>] | engineering review show <review-id-or-alias> | engineering review request --milestone <id> ... | engineering review record <review-id-or-alias> <decision> ... | engineering review reopen <review-id-or-alias> --reason <reason> [--correct-record] | engineering review check --milestone <id> | engineering performance <smoke|capture|compare|report>");
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

    public ValidationSuite GetSuite(string id) => suites.TryGetValue(id, out var suite)
        ? suite
        : throw new EngineeringException($"unknown validation suite: {id}");

    public string SerializePlan(ValidationSuite suite)
    {
        var repository = Fingerprints.Repository(root);
        var suiteFingerprint = Fingerprints.Suite(suite);
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
            $"./eng/{suite.Id}.sh --verify",
            suite.Shards.SelectMany(shard => shard.Evidence).Distinct(StringComparer.Ordinal).ToArray());
        return JsonSerializer.Serialize(plan, json);
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
        var suiteFingerprint = Fingerprints.Suite(suite);
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
            new CompletionMetadata(started, DateTimeOffset.UtcNow, "linux-bash"),
            []);
        ReceiptStore.WriteAtomic(receiptPath, receipt, json);
        await stdout.WriteLineAsync($"{suite.Id}/{shard.Id}: passed; receipt {ReceiptPath(suite, shard)}");
        return 0;
    }

    public bool Verify(ValidationSuite suite, TextWriter diagnostics)
    {
        var expectedSuite = Fingerprints.Suite(suite);
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

        if (suite.Id == "m031-smoke")
        {
            var path = Absolute(Path.Combine("artifacts", "validation", suite.Id, "verify.json"));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(new { schema = "agentic2d.simulation-foundation-verification.v1", suite = suite.Id, status = success ? "passed" : "failed", receiptCount = suite.Shards.Count }, json));
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
        if (suite.Id != "m022-smoke")
        {
            throw new EngineeringException($"unsupported internal shard: {suite.Id}/{shard.Id}");
        }

        return shard.Id switch
        {
            "platform-and-leakage" => CheckPlatformAndLeakage(diagnostics),
            _ => throw new EngineeringException($"unsupported internal shard: {shard.Id}")
        };
    }

    private int CheckPlatformAndLeakage(TextWriter diagnostics)
    {
        if (!OperatingSystem.IsLinux())
        {
            diagnostics.WriteLine("error: this migration only declares and tests Linux/Bash support");
            return 1;
        }

        var forbidden = new[] { "docs/research", "prompt template", "external guide" };
        var paths = new[] { "AGENTS.md", "docs/ENGINEERING.md", "docs/engineering/command-contract.md" };
        foreach (var path in paths)
        {
            var text = File.ReadAllText(Absolute(path));
            if (forbidden.Any(value => text.Contains(value, StringComparison.OrdinalIgnoreCase) && !text.Contains("Do not", StringComparison.OrdinalIgnoreCase)))
            {
                diagnostics.WriteLine($"error: active engineering document may treat guide material as authority: {path}");
                return 1;
            }
        }

        return 0;
    }

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
            && receipt.SuiteFingerprint == Fingerprints.Suite(suite)
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

    private string ReceiptPath(ValidationSuite suite, ValidationShard shard) => (suite.Id == "m031-smoke"
        ? Path.Combine("artifacts", "validation", suite.Id, "receipts", shard.Id + ".json")
        : Path.Combine("artifacts", "validation", suite.Id, shard.Id + ".json")).Replace('\\', '/');
    private string Absolute(string relative) => Path.Combine(root, relative);

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
        new("m022-smoke", "resumable-sharded",
        [
            Shard("engineering-host-tests", "Engineering host plan and receipt tests.", "./eng/test-filter.sh EngineeringHost", ["src/Agentic2D.Engineering/EngineeringHost.cs", "tests/unit/Agentic2D.Tests.Unit/EngineeringHostTests.cs"]),
            Shard("m019-suite", "Current M019 receipt verification.", "./eng/m019-smoke.sh --verify", ["artifacts/validation/m019-smoke/replay.json"]),
            Shard("m020-suite", "Current M020 receipt verification.", "./eng/m020-smoke.sh --verify", ["artifacts/validation/m020-smoke/review.json"]),
            Shard("m021-suite", "Current M021 receipt verification.", "./eng/m021-smoke.sh --verify", ["artifacts/validation/m021-smoke/review.json"]),
            Shard("review-workflow", "Historical M022 migration review is present.", "./eng/review-check.sh --milestone M022", [".review/records/migration-guide-v050.json"]),
            Shard("platform-and-leakage", "Declared Linux/Bash support and authority isolation.", "internal:platform-and-leakage", ["AGENTS.md", "docs/ENGINEERING.md"], isInternal: true)
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
        new("m024-smoke", "resumable-sharded",
        [
            Shard("export-contracts", "Export manifest, inventory, and validation contracts.", "./eng/export-linux-smoke.sh", ["artifacts/smoke/m024-export/validate/export-validation.json"]),
            Shard("game-host", "Dedicated standalone host builds and exposes bounded options.", "./eng/test-filter.sh GameHost", ["src/Agentic2D.GameHost/Program.cs"]),
            Shard("export-build", "Self-contained Linux export assembly.", "./eng/export-linux-smoke.sh", ["artifacts/smoke/m024-export/game/agentic2d.export.json", "artifacts/smoke/m024-export/game/export-files.json"]),
            Shard("isolated-headless-launch", "Direct executable launch outside source tree.", "./eng/export-isolated-launch-smoke.sh", ["artifacts/smoke/m024-isolated-launch/isolated-launch-result.json"]),
            Shard("semantic-equivalence", "Development/export semantic comparison.", "./eng/export-equivalence-smoke.sh", ["artifacts/smoke/m024-equivalence/development-export-equivalence.json"]),
            Shard("performance-report", "Same-machine export performance report.", "./eng/export-performance-smoke.sh", ["artifacts/performance/M024/performance-report.json", "artifacts/performance/M024/performance-report.md"]),
            Shard("graphical-review", "Optional graphical-session review or explicit skip.", "./eng/export-graphical-smoke.sh", ["artifacts/smoke/m024-graphical/graphical-review.json"]),
            Shard("integrated", "Build and direct export integration.", "./eng/build.sh && ./eng/export-isolated-launch-smoke.sh", ["artifacts/smoke/m024-isolated-launch/isolated-launch-run-manifest.json"])
        ]),
        new("m025-smoke", "resumable-sharded",
        [
            Shard("workspace-isolation", "Relocated consumer workspace validates, builds, and runs.", "./eng/signal-passage-isolation.sh", ["artifacts/signal-passage/isolation/workspace-validation.json", "artifacts/signal-passage/isolation/run-manifest.json"]),
            Shard("geometric-presentation", "Consumer geometric visuals project into structural render evidence.", "./eng/signal-passage-smoke.sh", ["consumers/signal-passage/artifacts/runs/geometry/render/render-commands.jsonl"]),
            Shard("sound-synthesis", "Offline cue definitions validate and regenerate PCM WAV assets.", "./eng/signal-passage-validate.sh", ["consumers/signal-passage/artifacts/sound-validation/sound-synthesis-result.json", "consumers/signal-passage/game-content/generated/sounds/objective-completed.wav"]),
            Shard("consumer-gameplay", "Consumer-owned objective journey completes.", "./eng/signal-passage-smoke.sh", ["consumers/signal-passage/artifacts/journey/complete-journey.json"]),
            Shard("save-resume", "Save/resume state contains no replayed transient feedback.", "./eng/signal-passage-smoke.sh", ["consumers/signal-passage/artifacts/journey/save.json"]),
            Shard("linux-export", "Consumer workspace exports and runs through the standalone Linux host.", "./eng/signal-passage-export.sh", ["artifacts/signal-passage/export/game/agentic2d.export.json", "artifacts/signal-passage/export/run/run-manifest.json"]),
            Shard("performance-report", "M025 advisory performance report is present.", "./eng/signal-passage-performance.sh", ["artifacts/performance/M025/performance-report.json", "artifacts/performance/M025/performance-report.md"]),
            Shard("extension-discovery", "Consumer extension classifications are complete.", "test -f consumers/signal-passage/consumer-extension-report.json && test -f consumers/signal-passage/consumer-extension-report.md", ["consumers/signal-passage/consumer-extension-report.json", "consumers/signal-passage/consumer-extension-report.md"]),
            Shard("human-review", "Historical M025 review is present and approved.", "./eng/review-check.sh --milestone M025", [".review/records/review.m025.signal-passage-playable-vertical-slice.json"]),
            Shard("integrated", "Provider build and consumer journey integration.", "./eng/build.sh && ./eng/signal-passage-smoke.sh", ["consumers/signal-passage/artifacts/journey/complete-journey.json"])
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
        ])
    ];

    private static ValidationShard Shard(string id, string description, string command, IReadOnlyList<string> evidence, IReadOnlyList<string>? dependsOn = null, bool isInternal = false) =>
        new(id, description, command, evidence, dependsOn ?? [], isInternal);
}

public static class Fingerprints
{
    public static string Repository(string root) => HashLines(EnumerateRelevantFiles(root, includeReview: false).Select(path => $"{Relative(root, path)}:{Path(path)}"));
    public static string Review(string root) => HashLines(EnumerateRelevantFiles(root, includeReview: false).Select(path => $"{Relative(root, path)}:{Path(path)}"));
    public static string Suite(ValidationSuite suite) => Hash(JsonSerializer.Serialize(suite));
    public static string Command(ValidationShard shard) => Hash($"{shard.Id}\n{shard.Command}\n{string.Join("\n", shard.Evidence)}\n{string.Join("\n", shard.DependsOn)}");
    public static string Input(string root, ValidationShard shard) => HashLines(shard.Evidence.Where(path => !path.StartsWith("artifacts/", StringComparison.Ordinal)).Select(path => $"{path}:{Path(System.IO.Path.Combine(root, path))}"));
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

            File.Move(temporary, path, overwrite: true);
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
        var start = new ProcessStartInfo("bash", ["-lc", command])
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(start) ?? throw new EngineeringException($"unable to start shard command: {command}");
        var outTask = PumpAsync(process.StandardOutput, stdout);
        var errTask = PumpAsync(process.StandardError, stderr);
        await Task.WhenAll(process.WaitForExitAsync(), outTask, errTask);
        return process.ExitCode;
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
public sealed record CompletionMetadata(DateTimeOffset StartedAtUtc, DateTimeOffset CompletedAtUtc, string Platform);
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
