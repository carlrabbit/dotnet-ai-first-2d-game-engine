using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.Simulation;

namespace Agentic2D.Persistence;

public sealed record SemanticContentEntry(string Kind, string StableId, int SchemaVersion, string SemanticFingerprint);
public sealed record CanonicalSaveIdentity(string SaveId, string ProjectId, string WorldId, string WorldConfigurationId, string WorldConfigurationFingerprint, string SemanticContentFingerprint);
public sealed record CanonicalGameSaveEnvelope(
    string Schema, int Version, string SaveId, string ProjectId, string WorldId, string WorldConfigurationId,
    string WorldConfigurationFingerprint, string SemanticContentFingerprint, string WorldPayloadSchema,
    int WorldPayloadVersion, string ComponentRegistrationFingerprint, string PayloadFingerprint,
    string PayloadChecksum, string CanonicalSaveFingerprint, JsonElement WorldPayload);
public sealed record CanonicalSaveLoadResult(bool Success, SimulationWorld? World, CanonicalGameSaveEnvelope? Envelope, IReadOnlyList<string> Diagnostics)
{
    public static CanonicalSaveLoadResult Failure(params string[] diagnostics) => new(false, null, null, diagnostics);
}
public sealed record CanonicalSaveValidation(bool Success, CanonicalGameSaveEnvelope? Envelope, IReadOnlyList<string> Diagnostics);

