using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic2D.Validation;

namespace Agentic2D.Animation;

/// <summary>Typed, presentation-only animation data. It has no runtime world reference and cannot mutate components.</summary>
public static class AnimationProperties
{
    public const string Region = "visual.region";
    public static readonly IReadOnlyDictionary<string, string> ValueTypes = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [Region] = "asset-region",
        ["visual.offset.x"] = "scalar",
        ["visual.offset.y"] = "scalar",
        ["visual.scale.x"] = "scalar",
        ["visual.scale.y"] = "scalar",
        ["visual.rotation-degrees"] = "scalar",
        ["visual.tint.red"] = "scalar",
        ["visual.tint.green"] = "scalar",
        ["visual.tint.blue"] = "scalar",
        ["visual.opacity"] = "scalar"
    };
    public static bool IsValid(string property, string type) => ValueTypes.TryGetValue(property, out var expected) && expected == type;
    public static bool ValidScalar(string property, double value) => double.IsFinite(value) &&
        (property is not ("visual.scale.x" or "visual.scale.y") || value > 0) &&
        (property is not ("visual.tint.red" or "visual.tint.green" or "visual.tint.blue" or "visual.opacity") || value is >= 0 and <= 1);
}

public sealed record AnimationDefinitionSource
{
    [JsonPropertyName("schema")] public string Schema { get; init; } = "";
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("visualDefinitionId")] public string VisualDefinitionId { get; init; } = "";
    [JsonPropertyName("clips")] public IReadOnlyList<AnimationClipSource> Clips { get; init; } = [];
    [JsonPropertyName("provenance")] public Dictionary<string, JsonElement>? Provenance { get; init; }
}
public sealed record AnimationClipSource
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("durationTicks")] public int DurationTicks { get; init; }
    [JsonPropertyName("loop")] public string Loop { get; init; } = "";
    [JsonPropertyName("tracks")] public IReadOnlyList<AnimationTrackSource> Tracks { get; init; } = [];
    [JsonPropertyName("spriteSequences")] public IReadOnlyList<SpriteSequenceSource> SpriteSequences { get; init; } = [];
    [JsonPropertyName("markers")] public IReadOnlyList<PresentationMarkerSource> Markers { get; init; } = [];
}
public sealed record AnimationTrackSource
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("partId")] public string PartId { get; init; } = "";
    [JsonPropertyName("property")] public string Property { get; init; } = "";
    [JsonPropertyName("valueType")] public string ValueType { get; init; } = "";
    [JsonPropertyName("interpolation")] public string Interpolation { get; init; } = "";
    [JsonPropertyName("keyframes")] public IReadOnlyList<AnimationKeyframeSource> Keyframes { get; init; } = [];
}
public sealed record AnimationKeyframeSource
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("tick")] public int Tick { get; init; }
    [JsonPropertyName("scalar")] public double? Scalar { get; init; }
    [JsonPropertyName("regionId")] public string? RegionId { get; init; }
}
public sealed record SpriteSequenceSource
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("kind")] public string Kind { get; init; } = "";
    [JsonPropertyName("partId")] public string PartId { get; init; } = "";
    [JsonPropertyName("regions")] public IReadOnlyList<string> Regions { get; init; } = [];
    [JsonPropertyName("ticksPerFrame")] public int TicksPerFrame { get; init; }
    [JsonPropertyName("loop")] public string Loop { get; init; } = "";
}
public sealed record PresentationMarkerSource
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("tick")] public int Tick { get; init; }
    [JsonPropertyName("kind")] public string Kind { get; init; } = "";
    [JsonPropertyName("payload")] public Dictionary<string, JsonElement> Payload { get; init; } = [];
}

