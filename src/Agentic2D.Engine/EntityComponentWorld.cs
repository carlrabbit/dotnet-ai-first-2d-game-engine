using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.Contracts;

namespace Agentic2D.Engine;

/// <summary>A deliberately small registered typed-component world. Storage is private and replaceable.</summary>
public sealed class EntityComponentWorld
{
    private readonly SortedSet<string> entities = new(StringComparer.Ordinal);
    private readonly Dictionary<Type, ComponentRegistration> registrations = [];
    private readonly Dictionary<string, Dictionary<string, object>> stores = new(StringComparer.Ordinal);
    private readonly List<EntityComponentMutation> mutations = [];
    private readonly List<RuntimeEvent> events = [];
    private readonly Dictionary<string, RuntimeEntityProvenance> provenance = new(StringComparer.Ordinal);

    public IReadOnlyList<EntityComponentMutation> Mutations => mutations;
    public IReadOnlyList<RuntimeEvent> Events => events;
    public IReadOnlyList<string> EntityIds => entities.ToArray();
    public IReadOnlyList<string> RegisteredComponentTypeIds => registrations.Values.Select(x => x.TypeId).Order(StringComparer.Ordinal).ToArray();
    public IReadOnlyDictionary<string, RuntimeEntityProvenance> Provenance => provenance;

    public void Register<T>(string typeId, string owner, Func<T, bool>? isValid = null) where T : notnull
    {
        if (registrations.ContainsKey(typeof(T))) return;
        registrations.Add(typeof(T), new ComponentRegistration(typeId, owner, value => isValid is null || isValid((T)value), value => JsonSerializer.Serialize(value)));
        stores.Add(typeId, new Dictionary<string, object>(StringComparer.Ordinal));
    }

    public EntityComponentResult CreateEntity(string id, int tick = 0)
    {
        if (string.IsNullOrWhiteSpace(id)) return Reject("ENTITY0003", "invalid entity ID", id, null, "create");
        if (!entities.Add(id)) return Reject("ENTITY0001", "duplicate entity ID", id, null, "create");
        events.Add(new RuntimeEvent("entity.created", tick, id));
        mutations.Add(new(tick, $"command.entity.create.{id}", id, null, "create", "accepted", null, null, ["entity.created"], []));
        return new(true, "accepted", null);
    }

    public EntityComponentResult DestroyEntity(string id, int tick = 0)
    {
        if (!entities.Remove(id)) return Reject("ENTITY0002", "entity not found", id, null, "destroy");
        foreach (var store in stores.Values) store.Remove(id);
        provenance.Remove(id);
        events.Add(new RuntimeEvent("entity.destroyed", tick, id));
        mutations.Add(new(tick, $"command.entity.destroy.{id}", id, null, "destroy", "accepted", null, null, ["entity.destroyed"], []));
        return new(true, "accepted", null);
    }

    public bool Exists(string id) => entities.Contains(id);
    public EntityComponentResult SetProvenance(string id, RuntimeEntityProvenance value, int tick = 0, string? commandId = null)
    {
        if (!entities.Contains(id)) return Reject("ENTITY0002", "entity not found", id, "runtime.provenance", "set-provenance");
        if (provenance.ContainsKey(id)) return Reject("ENTITY0004", "provenance already exists", id, "runtime.provenance", "set-provenance");
        provenance.Add(id, value);
        events.Add(new RuntimeEvent("entity.provenance-recorded", tick, id));
        mutations.Add(new(tick, commandId ?? $"command.entity.provenance.{id}", id, "runtime.provenance", "add", "accepted", null, JsonSerializer.Serialize(value), ["entity.provenance-recorded"], []));
        return new(true, "accepted", null);
    }
    public EntityComponentResult Set<T>(string id, T value, int tick = 0, string? commandId = null) where T : notnull
    {
        if (!entities.Contains(id)) return Reject("ENTITY0002", "entity not found", id, TypeId<T>(), "set");
        if (!registrations.TryGetValue(typeof(T), out var registration)) return Reject("COMPONENT0001", "component type not registered", id, null, "set");
        if (!registration.IsValid(value)) return Reject("COMPONENT0002", "invalid component value", id, registration.TypeId, "set");
        var store = stores[registration.TypeId]; var had = store.TryGetValue(id, out var old);
        store[id] = value;
        var kind = had ? "update" : "add"; var eventType = had ? "entity.component-updated" : "entity.component-added";
        events.Add(new RuntimeEvent(eventType, tick, id + ":" + registration.TypeId));
        mutations.Add(new(tick, commandId ?? $"command.component.{kind}.{id}.{registration.TypeId}", id, registration.TypeId, kind, "accepted", old is null ? null : registration.Serialize(old), registration.Serialize(value), [eventType], []));
        return new(true, "accepted", null);
    }

