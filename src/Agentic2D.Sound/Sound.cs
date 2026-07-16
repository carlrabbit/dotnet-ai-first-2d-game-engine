using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Agentic2D.Validation;

namespace Agentic2D.Sound;

public sealed record SoundDefinitionSource
{
    [JsonPropertyName("schema")] public string Schema { get; init; } = "";
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("cueId")] public string CueId { get; init; } = "";
    [JsonPropertyName("groupId")] public string GroupId { get; init; } = "";
    [JsonPropertyName("variants")] public IReadOnlyList<SoundVariantSource> Variants { get; init; } = [];
    [JsonPropertyName("defaults")] public SoundValues Defaults { get; init; } = new();
    [JsonPropertyName("tags")] public IReadOnlyList<string> Tags { get; init; } = [];
    [JsonPropertyName("provenance")] public Dictionary<string, JsonElement> Provenance { get; init; } = [];
}

public sealed record SoundVariantSource
{
    [JsonPropertyName("id")] public string Id { get; init; } = "";
    [JsonPropertyName("asset")] public string Asset { get; init; } = "";
    [JsonPropertyName("values")] public SoundValues Values { get; init; } = new();
}

public sealed record SoundValues
{
    [JsonPropertyName("volume")] public double Volume { get; init; } = 1;
    [JsonPropertyName("pitch")] public double Pitch { get; init; } = 1;
    [JsonPropertyName("pan")] public double Pan { get; init; }
    public bool IsValid => double.IsFinite(Volume) && double.IsFinite(Pitch) && double.IsFinite(Pan) && Volume is >= 0 and <= 1 && Pitch is >= .25 and <= 4 && Pan is >= -1 and <= 1;
}

public sealed record SoundDiagnostic(string Id, string Severity, string Message, string Target);
public sealed record SoundCatalog(IReadOnlyList<SoundDefinitionSource> Definitions, IReadOnlyList<SoundDiagnostic> Diagnostics)
{
    public bool Passed => Diagnostics.All(x => x.Severity != "error");
}

public sealed record CueRequest(string CueId, string SourceKind, string SourceId, int RuntimeTick, int OccurrenceOrdinal, string Seed, string? ExplicitVariantId = null, string? OriginEventId = null);
public sealed record SoundCueSelection(string CueId, string DefinitionId, string VariantId, string SourceKind, string SourceId, int RuntimeTick, int OccurrenceOrdinal, string Mapping, SoundValues Values, string Fingerprint);
public sealed record SoundCommand(string CommandId, string Kind, int RuntimeTick, string? CueId = null, string? VariantId = null, string? LoopInstanceKey = null, string? GroupId = null, double? Volume = null, string? Result = null);
public sealed record SoundCommandFrame(int RuntimeTick, IReadOnlyList<SoundCueSelection> Selections, IReadOnlyList<SoundCommand> Commands, IReadOnlyDictionary<string, string> LoopState, IReadOnlyList<SoundDiagnostic> Diagnostics, string Fingerprint);

public static class SoundContent
{
    public static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true, WriteIndented = true };
    public static SoundCatalog LoadAll(string? root = null)
    {
        root ??= Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game", "sounds");
        var definitions = new List<SoundDefinitionSource>();
        var diagnostics = new List<SoundDiagnostic>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        var paths = Directory.Exists(root) ? Directory.EnumerateFiles(root, "*.json").Order(StringComparer.Ordinal) : Enumerable.Empty<string>();
        foreach (var path in paths)
        {
            try
            {
                var definition = JsonSerializer.Deserialize<SoundDefinitionSource>(File.ReadAllText(path), Json) ?? new();
                Validate(definition, path, ids, diagnostics);
                definitions.Add(definition);
            }
            catch (JsonException exception)
            {
                diagnostics.Add(new("SOUND0001", "error", "Malformed sound JSON: " + exception.Message, path));
            }
        }
        foreach (var required in new[] { "sound-definition.player-footstep", "sound-definition.entity-damage", "sound-definition.entity-defeat", "sound-definition.item-collection", "sound-definition.ambient-loop-smoke" })
        {
            if (!definitions.Any(x => x.Id == required)) diagnostics.Add(new("SOUND0002", "error", "Required sound definition is missing: " + required, root));
        }
        return new(definitions.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray(), diagnostics.OrderBy(x => x.Id).ThenBy(x => x.Target, StringComparer.Ordinal).ToArray());
    }

    private static void Validate(SoundDefinitionSource x, string path, HashSet<string> ids, List<SoundDiagnostic> ds)
    {
        if (x.Schema != "agentic2d.sound-definition.v1" || !Stable(x.Id) || !x.Id.StartsWith("sound-definition.", StringComparison.Ordinal) || !ids.Add(x.Id)) ds.Add(new("SOUND0003", "error", "Sound schema or stable definition ID is invalid or duplicated.", path));
        if (!Stable(x.CueId) || !x.CueId.StartsWith("cue.", StringComparison.Ordinal) || x.GroupId is not ("sound-group.effects" or "sound-group.ambience")) ds.Add(new("SOUND0004", "error", "Cue ID or sound group is invalid.", path));
        if (!x.Defaults.IsValid || x.Variants.Count == 0 || x.Variants.Select(v => v.Id).Distinct(StringComparer.Ordinal).Count() != x.Variants.Count) ds.Add(new("SOUND0005", "error", "Defaults or variants are invalid.", path));
        foreach (var variant in x.Variants)
        {
            if (!Stable(variant.Id) || string.IsNullOrWhiteSpace(variant.Asset) || !variant.Values.IsValid || !File.Exists(Path.Combine(ContentTargetResolver.FindRepositoryRoot(), variant.Asset))) ds.Add(new("SOUND0006", "error", "Variant ID, asset reference, or values are invalid.", path));
        }
    }

    private static bool Stable(string value) => !string.IsNullOrWhiteSpace(value) && value.All(c => char.IsLetterOrDigit(c) || c is '.' or '-');
}

