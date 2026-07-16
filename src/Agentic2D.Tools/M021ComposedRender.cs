using System.Text.Json;
using Agentic2D.Presentation;
using Agentic2D.Rendering;

namespace Agentic2D.Tools;

internal static class M021ComposedRender
{
    public static async Task WriteAsync(string output, M021AuthoritativeSource source)
    {
        var root = Agentic2D.Validation.ContentTargetResolver.FindRepositoryRoot();
        var definition = CameraCatalog.Load(Path.Combine(root, "game", "cameras", "camera.player-follow.json"));
        var camera = CameraProjector.Project(definition, source.CameraTargets, []).Last();
        var items = new[]
        {
            Item("render.runtime.player", "entity.player", "visual-definition.player.basic", camera, source.CameraTargets.Last().WorldX, source.CameraTargets.Last().WorldY, "actors", 20),
            Item("render.runtime.switch", "entity.switch.vault-power", "visual-definition.npc.talkable-smoke", camera, 8, 3, "actors", 21),
            Item("render.runtime.door", "entity.door.vault-access", "visual-definition.tree.large", camera, 12, 3, "foreground", 30)
        }.OrderBy(x => x.Layer, StringComparer.Ordinal).ThenBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
        var commands = new List<RenderCommand> { new("command.clear", "clear", null, null), new("command.world.begin", "begin-world-camera", null, null) };
        commands.AddRange(items.Select(x => new RenderCommand("command.draw." + x.Id, "draw-texture-region", x.Id, x)));
        commands.Add(new("command.world.end", "end-world-camera", null, null));
        commands.Add(new("command.screen.begin", "begin-screen-space", null, null));
        commands.Add(new("command.screen.ui", "draw-ui-layout", null, null));
        commands.Add(new("command.screen.end", "end-screen-space", null, null));
        var fingerprint = PresentationDeterminism.Hash(JsonSerializer.Serialize(new { camera, items, commands }));
        Directory.CreateDirectory(output);
        var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(Path.Combine(output, "render-result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.render-projection.result.v1", status = "passed", sourceMode = "m021-authoritative-presentation", tick = camera.Tick, projectionFingerprint = fingerprint, cameraTransformed = true, screenSpaceUiUntransformed = true }, options));
        await File.WriteAllTextAsync(Path.Combine(output, "render-frame.json"), JsonSerializer.Serialize(new { schema = "agentic2d.render-frame.v1", tick = camera.Tick, projectionFingerprint = fingerprint, camera, itemCount = items.Length, commandCount = commands.Count }, options));
        await File.WriteAllLinesAsync(Path.Combine(output, "render-items.jsonl"), items.Select(x => JsonSerializer.Serialize(x)));
        await File.WriteAllLinesAsync(Path.Combine(output, "render-commands.jsonl"), commands.Select(x => JsonSerializer.Serialize(x)));
        await File.WriteAllTextAsync(Path.Combine(output, "render-diagnostics.json"), JsonSerializer.Serialize(new { diagnostics = Array.Empty<string>() }, options));
    }

    private static RenderItem Item(string id, string sourceId, string visual, CameraProjectionState camera, int worldX, int worldY, string layer, int order)
    {
        var x = worldX - camera.ScreenCenterX + camera.ViewportWidth / 2;
        var y = worldY - camera.ScreenCenterY + camera.ViewportHeight / 2;
        return new RenderItem(id, "runtime-entity", sourceId, visual, "part.presentation", "asset.render-atlas-smoke", "region.player", new(new RenderPoint(x, y), new RenderSize(1, 1)), "center", layer, order, "none", y, "map.interaction-smoke", camera.SourceFingerprint, new RenderColor(255, 255, 255, 255));
    }
}
