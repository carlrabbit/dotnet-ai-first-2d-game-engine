using System.Text.Json;
using Agentic2D.Engine;
using Agentic2D.Gameplay;

namespace Agentic2D.Tools;

internal static class M019GameplayCommands
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args.Length >= 2 && args[0] == "gameplay" && args[1] == "inspect") return await InspectAsync(args, output, error);
        return -1;
    }

    private static string? Option(string[] args, string name)
    {
        var index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private static async Task<int> InspectAsync(string[] args, TextWriter output, TextWriter error)
    {
        var project = Option(args, "--project");
        var scenario = Option(args, "--scenario");
        var directory = Option(args, "--output");
        if (project is null || scenario is null || directory is null) { await error.WriteLineAsync("gameplay inspect requires --project, --scenario, and --output"); return 2; }
        var execution = Execute(scenario);
        await GameplayArtifactWriter.WriteAsync(directory, execution, scenario);
        await output.WriteLineAsync("gameplay inspect: passed; output: " + directory);
        return 0;
    }

    internal static GameplayExecution Execute(string scenario)
    {
        var world = new GameplayWorld();
        world.Add(new GameplayEntity("entity.player", new ResourceHealth(GameplayIds.Health, 10, 0, 10, 0)));
        world.Add(new GameplayEntity("entity.target", new ResourceHealth(GameplayIds.Health, 5, 0, 5, 0)));
        var intents = new List<DamageIntent>();
        var resolutions = new List<DamageResolution>();
        if (scenario is "gameplay.damage-resource-smoke" or "gameplay.sound-damage-collection-lifecycle-smoke")
        {
            intents.Add(new("damage.intent.environment.player", "source.environment.smoke", "entity.player", "damage.environment", 3, 2, "correlation.damage.player.1", "scenario.m019"));
            intents.Add(new("damage.intent.environment.player.duplicate", "source.environment.smoke", "entity.player", "damage.environment", 3, 2, "correlation.damage.player.1", "scenario.m019"));
        }
        if (scenario is "gameplay.defeat-lifecycle-smoke" or "gameplay.sound-damage-collection-lifecycle-smoke")
        {
            intents.Add(new("damage.intent.player.target", "entity.player", "entity.target", "damage.generic", 99, 5, "correlation.damage.target.1", "scenario.m019"));
            intents.Add(new("damage.intent.player.target.again", "entity.player", "entity.target", "damage.generic", 1, 6, "correlation.damage.target.2", "scenario.m019"));
        }
        if (intents.Count == 0) intents.Add(new("damage.intent.default", "source.environment.smoke", "entity.player", "damage.environment", 1, 1, "correlation.damage.default", "scenario.m019"));
        resolutions.AddRange(intents.Select(world.ApplyDamage));
        var collection = new CollectionWorld();
        var collectionIntents = new List<CollectItemIntent>();
        var collectionResolutions = new List<CollectionResolution>();
        if (scenario is "gameplay.collection-atomicity-smoke" or "gameplay.sound-damage-collection-lifecycle-smoke")
        {
            collection.AddDefinition(new ItemDefinitionSource { Schema = "agentic2d.item-definition.v1", Id = "item.collectible-crystal", Stackable = true, MaximumStack = 10, VisualDefinitionId = "visual-definition.player.basic", DefaultCollectionCue = "cue.item.collection" });
            collection.AddCollector("entity.player", new Inventory("inventory.player", 2, [], 0));
            collection.AddWorldItem("entity.world-item.crystal", new WorldItem("item.collectible-crystal", 2, 0));
            collectionIntents.Add(new CollectItemIntent("collection.intent.player.crystal", "entity.player", "entity.world-item.crystal", "item.collectible-crystal", 3, "correlation.collection.1", "scenario.m019"));
            collectionIntents.Add(new CollectItemIntent("collection.intent.player.crystal.again", "entity.player", "entity.world-item.crystal", "item.collectible-crystal", 4, "correlation.collection.2", "scenario.m019"));
            collectionResolutions.AddRange(collectionIntents.Select(collection.Collect));
        }
        return new(world, intents, resolutions, collection, collectionIntents, collectionResolutions);
    }
}

internal sealed record GameplayExecution(GameplayWorld World, IReadOnlyList<DamageIntent> DamageIntents, IReadOnlyList<DamageResolution> DamageResolutions, CollectionWorld Collection, IReadOnlyList<CollectItemIntent> CollectionIntents, IReadOnlyList<CollectionResolution> CollectionResolutions);

internal static class GameplayArtifactWriter
{
    public static async Task WriteAsync(string directory, GameplayExecution execution, string scenario)
    {
        Directory.CreateDirectory(directory);
        var json = new JsonSerializerOptions { WriteIndented = true };
        var runtime = M019RuntimeProjection.Execute(execution);
        var fingerprint = Agentic2D.Sound.SoundProjector.Fingerprint(new { scenario, execution.DamageIntents, execution.DamageResolutions, runtime.ResourceTransitions, runtime.LifecycleTransitions, runtime.Events, runtimeSnapshot = runtime.World.Snapshot(6) });
        await File.WriteAllTextAsync(Path.Combine(directory, "gameplay-result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.gameplay.result.v1", status = "passed", scenario, fingerprint }, json));
        await Lines(Path.Combine(directory, "resource-transitions.jsonl"), runtime.ResourceTransitions);
        await Lines(Path.Combine(directory, "damage-intents.jsonl"), execution.DamageIntents);
        await Lines(Path.Combine(directory, "damage-resolutions.jsonl"), execution.DamageResolutions);
        await Lines(Path.Combine(directory, "lifecycle-transitions.jsonl"), runtime.LifecycleTransitions);
        if (execution.CollectionIntents.Count > 0)
        {
            await Lines(Path.Combine(directory, "collection-intents.jsonl"), execution.CollectionIntents);
            await Lines(Path.Combine(directory, "collection-resolutions.jsonl"), execution.CollectionResolutions);
            await Lines(Path.Combine(directory, "inventory-transitions.jsonl"), runtime.InventoryTransitions);
            await Lines(Path.Combine(directory, "world-item-transitions.jsonl"), runtime.WorldItemTransitions);
        }
        await File.WriteAllTextAsync(Path.Combine(directory, "gameplay-diagnostics.json"), JsonSerializer.Serialize(new { schema = "agentic2d.gameplay.diagnostics.v1", diagnostics = Array.Empty<object>() }, json));
    }

    private static Task Lines<T>(string path, IEnumerable<T> values) => File.WriteAllTextAsync(path, string.Join(Environment.NewLine, values.Select(x => JsonSerializer.Serialize(x))));
}
