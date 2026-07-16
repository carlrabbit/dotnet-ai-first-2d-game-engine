using System.Text.Json;

namespace Agentic2D.Presentation;

public static class AuthoredEmitterCatalog
{
    public static ParticleEmitterDefinition Load(string effectPath)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(effectPath)); var root = document.RootElement;
        var emitter = root.GetProperty("particleEmitter");
        double[] Doubles(string name) => emitter.GetProperty(name).EnumerateArray().Select(x => x.GetDouble()).ToArray();
        byte[] Bytes(string name) => emitter.GetProperty(name).EnumerateArray().Select(x => x.GetByte()).ToArray();
        var result = new ParticleEmitterDefinition(emitter.GetProperty("id").GetString()!, emitter.GetProperty("visualDefinitionId").GetString()!, emitter.GetProperty("partId").GetString()!, emitter.GetProperty("particleCount").GetInt32(), emitter.GetProperty("durationTicks").GetInt32(), emitter.GetProperty("particleLifetimeTicks").GetInt32(), Doubles("spawnOffsetMin"), Doubles("spawnOffsetMax"), Doubles("velocityMin"), Doubles("velocityMax"), Doubles("scaleMinMax"), Doubles("rotationMinMax"), Doubles("angularVelocityMinMax"), Bytes("tintMin"), Bytes("tintMax"), emitter.GetProperty("scaleCurve").GetString()!, emitter.GetProperty("opacityCurve").GetString()!, emitter.GetProperty("layer").GetString()!);
        if (result.ParticleCount <= 0 || result.ParticleCount > 256 || result.ParticleLifetimeTicks <= 0 || result.ScaleCurve is not ("constant" or "linear" or "linear-inverse") || result.OpacityCurve is not ("constant" or "linear" or "linear-inverse")) throw new InvalidOperationException("PARTICLE0211: invalid authored emitter");
        return result;
    }
}
