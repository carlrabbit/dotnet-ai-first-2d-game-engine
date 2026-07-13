using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.Validation;

namespace Agentic2D.ScenarioRunner;

public sealed class RuntimeInspector
{
    public RuntimeInspectionRun Inspect(string scenarioReference, string? mapReference)
    {
        var diagnostics = new List<ContentValidationDiagnostic>();
        var contentReferences = new RuntimeContentReferencesDocument(
            "agentic2d.runtime-inspection.content-references.v1",
            new RuntimeContentReference(string.Empty, string.Empty),
            null,
            []);

        var scenarioResolution = ScenarioSourceResolver.Resolve(scenarioReference);
        if (!scenarioResolution.IsSuccess)
        {
            diagnostics.AddRange(scenarioResolution.Diagnostics.Select(static diagnostic => new ContentValidationDiagnostic("INSPECT0001", diagnostic.Severity, diagnostic.Message, "scenario", null, diagnostic.Id)));
            return RuntimeInspectionRun.From(scenarioReference, mapReference, contentReferences, [], [], [], [], diagnostics, ContentValidationStatus.Failed, 1, 0);
        }

        var scenarioLoad = ScenarioSourceLoader.Load(scenarioResolution.Source, scenarioResolution.Path);
        if (!scenarioLoad.IsSuccess)
        {
            diagnostics.AddRange(scenarioLoad.Diagnostics.Select(static diagnostic => new ContentValidationDiagnostic("INSPECT0001", diagnostic.Severity, diagnostic.Message, "scenario", null, diagnostic.Id)));
            return RuntimeInspectionRun.From(scenarioReference, mapReference, contentReferences, [], [], [], [], diagnostics, ContentValidationStatus.Failed, 1, 0);
        }

        MapContentSource? map = null;
        string? mapPath = null;
        if (!string.IsNullOrWhiteSpace(mapReference))
        {
            var mapResolution = MapInspector.ResolveTarget(mapReference);
            if (!mapResolution.IsSuccess)
            {
                diagnostics.AddRange(mapResolution.Diagnostics.Select(static diagnostic => new ContentValidationDiagnostic("INSPECT0002", diagnostic.Severity, diagnostic.Message, "map", diagnostic.Field, diagnostic.ItemId)));
                contentReferences = new RuntimeContentReferencesDocument(
                    "agentic2d.runtime-inspection.content-references.v1",
                    new RuntimeContentReference(scenarioLoad.SourceScenario.Id, ScenarioRunner.FindRepositoryRoot() == string.Empty ? scenarioReference : ScenarioRunner.FindRepositoryRoot()),
                    null,
                    []);
                return RuntimeInspectionRun.From(scenarioLoad.SourceScenario.Id, mapReference, contentReferences, [], [], [], [], diagnostics, ContentValidationStatus.Failed, 1, 0);
            }

            var mapValidation = new MapContentValidator().ValidateFile(mapResolution.MapPath);
            diagnostics.AddRange(mapValidation.Diagnostics.Select(static diagnostic => new ContentValidationDiagnostic("INSPECT0002", diagnostic.Severity, diagnostic.Message, diagnostic.Target, diagnostic.Field, diagnostic.ItemId)));
            if (mapValidation.Status != ContentValidationStatus.Passed || mapValidation.Map is null)
            {
                return RuntimeInspectionRun.From(scenarioLoad.SourceScenario.Id, mapReference, contentReferences, [], [], [], [], diagnostics, ContentValidationStatus.Failed, 1, 0);
            }

            map = mapValidation.Map;
            mapPath = mapValidation.Path;
        }

        if (M014ScenarioExecutor.IsM014(scenarioLoad.SourceScenario))
        {
            var m014 = M014ScenarioExecutor.Execute(scenarioLoad.SourceScenario);
            var m014Events = (m014.Events ?? []).Select(item => new RuntimeInspectionEventRecord(item.Sequence, item.Tick, item.Type, item.Message)).ToArray();
            var m014Assertions = scenarioLoad.SourceScenario.Assertions.Select(item => new RuntimeInspectionAssertion(item.Id, item.Type != "eventOccurred" || m014Events.Any(e => e.Type == item.EventType), item.Id, null, null)).ToArray();
            var m014Diagnostics = m014.Diagnostics.Select(item => new ContentValidationDiagnostic(item.Id, item.Severity, item.Message, scenarioLoad.SourceScenario.Id)).ToArray();
            return RuntimeInspectionRun.From(scenarioLoad.SourceScenario.Id, map?.Id ?? scenarioLoad.SourceScenario.Runtime!.MapId, BuildContentReferences(scenarioLoad.SourceScenario, scenarioResolution.Path, map, mapPath), m014.Entities, [], m014Events, m014Assertions, m014Diagnostics, m014Diagnostics.Any(x => x.Severity == ContentDiagnosticSeverity.Error) ? ContentValidationStatus.Failed : ContentValidationStatus.Passed, m014Diagnostics.Any(x => x.Severity == ContentDiagnosticSeverity.Error) ? 1 : 0, scenarioLoad.SourceScenario.Runtime!.Ticks, null, null, m014);
        }

        if (ContinuousScenarioExecutor.IsContinuous(scenarioLoad.SourceScenario))
        {
            var continuous = ContinuousScenarioExecutor.Execute(scenarioLoad.SourceScenario);
            var continuousCommands = continuous.Resolutions.Select((item, index) => new RuntimeInspectionCommandRecord(index + 1, item.IntentId, "continuous-move", item.EntityId, (int)Math.Round(item.AppliedX), item.Outcome, item.Outcome)).ToArray();
            var continuousEvents = continuous.Events.Select(item => new RuntimeInspectionEventRecord(item.Sequence, item.Tick, item.Type, item.Message)).ToArray();
            var continuousAssertions = scenarioLoad.SourceScenario.Assertions.Select(item => new RuntimeInspectionAssertion(item.Id, item.Type != "eventOccurred" || continuous.Events.Any(e => e.Type == item.EventType), item.Id, null, null)).ToArray();
            var continuousDiagnostics = continuous.Diagnostics.Select(item => new ContentValidationDiagnostic(item.Id, item.Severity, item.Message, scenarioLoad.SourceScenario.Id)).ToArray();
            return RuntimeInspectionRun.From(scenarioLoad.SourceScenario.Id, map?.Id ?? mapReference, BuildContentReferences(scenarioLoad.SourceScenario, scenarioResolution.Path, map, mapPath), continuous.Entities, continuousCommands, continuousEvents, continuousAssertions, continuousDiagnostics, continuousDiagnostics.Any(x => x.Severity == ContentDiagnosticSeverity.Error) ? ContentValidationStatus.Failed : ContentValidationStatus.Passed, continuousDiagnostics.Any(x => x.Severity == ContentDiagnosticSeverity.Error) ? 1 : 0, scenarioLoad.SourceScenario.Runtime!.Ticks, null, continuous);
        }

        if (BehaviorGridScenarioExecutor.IsBehaviorScenario(scenarioLoad.SourceScenario))
        {
            var behavior = BehaviorGridScenarioExecutor.Execute(scenarioLoad.SourceScenario);
            var behaviorDiagnostics = behavior.Diagnostics.Select(item => new ContentValidationDiagnostic(item.Id, item.Severity, item.Message, scenarioLoad.SourceScenario.Id)).ToArray();
            var behaviorAssertions = scenarioLoad.SourceScenario.Assertions.Select(item => BehaviorGridScenarioExecutor.EvaluateAssertion(item, scenarioLoad.SourceScenario.Runtime!.Ticks, behavior)).Select(item => new RuntimeInspectionAssertion(item.Id, item.Passed, item.Message, item.Expected, item.Actual)).ToArray();
            var behaviorCommands = behavior.Resolutions.Select((item, index) => new RuntimeInspectionCommandRecord(index + 1, item.IntentId, "move", item.IntentId, item.Accepted ? 1 : 0, item.Accepted ? "accepted" : "rejected", item.Reason)).ToArray();
            var behaviorEvents = behavior.Events.Select(item => new RuntimeInspectionEventRecord(item.Sequence, item.Tick, item.Type, item.Message)).ToArray();
            return RuntimeInspectionRun.From(scenarioLoad.SourceScenario.Id, map?.Id ?? mapReference, BuildContentReferences(scenarioLoad.SourceScenario, scenarioResolution.Path, map, mapPath), behavior.Entities, behaviorCommands, behaviorEvents, behaviorAssertions, behaviorDiagnostics, behavior.Diagnostics.Any(item => item.Severity == "error") ? ContentValidationStatus.Failed : ContentValidationStatus.Passed, behavior.Diagnostics.Any(item => item.Severity == "error") ? 1 : 0, scenarioLoad.SourceScenario.Runtime!.Ticks, behavior);
        }

        var runtime = new MinimalRuntime();
        var commandRecords = new List<RuntimeInspectionCommandRecord>();
        try
        {
            foreach (var entity in scenarioLoad.SourceScenario.InitialState!.Entities)
            {
                runtime.CreateEntity(new EntityId(entity.Id), entity.Position);
            }

            var sequence = 1;
            RuntimeCommandResult? lastCommandResult = null;
            MoveCommand? acceptedMove = null;
            foreach (var step in scenarioLoad.SourceScenario.Steps)
            {
                var move = new MoveCommand(new EntityId(step.Command.EntityId), step.Command.Amount);
                var result = runtime.Submit(move);
                lastCommandResult = result;
                commandRecords.Add(new RuntimeInspectionCommandRecord(sequence++, step.Id, step.Command.Type, step.Command.EntityId, step.Command.Amount, result.Status, result.Message));
                if (StringComparer.Ordinal.Equals(result.Status, "accepted"))
                {
                    acceptedMove = move;
                }
            }

            if (acceptedMove is null || lastCommandResult is null || !StringComparer.Ordinal.Equals(lastCommandResult.Status, "accepted"))
            {
                diagnostics.Add(new ContentValidationDiagnostic("INSPECT0004", ContentDiagnosticSeverity.Error, "Command projection or acceptance was inconsistent.", scenarioLoad.SourceScenario.Id));
                return RuntimeInspectionRun.From(scenarioLoad.SourceScenario.Id, map?.Id, BuildContentReferences(scenarioLoad.SourceScenario, scenarioResolution.Path, map, mapPath), runtime.QueryEntities(), commandRecords, ProjectEvents(runtime.Events), [], diagnostics, ContentValidationStatus.Failed, 1, runtime.CurrentTick.Value);
            }

            runtime.Run(scenarioLoad.SourceScenario.Runtime!.Ticks, acceptedMove);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            diagnostics.Add(new ContentValidationDiagnostic("INSPECT0003", ContentDiagnosticSeverity.Error, exception.Message, scenarioLoad.SourceScenario.Id));
            return RuntimeInspectionRun.From(scenarioLoad.SourceScenario.Id, map?.Id, BuildContentReferences(scenarioLoad.SourceScenario, scenarioResolution.Path, map, mapPath), runtime.QueryEntities(), commandRecords, ProjectEvents(runtime.Events), [], diagnostics, ContentValidationStatus.Error, 3, runtime.CurrentTick.Value);
        }

        var events = ProjectEvents(runtime.Events);
        var assertions = ScenarioAssertions.Evaluate(scenarioLoad.SourceScenario, runtime, events.Select(static item => new ScenarioEvent(item.Sequence, item.Tick, item.Type, item.Message)).ToArray())
            .Select(static assertion => new RuntimeInspectionAssertion(assertion.Id, assertion.Passed, assertion.Message, assertion.Expected, assertion.Actual))
            .ToArray();

        var runtimeDiagnostics = runtime.Diagnostics.Select(static diagnostic => new ContentValidationDiagnostic("INSPECT0003", diagnostic.Severity, diagnostic.Message, diagnostic.Code)).ToArray();
        diagnostics.AddRange(runtimeDiagnostics);

        var status = diagnostics.Any(static diagnostic => diagnostic.Severity == ContentDiagnosticSeverity.Error) || assertions.Any(static assertion => !assertion.Passed)
            ? ContentValidationStatus.Failed
            : ContentValidationStatus.Passed;
        var exitCode = status == ContentValidationStatus.Passed ? 0 : 1;
        contentReferences = BuildContentReferences(scenarioLoad.SourceScenario, scenarioResolution.Path, map, mapPath);

        return RuntimeInspectionRun.From(
            scenarioLoad.SourceScenario.Id,
            map?.Id,
            contentReferences,
            runtime.QueryEntities(),
            commandRecords,
            events,
            assertions,
            diagnostics,
            status,
            exitCode,
            runtime.CurrentTick.Value);
    }

