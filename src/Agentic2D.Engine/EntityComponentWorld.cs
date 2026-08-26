using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.Contracts;

namespace Agentic2D.Engine;

/// <summary>One registered typed-component world. Generic and descriptor APIs share these stores.</summary>
public sealed class EntityComponentWorld
{
    private readonly SortedSet<string> entities = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ComponentDescriptor> descriptors = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, SortedSet<string>> typeIds = [];
    private readonly Dictionary<string, Dictionary<string, object>> stores = new(StringComparer.Ordinal);
    private readonly List<EntityComponentMutation> mutations = [];
    private readonly List<RuntimeEvent> events = [];
    private readonly Dictionary<string, RuntimeEntityProvenance> provenance = new(StringComparer.Ordinal);
    public IReadOnlyList<EntityComponentMutation> Mutations => mutations;
    public IReadOnlyList<RuntimeEvent> Events => events;
    public IReadOnlyList<string> EntityIds => entities.ToArray();
    public IReadOnlyList<string> RegisteredComponentTypeIds => descriptors.Keys.Order(StringComparer.Ordinal).ToArray();
    public IReadOnlyDictionary<string, RuntimeEntityProvenance> Provenance => provenance;
    public EntityComponentTransaction BeginTransaction(int tick = 0, string? commandId = null) => new(this, tick, commandId ?? $"command.runtime.transaction.{tick}");

