using Agentic2D.Contracts;

namespace Agentic2D.Engine;

/// <summary>Bounded staged lifecycle/provenance/component mutation transaction.</summary>
public sealed class EntityComponentTransaction
{
    internal enum Kind { Create, Destroy, Provenance, Set, Remove }
    internal sealed record Operation(Kind Kind, string EntityId, string? TypeId, object? Value);
    private readonly EntityComponentWorld world;
    private readonly List<Operation> operations = [];
    internal EntityComponentTransaction(EntityComponentWorld world, int tick, string commandId) { this.world = world; Tick = tick; CommandId = commandId; }
    public int Tick { get; }
    public string CommandId { get; }
    public EntityComponentTransaction CreateEntity(string id) { operations.Add(new(Kind.Create, id, null, null)); return this; }
    public EntityComponentTransaction DestroyEntity(string id) { operations.Add(new(Kind.Destroy, id, null, null)); return this; }
    public EntityComponentTransaction SetProvenance(string id, RuntimeEntityProvenance value) { operations.Add(new(Kind.Provenance, id, "runtime.provenance", value)); return this; }
    public EntityComponentTransaction SetComponent(string id, string typeId, object value) { operations.Add(new(Kind.Set, id, typeId, value)); return this; }
    public EntityComponentTransaction RemoveComponent(string id, string typeId) { operations.Add(new(Kind.Remove, id, typeId, null)); return this; }
    public EntityComponentBatchResult Commit() => world.Commit(this);
    internal IReadOnlyList<Operation> Operations => operations;
}
