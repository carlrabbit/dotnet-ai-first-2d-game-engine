using Agentic2D.Contracts;
using Agentic2D.Metrics;

namespace Agentic2D.Engine;

public static class RuntimeSmokeScenario
{
    public const string CommandName = "runtime smoke";

    public static RuntimeResult Run(int ticksRequested, MetricsCollectionMode metricsMode = MetricsCollectionMode.Off)
    {
        var runtime = new MinimalRuntime(metricsMode);
        var playerId = EntityId.Player;

        runtime.CreateEntity(playerId, position: 0);

        var moveCommand = new MoveCommand(playerId, Amount: 1);
        var commandResult = runtime.Submit(moveCommand);

        if (!StringComparer.Ordinal.Equals(commandResult.Status, "accepted"))
        {
            return CreateResult(ticksRequested, runtime, RuntimeStatus.Error);
        }

        runtime.Run(ticksRequested, moveCommand);

        var assertions = EvaluateAssertions(ticksRequested, runtime, playerId);
        var status = assertions.All(static assertion => assertion.Passed) && runtime.Diagnostics.All(static diagnostic => diagnostic.Severity != "error")
            ? RuntimeStatus.Passed
            : RuntimeStatus.Failed;

        return CreateResult(ticksRequested, runtime, status, assertions);
    }

    public static RuntimeMetricsSnapshot RunWithMetrics(int ticksRequested, MetricsCollectionMode metricsMode)
    {
        var runtime = new MinimalRuntime(metricsMode);
        var playerId = EntityId.Player;
        runtime.CreateEntity(playerId, 0);
        var command = new MoveCommand(playerId, 1);
        _ = runtime.Submit(command);
        runtime.Run(ticksRequested, command);
        return runtime.Metrics?.Snapshot() ?? RuntimeMetricsSnapshot.Off;
    }

    private static RuntimeResult CreateResult(
        int ticksRequested,
        MinimalRuntime runtime,
        string status,
        IReadOnlyList<RuntimeAssertion>? assertions = null)
    {
        return new RuntimeResult(
            SchemaVersion: 1,
            Command: CommandName,
            Status: status,
            TicksRequested: ticksRequested,
            FinalTick: runtime.CurrentTick.Value,
            Entities: runtime.QueryEntities(),
            Events: runtime.Events,
            Assertions: assertions ?? [],
            Diagnostics: runtime.Diagnostics);
    }

    private static RuntimeAssertion[] EvaluateAssertions(int ticksRequested, MinimalRuntime runtime, EntityId playerId)
    {
        var playerPosition = runtime.TryGetEntityPosition(playerId);

        return
        [
            new RuntimeAssertion("assert.finalTick", runtime.CurrentTick.Value == ticksRequested, "final tick equals requested tick count"),
            new RuntimeAssertion("assert.playerExists", playerPosition is not null, "entity.player exists"),
            new RuntimeAssertion("assert.playerPosition", playerPosition == 1, "entity.player position equals 1"),
            EventAssertion(runtime, "assert.runtimeStartedEvent", "runtime.started"),
            EventAssertion(runtime, "assert.entityCreatedEvent", "entity.created"),
            EventAssertion(runtime, "assert.commandAcceptedEvent", "command.accepted"),
            EventAssertion(runtime, "assert.entityMovedEvent", "entity.moved"),
            EventAssertion(runtime, "assert.runtimeCompletedEvent", "runtime.completed"),
        ];
    }

    private static RuntimeAssertion EventAssertion(MinimalRuntime runtime, string assertionId, string eventType)
    {
        return new RuntimeAssertion(assertionId, runtime.Events.Any(runtimeEvent => runtimeEvent.Type == eventType), $"{eventType} event exists");
    }
}
