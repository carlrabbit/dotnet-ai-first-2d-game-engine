using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Agentic2D.Validation;

public sealed class ContentValidator
{
    public const string ScenariosScope = "scenarios";
    public const string ScenarioSchema = "agentic2d.scenario.v1";

    private static readonly Regex ScenarioIdPattern = new("^[a-z0-9]+(\\.[a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private static readonly Regex StableIdPattern = new("^[A-Za-z0-9]+([._-][A-Za-z0-9]+)*$", RegexOptions.CultureInvariant);

    public ContentValidationRun Validate(string target)
    {
        var resolution = ContentTargetResolver.Resolve(target);
        if (!resolution.IsSuccess)
        {
            return ContentValidationRun.FromDiagnostics(
                target,
                ContentValidationStatus.Error,
                2,
                [],
                resolution.Diagnostics);
        }

        var items = new List<ValidatedContentItem>();
        var diagnostics = new List<ContentValidationDiagnostic>();

        foreach (var path in resolution.Paths)
        {
            if (path.EndsWith(".asset.json", StringComparison.OrdinalIgnoreCase))
            {
                ValidateAssetFile(path, items, diagnostics);
            }
            else
            {
                ValidateScenarioFile(path, items, diagnostics);
            }
        }

        var status = diagnostics.Any(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error)
            ? ContentValidationStatus.Failed
            : ContentValidationStatus.Passed;
        var exitCode = status == ContentValidationStatus.Passed ? 0 : 1;

        return ContentValidationRun.FromDiagnostics(target, status, exitCode, items, diagnostics);
    }

    private static void ValidateAssetFile(
        string path,
        List<ValidatedContentItem> items,
        List<ContentValidationDiagnostic> diagnostics)
    {
        var item = new AssetMetadataValidator().ValidateFile(path);
        diagnostics.AddRange(item.Diagnostics);
        items.Add(new ValidatedContentItem(ContentKind.Asset, item.Id, item.Path, item.Status));
    }

    private static void ValidateScenarioFile(
        string path,
        List<ValidatedContentItem> items,
        List<ContentValidationDiagnostic> diagnostics)
    {
        var relativePath = ContentTargetResolver.ToRepositoryRelativePath(path);
        JsonDocument document;
        ScenarioSource? scenario;

        try
        {
            using var stream = File.OpenRead(path);
            document = JsonDocument.Parse(stream);
            scenario = document.Deserialize<ScenarioSource>(ContentValidationJson.Options);
        }
        catch (JsonException exception)
        {
            diagnostics.Add(ContentDiagnostic.InvalidSchemaValue(relativePath, "json", $"Scenario JSON is malformed: {exception.Message}"));
            items.Add(new ValidatedContentItem(ContentKind.Scenario, Path.GetFileNameWithoutExtension(path), relativePath, ContentValidationStatus.Failed));
            return;
        }
        catch (IOException exception)
        {
            diagnostics.Add(ContentDiagnostic.InvalidScopeOrPath(relativePath, $"Could not read content file: {exception.Message}"));
            items.Add(new ValidatedContentItem(ContentKind.Scenario, Path.GetFileNameWithoutExtension(path), relativePath, ContentValidationStatus.Error));
            return;
        }

        using (document)
        {
            if (scenario is null)
            {
                diagnostics.Add(ContentDiagnostic.MissingRequiredField(relativePath, "$", "Scenario JSON must contain an object."));
                items.Add(new ValidatedContentItem(ContentKind.Scenario, Path.GetFileNameWithoutExtension(path), relativePath, ContentValidationStatus.Failed));
                return;
            }

            var itemDiagnosticsStart = diagnostics.Count;
            ValidateScenarioContract(document.RootElement, scenario, relativePath, diagnostics);
            var itemStatus = diagnostics.Count == itemDiagnosticsStart
                ? ContentValidationStatus.Passed
                : ContentValidationStatus.Failed;
            var itemId = string.IsNullOrWhiteSpace(scenario.Id) ? Path.GetFileNameWithoutExtension(path) : scenario.Id;
            items.Add(new ValidatedContentItem(ContentKind.Scenario, itemId, relativePath, itemStatus));
        }
    }

    private static void ValidateScenarioContract(
        JsonElement root,
        ScenarioSource scenario,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        foreach (var field in ScenarioRequiredFields.TopLevel)
        {
            if (!root.TryGetProperty(field, out _))
            {
                diagnostics.Add(ContentDiagnostic.MissingRequiredField(target, field, $"Missing required field: {field}"));
            }
        }

        RequireString(scenario.Schema, target, "schema", diagnostics);
        RequireString(scenario.Id, target, "id", diagnostics);
        RequireString(scenario.Category, target, "category", diagnostics);
        RequireString(scenario.Title, target, "title", diagnostics);
        RequireString(scenario.Purpose, target, "purpose", diagnostics);
        RequireString(scenario.SeedPolicy, target, "seedPolicy", diagnostics);

        if (!StringComparer.Ordinal.Equals(scenario.Schema, ScenarioSchema))
        {
            diagnostics.Add(ContentDiagnostic.InvalidSchemaValue(target, "schema", "Scenario schema must be agentic2d.scenario.v1."));
        }

        if (!string.IsNullOrWhiteSpace(scenario.Id) && !ScenarioIdPattern.IsMatch(scenario.Id))
        {
            diagnostics.Add(ContentDiagnostic.InvalidStableId(target, "id", scenario.Id, "Scenario ID must use lowercase dotted segments."));
        }

        if (!StringComparer.Ordinal.Equals(scenario.Category, "smoke"))
        {
            diagnostics.Add(ContentDiagnostic.InvalidSchemaValue(target, "category", "Scenario category must be smoke for Milestone 006."));
        }

        if (!StringComparer.Ordinal.Equals(scenario.SeedPolicy, "none"))
        {
            diagnostics.Add(ContentDiagnostic.InvalidSchemaValue(target, "seedPolicy", "Scenario seedPolicy must be none for Milestone 006."));
        }

        ValidateRuntime(root, scenario, target, diagnostics);
        ValidateEntities(scenario, target, diagnostics);
        ValidateSteps(scenario, target, diagnostics);
        ValidateExpectedEvents(scenario, target, diagnostics);
        ValidateAssertions(scenario, target, diagnostics);
        ValidateArtifacts(scenario, target, diagnostics);
        ValidateHumanReview(root, scenario, target, diagnostics);
    }

    private static void ValidateRuntime(
        JsonElement root,
        ScenarioSource scenario,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (scenario.Runtime is null)
        {
            return;
        }

        if (!root.TryGetProperty("runtime", out var runtimeElement)
            || !runtimeElement.TryGetProperty("ticks", out var ticksElement))
        {
            diagnostics.Add(ContentDiagnostic.MissingRequiredField(target, "runtime.ticks", "Missing required field: runtime.ticks"));
            return;
        }

        if (ticksElement.ValueKind != JsonValueKind.Number || !ticksElement.TryGetInt32(out var ticks) || ticks <= 0)
        {
            diagnostics.Add(ContentDiagnostic.InvalidSchemaValue(target, "runtime.ticks", "runtime.ticks must be a positive integer."));
        }
    }

    private static void ValidateEntities(ScenarioSource scenario, string target, List<ContentValidationDiagnostic> diagnostics)
    {
        if (scenario.InitialState is null)
        {
            return;
        }

        if (scenario.InitialState.Entities.Count == 0)
        {
            diagnostics.Add(ContentDiagnostic.MissingRequiredField(target, "initialState.entities", "initialState.entities must contain at least one entity."));
            return;
        }

        var entityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entity in scenario.InitialState.Entities)
        {
            if (!RequireStableId(entity.Id, target, "initialState.entities[].id", diagnostics))
            {
                continue;
            }

            if (!entityIds.Add(entity.Id))
            {
                diagnostics.Add(ContentDiagnostic.DuplicateId(target, "initialState.entities[].id", entity.Id));
            }
        }
    }

