namespace Agentic2D.Contracts;

/// <summary>Read-only, phase-scoped world data. Spatial representations remain module-owned.</summary>
public sealed record BehaviorSnapshot(int Tick, string Fingerprint, IReadOnlySet<string> EntityIds);

public sealed record MoveIntent(string Id, string AssignmentId, string BehaviorId, string EntityId, string Direction, string OrderingKey);

public interface IIntentEmitter
{
    void Emit(MoveIntent intent);
}

public interface IDeterministicRandom
{
    int NextInt(int minimumInclusive, int maximumExclusive);
}

public sealed class ScenarioRandomSource : IDeterministicRandom
{
    private readonly Random random;
    public ScenarioRandomSource(int seed) => random = new Random(seed);
    public int NextInt(int minimumInclusive, int maximumExclusive) => random.Next(minimumInclusive, maximumExclusive);
}

public sealed record BehaviorContext(BehaviorSnapshot Snapshot, string AssignmentId, string EntityId, IDeterministicRandom Random, IIntentEmitter Intents);

public interface IBehaviorModule { string Id { get; } void Execute(BehaviorContext context); }
public interface IBehaviorRegistry { bool TryGet(string behaviorId, out IBehaviorModule? behavior); IReadOnlyList<string> RegisteredIds { get; } }

/// <summary>Narrow engine-facing spatial outcome; no grid coordinate or tile API leaks here.</summary>
public sealed record SpatialResolution(string IntentId, string ModuleId, string EntityId, bool Accepted, string Reason, string? CommandId, IReadOnlyList<string> Events, IReadOnlyList<string> Diagnostics);
public interface ISpatialResolver { string Id { get; } SpatialResolution Resolve(MoveIntent intent); }