public sealed record CompiledAnimation(string Schema, string Id, string VisualDefinitionId, IReadOnlyList<CompiledClip> Clips, string Fingerprint);
public sealed record CompiledClip(string Id, int DurationTicks, string Loop, IReadOnlyList<CompiledTrack> Tracks, IReadOnlyList<CompiledMarker> Markers);
public sealed record CompiledTrack(string Id, string PartId, string Property, string ValueType, string Interpolation, IReadOnlyList<CompiledKeyframe> Keyframes);
public sealed record CompiledKeyframe(string Id, int Tick, double? Scalar, string? RegionId);
public sealed record CompiledMarker(string Id, int Tick, string Kind, IReadOnlyDictionary<string, JsonElement> Payload);
public sealed record AnimationDiagnostic(string Id, string Severity, string Message, string Target, string? Field = null, string? ItemId = null);
public sealed record AnimationValidationRun(IReadOnlyList<CompiledAnimation> Animations, IReadOnlyList<AnimationDiagnostic> Diagnostics)
{ public bool Passed => Diagnostics.All(x => x.Severity != "error"); }

public sealed class AnimationCompiler
{
    private static readonly HashSet<string> MarkerKinds = new(StringComparer.Ordinal) { "presentation.footstep", "presentation.effect", "presentation.debug", "presentation.animation-complete" };
    private readonly VisualDefinitionCatalog visuals;
    private readonly IReadOnlyDictionary<string, HashSet<string>> assetRegions;
    public AnimationCompiler()
    {
        visuals = VisualDefinitionCatalog.LoadAll(out _);
        assetRegions = LoadAssetRegions();
    }
    public AnimationValidationRun LoadAndCompileAll()
    {
        var root = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game", "animations");
        var diagnostics = new List<AnimationDiagnostic>(); var compiled = new List<CompiledAnimation>();
        foreach (var path in Directory.Exists(root) ? Directory.EnumerateFiles(root, "*.json").Order(StringComparer.Ordinal).ToArray() : Array.Empty<string>())
        {
            try { var source = JsonSerializer.Deserialize<AnimationDefinitionSource>(File.ReadAllText(path), JsonOptions) ?? new(); var value = Compile(source, ContentTargetResolver.ToRepositoryRelativePath(path), diagnostics); if (value is not null) compiled.Add(value); }
            catch (JsonException ex) { diagnostics.Add(D("ANIMATION0001", "Malformed animation JSON: " + ex.Message, path)); }
        }
        return new(compiled.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray(), diagnostics.OrderBy(x => x.Id).ThenBy(x => x.Target).ThenBy(x => x.ItemId).ToArray());
    }
    public CompiledAnimation? Compile(AnimationDefinitionSource source, string target, List<AnimationDiagnostic>? external = null)
    {
        var ds = external ?? new List<AnimationDiagnostic>(); var start = ds.Count;
        if (source.Schema != "agentic2d.animation-definition.v1" || !Stable(source.Id) || !source.Id.StartsWith("animation-definition.", StringComparison.Ordinal)) ds.Add(D("ANIMATION0001", "Invalid animation definition schema or stable ID.", target, "schema", source.Id));
        if (!visuals.TryGet(source.VisualDefinitionId, out var visual) || visual is null) ds.Add(D("ANIMATION0002", "visualDefinitionId does not resolve.", target, "visualDefinitionId", source.Id));
        var clipIds = new HashSet<string>(StringComparer.Ordinal); var clips = new List<CompiledClip>();
        foreach (var clip in source.Clips.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            if (!Stable(clip.Id) || !clipIds.Add(clip.Id)) ds.Add(D("ANIMATION0003", "Clip ID is invalid or duplicated.", target, "clips[].id", clip.Id));
            if (clip.DurationTicks <= 0 || clip.Loop is not ("once" or "repeat")) ds.Add(D("ANIMATION0004", "durationTicks must be positive and loop must be once or repeat.", target, "clips", clip.Id));
            var trackIds = new HashSet<string>(StringComparer.Ordinal); var targets = new HashSet<string>(StringComparer.Ordinal); var tracks = new List<CompiledTrack>();
            foreach (var track in clip.Tracks.OrderBy(x => x.Id, StringComparer.Ordinal)) tracks.Add(CompileTrack(track, clip, visual, target, ds, trackIds, targets));
            foreach (var seq in clip.SpriteSequences.OrderBy(x => x.Id, StringComparer.Ordinal)) tracks.Add(CompileSequence(seq, clip, visual, target, ds, trackIds, targets));
            var markerIds = new HashSet<string>(StringComparer.Ordinal); var markers = new List<CompiledMarker>();
            foreach (var marker in clip.Markers.OrderBy(x => x.Tick).ThenBy(x => x.Id, StringComparer.Ordinal))
            {
                if (!Stable(marker.Id) || !markerIds.Add(marker.Id) || marker.Tick < 0 || marker.Tick >= clip.DurationTicks || !MarkerKinds.Contains(marker.Kind)) ds.Add(D("ANIMATION0007", "Marker ID, tick, or kind is invalid.", target, "markers", marker.Id));
                markers.Add(new(marker.Id, marker.Tick, marker.Kind, marker.Payload));
            }
            clips.Add(new(clip.Id, clip.DurationTicks, clip.Loop, tracks.OrderBy(x => x.PartId).ThenBy(x => x.Property).ThenBy(x => x.Id).ToArray(), markers));
        }
        if (ds.Count > start) return null;
        var canonical = new CompiledAnimation("agentic2d.compiled-animation.v1", source.Id, source.VisualDefinitionId, clips, "");
        return canonical with { Fingerprint = Fingerprint(canonical) };
    }
    private CompiledTrack CompileTrack(AnimationTrackSource track, AnimationClipSource clip, VisualDefinitionSource? visual, string target, List<AnimationDiagnostic> ds, HashSet<string> ids, HashSet<string> targets)
    {
        ValidateTrack(track.Id, track.PartId, track.Property, track.ValueType, track.Interpolation, visual, target, ds, ids, targets);
        var keyIds = new HashSet<string>(StringComparer.Ordinal); var ticks = new HashSet<int>(); var frames = new List<CompiledKeyframe>();
        foreach (var key in track.Keyframes.OrderBy(x => x.Tick).ThenBy(x => x.Id, StringComparer.Ordinal))
        {
            if (!Stable(key.Id) || !keyIds.Add(key.Id) || !ticks.Add(key.Tick) || key.Tick < 0 || key.Tick >= clip.DurationTicks) ds.Add(D("ANIMATION0006", "Keyframe ID or tick is invalid.", target, "keyframes", key.Id));
            if (track.ValueType == "scalar" && (key.Scalar is null || !AnimationProperties.ValidScalar(track.Property, key.Scalar.Value))) ds.Add(D("ANIMATION0006", "Scalar keyframe is not finite or range-valid.", target, "keyframes", key.Id));
            if (track.ValueType == "asset-region" && (string.IsNullOrWhiteSpace(key.RegionId) || !RegionExists(key.RegionId!))) ds.Add(D("ANIMATION0002", "Region keyframe does not resolve through asset metadata.", target, "keyframes", key.Id));
            frames.Add(new(key.Id, key.Tick, key.Scalar, key.RegionId));
        }
        if (!ticks.Contains(0)) ds.Add(D("ANIMATION0006", "Every track requires a tick-zero keyframe.", target, "keyframes", track.Id));
        return new(track.Id, track.PartId, track.Property, track.ValueType, track.Interpolation, frames);
    }
    private CompiledTrack CompileSequence(SpriteSequenceSource sequence, AnimationClipSource clip, VisualDefinitionSource? visual, string target, List<AnimationDiagnostic> ds, HashSet<string> ids, HashSet<string> targets)
    {
        if (sequence.Kind != "sprite-sequence" || sequence.TicksPerFrame <= 0 || sequence.Loop != clip.Loop || sequence.Regions.Count == 0) ds.Add(D("ANIMATION0008", "Sprite-sequence shorthand is inconsistent.", target, "spriteSequences", sequence.Id));
        ValidateTrack(sequence.Id, sequence.PartId, AnimationProperties.Region, "asset-region", "step", visual, target, ds, ids, targets);
        var frames = sequence.Regions.Select((region, index) => new CompiledKeyframe(sequence.Id + ".frame." + index, index * Math.Max(1, sequence.TicksPerFrame), null, region)).Where(x => x.Tick < clip.DurationTicks).ToArray();
        foreach (var frame in frames) if (!RegionExists(frame.RegionId!)) ds.Add(D("ANIMATION0002", "Sprite-sequence region does not resolve.", target, "spriteSequences", sequence.Id));
        return new(sequence.Id, sequence.PartId, AnimationProperties.Region, "asset-region", "step", frames);
    }
    private static void ValidateTrack(string id, string part, string property, string valueType, string interpolation, VisualDefinitionSource? visual, string target, List<AnimationDiagnostic> ds, HashSet<string> ids, HashSet<string> targets)
    {
        if (!Stable(id) || !ids.Add(id) || visual is null || !visual.Parts.Any(x => x.Id == part) || !AnimationProperties.IsValid(property, valueType) || interpolation is not ("step" or "linear") || (valueType == "asset-region" && interpolation != "step") || !targets.Add(part + "|" + property)) ds.Add(D("ANIMATION0005", "Track target/type/interpolation is invalid or duplicate within the clip.", target, "tracks", id));
    }
    private static bool Stable(string value) => !string.IsNullOrWhiteSpace(value) && value.All(x => char.IsLetterOrDigit(x) || x is '.' or '-');
    private bool RegionExists(string region) => assetRegions.Values.Any(x => x.Contains(region));
    private static AnimationDiagnostic D(string id, string message, string target, string? field = null, string? item = null) => new(id, "error", message, target, field, item);
    public static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    public static string Fingerprint(object value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, JsonOptions)))).ToLowerInvariant();
    private static IReadOnlyDictionary<string, HashSet<string>> LoadAssetRegions()
    {
        var root = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game/assets/metadata"); var result = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var path in Directory.EnumerateFiles(root, "*.asset.json").Order(StringComparer.Ordinal)) { var asset = JsonSerializer.Deserialize<AssetMetadataSource>(File.ReadAllText(path), JsonOptions); if (asset?.Id is not null) result[asset.Id] = asset.Tiles.Where(x => x.Id is not null).Select(x => x.Id!).ToHashSet(StringComparer.Ordinal); }
        return result;
    }
}

