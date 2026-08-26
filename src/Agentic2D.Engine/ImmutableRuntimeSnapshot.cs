using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.Contracts;

namespace Agentic2D.Engine;

/// <summary>Detached, immutable, typed read boundary for one runtime phase.</summary>
public sealed class ImmutableRuntimeSnapshot : IRuntimeSnapshotView
{
    private readonly IReadOnlySet<string> entities;
    private readonly IReadOnlyDictionary<string, ComponentDescriptor> descriptors;
    private readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> values;

    internal ImmutableRuntimeSnapshot(int tick, IEnumerable<string> entityIds, IReadOnlyDictionary<string, ComponentDescriptor> sourceDescriptors, IReadOnlyDictionary<string, Dictionary<string, object>> stores)
    {
        Tick = tick;
        entities = entityIds.ToHashSet(StringComparer.Ordinal);
        descriptors = sourceDescriptors.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        values = sourceDescriptors.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(
            x => x.Key,
            x => (IReadOnlyDictionary<string, string>)stores[x.Key].OrderBy(y => y.Key, StringComparer.Ordinal).ToDictionary(y => y.Key, y => x.Value.Serialize(y.Value), StringComparer.Ordinal),
            StringComparer.Ordinal);
        Fingerprint = ComputeFingerprint();
    }

    public int Tick { get; }
    public IReadOnlyList<string> EntityIds => entities.Order(StringComparer.Ordinal).ToArray();
    public string Fingerprint { get; }
    public bool Exists(string entityId) => entities.Contains(entityId);
    public bool TryGet<T>(string entityId, out T? value) where T : notnull
    {
        value = default;
        var ids = descriptors.Values.Where(x => x.RuntimeType == typeof(T)).Select(x => x.TypeId).Order(StringComparer.Ordinal).ToArray();
        if (ids.Length > 1) throw new InvalidOperationException("COMPONENT0005: generic component binding is ambiguous; use an explicit stable type ID");
        return ids.Length == 1 && TryGetByTypeId(entityId, ids[0], out value);
    }
    public bool TryGetByTypeId<T>(string entityId, string typeId, out T? value) where T : notnull
    {
        value = default;
        if (!values.TryGetValue(typeId, out var store) || !store.TryGetValue(entityId, out var json) || !descriptors[typeId].RuntimeType.IsAssignableTo(typeof(T))) return false;
        value = (T)descriptors[typeId].Deserialize(json);
        return true;
    }
    public IReadOnlyList<string> Query<T>() where T : notnull
    {
        var ids = descriptors.Values.Where(x => x.RuntimeType == typeof(T)).Select(x => x.TypeId).Order(StringComparer.Ordinal).ToArray();
        if (ids.Length > 1) throw new InvalidOperationException("COMPONENT0005: generic component binding is ambiguous; use an explicit stable type ID");
        return ids.Length == 1 ? QueryByTypeId(ids[0]) : [];
    }
    public IReadOnlyList<string> QueryByTypeId(string typeId) => values.TryGetValue(typeId, out var store) ? store.Keys.Order(StringComparer.Ordinal).ToArray() : [];
    public IReadOnlyList<(string TypeId, object Value)> ComponentsFor(string entityId) => values.Where(x => x.Value.ContainsKey(entityId)).OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => (x.Key, descriptors[x.Key].Deserialize(x.Value[entityId]))).ToArray();

    private string ComputeFingerprint()
    {
        var canonical = JsonSerializer.Serialize(new
        {
            tick = Tick,
            entities = EntityIds,
            components = values.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => new { typeId = x.Key, schema = descriptors[x.Key].SchemaVersion, values = x.Value.OrderBy(y => y.Key, StringComparer.Ordinal).Select(y => new { entityId = y.Key, value = y.Value }) })
        });
        return "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