    private static void ValidateSteps(ScenarioSource scenario, string target, List<ContentValidationDiagnostic> diagnostics)
    {
        if (scenario.Steps.Count == 0)
        {
            diagnostics.Add(ContentDiagnostic.MissingRequiredField(target, "steps", "steps must contain at least one step."));
            return;
        }

        var entityIds = scenario.InitialState?.Entities
            .Select(static entity => entity.Id)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal) ?? [];
        var stepIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var step in scenario.Steps)
        {
            if (RequireStableId(step.Id, target, "steps[].id", diagnostics) && !stepIds.Add(step.Id))
            {
                diagnostics.Add(ContentDiagnostic.DuplicateId(target, "steps[].id", step.Id));
            }

            if (step.Command is null)
            {
                diagnostics.Add(ContentDiagnostic.MissingRequiredField(target, "steps[].command", "Scenario step is missing required field: command"));
                continue;
            }

            if (!StringComparer.Ordinal.Equals(step.Command.Type, "move"))
            {
                diagnostics.Add(ContentDiagnostic.UnsupportedCommandType(target, "steps[].command.type", step.Id, step.Command.Type));
                continue;
            }

            if (string.IsNullOrWhiteSpace(step.Command.EntityId) || !entityIds.Contains(step.Command.EntityId))
            {
                diagnostics.Add(ContentDiagnostic.InvalidReference(target, "steps[].command.entityId", step.Command.EntityId, "Move command entityId must reference an initial state entity."));
            }
        }
    }

    private static void ValidateExpectedEvents(ScenarioSource scenario, string target, List<ContentValidationDiagnostic> diagnostics)
    {
        if (scenario.ExpectedEvents.Count == 0)
        {
            diagnostics.Add(ContentDiagnostic.MissingRequiredField(target, "expectedEvents", "expectedEvents must contain at least one event type."));
            return;
        }

        foreach (var expectedEvent in scenario.ExpectedEvents)
        {
            RequireStableId(expectedEvent, target, "expectedEvents[]", diagnostics);
        }
    }

    private static void ValidateAssertions(ScenarioSource scenario, string target, List<ContentValidationDiagnostic> diagnostics)
    {
        if (scenario.Assertions.Count == 0)
        {
            diagnostics.Add(ContentDiagnostic.MissingRequiredField(target, "assertions", "assertions must contain at least one assertion."));
            return;
        }

        var entityIds = scenario.InitialState?.Entities
            .Select(static entity => entity.Id)
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal) ?? [];
        var expectedEvents = scenario.ExpectedEvents.ToHashSet(StringComparer.Ordinal);
        var assertionIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var assertion in scenario.Assertions)
        {
            if (RequireStableId(assertion.Id, target, "assertions[].id", diagnostics) && !assertionIds.Add(assertion.Id))
            {
                diagnostics.Add(ContentDiagnostic.DuplicateId(target, "assertions[].id", assertion.Id));
            }

            switch (assertion.Type)
            {
                case "finalTickEqualsRequested":
                    break;
                case "entityExists":
                    ValidateEntityReference(assertion.EntityId, target, "assertions[].entityId", entityIds, diagnostics);
                    break;
                case "entityPositionEquals":
                    ValidateEntityReference(assertion.EntityId, target, "assertions[].entityId", entityIds, diagnostics);
                    if (assertion.Position is null)
                    {
                        diagnostics.Add(ContentDiagnostic.MissingRequiredField(target, "assertions[].position", "entityPositionEquals assertion requires position."));
                    }

                    break;
                case "eventOccurred":
                    if (string.IsNullOrWhiteSpace(assertion.EventType) || !expectedEvents.Contains(assertion.EventType))
                    {
                        diagnostics.Add(ContentDiagnostic.InvalidReference(target, "assertions[].eventType", assertion.EventType, "eventOccurred assertion eventType must reference expectedEvents."));
                    }

                    break;
                default:
                    diagnostics.Add(ContentDiagnostic.UnsupportedAssertionType(target, "assertions[].type", assertion.Id, assertion.Type));
                    break;
            }
        }
    }

    private static void ValidateArtifacts(ScenarioSource scenario, string target, List<ContentValidationDiagnostic> diagnostics)
    {
        if (scenario.Artifacts is null)
        {
            return;
        }

        ValidateArtifactName(scenario.Artifacts.Result, target, "artifacts.result", diagnostics);
        ValidateArtifactName(scenario.Artifacts.Events, target, "artifacts.events", diagnostics);
        ValidateArtifactName(scenario.Artifacts.Diagnostics, target, "artifacts.diagnostics", diagnostics);
    }

    private static void ValidateHumanReview(
        JsonElement root,
        ScenarioSource scenario,
        string target,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (scenario.HumanReview is null)
        {
            return;
        }

        if (!root.TryGetProperty("humanReview", out var humanReview)
            || !humanReview.TryGetProperty("required", out var required)
            || required.ValueKind is not JsonValueKind.True and not JsonValueKind.False)
        {
            diagnostics.Add(ContentDiagnostic.InvalidHumanReviewDeclaration(target, "humanReview.required", "humanReview.required must be a boolean."));
        }
    }

    private static void ValidateEntityReference(
        string? entityId,
        string target,
        string field,
        HashSet<string> entityIds,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(entityId) || !entityIds.Contains(entityId))
        {
            diagnostics.Add(ContentDiagnostic.InvalidReference(target, field, entityId, "Assertion entityId must reference an initial state entity."));
        }
    }

    private static void RequireString(
        string? value,
        string target,
        string field,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(ContentDiagnostic.MissingRequiredField(target, field, $"Missing required field: {field}"));
        }
    }

    private static bool RequireStableId(
        string? value,
        string target,
        string field,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(ContentDiagnostic.MissingRequiredField(target, field, $"Missing required stable ID field: {field}"));
            return false;
        }

        if (!StableIdPattern.IsMatch(value))
        {
            diagnostics.Add(ContentDiagnostic.InvalidStableId(target, field, value, "Stable IDs must be non-empty strings using letters, numbers, dots, underscores, or hyphens."));
            return false;
        }

        return true;
    }

    private static void ValidateArtifactName(
        string? value,
        string target,
        string field,
        List<ContentValidationDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value)
            || Path.IsPathRooted(value)
            || value.Contains('/')
            || value.Contains('\\')
            || value is "." or "..")
        {
            diagnostics.Add(ContentDiagnostic.InvalidArtifactDeclaration(target, field, value ?? string.Empty));
        }
    }
}

