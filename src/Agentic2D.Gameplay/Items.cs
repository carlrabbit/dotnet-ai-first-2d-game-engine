using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic2D.Gameplay;

public sealed record ItemDefinitionSource
{
    [JsonPropertyName("schema")] public string Schema { get; init; } = "";
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("stackable")] public bool Stackable { get; init; }
    [JsonPropertyName("maximumStack")] public int MaximumStack { get; init; }
    [JsonPropertyName("tags")] public IReadOnlyList<string> Tags { get; init; } = [];
    [JsonPropertyName("visualDefinitionId")] public string VisualDefinitionId { get; init; } = "";
    [JsonPropertyName("defaultCollectionCue")] public string DefaultCollectionCue { get; init; } = "";
    [JsonPropertyName("provenance")] public Dictionary<string, JsonElement> Provenance { get; init; } = [];
    public bool IsValid => Schema == "agentic2d.item-definition.v1" && Id.StartsWith("item.", StringComparison.Ordinal) && MaximumStack > 0 && !string.IsNullOrWhiteSpace(VisualDefinitionId) && DefaultCollectionCue.StartsWith("cue.", StringComparison.Ordinal);
}

public sealed record WorldItem(string ItemDefinitionId, int Quantity, int Revision)
{
    public bool IsValid => ItemDefinitionId.StartsWith("item.", StringComparison.Ordinal) && Quantity > 0 && Revision >= 0;
}

public sealed record InventoryEntry(string ItemDefinitionId, int Quantity)
{
    public bool IsValid => ItemDefinitionId.StartsWith("item.", StringComparison.Ordinal) && Quantity > 0;
}

public sealed record Inventory(string InventoryId, int MaximumDistinctEntries, IReadOnlyList<InventoryEntry> Entries, int Revision)
{
    public bool IsValid => InventoryId.StartsWith("inventory.", StringComparison.Ordinal) && MaximumDistinctEntries > 0 && Revision >= 0 && Entries.All(x => x.IsValid) && Entries.Select(x => x.ItemDefinitionId).SequenceEqual(Entries.Select(x => x.ItemDefinitionId).Order(StringComparer.Ordinal));
    public Inventory WithAdded(ItemDefinitionSource definition, int quantity)
    {
        var existing = Entries.SingleOrDefault(x => x.ItemDefinitionId == definition.Id);
        if (existing is null) return this with { Entries = Entries.Append(new InventoryEntry(definition.Id, quantity)).OrderBy(x => x.ItemDefinitionId, StringComparer.Ordinal).ToArray(), Revision = Revision + 1 };
        return this with { Entries = Entries.Select(x => x.ItemDefinitionId == definition.Id ? x with { Quantity = x.Quantity + quantity } : x).OrderBy(x => x.ItemDefinitionId, StringComparer.Ordinal).ToArray(), Revision = Revision + 1 };
    }
}

public sealed record CollectItemIntent(string IntentId, string CollectorEntityId, string WorldItemEntityId, string? ExpectedItemDefinitionId, int RuntimeTick, string CorrelationId, string Provenance);
public sealed record CollectionResolution(string IntentId, string CorrelationId, string Status, string? RejectionReason, string TransactionId, string? ItemDefinitionId = null, int Quantity = 0);
public sealed record InventoryTransition(string CollectorEntityId, Inventory Before, Inventory After, int RuntimeTick, string TransactionId, string CorrelationId);
public sealed record WorldItemTransition(string EntityId, WorldItem Before, string Outcome, int RuntimeTick, string TransactionId, string CorrelationId);

/// <summary>Prevalidates collection completely and commits inventory plus entity removal as one operation.</summary>
public sealed class CollectionWorld
{
    private readonly Dictionary<string, string> lifecycles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Inventory> inventories = new(StringComparer.Ordinal);
    private readonly Dictionary<string, WorldItem> items = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ItemDefinitionSource> definitions = new(StringComparer.Ordinal);
    private readonly HashSet<string> correlations = new(StringComparer.Ordinal);
    private int eventOrdinal;

