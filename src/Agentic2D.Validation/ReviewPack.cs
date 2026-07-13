using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic2D.Validation;

public sealed class ReviewPackGenerator
{
    public ReviewPackRun Generate(string artifactRoot)
    {
        var normalizedRoot = NormalizeReference(artifactRoot);
        var diagnostics = new List<ContentValidationDiagnostic>();
        var groups = new List<ReviewArtifactGroup>();
        var sourceItems = new List<ReviewSourceItem>();

        if (string.IsNullOrWhiteSpace(artifactRoot) || !Directory.Exists(artifactRoot))
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                normalizedRoot,
                "input",
                $"Artifact root was not found: {normalizedRoot}"));
        }
        else
        {
            foreach (var resultPath in Directory.EnumerateFiles(artifactRoot, "result.json", SearchOption.AllDirectories)
                         .Order(StringComparer.Ordinal))
            {
                TryIncludeResult(artifactRoot, resultPath, groups, sourceItems, diagnostics);
            }
        }

        AddMissingFamilyDiagnostics(normalizedRoot, groups, diagnostics);

        var reviewQuestions = BuildReviewQuestions(sourceItems, groups, diagnostics);
        var hasErrors = diagnostics.Any(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error);
        var hasFailedGroups = groups.Any(static group => group.Kind != "asset-review-apply" && (group.Status is ContentValidationStatus.Failed or ContentValidationStatus.Error));
        var status = hasErrors || hasFailedGroups ? ContentValidationStatus.Failed : ContentValidationStatus.Passed;
        var exitCode = status == ContentValidationStatus.Passed ? 0 : 1;

        return ReviewPackRun.From(
            normalizedRoot,
            status,
            exitCode,
            groups.DistinctBy(static group => (group.Kind, group.Path)).OrderBy(static group => group.Kind).ThenBy(static group => group.Path, StringComparer.Ordinal).ToArray(),
            sourceItems.DistinctBy(static item => (item.Kind, item.Id, item.Path)).OrderBy(static item => item.Kind).ThenBy(static item => item.Id, StringComparer.Ordinal).ToArray(),
            reviewQuestions,
            diagnostics);
    }

    private static void TryIncludeResult(
        string artifactRoot,
        string resultPath,
        List<ReviewArtifactGroup> groups,
        List<ReviewSourceItem> sourceItems,
        List<ContentValidationDiagnostic> diagnostics)
    {
        var relativeResultPath = NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(resultPath));
        JsonDocument document;

        try
        {
            document = JsonDocument.Parse(File.ReadAllText(resultPath));
        }
        catch (JsonException exception)
        {
            if (LooksLikeKnownArtifactPath(artifactRoot, resultPath))
            {
                diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                    relativeResultPath,
                    "result.json",
                    $"Known artifact result JSON is malformed: {exception.Message}"));
            }

            return;
        }
        catch (IOException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                relativeResultPath,
                "result.json",
                $"Could not read artifact result: {exception.Message}"));
            return;
        }

        using (document)
        {
            var root = document.RootElement;
            var schema = GetString(root, "schema");
            var command = GetString(root, "command");
            var status = GetString(root, "status") ?? ContentValidationStatus.Error;

            switch (schema)
            {
                case "agentic2d.scenario.result.v1":
                    groups.Add(new ReviewArtifactGroup("scenario-runner", status, relativeResultPath, command));
                    AddScenarioSourceItem(root, sourceItems);
                    AddFailedGroupDiagnostic(relativeResultPath, "scenario-runner", status, diagnostics);
                    break;

                case "agentic2d.content-validation.result.v1":
                    groups.Add(new ReviewArtifactGroup("content-validation", status, relativeResultPath, command));
                    AddContentSourceItems(resultPath, sourceItems, diagnostics);
                    AddFailedGroupDiagnostic(relativeResultPath, "content-validation", status, diagnostics);
                    break;

                case "agentic2d.asset-inspection.result.v1":
                    groups.Add(new ReviewArtifactGroup("asset-inspection", status, relativeResultPath, command));
                    AddAssetInspectionSourceItem(resultPath, sourceItems, diagnostics);
                    AddFailedGroupDiagnostic(relativeResultPath, "asset-inspection", status, diagnostics);
                    break;

                case "agentic2d.asset-perception.result.v1":
                    groups.Add(new ReviewArtifactGroup("asset-perception", status, relativeResultPath, command));
                    AddAssetPerceptionSourceItem(resultPath, sourceItems, diagnostics);
                    AddFailedGroupDiagnostic(relativeResultPath, "asset-perception", status, diagnostics);
                    break;

                case "agentic2d.asset-review-apply.result.v1":
                    groups.Add(new ReviewArtifactGroup("asset-review-apply", status, relativeResultPath, command));
                    AddAssetReviewSourceItem(root, sourceItems);
                    AddFailedGroupDiagnostic(relativeResultPath, "asset-review-apply", status, diagnostics);
                    break;

                case "agentic2d.map-inspection.result.v1":
                    groups.Add(new ReviewArtifactGroup("map-inspection", status, relativeResultPath, command));
                    AddMapInspectionSourceItem(resultPath, sourceItems, diagnostics);
                    AddFailedGroupDiagnostic(relativeResultPath, "map-inspection", status, diagnostics);
                    break;

                case "agentic2d.runtime-inspection.result.v1":
                    groups.Add(new ReviewArtifactGroup("runtime-inspection", status, relativeResultPath, command, RuntimeCapabilities(resultPath)));
                    AddRuntimeInspectionSourceItems(resultPath, sourceItems, diagnostics);
                    AddFailedGroupDiagnostic(relativeResultPath, "runtime-inspection", status, diagnostics);
                    break;
            }
        }
    }

    private static void AddScenarioSourceItem(JsonElement root, List<ReviewSourceItem> sourceItems)
    {
        if (!root.TryGetProperty("scenario", out var scenario))
        {
            return;
        }

        var id = GetString(scenario, "id");
        var path = GetString(scenario, "source");
        if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(path))
        {
            sourceItems.Add(new ReviewSourceItem("scenario", id, NormalizeReference(path)));
        }
    }

    private static void AddContentSourceItems(
        string resultPath,
        List<ReviewSourceItem> sourceItems,
        List<ContentValidationDiagnostic> diagnostics)
    {
        var itemsPath = Path.Combine(Path.GetDirectoryName(resultPath) ?? string.Empty, "validated-items.json");
        if (!File.Exists(itemsPath))
        {
            diagnostics.Add(ReviewDiagnostic.IncompleteSourceReference(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(resultPath)),
                "validated-items.json",
                "Content validation artifact is missing validated-items.json."));
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(itemsPath));
            if (!document.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            {
                diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                    NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(itemsPath)),
                    "items",
                    "Content validation items artifact is malformed."));
                return;
            }

            foreach (var item in items.EnumerateArray())
            {
                var kind = GetString(item, "kind");
                var id = GetString(item, "id");
                var path = GetString(item, "path");
                if (!string.IsNullOrWhiteSpace(kind) && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(path))
                {
                    sourceItems.Add(new ReviewSourceItem(kind, id, NormalizeReference(path)));
                }
            }
        }
        catch (JsonException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(itemsPath)),
                "validated-items.json",
                $"Content validation items JSON is malformed: {exception.Message}"));
        }
        catch (IOException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(itemsPath)),
                "validated-items.json",
                $"Could not read content validation items: {exception.Message}"));
        }
    }

    private static void AddAssetInspectionSourceItem(
        string resultPath,
        List<ReviewSourceItem> sourceItems,
        List<ContentValidationDiagnostic> diagnostics)
    {
        var summaryPath = Path.Combine(Path.GetDirectoryName(resultPath) ?? string.Empty, "asset-summary.json");
        if (!File.Exists(summaryPath))
        {
            diagnostics.Add(ReviewDiagnostic.IncompleteSourceReference(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(resultPath)),
                "asset-summary.json",
                "Asset inspection artifact is missing asset-summary.json."));
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(summaryPath));
            if (!document.RootElement.TryGetProperty("asset", out var asset))
            {
                diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                    NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(summaryPath)),
                    "asset",
                    "Asset inspection summary is missing asset identity."));
                return;
            }

            var id = GetString(asset, "id");
            var path = GetString(asset, "metadataPath");
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(path))
            {
                sourceItems.Add(new ReviewSourceItem("asset", id, NormalizeReference(path)));
            }
        }
        catch (JsonException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(summaryPath)),
                "asset-summary.json",
                $"Asset inspection summary JSON is malformed: {exception.Message}"));
        }
        catch (IOException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(summaryPath)),
                "asset-summary.json",
                $"Could not read asset inspection summary: {exception.Message}"));
        }
    }

    private static void AddAssetPerceptionSourceItem(
        string resultPath,
        List<ReviewSourceItem> sourceItems,
        List<ContentValidationDiagnostic> diagnostics)
    {
        var featuresPath = Path.Combine(Path.GetDirectoryName(resultPath) ?? string.Empty, "tile-features.json");
        if (!File.Exists(featuresPath))
        {
            diagnostics.Add(ReviewDiagnostic.IncompleteSourceReference(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(resultPath)),
                "tile-features.json",
                "Asset perception artifact is missing tile-features.json."));
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(featuresPath));
            var assetId = GetString(document.RootElement, "assetId");
            var path = GetString(document.RootElement, "metadataPath");
            if (!string.IsNullOrWhiteSpace(assetId) && !string.IsNullOrWhiteSpace(path))
            {
                sourceItems.Add(new ReviewSourceItem(ContentKind.Asset, assetId, NormalizeReference(path)));
            }
        }
        catch (JsonException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(featuresPath)),
                "tile-features.json",
                $"Asset perception features JSON is malformed: {exception.Message}"));
        }
        catch (IOException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(featuresPath)),
                "tile-features.json",
                $"Could not read asset perception features: {exception.Message}"));
        }
    }

    private static void AddAssetReviewSourceItem(JsonElement root, List<ReviewSourceItem> sourceItems)
    {
        var assetId = GetString(root, "assetId");
        var metadataPath = GetString(root, "metadataPath");
        if (!string.IsNullOrWhiteSpace(assetId) && !string.IsNullOrWhiteSpace(metadataPath))
        {
            sourceItems.Add(new ReviewSourceItem(ContentKind.Asset, assetId, NormalizeReference(metadataPath)));
        }
    }

    private static void AddMapInspectionSourceItem(
        string resultPath,
        List<ReviewSourceItem> sourceItems,
        List<ContentValidationDiagnostic> diagnostics)
    {
        var summaryPath = Path.Combine(Path.GetDirectoryName(resultPath) ?? string.Empty, "map-summary.json");
        if (!File.Exists(summaryPath))
        {
            diagnostics.Add(ReviewDiagnostic.IncompleteSourceReference(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(resultPath)),
                "map-summary.json",
                "Map inspection artifact is missing map-summary.json."));
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(summaryPath));
            if (!document.RootElement.TryGetProperty("map", out var map))
            {
                diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                    NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(summaryPath)),
                    "map",
                    "Map inspection summary is missing map identity."));
                return;
            }

            var id = GetString(map, "id");
            var path = GetString(map, "path");
            if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(path))
            {
                sourceItems.Add(new ReviewSourceItem(ContentKind.Map, id, NormalizeReference(path)));
            }
        }
        catch (JsonException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(summaryPath)),
                "map-summary.json",
                $"Map inspection summary JSON is malformed: {exception.Message}"));
        }
        catch (IOException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(summaryPath)),
                "map-summary.json",
                $"Could not read map inspection summary: {exception.Message}"));
        }
    }

    private static IReadOnlyList<string>? RuntimeCapabilities(string resultPath)
    {
        var directory = Path.GetDirectoryName(resultPath) ?? string.Empty;
        var capabilities = new List<string>();
        if (File.Exists(Path.Combine(directory, "behaviors.json"))) capabilities.Add("behavior-execution");
        if (File.Exists(Path.Combine(directory, "spatial-resolutions.jsonl"))) capabilities.Add("spatial-resolution");
        return capabilities.Count == 0 ? null : capabilities;
    }


    private static void AddRuntimeInspectionSourceItems(
        string resultPath,
        List<ReviewSourceItem> sourceItems,
        List<ContentValidationDiagnostic> diagnostics)
    {
        var referencesPath = Path.Combine(Path.GetDirectoryName(resultPath) ?? string.Empty, "content-references.json");
        if (!File.Exists(referencesPath))
        {
            diagnostics.Add(ReviewDiagnostic.IncompleteSourceReference(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(resultPath)),
                "content-references.json",
                "Runtime inspection artifact is missing content-references.json."));
            return;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(referencesPath));
            if (document.RootElement.TryGetProperty("scenario", out var scenario))
            {
                var id = GetString(scenario, "id");
                var path = GetString(scenario, "path");
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(path))
                {
                    sourceItems.Add(new ReviewSourceItem(ContentKind.Scenario, id, NormalizeReference(path)));
                }
            }

            if (document.RootElement.TryGetProperty("map", out var map) && map.ValueKind == JsonValueKind.Object)
            {
                var id = GetString(map, "id");
                var path = GetString(map, "path");
                if (!string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(path))
                {
                    sourceItems.Add(new ReviewSourceItem(ContentKind.Map, id, NormalizeReference(path)));
                }
            }
        }
        catch (JsonException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(referencesPath)),
                "content-references.json",
                $"Runtime inspection content references JSON is malformed: {exception.Message}"));
        }
        catch (IOException exception)
        {
            diagnostics.Add(ReviewDiagnostic.MalformedArtifactGroup(
                NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(referencesPath)),
                "content-references.json",
                $"Could not read runtime inspection content references: {exception.Message}"));
        }
    }

    private static IReadOnlyList<ReviewQuestion> BuildReviewQuestions(
        IReadOnlyList<ReviewSourceItem> sourceItems,
        IReadOnlyList<ReviewArtifactGroup> groups,
        IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        var questions = new List<ReviewQuestion>
        {
            new("review.diagnostics.clarity", "review-pack", "Are diagnostics clear enough to diagnose failures?"),
            new("review.references.sufficient", "review-pack", "Are source references and artifact references sufficient for follow-up work?"),
        };

        foreach (var asset in sourceItems.Where(static item => item.Kind == ContentKind.Asset).OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            questions.Add(new ReviewQuestion(
                $"review.{asset.Id}.semantic-proposals",
                asset.Id,
                "Are proposed visual labels acceptable as proposals?"));
            questions.Add(new ReviewQuestion(
                $"review.{asset.Id}.physical-approval-evidence",
                asset.Id,
                "Are any approved physical/gameplay behaviors backed by human review evidence?"));
        }

        foreach (var map in sourceItems.Where(static item => item.Kind == ContentKind.Map).OrderBy(static item => item.Id, StringComparer.Ordinal))
        {
            questions.Add(new ReviewQuestion(
                $"review.{map.Id}.map-diagnostics",
                map.Id,
                "Do map diagnostics clearly identify map, layer, cell, asset, and tile references?"));
        }

        if (groups.Any(static group => group.Kind == "asset-perception"))
        {
            questions.Add(new ReviewQuestion(
                "review.asset-perception.proposal-boundary",
                "asset-perception",
                "Are perception proposals visibly distinct from approved semantics?"));
        }

        if (groups.Any(static group => group.Kind == "runtime-inspection"))
        {
            questions.Add(new ReviewQuestion(
                "review.runtime-inspection.diagnosis",
                "runtime-inspection",
                "Does runtime inspection make the executed state diagnosable without reading source?"));
        }

        if (groups.Count == 0 || diagnostics.Count > 0)
        {
            questions.Add(new ReviewQuestion(
                "review.artifact-coverage",
                "review-pack",
                "Are the included artifact groups sufficient for this review?"));
        }

        return questions.DistinctBy(static question => question.Id).OrderBy(static question => question.Id, StringComparer.Ordinal).ToArray();
    }

    private static void AddMissingFamilyDiagnostics(
        string artifactRoot,
        IReadOnlyList<ReviewArtifactGroup> groups,
        List<ContentValidationDiagnostic> diagnostics)
    {
        foreach (var kind in new[] { "scenario-runner", "content-validation", "asset-inspection" })
        {
            if (!groups.Any(group => group.Kind == kind))
            {
                diagnostics.Add(ReviewDiagnostic.MissingArtifactGroup(
                    artifactRoot,
                    kind,
                    $"Known artifact group was not found: {kind}."));
            }
        }
    }

    private static void AddFailedGroupDiagnostic(
        string path,
        string kind,
        string status,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (kind == "asset-review-apply")
        {
            return;
        }

        if (status is ContentValidationStatus.Failed or ContentValidationStatus.Error)
        {
            diagnostics.Add(ReviewDiagnostic.FailedArtifactGroup(
                path,
                kind,
                $"Included artifact group reported status {status}: {kind}."));
        }
    }

    private static bool LooksLikeKnownArtifactPath(string artifactRoot, string resultPath)
    {
        var relative = Path.GetRelativePath(artifactRoot, resultPath).Replace(Path.DirectorySeparatorChar, '/');
        return relative.StartsWith("scenarios/", StringComparison.Ordinal)
            || relative.StartsWith("content/", StringComparison.Ordinal)
            || relative.StartsWith("assets/", StringComparison.Ordinal)
            || relative.StartsWith("maps/", StringComparison.Ordinal)
            || relative.StartsWith("runtime/", StringComparison.Ordinal)
            || relative.StartsWith("asset-review/", StringComparison.Ordinal);
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : null;
    }

    internal static string NormalizeReference(string path)
    {
        return path.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');
    }
}

