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

        await Assert.That(RuntimeResultJson.Serialize(first)).IsEqualTo(RuntimeResultJson.Serialize(second));
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
        var parseResult = RuntimeSmokeCommand.TryParse(["runtime", "smoke", "--ticks", "0", "--output", "artifacts/runtime-smoke"]);

        await Assert.That(parseResult.IsSuccess).IsFalse();
        await Assert.That(parseResult.Error).IsEqualTo("--ticks must be a positive integer");
    }

    [Test]
    public async Task CliParserRequiresOutput()
    {
        var parseResult = RuntimeSmokeCommand.TryParse(["runtime", "smoke", "--ticks", "3"]);

        await Assert.That(parseResult.IsSuccess).IsFalse();
        await Assert.That(parseResult.Error).IsEqualTo("missing required --output <directory>");
    }

    [Test]
    public async Task RuntimeResultSerializationIncludesRequiredShape()
    {
        var result = RuntimeSmokeScenario.Run(ticksRequested: 3);
        var json = RuntimeResultJson.Serialize(result);
        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        await Assert.That(root.GetProperty("schemaVersion").GetInt32()).IsEqualTo(1);
        await Assert.That(root.GetProperty("command").GetString()).IsEqualTo("runtime smoke");
        await Assert.That(root.GetProperty("status").GetString()).IsEqualTo("passed");
        await Assert.That(root.GetProperty("ticksRequested").GetInt32()).IsEqualTo(3);
        await Assert.That(root.GetProperty("finalTick").GetInt32()).IsEqualTo(3);
        await Assert.That(root.GetProperty("entities").GetArrayLength()).IsEqualTo(1);
        await Assert.That(root.GetProperty("events").GetArrayLength()).IsEqualTo(5);
        await Assert.That(root.GetProperty("assertions").GetArrayLength()).IsEqualTo(8);
        await Assert.That(root.GetProperty("diagnostics").GetArrayLength()).IsEqualTo(0);
    }
}
