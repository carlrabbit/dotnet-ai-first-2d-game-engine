using System.Text.Json;
using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.Tools;

namespace Agentic2D.Tests.Unit;

public sealed class RuntimeSmokeTests
{
    [Test]
    public async Task RuntimeSmokeScenarioProducesDeterministicResult()
    {
        var first = RuntimeSmokeScenario.Run(ticksRequested: 3);
        var second = RuntimeSmokeScenario.Run(ticksRequested: 3);

        await Assert.That(System.Text.Json.JsonSerializer.Serialize(first)).IsEqualTo(System.Text.Json.JsonSerializer.Serialize(second));
        await Assert.That(first.Status).IsEqualTo(RuntimeStatus.Passed);
        await Assert.That(first.FinalTick).IsEqualTo(3);
        await Assert.That(first.Entities.Single(entity => entity.Id == "entity.player").Position).IsEqualTo(1);
    }

    [Test]
    public async Task RuntimeSmokeScenarioEmitsExpectedEventsInOrder()
    {
        var result = RuntimeSmokeScenario.Run(ticksRequested: 3);
        var eventTypes = result.Events.Select(runtimeEvent => runtimeEvent.Type).ToArray();

        await Assert.That(string.Join(",", eventTypes)).IsEqualTo("runtime.started,entity.created,command.accepted,entity.moved,runtime.completed");
        await Assert.That(result.Events.Single(runtimeEvent => runtimeEvent.Type == "entity.moved").Tick).IsEqualTo(1);
    }

    [Test]
    public async Task RuntimeSmokeScenarioEvaluatesRequiredAssertions()
    {
        var result = RuntimeSmokeScenario.Run(ticksRequested: 3);
        var assertionIds = result.Assertions.Select(assertion => assertion.Id).ToArray();

        await Assert.That(result.Assertions.All(assertion => assertion.Passed)).IsTrue();
        await Assert.That(assertionIds).Contains("assert.finalTick");
        await Assert.That(assertionIds).Contains("assert.playerExists");
        await Assert.That(assertionIds).Contains("assert.playerPosition");
        await Assert.That(assertionIds).Contains("assert.runtimeStartedEvent");
        await Assert.That(assertionIds).Contains("assert.entityCreatedEvent");
        await Assert.That(assertionIds).Contains("assert.commandAcceptedEvent");
        await Assert.That(assertionIds).Contains("assert.entityMovedEvent");
        await Assert.That(assertionIds).Contains("assert.runtimeCompletedEvent");
    }

    [Test]
    public async Task RuntimeRejectsInvalidCommandWithDiagnostic()
    {
        var runtime = new MinimalRuntime();
        var commandResult = runtime.Submit(new MoveCommand(EntityId.Player, Amount: 1));

        await Assert.That(commandResult.Status).IsEqualTo("rejected");
        await Assert.That(runtime.Diagnostics.Single().Code).IsEqualTo("runtime.entityNotFound");
    }

    [Test]
    public async Task CliParserRejectsInvalidTickInput()
    {
        var parseResult = ToolsCliParser.TryParse(["runtime", "smoke", "--ticks", "0", "--output", "artifacts/runtime-smoke"]);

        await Assert.That(parseResult.IsSuccess).IsFalse();
        await Assert.That(parseResult.Error).IsEqualTo("--ticks must be a positive integer");
    }

    [Test]
    public async Task CliParserRequiresOutput()
    {
        var parseResult = ToolsCliParser.TryParse(["runtime", "smoke", "--ticks", "3"]);

        await Assert.That(parseResult.IsSuccess).IsFalse();
        await Assert.That(parseResult.Error).IsEqualTo("missing required --output <directory>");
    }

    [Test]
    public async Task CliParserParsesValidateCommand()
    {
        var parseResult = ToolsCliParser.TryParse(["validate", "--output", "artifacts/cli/validate"]);

        await Assert.That(parseResult.IsSuccess).IsTrue();
        await Assert.That(parseResult.Command.Name).IsEqualTo("validate");
        await Assert.That(parseResult.Command.OutputDirectory).IsEqualTo("artifacts/cli/validate");
    }

    [Test]
    public async Task ProductCliResultSerializationIncludesRequiredShape()
    {
        var result = RuntimeSmokeScenario.Run(ticksRequested: 3);
        var json = ProductCliResultJson.Serialize(ProductCliResultJson.FromRuntimeResult("runtime smoke", result));
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        await Assert.That(root.GetProperty("schema").GetString()).IsEqualTo("agentic2d.product-cli.result.v1");
        await Assert.That(root.GetProperty("command").GetString()).IsEqualTo("runtime smoke");
        await Assert.That(root.GetProperty("status").GetString()).IsEqualTo("passed");
        await Assert.That(root.GetProperty("exitCode").GetInt32()).IsEqualTo(0);
        await Assert.That(root.GetProperty("diagnostics").GetArrayLength()).IsEqualTo(0);
        await Assert.That(root.GetProperty("artifacts").GetArrayLength()).IsEqualTo(0);
        await Assert.That(root.GetProperty("runtime").GetProperty("ticksExecuted").GetInt32()).IsEqualTo(3);
        await Assert.That(root.GetProperty("runtime").GetProperty("eventsEmitted").GetInt32()).IsEqualTo(5);
    }

    [Test]
    public async Task RuntimeSmokeCommandWritesResultArtifactAndReturnsSuccess()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "agentic2d-tests", Guid.NewGuid().ToString("N"));
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["runtime", "smoke", "--output", outputDirectory], stdout, stderr);
        var resultPath = Path.Combine(outputDirectory, "result.json");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(resultPath)).IsTrue();
        await Assert.That(stdout.ToString()).Contains(resultPath);
        await Assert.That(stderr.ToString()).IsEqualTo(string.Empty);

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(resultPath));
        await Assert.That(document.RootElement.GetProperty("command").GetString()).IsEqualTo("runtime smoke");
        await Assert.That(document.RootElement.GetProperty("status").GetString()).IsEqualTo("passed");
    }

    [Test]
    public async Task ValidateCommandWritesResultArtifactAndReturnsSuccess()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "agentic2d-tests", Guid.NewGuid().ToString("N"));
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["validate", "--output", outputDirectory], stdout, stderr);
        var resultPath = Path.Combine(outputDirectory, "result.json");

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(resultPath)).IsTrue();

        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(resultPath));
        await Assert.That(document.RootElement.GetProperty("command").GetString()).IsEqualTo("validate");
        await Assert.That(document.RootElement.GetProperty("status").GetString()).IsEqualTo("passed");
    }

    [Test]
    public async Task InvalidUsageReturnsUsageExitCode()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["asset", "inspect"], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(stdout.ToString()).IsEqualTo(string.Empty);
        await Assert.That(stderr.ToString()).Contains("missing required asset ID or path");
    }
}