    public EntityComponentResult Remove<T>(string id, int tick = 0, string? commandId = null) where T : notnull
    {
        if (!entities.Contains(id)) return Reject("ENTITY0002", "entity not found", id, TypeId<T>(), "remove");
        if (!registrations.TryGetValue(typeof(T), out var registration)) return Reject("COMPONENT0001", "component type not registered", id, null, "remove");
        if (!stores[registration.TypeId].Remove(id, out var old)) return Reject("COMPONENT0004", "component removal target missing", id, registration.TypeId, "remove");
        events.Add(new RuntimeEvent("entity.component-removed", tick, id + ":" + registration.TypeId));
        mutations.Add(new(tick, commandId ?? $"command.component.remove.{id}.{registration.TypeId}", id, registration.TypeId, "remove", "accepted", registration.Serialize(old), null, ["entity.component-removed"], []));
        return new(true, "accepted", null);
    }

    public bool TryGet<T>(string id, out T? value) where T : notnull
    {
        value = default;
        return registrations.TryGetValue(typeof(T), out var registration) && stores[registration.TypeId].TryGetValue(id, out var raw) && (value = (T)raw) is not null;
    }
    public IReadOnlyList<string> Query<T>() where T : notnull => registrations.TryGetValue(typeof(T), out var registration) ? stores[registration.TypeId].Keys.Order(StringComparer.Ordinal).ToArray() : [];
    public IReadOnlyList<string> Query<T1, T2>() where T1 : notnull where T2 : notnull => Query<T1>().Where(id => TryGet<T2>(id, out _)).ToArray();
    public EntityComponentSnapshot Snapshot(int tick) => new(tick, entities.ToArray(), registrations.Values.OrderBy(r => r.TypeId, StringComparer.Ordinal).Select(r => new ComponentSnapshotType(r.TypeId, r.Owner, stores[r.TypeId].OrderBy(kv => kv.Key, StringComparer.Ordinal).Select(kv => new ComponentSnapshotValue(kv.Key, r.Serialize(kv.Value))).ToArray())).ToArray());
    public string TypeId<T>() where T : notnull => registrations.TryGetValue(typeof(T), out var registration) ? registration.TypeId : string.Empty;
    private EntityComponentResult Reject(string diagnostic, string message, string id, string? typeId, string kind)
    {
        mutations.Add(new(0, $"command.component.{kind}.{id}", id, typeId, kind, "rejected", null, null, [], [diagnostic]));
        return new(false, "rejected", diagnostic);
    }
    private sealed record ComponentRegistration(string TypeId, string Owner, Func<object, bool> IsValid, Func<object, string> Serialize);
}

public sealed record EntityComponentResult(bool Accepted, string Status, string? Diagnostic);
public sealed record EntityComponentMutation(int Tick, string CommandId, string EntityId, string? ComponentTypeId, string MutationKind, string Status, string? PreviousValue, string? ResultingValue, IReadOnlyList<string> Events, IReadOnlyList<string> Diagnostics);
public sealed record ComponentSnapshotType(string TypeId, string Owner, IReadOnlyList<ComponentSnapshotValue> Values);
public sealed record ComponentSnapshotValue(string EntityId, string Value);
public sealed record EntityComponentSnapshot(int Tick, IReadOnlyList<string> EntityIds, IReadOnlyList<ComponentSnapshotType> Components)
{
    public string Fingerprint
    {
        get
        {
            var semantic = Tick + "|" + string.Join("|", EntityIds) + "|" + string.Join("|", Components.Select(c => c.TypeId + ":" + string.Join(",", c.Values.Select(v => v.EntityId + "=" + v.Value))));
            return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(semantic))).ToLowerInvariant();
        }
    }
}
