using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Agentic2D.Validation;

public sealed class AssetReviewApplier
{
    private static readonly HashSet<string> SupportedStates = new(StringComparer.Ordinal)
    {
        "approved",
        "rejected",
        "needs-revision",
        "clear",
    };

    private static readonly HashSet<string> SupportedValues = new(StringComparer.Ordinal)
    {
        "walkable",
        "blocked",
        "collision",
        "navigation-cost",
        "damage",
        "interactable",
        "progression-blocker",
        "spawnable",
    };

    public AssetReviewApplyRun Apply(string decisionPath, bool dryRun)
    {
        var resolvedDecisionPath = ResolveDecisionPath(decisionPath);
        if (!File.Exists(resolvedDecisionPath))
        {
            var diagnostics = new[]
            {
                new ContentValidationDiagnostic("REVIEW0001", ContentDiagnosticSeverity.Error, $"Decision file was not found: {decisionPath}", decisionPath),
            };
            return AssetReviewApplyRun.From(decisionPath, string.Empty, string.Empty, dryRun, string.Empty, string.Empty, [], null, ContentValidationStatus.Failed, 1, diagnostics);
        }

        JsonNode? metadataNode = null;
        AssetReviewDecisionSource? decisionSource;
        try
        {
            decisionSource = JsonSerializer.Deserialize<AssetReviewDecisionSource>(File.ReadAllText(resolvedDecisionPath), ContentValidationJson.Options);
        }
        catch (JsonException exception)
        {
            var diagnostics = new[]
            {
                new ContentValidationDiagnostic("REVIEW0001", ContentDiagnosticSeverity.Error, $"Decision JSON is malformed: {exception.Message}", Normalize(decisionPath), "json"),
            };
            return AssetReviewApplyRun.From(Normalize(decisionPath), string.Empty, string.Empty, dryRun, string.Empty, string.Empty, [], null, ContentValidationStatus.Failed, 1, diagnostics);
        }

        if (decisionSource is null)
        {
            var diagnostics = new[]
            {
                new ContentValidationDiagnostic("REVIEW0001", ContentDiagnosticSeverity.Error, "Decision JSON must contain an object.", Normalize(decisionPath), "$"),
            };
            return AssetReviewApplyRun.From(Normalize(decisionPath), string.Empty, string.Empty, dryRun, string.Empty, string.Empty, [], null, ContentValidationStatus.Failed, 1, diagnostics);
        }

        var diagnosticsList = ValidateDecisionSource(decisionSource, Normalize(decisionPath));
        var metadataPath = decisionSource.MetadataPath ?? string.Empty;
        if (diagnosticsList.Any(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error))
        {
            return AssetReviewApplyRun.From(Normalize(decisionPath), decisionSource.AssetId ?? string.Empty, metadataPath, dryRun, decisionSource.ExpectedSourceFingerprint ?? string.Empty, string.Empty, [], null, ContentValidationStatus.Failed, 1, diagnosticsList);
        }

        var resolvedMetadata = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), metadataPath);
        if (!File.Exists(resolvedMetadata))
        {
            diagnosticsList.Add(new ContentValidationDiagnostic("REVIEW0002", ContentDiagnosticSeverity.Error, $"Target metadata file was not found: {metadataPath}", metadataPath));
            return AssetReviewApplyRun.From(Normalize(decisionPath), decisionSource.AssetId ?? string.Empty, metadataPath, dryRun, decisionSource.ExpectedSourceFingerprint ?? string.Empty, string.Empty, [], null, ContentValidationStatus.Failed, 1, diagnosticsList);
        }

        var sourceBytes = File.ReadAllBytes(resolvedMetadata);
        var actualFingerprint = AssetFingerprint.FromBytes(sourceBytes);
        if (!StringComparer.Ordinal.Equals(decisionSource.ExpectedSourceFingerprint, actualFingerprint))
        {
            diagnosticsList.Add(new ContentValidationDiagnostic("REVIEW0003", ContentDiagnosticSeverity.Error, "Source fingerprint mismatch prevented mutation.", metadataPath, "expectedSourceFingerprint", decisionSource.Id));
            return AssetReviewApplyRun.From(Normalize(decisionPath), decisionSource.AssetId ?? string.Empty, metadataPath, dryRun, decisionSource.ExpectedSourceFingerprint ?? string.Empty, actualFingerprint, [], null, ContentValidationStatus.Failed, 1, diagnosticsList);
        }

        var metadataText = File.ReadAllText(resolvedMetadata);
        metadataNode = JsonNode.Parse(metadataText);
        if (metadataNode is not JsonObject metadataObject)
        {
            diagnosticsList.Add(new ContentValidationDiagnostic("REVIEW0002", ContentDiagnosticSeverity.Error, "Target metadata must contain a JSON object.", metadataPath));
            return AssetReviewApplyRun.From(Normalize(decisionPath), decisionSource.AssetId ?? string.Empty, metadataPath, dryRun, decisionSource.ExpectedSourceFingerprint ?? string.Empty, actualFingerprint, [], null, ContentValidationStatus.Failed, 1, diagnosticsList);
        }

        var assetValidation = new AssetMetadataValidator().ValidateFile(resolvedMetadata);
        diagnosticsList.AddRange(assetValidation.Diagnostics);
        if (assetValidation.Metadata?.Id != decisionSource.AssetId)
        {
            diagnosticsList.Add(new ContentValidationDiagnostic("REVIEW0002", ContentDiagnosticSeverity.Error, "Decision assetId does not match target metadata ID.", metadataPath, "assetId", decisionSource.AssetId));
        }

        if (diagnosticsList.Any(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error))
        {
            return AssetReviewApplyRun.From(Normalize(decisionPath), decisionSource.AssetId ?? string.Empty, metadataPath, dryRun, decisionSource.ExpectedSourceFingerprint ?? string.Empty, actualFingerprint, [], null, ContentValidationStatus.Failed, 1, diagnosticsList);
        }

        var mutations = ApplyDecisions(decisionSource, metadataObject, actualFingerprint, diagnosticsList);
        var proposedJson = metadataObject.ToJsonString(ContentValidationJson.Options);
        var validationSnapshot = new AssetMetadataValidator().ValidateJson(metadataPath, proposedJson);
        diagnosticsList.AddRange(validationSnapshot.Diagnostics.Where(static diagnostic => diagnostic.Id != "ASSET0005"));

        var validationRun = ContentValidationRun.FromDiagnostics(
            metadataPath,
            validationSnapshot.Status,
            validationSnapshot.Status == ContentValidationStatus.Passed ? 0 : 1,
            [new ValidatedContentItem(ContentKind.Asset, validationSnapshot.Id, metadataPath, validationSnapshot.Status)],
            validationSnapshot.Diagnostics);

        if (validationSnapshot.Status != ContentValidationStatus.Passed)
        {
            diagnosticsList.Add(new ContentValidationDiagnostic("REVIEW0007", ContentDiagnosticSeverity.Error, "Updated metadata failed validation.", metadataPath, "metadata", decisionSource.AssetId));
            return AssetReviewApplyRun.From(Normalize(decisionPath), decisionSource.AssetId ?? string.Empty, metadataPath, dryRun, decisionSource.ExpectedSourceFingerprint ?? string.Empty, actualFingerprint, mutations, proposedJson, ContentValidationStatus.Failed, 1, diagnosticsList, validationRun.Result);
        }

        if (!dryRun)
        {
            try
            {
                var temporaryPath = Path.Combine(Path.GetDirectoryName(resolvedMetadata) ?? string.Empty, $".{Path.GetFileName(resolvedMetadata)}.tmp");
                File.WriteAllText(temporaryPath, proposedJson);
                File.Move(temporaryPath, resolvedMetadata, overwrite: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                diagnosticsList.Add(new ContentValidationDiagnostic("REVIEW0008", ContentDiagnosticSeverity.Error, $"Mutation write failed: {exception.Message}", metadataPath));
                return AssetReviewApplyRun.From(Normalize(decisionPath), decisionSource.AssetId ?? string.Empty, metadataPath, dryRun, decisionSource.ExpectedSourceFingerprint ?? string.Empty, actualFingerprint, mutations, null, ContentValidationStatus.Error, 3, diagnosticsList, validationRun.Result);
            }
        }

        return AssetReviewApplyRun.From(Normalize(decisionPath), decisionSource.AssetId ?? string.Empty, metadataPath, dryRun, decisionSource.ExpectedSourceFingerprint ?? string.Empty, actualFingerprint, mutations, dryRun ? proposedJson : null, ContentValidationStatus.Passed, 0, diagnosticsList, validationRun.Result);
    }

    private static List<ContentValidationDiagnostic> ValidateDecisionSource(AssetReviewDecisionSource source, string target)
    {
        var diagnostics = new List<ContentValidationDiagnostic>();

        if (!StringComparer.Ordinal.Equals(source.Schema, "agentic2d.asset-review-decisions.v1"))
        {
            diagnostics.Add(new ContentValidationDiagnostic("REVIEW0001", ContentDiagnosticSeverity.Error, "Decision schema must be agentic2d.asset-review-decisions.v1.", target, "schema"));
        }

        RequireValue(source.Id, target, "id", diagnostics);
        RequireValue(source.AssetId, target, "assetId", diagnostics);
        RequireValue(source.MetadataPath, target, "metadataPath", diagnostics);
        RequireValue(source.ExpectedSourceFingerprint, target, "expectedSourceFingerprint", diagnostics);
        RequireValue(source.Provenance?.SourceKind, target, "provenance.sourceKind", diagnostics);
        RequireValue(source.Provenance?.CreatedBy, target, "provenance.createdBy", diagnostics);
        RequireValue(source.Provenance?.ReviewedBy, target, "provenance.reviewedBy", diagnostics);
        RequireValue(source.Provenance?.ReviewedAt, target, "provenance.reviewedAt", diagnostics);

        if (!string.IsNullOrWhiteSpace(source.MetadataPath) && (Path.IsPathRooted(source.MetadataPath) || source.MetadataPath.Split(['/', '\\']).Contains("..", StringComparer.Ordinal)))
        {
            diagnostics.Add(new ContentValidationDiagnostic("REVIEW0001", ContentDiagnosticSeverity.Error, "metadataPath must be repository-relative and safe.", target, "metadataPath"));
        }

        var decisionIds = new HashSet<string>(StringComparer.Ordinal);
        var targetKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var decision in source.Decisions.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            RequireValue(decision.Id, target, "decisions[].id", diagnostics);
            RequireValue(decision.Target?.TileId, target, "decisions[].target.tileId", diagnostics);
            RequireValue(decision.Target?.Field, target, "decisions[].target.field", diagnostics);
            RequireValue(decision.Target?.Value, target, "decisions[].target.value", diagnostics);
            RequireValue(decision.State, target, "decisions[].state", diagnostics);

            if (!string.IsNullOrWhiteSpace(decision.Id) && !decisionIds.Add(decision.Id))
            {
                diagnostics.Add(new ContentValidationDiagnostic("REVIEW0005", ContentDiagnosticSeverity.Error, $"Duplicate decision ID: {decision.Id}", target, "decisions[].id", decision.Id));
            }

            if (!SupportedStates.Contains(decision.State ?? string.Empty))
            {
                diagnostics.Add(new ContentValidationDiagnostic("REVIEW0001", ContentDiagnosticSeverity.Error, $"Unsupported decision state: {decision.State}", target, "decisions[].state", decision.Id));
            }

            if (!StringComparer.Ordinal.Equals(decision.Target?.Field, "physicalBehaviorsApproved") || !SupportedValues.Contains(decision.Target?.Value ?? string.Empty))
            {
                diagnostics.Add(new ContentValidationDiagnostic("REVIEW0004", ContentDiagnosticSeverity.Error, "Only physicalBehaviorsApproved decisions for supported gameplay values are allowed.", target, "decisions[].target", decision.Id));
            }

            var targetKey = $"{decision.Target?.TileId}|{decision.Target?.Field}|{decision.Target?.Value}";
            if (!targetKeys.Add(targetKey))
            {
                diagnostics.Add(new ContentValidationDiagnostic("REVIEW0005", ContentDiagnosticSeverity.Error, $"Duplicate or contradictory decision target: {targetKey}", target, "decisions[].target", decision.Id));
            }
        }

        return diagnostics;
    }

    private static List<AssetMutationPlanEntry> ApplyDecisions(
        AssetReviewDecisionSource decisionSource,
        JsonObject metadata,
        string actualFingerprint,
        List<ContentValidationDiagnostic> diagnostics)
    {
        var tiles = metadata["tiles"]?.AsArray();
        var humanReview = metadata["humanReview"] as JsonObject ?? new JsonObject();
        metadata["humanReview"] = humanReview;
        humanReview["requiredForApprovedPhysicalBehaviors"] = true;
        var approvals = humanReview["approvals"]?.AsArray() ?? new JsonArray();
        humanReview["approvals"] = approvals;
        var semantics = metadata["semantics"] as JsonObject ?? new JsonObject();
        metadata["semantics"] = semantics;

        var mutations = new List<AssetMutationPlanEntry>();
        if (tiles is null)
        {
            diagnostics.Add(new ContentValidationDiagnostic("REVIEW0002", ContentDiagnosticSeverity.Error, "Target metadata is missing tiles.", decisionSource.MetadataPath ?? string.Empty, "tiles"));
            return mutations;
        }

        foreach (var decision in decisionSource.Decisions.OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            var tileObject = tiles.OfType<JsonObject>().SingleOrDefault(tile => StringComparer.Ordinal.Equals(tile["id"]?.GetValue<string>(), decision.Target?.TileId));
            if (tileObject is null)
            {
                diagnostics.Add(new ContentValidationDiagnostic("REVIEW0002", ContentDiagnosticSeverity.Error, $"Target tile was not found: {decision.Target?.TileId}", decisionSource.MetadataPath ?? string.Empty, "tiles", decision.Target?.TileId));
                continue;
            }

            var fieldName = decision.Target!.Field ?? "physicalBehaviorsApproved";
            var values = tileObject[fieldName]?.AsArray() ?? new JsonArray();
            tileObject[fieldName] = values;
            var current = values.Select(static item => item?.GetValue<string>() ?? string.Empty).Where(static item => item.Length > 0).Order(StringComparer.Ordinal).ToList();
            var next = current.ToHashSet(StringComparer.Ordinal);

            if (decision.State == "approved")
            {
                next.Add(decision.Target.Value!);
                UpsertApproval(approvals, decisionSource, decision, actualFingerprint);
            }
            else
            {
                next.Remove(decision.Target.Value!);
                RemoveApproval(approvals, decision.Id);
            }

            var nextValues = next.Order(StringComparer.Ordinal).ToArray();
            values.Clear();
            foreach (var value in nextValues)
            {
                values.Add(value);
            }

            mutations.Add(new AssetMutationPlanEntry(
                decision.Id ?? string.Empty,
                decision.State ?? string.Empty,
                decision.Target.TileId ?? string.Empty,
                decision.Target.Field,
                current,
                nextValues,
                current.SequenceEqual(nextValues, StringComparer.Ordinal) ? "no-op" : decision.State == "approved" ? "apply-approved-value" : "clear-approved-value"));
        }

        var aggregate = tiles.OfType<JsonObject>()
            .SelectMany(static tile => tile["physicalBehaviorsApproved"]?.AsArray()?.Select(static item => item?.GetValue<string>() ?? string.Empty) ?? [])
            .Where(static value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var aggregateArray = new JsonArray();
        foreach (var value in aggregate)
        {
            aggregateArray.Add(value);
        }

        semantics["physicalBehaviorsApproved"] = aggregateArray;

        return mutations;
    }

    private static void UpsertApproval(JsonArray approvals, AssetReviewDecisionSource source, AssetReviewDecisionEntry decision, string fingerprint)
    {
        RemoveApproval(approvals, decision.Id);
        approvals.Add(new JsonObject
        {
            ["id"] = $"approval.{decision.Id}",
            ["approvedBy"] = source.Provenance?.ReviewedBy,
            ["scope"] = decision.Target?.TileId,
            ["approvedAt"] = source.Provenance?.ReviewedAt,
            ["reason"] = decision.Reason,
            ["decisionId"] = decision.Id,
            ["sourceFingerprint"] = fingerprint,
        });
    }

    private static void RemoveApproval(JsonArray approvals, string? decisionId)
    {
        for (var index = approvals.Count - 1; index >= 0; index--)
        {
            if (approvals[index] is JsonObject approval
                && StringComparer.Ordinal.Equals(approval["decisionId"]?.GetValue<string>(), decisionId))
            {
                approvals.RemoveAt(index);
            }
        }
    }

    private static void RequireValue(string? value, string target, string field, List<ContentValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(new ContentValidationDiagnostic("REVIEW0001", ContentDiagnosticSeverity.Error, $"Missing required field: {field}", target, field));
        }
    }

    private static string ResolveDecisionPath(string path)
    {
        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(ContentTargetResolver.FindRepositoryRoot(), path);
    }

    private static string Normalize(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }
}

