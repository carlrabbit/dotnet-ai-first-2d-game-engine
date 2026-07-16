using Agentic2D.Gameplay;

namespace Agentic2D.Engine;

/// <summary>
/// Runtime authority for M019 gameplay state. Behavior and presentation callers submit
/// intents; only this boundary changes registered entity components or removes entities.
/// </summary>
public sealed class M019GameplayRuntime
{
    private readonly HashSet<string> damageCorrelations = new(StringComparer.Ordinal);
    private readonly HashSet<string> collectionCorrelations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ItemDefinitionSource> itemDefinitions = new(StringComparer.Ordinal);
    private int eventOrdinal;

    public M019GameplayRuntime()
    {
        World = new EntityComponentWorld();
        World.Register<ResourceHealth>("resource.health", "runtime", x => x.IsValid);
        World.Register<RuntimeLifecycle>("lifecycle.state", "runtime", x => x.State is GameplayIds.Active or GameplayIds.Defeated or GameplayIds.Inactive);
        World.Register<Inventory>("inventory.entries", "runtime", x => x.IsValid);
        World.Register<WorldItem>("world-item", "runtime", x => x.IsValid);
    }

    public EntityComponentWorld World { get; }
    public List<DomainEvent> Events { get; } = [];
    public List<ResourceTransition> ResourceTransitions { get; } = [];
    public List<LifecycleTransition> LifecycleTransitions { get; } = [];
    public List<InventoryTransition> InventoryTransitions { get; } = [];
    public List<WorldItemTransition> WorldItemTransitions { get; } = [];

    public bool CreateEntity(string id, int tick, ResourceHealth? health = null, Inventory? inventory = null, WorldItem? worldItem = null)
    {
        if (!World.CreateEntity(id, tick).Accepted) return false;
        if (!World.Set(id, new RuntimeLifecycle(GameplayIds.Active), tick).Accepted) return false;
        if (health is not null && !World.Set(id, health, tick).Accepted) return false;
        if (inventory is not null && !World.Set(id, inventory, tick).Accepted) return false;
        if (worldItem is not null && !World.Set(id, worldItem, tick).Accepted) return false;
        return true;
    }

    public void RegisterItem(ItemDefinitionSource definition)
    {
        if (!definition.IsValid) throw new ArgumentException("Item definition is invalid.", nameof(definition));
        itemDefinitions.Add(definition.Id, definition);
    }

    public DamageResolution ApplyDamage(DamageIntent intent)
    {
        var transaction = "damage-transaction." + intent.IntentId;
        if (intent.DamageKindId is not ("damage.generic" or "damage.environment")) return DamageReject("invalid-damage-kind");
        if (intent.RequestedAmount <= 0) return DamageReject("non-positive-damage");
        if (string.IsNullOrWhiteSpace(intent.CorrelationId) || !damageCorrelations.Add(intent.CorrelationId)) return DamageReject("duplicate-correlation");
        if (!World.Exists(intent.TargetEntityId)) return DamageReject("missing-target");
        if (!World.TryGet<RuntimeLifecycle>(intent.TargetEntityId, out var lifecycle) || lifecycle is null || lifecycle.State != GameplayIds.Active) return DamageReject(lifecycle?.State == GameplayIds.Defeated ? "already-defeated" : "invalid-lifecycle");
        if (!World.TryGet<ResourceHealth>(intent.TargetEntityId, out var before) || before is null) return DamageReject("target-without-health");
        var applied = Math.Min(intent.RequestedAmount, before.Current - before.Minimum);
        var after = before.ApplyDamage(applied);
        if (!World.Set(intent.TargetEntityId, after, intent.RuntimeTick, transaction + ".resource").Accepted) return DamageReject("resource-write-failed");
        ResourceTransitions.Add(new(intent.TargetEntityId, before, after, intent.RuntimeTick, transaction, intent.CorrelationId));
        Emit("resource.changed", intent, transaction, new { resourceTypeId = GameplayIds.Health, before = before.Current, after = after.Current });
        Emit("entity.damaged", intent, transaction, new { requested = intent.RequestedAmount, applied });
        var lifecycleAfter = lifecycle.State;
        if (after.Current == after.Minimum)
        {
            lifecycleAfter = GameplayIds.Defeated;
            var nextLifecycle = new RuntimeLifecycle(lifecycleAfter);
            if (!World.Set(intent.TargetEntityId, nextLifecycle, intent.RuntimeTick, transaction + ".lifecycle").Accepted) throw new InvalidOperationException("Lifecycle transaction failed after resource transition.");
            LifecycleTransitions.Add(new(intent.TargetEntityId, lifecycle.State, lifecycleAfter, intent.RuntimeTick, transaction, intent.CorrelationId));
            Emit("entity.defeated", intent, transaction, new { lifecycle = lifecycleAfter });
        }
        return new(intent.IntentId, intent.CorrelationId, "accepted", null, intent.RequestedAmount, applied, before.Current, after.Current, lifecycle.State, lifecycleAfter, transaction);

        DamageResolution DamageReject(string reason) => new(intent.IntentId, intent.CorrelationId, "rejected", reason, intent.RequestedAmount, 0, null, null, null, null, transaction);
    }

