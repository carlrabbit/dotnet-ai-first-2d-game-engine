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
            "list" when args.Length == 1 => host.ListReviews(stdout),
            "check" when args.Length == 1 => host.CheckReviews(stderr) ? 0 : 1,
            "request" => await host.CreateReviewRequestAsync(args[1..], stdout),
            "record" => await host.RecordReviewAsync(args[1..], stdout),
            _ => Usage(stderr)
        };
    }

    private static int Usage(TextWriter stderr)
    {
        stderr.WriteLine("usage: engineering suite <id> [--list|--plan-json|--shard <id>|--verify] | engineering review <list|check|request|record> | engineering performance <smoke|capture|compare|report>");
        return 2;
    }
}

public sealed class EngineeringHost
{
    private const string PlanSchema = "agentic2d.engineering.validation-plan.v1";
    private const string ReceiptSchema = "agentic2d.engineering.validation-receipt.v1";
    private const string ReviewSchema = "agentic2d.engineering.review.v1";
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

        return success;
    }

    public int ListReviews(TextWriter stdout)
    {
        foreach (var review in ReadReviews())
        {
            stdout.WriteLine($"{review.Id}\t{review.Status}\t{review.Level}\t{review.Subject}\t{review.Path}");
        }

        return 0;
    }

    public bool CheckReviews(TextWriter diagnostics)
    {
        var reviews = ReadReviews();
        var success = true;
        foreach (var review in reviews.Where(review => review.Level is "required" or "blocking"))
        {
            if (review.Status != "approved")
            {
                diagnostics.WriteLine($"error: required review '{review.Id}' is {review.Status}");
                success = false;
                continue;
            }

            if (string.IsNullOrWhiteSpace(review.ReviewedFingerprint) || review.ReviewedFingerprint != Fingerprints.Review(root))
            {
                diagnostics.WriteLine($"error: required review '{review.Id}' is stale");
                success = false;
            }

            if (review.Evidence.Count == 0 || review.Evidence.Any(path => !File.Exists(Absolute(path)) && !Directory.Exists(Absolute(path))))
            {
                diagnostics.WriteLine($"error: required review '{review.Id}' has missing evidence");
                success = false;
            }
        }

        if (!reviews.Any(review => review.Level is "required" or "blocking"))
        {
            diagnostics.WriteLine("error: no required or blocking review record exists");
            return false;
        }

        if (success)
        {
            diagnostics.WriteLine("review-check: passed");
        }

        return success;
    }

    public async Task<int> CreateReviewRequestAsync(string[] args, TextWriter stdout)
    {
        var options = ParseOptions(args);
        var id = Required(options, "--id");
        var review = new ReviewState(
            ReviewSchema,
            id,
            Required(options, "--subject"),
            Required(options, "--class"),
            Required(options, "--level"),
            Required(options, "--source"),
            options.GetValueOrDefault("--reviewer", "human reviewer"),
            "pending",
            Split(options.GetValueOrDefault("--evidence", string.Empty)),
            string.Empty,
            string.Empty,
            Split(options.GetValueOrDefault("--triggers", "source,acceptance criteria,evidence,fingerprint")),
            Path.Combine(".review", "pending", id + ".json"));
        WriteReview(review, Path.Combine(".review", "pending", id + ".json"));
        await stdout.WriteLineAsync($"review request created: {review.Path}");
        return 0;
    }

    public async Task<int> RecordReviewAsync(string[] args, TextWriter stdout)
    {
        var options = ParseOptions(args);
        var id = Required(options, "--id");
        var pendingPath = Path.Combine(".review", "pending", id + ".json");
        if (!TryReadReview(Absolute(pendingPath), out var pending, out var error))
        {
            throw new EngineeringException(error);
        }

        var status = options.GetValueOrDefault("--status", "approved");
        if (status is not ("approved" or "changes-requested" or "rejected" or "waived"))
        {
            throw new EngineeringException($"invalid review status: {status}");
        }

        var evidence = options.TryGetValue("--evidence", out var suppliedEvidence) ? Split(suppliedEvidence) : pending!.Evidence;
        var recordPath = Path.Combine(".review", "records", id + ".json");
        var record = pending! with
        {
            Status = status,
            ReviewerRole = options.GetValueOrDefault("--reviewer", pending.ReviewerRole),
            Evidence = evidence,
            Decision = Required(options, "--decision"),
            ReviewedFingerprint = Fingerprints.Review(root),
            Path = recordPath
        };
        WriteReview(record, recordPath);
        File.Delete(Absolute(pendingPath));
        await stdout.WriteLineAsync($"review record written: {recordPath}");
        return 0;
    }

    private async Task<int> RunInternalShardAsync(ValidationSuite suite, ValidationShard shard, TextWriter diagnostics)
    {
        if (suite.Id == "m023-smoke" && shard.Id == "guide-v051")
        {
            return CheckGuideV051(diagnostics);
        }

        if (suite.Id != "guide-migration-v050")
        {
            throw new EngineeringException($"unsupported internal shard: {suite.Id}/{shard.Id}");
        }

        return shard.Id switch
        {
            "profile-and-docs" => CheckFiles([".guide-profile.json", "docs/engineering/constrained-validation-execution.md", "docs/engineering/human-review-workflow.md"], diagnostics),
            "platform-and-leakage" => CheckPlatformAndLeakage(diagnostics),
            _ => throw new EngineeringException($"unsupported internal shard: {shard.Id}")
        };
    }

    private int CheckFiles(IEnumerable<string> files, TextWriter diagnostics)
    {
        var missing = files.Where(file => !File.Exists(Absolute(file))).ToArray();
        if (missing.Length > 0)
        {
            diagnostics.WriteLine($"error: missing required migration inputs: {string.Join(", ", missing)}");
            return 1;
        }

        using var profile = JsonDocument.Parse(File.ReadAllText(Absolute(".guide-profile.json")));
        if (profile.RootElement.GetProperty("guideSystemVersion").GetString() != "0.5.0")
        {
            diagnostics.WriteLine("error: guide profile does not declare version 0.5.0");
            return 1;
        }

        return 0;
    }

    private int CheckGuideV051(TextWriter diagnostics)
    {
        var required = new[] { ".guide-profile.json", "docs/milestones/MILESTONE-023-lightweight-runtime-metrics-comparative-performance-checks-and-milestone-performance-reporting.md" };
        if (required.Any(path => !File.Exists(Absolute(path))))
        {
            diagnostics.WriteLine("error: M023 guide v0.5.1 corrective-assessment authority is missing");
            return 1;
        }
        using var profile = JsonDocument.Parse(File.ReadAllText(Absolute(".guide-profile.json")));
        var root = profile.RootElement;
        if (root.GetProperty("guideSystemVersion").GetString() != "0.5.1" || root.GetProperty("repositoryRole").GetString() != "capability-provider")
        {
            diagnostics.WriteLine("error: guide profile does not preserve the v0.5.1 capability-provider profile");
            return 1;
        }
        var adoption = root.GetProperty("adoption");
        if (adoption.GetProperty("validationExecutionModel").GetString() != "direct-or-resumable-sharded" || adoption.GetProperty("engineeringCommandModel").GetString() != "thin-launchers-over-tested-dotnet-host")
        {
            diagnostics.WriteLine("error: guide profile does not preserve validation/engineering command model");
            return 1;
        }
        return 0;
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

    private IEnumerable<ReviewState> ReadReviews()
    {
        foreach (var directory in new[] { ".review/pending", ".review/records" })
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
            review = JsonSerializer.Deserialize<ReviewState>(File.ReadAllText(path), json);
            if (review is null || review.Schema != ReviewSchema || string.IsNullOrWhiteSpace(review.Id) || review.Evidence is null)
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

    private string ReceiptPath(ValidationSuite suite, ValidationShard shard) => Path.Combine("artifacts", "validation", suite.Id, shard.Id + ".json").Replace('\\', '/');
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
        new("guide-migration-v050", "resumable-sharded",
        [
            Shard("profile-and-docs", "Profile and localized migration authority.", "internal:profile-and-docs", [".guide-profile.json", "docs/engineering/constrained-validation-execution.md"], isInternal: true),
            Shard("engineering-host-tests", "Engineering host unit tests.", "./eng/test.sh", ["src/Agentic2D.Engineering/EngineeringHost.cs"]),
            Shard("m019-suite", "Current M019 receipt verification.", "./eng/m019-smoke.sh --verify", ["artifacts/validation/m019-smoke/replay.json"]),
            Shard("m020-suite", "Current M020 receipt verification.", "./eng/m020-smoke.sh --verify", ["artifacts/validation/m020-smoke/review.json"]),
            Shard("m021-suite", "Current M021 receipt verification.", "./eng/m021-smoke.sh --verify", ["artifacts/validation/m021-smoke/review.json"]),
            Shard("review-workflow", "Required migration review is current.", "./eng/review-check.sh", [".review/records/migration-guide-v050.json"]),
            Shard("platform-and-leakage", "Declared Linux/Bash support and authority isolation.", "internal:platform-and-leakage", ["AGENTS.md", "docs/ENGINEERING.md"], isInternal: true)
        ]),
        new("m023-smoke", "resumable-sharded",
        [
            Shard("metrics-contracts", "Finite metric vocabulary and bounded collector tests.", "./eng/test-filter.sh Metrics", ["src/Agentic2D.Metrics/RuntimeMetrics.cs", "tests/unit/Agentic2D.Tests.Unit/MetricsTests.cs"]),
            Shard("runtime-instrumentation", "Runtime metrics leave semantic output unchanged.", "./eng/test-filter.sh Metrics", ["src/Agentic2D.Engine/MinimalRuntime.cs", "tests/unit/Agentic2D.Tests.Unit/MetricsTests.cs"]),
            Shard("metrics-artifacts", "Summary and bounded per-tick artifacts.", "./eng/metrics-artifacts-smoke.sh", ["artifacts/smoke/m023-metrics/metrics-summary.json", "artifacts/smoke/m023-metrics/metrics-ticks.jsonl"]),
            Shard("comparative-workloads", "Reference workload capture.", "./eng/perf-smoke.sh", ["artifacts/performance/smoke/performance-capture.json"]),
            Shard("performance-report", "Advisory comparison and report generation.", "./eng/perf-report-smoke.sh", ["artifacts/performance/m023/performance-report.json", "artifacts/performance/m023/performance-report.md"]),
            Shard("integrated", "Direct build, test, and product integration checks.", "./eng/build.sh && ./eng/test.sh && ./eng/cli-smoke.sh && ./eng/product-validate.sh", ["artifacts/cli/runtime-smoke/result.json", "artifacts/cli/validate/result.json"]),
            Shard("guide-v051", "v0.5.1 corrective assessment profile check.", "internal:guide-v051", [".guide-profile.json", "docs/milestones/MILESTONE-023-lightweight-runtime-metrics-comparative-performance-checks-and-milestone-performance-reporting.md"], isInternal: true)
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
            Shard("human-review", "Blocking human review is current and approved.", "./eng/review-check.sh", [".review/records/review.m025.signal-passage-playable-vertical-slice.json"]),
            Shard("integrated", "Provider build and consumer journey integration.", "./eng/build.sh && ./eng/signal-passage-smoke.sh", ["consumers/signal-passage/artifacts/journey/complete-journey.json"])
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
        path.StartsWith(".git/", StringComparison.Ordinal) || path.StartsWith("artifacts/", StringComparison.Ordinal) ||
        path.Contains("/bin/", StringComparison.Ordinal) || path.Contains("/obj/", StringComparison.Ordinal) ||
        path.StartsWith(".guide-sync/", StringComparison.Ordinal) || (!includeReview && path.StartsWith(".review/", StringComparison.Ordinal)) ||
        path.EndsWith(".zip", StringComparison.Ordinal) || path.StartsWith("game/assets/generated/", StringComparison.Ordinal);
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
public sealed record ReviewState(string Schema, string Id, string Subject, string Class, string Level, string Source, string ReviewerRole, string Status, IReadOnlyList<string> Evidence, string Decision, string ReviewedFingerprint, IReadOnlyList<string> ReReviewTriggers, string Path);