public static class ReviewPackArtifactWriter
{
    public static async Task<int> WriteAsync(string outputDirectory, ReviewPackRun run)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "review-manifest.json"),
                JsonSerializer.Serialize(run.Manifest, ContentValidationJson.Options));
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "diagnostics.json"),
                JsonSerializer.Serialize(run.DiagnosticsDocument, ContentValidationJson.Options));
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "review-summary.md"),
                ReviewSummaryMarkdown.Write(run.Manifest));

            return run.Manifest.ExitCode;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"failed to write review pack artifacts: {exception.Message}", exception);
        }
    }
}

public sealed record ReviewPackRun(ReviewPackManifest Manifest, ReviewPackDiagnosticsDocument DiagnosticsDocument)
{
    public static ReviewPackRun From(
        string artifactRoot,
        string status,
        int exitCode,
        IReadOnlyList<ReviewArtifactGroup> groups,
        IReadOnlyList<ReviewSourceItem> sourceItems,
        IReadOnlyList<ReviewQuestion> reviewQuestions,
        IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        var manifest = new ReviewPackManifest(
            "agentic2d.review-pack.manifest.v1",
            "review pack",
            new ReviewPackInput(artifactRoot),
            status,
            exitCode,
            new ReviewPackSummary(
                groups.Count,
                diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error),
                diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Warning),
                reviewQuestions.Count),
            groups,
            sourceItems,
            reviewQuestions,
            diagnostics,
            [
                new ContentArtifactReference("review-summary.md", "review-summary"),
                new ContentArtifactReference("diagnostics.json", "diagnostics"),
            ]);

        return new ReviewPackRun(manifest, new ReviewPackDiagnosticsDocument("agentic2d.review-pack.diagnostics.v1", diagnostics));
    }
}

