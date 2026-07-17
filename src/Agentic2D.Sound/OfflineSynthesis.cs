using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Agentic2D.Sound;

/// <summary>Deterministic, offline-only authoring support for compact PCM cue assets.</summary>
public sealed record SoundSynthesisDefinition
{
    [JsonPropertyName("schema")] public string Schema { get; init; } = "";
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("outputAssetId")] public string OutputAssetId { get; init; } = "";
    [JsonPropertyName("outputPath")] public string OutputPath { get; init; } = "";
    [JsonPropertyName("segments")] public IReadOnlyList<SoundSynthesisSegment> Segments { get; init; } = [];
    [JsonPropertyName("tags")] public IReadOnlyList<string> Tags { get; init; } = [];
    [JsonPropertyName("provenance")] public Dictionary<string, JsonElement> Provenance { get; init; } = [];
}

public sealed record SoundSynthesisSegment
{
    [JsonPropertyName("oscillator")] public string Oscillator { get; init; } = "";
    [JsonPropertyName("startFrequency")] public double StartFrequency { get; init; }
    [JsonPropertyName("endFrequency")] public double? EndFrequency { get; init; }
    [JsonPropertyName("durationSeconds")] public double DurationSeconds { get; init; }
    [JsonPropertyName("gain")] public double Gain { get; init; } = 1;
    [JsonPropertyName("attackSeconds")] public double AttackSeconds { get; init; }
    [JsonPropertyName("decaySeconds")] public double DecaySeconds { get; init; }
    [JsonPropertyName("sustainLevel")] public double SustainLevel { get; init; } = 1;
    [JsonPropertyName("releaseSeconds")] public double ReleaseSeconds { get; init; }
    [JsonPropertyName("sampleRate")] public int SampleRate { get; init; } = 22050;
    [JsonPropertyName("noiseSeed")] public uint? NoiseSeed { get; init; }
}

public sealed record SoundSynthesisDiagnostic(string Id, string Severity, string Message, string Target);
public sealed record SoundSynthesisArtifact(string Schema, string Id, string OutputAssetId, string OutputPath, string DefinitionFingerprint, string ImplementationVersion, int SampleCount, int SampleRate, double DurationSeconds, double Peak, double Rms, string OutputSha256, IReadOnlyList<SoundSynthesisDiagnostic> Diagnostics);

public static class OfflineSoundSynthesis
{
    public const string Schema = "agentic2d.sound-synthesis.v1";
    public const string ImplementationVersion = "m025-offline-pcm-v1";
    public static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<SoundSynthesisDiagnostic> Validate(SoundSynthesisDefinition value, string target)
    {
        var diagnostics = new List<SoundSynthesisDiagnostic>();
        if (value.Schema != Schema || !Stable(value.Id) || !value.Id.StartsWith("sound-synthesis.", StringComparison.Ordinal) || !Stable(value.OutputAssetId) || string.IsNullOrWhiteSpace(value.OutputPath) || Path.IsPathRooted(value.OutputPath) || value.OutputPath.Contains("..", StringComparison.Ordinal)) diagnostics.Add(new("SYNTH0001", "error", "Synthesis schema, IDs, or relative output path is invalid.", target));
        if (value.Segments.Count is < 1 or > 8) diagnostics.Add(new("SYNTH0002", "error", "A synthesis definition requires one through eight segments.", target));
        foreach (var segment in value.Segments)
        {
            if (segment.Oscillator is not ("sine" or "square" or "triangle" or "noise")) diagnostics.Add(new("SYNTH0003", "error", "Unsupported oscillator.", target));
            if (!Finite(segment.StartFrequency) || segment.StartFrequency <= 0 || (segment.EndFrequency is not null && (!Finite(segment.EndFrequency.Value) || segment.EndFrequency.Value <= 0)) || !Finite(segment.DurationSeconds) || segment.DurationSeconds <= 0 || segment.DurationSeconds > 4 || !Finite(segment.Gain) || segment.Gain < 0 || segment.Gain > 1 || !Finite(segment.AttackSeconds) || !Finite(segment.DecaySeconds) || !Finite(segment.ReleaseSeconds) || segment.AttackSeconds < 0 || segment.DecaySeconds < 0 || segment.ReleaseSeconds < 0 || !Finite(segment.SustainLevel) || segment.SustainLevel < 0 || segment.SustainLevel > 1 || segment.AttackSeconds + segment.DecaySeconds + segment.ReleaseSeconds > segment.DurationSeconds || segment.SampleRate is not (22050 or 44100) || (segment.Oscillator == "noise" && segment.NoiseSeed is null)) diagnostics.Add(new("SYNTH0004", "error", "A segment has invalid bounded frequency, envelope, duration, gain, sample rate, or noise seed.", target));
        }
        if (value.Segments.Select(x => x.SampleRate).Distinct().Count() > 1) diagnostics.Add(new("SYNTH0005", "error", "All segments in a cue must use one sample rate.", target));
        return diagnostics;
    }

