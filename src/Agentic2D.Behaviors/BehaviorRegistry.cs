using Agentic2D.Contracts;

namespace Agentic2D.Behaviors;

public sealed class BehaviorRegistry : IBehaviorRegistry
{
    private readonly IReadOnlyDictionary<string, IBehaviorModule> modules = new Dictionary<string, IBehaviorModule>(StringComparer.Ordinal)
    {
        [PlayerMoveEastBehavior.BehaviorId] = new PlayerMoveEastBehavior(),
        [PlayerMoveEastContinuousBehavior.BehaviorId] = new PlayerMoveEastContinuousBehavior(),
        [PlayerInteractBehavior.BehaviorId] = new PlayerInteractBehavior(),
    };

    public IReadOnlyList<string> RegisteredIds => modules.Keys.Order(StringComparer.Ordinal).ToArray();
    public bool TryGet(string behaviorId, out IBehaviorModule? behavior) => modules.TryGetValue(behaviorId, out behavior);
}

public sealed class PlayerMoveEastBehavior : IBehaviorModule
{
    public const string BehaviorId = "behavior.player-move-east";
    public string Id => BehaviorId;
    public void Execute(BehaviorContext context) => context.Intents.Emit(new MoveIntent($"intent.{context.AssignmentId}.east.tick-{context.Snapshot.Tick}", context.AssignmentId, Id, context.EntityId, "east", $"{context.EntityId}:{context.AssignmentId}"));
}

public sealed class PlayerMoveEastContinuousBehavior : IBehaviorModule
{
    public const string BehaviorId = "behavior.player-move-east-continuous";
    public string Id => BehaviorId;
    public void Execute(BehaviorContext context) => context.Intents.Emit(new ContinuousMoveIntent("intent." + context.AssignmentId + ".east.tick-" + context.Snapshot.Tick, context.AssignmentId, Id, context.EntityId, 1d, 0d, context.EntityId + ":" + context.AssignmentId));
}

public sealed class PlayerInteractBehavior : IBehaviorModule
{
    public const string BehaviorId = "behavior.player-interact";
    public string Id => BehaviorId;
    public void Execute(BehaviorContext context) => context.Intents.Emit(new InteractIntent("intent." + context.AssignmentId + ".interact.tick-" + context.Snapshot.Tick, context.EntityId, null, "interaction.talk", context.AssignmentId, context.EntityId + ":" + context.AssignmentId));
}