    private static RuntimeContentReferencesDocument BuildContentReferences(ScenarioSource scenario, string scenarioPath, MapContentSource? map, string? mapPath)
    {
        return new RuntimeContentReferencesDocument(
            "agentic2d.runtime-inspection.content-references.v1",
            new RuntimeContentReference(scenario.Id, ContentTargetResolver.ToRepositoryRelativePath(scenarioPath)),
            map is null ? null : new RuntimeContentReference(map.Id, mapPath ?? string.Empty),
            map?.AssetRefs.OrderBy(static item => item.AssetId, StringComparer.Ordinal)
                .Select(static asset => new RuntimeContentReference(asset.AssetId, asset.AssetId))
                .ToArray() ?? []);
    }

    private static RuntimeInspectionEventRecord[] ProjectEvents(IReadOnlyList<RuntimeEvent> events)
    {
        return events.Select(static (runtimeEvent, index) => new RuntimeInspectionEventRecord(index + 1, runtimeEvent.Tick, runtimeEvent.Type, runtimeEvent.Message)).ToArray();
    }
}

public static class RuntimeInspectionArtifactWriter
{
    public static async Task<int> WriteAsync(string outputDirectory, RuntimeInspectionRun run)
    {
        try
        {
            Directory.CreateDirectory(outputDirectory);
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "result.json"), JsonSerializer.Serialize(run.Result, ScenarioRunner.JsonOptions));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "diagnostics.json"), JsonSerializer.Serialize(run.DiagnosticsDocument, ScenarioRunner.JsonOptions));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "runtime-summary.json"), JsonSerializer.Serialize(run.RuntimeSummary, ScenarioRunner.JsonOptions));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "entities.json"), JsonSerializer.Serialize(run.EntitiesDocument, ScenarioRunner.JsonOptions));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "final-state.json"), JsonSerializer.Serialize(run.FinalState, ScenarioRunner.JsonOptions));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "assertions.json"), JsonSerializer.Serialize(run.AssertionsDocument, ScenarioRunner.JsonOptions));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "content-references.json"), JsonSerializer.Serialize(run.ContentReferences, ScenarioRunner.JsonOptions));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "entity-registry.json"), JsonSerializer.Serialize(new { schema = "agentic2d.entity-registry.v1", entities = run.EntitiesDocument.Entities.Select(x => x.Id).OrderBy(x => x, StringComparer.Ordinal) }, ScenarioRunner.JsonOptions));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "components.json"), JsonSerializer.Serialize(new { schema = "agentic2d.components.v1", registeredComponentTypeIds = new[] { "component.grid-position", "component.continuous-transform-2d", "component.kinematic-motion-2d", "component.collision-aabb-2d", "component.spatial-membership" }, entities = run.EntitiesDocument.Entities }, ScenarioRunner.JsonOptions));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "component-mutations.jsonl"), M013EvidenceProjection.ComponentMutationsJsonl(run.ContinuousEvidence?.World));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "static-spatial-world.json"), JsonSerializer.Serialize(new { schema = "agentic2d.static-spatial-world.v1", mapId = run.ContentReferences.Map?.Id, mapBounds = run.ContentReferences.Map is null ? null : "map-derived" }, ScenarioRunner.JsonOptions));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "continuous-resolutions.jsonl"), M013EvidenceProjection.ContinuousResolutionsJsonl(run.ContinuousEvidence));
            if (run.M014Evidence is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, "entity-instantiations.jsonl"), JsonLines(run.M014Evidence.Instantiations));
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, "spatial-queries.jsonl"), JsonLines(run.M014Evidence.SpatialQueries));
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, "trigger-transitions.jsonl"), JsonLines(run.M014Evidence.TriggerTransitions));
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, "interaction-resolutions.jsonl"), JsonLines(run.M014Evidence.InteractionResolutions));
            }

            if (run.BehaviorEvidence is not null)
            {
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, "behaviors.json"), JsonSerializer.Serialize(new { schema = "agentic2d.behavior-execution.v1", assignments = run.BehaviorEvidence.Behaviors, finalGridPositions = run.BehaviorEvidence.GridPositions }, ScenarioRunner.JsonOptions));
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, "intents.jsonl"), string.Join(Environment.NewLine, run.BehaviorEvidence.Intents.Select(item => JsonSerializer.Serialize(item, ScenarioRunner.JsonLineOptions))) + (run.BehaviorEvidence.Intents.Count == 0 ? string.Empty : Environment.NewLine));
                await File.WriteAllTextAsync(Path.Combine(outputDirectory, "spatial-resolutions.jsonl"), string.Join(Environment.NewLine, run.BehaviorEvidence.Resolutions.Select(item => JsonSerializer.Serialize(item, ScenarioRunner.JsonLineOptions))) + (run.BehaviorEvidence.Resolutions.Count == 0 ? string.Empty : Environment.NewLine));
            }

            var commandsJsonl = run.CommandsDocument.Commands.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine, run.CommandsDocument.Commands.Select(command => JsonSerializer.Serialize(command, ScenarioRunner.JsonLineOptions))) + Environment.NewLine;
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "commands.jsonl"), commandsJsonl);

            var eventsJsonl = run.EventsDocument.Events.Count == 0
                ? string.Empty
                : string.Join(Environment.NewLine, run.EventsDocument.Events.Select(command => JsonSerializer.Serialize(command, ScenarioRunner.JsonLineOptions))) + Environment.NewLine;
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "events.jsonl"), eventsJsonl);

            return run.Result.ExitCode;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            throw new IOException($"failed to write runtime inspection artifacts: {exception.Message}", exception);
        }
    }

    private static string JsonLines(IEnumerable<object> records)
    {
        var lines = records.Select(item => JsonSerializer.Serialize(item, ScenarioRunner.JsonLineOptions)).ToArray();
        return lines.Length == 0 ? string.Empty : string.Join(Environment.NewLine, lines) + Environment.NewLine;
    }
}

