using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Globalization;

namespace Agentic2D.Presentation;

public sealed record EffectDefinition(string Id, int DurationTicks, string Domain, string? EmitterId, string? NotificationTextId, bool RequestsShake, string Provenance);
public sealed record PresentationEvent(string EventId, string Type, int Tick, string? SourceEntityId, string? TargetEntityId, string Anchor, string Provenance);
public sealed record EffectRequest(string RequestId, string DefinitionId, string SourceEventOrOperationId, string? SourceEntityId, string? TargetEntityId, int RuntimeTick, string Anchor, int OccurrenceOrdinal, string SeedContext, string Provenance);
public sealed record EffectInstance(string InstanceId, string DefinitionId, string SourceRequestId, string SourceEventId, int StartTick, int DurationTicks, string Seed, int CurrentAge, string State, IReadOnlyList<string> ChildPresentationRequests, string Fingerprint);
public sealed record ParticleEmitterDefinition(string Id, string VisualDefinitionId, string PartId, int ParticleCount, int DurationTicks, int ParticleLifetimeTicks, double[] SpawnOffsetMin, double[] SpawnOffsetMax, double[] VelocityMin, double[] VelocityMax, double[] ScaleMinMax, double[] RotationMinMax, double[] AngularVelocityMinMax, byte[] TintMin, byte[] TintMax, string ScaleCurve, string OpacityCurve, string Layer);
public sealed record ParticleSpawn(string Id, string EmitterId, string EffectInstanceId, int Ordinal, int SpawnTick, int LifetimeTicks, double X, double Y, double VelocityX, double VelocityY, double Scale, double Rotation, double AngularVelocity, byte R, byte G, byte B, byte A, string Seed, string Fingerprint);
public sealed record ParticleSample(string ParticleId, int Tick, int Age, double X, double Y, double Rotation, double Scale, byte Opacity, string Fingerprint);
public sealed record CameraShakeRequest(string RequestId, string SourceEffectOrEventId, int StartTick, int DurationTicks, int MaximumX, int MaximumY, string FrequencyPolicy, string Seed);

public static class PresentationDeterminism
{
    public static string Id(string prefix, params object?[] values) => prefix + "." + Hash(string.Join("|", values.Select(x => x?.ToString() ?? "")))[..20];
    public static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
    public static int Integer(string seed, int ordinal, int min, int max)
    {
        if (max <= min) return min;
        var value = Convert.ToUInt32(Hash(seed + "|" + ordinal)[..8], 16);
        return min + (int)(value % (uint)(max - min + 1));
    }
    public static double Number(string seed, int ordinal, double min, double max)
    {
        if (max <= min) return Round(min);
        var unit = Convert.ToUInt32(Hash(seed + "|" + ordinal)[..8], 16) / (double)uint.MaxValue;
        return Round(min + ((max - min) * unit));
    }
    public static double Round(double value) => Math.Round(value, 4, MidpointRounding.AwayFromZero);
}

public static class EffectCatalog
{
    public static IReadOnlyList<EffectDefinition> Load(string root, out IReadOnlyList<string> diagnostics)
    {
        var results = new List<EffectDefinition>(); var errors = new List<string>();
        foreach (var path in Directory.Exists(root) ? Directory.EnumerateFiles(root, "*.json").Order(StringComparer.Ordinal) : Enumerable.Empty<string>())
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(path)); var item = doc.RootElement;
                var schema = item.TryGetProperty("schema", out var s) ? s.GetString() : null;
                var id = item.TryGetProperty("id", out var i) ? i.GetString() : null;
                var duration = item.TryGetProperty("durationTicks", out var d) ? d.GetInt32() : 0;
                if (schema != "agentic2d.presentation-effect.v1" || string.IsNullOrWhiteSpace(id) || !id.StartsWith("effect.", StringComparison.Ordinal) || duration <= 0) { errors.Add("EFFECT0211: invalid definition " + path); continue; }
                var domain = item.GetProperty("domain").GetString() ?? "world";
                if (domain is not ("world" or "screen")) { errors.Add("EFFECT0212: invalid domain " + id); continue; }
                results.Add(new(id, duration, domain, Optional(item, "emitterId"), Optional(item, "notificationTextId"), item.TryGetProperty("cameraShake", out var shake) && shake.ValueKind == JsonValueKind.Object, Optional(item, "provenance") ?? "authored"));
            }
            catch (Exception e) when (e is JsonException or InvalidOperationException) { errors.Add("EFFECT0213: malformed JSON " + path); }
        }
        var duplicate = results.GroupBy(x => x.Id, StringComparer.Ordinal).Where(x => x.Count() != 1).Select(x => x.Key);
        errors.AddRange(duplicate.Select(x => "EFFECT0214: duplicate ID " + x));
        diagnostics = errors.Order(StringComparer.Ordinal).ToArray(); return results.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
    }
    private static string? Optional(JsonElement item, string property) => item.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
}