public static class ContentValidationArtifactWriter
{
    public static async Task<int> WriteAsync(string outputDirectory, ContentValidationRun run)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "result.json"),
                JsonSerializer.Serialize(run.Result, ContentValidationJson.Options));

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "diagnostics.json"),
                JsonSerializer.Serialize(run.DiagnosticsDocument, ContentValidationJson.Options));

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "validated-items.json"),
                JsonSerializer.Serialize(run.ValidatedItemsDocument, ContentValidationJson.Options));

            return run.Result.ExitCode;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"failed to write content validation artifacts: {exception.Message}", exception);
        }
    }
}

public static class ContentTargetResolver
{
    public static ContentTargetResolution Resolve(string target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return ContentTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Content validation target must not be empty.")]);
        }

        if (StringComparer.Ordinal.Equals(target, ContentValidator.ScenariosScope))
        {
            var scenarioRoot = Path.Combine(FindRepositoryRoot(), "game", "scenarios");
            if (!Directory.Exists(scenarioRoot))
            {
                return ContentTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Scenario content directory was not found: game/scenarios")]);
            }

            var paths = Directory.EnumerateFiles(scenarioRoot, "*.json", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .ToArray();

            return paths.Length == 0
                ? ContentTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "No scenario JSON files were found under game/scenarios.")])
                : ContentTargetResolution.Success(paths);
        }

        if (StringComparer.Ordinal.Equals(target, AssetMetadataValidator.AssetsScope))
        {
            var assetMetadataRoot = Path.Combine(FindRepositoryRoot(), "game", "assets", "metadata");
            if (!Directory.Exists(assetMetadataRoot))
            {
                return ContentTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Asset metadata directory was not found: game/assets/metadata")]);
            }

            var paths = Directory.EnumerateFiles(assetMetadataRoot, "*.asset.json", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
                .ToArray();

            return paths.Length == 0
                ? ContentTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "No asset metadata JSON files were found under game/assets/metadata.")])
                : ContentTargetResolution.Success(paths);
        }

        if (!target.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return ContentTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Unsupported content target. Expected scenarios, assets, or a repository-relative .json path.")]);
        }

        if (!Path.IsPathRooted(target) && target.Split(['/', '\\']).Contains("..", StringComparer.Ordinal))
        {
            return ContentTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, "Content path must be repository-relative and must not escape the repository.")]);
        }

        var resolvedPath = Path.IsPathRooted(target)
            ? target
            : Path.Combine(FindRepositoryRoot(), target);

        if (!File.Exists(resolvedPath))
        {
            return ContentTargetResolution.Failure([ContentDiagnostic.InvalidScopeOrPath(target, $"Content file was not found: {target}")]);
        }

        return ContentTargetResolution.Success([resolvedPath]);
    }

    public static string ToRepositoryRelativePath(string path)
    {
        var fullPath = Path.GetFullPath(path);
        var repoRoot = FindRepositoryRoot();

        return Path.GetRelativePath(repoRoot, fullPath).Replace(Path.DirectorySeparatorChar, '/');
    }

    public static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "dotnet-ai-first-2d-game-engine.slnx")))
            {
                return Path.GetFullPath(directory);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return Directory.GetCurrentDirectory();
    }
}

