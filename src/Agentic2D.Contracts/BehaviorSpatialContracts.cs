namespace Agentic2D.Contracts;

/// <summary>Read-only typed view exposed to evaluators for one phase.</summary>
public interface IRuntimeSnapshotView
{
    int Tick { get; }
    string Fingerprint { get; }
    IReadOnlyList<string> EntityIds { get; }
    bool Exists(string entityId);
    bool TryGet<T>(string entityId, out T? value) where T : notnull;
    bool TryGetByTypeId<T>(string entityId, string typeId, out T? value) where T : notnull;
    IReadOnlyList<string> Query<T>() where T : notnull;
    IReadOnlyList<string> QueryByTypeId(string typeId);
}

/// <summary>Read-only, phase-scoped world data. Spatial representations remain module-owned.</summary>
public sealed record BehaviorSnapshot(int Tick, string Fingerprint, IReadOnlySet<string> EntityIds)
{
    public IRuntimeSnapshotView? Runtime { get; init; }
    public BehaviorSnapshot(IRuntimeSnapshotView runtime) : this(runtime.Tick, runtime.Fingerprint, runtime.EntityIds.ToHashSet(StringComparer.Ordinal)) => Runtime = runtime;
}

public sealed record MoveIntent(string Id, string AssignmentId, string BehaviorId, string EntityId, string Direction, string OrderingKey);
public sealed record ContinuousMoveIntent(string Id, string AssignmentId, string BehaviorId, string EntityId, double DirectionX, double DirectionY, string OrderingKey);
public sealed record InteractIntent(string Id, string InteractorEntityId, string? ExplicitTargetEntityId, string? RequestedInteractionKind, string BehaviorAssignmentId, string OrderingKey);

public interface IIntentEmitter
{
    void Emit(MoveIntent intent);
    void Emit(ContinuousMoveIntent intent);
    void Emit(InteractIntent intent);
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

public interface IBehaviorInput
{
    double Scalar(string actionId);
    (double X, double Y) Vector2(string actionId);
    string DigitalPhase(string actionId);
}

public sealed record BehaviorContext(BehaviorSnapshot Snapshot, string AssignmentId, string EntityId, IDeterministicRandom Random, IIntentEmitter Intents, IBehaviorInput? Input = null);

public interface IBehaviorModule { string Id { get; } void Execute(BehaviorContext context); }
public interface IBehaviorRegistry { bool TryGet(string behaviorId, out IBehaviorModule? behavior); IReadOnlyList<string> RegisteredIds { get; } }

/// <summary>Narrow engine-facing spatial outcome; no grid coordinate or tile API leaks here.</summary>
public sealed record SpatialResolution(string IntentId, string ModuleId, string EntityId, bool Accepted, string Reason, string? CommandId, IReadOnlyList<string> Events, IReadOnlyList<string> Diagnostics);
public interface ISpatialResolver { string Id { get; } SpatialResolution Resolve(MoveIntent intent); }
