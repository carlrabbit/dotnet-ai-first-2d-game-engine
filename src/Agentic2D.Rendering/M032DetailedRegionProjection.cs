using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Rendering;

/// <summary>Read-only structural projection for the M032 detailed-region proof.</summary>
public static class M032DetailedRegionProjection
{
    public static RenderFrame Project(M032Run run, string frameId, string activityOverlay)
    {
        var items = new List<RenderItem>();
        foreach (var entity in run.World.Entities.Where(x => x.RegionId == "region.forest.active").OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (!Position(entity, out var x, out var y)) continue;
            var (kind, color, size) = entity.Id.StartsWith("worker.", StringComparison.Ordinal) ? ("worker", new RenderColor(86, 156, 214, 255), 0.8) : entity.Id.StartsWith("tree.", StringComparison.Ordinal) ? ("tree", new RenderColor(42, 133, 58, 255), 0.9) : entity.Id.StartsWith("storage.", StringComparison.Ordinal) ? ("storage", new RenderColor(163, 105, 61, 255), 1d) : entity.Id.StartsWith("need.", StringComparison.Ordinal) ? ("need-source", new RenderColor(220, 183, 69, 255), 0.65) : ("other", new RenderColor(180, 180, 180, 255), 0.5);
            if (entity.Id.StartsWith("designation.", StringComparison.Ordinal)) continue;
            items.Add(Item($"render.m032.{kind}.{entity.Id}", kind, entity.Id, x, y, size, color, "world"));
            if (entity.Id.StartsWith("worker.", StringComparison.Ordinal) && entity.Components.TryGetValue("component.m032.worker", out var worker) && Property(worker, "wood").GetInt32() > 0) items.Add(Item($"render.m032.carried-wood.{entity.Id}", "carried-resource", entity.Id, x + .25, y - .25, .25, new RenderColor(203, 139, 72, 255), "overlay"));
        }
        foreach (var designation in M032AutonomousDetailedRegion.InspectDesignations(run.World).OrderBy(x => x.Id, StringComparer.Ordinal)) foreach (var cell in designation.Cells) items.Add(Item($"render.m032.designation.{designation.Id}.{cell.X}.{cell.Y}", "designation", designation.Id, cell.X, cell.Y, 1, designation.Enabled ? new RenderColor(78, 183, 255, 90) : new RenderColor(128, 128, 128, 70), "overlay"));
        foreach (var route in run.Navigation.Where(x => x.Status is "found" or "already-at-goal").OrderBy(x => x.RequestId, StringComparer.Ordinal)) foreach (var cell in route.Path) items.Add(Item($"render.m032.route.{route.RequestId}.{cell.X}.{cell.Y}", "route", route.ActorId, cell.X + .35, cell.Y + .35, .3, new RenderColor(255, 235, 85, 180), "overlay"));
        items.Add(Item($"render.m032.activity.{frameId}", "activity-overlay", activityOverlay, 0, 0, .1, new RenderColor(255, 255, 255, 255), "ui"));
        var ordered = items.OrderBy(x => x.Layer, StringComparer.Ordinal).ThenBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal).ToArray();
        var commands = new List<RenderCommand> { new("command.clear", "clear", null, null), new("command.world.begin", "begin-world-camera", null, null) };
        commands.AddRange(ordered.Where(x => x.Layer != "ui").Select(item => new RenderCommand("command.draw." + item.Id, "draw-solid-rectangle", item.Id, item)));
        commands.Add(new("command.world.end", "end-world-camera", null, null)); commands.Add(new("command.screen.begin", "begin-screen-space", null, null)); commands.AddRange(ordered.Where(x => x.Layer == "ui").Select(item => new RenderCommand("command.draw." + item.Id, "draw-text", item.Id, item, activityOverlay))); commands.Add(new("command.screen.end", "end-screen-space", null, null));
        var fingerprint = "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(ordered) + JsonSerializer.Serialize(commands)))).ToLowerInvariant();
        return new RenderFrame("agentic2d.render-frame.v1", "m032-detailed-region", M032AutonomousDetailedRegion.ScenarioId, "map.m032.detailed-forest", checked((int)(run.World.Clock.Now.Microseconds / 1_000_000)), run.Fingerprint, fingerprint, ordered, commands);
    }

    private static RenderItem Item(string id, string sourceKind, string sourceId, double x, double y, double size, RenderColor color, string layer) => new(id, sourceKind, sourceId, "", "", null, null, new(new RenderPoint(x, y), new RenderSize(size, size)), "center", layer, 0, "none", y, "map.m032.detailed-forest", "m032", color, new RenderGeometry("rectangle", color, null, 0, 1, 0, 4, 0, null));
    private static bool Position(SimulationEntity entity, out double x, out double y) { var component = entity.Components.Values.FirstOrDefault(value => value.ValueKind == JsonValueKind.Object && (value.TryGetProperty("x", out _) || value.TryGetProperty("X", out _))); if (component.ValueKind != JsonValueKind.Object) { x = y = 0; return false; } x = Property(component, "x").GetDouble(); y = Property(component, "y").GetDouble(); return true; }
    private static JsonElement Property(JsonElement value, string name) => value.TryGetProperty(name, out var property) ? property : value.GetProperty(char.ToUpperInvariant(name[0]) + name[1..]);
}
