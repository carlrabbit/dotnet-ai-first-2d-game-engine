namespace Agentic2D.Persistence;

/// <summary>Stages contributor application off-runtime; Commit creates the only load-visible runtime.</summary>
public sealed class PersistentWorldLoadTransaction
{
    private int tick; private string seed = string.Empty; private string continuation = string.Empty;
    private readonly SortedDictionary<string, PersistentEntity> entities = new(StringComparer.Ordinal);
    private readonly SortedSet<string> removed = new(StringComparer.Ordinal); private readonly SortedDictionary<string, PersistentFlag> flags = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, PersistentSwitch> switches = new(StringComparer.Ordinal); private readonly SortedDictionary<string, PersistentDoor> doors = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, string> interaction = new(StringComparer.Ordinal); private readonly SortedDictionary<string, string> triggers = new(StringComparer.Ordinal); private readonly SortedDictionary<string, string> animation = new(StringComparer.Ordinal);
    private readonly HashSet<string> applied = new(StringComparer.Ordinal);

    public void ApplyRuntime(int value, string deterministicSeed, string deterministicContinuation) { tick = value; seed = deterministicSeed; continuation = deterministicContinuation; }
    public void ApplyEntityIdentities(IEnumerable<string> values) { foreach (var id in values.Order(StringComparer.Ordinal)) Ensure(id); }
    public void ApplyComponents(IEnumerable<PersistentEntity> values) { foreach (var x in values) { Ensure(x.Id); entities[x.Id] = entities[x.Id] with { Components = Strings(x.Components) }; } }
    public void ApplyResources(IEnumerable<PersistentEntity> values) { foreach (var x in values) { Ensure(x.Id); entities[x.Id] = entities[x.Id] with { Resources = Integers(x.Resources) }; } }
    public void ApplyLifecycle(IEnumerable<PersistentEntity> values) { foreach (var x in values) { Ensure(x.Id); entities[x.Id] = entities[x.Id] with { Lifecycle = x.Lifecycle }; } }
    public void ApplyInventory(IEnumerable<PersistentEntity> values) { foreach (var x in values) { Ensure(x.Id); entities[x.Id] = entities[x.Id] with { Inventory = x.Inventory.OrderBy(y => y.ItemDefinitionId, StringComparer.Ordinal).ToArray() }; } }
    public void ApplyRemovedEntities(IEnumerable<string> values) { foreach (var value in values.Order(StringComparer.Ordinal)) removed.Add(value); }
    public void ApplyInteractionState(IReadOnlyDictionary<string, string> values) { foreach (var x in values.OrderBy(x => x.Key, StringComparer.Ordinal)) interaction[x.Key] = x.Value; }
    public void ApplyTriggerState(IReadOnlyDictionary<string, string> values) { foreach (var x in values.OrderBy(x => x.Key, StringComparer.Ordinal)) triggers[x.Key] = x.Value; }
    public void ApplyAnimationContinuity(IReadOnlyDictionary<string, string> values) { foreach (var x in values.OrderBy(x => x.Key, StringComparer.Ordinal)) animation[x.Key] = x.Value; }
    public void ApplyFlags(IEnumerable<PersistentFlag> values) { foreach (var x in values.OrderBy(x => x.Id, StringComparer.Ordinal)) flags.Add(x.Id, x); }
    public void ApplySwitches(IEnumerable<PersistentSwitch> values) { foreach (var x in values.OrderBy(x => x.EntityId, StringComparer.Ordinal)) switches.Add(x.EntityId, x); }
    public void ApplyDoors(IEnumerable<PersistentDoor> values) { foreach (var x in values.OrderBy(x => x.EntityId, StringComparer.Ordinal)) doors.Add(x.EntityId, x); }
    public void MarkApplied(string contributorId) => applied.Add(contributorId);

    public PersistentWorldRuntime Commit(IEnumerable<string> requiredContributorIds, IEnumerable<FlagDefinition>? definitions)
    {
        var missing = requiredContributorIds.Where(x => !applied.Contains(x)).Order(StringComparer.Ordinal).ToArray();
        if (missing.Length != 0) throw new InvalidOperationException("SAVE0217: incomplete load transaction: " + string.Join(", ", missing));
        var snapshot = new PersistentWorldSnapshot(tick, seed, continuation, entities.Values.ToArray(), removed.ToArray(), flags.Values.ToArray(), switches.Values.ToArray(), doors.Values.ToArray(), interaction, triggers, animation);
        return PersistentWorldRuntime.From(snapshot, definitions);
    }
    private void Ensure(string id) { if (!entities.ContainsKey(id)) entities.Add(id, new PersistentEntity(id, string.Empty, new SortedDictionary<string, string>(StringComparer.Ordinal), [], new SortedDictionary<string, int>(StringComparer.Ordinal))); }
    private static IReadOnlyDictionary<string, string> Strings(IReadOnlyDictionary<string, string> value) => new SortedDictionary<string, string>(value.ToDictionary(x => x.Key, x => x.Value), StringComparer.Ordinal);
    private static IReadOnlyDictionary<string, int> Integers(IReadOnlyDictionary<string, int> value) => new SortedDictionary<string, int>(value.ToDictionary(x => x.Key, x => x.Value), StringComparer.Ordinal);
}
