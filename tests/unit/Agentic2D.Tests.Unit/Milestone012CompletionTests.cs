using System.Text.Json;
using Agentic2D.ScenarioRunner;

namespace Agentic2D.Tests.Unit;

public sealed class Milestone012CompletionTests
{
    [Test]
    public async Task OnceExecutesOnceAndEachTickExecutesForEveryTickWithFreshSnapshots()
    {
        var once = new RuntimeInspector().Inspect(WriteScenario("once"), "map.smoke");
        var eachTick = new RuntimeInspector().Inspect(WriteScenario("each-tick"), "map.smoke");

        await Assert.That(once.BehaviorEvidence!.Behaviors).Count().IsEqualTo(1);
        await Assert.That(eachTick.BehaviorEvidence!.Behaviors).Count().IsEqualTo(3);
        await Assert.That(eachTick.BehaviorEvidence.Behaviors.Select(item => item.ExecutionTick)).IsEquivalentTo([1, 2, 3]);
        await Assert.That(eachTick.BehaviorEvidence.Behaviors.Select(item => item.SnapshotFingerprint).Distinct()).Count().IsEqualTo(3);
        await Assert.That(eachTick.BehaviorEvidence.Intents).Count().IsEqualTo(3);
        await Assert.That(eachTick.BehaviorEvidence.Resolutions.Select(item => item.Accepted)).IsEquivalentTo([true, false, false]);
    }

    [Test]
    public async Task RepeatedEachTickRunsProduceEquivalentBehaviorEvidence()
    {
        var scenario = WriteScenario("each-tick");
        var first = new RuntimeInspector().Inspect(scenario, "map.smoke");
        var second = new RuntimeInspector().Inspect(scenario, "map.smoke");

        await Assert.That(JsonSerializer.Serialize(first.BehaviorEvidence)).IsEqualTo(JsonSerializer.Serialize(second.BehaviorEvidence));
        await Assert.That(first.EventsDocument.Events.Select(item => item.Type)).IsEquivalentTo(second.EventsDocument.Events.Select(item => item.Type));
    }

    private static string WriteScenario(string lifecycle)
    {
        var path = Path.Combine(Path.GetTempPath(), "agentic2d-m012-" + Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, """
        { "schema":"agentic2d.scenario.v1", "id":"behavior.lifecycle-{{lifecycle}}", "category":"smoke", "title":"Lifecycle", "purpose":"Focused lifecycle test", "seedPolicy":"scenario", "runtime":{"ticks":3,"spatialModule":"spatial.grid","mapId":"map.smoke","randomSeed":12}, "initialState":{"entities":[{"id":"entity.player","position":0,"gridPosition":{"x":0,"y":0}}]}, "steps":[], "behaviors":[{"id":"assignment.player","entityId":"entity.player","behaviorId":"behavior.player-move-east","lifecycle":"{{lifecycle}}"}], "expectedEvents":["behavior.started"], "assertions":[{"id":"assert.tick","type":"finalTickEqualsRequested"}], "artifacts":{"result":"result.json","events":"events.jsonl","diagnostics":"diagnostics.json"}, "humanReview":{"required":false} }
        """.Replace("{{lifecycle}}", lifecycle));
        return path;
    }
}
