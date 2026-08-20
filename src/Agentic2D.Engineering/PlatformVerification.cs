using System.Text.Json;

namespace Agentic2D.Engineering;

public sealed record PlatformVerificationState(
    string Schema,
    IReadOnlyList<string> SupportedDevelopmentPlatforms,
    PlatformEpoch ActiveEpoch,
    IReadOnlyList<DeferredPlatformVerification> DeferredVerification)
{
    public string ActivePlatform => ActiveEpoch.Platform;

    public static PlatformVerificationState Load(string root)
    {
        var path = Path.Combine(root, "eng", "platform-verification.json");
        if (!File.Exists(path)) throw new EngineeringException("eng/platform-verification.json is missing");
        try
        {
            var state = JsonSerializer.Deserialize<PlatformVerificationState>(File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new EngineeringException("platform verification state is empty");
            Validate(state);
            return state;
        }
        catch (JsonException exception)
        {
            throw new EngineeringException($"platform verification state is malformed: {exception.Message}");
        }
    }

    public static void Validate(PlatformVerificationState state)
    {
        if (state.Schema != "agentic2d.engineering.platform-verification.v1") throw new EngineeringException("unsupported platform verification schema");
        var supported = state.SupportedDevelopmentPlatforms.Where(IsSupportedPlatform).Distinct(StringComparer.Ordinal).ToArray();
        if (supported.Length != state.SupportedDevelopmentPlatforms.Count || supported.Length == 0) throw new EngineeringException("supported development platforms must contain valid unique platform IDs");
        if (!IsSupportedPlatform(state.ActiveEpoch.Platform) || !supported.Contains(state.ActiveEpoch.Platform, StringComparer.Ordinal)) throw new EngineeringException("active epoch platform must be a supported development target");
        foreach (var item in state.DeferredVerification)
        {
            if (!IsSupportedPlatform(item.Platform) || !supported.Contains(item.Platform, StringComparer.Ordinal)) throw new EngineeringException("deferred verification must target a supported platform");
            if (item.Platform == state.ActiveEpoch.Platform) throw new EngineeringException("deferred verification must target an inactive platform");
            if (string.IsNullOrWhiteSpace(item.SourceMilestone) || item.Checks.Count == 0) throw new EngineeringException("deferred verification requires a source milestone and checks");
        }
    }

    public bool IsActive(string platform) => string.Equals(platform, ActivePlatform, StringComparison.Ordinal);
    public bool IsSupported(string platform) => SupportedDevelopmentPlatforms.Contains(platform, StringComparer.Ordinal);
    private static bool IsSupportedPlatform(string platform) => platform is "linux" or "windows";
}

public sealed record PlatformEpoch(string Id, string Platform, string StartedAtMilestone, string Reason);
public sealed record DeferredPlatformVerification(string Platform, string SourceMilestone, IReadOnlyList<string> Checks, string? Review);

public static class PlatformVerificationPolicy
{
    public static string EvidenceStatus(PlatformVerificationState state, string platform, bool executed) =>
        !state.IsSupported(platform) ? "invalid-platform" : !state.IsActive(platform) ? "deferred-inactive-platform" : executed ? "passed" : "pending-active-platform-proof";

    public static string CompletionOutcome(string reviewStatus, bool activePlatformObligationsComplete) =>
        !activePlatformObligationsComplete ? "BLOCKED" : reviewStatus == "approved" ? "COMPLETE" : reviewStatus == "pending" ? "AWAITING HUMAN REVIEW" : "BLOCKED";
}
