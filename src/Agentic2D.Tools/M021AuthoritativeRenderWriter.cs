using System.Text.Json;
using Agentic2D.Presentation;
using Agentic2D.Rendering;
using Agentic2D.ScenarioRunner;

namespace Agentic2D.Tools;

internal static class M021AuthoritativeRenderWriter
{
    public static async Task WriteAsync(string output, M021AuthoritativeSource source)
    {
        var root = Agentic2D.Validation.ContentTargetResolver.FindRepositoryRoot();
        var definition = CameraCatalog.Load(Path.Combine(root, "game", "cameras", "camera.player-follow.json"));
        var camera = CameraProjector.Project(definition, source.CameraTargets, []).Last();
        var target = source.CameraTargets.Last();
        var snapshot = new ScenarioPresentationSnapshot("presentation.persistent-world-player-facing-smoke", "map.interaction-smoke", camera.Tick, source.Snapshot.DeterministicSeed + ":" + source.Snapshot.RuntimeTick, [new ScenarioPresentationEntity("entity.player", "entity-definition.player.basic", target.WorldX, target.WorldY)]);
        var baseProjection = new RenderProjectionService().ProjectPresentationSnapshot(snapshot, "m021-authoritative-source");
        var items = baseProjection.Frame.Items.Select(item => item with
        {
            Destination = item.Destination with { Position = new RenderPoint(item.Destination.Position.X - camera.ScreenCenterX + camera.ViewportWidth / 2, item.Destination.Position.Y - camera.ScreenCenterY + camera.ViewportHeight / 2) },
            SnapshotFingerprint = camera.SourceFingerprint
        }).OrderBy(x => x.Layer, StringComparer.Ordinal).ThenBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
        var commands = new List<RenderCommand> { new("command.clear", "clear", null, null), new("command.world.begin", "begin-world-camera", null, null) };
        commands.AddRange(items.Select(x => new RenderCommand("command.draw." + x.Id, "draw-texture-region", x.Id, x)));
        commands.AddRange([new RenderCommand("command.world.end", "end-world-camera", null, null), new RenderCommand("command.screen.begin", "begin-screen-space", null, null), new RenderCommand("command.screen.ui", "draw-ui-layout", null, null), new RenderCommand("command.screen.end", "end-screen-space", null, null)]);
        var fingerprint = PresentationDeterminism.Hash(JsonSerializer.Serialize(new { camera, items, commands, source.Snapshot }));
        Directory.CreateDirectory(output); var options = new JsonSerializerOptions { WriteIndented = true };
        await File.WriteAllTextAsync(Path.Combine(output, "render-result.json"), JsonSerializer.Serialize(new { schema = "agentic2d.render-projection.result.v1", status = "passed", sourceMode = "m021-authoritative-render-projector", tick = camera.Tick, projectionFingerprint = fingerprint, cameraTransformed = true, screenSpaceUiUntransformed = true }, options));
        await File.WriteAllTextAsync(Path.Combine(output, "render-frame.json"), JsonSerializer.Serialize(new { schema = "agentic2d.render-frame.v1", tick = camera.Tick, projectionFingerprint = fingerprint, camera, itemCount = items.Length, commandCount = commands.Count }, options));
        await File.WriteAllLinesAsync(Path.Combine(output, "render-items.jsonl"), items.Select(x => JsonSerializer.Serialize(x)));
        await File.WriteAllLinesAsync(Path.Combine(output, "render-commands.jsonl"), commands.Select(x => JsonSerializer.Serialize(x)));
        await File.WriteAllTextAsync(Path.Combine(output, "render-diagnostics.json"), JsonSerializer.Serialize(new { diagnostics = baseProjection.Diagnostics }, options));
    }
}
