using System.Text.Json;

namespace Agentic2D.Tools;

public enum PreviewSurfaceKind { Image, Animation, Audio, Unsupported }

public sealed record PreviewContext(
    string Modality,
    string CandidateId,
    string PresentationRole,
    string SelectedVariant,
    IReadOnlyList<string> Corrections,
    string RawMeaning,
    string ProcessedMeaning,
    string SubjectiveQuestion,
    int? RawDurationSamples = null,
    int? ProcessedDurationSamples = null,
    int? SampleRate = null);

public sealed record PreviewSurface(PreviewSurfaceKind Kind, IReadOnlyList<string> Controls, PreviewContext Context, string Diagnostic)
{
    public bool Has(string control) => Controls.Contains(control, StringComparer.Ordinal);
}

public sealed class PreviewSurfaceState
{
    private readonly PreviewSurface surface;
    private double playbackElapsed;
    public int FrameIndex { get; private set; }
    public bool Playing { get; private set; }
    public double Speed { get; private set; } = 1;
    public string ImageComparison { get; private set; } = "processed";
    public bool IsolatedRegion { get; private set; } = true;
    public string Filtering { get; private set; } = "nearest";
    public bool Overlays { get; private set; } = true;
    public string AudioState { get; private set; } = "not played; playback is manual";
    public void SetAudioDiagnostic(string diagnostic) => AudioState = diagnostic;

    public PreviewSurfaceState(PreviewSurface surface) => this.surface = surface;
    public bool IsActive(string control) => control switch
    {
        "Source" => ImageComparison == "source",
        "Processed" => ImageComparison == "processed",
        "Isolated region" => IsolatedRegion,
        "Source context" => !IsolatedRegion,
        "Nearest" => Filtering == "nearest",
        "Smooth" => Filtering == "smooth",
        "Overlays" => Overlays,
        "Play" => Playing,
        "Pause" => !Playing,
        "0.5x" => Speed == .5,
        "1x" => Speed == 1,
        "2x" => Speed == 2,
        "Play Raw" => AudioState.StartsWith("raw/base playing", StringComparison.Ordinal),
        "Play Processed" => AudioState.StartsWith("processed/current-draft playing", StringComparison.Ordinal),
        _ => false,
    };
    public bool Apply(string control, int frameCount = 0)
    {
        if (!surface.Has(control)) return false;
        switch (control)
        {
            case "Source": ImageComparison = "source"; break;
            case "Processed": ImageComparison = "processed"; break;
            case "Isolated region": IsolatedRegion = true; break;
            case "Source context": IsolatedRegion = false; break;
            case "Nearest": Filtering = "nearest"; break;
            case "Smooth": Filtering = "smooth"; break;
            case "Overlays": Overlays = !Overlays; break;
            case "Play": Playing = true; break;
            case "Pause": Playing = false; break;
            case "Step": if (!Playing && frameCount > 0) FrameIndex = (FrameIndex + 1) % frameCount; break;
            case "Reset": FrameIndex = 0; Playing = false; playbackElapsed = 0; break;
            case "0.5x": Speed = .5; break;
            case "1x": Speed = 1; break;
            case "2x": Speed = 2; break;
            case "Play Raw": AudioState = "raw/base playing (explicit request)"; break;
            case "Play Processed": AudioState = "processed/current-draft playing (explicit request)"; break;
            case "Stop": AudioState = "stopped"; break;
        }
        return true;
    }
    public bool Advance(double seconds, int frameCount)
    {
        if (!Playing || frameCount < 2) return false;
        playbackElapsed += Math.Max(0, seconds) * Speed;
        var before = FrameIndex;
        while (playbackElapsed >= .25)
        {
            playbackElapsed -= .25;
            FrameIndex = (FrameIndex + 1) % frameCount;
        }
        return before != FrameIndex;
    }
}

public static class AssetPreviewSurfaceModel
{
    public static PreviewSurface FromBundle(JsonElement bundle)
    {
        var mediaKind = bundle.TryGetProperty("mediaKind", out var kind) && kind.ValueKind == JsonValueKind.String ? kind.GetString() : null;
        var context = ReadContext(bundle, mediaKind ?? "unsupported");
        return mediaKind switch
        {
            "image" => new(PreviewSurfaceKind.Image, ["Source", "Processed", "Isolated region", "Source context", "Nearest", "Smooth", "Overlays"], context, string.Empty),
            "animation" => new(PreviewSurfaceKind.Animation, ["Play", "Pause", "Step", "Reset", "0.5x", "1x", "2x"], context, string.Empty),
            "audio" => new(PreviewSurfaceKind.Audio, ["Play Raw", "Play Processed", "Stop"], context, string.Empty),
            _ => new(PreviewSurfaceKind.Unsupported, [], context with { Modality = "unsupported" }, "Unsupported or missing structured bundle mediaKind; no modality surface was selected."),
        };
    }

    private static PreviewContext ReadContext(JsonElement bundle, string modality)
    {
        if (bundle.TryGetProperty("reviewContext", out var value) && value.ValueKind == JsonValueKind.Object)
        {
            var corrections = value.TryGetProperty("corrections", out var correctionArray) && correctionArray.ValueKind == JsonValueKind.Array ? correctionArray.EnumerateArray().Select(x => x.GetString() ?? string.Empty).ToArray() : [];
            return new(
                modality,
                StringValue(value, "candidateId", "candidate unresolved"),
                StringValue(value, "presentationRole", "review subject"),
                StringValue(value, "selectedVariant", "none"),
                corrections,
                StringValue(value, "rawMeaning", "raw/base candidate media"),
                StringValue(value, "processedMeaning", "processed/current-draft media"),
                StringValue(value, "subjectiveQuestion", "Is this candidate clear and usable for its intended purpose?"),
                IntValue(value, "rawDurationSamples"), IntValue(value, "processedDurationSamples"), IntValue(value, "sampleRate"));
        }
        return new(modality, "candidate unresolved", "review subject", "none", [], "raw/base candidate media", "processed/current-draft media", "Is this candidate clear and usable for its intended purpose?");
    }

    private static string StringValue(JsonElement value, string name, string fallback) => value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String ? property.GetString() ?? fallback : fallback;
    private static int? IntValue(JsonElement value, string name) => value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var number) ? number : null;
}
