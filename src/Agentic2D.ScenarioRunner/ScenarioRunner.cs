using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.Validation;

namespace Agentic2D.ScenarioRunner;

public sealed class ScenarioRunner
{
    public const string BuiltInRuntimeSmokeId = "runtime.smoke";
    public const string BuiltInRuntimeSmokePath = "game/scenarios/smoke/runtime-smoke.json";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly JsonSerializerOptions JsonLineSerializerOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public ScenarioRunResult Run(string scenarioReference)
    {
        var resolution = ScenarioSourceResolver.Resolve(scenarioReference);
        if (!resolution.IsSuccess)
        {
            return ScenarioRunResult.InvalidInput(
                CreateUnknownScenarioSummary(scenarioReference),
                resolution.Diagnostics);
        }

        return RunResolved(resolution.Path, resolution.Source);
    }

    public async Task<int> RunAndWriteAsync(string scenarioReference, string outputDirectory)
    {
        var result = Run(scenarioReference);
        return await ScenarioArtifactWriter.WriteAsync(outputDirectory, result);
    }

    private static ScenarioRunResult RunResolved(string sourcePath, string source)
    {
        var loadResult = ScenarioSourceLoader.Load(source, sourcePath);
        if (!loadResult.IsSuccess)
        {
            return ScenarioRunResult.InvalidInput(loadResult.Scenario, loadResult.Diagnostics);
        }

        var scenario = loadResult.SourceScenario;
        var runtime = new MinimalRuntime();

        try
        {
            foreach (var entity in scenario.InitialState!.Entities)
            {
                runtime.CreateEntity(new EntityId(entity.Id), entity.Position);
            }

            var step = scenario.Steps.Single();
            var moveCommand = new MoveCommand(new EntityId(step.Command.EntityId), step.Command.Amount);
            var commandResult = runtime.Submit(moveCommand);

            if (!StringComparer.Ordinal.Equals(commandResult.Status, "accepted"))
            {
                var runtimeDiagnostics = runtime.Diagnostics.Select(ToScenarioDiagnostic).ToArray();
                return ScenarioRunResult.RuntimeError(CreateScenarioSummary(scenario, sourcePath), scenario.Runtime!.Ticks, runtime, runtimeDiagnostics);
            }

            runtime.Run(scenario.Runtime!.Ticks, moveCommand);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return ScenarioRunResult.RuntimeError(
                CreateScenarioSummary(scenario, sourcePath),
                scenario.Runtime!.Ticks,
                runtime,
                [new ScenarioDiagnostic("SCENARIO3001", "error", exception.Message)]);
        }

        var events = runtime.Events
            .Select(static (runtimeEvent, index) => new ScenarioEvent(index + 1, runtimeEvent.Tick, runtimeEvent.Type, runtimeEvent.Message))
            .ToArray();
        var assertions = ScenarioAssertions.Evaluate(scenario, runtime, events);
        var diagnosticsForExpectedEvents = ValidateExpectedEventOrder(scenario.ExpectedEvents, events);
        var allAssertions = assertions
            .Concat(diagnosticsForExpectedEvents.Select(static diagnostic => new ScenarioAssertion(
                $"assert.diagnostic.{diagnostic.Id}",
                Passed: false,
                Message: diagnostic.Message)))
            .ToArray();
        var diagnostics = runtime.Diagnostics.Select(ToScenarioDiagnostic).Concat(diagnosticsForExpectedEvents).ToArray();
        var hasErrorDiagnostics = diagnostics.Any(static diagnostic => diagnostic.Severity == "error");
        var status = allAssertions.All(static assertion => assertion.Passed) && !hasErrorDiagnostics
            ? RuntimeStatus.Passed
            : RuntimeStatus.Failed;
        var exitCode = status == RuntimeStatus.Passed ? 0 : 1;

        return new ScenarioRunResult(
            Result: ScenarioResultDocument.FromExecution(
                CreateScenarioSummary(scenario, sourcePath),
                status,
                exitCode,
                scenario.Runtime!.Ticks,
                runtime.CurrentTick.Value,
                events,
                runtime.QueryEntities(),
                allAssertions,
                diagnostics),
            Events: events,
            Diagnostics: diagnostics);
    }