    public void Register<T>(string typeId, string owner, Func<T, bool>? isValid = null) where T : notnull => RegisterDescriptor(new(typeId, 1, typeof(T), owner, value => isValid is null || isValid((T)value), value => JsonSerializer.Serialize(value), json => JsonSerializer.Deserialize<T>(json) ?? throw new InvalidOperationException("component decode returned null")));
    public void RegisterDescriptor(ComponentDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.TypeId) || descriptor.SchemaVersion < 1) throw new ArgumentException("component descriptor requires stable type ID and schema version");
        if (!descriptors.TryAdd(descriptor.TypeId, descriptor)) throw new InvalidOperationException("duplicate component type ID: " + descriptor.TypeId);
        if (!typeIds.TryGetValue(descriptor.RuntimeType, out var boundIds)) typeIds[descriptor.RuntimeType] = boundIds = new(StringComparer.Ordinal);
        boundIds.Add(descriptor.TypeId);
        stores.Add(descriptor.TypeId, new Dictionary<string, object>(StringComparer.Ordinal));
    }
    public bool TryGetDescriptor(string typeId, out ComponentDescriptor? descriptor) => descriptors.TryGetValue(typeId, out descriptor);
    public EntityComponentResult CreateEntity(string id, int tick = 0)
    {
        if (string.IsNullOrWhiteSpace(id)) return Reject("ENTITY0003", "invalid entity ID", id, null, "create", tick);
        if (!entities.Add(id)) return Reject("ENTITY0001", "duplicate entity ID", id, null, "create", tick);
        events.Add(new("entity.created", tick, id)); mutations.Add(new(tick, $"command.entity.create.{id}", id, null, "create", "accepted", null, null, ["entity.created"], [])); return new(true, "accepted", null);
    }
    public EntityComponentResult DestroyEntity(string id, int tick = 0)
    {
        if (!entities.Remove(id)) return Reject("ENTITY0002", "entity not found", id, null, "destroy", tick);
        foreach (var store in stores.Values) store.Remove(id); provenance.Remove(id); events.Add(new("entity.destroyed", tick, id)); mutations.Add(new(tick, $"command.entity.destroy.{id}", id, null, "destroy", "accepted", null, null, ["entity.destroyed"], [])); return new(true, "accepted", null);
    }
    public bool Exists(string id) => entities.Contains(id);
    public EntityComponentResult SetProvenance(string id, RuntimeEntityProvenance value, int tick = 0, string? commandId = null)
    {
        if (!entities.Contains(id)) return Reject("ENTITY0002", "entity not found", id, "runtime.provenance", "set-provenance", tick, commandId);
        if (provenance.ContainsKey(id)) return Reject("ENTITY0004", "provenance already exists", id, "runtime.provenance", "set-provenance", tick, commandId);
        provenance.Add(id, value); events.Add(new("entity.provenance-recorded", tick, id)); mutations.Add(new(tick, commandId ?? $"command.entity.provenance.{id}", id, "runtime.provenance", "add", "accepted", null, JsonSerializer.Serialize(value), ["entity.provenance-recorded"], [])); return new(true, "accepted", null);
    }
    public EntityComponentResult Set<T>(string id, T value, int tick = 0, string? commandId = null) where T : notnull => SetByTypeId(id, TypeId<T>(), value, tick, commandId);
    public EntityComponentResult SetByTypeId(string id, string typeId, object value, int tick = 0, string? commandId = null)
    {
        if (!entities.Contains(id)) return Reject("ENTITY0002", "entity not found", id, typeId, "set", tick, commandId);
        if (!descriptors.TryGetValue(typeId, out var descriptor)) return Reject("COMPONENT0001", "component type not registered", id, typeId, "set", tick, commandId);
        if (!descriptor.RuntimeType.IsInstanceOfType(value) || !descriptor.IsValid(value)) return Reject("COMPONENT0002", "invalid component value", id, typeId, "set", tick, commandId);
        var store = stores[typeId]; var had = store.TryGetValue(id, out var old); store[id] = descriptor.Copy(value); var kind = had ? "update" : "add"; var eventType = had ? "entity.component-updated" : "entity.component-added";
        events.Add(new(eventType, tick, id + ":" + typeId)); mutations.Add(new(tick, commandId ?? $"command.component.{kind}.{id}.{typeId}", id, typeId, kind, "accepted", old is null ? null : descriptor.Serialize(old), descriptor.Serialize(value), [eventType], [])); return new(true, "accepted", null);
    }
    public EntityComponentResult Remove<T>(string id, int tick = 0, string? commandId = null) where T : notnull => RemoveByTypeId(id, TypeId<T>(), tick, commandId);
    public EntityComponentResult RemoveByTypeId(string id, string typeId, int tick = 0, string? commandId = null)
    {
        if (!entities.Contains(id)) return Reject("ENTITY0002", "entity not found", id, typeId, "remove", tick, commandId);
        if (!descriptors.TryGetValue(typeId, out var descriptor)) return Reject("COMPONENT0001", "component type not registered", id, typeId, "remove", tick, commandId);
        if (!stores[typeId].Remove(id, out var old)) return Reject("COMPONENT0004", "component removal target missing", id, typeId, "remove", tick, commandId);
        events.Add(new("entity.component-removed", tick, id + ":" + typeId)); mutations.Add(new(tick, commandId ?? $"command.component.remove.{id}.{typeId}", id, typeId, "remove", "accepted", descriptor.Serialize(old), null, ["entity.component-removed"], [])); return new(true, "accepted", null);
    }
    public bool TryGet<T>(string id, out T? value) where T : notnull { value = default; var typeId = TypeId<T>(); if (typeId.Length == 0 || !stores[typeId].TryGetValue(id, out var raw)) return false; value = (T)descriptors[typeId].Copy(raw); return true; }
    public bool TryGetByTypeId(string id, string typeId, out object? value) { value = null; if (!descriptors.ContainsKey(typeId) || !stores[typeId].TryGetValue(id, out var raw)) return false; value = descriptors[typeId].Copy(raw); return true; }
    public IReadOnlyList<string> Query<T>() where T : notnull => QueryByTypeId(TypeId<T>());
    public IReadOnlyList<string> QueryByTypeId(string typeId) => stores.TryGetValue(typeId, out var store) ? store.Keys.Order(StringComparer.Ordinal).ToArray() : [];
    public IReadOnlyList<(string TypeId, object Value)> ComponentsFor(string id) => descriptors.Keys.Order(StringComparer.Ordinal).Where(key => stores[key].ContainsKey(id)).Select(key => (key, descriptors[key].Copy(stores[key][id]))).ToArray();
    public IReadOnlyList<string> Query<T1, T2>() where T1 : notnull where T2 : notnull => Query<T1>().Where(id => TryGet<T2>(id, out _)).ToArray();
    public EntityComponentBatchResult CommitBatch(IReadOnlyList<EntityComponentBatchMutation> changes, int tick = 0, string? commandId = null)
    {
        if (changes.Count == 0 || changes.GroupBy(x => (x.EntityId, x.TypeId)).Any(x => x.Count() != 1)) return new(false, "duplicate-or-empty-batch", []);
        var staged = new List<(EntityComponentBatchMutation Change, ComponentDescriptor Descriptor, object? Previous)>();
        foreach (var change in changes) { if (!entities.Contains(change.EntityId) || !descriptors.TryGetValue(change.TypeId, out var descriptor) || !descriptor.RuntimeType.IsInstanceOfType(change.Value) || !descriptor.IsValid(change.Value)) return new(false, "invalid-batch", []); stores[change.TypeId].TryGetValue(change.EntityId, out var previous); staged.Add((change, descriptor, previous is null ? null : descriptor.Copy(previous))); }
        foreach (var item in staged) stores[item.Change.TypeId][item.Change.EntityId] = item.Descriptor.Copy(item.Change.Value);
        foreach (var item in staged) { var had = item.Previous is not null; var eventType = had ? "entity.component-updated" : "entity.component-added"; events.Add(new(eventType, tick, item.Change.EntityId + ":" + item.Change.TypeId)); mutations.Add(new(tick, commandId ?? "command.component.batch", item.Change.EntityId, item.Change.TypeId, had ? "update" : "add", "accepted", item.Previous is null ? null : item.Descriptor.Serialize(item.Previous), item.Descriptor.Serialize(item.Change.Value), [eventType], [])); }
        return new(true, "accepted", staged.Select(x => x.Change.TypeId + ":" + x.Change.EntityId).ToArray());
    }
    public EntityComponentSnapshot Snapshot(int tick) => new(tick, entities.ToArray(), descriptors.Values.OrderBy(r => r.TypeId, StringComparer.Ordinal).Select(r => new ComponentSnapshotType(r.TypeId, r.Owner, stores[r.TypeId].OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new ComponentSnapshotValue(kv.Key, r.Serialize(kv.Value))).ToArray())).ToArray());
    public string TypeId<T>() where T : notnull
    {
        if (!typeIds.TryGetValue(typeof(T), out var ids)) return string.Empty;
        if (ids.Count != 1) throw new InvalidOperationException("COMPONENT0005: generic component binding is ambiguous; use an explicit stable type ID");
        return ids.Min!;
    }
    public ImmutableRuntimeSnapshot TypedSnapshot(int tick) => new(tick, entities, descriptors, stores);
    public EntityComponentBatchResult Commit(EntityComponentTransaction transaction)
    {
        var stagedEntities = entities.ToHashSet(StringComparer.Ordinal);
        var stagedProvenance = provenance.Keys.ToHashSet(StringComparer.Ordinal);
        var stagedComponents = stores.ToDictionary(x => x.Key, x => x.Value.Keys.ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);
        var changes = new List<(EntityComponentTransaction.Operation Op, ComponentDescriptor? Descriptor, object? Previous)>();
        foreach (var op in transaction.Operations)
        {
            if (op.Kind == EntityComponentTransaction.Kind.Create)
            {
                if (string.IsNullOrWhiteSpace(op.EntityId) || !stagedEntities.Add(op.EntityId)) return RejectTransaction(transaction, "ENTITY0001");
            }
            else if (op.Kind == EntityComponentTransaction.Kind.Destroy)
            {
                if (!stagedEntities.Remove(op.EntityId)) return RejectTransaction(transaction, "ENTITY0002");
                stagedProvenance.Remove(op.EntityId); foreach (var set in stagedComponents.Values) set.Remove(op.EntityId);
            }
            else if (!stagedEntities.Contains(op.EntityId)) return RejectTransaction(transaction, "ENTITY0002");
            else if (op.Kind == EntityComponentTransaction.Kind.Provenance)
            {
                if (!stagedProvenance.Add(op.EntityId)) return RejectTransaction(transaction, "ENTITY0004");
            }
            else
            {
                if (op.TypeId is null || !descriptors.TryGetValue(op.TypeId, out var descriptor)) return RejectTransaction(transaction, "COMPONENT0001");
                if (op.Kind == EntityComponentTransaction.Kind.Set)
                {
                    if (op.Value is null || !descriptor.RuntimeType.IsInstanceOfType(op.Value) || !descriptor.IsValid(op.Value)) return RejectTransaction(transaction, "COMPONENT0002");
                    stagedComponents[op.TypeId].TryGetValue(op.EntityId, out _); changes.Add((op, descriptor, stores[op.TypeId].TryGetValue(op.EntityId, out var prior) ? descriptor.Copy(prior) : null)); stagedComponents[op.TypeId].Add(op.EntityId);
                }
                else if (!stagedComponents[op.TypeId].Remove(op.EntityId)) return RejectTransaction(transaction, "COMPONENT0004");
                changes.Add((op, descriptor, null));
            }
        }
        foreach (var op in transaction.Operations)
        {
            switch (op.Kind)
            {
                case EntityComponentTransaction.Kind.Create: entities.Add(op.EntityId); events.Add(new("entity.created", transaction.Tick, op.EntityId)); break;
                case EntityComponentTransaction.Kind.Destroy: foreach (var store in stores.Values) store.Remove(op.EntityId); provenance.Remove(op.EntityId); events.Add(new("entity.destroyed", transaction.Tick, op.EntityId)); break;
                case EntityComponentTransaction.Kind.Provenance: provenance[op.EntityId] = (RuntimeEntityProvenance)op.Value!; events.Add(new("entity.provenance-recorded", transaction.Tick, op.EntityId)); break;
                case EntityComponentTransaction.Kind.Set: stores[op.TypeId!][op.EntityId] = descriptors[op.TypeId!].Copy(op.Value!); events.Add(new("entity.component-added", transaction.Tick, op.EntityId + ":" + op.TypeId)); break;
                case EntityComponentTransaction.Kind.Remove: stores[op.TypeId!].Remove(op.EntityId); events.Add(new("entity.component-removed", transaction.Tick, op.EntityId + ":" + op.TypeId)); break;
            }
        }
        mutations.Add(new(transaction.Tick, transaction.CommandId, string.Join(",", transaction.Operations.Select(x => x.EntityId).Distinct(StringComparer.Ordinal)), null, "transaction", "accepted", null, null, events.TakeLast(transaction.Operations.Count).Select(x => x.Type).ToArray(), []));
        return new(true, "accepted", transaction.Operations.Select(x => (x.TypeId ?? x.Kind.ToString()) + ":" + x.EntityId).ToArray());
    }
    private EntityComponentBatchResult RejectTransaction(EntityComponentTransaction transaction, string diagnostic)
    {
        mutations.Add(new(transaction.Tick, transaction.CommandId, string.Join(",", transaction.Operations.Select(x => x.EntityId).Distinct(StringComparer.Ordinal)), null, "transaction", "rejected", null, null, [], [diagnostic]));
        return new(false, "rejected", []);
    }
    private EntityComponentResult Reject(string diagnostic, string message, string id, string? typeId, string kind, int tick = 0, string? commandId = null) { mutations.Add(new(tick, commandId ?? $"command.component.{kind}.{id}", id, typeId, kind, "rejected", null, null, [], [diagnostic])); return new(false, "rejected", diagnostic); }
}
public sealed record ComponentDescriptor(string TypeId, int SchemaVersion, Type RuntimeType, string Owner, Func<object, bool> IsValid, Func<object, string> Serialize, Func<string, object> Deserialize) { public object Copy(object value) => Deserialize(Serialize(value)); }
public sealed record EntityComponentBatchMutation(string EntityId, string TypeId, object Value);
public sealed record EntityComponentBatchResult(bool Accepted, string Status, IReadOnlyList<string> ChangedKeys);
public sealed record EntityComponentResult(bool Accepted, string Status, string? Diagnostic);
public sealed record EntityComponentMutation(int Tick, string CommandId, string EntityId, string? ComponentTypeId, string MutationKind, string Status, string? PreviousValue, string? ResultingValue, IReadOnlyList<string> Events, IReadOnlyList<string> Diagnostics);
public sealed record ComponentSnapshotType(string TypeId, string Owner, IReadOnlyList<ComponentSnapshotValue> Values);
public sealed record ComponentSnapshotValue(string EntityId, string Value);
public sealed record EntityComponentSnapshot(int Tick, IReadOnlyList<string> EntityIds, IReadOnlyList<ComponentSnapshotType> Components)
{
    public string Fingerprint => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Tick + "|" + string.Join("|", EntityIds) + "|" + string.Join("|", Components.Select(c => c.TypeId + ":" + string.Join(",", c.Values.Select(v => v.EntityId + "=" + v.Value))))))).ToLowerInvariant();
}