public sealed class ContentTargetResolution
{
    private ContentTargetResolution(IReadOnlyList<string> paths, IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        Paths = paths;
        Diagnostics = diagnostics;
    }

    public bool IsSuccess => Diagnostics.Count == 0;

    public IReadOnlyList<string> Paths { get; }

    public IReadOnlyList<ContentValidationDiagnostic> Diagnostics { get; }

    public static ContentTargetResolution Success(IReadOnlyList<string> paths)
    {
        return new ContentTargetResolution(paths, []);
    }

    public static ContentTargetResolution Failure(IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        return new ContentTargetResolution([], diagnostics);
    }
}

public sealed record ContentValidationRun(
    ContentValidationResultDocument Result,
    ContentDiagnosticsDocument DiagnosticsDocument,
    ContentValidatedItemsDocument ValidatedItemsDocument)
{
    public static ContentValidationRun FromDiagnostics(
        string scope,
        string status,
        int exitCode,
        IReadOnlyList<ValidatedContentItem> items,
        IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        var artifacts = new[]
        {
            new ContentArtifactReference("diagnostics.json", "diagnostics"),
            new ContentArtifactReference("validated-items.json", "validated-items"),
        };
        var result = new ContentValidationResultDocument(
            "agentic2d.content-validation.result.v1",
            "content validate",
            scope,
            status,
            exitCode,
            new ContentValidationSummary(
                items.Count,
                diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error),
                diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Warning),
                diagnostics.Count(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Info),
                items.Count(static item => item.Kind == ContentKind.Scenario),
                items.Count(static item => item.Kind == ContentKind.Asset)),
            diagnostics,
            artifacts);

        return new ContentValidationRun(
            result,
            new ContentDiagnosticsDocument("agentic2d.content-validation.diagnostics.v1", diagnostics),
            new ContentValidatedItemsDocument("agentic2d.content-validation.items.v1", items));
    }
}