    private static ScenarioSummary CreateScenarioSummary(ScenarioSource scenario, string sourcePath)
    {
        return new ScenarioSummary(scenario.Id, scenario.Category, ToRepositoryRelativePath(sourcePath));
    }

    private static ScenarioSummary CreateUnknownScenarioSummary(string scenarioReference)
    {
        return new ScenarioSummary(scenarioReference, "unknown", scenarioReference);
    }

    private static string ToRepositoryRelativePath(string sourcePath)
    {
        var fullPath = Path.GetFullPath(sourcePath);
        var repoRoot = FindRepositoryRoot();

        return Path.GetRelativePath(repoRoot, fullPath).Replace(Path.DirectorySeparatorChar, '/');
    }

    internal static string FindRepositoryRoot()
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

    private static ScenarioDiagnostic ToScenarioDiagnostic(Diagnostic diagnostic)
    {
        return new ScenarioDiagnostic(diagnostic.Code, diagnostic.Severity, diagnostic.Message);
    }

    private static ScenarioDiagnostic[] ValidateExpectedEventOrder(IReadOnlyList<string> expectedEvents, IReadOnlyList<ScenarioEvent> actualEvents)
    {
        var diagnostics = new List<ScenarioDiagnostic>();
        var searchIndex = 0;

        foreach (var expectedEvent in expectedEvents)
        {
            var foundIndex = -1;
            for (var index = searchIndex; index < actualEvents.Count; index++)
            {
                if (StringComparer.Ordinal.Equals(actualEvents[index].Type, expectedEvent))
                {
                    foundIndex = index;
                    break;
                }
            }

            if (foundIndex < 0)
            {
                diagnostics.Add(new ScenarioDiagnostic("SCENARIO2001", "error", $"Expected event was not emitted in deterministic order: {expectedEvent}"));
                break;
            }

            searchIndex = foundIndex + 1;
        }

        return diagnostics.ToArray();
    }

    public static JsonSerializerOptions JsonOptions => SerializerOptions;

    public static JsonSerializerOptions JsonLineOptions => JsonLineSerializerOptions;
}

public static class ScenarioArtifactWriter
{
    public static async Task<int> WriteAsync(string outputDirectory, ScenarioRunResult runResult)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);

            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "result.json"),
                JsonSerializer.Serialize(runResult.Result, ScenarioRunner.JsonOptions));

            var eventLines = runResult.Events.Select(static scenarioEvent => JsonSerializer.Serialize(scenarioEvent, ScenarioRunner.JsonLineOptions)).ToArray();
            var eventsJsonl = eventLines.Length == 0
                ? string.Empty
                : string.Join(Environment.NewLine, eventLines) + Environment.NewLine;
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "events.jsonl"), eventsJsonl);

            var diagnosticsDocument = new ScenarioDiagnosticsDocument("agentic2d.diagnostics.v1", runResult.Diagnostics);
            await File.WriteAllTextAsync(
                Path.Combine(outputDirectory, "diagnostics.json"),
                JsonSerializer.Serialize(diagnosticsDocument, ScenarioRunner.JsonOptions));

            return runResult.Result.ExitCode;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"failed to write scenario artifacts: {exception.Message}", exception);
        }
    }
}

public static class ScenarioSourceResolver
{
    public static ScenarioSourceResolution Resolve(string scenarioReference)
    {
        if (string.IsNullOrWhiteSpace(scenarioReference))
        {
            return ScenarioSourceResolution.Failure(
                [new ScenarioDiagnostic("SCENARIO0007", "error", "Scenario reference must not be empty.")]);
        }

        var path = StringComparer.Ordinal.Equals(scenarioReference, ScenarioRunner.BuiltInRuntimeSmokeId)
            ? ScenarioRunner.BuiltInRuntimeSmokePath
            : scenarioReference;

        if (!Path.GetExtension(path).Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            return ScenarioSourceResolution.Failure(
                [new ScenarioDiagnostic("SCENARIO0008", "error", $"Unknown scenario ID: {scenarioReference}")]);
        }

        var resolvedPath = Path.IsPathRooted(path)
            ? path
            : Path.Combine(ScenarioRunner.FindRepositoryRoot(), path);

        if (!File.Exists(resolvedPath))
        {
            return ScenarioSourceResolution.Failure(
                [new ScenarioDiagnostic("SCENARIO0007", "error", $"Scenario file was not found: {path}")]);
        }

        return ScenarioSourceResolution.Success(resolvedPath, File.ReadAllText(resolvedPath));
    }
}

