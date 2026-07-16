using System.Text.Json;

namespace Agentic2D.Presentation;

public sealed record CameraDefinition(
    string Id,
    string TargetSelector,
    int ViewportWidth,
    int ViewportHeight,
    string FollowPolicy,
    int InterpolationPerTick,
    int DeadZoneX,
    int DeadZoneY,
    int DeadZoneWidth,
    int DeadZoneHeight,
    int WorldX,
    int WorldY,
    int WorldWidth,
    int WorldHeight,
    string PixelSnap,
    int Zoom,
    string LayerProjectionPolicy,
    string Provenance);

public sealed record CameraTargetEvidence(string EntityId, int Tick, int WorldX, int WorldY, string SourceFingerprint);
public sealed record CameraProjectionState(string Id, string CameraId, int Tick, string TargetEntityId, int CenterX, int CenterY, int OffsetX, int OffsetY, int ScreenCenterX, int ScreenCenterY, int ViewportWidth, int ViewportHeight, string SourceFingerprint, string Fingerprint);

public static class CameraCatalog
{
    public static CameraDefinition Load(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        if (root.GetProperty("schema").GetString() != "agentic2d.camera-definition.v1") throw new InvalidOperationException("CAMERA0211: invalid camera schema");
        var viewport = root.GetProperty("logicalViewport"); var deadZone = root.GetProperty("deadZone"); var bounds = root.GetProperty("worldBounds");
        var definition = new CameraDefinition(root.GetProperty("id").GetString() ?? "", root.GetProperty("targetSelector").GetString() ?? "", viewport.GetProperty("width").GetInt32(), viewport.GetProperty("height").GetInt32(), root.GetProperty("followPolicy").GetString() ?? "", root.TryGetProperty("interpolationPerTick", out var interpolation) ? interpolation.GetInt32() : 0, deadZone.GetProperty("x").GetInt32(), deadZone.GetProperty("y").GetInt32(), deadZone.GetProperty("width").GetInt32(), deadZone.GetProperty("height").GetInt32(), bounds.GetProperty("x").GetInt32(), bounds.GetProperty("y").GetInt32(), bounds.GetProperty("width").GetInt32(), bounds.GetProperty("height").GetInt32(), root.GetProperty("pixelSnap").GetString() ?? "", root.GetProperty("zoom").GetInt32(), root.GetProperty("layerProjectionPolicy").GetString() ?? "", root.GetProperty("provenance").GetString() ?? "");
        if (definition.Id != "camera.player-follow" || definition.FollowPolicy is not ("immediate" or "bounded-linear-per-tick") || definition.ViewportWidth <= 0 || definition.ViewportHeight <= 0 || definition.Zoom != 1) throw new InvalidOperationException("CAMERA0212: invalid camera definition");
        return definition;
    }
}

public static class CameraProjector
{
    public static IReadOnlyList<CameraProjectionState> Project(CameraDefinition definition, IReadOnlyList<CameraTargetEvidence> targets, IReadOnlyList<CameraShakeRequest> shakes)
    {
        if (targets.Count == 0) throw new InvalidOperationException("CAMERA0213: missing deterministic target");
        if (targets.Select(x => (x.Tick, x.EntityId)).Distinct().Count() != targets.Count) throw new InvalidOperationException("CAMERA0214: ambiguous deterministic target");
        var centerX = definition.WorldX + definition.ViewportWidth / 2; var centerY = definition.WorldY + definition.ViewportHeight / 2;
        var results = new List<CameraProjectionState>();
        foreach (var target in targets.OrderBy(x => x.Tick).ThenBy(x => x.EntityId, StringComparer.Ordinal))
        {
            var nextX = Follow(centerX, target.WorldX, definition.DeadZoneX, definition.DeadZoneWidth, definition.ViewportWidth, definition.FollowPolicy, definition.InterpolationPerTick);
            var nextY = Follow(centerY, target.WorldY, definition.DeadZoneY, definition.DeadZoneHeight, definition.ViewportHeight, definition.FollowPolicy, definition.InterpolationPerTick);
            centerX = Clamp(nextX, definition.WorldX + definition.ViewportWidth / 2, definition.WorldX + definition.WorldWidth - definition.ViewportWidth / 2);
            centerY = Clamp(nextY, definition.WorldY + definition.ViewportHeight / 2, definition.WorldY + definition.WorldHeight - definition.ViewportHeight / 2);
            var shake = shakes.Where(x => target.Tick >= x.StartTick && target.Tick < x.StartTick + x.DurationTicks).OrderBy(x => x.RequestId, StringComparer.Ordinal).FirstOrDefault();
            var offsetX = shake is null ? 0 : PresentationDeterminism.Integer(shake.Seed, target.Tick * 2, -shake.MaximumX, shake.MaximumX);
            var offsetY = shake is null ? 0 : PresentationDeterminism.Integer(shake.Seed, target.Tick * 2 + 1, -shake.MaximumY, shake.MaximumY);
            var id = "camera-state." + definition.Id + "." + target.Tick;
            results.Add(new(id, definition.Id, target.Tick, target.EntityId, centerX, centerY, offsetX, offsetY, Snap(centerX + offsetX), Snap(centerY + offsetY), definition.ViewportWidth, definition.ViewportHeight, target.SourceFingerprint, PresentationDeterminism.Hash(id + "|" + centerX + "|" + centerY + "|" + offsetX + "|" + offsetY + "|" + target.SourceFingerprint)));
        }
        return results;
    }
    private static int Follow(int current, int target, int deadZoneStart, int deadZoneSize, int viewport, string policy, int maximum)
    {
        var low = current - viewport / 2 + deadZoneStart; var high = low + deadZoneSize;
        var desired = target < low ? current - (low - target) : target > high ? current + (target - high) : current;
        return policy == "immediate" ? desired : current + Math.Clamp(desired - current, -maximum, maximum);
    }
    private static int Clamp(int value, int minimum, int maximum) => Math.Clamp(value, minimum, Math.Max(minimum, maximum));
    private static int Snap(int value) => value;
}