public sealed record ContentValidationResultDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("scope")] string Scope,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("summary")] ContentValidationSummary Summary,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ContentArtifactReference> Artifacts);

public sealed record ContentValidationSummary(
    [property: JsonPropertyName("itemsValidated")] int ItemsValidated,
    [property: JsonPropertyName("errors")] int Errors,
    [property: JsonPropertyName("warnings")] int Warnings,
    [property: JsonPropertyName("infos")] int Infos,
    [property: JsonPropertyName("scenariosValidated")] int ScenariosValidated,
    [property: JsonPropertyName("assetsValidated")] int AssetsValidated);

public sealed record ContentDiagnosticsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics);

public sealed record ContentValidatedItemsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("items")] IReadOnlyList<ValidatedContentItem> Items);

public sealed record ValidatedContentItem(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("status")] string Status);

public sealed record ContentArtifactReference(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("kind")] string Kind);

public sealed record ContentValidationDiagnostic(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("target")] string Target,
    [property: JsonPropertyName("field")] string? Field = null,
    [property: JsonPropertyName("itemId")] string? ItemId = null);

public static class ContentDiagnostic
{
    public static ContentValidationDiagnostic MissingRequiredField(string target, string field, string message)
    {
        return new ContentValidationDiagnostic("CONTENT0001", ContentDiagnosticSeverity.Error, message, target, field);
    }