public sealed record AnimationSelection(string Layer, string ClipId, string SelectionKey, string Reason, int StartedAtRuntimeTick);
public sealed record DerivedPlaybackState(string ClipId, string SelectionKey, int StartedAtRuntimeTick, int ElapsedTicks, int LocalTick, int LoopIteration, string Status);
public sealed record SampledPresentationPatch(string PartId, string Property, double? Scalar, string? RegionId, string TrackId, string KeyframeId, string Layer);
public sealed record SampledLayer(AnimationSelection Selection, DerivedPlaybackState Playback, IReadOnlyList<SampledPresentationPatch> Patches);
public sealed record PresentationMarkerOccurrence(string SourceId, string AnimationDefinitionId, string Layer, string ClipId, string SelectionKey, string MarkerId, string MarkerKind, int RuntimeObservationTick, int LocalMarkerTick, int LoopIteration, IReadOnlyDictionary<string, JsonElement> Payload);

public sealed class AnimationSampler
{
    public SampledLayer Sample(CompiledAnimation animation, AnimationSelection selection, int runtimeTick)
    {
        if (runtimeTick < selection.StartedAtRuntimeTick) throw new ArgumentOutOfRangeException(nameof(runtimeTick), "Samples before selection start are invalid.");
        var clip = animation.Clips.Single(x => x.Id == selection.ClipId); var elapsed = runtimeTick - selection.StartedAtRuntimeTick;
        var state = clip.Loop == "repeat" ? new DerivedPlaybackState(clip.Id, selection.SelectionKey, selection.StartedAtRuntimeTick, elapsed, elapsed % clip.DurationTicks, elapsed / clip.DurationTicks, "playing") : new DerivedPlaybackState(clip.Id, selection.SelectionKey, selection.StartedAtRuntimeTick, elapsed, Math.Min(elapsed, clip.DurationTicks - 1), 0, elapsed >= clip.DurationTicks ? "completed" : "playing");
        var patches = clip.Tracks.Select(track => SampleTrack(track, state.LocalTick, selection.Layer)).OrderBy(x => x.PartId).ThenBy(x => x.Property).ToArray(); return new(selection, state, patches);
    }
    public IReadOnlyList<PresentationMarkerOccurrence> Markers(CompiledAnimation animation, AnimationSelection selection, int? previousObservationTick, int runtimeTick, string sourceId)
    {
        if (runtimeTick < selection.StartedAtRuntimeTick) return [];
        var clip = animation.Clips.Single(x => x.Id == selection.ClipId); var first = previousObservationTick is null ? 0 : Math.Max(0, previousObservationTick.Value - selection.StartedAtRuntimeTick + 1); var last = runtimeTick - selection.StartedAtRuntimeTick;
        if (clip.Loop == "once") last = Math.Min(last, clip.DurationTicks - 1); if (last < first) return [];
        var values = new List<PresentationMarkerOccurrence>();
        for (var elapsed = first; elapsed <= last; elapsed++) { var loop = clip.Loop == "repeat" ? elapsed / clip.DurationTicks : 0; var local = clip.Loop == "repeat" ? elapsed % clip.DurationTicks : elapsed; foreach (var marker in clip.Markers.Where(x => x.Tick == local).OrderBy(x => x.Id, StringComparer.Ordinal)) values.Add(new(sourceId, animation.Id, selection.Layer, clip.Id, selection.SelectionKey, marker.Id, marker.Kind, runtimeTick, marker.Tick, loop, marker.Payload)); }
        return values;
    }
    private static SampledPresentationPatch SampleTrack(CompiledTrack track, int tick, string layer)
    {
        var before = track.Keyframes.Where(x => x.Tick <= tick).OrderBy(x => x.Tick).Last();
        if (track.Interpolation == "linear" && track.ValueType == "scalar") { var next = track.Keyframes.Where(x => x.Tick > tick).OrderBy(x => x.Tick).FirstOrDefault(); if (next is not null) { var t = (tick - before.Tick) / (double)(next.Tick - before.Tick); return new(track.PartId, track.Property, before.Scalar!.Value + ((next.Scalar!.Value - before.Scalar.Value) * t), null, track.Id, before.Id, layer); } }
        return new(track.PartId, track.Property, before.Scalar, before.RegionId, track.Id, before.Id, layer);
    }
}