    public List<InventoryTransition> InventoryTransitions { get; } = [];
    public List<WorldItemTransition> WorldItemTransitions { get; } = [];
    public List<DomainEvent> Events { get; } = [];
    public IReadOnlyDictionary<string, Inventory> Inventories => inventories;
    public IReadOnlyDictionary<string, WorldItem> WorldItems => items;

    public void AddDefinition(ItemDefinitionSource definition) => definitions.Add(definition.Id, definition);
    public void AddCollector(string entityId, Inventory inventory, string lifecycle = GameplayIds.Active) { lifecycles.Add(entityId, lifecycle); inventories.Add(entityId, inventory); }
    public void AddWorldItem(string entityId, WorldItem item) => items.Add(entityId, item);

    public CollectionResolution Collect(CollectItemIntent intent)
    {
        var transaction = "collection-transaction." + intent.IntentId;
        if (string.IsNullOrWhiteSpace(intent.CorrelationId) || !correlations.Add(intent.CorrelationId)) return Reject("duplicate-correlation");
        if (!lifecycles.TryGetValue(intent.CollectorEntityId, out var lifecycle) || lifecycle != GameplayIds.Active) return Reject("missing-or-inactive-collector");
        if (!inventories.TryGetValue(intent.CollectorEntityId, out var inventory)) return Reject("collector-without-inventory");
        if (!items.TryGetValue(intent.WorldItemEntityId, out var item)) return Reject("missing-world-item");
        if (!item.IsValid || !definitions.TryGetValue(item.ItemDefinitionId, out var definition) || !definition.IsValid) return Reject("invalid-item-reference");
        if (intent.ExpectedItemDefinitionId is not null && intent.ExpectedItemDefinitionId != item.ItemDefinitionId) return Reject("invalid-item-reference");
        var current = inventory.Entries.SingleOrDefault(x => x.ItemDefinitionId == item.ItemDefinitionId);
        if (current is null && inventory.Entries.Count >= inventory.MaximumDistinctEntries) return Reject("insufficient-distinct-entry-capacity");
        if ((!definition.Stackable && item.Quantity != 1) || (current?.Quantity ?? 0) + item.Quantity > definition.MaximumStack) return Reject("stack-overflow");

        // All checks passed: commit both mutations together before emitting the post-commit event.
        var updated = inventory.WithAdded(definition, item.Quantity);
        inventories[intent.CollectorEntityId] = updated;
        if (!items.Remove(intent.WorldItemEntityId)) throw new InvalidOperationException("Atomic collection remove unexpectedly failed.");
        InventoryTransitions.Add(new(intent.CollectorEntityId, inventory, updated, intent.RuntimeTick, transaction, intent.CorrelationId));
        WorldItemTransitions.Add(new(intent.WorldItemEntityId, item, "removed", intent.RuntimeTick, transaction, intent.CorrelationId));
        Events.Add(new("domain-event.collection." + (++eventOrdinal).ToString("D4"), "item.collected", intent.RuntimeTick, intent.CollectorEntityId, intent.WorldItemEntityId, intent.CorrelationId, transaction, new { itemDefinitionId = item.ItemDefinitionId, item.Quantity }, intent.Provenance));
        Events.Add(new("domain-event.collection." + (++eventOrdinal).ToString("D4"), "entity.removed", intent.RuntimeTick, intent.CollectorEntityId, intent.WorldItemEntityId, intent.CorrelationId, transaction, new { reason = "collection" }, intent.Provenance));
        return new(intent.IntentId, intent.CorrelationId, "accepted", null, transaction, item.ItemDefinitionId, item.Quantity);

        CollectionResolution Reject(string reason) => new(intent.IntentId, intent.CorrelationId, "rejected", reason, transaction);
    }
}