    public static ContentValidationDiagnostic InvalidSchemaValue(string target, string field, string message)
    {
        return new ContentValidationDiagnostic("CONTENT0002", ContentDiagnosticSeverity.Error, message, target, field);
    }

    public static ContentValidationDiagnostic InvalidStableId(string target, string field, string itemId, string message)
    {
        return new ContentValidationDiagnostic("CONTENT0003", ContentDiagnosticSeverity.Error, message, target, field, itemId);
    }

    public static ContentValidationDiagnostic DuplicateId(string target, string field, string itemId)
    {
        return new ContentValidationDiagnostic("CONTENT0004", ContentDiagnosticSeverity.Error, $"Duplicate stable ID: {itemId}", target, field, itemId);
    }

    public static ContentValidationDiagnostic InvalidReference(string target, string field, string? itemId, string message)
    {
        return new ContentValidationDiagnostic("CONTENT0005", ContentDiagnosticSeverity.Error, message, target, field, itemId);
    }

    public static ContentValidationDiagnostic UnsupportedCommandType(string target, string field, string itemId, string commandType)
    {
        return new ContentValidationDiagnostic("CONTENT0006", ContentDiagnosticSeverity.Error, $"Unsupported command type: {commandType}", target, field, itemId);
    }

    public static ContentValidationDiagnostic UnsupportedAssertionType(string target, string field, string itemId, string assertionType)
    {
        return new ContentValidationDiagnostic("CONTENT0007", ContentDiagnosticSeverity.Error, $"Unsupported assertion type: {assertionType}", target, field, itemId);
    }

    public static ContentValidationDiagnostic InvalidArtifactDeclaration(string target, string field, string itemId)
    {
        return new ContentValidationDiagnostic("CONTENT0008", ContentDiagnosticSeverity.Error, "Artifact declarations must be relative filenames.", target, field, itemId);
    }

    public static ContentValidationDiagnostic InvalidHumanReviewDeclaration(string target, string field, string message)
    {
        return new ContentValidationDiagnostic("CONTENT0009", ContentDiagnosticSeverity.Error, message, target, field);
    }

    public static ContentValidationDiagnostic InvalidScopeOrPath(string target, string message)
    {
        return new ContentValidationDiagnostic("CONTENT0010", ContentDiagnosticSeverity.Error, message, target);
    }
}

public static class ContentValidationStatus
{
    public const string Passed = "passed";
    public const string Failed = "failed";
    public const string Error = "error";
}

public static class ContentDiagnosticSeverity
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Error = "error";
}

public static class ContentKind
{
    public const string Scenario = "scenario";
    public const string Asset = "asset";
}

public static class ContentValidationJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
}

internal static class ScenarioRequiredFields
{
    public static readonly string[] TopLevel =
    [
        "schema",
        "id",
        "category",
        "title",
        "purpose",
        "seedPolicy",
        "runtime",
        "initialState",
        "steps",
        "expectedEvents",
        "assertions",
        "artifacts",
        "humanReview",
    ];
}