public static class ScenarioSourceLoader
{
    public static ScenarioLoadResult Load(string source, string sourcePath)
    {
        ScenarioSource? scenario;
        try
        {
            scenario = JsonSerializer.Deserialize<ScenarioSource>(source, ScenarioRunner.JsonOptions);
        }
        catch (JsonException exception)
        {
            return ScenarioLoadResult.Failure(
                new ScenarioSummary(Path.GetFileNameWithoutExtension(sourcePath), "unknown", sourcePath),
                [new ScenarioDiagnostic("SCENARIO0000", "error", $"Scenario JSON is malformed: {exception.Message}")]);
        }

        if (scenario is null)
        {
            return ScenarioLoadResult.Failure(
                new ScenarioSummary(Path.GetFileNameWithoutExtension(sourcePath), "unknown", sourcePath),
                [new ScenarioDiagnostic("SCENARIO0000", "error", "Scenario JSON is empty.")]);
        }

        var diagnostics = Validate(scenario);
        var summary = new ScenarioSummary(
            string.IsNullOrWhiteSpace(scenario.Id) ? Path.GetFileNameWithoutExtension(sourcePath) : scenario.Id,
            string.IsNullOrWhiteSpace(scenario.Category) ? "unknown" : scenario.Category,
            sourcePath);

        return diagnostics.Count == 0
            ? ScenarioLoadResult.Success(scenario)
            : ScenarioLoadResult.Failure(summary, diagnostics);
    }

    private static List<ScenarioDiagnostic> Validate(ScenarioSource scenario)
    {
        var diagnostics = new List<ScenarioDiagnostic>();
        RequireString(scenario.Schema, "schema", diagnostics);
        RequireString(scenario.Id, "id", diagnostics);
        RequireString(scenario.Category, "category", diagnostics);
        RequireString(scenario.Title, "title", diagnostics);
        RequireString(scenario.Purpose, "purpose", diagnostics);
        RequireString(scenario.SeedPolicy, "seedPolicy", diagnostics);

        if (!StringComparer.Ordinal.Equals(scenario.Schema, "agentic2d.scenario.v1"))
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0002", "error", "Scenario schema must be agentic2d.scenario.v1."));
        }