public sealed record ReviewPackManifest(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("input")] ReviewPackInput Input,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("summary")] ReviewPackSummary Summary,
    [property: JsonPropertyName("artifactGroups")] IReadOnlyList<ReviewArtifactGroup> ArtifactGroups,
    [property: JsonPropertyName("sourceItems")] IReadOnlyList<ReviewSourceItem> SourceItems,
    [property: JsonPropertyName("reviewQuestions")] IReadOnlyList<ReviewQuestion> ReviewQuestions,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ContentArtifactReference> Artifacts);

public sealed record ReviewPackInput([property: JsonPropertyName("artifactRoot")] string ArtifactRoot);

public sealed record ReviewPackSummary(
    [property: JsonPropertyName("artifactGroupsIncluded")] int ArtifactGroupsIncluded,
    [property: JsonPropertyName("errors")] int Errors,
    [property: JsonPropertyName("warnings")] int Warnings,
    [property: JsonPropertyName("reviewQuestions")] int ReviewQuestions);

public sealed record ReviewArtifactGroup(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("command")] string? Command = null,
    [property: JsonPropertyName("capabilities")] IReadOnlyList<string>? Capabilities = null);

public sealed record ReviewSourceItem(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("path")] string Path);

public sealed record ReviewQuestion(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("question")] string Question);

public sealed record ReviewPackDiagnosticsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics);

public static class ReviewDiagnostic
{
    public static ContentValidationDiagnostic MissingArtifactGroup(string target, string itemId, string message)
    {
        return new ContentValidationDiagnostic("REVIEW0001", ContentDiagnosticSeverity.Warning, message, target, null, itemId);
    }

    public static ContentValidationDiagnostic MalformedArtifactGroup(string target, string field, string message)
    {
        return new ContentValidationDiagnostic("REVIEW0002", ContentDiagnosticSeverity.Error, message, target, field);
    }

    public static ContentValidationDiagnostic FailedArtifactGroup(string target, string itemId, string message)
    {
        return new ContentValidationDiagnostic("REVIEW0003", ContentDiagnosticSeverity.Error, message, target, null, itemId);
    }

    public static ContentValidationDiagnostic IncompleteSourceReference(string target, string field, string message)
    {
        return new ContentValidationDiagnostic("REVIEW0004", ContentDiagnosticSeverity.Warning, message, target, field);
    }

    public static ContentValidationDiagnostic MissingApprovalEvidence(string target, string itemId, string message)
    {
        return new ContentValidationDiagnostic("REVIEW0005", ContentDiagnosticSeverity.Warning, message, target, null, itemId);
    }
}

