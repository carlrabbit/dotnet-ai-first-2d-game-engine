using Agentic2D.Engine;
using Agentic2D.Gameplay;

namespace Agentic2D.Tools;

/// <summary>Runs the same authored M019 intents through the registered runtime component boundary.</summary>
internal static class M019RuntimeProjection
{
    public static M019GameplayRuntime Execute(GameplayExecution execution)
    {
        var runtime = new M019GameplayRuntime();
        runtime.CreateEntity("entity.player", 0, new ResourceHealth(GameplayIds.Health, 10, 0, 10, 0), new Inventory("inventory.player", 2, [], 0));
        runtime.CreateEntity("entity.target", 0, new ResourceHealth(GameplayIds.Health, 5, 0, 5, 0));
        runtime.RegisterItem(new ItemDefinitionSource { Schema = "agentic2d.item-definition.v1", Id = "item.collectible-crystal", Stackable = true, MaximumStack = 10, VisualDefinitionId = "visual-definition.player.basic", DefaultCollectionCue = "cue.item.collection" });
        if (execution.CollectionIntents.Count > 0) runtime.CreateEntity("entity.world-item.crystal", 0, worldItem: new WorldItem("item.collectible-crystal", 2, 0));
        foreach (var intent in execution.DamageIntents) runtime.ApplyDamage(intent);
        foreach (var intent in execution.CollectionIntents) runtime.Collect(intent);
        return runtime;
    }
}