public static class AssetReviewApplyArtifactWriter
{
    public static async Task<int> WriteAsync(string outputDirectory, AssetReviewApplyRun run)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(run.Result, ContentValidationJson.Options));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "diagnostics.json"), JsonSerializer.Serialize(run.DiagnosticsDocument, ContentValidationJson.Options));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "mutation-plan.json"), JsonSerializer.Serialize(run.MutationPlan, ContentValidationJson.Options));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "validation-result.json"), JsonSerializer.Serialize(run.ValidationResult, ContentValidationJson.Options));
            if (run.ProposedMetadata is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, "proposed-metadata.json"), run.ProposedMetadata);
            }

            return run.Result.ExitCode;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"failed to write asset review apply artifacts: {exception.Message}", exception);
        }
    }
}

public sealed record AssetReviewApplyRun(
    AssetReviewApplyResultDocument Result,
    ContentDiagnosticsDocument DiagnosticsDocument,
    AssetMutationPlanDocument MutationPlan,
    ContentValidationResultDocument ValidationResult,
    string? ProposedMetadata)
{
    public static AssetReviewApplyRun From(
        string decisionSource,
        string assetId,
        string metadataPath,
        bool dryRun,
        string expectedFingerprint,
        string actualFingerprint,
        IReadOnlyList<AssetMutationPlanEntry> mutations,
        string? proposedMetadata,
        string status,
        int exitCode,
        IReadOnlyList<ContentValidationDiagnostic> diagnostics,
        ContentValidationResultDocument? validationResult = null)
    {
        var artifacts = new List<ContentArtifactReference>
        {
            new("diagnostics.json", "diagnostics"),
            new("mutation-plan.json", "mutation-plan"),
            new("validation-result.json", "validation-result"),
        };

        if (dryRun)
        {
            artifacts.Add(new ContentArtifactReference("proposed-metadata.json", "proposed-metadata"));
        }

        return new AssetReviewApplyRun(
            new AssetReviewApplyResultDocument(
                "agentic2d.asset-review-apply.result.v1",
                "asset review apply",
                decisionSource,
                assetId,
                metadataPath,
                dryRun,
                expectedFingerprint,
                actualFingerprint,
                status,
                exitCode,
                new AssetReviewApplySummary(
                    mutations.Count,
                    mutations.Count(static mutation => mutation.Action != "no-op"),
                    mutations.Count(static mutation => mutation.Action == "no-op"),
                    validationResult?.Status ?? ContentValidationStatus.Error),
                diagnostics,
                artifacts),
            new ContentDiagnosticsDocument("agentic2d.asset-review-apply.diagnostics.v1", diagnostics),
            new AssetMutationPlanDocument("agentic2d.asset-review-apply.mutation-plan.v1", mutations),
            validationResult ?? ContentValidationRun.FromDiagnostics(metadataPath, ContentValidationStatus.Error, 1, [], diagnostics).Result,
            proposedMetadata);
    }
}