public sealed class EffectProjector
{
    private static readonly IReadOnlyDictionary<string, string> Mappings = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["entity.damaged"] = "effect.damage-feedback",
        ["item.collected"] = "effect.collection-burst",
        ["switch.activated"] = "effect.switch-activation",
        ["door.opened"] = "effect.door-open",
        ["save.created"] = "effect.save-confirmation"
    };
    public IReadOnlyDictionary<string, string> EventMappings => Mappings;
    public IReadOnlyList<EffectRequest> Requests(IReadOnlyList<PresentationEvent> events, string scenarioSeed)
    {
        var ordinals = new Dictionary<string, int>(StringComparer.Ordinal); var output = new List<EffectRequest>();
        foreach (var source in events.OrderBy(x => x.Tick).ThenBy(x => x.EventId, StringComparer.Ordinal))
        {
            if (!Mappings.TryGetValue(source.Type, out var definition)) continue;
            var key = source.EventId + "|" + definition; ordinals.TryGetValue(key, out var ordinal); ordinals[key] = ++ordinal;
            var context = scenarioSeed + "|" + definition + "|" + source.EventId + "|" + source.Tick + "|" + ordinal;
            output.Add(new(PresentationDeterminism.Id("effect-request", source.EventId, definition, ordinal), definition, source.EventId, source.SourceEntityId, source.TargetEntityId, source.Tick, source.Anchor, ordinal, context, source.Provenance));
        }
        return output;
    }
    public IReadOnlyList<EffectInstance> Instances(IReadOnlyList<EffectRequest> requests, IReadOnlyDictionary<string, EffectDefinition> definitions, int observedTick)
    {
        var output = new List<EffectInstance>();
        foreach (var request in requests.OrderBy(x => x.RuntimeTick).ThenBy(x => x.RequestId, StringComparer.Ordinal))
        {
            if (!definitions.TryGetValue(request.DefinitionId, out var definition)) continue;
            var id = PresentationDeterminism.Id("effect-instance", request.RequestId, request.DefinitionId);
            var seed = PresentationDeterminism.Hash(request.SeedContext + "|" + id);
            var age = Math.Max(0, observedTick - request.RuntimeTick); var state = age >= definition.DurationTicks ? "completed" : "active";
            var children = new List<string>(); if (definition.EmitterId is not null) children.Add("particle-request." + id); if (definition.NotificationTextId is not null) children.Add("notification-request." + id); if (definition.RequestsShake) children.Add("camera-request." + id);
            var fingerprint = PresentationDeterminism.Hash(id + "|" + definition.Id + "|" + request.SourceEventOrOperationId + "|" + request.RuntimeTick + "|" + definition.DurationTicks + "|" + seed);
            output.Add(new(id, definition.Id, request.RequestId, request.SourceEventOrOperationId, request.RuntimeTick, definition.DurationTicks, seed, age, state, children, fingerprint));
        }
        return output;
    }
    public IReadOnlyList<CameraShakeRequest> Shakes(IReadOnlyList<EffectInstance> instances, IReadOnlyDictionary<string, EffectDefinition> definitions) => instances.Where(x => definitions.TryGetValue(x.DefinitionId, out var d) && d.RequestsShake).OrderBy(x => x.InstanceId, StringComparer.Ordinal).Select(x => new CameraShakeRequest(PresentationDeterminism.Id("camera-shake", x.InstanceId), x.InstanceId, x.StartTick, x.DurationTicks, 3, 2, "tick-hash-v1", PresentationDeterminism.Hash(x.Seed + "|shake"))).ToArray();
}