/// <summary>Immutable presentation projection. It has no gameplay or native-audio dependency.</summary>
public sealed class SoundProjector
{
    private readonly IReadOnlyDictionary<string, SoundDefinitionSource> byCue;
    private readonly Dictionary<string, string> loops = new(StringComparer.Ordinal);

    public SoundProjector(IEnumerable<SoundDefinitionSource> definitions) => byCue = definitions.ToDictionary(x => x.CueId, StringComparer.Ordinal);

    public SoundCueSelection Select(CueRequest request, string mapping)
    {
        if (!byCue.TryGetValue(request.CueId, out var definition)) throw new InvalidOperationException("Unknown sound cue: " + request.CueId);
        var variants = definition.Variants.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
        var variant = request.ExplicitVariantId is null ? variants[StableIndex(request, variants.Length)] : variants.SingleOrDefault(x => x.Id == request.ExplicitVariantId) ?? throw new InvalidOperationException("Explicit variant is not available.");
        var values = new SoundValues { Volume = definition.Defaults.Volume * variant.Values.Volume, Pitch = definition.Defaults.Pitch * variant.Values.Pitch, Pan = Math.Clamp(definition.Defaults.Pan + variant.Values.Pan, -1, 1) };
        return new(request.CueId, definition.Id, variant.Id, request.SourceKind, request.SourceId, request.RuntimeTick, request.OccurrenceOrdinal, mapping, values, Fingerprint(new { request, definition = definition.Id, variant = variant.Id, values }));
    }

    public SoundCommandFrame Project(int tick, IEnumerable<(CueRequest Request, string Mapping)> requests, IEnumerable<SoundCommand>? explicitCommands = null)
    {
        var selections = requests.Select(x => Select(x.Request, x.Mapping)).OrderBy(x => SourcePriority(x.SourceKind)).ThenBy(x => x.SourceId, StringComparer.Ordinal).ThenBy(x => x.OccurrenceOrdinal).ThenBy(x => x.CueId, StringComparer.Ordinal).ToArray();
        var commands = selections.Select((x, i) => new SoundCommand("sound-command." + tick + "." + i.ToString("D3"), "PlayCue", tick, x.CueId, x.VariantId, Result: "accepted"))
            .Concat(explicitCommands ?? [])
            .OrderBy(x => SourcePriority(x.Kind)).ThenBy(x => x.LoopInstanceKey, StringComparer.Ordinal).ThenBy(x => x.CommandId, StringComparer.Ordinal)
            .Select(Apply).ToArray();
        var state = loops.OrderBy(x => x.Key, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        return new(tick, selections, commands, state, [], Fingerprint(new { tick, selections, commands, state }));
    }

    private SoundCommand Apply(SoundCommand command)
    {
        if (command.Kind == "StartLoop")
        {
            if (command.LoopInstanceKey is null || loops.ContainsKey(command.LoopInstanceKey)) return command with { Result = "rejected-active-key" };
            loops.Add(command.LoopInstanceKey, command.CueId ?? "");
            return command with { Result = "accepted" };
        }
        if (command.Kind == "ReplaceLoop")
        {
            if (command.LoopInstanceKey is null || !loops.ContainsKey(command.LoopInstanceKey)) return command with { Result = "rejected-missing-key" };
            loops[command.LoopInstanceKey] = command.CueId ?? "";
            return command with { Result = "accepted-restarted" };
        }
        if (command.Kind == "StopLoop")
        {
            if (command.LoopInstanceKey is null || !loops.Remove(command.LoopInstanceKey)) return command with { Result = "accepted-no-op-missing-key" };
            return command with { Result = "accepted-stopped" };
        }
        return command.Kind is "PlayCue" or "SetGroupVolume" ? command with { Result = "accepted" } : command with { Result = "rejected-unknown-command" };
    }

    private static int StableIndex(CueRequest request, int count)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", request.CueId, request.SourceId, request.RuntimeTick, request.OccurrenceOrdinal, request.Seed)));
        return (int)(BitConverter.ToUInt32(hash, 0) % count);
    }

    public static int SourcePriority(string source) => source switch { "marker" or "presentation.footstep" => 0, "event" or "entity.damaged" => 1, "entity.defeated" => 2, "item.collected" => 3, _ => 9 };
    public static string Fingerprint(object value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value, SoundContent.Json)))).ToLowerInvariant();
}