public sealed record AssetReviewApplyResultDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("decisionSource")] string DecisionSource,
    [property: JsonPropertyName("assetId")] string AssetId,
    [property: JsonPropertyName("metadataPath")] string MetadataPath,
    [property: JsonPropertyName("dryRun")] bool DryRun,
    [property: JsonPropertyName("expectedSourceFingerprint")] string ExpectedSourceFingerprint,
    [property: JsonPropertyName("actualSourceFingerprint")] string ActualSourceFingerprint,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("summary")] AssetReviewApplySummary Summary,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ContentArtifactReference> Artifacts);

public sealed record AssetReviewApplySummary(
    [property: JsonPropertyName("decisions")] int Decisions,
    [property: JsonPropertyName("mutations")] int Mutations,
    [property: JsonPropertyName("noOps")] int NoOps,
    [property: JsonPropertyName("validationStatus")] string ValidationStatus);

public sealed record AssetMutationPlanDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("mutations")] IReadOnlyList<AssetMutationPlanEntry> Mutations);

public sealed record AssetMutationPlanEntry(
    [property: JsonPropertyName("decisionId")] string DecisionId,
    [property: JsonPropertyName("state")] string State,
    [property: JsonPropertyName("tileId")] string TileId,
    [property: JsonPropertyName("field")] string? Field,
    [property: JsonPropertyName("previousValues")] IReadOnlyList<string> PreviousValues,
    [property: JsonPropertyName("proposedValues")] IReadOnlyList<string> ProposedValues,
    [property: JsonPropertyName("action")] string Action);