        if (!StringComparer.Ordinal.Equals(scenario.Category, "smoke"))
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0002", "error", "Scenario category must be smoke for Milestone 005."));
        }

        if (!StringComparer.Ordinal.Equals(scenario.SeedPolicy, "none"))
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0002", "error", "Scenario seedPolicy must be none for Milestone 005."));
        }

        if (scenario.Runtime is null)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "Scenario file is missing required field: runtime"));
        }
        else if (scenario.Runtime.Ticks <= 0)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0003", "error", "runtime.ticks must be a positive integer."));
        }

        ValidateEntities(scenario, diagnostics);
        ValidateSteps(scenario, diagnostics);
        ValidateExpectedEvents(scenario, diagnostics);
        ValidateAssertions(scenario, diagnostics);

        if (scenario.Artifacts is null)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "Scenario file is missing required field: artifacts"));
        }
        else
        {
            ValidateArtifactName(scenario.Artifacts.Result, "artifacts.result", "result.json", diagnostics);
            ValidateArtifactName(scenario.Artifacts.Events, "artifacts.events", "events.jsonl", diagnostics);
            ValidateArtifactName(scenario.Artifacts.Diagnostics, "artifacts.diagnostics", "diagnostics.json", diagnostics);
        }

        if (scenario.HumanReview is null)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "Scenario file is missing required field: humanReview"));
        }

        return diagnostics;
    }

    private static void ValidateEntities(ScenarioSource scenario, List<ScenarioDiagnostic> diagnostics)
    {
        if (scenario.InitialState is null)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "Scenario file is missing required field: initialState"));
            return;
        }

        if (scenario.InitialState.Entities.Count == 0)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "initialState.entities must contain at least one entity."));
            return;
        }

        var entityIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entity in scenario.InitialState.Entities)
        {
            if (string.IsNullOrWhiteSpace(entity.Id))
            {
                diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "Scenario entity is missing required field: id"));
                continue;
            }

            if (!entityIds.Add(entity.Id))
            {
                diagnostics.Add(new ScenarioDiagnostic("SCENARIO0004", "error", $"Scenario entity ID is duplicated: {entity.Id}"));
            }
        }
    }

    private static void ValidateSteps(ScenarioSource scenario, List<ScenarioDiagnostic> diagnostics)
    {
        if (scenario.Steps.Count != 1)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0005", "error", "Milestone 005 scenarios must contain exactly one step."));
            return;
        }

        var entityIds = scenario.InitialState?.Entities.Select(static entity => entity.Id).ToHashSet(StringComparer.Ordinal) ?? [];
        var step = scenario.Steps[0];
        if (string.IsNullOrWhiteSpace(step.Id))
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "Scenario step is missing required field: id"));
        }

        if (step.Command is null)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "Scenario step is missing required field: command"));
            return;
        }

        if (!StringComparer.Ordinal.Equals(step.Command.Type, "move"))
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0005", "error", "Unsupported command type. Milestone 005 supports only: move."));
        }

        if (string.IsNullOrWhiteSpace(step.Command.EntityId) || !entityIds.Contains(step.Command.EntityId))
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0005", "error", "Move command entityId must reference an initial state entity."));
        }

        if (step.Command.Amount == 0)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0005", "error", "Move command amount must be non-zero."));
        }
    }

    private static void ValidateExpectedEvents(ScenarioSource scenario, List<ScenarioDiagnostic> diagnostics)
    {
        if (scenario.ExpectedEvents.Count == 0)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "expectedEvents must contain at least one event type."));
        }
    }

    private static void ValidateAssertions(ScenarioSource scenario, List<ScenarioDiagnostic> diagnostics)
    {
        if (scenario.Assertions.Count == 0)
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "assertions must contain at least one assertion."));
            return;
        }

        foreach (var assertion in scenario.Assertions)
        {
            if (string.IsNullOrWhiteSpace(assertion.Id))
            {
                diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", "Scenario assertion is missing required field: id"));
            }

            switch (assertion.Type)
            {
                case "finalTickEqualsRequested":
                    break;
                case "entityExists":
                    if (string.IsNullOrWhiteSpace(assertion.EntityId))
                    {
                        diagnostics.Add(new ScenarioDiagnostic("SCENARIO0006", "error", "entityExists assertion requires entityId."));
                    }

                    break;
                case "entityPositionEquals":
                    if (string.IsNullOrWhiteSpace(assertion.EntityId) || assertion.Position is null)
                    {
                        diagnostics.Add(new ScenarioDiagnostic("SCENARIO0006", "error", "entityPositionEquals assertion requires entityId and position."));
                    }

                    break;
                case "eventOccurred":
                    if (string.IsNullOrWhiteSpace(assertion.EventType))
                    {
                        diagnostics.Add(new ScenarioDiagnostic("SCENARIO0006", "error", "eventOccurred assertion requires eventType."));
                    }

                    break;
                default:
                    diagnostics.Add(new ScenarioDiagnostic("SCENARIO0006", "error", $"Unsupported assertion type: {assertion.Type}"));
                    break;
            }
        }
    }

    private static void RequireString(string? value, string fieldName, List<ScenarioDiagnostic> diagnostics)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", $"Scenario file is missing required field: {fieldName}"));
        }
    }

    private static void ValidateArtifactName(string? actual, string fieldName, string expected, List<ScenarioDiagnostic> diagnostics)
    {
        if (!StringComparer.Ordinal.Equals(actual, expected))
        {
            diagnostics.Add(new ScenarioDiagnostic("SCENARIO0001", "error", $"{fieldName} must be {expected}."));
        }
    }
}

public static class ScenarioAssertions
{
    public static ScenarioAssertion[] Evaluate(ScenarioSource scenario, MinimalRuntime runtime, IReadOnlyList<ScenarioEvent> events)
    {
        return scenario.Assertions
            .Select(assertion => EvaluateAssertion(assertion, scenario.Runtime!.Ticks, runtime, events))
            .ToArray();
    }

