using System.Collections.ObjectModel;

namespace Agentic2D.Tools;

/// <summary>Bounded, explicit mapping from M048 review IDs to real candidate-preview fixtures.</summary>
public static class M048ReviewExperienceRegistry
{
    private static readonly IReadOnlyDictionary<string, string> Registered = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["review.m048.01-image-candidate-curation"] = "image",
        ["review.m048.02-animation-candidate-curation"] = "animation",
        ["review.m048.03-audio-candidate-curation"] = "audio"
    });

    public static IReadOnlyCollection<string> ReviewIds => Registered.Keys.ToArray();

    public static bool TryResolve(string reviewId, string root, out M048ActualCandidatePreview.ReviewFixture? fixture, out string error)
    {
        fixture = null;
        if (!Registered.ContainsKey(reviewId)) { error = "review ID is not registered to an M048 candidate-preview experience"; return false; }
        try { fixture = M048ActualCandidatePreview.CreateReviewFixture(root, reviewId); error = string.Empty; return true; }
        catch (Exception exception) { error = "M048 candidate-preview fixture is unavailable: " + exception.Message; return false; }
    }

    public static bool IsPlaceholderOnly(string reviewId) => !Registered.ContainsKey(reviewId);
}