public sealed record RuntimeInspectionRun(
    RuntimeInspectionResultDocument Result,
    RuntimeInspectionDiagnosticsDocument DiagnosticsDocument,
    RuntimeInspectionSummaryDocument RuntimeSummary,
    RuntimeInspectionEntitiesDocument EntitiesDocument,
    RuntimeInspectionCommandsDocument CommandsDocument,
    RuntimeInspectionEventsDocument EventsDocument,
    RuntimeInspectionFinalStateDocument FinalState,
    RuntimeInspectionAssertionsDocument AssertionsDocument,
    RuntimeContentReferencesDocument ContentReferences,
    BehaviorExecutionEvidence? BehaviorEvidence = null,
    ContinuousScenarioExecutor.ContinuousExecution? ContinuousEvidence = null,
    M014Execution? M014Evidence = null)
{
    public static RuntimeInspectionRun From(
        string scenarioId,
        string? mapId,
        RuntimeContentReferencesDocument contentReferences,
        IReadOnlyList<EntitySummary> entities,
        IReadOnlyList<RuntimeInspectionCommandRecord> commands,
        IReadOnlyList<RuntimeInspectionEventRecord> events,
        IReadOnlyList<RuntimeInspectionAssertion> assertions,
        IReadOnlyList<ContentValidationDiagnostic> diagnostics,
        string status,
        int exitCode,
        int finalTick,
        BehaviorExecutionEvidence? behaviorEvidence = null,
        ContinuousScenarioExecutor.ContinuousExecution? continuousEvidence = null,
        M014Execution? m014Evidence = null)
    {
        var artifacts = new List<ContentArtifactReference>
        {
            new ContentArtifactReference("diagnostics.json", "diagnostics"),
            new ContentArtifactReference("runtime-summary.json", "runtime-summary"),
            new ContentArtifactReference("entities.json", "entities"),
            new ContentArtifactReference("commands.jsonl", "commands"),
            new ContentArtifactReference("events.jsonl", "events"),
            new ContentArtifactReference("final-state.json", "final-state"),
            new ContentArtifactReference("assertions.json", "assertions"),
            new ContentArtifactReference("content-references.json", "content-references"),
            new ContentArtifactReference("entity-registry.json", "entity-registry"),
            new ContentArtifactReference("components.json", "components"),
            new ContentArtifactReference("component-mutations.jsonl", "component-mutations"),
            new ContentArtifactReference("static-spatial-world.json", "static-spatial-world"),
            new ContentArtifactReference("continuous-resolutions.jsonl", "continuous-resolutions"),
        };
        if (m014Evidence is not null)
        {
            artifacts.Add(new("entity-instantiations.jsonl", "entity-instantiations"));
            artifacts.Add(new("spatial-queries.jsonl", "spatial-queries"));
            artifacts.Add(new("trigger-transitions.jsonl", "trigger-transitions"));
            artifacts.Add(new("interaction-resolutions.jsonl", "interaction-resolutions"));
        }

        return new RuntimeInspectionRun(
            new RuntimeInspectionResultDocument(
                "agentic2d.runtime-inspection.result.v1",
                "runtime inspect",
                scenarioId,
                mapId,
                status,
                exitCode,
                new RuntimeInspectionSummary(finalTick, entities.Count, commands.Count, events.Count, assertions.Count(static assertion => assertion.Passed), assertions.Count(static assertion => !assertion.Passed), diagnostics.Count),
                diagnostics,
                artifacts),
            new RuntimeInspectionDiagnosticsDocument("agentic2d.runtime-inspection.diagnostics.v1", diagnostics),
            new RuntimeInspectionSummaryDocument("agentic2d.runtime-inspection.summary.v1", scenarioId, mapId, finalTick, entities.Count, commands.Count, events.Count, status == ContentValidationStatus.Passed),
            new RuntimeInspectionEntitiesDocument("agentic2d.runtime-inspection.entities.v1", scenarioId, entities),
            new RuntimeInspectionCommandsDocument("agentic2d.runtime-inspection.commands.v1", commands),
            new RuntimeInspectionEventsDocument("agentic2d.runtime-inspection.events.v1", events),
            new RuntimeInspectionFinalStateDocument("agentic2d.runtime-inspection.final-state.v1", finalTick, entities),
            new RuntimeInspectionAssertionsDocument("agentic2d.runtime-inspection.assertions.v1", assertions),
            contentReferences,
            behaviorEvidence,
            continuousEvidence,
            m014Evidence);
    }
}