public static class ParticleProjector
{
    public static IReadOnlyList<ParticleSpawn> Spawn(ParticleEmitterDefinition emitter, EffectInstance effect, int sourceTick, string anchor, string scenarioSeed)
    {
        var xy = anchor.Split(',', StringSplitOptions.TrimEntries);
        var baseX = xy.Length > 0 && double.TryParse(xy[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var px) ? px : 0;
        var baseY = xy.Length > 1 && double.TryParse(xy[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var py) ? py : 0;
        var result = new List<ParticleSpawn>();
        for (var ordinal = 0; ordinal < emitter.ParticleCount; ordinal++)
        {
            var seed = PresentationDeterminism.Hash(string.Join("|", scenarioSeed, effect.DefinitionId, effect.InstanceId, effect.SourceEventId, sourceTick, ordinal));
            var x = PresentationDeterminism.Round(baseX + PresentationDeterminism.Number(seed, 0, emitter.SpawnOffsetMin[0], emitter.SpawnOffsetMax[0])); var y = PresentationDeterminism.Round(baseY + PresentationDeterminism.Number(seed, 1, emitter.SpawnOffsetMin[1], emitter.SpawnOffsetMax[1]));
            var vx = PresentationDeterminism.Number(seed, 2, emitter.VelocityMin[0], emitter.VelocityMax[0]); var vy = PresentationDeterminism.Number(seed, 3, emitter.VelocityMin[1], emitter.VelocityMax[1]); var scale = PresentationDeterminism.Number(seed, 4, emitter.ScaleMinMax[0], emitter.ScaleMinMax[1]); var rotation = PresentationDeterminism.Number(seed, 5, emitter.RotationMinMax[0], emitter.RotationMinMax[1]); var angular = PresentationDeterminism.Number(seed, 6, emitter.AngularVelocityMinMax[0], emitter.AngularVelocityMinMax[1]);
            var r = (byte)PresentationDeterminism.Integer(seed, 7, emitter.TintMin[0], emitter.TintMax[0]); var g = (byte)PresentationDeterminism.Integer(seed, 8, emitter.TintMin[1], emitter.TintMax[1]); var b = (byte)PresentationDeterminism.Integer(seed, 9, emitter.TintMin[2], emitter.TintMax[2]); var a = (byte)PresentationDeterminism.Integer(seed, 10, emitter.TintMin[3], emitter.TintMax[3]); var id = PresentationDeterminism.Id("particle", effect.InstanceId, ordinal);
            result.Add(new(id, emitter.Id, effect.InstanceId, ordinal, sourceTick, emitter.ParticleLifetimeTicks, x, y, vx, vy, scale, rotation, angular, r, g, b, a, seed, PresentationDeterminism.Hash(id + "|" + seed)));
        }
        return result;
    }
    public static IReadOnlyList<ParticleSample> Sample(IEnumerable<ParticleSpawn> particles, int tick, string scaleCurve, string opacityCurve) => particles.Where(x => tick >= x.SpawnTick && tick < x.SpawnTick + x.LifetimeTicks).OrderBy(x => x.Id, StringComparer.Ordinal).Select(x =>
    {
        var age = tick - x.SpawnTick; var unit = age / (double)x.LifetimeTicks; var scale = Curve(x.Scale, unit, scaleCurve); var opacity = (byte)Math.Clamp((int)Math.Round(x.A * Curve(1, unit, opacityCurve), MidpointRounding.AwayFromZero), 0, 255); var px = PresentationDeterminism.Round(x.X + (x.VelocityX * age)); var py = PresentationDeterminism.Round(x.Y + (x.VelocityY * age)); var rotation = PresentationDeterminism.Round(x.Rotation + (x.AngularVelocity * age)); return new ParticleSample(x.Id, tick, age, px, py, rotation, scale, opacity, PresentationDeterminism.Hash(x.Id + "|" + tick + "|" + px + "|" + py + "|" + rotation + "|" + scale + "|" + opacity));
    }).ToArray();
    private static double Curve(double initial, double unit, string curve) => PresentationDeterminism.Round(curve switch { "constant" => initial, "linear" => initial * unit, "linear-inverse" => initial * (1 - unit), _ => throw new InvalidOperationException("unsupported particle curve " + curve) });
}