public sealed class AssetReviewDecisionSource
{
    [JsonPropertyName("schema")]
    public string? Schema { get; init; }

    [JsonPropertyName("id")]
    public string? Id { get; init; }

    [JsonPropertyName("assetId")]
    public string? AssetId { get; init; }

    [JsonPropertyName("metadataPath")]
    public string? MetadataPath { get; init; }

    [JsonPropertyName("expectedSourceFingerprint")]
    public string? ExpectedSourceFingerprint { get; init; }

    [JsonPropertyName("provenance")]
    public AssetReviewDecisionProvenance? Provenance { get; init; }

    [JsonPropertyName("decisions")]
    public IReadOnlyList<AssetReviewDecisionEntry> Decisions { get; init; } = [];
}

public sealed record AssetReviewDecisionProvenance(
    [property: JsonPropertyName("sourceKind")] string? SourceKind,
    [property: JsonPropertyName("createdBy")] string? CreatedBy,
    [property: JsonPropertyName("reviewedBy")] string? ReviewedBy,
    [property: JsonPropertyName("reviewedAt")] string? ReviewedAt,
    [property: JsonPropertyName("notes")] string? Notes);

public sealed record AssetReviewDecisionEntry(
    [property: JsonPropertyName("id")] string? Id,
    [property: JsonPropertyName("target")] AssetReviewDecisionTarget? Target,
    [property: JsonPropertyName("state")] string? State,
    [property: JsonPropertyName("reason")] string? Reason);

public sealed record AssetReviewDecisionTarget(
    [property: JsonPropertyName("tileId")] string? TileId,
    [property: JsonPropertyName("field")] string? Field,
    [property: JsonPropertyName("value")] string? Value);