public sealed record RuntimeInspectionResultDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("scenarioId")] string ScenarioId,
    [property: JsonPropertyName("mapId")] string? MapId,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("exitCode")] int ExitCode,
    [property: JsonPropertyName("summary")] RuntimeInspectionSummary Summary,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ContentArtifactReference> Artifacts);

public sealed record RuntimeInspectionSummary(
    [property: JsonPropertyName("finalTick")] int FinalTick,
    [property: JsonPropertyName("entities")] int Entities,
    [property: JsonPropertyName("commands")] int Commands,
    [property: JsonPropertyName("events")] int Events,
    [property: JsonPropertyName("assertionsPassed")] int AssertionsPassed,
    [property: JsonPropertyName("assertionsFailed")] int AssertionsFailed,
    [property: JsonPropertyName("diagnostics")] int Diagnostics);

public sealed record RuntimeInspectionSummaryDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("scenarioId")] string ScenarioId,
    [property: JsonPropertyName("mapId")] string? MapId,
    [property: JsonPropertyName("finalTick")] int FinalTick,
    [property: JsonPropertyName("entityCount")] int EntityCount,
    [property: JsonPropertyName("commandCount")] int CommandCount,
    [property: JsonPropertyName("eventCount")] int EventCount,
    [property: JsonPropertyName("completed")] bool Completed);

public sealed record RuntimeInspectionEntitiesDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("scenarioId")] string ScenarioId,
    [property: JsonPropertyName("entities")] IReadOnlyList<EntitySummary> Entities);

public sealed record RuntimeInspectionCommandsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("commands")] IReadOnlyList<RuntimeInspectionCommandRecord> Commands);

public sealed record RuntimeInspectionCommandRecord(
    [property: JsonPropertyName("sequence")] int Sequence,
    [property: JsonPropertyName("stepId")] string StepId,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("entityId")] string EntityId,
    [property: JsonPropertyName("amount")] int Amount,
    [property: JsonPropertyName("outcome")] string Outcome,
    [property: JsonPropertyName("message")] string Message);

public sealed record RuntimeInspectionEventsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("events")] IReadOnlyList<RuntimeInspectionEventRecord> Events);

public sealed record RuntimeInspectionEventRecord(
    [property: JsonPropertyName("sequence")] int Sequence,
    [property: JsonPropertyName("tick")] int Tick,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("message")] string Message);

public sealed record RuntimeInspectionFinalStateDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("finalTick")] int FinalTick,
    [property: JsonPropertyName("entities")] IReadOnlyList<EntitySummary> Entities);

public sealed record RuntimeInspectionAssertionsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("assertions")] IReadOnlyList<RuntimeInspectionAssertion> Assertions);

public sealed record RuntimeInspectionAssertion(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("passed")] bool Passed,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("expected")] string? Expected,
    [property: JsonPropertyName("actual")] string? Actual);

public sealed record RuntimeContentReferencesDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("scenario")] RuntimeContentReference Scenario,
    [property: JsonPropertyName("map")] RuntimeContentReference? Map,
    [property: JsonPropertyName("assets")] IReadOnlyList<RuntimeContentReference> Assets);

public sealed record RuntimeContentReference(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("path")] string Path);

public sealed record RuntimeInspectionDiagnosticsDocument(
    [property: JsonPropertyName("schema")] string Schema,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<ContentValidationDiagnostic> Diagnostics);