internal static class ReviewSummaryMarkdown
{
    public static string Write(ReviewPackManifest manifest)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Review Pack");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine($"Status: `{manifest.Status}`");
        builder.AppendLine($"Input artifact root: `{manifest.Input.ArtifactRoot}`");
        builder.AppendLine();
        builder.AppendLine("## Included artifact groups");
        builder.AppendLine();
        AppendTable(builder, ["Kind", "Status", "Path"], manifest.ArtifactGroups.Select(static group => new[] { group.Kind, group.Status, group.Path }));
        builder.AppendLine();
        builder.AppendLine("## Diagnostics");
        builder.AppendLine();
        AppendTable(builder, ["ID", "Severity", "Target", "Message"], manifest.Diagnostics.Select(static diagnostic => new[] { diagnostic.Id, diagnostic.Severity, diagnostic.Target, diagnostic.Message }));
        builder.AppendLine();
        builder.AppendLine("## Source items");
        builder.AppendLine();
        AppendTable(builder, ["Kind", "ID", "Path"], manifest.SourceItems.Select(static item => new[] { item.Kind, item.Id, item.Path }));
        builder.AppendLine();
        builder.AppendLine("## Human review questions");
        builder.AppendLine();
        AppendTable(builder, ["ID", "Target", "Question"], manifest.ReviewQuestions.Select(static question => new[] { question.Id, question.Target, question.Question }));
        builder.AppendLine();
        builder.AppendLine("## Artifact references");
        builder.AppendLine();
        AppendTable(builder, ["Kind", "Path"], manifest.Artifacts.Select(static artifact => new[] { artifact.Kind, artifact.Path }));
        return builder.ToString();
    }

    private static void AppendTable(StringBuilder builder, IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        builder.Append("| ");
        builder.AppendJoin(" | ", headers);
        builder.AppendLine(" |");
        builder.Append("| ");
        builder.AppendJoin(" | ", headers.Select(static _ => "---"));
        builder.AppendLine(" |");

        var rowCount = 0;
        foreach (var row in rows)
        {
            rowCount++;
            builder.Append("| ");
            builder.AppendJoin(" | ", row.Select(EscapeTableCell));
            builder.AppendLine(" |");
        }

        if (rowCount == 0)
        {
            builder.Append("| ");
            builder.AppendJoin(" | ", headers.Select(static _ => "None"));
            builder.AppendLine(" |");
        }
    }

    private static string EscapeTableCell(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}

public sealed class AssetCurationWorkbenchGenerator
{
    public AssetCurationWorkbenchRun Generate(string assetTarget, string reviewPackPath)
    {
        var diagnostics = new List<ContentValidationDiagnostic>();
        var metadataResolution = ResolveAssetMetadata(assetTarget);
        AssetMetadataSource? metadata = null;
        string metadataPath = string.Empty;

        if (!metadataResolution.IsSuccess)
        {
            diagnostics.AddRange(metadataResolution.Diagnostics.Select(static diagnostic => new ContentValidationDiagnostic(
                "CURATION0001",
                diagnostic.Severity,
                diagnostic.Message,
                diagnostic.Target,
                diagnostic.Field,
                diagnostic.ItemId)));
        }
        else
        {
            var validationItem = new AssetMetadataValidator().ValidateFile(metadataResolution.MetadataPath);
            metadata = validationItem.Metadata;
            metadataPath = validationItem.Path;
            diagnostics.AddRange(validationItem.Diagnostics.Select(static diagnostic => new ContentValidationDiagnostic(
                diagnostic.Id,
                diagnostic.Severity,
                diagnostic.Message,
                diagnostic.Target,
                diagnostic.Field,
                diagnostic.ItemId)));
        }

        var reviewPackManifestPath = ResolveReviewPackManifestPath(reviewPackPath);
        ReviewPackManifest? manifest = null;
        if (!File.Exists(reviewPackManifestPath))
        {
            diagnostics.Add(new ContentValidationDiagnostic(
                "CURATION0002",
                ContentDiagnosticSeverity.Error,
                $"Review pack manifest was not found: {ReviewPackGenerator.NormalizeReference(reviewPackPath)}",
                ReviewPackGenerator.NormalizeReference(reviewPackPath)));
        }
        else
        {
            try
            {
                manifest = JsonSerializer.Deserialize<ReviewPackManifest>(File.ReadAllText(reviewPackManifestPath), ContentValidationJson.Options);
                if (manifest?.Schema != "agentic2d.review-pack.manifest.v1")
                {
                    diagnostics.Add(new ContentValidationDiagnostic(
                        "CURATION0002",
                        ContentDiagnosticSeverity.Error,
                        "Review pack manifest schema is unsupported.",
                        ReviewPackGenerator.NormalizeReference(reviewPackPath),
                        "schema"));
                }
            }
            catch (JsonException exception)
            {
                diagnostics.Add(new ContentValidationDiagnostic(
                    "CURATION0002",
                    ContentDiagnosticSeverity.Error,
                    $"Review pack manifest JSON is malformed: {exception.Message}",
                    ReviewPackGenerator.NormalizeReference(reviewPackPath)));
            }
        }

        if (metadata?.Id is { Length: > 0 } assetId
            && manifest is not null
            && !manifest.SourceItems.Any(item => item.Kind == ContentKind.Asset && item.Id == assetId))
        {
            diagnostics.Add(new ContentValidationDiagnostic(
                "CURATION0003",
                ContentDiagnosticSeverity.Warning,
                "Review pack does not include asset inspection or content evidence for this asset.",
                assetId));
        }

        var perception = metadata?.Id is { Length: > 0 } perceptionAssetId && manifest is not null
            ? TryLoadPerceptionEvidence(manifest, perceptionAssetId, diagnostics)
            : null;

        var hasErrors = diagnostics.Any(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error);
        var status = hasErrors ? ContentValidationStatus.Failed : ContentValidationStatus.Passed;
        var exitCode = status == ContentValidationStatus.Passed ? 0 : 1;

        return AssetCurationWorkbenchRun.From(
            metadata,
            metadataPath,
            ReviewPackGenerator.NormalizeReference(ContentTargetResolver.ToRepositoryRelativePath(reviewPackManifestPath)),
            perception,
            status,
            exitCode,
            diagnostics);
    }

