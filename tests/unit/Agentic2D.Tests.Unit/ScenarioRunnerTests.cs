using System.Text.Json;
using Agentic2D.Contracts;
using Agentic2D.ScenarioRunner;
using Agentic2D.Tools;

namespace Agentic2D.Tests.Unit;

public sealed class ScenarioRunnerTests
{
    private const string SmokeScenarioPath = "game/scenarios/smoke/runtime-smoke.json";

    [Test]
    public async Task AuthoredSmokeScenarioCanBeLoaded()
    {
        var source = await File.ReadAllTextAsync(RepositoryPath(SmokeScenarioPath));
        var loadResult = ScenarioSourceLoader.Load(source, SmokeScenarioPath);

        await Assert.That(loadResult.IsSuccess).IsTrue();
        await Assert.That(loadResult.SourceScenario.Id).IsEqualTo("runtime.smoke");
        await Assert.That(loadResult.SourceScenario.Category).IsEqualTo("smoke");
        await Assert.That(loadResult.SourceScenario.Runtime!.Ticks).IsEqualTo(3);
        await Assert.That(loadResult.SourceScenario.InitialState!.Entities.Single().Id).IsEqualTo("entity.player");
    }

    [Test]
    public async Task ScenarioValidationProducesStableDiagnosticsForMissingRequiredFields()
    {
        const string source = """
            {
              "schema": "agentic2d.scenario.v1",
              "category": "smoke",
              "title": "Broken",
              "purpose": "Validate diagnostics.",
              "seedPolicy": "none",
              "runtime": { "ticks": 3 },
              "initialState": { "entities": [{ "id": "entity.player", "position": 0 }] },
              "steps": [{ "id": "step.move-player", "command": { "type": "move", "entityId": "entity.player", "amount": 1 } }],
              "expectedEvents": ["runtime.started"],
              "assertions": [{ "id": "assert.finalTick", "type": "finalTickEqualsRequested" }],
              "artifacts": { "result": "result.json", "events": "events.jsonl", "diagnostics": "diagnostics.json" },
              "humanReview": { "required": false }
            }
            """;

        var loadResult = ScenarioSourceLoader.Load(source, "broken.json");

        await Assert.That(loadResult.IsSuccess).IsFalse();
        await Assert.That(loadResult.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("SCENARIO0001");
    }

    [Test]
    public async Task ScenarioValidationRejectsMalformedScenarioInputWithStableDiagnostic()
    {
        var loadResult = ScenarioSourceLoader.Load("{", "broken.json");

        await Assert.That(loadResult.IsSuccess).IsFalse();
        await Assert.That(loadResult.Diagnostics.Single().Id).IsEqualTo("SCENARIO0000");
    }

    [Test]
    public async Task ScenarioReferenceResolutionIsDeterministicForIdAndPath()
    {
        var byPath = new ScenarioRunner.ScenarioRunner().Run(SmokeScenarioPath);
        var byId = new ScenarioRunner.ScenarioRunner().Run("runtime.smoke");

        await Assert.That(byPath.Result.Scenario.Id).IsEqualTo("runtime.smoke");
        await Assert.That(byId.Result.Scenario.Id).IsEqualTo("runtime.smoke");
        await Assert.That(byPath.Result.Status).IsEqualTo(byId.Result.Status);
        await Assert.That(byPath.Events.Select(static scenarioEvent => scenarioEvent.Type).ToArray())
            .IsEquivalentTo(byId.Events.Select(static scenarioEvent => scenarioEvent.Type).ToArray());
    }

    [Test]
    public async Task ScenarioRunnerProducesDeterministicRuntimeEvidence()
    {
        var runner = new ScenarioRunner.ScenarioRunner();

        var first = runner.Run(SmokeScenarioPath);
        var second = runner.Run(SmokeScenarioPath);

        await Assert.That(JsonSerializer.Serialize(first.Result, ScenarioRunner.ScenarioRunner.JsonOptions))
            .IsEqualTo(JsonSerializer.Serialize(second.Result, ScenarioRunner.ScenarioRunner.JsonOptions));
        await Assert.That(first.Result.Status).IsEqualTo(RuntimeStatus.Passed);
        await Assert.That(first.Result.Runtime.TicksRequested).IsEqualTo(3);
        await Assert.That(first.Result.Runtime.FinalTick).IsEqualTo(3);
        await Assert.That(first.Result.Entities.Single(entity => entity.Id == "entity.player").Position).IsEqualTo(1);
        await Assert.That(first.Result.Assertions.All(static assertion => assertion.Passed)).IsTrue();
    }

    [Test]
    public async Task ScenarioRunnerEmitsRequiredEventsInDeterministicOrder()
    {
        var result = new ScenarioRunner.ScenarioRunner().Run(SmokeScenarioPath);
        var eventTypes = result.Events.Select(static scenarioEvent => scenarioEvent.Type).ToArray();

        await Assert.That(string.Join(",", eventTypes)).IsEqualTo("runtime.started,entity.created,command.accepted,entity.moved,runtime.completed");
        await Assert.That(result.Events.Single(scenarioEvent => scenarioEvent.Type == "entity.moved").Tick).IsEqualTo(1);
    }

    [Test]
    public async Task ScenarioArtifactsAreWrittenWithContractShape()
    {
        var outputDirectory = CreateTempDirectory();
        var result = new ScenarioRunner.ScenarioRunner().Run(SmokeScenarioPath);

        var exitCode = await ScenarioArtifactWriter.WriteAsync(outputDirectory, result);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "result.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "events.jsonl"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();

        using var resultDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "result.json")));
        var root = resultDocument.RootElement;
        await Assert.That(root.GetProperty("schema").GetString()).IsEqualTo("agentic2d.scenario.result.v1");
        await Assert.That(root.GetProperty("scenario").GetProperty("id").GetString()).IsEqualTo("runtime.smoke");
        await Assert.That(root.GetProperty("artifacts")[0].GetProperty("path").GetString()).IsEqualTo("events.jsonl");
        await Assert.That(Path.IsPathRooted(root.GetProperty("artifacts")[0].GetProperty("path").GetString()!)).IsFalse();

        var eventLines = (await File.ReadAllLinesAsync(Path.Combine(outputDirectory, "events.jsonl")))
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        await Assert.That(eventLines.Length).IsEqualTo(5);
        foreach (var line in eventLines)
        {
            using var eventDocument = JsonDocument.Parse(line);
            await Assert.That(eventDocument.RootElement.TryGetProperty("sequence", out _)).IsTrue();
        }

        using var diagnosticsDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "diagnostics.json")));
        await Assert.That(diagnosticsDocument.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.diagnostics.v1");
        await Assert.That(diagnosticsDocument.RootElement.GetProperty("diagnostics").GetArrayLength()).IsEqualTo(0);
    }

    [Test]
    public async Task ScenarioRunCommandSucceedsForValidSmokeScenario()
    {
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["scenario", "run", SmokeScenarioPath, "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "result.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "events.jsonl"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();
        await Assert.That(stderr.ToString()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ScenarioRunCommandRequiresOutput()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["scenario", "run", SmokeScenarioPath], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(stdout.ToString()).IsEqualTo(string.Empty);
        await Assert.That(stderr.ToString()).Contains("missing required --output");
    }

    [Test]
    public async Task ScenarioRunCommandReturnsInvalidInputForUnknownScenario()
    {
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["scenario", "run", "unknown.smoke", "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();

        using var diagnosticsDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "diagnostics.json")));
        await Assert.That(diagnosticsDocument.RootElement.GetProperty("diagnostics")[0].GetProperty("id").GetString()).IsEqualTo("SCENARIO0008");
    }

    [Test]
    public async Task ScenarioRunCommandReturnsInvalidInputForInvalidScenarioFile()
    {
        var scenarioPath = Path.Combine(CreateTempDirectory(), "invalid.json");
        await File.WriteAllTextAsync(scenarioPath, """{"schema":"agentic2d.scenario.v1"}""");
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["scenario", "run", scenarioPath, "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(2);

        using var diagnosticsDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "diagnostics.json")));
        await Assert.That(diagnosticsDocument.RootElement.GetProperty("diagnostics").GetArrayLength()).IsGreaterThan(0);
    }

    [Test]
    public async Task ScenarioRunCommandReturnsRuntimeErrorForArtifactWriteFailure()
    {
        var outputFile = Path.Combine(Path.GetTempPath(), "agentic2d-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);
        await File.WriteAllTextAsync(outputFile, "not a directory");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["scenario", "run", SmokeScenarioPath, "--output", outputFile], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(3);
        await Assert.That(stderr.ToString()).Contains("failed to write scenario artifacts");
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "agentic2d-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string RepositoryPath(string relativePath)
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(directory))
        {
            if (File.Exists(Path.Combine(directory, "dotnet-ai-first-2d-game-engine.slnx")))
            {
                return Path.Combine(directory, relativePath);
            }

            directory = Directory.GetParent(directory)?.FullName;
        }

        return relativePath;
    }
}