    private static ScenarioAssertion EvaluateAssertion(ScenarioAssertionSource assertion, int ticksRequested, MinimalRuntime runtime, IReadOnlyList<ScenarioEvent> events)
    {
        return assertion.Type switch
        {
            "finalTickEqualsRequested" => new ScenarioAssertion(
                assertion.Id,
                runtime.CurrentTick.Value == ticksRequested,
                "final tick equals requested tick count",
                ticksRequested.ToString(System.Globalization.CultureInfo.InvariantCulture),
                runtime.CurrentTick.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)),
            "entityExists" => EntityExists(assertion, runtime),
            "entityPositionEquals" => EntityPositionEquals(assertion, runtime),
            "eventOccurred" => EventOccurred(assertion, events),
            _ => new ScenarioAssertion(assertion.Id, Passed: false, Message: $"Unsupported assertion type: {assertion.Type}"),
        };
    }

    private static ScenarioAssertion EntityExists(ScenarioAssertionSource assertion, MinimalRuntime runtime)
    {
        var exists = runtime.TryGetEntityPosition(new EntityId(assertion.EntityId!)) is not null;
        return new ScenarioAssertion(assertion.Id, exists, $"{assertion.EntityId} exists", Expected: "exists", Actual: exists ? "exists" : "missing");
    }

    private static ScenarioAssertion EntityPositionEquals(ScenarioAssertionSource assertion, MinimalRuntime runtime)
    {
        var actual = runtime.TryGetEntityPosition(new EntityId(assertion.EntityId!));
        return new ScenarioAssertion(
            assertion.Id,
            actual == assertion.Position,
            $"{assertion.EntityId} position equals {assertion.Position}",
            assertion.Position?.ToString(System.Globalization.CultureInfo.InvariantCulture),
            actual?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "missing");
    }

    private static ScenarioAssertion EventOccurred(ScenarioAssertionSource assertion, IReadOnlyList<ScenarioEvent> events)
    {
        var occurred = events.Any(scenarioEvent => scenarioEvent.Type == assertion.EventType);
        return new ScenarioAssertion(assertion.Id, occurred, $"{assertion.EventType} event exists", Expected: "occurred", Actual: occurred ? "occurred" : "missing");
    }
}

public sealed record ScenarioRunResult(
    [property: JsonPropertyName("result")] ScenarioResultDocument Result,
    [property: JsonPropertyName("events")] IReadOnlyList<ScenarioEvent> Events,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ScenarioDiagnostic> Diagnostics)
{
    public static ScenarioRunResult InvalidInput(ScenarioSummary scenario, IReadOnlyList<ScenarioDiagnostic> diagnostics)
    {
        return new ScenarioRunResult(
            ScenarioResultDocument.FromExecution(scenario, RuntimeStatus.Error, 2, 0, 0, [], [], [], diagnostics),
            Events: [],
            Diagnostics: diagnostics);
    }

    public static ScenarioRunResult RuntimeError(
        ScenarioSummary scenario,
        int ticksRequested,
        MinimalRuntime runtime,
        IReadOnlyList<ScenarioDiagnostic> diagnostics)
    {
        var events = runtime.Events
            .Select(static (runtimeEvent, index) => new ScenarioEvent(index + 1, runtimeEvent.Tick, runtimeEvent.Type, runtimeEvent.Message))
            .ToArray();

        return new ScenarioRunResult(
            ScenarioResultDocument.FromExecution(
                scenario,
                RuntimeStatus.Error,
                3,
                ticksRequested,
                runtime.CurrentTick.Value,
                events,
                runtime.QueryEntities(),
                [],
                diagnostics),
            Events: events,
            Diagnostics: diagnostics);
    }
}