/// <summary>One current durable boundary around the actual SimulationWorld v2 payload.</summary>
public sealed class CanonicalRuntimePersistenceService
{
    public const string EnvelopeSchema = "agentic2d.game-save.v1";
    public const int EnvelopeVersion = 1;
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } };

    public CanonicalGameSaveEnvelope Capture(SimulationWorld world, string saveId, string projectId, string worldConfigurationId, string worldConfigurationFingerprint, IEnumerable<SemanticContentEntry> semanticContent)
    {
        if (world is null || string.IsNullOrWhiteSpace(saveId) || string.IsNullOrWhiteSpace(projectId)) throw new ArgumentException("SAVE0430: save identity is incomplete");
        var payload = JsonDocument.Parse(world.CanonicalJson()).RootElement.Clone();
        var payloadText = Canonical(payload);
        var content = SemanticFingerprint(semanticContent);
        var payloadFingerprint = Fingerprint(payloadText);
        var checksum = Checksum(payloadText);
        var provisional = new CanonicalGameSaveEnvelope(EnvelopeSchema, EnvelopeVersion, saveId, projectId, world.Id.Value, worldConfigurationId, worldConfigurationFingerprint, content, SimulationWorld.SaveSchema, 2, world.RegistrationFingerprint, payloadFingerprint, checksum, "", payload);
        return provisional with { CanonicalSaveFingerprint = Fingerprint(new { provisional.Schema, provisional.Version, provisional.SaveId, provisional.ProjectId, provisional.WorldId, provisional.WorldConfigurationId, provisional.WorldConfigurationFingerprint, provisional.SemanticContentFingerprint, provisional.WorldPayloadSchema, provisional.WorldPayloadVersion, provisional.ComponentRegistrationFingerprint, provisional.PayloadFingerprint, provisional.PayloadChecksum, worldPayload = payloadText }) };
    }

    public CanonicalSaveValidation ValidateEnvelope(CanonicalGameSaveEnvelope envelope, string projectId, string worldConfigurationId, string worldConfigurationFingerprint, IEnumerable<SemanticContentEntry> semanticContent)
    {
        var errors = new List<string>();
        if (envelope.Schema != EnvelopeSchema || envelope.Version != EnvelopeVersion) errors.Add("SAVE0431: unsupported canonical game-save schema/version");
        if (envelope.WorldPayloadSchema == SimulationWorld.UnsupportedV1Schema || envelope.WorldPayloadVersion == 1) errors.Add("SAVE0432: unsupported SimulationWorld v1 payload");
        if (envelope.WorldPayloadSchema != SimulationWorld.SaveSchema || envelope.WorldPayloadVersion != 2) errors.Add("SAVE0433: embedded SimulationWorld payload is not v2");
        if (envelope.ProjectId != projectId || envelope.WorldConfigurationId != worldConfigurationId || envelope.WorldConfigurationFingerprint != worldConfigurationFingerprint) errors.Add("SAVE0434: project/world configuration compatibility mismatch");
        var payloadText = Canonical(envelope.WorldPayload);
        if (envelope.PayloadChecksum != Checksum(payloadText)) errors.Add("SAVE0435: canonical payload checksum mismatch");
        if (envelope.PayloadFingerprint != Fingerprint(payloadText)) errors.Add("SAVE0436: canonical payload fingerprint mismatch");
        var content = SemanticFingerprint(semanticContent);
        if (envelope.SemanticContentFingerprint != content) errors.Add("SAVE0437: semantic content fingerprint mismatch");
        var expected = envelope with { CanonicalSaveFingerprint = "" };
        var canonical = Fingerprint(new { expected.Schema, expected.Version, expected.SaveId, expected.ProjectId, expected.WorldId, expected.WorldConfigurationId, expected.WorldConfigurationFingerprint, expected.SemanticContentFingerprint, expected.WorldPayloadSchema, expected.WorldPayloadVersion, expected.ComponentRegistrationFingerprint, expected.PayloadFingerprint, expected.PayloadChecksum, worldPayload = payloadText });
        if (envelope.CanonicalSaveFingerprint != canonical) errors.Add("SAVE0438: canonical save fingerprint mismatch");
        return new(errors.Count == 0, errors.Count == 0 ? envelope : null, errors);
    }

    public CanonicalSaveValidation ValidateFile(string path, string projectId, string worldConfigurationId, string worldConfigurationFingerprint, IEnumerable<SemanticContentEntry> semanticContent)
    {
        try { return ValidateEnvelope(JsonSerializer.Deserialize<CanonicalGameSaveEnvelope>(File.ReadAllText(path), Json) ?? throw new InvalidOperationException("SAVE0439: malformed canonical save"), projectId, worldConfigurationId, worldConfigurationFingerprint, semanticContent); }
        catch (JsonException) { return new(false, null, ["SAVE0440: malformed or truncated canonical save"]); }
        catch (IOException) { return new(false, null, ["SAVE0441: canonical save cannot be read"]); }
    }

    public CanonicalSaveLoadResult LoadFresh(string path, IEnumerable<SimulationComponentRegistration> registrations, string projectId, string worldConfigurationId, string worldConfigurationFingerprint, IEnumerable<SemanticContentEntry> semanticContent)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<CanonicalGameSaveEnvelope>(File.ReadAllText(path), Json) ?? throw new InvalidOperationException("SAVE0440: malformed or truncated canonical save");
            var validation = ValidateEnvelope(envelope, projectId, worldConfigurationId, worldConfigurationFingerprint, semanticContent);
            if (!validation.Success) return CanonicalSaveLoadResult.Failure(validation.Diagnostics.ToArray());
            var save = envelope.WorldPayload.Deserialize<SimulationSave>(Json) ?? throw new InvalidOperationException("SAVE0442: world payload is malformed");
            var loaded = SimulationWorld.Load(save, registrations);
            return loaded.Success ? new(true, loaded.World, envelope, []) : CanonicalSaveLoadResult.Failure(loaded.Diagnostics.Select(x => x.Code + ": " + x.Message).ToArray());
        }
        catch (JsonException) { return CanonicalSaveLoadResult.Failure("SAVE0440: malformed or truncated canonical save"); }
        catch (IOException) { return CanonicalSaveLoadResult.Failure("SAVE0441: canonical save cannot be read"); }
        catch (InvalidOperationException exception) { return CanonicalSaveLoadResult.Failure(exception.Message); }
    }

    public CanonicalSaveLoadResult LoadFreshFromEnvelope(CanonicalGameSaveEnvelope envelope, IEnumerable<SimulationComponentRegistration> registrations, string projectId, string worldConfigurationId, string worldConfigurationFingerprint, IEnumerable<SemanticContentEntry> semanticContent)
    {
        var validation = ValidateEnvelope(envelope, projectId, worldConfigurationId, worldConfigurationFingerprint, semanticContent);
        if (!validation.Success) return CanonicalSaveLoadResult.Failure(validation.Diagnostics.ToArray());
        try
        {
            var save = envelope.WorldPayload.Deserialize<SimulationSave>(Json) ?? throw new InvalidOperationException("SAVE0442: world payload is malformed");
            var loaded = SimulationWorld.Load(save, registrations);
            return loaded.Success ? new(true, loaded.World, envelope, []) : CanonicalSaveLoadResult.Failure(loaded.Diagnostics.Select(x => x.Code + ": " + x.Message).ToArray());
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException) { return CanonicalSaveLoadResult.Failure("SAVE0442: world payload could not be reconstructed: " + exception.Message); }
    }

    public void WriteAtomic(string path, CanonicalGameSaveEnvelope envelope, string projectId, string worldConfigurationId, string worldConfigurationFingerprint, IEnumerable<SemanticContentEntry> semanticContent)
    {
        var validation = ValidateEnvelope(envelope, projectId, worldConfigurationId, worldConfigurationFingerprint, semanticContent);
        if (!validation.Success) throw new InvalidOperationException(string.Join("; ", validation.Diagnostics));
        var full = Path.GetFullPath(path); Directory.CreateDirectory(Path.GetDirectoryName(full)!); var temp = full + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(envelope, Json));
        var written = ValidateFile(temp, projectId, worldConfigurationId, worldConfigurationFingerprint, semanticContent);
        if (!written.Success) { File.Delete(temp); throw new InvalidOperationException("SAVE0443: temporary canonical save failed validation"); }
        File.Move(temp, full, true);
    }

    public CanonicalSaveLoadResult Recover(string path, string previousGoodPath, IEnumerable<SimulationComponentRegistration> registrations, string projectId, string worldConfigurationId, string worldConfigurationFingerprint, IEnumerable<SemanticContentEntry> semanticContent)
    {
        var previous = LoadFresh(previousGoodPath, registrations, projectId, worldConfigurationId, worldConfigurationFingerprint, semanticContent);
        if (!previous.Success || previous.Envelope is null) return CanonicalSaveLoadResult.Failure("SAVE0444: previous-good canonical save failed validation");
        try { WriteAtomic(path, previous.Envelope, projectId, worldConfigurationId, worldConfigurationFingerprint, semanticContent); return LoadFresh(path, registrations, projectId, worldConfigurationId, worldConfigurationFingerprint, semanticContent); }
        catch (Exception exception) when (exception is IOException or InvalidOperationException) { return CanonicalSaveLoadResult.Failure("SAVE0445: canonical recovery failed: " + exception.Message); }
    }

    public static IReadOnlyList<SemanticContentEntry> ResolveSemanticContent(SimulationWorld world, string projectId, string worldConfigurationId, string worldConfigurationFingerprint)
    {
        var entries = new List<SemanticContentEntry> { new("project", projectId, 1, Fingerprint(projectId)), new("world-configuration", worldConfigurationId, 1, worldConfigurationFingerprint), new("component-registration", "registration", 1, world.RegistrationFingerprint) };
        entries.AddRange(world.Regions.Select(x => new SemanticContentEntry("region-definition", x.Id, 1, Fingerprint(new { x.Id, x.Name, x.Active }))));
        return entries;
    }

    private static string Canonical(JsonElement value) => JsonSerializer.Serialize(value, Json).Replace("\r\n", "\n", StringComparison.Ordinal);
    private static string SemanticFingerprint(IEnumerable<SemanticContentEntry> entries) => Fingerprint(entries.Where(x => !x.Kind.Equals("presentation", StringComparison.OrdinalIgnoreCase)).OrderBy(x => x.Kind, StringComparer.Ordinal).ThenBy(x => x.StableId, StringComparer.Ordinal).ThenBy(x => x.SchemaVersion).ToArray());
    private static string Fingerprint(object value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value is string text ? text : JsonSerializer.Serialize(value, Json)))).ToLowerInvariant();
    private static string Checksum(string value) => "sha256:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}