public sealed class AnimationSelections
{
    public AnimationSelection? Base { get; private set; }
    public AnimationSelection? Overlay { get; private set; }
    public void SelectBaseClip(string clipId, string key, string reason, int tick) => Base = Select("base", Base, clipId, key, reason, tick);
    public void RestartBaseClip(string clipId, string newKey, string reason, int tick) => SelectBaseClip(clipId, newKey, reason, tick);
    public void SelectOverlayClip(string clipId, string key, string reason, int tick) => Overlay = Select("overlay", Overlay, clipId, key, reason, tick);
    public void RestartOverlayClip(string clipId, string newKey, string reason, int tick) => SelectOverlayClip(clipId, newKey, reason, tick);
    public void ClearOverlayClip() => Overlay = null;
    private static AnimationSelection Select(string layer, AnimationSelection? previous, string clip, string key, string reason, int tick) => previous is not null && previous.SelectionKey == key ? previous : new(layer, clip, key, reason, tick);
}

public static class AnimationComposition
{
    public static IReadOnlyList<SampledPresentationPatch> Compose(SampledLayer? @base, SampledLayer? overlay) => new[] { @base, overlay }.Where(x => x is not null).SelectMany(x => x!.Patches).GroupBy(x => x.PartId + "|" + x.Property).Select(x => x.Last()).OrderBy(x => x.PartId).ThenBy(x => x.Property).ToArray();
}