    private static AssetTargetResolution ResolveAssetMetadata(string target)
    {
        if (StringComparer.Ordinal.Equals(target, AssetMetadataValidator.SmokeAssetId))
        {
            var smokePath = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), AssetMetadataValidator.SmokeAssetPath);
            return File.Exists(smokePath)
                ? AssetTargetResolution.Success(smokePath)
                : AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, $"Asset metadata file was not found: {AssetMetadataValidator.SmokeAssetPath}")]);
        }

        if (!target.EndsWith(".asset.json", StringComparison.OrdinalIgnoreCase))
        {
            return AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Unsupported asset target. Expected asset.tile-atlas-smoke or a repository-relative .asset.json path.")]);
        }

        if (Path.IsPathRooted(target) || target.Split(['/', '\\']).Contains("..", StringComparer.Ordinal))
        {
            return AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Asset metadata path must be repository-relative and must not escape the repository.")]);
        }

        var metadataPath = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), target);
        return File.Exists(metadataPath)
            ? AssetTargetResolution.Success(metadataPath)
            : AssetTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, $"Asset metadata file was not found: {target}")]);
    }

    private static string ResolveReviewPackManifestPath(string reviewPackPath)
    {
        var path = reviewPackPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            ? reviewPackPath
            : Path.Combine(reviewPackPath, "review-manifest.json");
        return Path.IsPathRooted(path)
            ? path
            : Path.Combine(ContentTargetResolver.FindRepositoryRoot(), path);
    }

    private static AssetPerceptionEvidence? TryLoadPerceptionEvidence(
        ReviewPackManifest manifest,
        string assetId,
        List<ContentValidationDiagnostic> diagnostics)
    {
        var group = manifest.ArtifactGroups.FirstOrDefault(static item => item.Kind == "asset-perception");
        if (group is null)
        {
            return null;
        }

        var resultPath = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), group.Path);
        var directory = Path.GetDirectoryName(resultPath) ?? string.Empty;
        var featuresPath = Path.Combine(directory, "tile-features.json");
        var proposalsPath = Path.Combine(directory, "semantic-proposals.json");
        if (!File.Exists(featuresPath) || !File.Exists(proposalsPath))
        {
            diagnostics.Add(new ContentValidationDiagnostic(
                "CURATION0004",
                ContentDiagnosticSeverity.Warning,
                "Asset perception artifacts were referenced by the review pack but are incomplete.",
                group.Path));
            return null;
        }

        try
        {
            var features = JsonSerializer.Deserialize<AssetTileFeaturesDocument>(File.ReadAllText(featuresPath), ContentValidationJson.Options);
            var proposals = JsonSerializer.Deserialize<AssetSemanticProposalsDocument>(File.ReadAllText(proposalsPath), ContentValidationJson.Options);
            if (features?.AssetId != assetId || proposals?.AssetId != assetId)
            {
                return null;
            }

            return new AssetPerceptionEvidence(features, proposals);
        }
        catch (JsonException exception)
        {
            diagnostics.Add(new ContentValidationDiagnostic(
                "CURATION0004",
                ContentDiagnosticSeverity.Warning,
                $"Asset perception artifacts are malformed: {exception.Message}",
                group.Path));
            return null;
        }
    }
}

public static class AssetCurationWorkbenchArtifactWriter
{
    public static async Task<int> WriteAsync(string outputDirectory, AssetCurationWorkbenchRun run)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "review-data.json"),
                JsonSerializer.Serialize(run.ReviewData, ContentValidationJson.Options));
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "diagnostics.json"),
                JsonSerializer.Serialize(run.DiagnosticsDocument, ContentValidationJson.Options));
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "index.html"),
                AssetCurationWorkbenchHtml.Write(run.ReviewData));

            return run.ReviewData.ExitCode;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"failed to write asset curation workbench artifacts: {exception.Message}", exception);
        }
    }
}