    public CollectionResolution Collect(CollectItemIntent intent)
    {
        var transaction = "collection-transaction." + intent.IntentId;
        if (string.IsNullOrWhiteSpace(intent.CorrelationId) || !collectionCorrelations.Add(intent.CorrelationId)) return CollectionReject("duplicate-correlation");
        if (!World.Exists(intent.CollectorEntityId) || !World.TryGet<RuntimeLifecycle>(intent.CollectorEntityId, out var lifecycle) || lifecycle?.State != GameplayIds.Active) return CollectionReject("missing-or-inactive-collector");
        if (!World.TryGet<Inventory>(intent.CollectorEntityId, out var beforeInventory) || beforeInventory is null) return CollectionReject("collector-without-inventory");
        if (!World.Exists(intent.WorldItemEntityId) || !World.TryGet<WorldItem>(intent.WorldItemEntityId, out var worldItem) || worldItem is null) return CollectionReject("missing-world-item");
        if (!worldItem.IsValid || !itemDefinitions.TryGetValue(worldItem.ItemDefinitionId, out var definition)) return CollectionReject("invalid-item-reference");
        if (intent.ExpectedItemDefinitionId is not null && intent.ExpectedItemDefinitionId != worldItem.ItemDefinitionId) return CollectionReject("invalid-item-reference");
        var current = beforeInventory.Entries.SingleOrDefault(x => x.ItemDefinitionId == worldItem.ItemDefinitionId);
        if (current is null && beforeInventory.Entries.Count >= beforeInventory.MaximumDistinctEntries) return CollectionReject("insufficient-distinct-entry-capacity");
        if ((!definition.Stackable && worldItem.Quantity != 1) || (current?.Quantity ?? 0) + worldItem.Quantity > definition.MaximumStack) return CollectionReject("stack-overflow");

        var afterInventory = beforeInventory.WithAdded(definition, worldItem.Quantity);
        if (!World.Set(intent.CollectorEntityId, afterInventory, intent.RuntimeTick, transaction + ".inventory").Accepted) return CollectionReject("inventory-write-failed");
        var removed = World.DestroyEntity(intent.WorldItemEntityId, intent.RuntimeTick);
        if (!removed.Accepted)
        {
            World.Set(intent.CollectorEntityId, beforeInventory, intent.RuntimeTick, transaction + ".rollback");
            return CollectionReject("world-item-removal-failed");
        }
        InventoryTransitions.Add(new(intent.CollectorEntityId, beforeInventory, afterInventory, intent.RuntimeTick, transaction, intent.CorrelationId));
        WorldItemTransitions.Add(new(intent.WorldItemEntityId, worldItem, "removed", intent.RuntimeTick, transaction, intent.CorrelationId));
        Events.Add(new("domain-event." + (++eventOrdinal).ToString("D4"), "item.collected", intent.RuntimeTick, intent.CollectorEntityId, intent.WorldItemEntityId, intent.CorrelationId, transaction, new { itemDefinitionId = worldItem.ItemDefinitionId, quantity = worldItem.Quantity }, intent.Provenance));
        Events.Add(new("domain-event." + (++eventOrdinal).ToString("D4"), "entity.removed", intent.RuntimeTick, intent.CollectorEntityId, intent.WorldItemEntityId, intent.CorrelationId, transaction, new { reason = "collection" }, intent.Provenance));
        return new(intent.IntentId, intent.CorrelationId, "accepted", null, transaction, worldItem.ItemDefinitionId, worldItem.Quantity);

        CollectionResolution CollectionReject(string reason) => new(intent.IntentId, intent.CorrelationId, "rejected", reason, transaction);
    }

    private void Emit(string type, DamageIntent intent, string transaction, object payload) => Events.Add(new("domain-event." + (++eventOrdinal).ToString("D4"), type, intent.RuntimeTick, intent.SourceId, intent.TargetEntityId, intent.CorrelationId, transaction, payload, intent.Provenance));
}

public sealed record RuntimeLifecycle(string State)
{
    public bool NormalBehaviorEnabled => State == GameplayIds.Active;
}