    public static SoundSynthesisArtifact Synthesize(SoundSynthesisDefinition definition, string outputDirectory)
    {
        var diagnostics = Validate(definition, definition.Id);
        if (diagnostics.Any(x => x.Severity == "error")) return new("agentic2d.sound-synthesis-artifact.v1", definition.Id, definition.OutputAssetId, definition.OutputPath, Fingerprint(definition), ImplementationVersion, 0, 0, 0, 0, 0, "", diagnostics);
        var samples = new List<short>();
        foreach (var segment in definition.Segments) samples.AddRange(Segment(segment));
        var bytes = Wav(samples, definition.Segments[0].SampleRate);
        var relative = definition.OutputPath.Replace('/', Path.DirectorySeparatorChar);
        var path = Path.Combine(outputDirectory, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, bytes);
        var peak = samples.Count == 0 ? 0 : samples.Max(x => Math.Abs((double)x) / short.MaxValue);
        var rms = samples.Count == 0 ? 0 : Math.Sqrt(samples.Select(x => Math.Pow(x / (double)short.MaxValue, 2)).Average());
        var artifact = new SoundSynthesisArtifact("agentic2d.sound-synthesis-artifact.v1", definition.Id, definition.OutputAssetId, definition.OutputPath, Fingerprint(definition), ImplementationVersion, samples.Count, definition.Segments[0].SampleRate, samples.Count / (double)definition.Segments[0].SampleRate, peak, rms, Hash(bytes), diagnostics);
        File.WriteAllText(Path.Combine(outputDirectory, Path.GetFileNameWithoutExtension(relative) + ".provenance.json"), JsonSerializer.Serialize(artifact, Json));
        return artifact;
    }

    private static IEnumerable<short> Segment(SoundSynthesisSegment s)
    {
        var count = (int)Math.Round(s.DurationSeconds * s.SampleRate, MidpointRounding.AwayFromZero); var state = s.NoiseSeed ?? 1u;
        for (var i = 0; i < count; i++)
        {
            var t = i / (double)s.SampleRate; var fraction = count <= 1 ? 0 : i / (double)(count - 1); var frequency = s.StartFrequency + ((s.EndFrequency ?? s.StartFrequency) - s.StartFrequency) * fraction; var phase = 2 * Math.PI * frequency * t;
            var wave = s.Oscillator switch { "sine" => Math.Sin(phase), "square" => Math.Sin(phase) >= 0 ? 1d : -1d, "triangle" => 2d / Math.PI * Math.Asin(Math.Sin(phase)), "noise" => Noise(ref state), _ => 0d };
            var envelope = Envelope(s, t); var value = Math.Clamp(wave * envelope * s.Gain, -1d, 1d); yield return (short)Math.Round(value * short.MaxValue, MidpointRounding.AwayFromZero);
        }
    }
    private static double Envelope(SoundSynthesisSegment s, double t)
    {
        if (t < s.AttackSeconds) return s.AttackSeconds == 0 ? 1 : t / s.AttackSeconds;
        if (t < s.AttackSeconds + s.DecaySeconds) return s.DecaySeconds == 0 ? s.SustainLevel : 1 - (1 - s.SustainLevel) * ((t - s.AttackSeconds) / s.DecaySeconds);
        if (t < s.DurationSeconds - s.ReleaseSeconds) return s.SustainLevel;
        return s.ReleaseSeconds == 0 ? 0 : s.SustainLevel * Math.Max(0, (s.DurationSeconds - t) / s.ReleaseSeconds);
    }
    private static double Noise(ref uint x) { x = x * 1664525u + 1013904223u; return ((x >> 8) / (double)0x00ffffff) * 2 - 1; }
    private static byte[] Wav(IReadOnlyList<short> samples, int rate)
    {
        var dataBytes = samples.Count * 2; using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream, Encoding.ASCII, true);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + dataBytes); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16); writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(dataBytes); foreach (var sample in samples) writer.Write(sample); writer.Flush(); return stream.ToArray();
    }
    public static string Fingerprint(object value) => "sha256:" + Hash(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, Json)));
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
    private static bool Finite(double value) => double.IsFinite(value);
    private static bool Stable(string value) => !string.IsNullOrWhiteSpace(value) && value.All(c => char.IsLetterOrDigit(c) || c is '.' or '-');
}