public sealed record AssetCurationWorkbenchRun(
    AssetCurationReviewData ReviewData,
    AssetCurationDiagnosticsDocument DiagnosticsDocument)
{
    public static AssetCurationWorkbenchRun From(
        AssetMetadataSource? metadata,
        string metadataPath,
        string reviewPackManifestPath,
        AssetPerceptionEvidence? perception,
        string status,
        int exitCode,
        IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        var approvals = metadata?.HumanReview?.Approvals
            .Select(static approval => approval.Scope)
            .Where(static scope => !string.IsNullOrWhiteSpace(scope))
            .Select(static scope => scope!)
            .ToHashSet(StringComparer.Ordinal) ?? [];

        var tiles = (metadata?.Tiles ?? [])
            .OrderBy(static tile => tile.Y)
            .ThenBy(static tile => tile.X)
            .ThenBy(static tile => tile.Id, StringComparer.Ordinal)
            .Select(tile =>
            {
                var feature = perception?.Features.Tiles.SingleOrDefault(item => item.Id == (tile.Id ?? string.Empty));
                var proposals = perception?.Proposals.Proposals.Where(item => item.TileId == (tile.Id ?? string.Empty)).OrderBy(static item => item.Value, StringComparer.Ordinal).ToArray() ?? [];
                return new AssetCurationTile(
                    tile.Id ?? string.Empty,
                    tile.X,
                    tile.Y,
                    tile.VisualLabelsProposed.Order(StringComparer.Ordinal).Select(static label => new ReviewStateValue(label, "proposed")).ToArray(),
                    tile.PhysicalBehaviorsApproved.Order(StringComparer.Ordinal).Select(behavior => new ReviewStateValue(
                        behavior,
                        approvals.Contains(tile.Id ?? string.Empty) ? "approved" : "needs-revision")).ToArray(),
                    feature is null
                        ? null
                        : new AssetCurationPerception(
                            feature.AlphaCoverage,
                            feature.RepresentativeAverageColor,
                            feature.RepresentativeDominantColor,
                            feature.DuplicateGroupId,
                            proposals.Select(static proposal => new ReviewStateValue(proposal.Value, proposal.State)).ToArray()),
                    BuildTileQuestions(tile, approvals, proposals.Length));
            })
            .ToArray();

        var reviewData = new AssetCurationReviewData(
            "agentic2d.asset-curation-workbench.review-data.v1",
            "asset curate",
            new AssetCurationAsset(
                metadata?.Id ?? string.Empty,
                metadataPath,
                metadata?.Source?.Path ?? string.Empty,
                metadata?.Kind ?? string.Empty,
                metadata?.Title ?? string.Empty,
                metadata?.TileAtlas is null ? null : new AssetSummaryTileAtlas(
                    metadata.TileAtlas.TileWidth,
                    metadata.TileAtlas.TileHeight,
                    metadata.TileAtlas.Columns,
                    metadata.TileAtlas.Rows,
                    metadata.Tiles.Count)),
            new AssetCurationReviewPack(reviewPackManifestPath),
            status,
            exitCode,
            tiles,
            diagnostics,
            [
                new ContentArtifactReference("index.html", "static-html-workbench"),
                new ContentArtifactReference("diagnostics.json", "diagnostics"),
            ]);

        return new AssetCurationWorkbenchRun(
            reviewData,
            new AssetCurationDiagnosticsDocument("agentic2d.asset-curation-workbench.diagnostics.v1", diagnostics));
    }

    private static IReadOnlyList<ReviewQuestion> BuildTileQuestions(AssetTileSource tile, HashSet<string> approvals, int perceptionProposalCount)
    {
        var questions = new List<ReviewQuestion>();
        if (tile.VisualLabelsProposed.Count > 0)
        {
            questions.Add(new ReviewQuestion(
                $"review.{tile.Id}.visual-proposals",
                tile.Id ?? string.Empty,
                "Are proposed visual labels acceptable as proposals?"));
        }

        if (tile.PhysicalBehaviorsApproved.Count > 0 && !approvals.Contains(tile.Id ?? string.Empty))
        {
            questions.Add(new ReviewQuestion(
                $"review.{tile.Id}.physical-approval-evidence",
                tile.Id ?? string.Empty,
                "Approved physical/gameplay behavior is missing explicit human review evidence."));
        }

        if (perceptionProposalCount > 0)
        {
            questions.Add(new ReviewQuestion(
                $"review.{tile.Id}.perception-proposals",
                tile.Id ?? string.Empty,
                "Are deterministic perception proposals acceptable as proposals and still distinct from approvals?"));
        }

        return questions;
    }
}

public sealed record AssetCurationReviewData(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("asset")] AssetCurationAsset Asset,
    [property: JsonPropertyName("reviewPack")] AssetCurationReviewPack ReviewPack,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("tiles")] IReadOnlyList<AssetCurationTile> Tiles,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ContentArtifactReference> Artifacts);

public sealed record AssetCurationAsset(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("metadataPath")] string MetadataPath,
    [property: JsonPropertyName("sourcePath")] string SourcePath,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("tileAtlas")] AssetSummaryTileAtlas? TileAtlas);

public sealed record AssetCurationReviewPack([property: JsonPropertyName("path")] string Path);

public sealed record AssetCurationTile(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("x")] int X,
    [property: JsonPropertyName("y")] int Y,
    [property: JsonPropertyName("visualLabels")] IReadOnlyList<ReviewStateValue> VisualLabels,
    [property: JsonPropertyName("physicalBehaviors")] IReadOnlyList<ReviewStateValue> PhysicalBehaviors,
    [property: JsonPropertyName("perception")] AssetCurationPerception? Perception,
    [property: JsonPropertyName("reviewQuestions")] IReadOnlyList<ReviewQuestion> ReviewQuestions);

public sealed record AssetCurationPerception(
    [property: JsonPropertyName("alphaCoverage")] double AlphaCoverage,
    [property: JsonPropertyName("averageColor")] string AverageColor,
    [property: JsonPropertyName("dominantColor")] string DominantColor,
    [property: JsonPropertyName("duplicateGroupId")] string DuplicateGroupId,
    [property: JsonPropertyName("proposals")] IReadOnlyList<ReviewStateValue> Proposals);

public sealed record ReviewStateValue(
    [property: JsonPropertyName("value")] string Value,
    [property: JsonPropertyName("reviewState")] string ReviewState);

public sealed record AssetCurationDiagnosticsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics);

internal static class AssetCurationWorkbenchHtml
{
    public static string Write(AssetCurationReviewData data)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang=\"en\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<title>Asset Curation Workbench</title>");
        builder.AppendLine("<style>body{font-family:system-ui,sans-serif;margin:2rem;line-height:1.45;color:#202124;background:#f8f8f6}main{max-width:960px}table{border-collapse:collapse;width:100%;background:#fff}th,td{border:1px solid #c9c9c4;padding:.5rem;text-align:left;vertical-align:top}th{background:#ece9e1}.state{font-weight:700}.proposed{color:#8a5a00}.approved{color:#17643b}.needs-revision{color:#9a3412}.empty{color:#6b7280}</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body><main>");
        builder.AppendLine($"<h1>{Encode(data.Asset.Id)} Asset Curation Workbench</h1>");
        builder.AppendLine($"<p>Status: <strong>{Encode(data.Status)}</strong></p>");
        builder.AppendLine("<h2>Asset identity</h2>");
        builder.AppendLine("<ul>");
        builder.AppendLine($"<li>Metadata: <code>{Encode(data.Asset.MetadataPath)}</code></li>");
        builder.AppendLine($"<li>Source: <code>{Encode(data.Asset.SourcePath)}</code></li>");
        builder.AppendLine($"<li>Review pack: <code>{Encode(data.ReviewPack.Path)}</code></li>");
        builder.AppendLine("</ul>");
        builder.AppendLine("<h2>Structural tile atlas summary</h2>");
        if (data.Asset.TileAtlas is null)
        {
            builder.AppendLine("<p class=\"empty\">No tile atlas metadata available.</p>");
        }
        else
        {
            builder.AppendLine($"<p>{data.Asset.TileAtlas.Columns} columns x {data.Asset.TileAtlas.Rows} rows, {data.Asset.TileAtlas.TileWidth}x{data.Asset.TileAtlas.TileHeight} tiles, {data.Asset.TileAtlas.DeclaredTileCount} declared tiles.</p>");
        }

        builder.AppendLine("<h2>Tiles</h2>");
        builder.AppendLine("<table><thead><tr><th>Tile ID</th><th>Coordinate</th><th>Proposed visual labels</th><th>Approved physical/gameplay behavior</th><th>Perception evidence</th><th>Review questions</th></tr></thead><tbody>");
        foreach (var tile in data.Tiles)
        {
            builder.AppendLine("<tr>");
            builder.AppendLine($"<td><code>{Encode(tile.Id)}</code></td>");
            builder.AppendLine($"<td>{tile.X}, {tile.Y}</td>");
            builder.AppendLine($"<td>{Values(tile.VisualLabels)}</td>");
            builder.AppendLine($"<td>{Values(tile.PhysicalBehaviors)}</td>");
            builder.AppendLine($"<td>{Perception(tile.Perception)}</td>");
            builder.AppendLine($"<td>{Questions(tile.ReviewQuestions)}</td>");
            builder.AppendLine("</tr>");
        }

        builder.AppendLine("</tbody></table>");
        builder.AppendLine("<h2>Diagnostics</h2>");
        if (data.Diagnostics.Count == 0)
        {
            builder.AppendLine("<p class=\"empty\">No diagnostics.</p>");
        }
        else
        {
            builder.AppendLine("<ul>");
            foreach (var diagnostic in data.Diagnostics)
            {
                builder.AppendLine($"<li><code>{Encode(diagnostic.Id)}</code> {Encode(diagnostic.Severity)}: {Encode(diagnostic.Message)} <code>{Encode(diagnostic.Target)}</code></li>");
            }

            builder.AppendLine("</ul>");
        }

        builder.AppendLine("<h2>Artifact references</h2>");
        builder.AppendLine("<ul>");
        foreach (var artifact in data.Artifacts)
        {
            builder.AppendLine($"<li>{Encode(artifact.Kind)}: <code>{Encode(artifact.Path)}</code></li>");
        }

        builder.AppendLine("</ul>");
        builder.AppendLine("</main></body></html>");
        return builder.ToString();
    }

    private static string Values(IReadOnlyList<ReviewStateValue> values)
    {
        if (values.Count == 0)
        {
            return "<span class=\"empty\">None</span>";
        }

        return string.Join("<br>", values.Select(static value => $"<span>{Encode(value.Value)} <span class=\"state {Encode(value.ReviewState)}\">{Encode(value.ReviewState)}</span></span>"));
    }

    private static string Questions(IReadOnlyList<ReviewQuestion> questions)
    {
        if (questions.Count == 0)
        {
            return "<span class=\"empty\">None</span>";
        }

        return string.Join("<br>", questions.Select(static question => Encode(question.Question)));
    }

    private static string Perception(AssetCurationPerception? perception)
    {
        if (perception is null)
        {
            return "<span class=\"empty\">None</span>";
        }

        var lines = new List<string>
        {
            $"alpha {perception.AlphaCoverage:0.######}",
            $"avg <code>{Encode(perception.AverageColor)}</code>",
            $"dominant <code>{Encode(perception.DominantColor)}</code>",
            $"group <code>{Encode(perception.DuplicateGroupId)}</code>",
        };

        if (perception.Proposals.Count > 0)
        {
            lines.Add($"proposals {Values(perception.Proposals)}");
        }

        return string.Join("<br>", lines);
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}

public sealed record AssetPerceptionEvidence(AssetTileFeaturesDocument Features, AssetSemanticProposalsDocument Proposals);