public sealed record ScenarioResultDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("scenario")] ScenarioSummary Scenario,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("runtime")] ScenarioRuntimeSummary Runtime,
    [property: JsonPropertyName("summary")] ScenarioResultSummary Summary,
    [property: JsonPropertyName("entities")] IReadOnlyList<EntitySummary> Entities,
    [property: JsonPropertyName("assertions")] IReadOnlyList<ScenarioAssertion> Assertions,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ScenarioArtifactReference> Artifacts)
{
    public static ScenarioResultDocument FromExecution(
        ScenarioSummary scenario,
        string status,
        int exitCode,
        int ticksRequested,
        int finalTick,
        IReadOnlyList<ScenarioEvent> events,
        IReadOnlyList<EntitySummary> entities,
        IReadOnlyList<ScenarioAssertion> assertions,
        IReadOnlyList<ScenarioDiagnostic> diagnostics)
    {
        return new ScenarioResultDocument(
            Schema: "agentic2d.scenario.result.v1",
            Scenario: scenario,
            Command: "scenario run",
            Status: status,
            ExitCode: exitCode,
            Runtime: new ScenarioRuntimeSummary(ticksRequested, finalTick),
            Summary: new ScenarioResultSummary(
                EventsEmitted: events.Count,
                AssertionsPassed: assertions.Count(static assertion => assertion.Passed),
                AssertionsFailed: assertions.Count(static assertion => !assertion.Passed),
                Diagnostics: diagnostics.Count),
            Entities: entities,
            Assertions: assertions,
            Artifacts:
            [
                new ScenarioArtifactReference("events.jsonl", "event-log"),
                new ScenarioArtifactReference("diagnostics.json", "diagnostics"),
            ]);
    }
}

public sealed record ScenarioSummary(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("category")] string Category,
    [property: JsonPropertyName("source")] string Source);

public sealed record ScenarioRuntimeSummary(
    [property: JsonPropertyName("ticksRequested")] int TicksRequested,
    [property: JsonPropertyName("finalTick")] int FinalTick);

public sealed record ScenarioResultSummary(
    [property: JsonPropertyName("eventsEmitted")] int EventsEmitted,
    [property: JsonPropertyName("assertionsPassed")] int AssertionsPassed,
    [property: JsonPropertyName("assertionsFailed")] int AssertionsFailed,
    [property: JsonPropertyName("diagnostics")] int Diagnostics);

public sealed record ScenarioArtifactReference(
    [property: JsonPropertyName("path")] string Path,
    [property: JsonPropertyName("kind")] string Kind);

public sealed record ScenarioEvent(
    [property: JsonPropertyName("sequence")] int Sequence,
    [property: JsonPropertyName("tick")] int Tick,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("message")] string Message);

public sealed record ScenarioAssertion(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("passed")] bool Passed,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("expected")] string? Expected = null,
    [property: JsonPropertyName("actual")] string? Actual = null);

public sealed record ScenarioDiagnostic(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("message")] string Message);

public sealed record ScenarioDiagnosticsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ScenarioDiagnostic> Diagnostics);

public sealed class ScenarioSourceResolution
{
    private ScenarioSourceResolution(string? path, string? source, IReadOnlyList<ScenarioDiagnostic> diagnostics)
    {
        Path = path ?? string.Empty;
        Source = source ?? string.Empty;
        Diagnostics = diagnostics;
    }

    public bool IsSuccess => Diagnostics.Count == 0;

    public string Path { get; }

    public string Source { get; }

    public IReadOnlyList<ScenarioDiagnostic> Diagnostics { get; }

    public static ScenarioSourceResolution Success(string path, string source)
    {
        return new ScenarioSourceResolution(path, source, []);
    }

    public static ScenarioSourceResolution Failure(IReadOnlyList<ScenarioDiagnostic> diagnostics)
    {
        return new ScenarioSourceResolution(null, null, diagnostics);
    }
}

public sealed class ScenarioLoadResult
{
    private ScenarioLoadResult(ScenarioSource? sourceScenario, ScenarioSummary scenario, IReadOnlyList<ScenarioDiagnostic> diagnostics)
    {
        SourceScenario = sourceScenario ?? new ScenarioSource();
        Scenario = scenario;
        Diagnostics = diagnostics;
    }

    public bool IsSuccess => Diagnostics.Count == 0;

    public ScenarioSource SourceScenario { get; }

    public ScenarioSummary Scenario { get; }

    public IReadOnlyList<ScenarioDiagnostic> Diagnostics { get; }

    public static ScenarioLoadResult Success(ScenarioSource scenario)
    {
        return new ScenarioLoadResult(scenario, new ScenarioSummary(scenario.Id, scenario.Category, string.Empty), []);
    }

    public static ScenarioLoadResult Failure(ScenarioSummary scenario, IReadOnlyList<ScenarioDiagnostic> diagnostics)
    {
        return new ScenarioLoadResult(null, scenario, diagnostics);
    }
}
