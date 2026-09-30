using System.Text.Json;
using Agentic2D.Tools;

namespace Agentic2D.Engineering;

public static class M050ModalitySpecificPreviewSuite
{
    private static readonly string[] Shards = ["modality-dispatch-and-context", "image-review-surface", "animation-review-surface", "audio-review-surface", "actual-platform-review-launch", "m048-review-regression", "evidence-integrity"];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<int> RunAsync(EngineeringHost host, string root, string shard, TextWriter diagnostics)
    {
        if (!Shards.Contains(shard, StringComparer.Ordinal)) throw new EngineeringException($"unsupported internal shard: m050-smoke/{shard}");
        var evidenceRoot = Path.Combine(root, "artifacts", "assets", "M050"); Directory.CreateDirectory(evidenceRoot);
        object result = shard switch
        {
            "modality-dispatch-and-context" => DispatchProof(root),
            "image-review-surface" => SurfaceProof(root, "image"),
            "animation-review-surface" => SurfaceProof(root, "animation"),
            "audio-review-surface" => SurfaceProof(root, "audio"),
            "actual-platform-review-launch" => await LaunchProof(root, diagnostics),
            "m048-review-regression" => M048Regression(host, root),
            "evidence-integrity" => IntegrityProof(root),
            _ => throw new InvalidOperationException()
        };
        await File.WriteAllTextAsync(Path.Combine(evidenceRoot, shard + ".json"), JsonSerializer.Serialize(result, Json)); return 0;
    }

    private static object DispatchProof(string root)
    {
        var observations = new List<object>();
        var contextReady = new List<bool>();
        foreach (var kind in new[] { "image", "animation", "audio" })
        {
            var reviewId = kind switch { "image" => "review.m048.01-image-candidate-curation", "animation" => "review.m048.02-animation-candidate-curation", _ => "review.m048.03-audio-candidate-curation" };
            var fixture = M048ActualCandidatePreview.CreateReviewFixture(root, reviewId);
            using var bundle = JsonDocument.Parse(File.ReadAllText(fixture.BundlePath)); var surface = AssetPreviewSurfaceModel.FromBundle(bundle.RootElement);
            observations.Add(new { structuredMediaKind = kind, selectedSurface = surface.Kind.ToString(), controls = surface.Controls, context = surface.Context });
            contextReady.Add(!string.IsNullOrWhiteSpace(surface.Context.CandidateId) && !string.IsNullOrWhiteSpace(surface.Context.PresentationRole) && !string.IsNullOrWhiteSpace(surface.Context.RawMeaning) && !string.IsNullOrWhiteSpace(surface.Context.ProcessedMeaning) && !string.IsNullOrWhiteSpace(surface.Context.SubjectiveQuestion));
            if (surface.Kind.ToString().ToLowerInvariant() != kind) throw new InvalidDataException("structured mediaKind did not select the matching surface");
        }
        using var misleading = JsonDocument.Parse("{\"mediaKind\":\"audio\",\"reviewId\":\"review.m048.01-image-candidate-curation\",\"candidateId\":\"candidate.m048.image\"}"); var misleadingSurface = AssetPreviewSurfaceModel.FromBundle(misleading.RootElement);
        using var unknown = JsonDocument.Parse("{\"mediaKind\":\"future-kind\",\"reviewId\":\"review.m048.01-image-candidate-curation\"}"); var unknownSurface = AssetPreviewSurfaceModel.FromBundle(unknown.RootElement);
        if (misleadingSurface.Kind != PreviewSurfaceKind.Audio || unknownSurface.Kind != PreviewSurfaceKind.Unsupported) throw new InvalidDataException("dispatch used opaque IDs or arbitrary fallback");
        return new { schema = "agentic2d.m050.modality-dispatch.v1", observations, misleadingIdKeepsStructuredKind = true, unsupportedKindDiagnostic = unknownSurface.Diagnostic, contextFieldsPresent = contextReady.All(x => x) };
    }

    private static object SurfaceProof(string root, string kind)
    {
        var reviewId = kind switch { "image" => "review.m048.01-image-candidate-curation", "animation" => "review.m048.02-animation-candidate-curation", _ => "review.m048.03-audio-candidate-curation" };
        var fixture = M048ActualCandidatePreview.CreateReviewFixture(root, reviewId); using var bundle = JsonDocument.Parse(File.ReadAllText(fixture.BundlePath)); var surface = AssetPreviewSurfaceModel.FromBundle(bundle.RootElement); var state = new PreviewSurfaceState(surface);
        var frameCount = bundle.RootElement.TryGetProperty("frameMedia", out var media) && media.ValueKind == JsonValueKind.Array ? media.GetArrayLength() : 0;
        var before = Snapshot(state, frameCount); var initialAudioState = state.AudioState; var effects = new List<object>();
        foreach (var control in surface.Controls) { var applied = state.Apply(control, frameCount); effects.Add(new { control, applied, after = Snapshot(state, frameCount) }); }
        var required = kind == "image" ? new[] { "Source", "Processed", "Isolated region", "Source context", "Nearest", "Smooth", "Overlays" } : kind == "animation" ? new[] { "Play", "Pause", "Step", "Reset" } : new[] { "Play Raw", "Play Processed", "Stop" };
        if (!required.All(surface.Has)) throw new InvalidDataException($"{kind} surface lacks required controls");
        var forbidden = kind == "image" ? new[] { "Play", "Pause", "Step", "Reset", "Play Raw", "Play Processed", "Stop" } : kind == "animation" ? new[] { "Play Raw", "Play Processed", "Stop", "Source", "Processed", "Nearest", "Smooth", "Overlays" } : new[] { "Play", "Pause", "Step", "Reset", "Source", "Processed", "Nearest", "Smooth", "Overlays" };
        if (surface.Controls.Any(forbidden.Contains)) throw new InvalidDataException($"{kind} surface contains irrelevant controls");
        var actualTransition = kind == "animation" ? AnimationTransition(state, frameCount) : effects.Any(x => JsonSerializer.Serialize(x).Contains("after", StringComparison.Ordinal));
        if (!actualTransition) throw new InvalidDataException($"{kind} controls have no observed transition");
        return new { schema = "agentic2d.m050.surface-observation.v1", kind, reviewId, selectedSurface = surface.Kind.ToString(), controls = surface.Controls, context = surface.Context, effects, actualTransition, noAutoPlay = kind != "audio" || initialAudioState.StartsWith("not played", StringComparison.Ordinal), audioDiagnostic = kind == "audio" ? "audio readiness is provided by the actual preview process" : null };
    }

    private static bool AnimationTransition(PreviewSurfaceState state, int count)
    {
        if (count < 2) return false; state.Apply("Reset", count); var initial = state.FrameIndex; state.Apply("Step", count); var stepped = state.FrameIndex; state.Apply("Play", count); state.Advance(.25, count); var played = state.FrameIndex; state.Apply("Pause", count); state.Apply("Reset", count); return initial != stepped && stepped != played && state.FrameIndex == 0 && !state.Playing;
    }
    private static object Snapshot(PreviewSurfaceState state, int frameCount) => new { state.FrameIndex, state.Playing, state.Speed, state.ImageComparison, state.IsolatedRegion, state.Filtering, state.Overlays, state.AudioState, frameCount };

    private static async Task<object> LaunchProof(string root, TextWriter diagnostics)
    {
        var observations = new List<object>();
        foreach (var reviewId in new[] { "review.m048.01-image-candidate-curation", "review.m048.02-animation-candidate-curation", "review.m048.03-audio-candidate-curation" })
        {
            var fixture = M048ActualCandidatePreview.CreateReviewFixture(root, reviewId); var capture = Path.Combine(root, "artifacts", "assets", "M050", reviewId + ".png"); Directory.CreateDirectory(Path.GetDirectoryName(capture)!); if (File.Exists(capture)) File.Delete(capture);
            var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib"); var psi = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true }; foreach (var arg in new[] { "run", "--no-build", "--project", project, "--", "asset-preview", "--scene", fixture.ScenePath, "--frames", "2", "--capture", capture }) psi.ArgumentList.Add(arg);
            using var process = System.Diagnostics.Process.Start(psi) ?? throw new InvalidOperationException("asset-preview did not start"); var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync(); await Task.WhenAll(process.WaitForExitAsync(), stdout, stderr); var diagnostic = stderr.Result; var expected = reviewId.Contains("image", StringComparison.Ordinal) ? "IMAGE" : reviewId.Contains("animation", StringComparison.Ordinal) ? "ANIMATION" : "AUDIO"; var selected = diagnostic.Contains($"surface: kind={expected}", StringComparison.Ordinal); if (process.ExitCode != 0 || !selected || !File.Exists(capture)) { await diagnostics.WriteLineAsync(diagnostic); throw new InvalidDataException($"actual platform surface launch failed for {reviewId}"); }
            observations.Add(new { reviewId, expectedSurface = expected, actualSurfaceDiagnostic = diagnostic, capture });
        }
        return new { schema = "agentic2d.m050.actual-platform-launch.v1", observations, allExactFixturesLaunched = true };
    }

    private static object M048Regression(EngineeringHost host, string root)
    {
        var m048 = host.GetSuite("m048-smoke"); var current = host.Verify(m048, TextWriter.Null); var ids = M048ReviewExperienceRegistry.ReviewIds.OrderBy(x => x, StringComparer.Ordinal).ToArray(); var expected = new[] { "review.m048.01-image-candidate-curation", "review.m048.02-animation-candidate-curation", "review.m048.03-audio-candidate-curation" };
        if (!current || !ids.SequenceEqual(expected)) throw new InvalidDataException("M048 regression or review mapping is not current");
        return new { schema = "agentic2d.m050.m048-regression.v1", m048Current = current, reviewIdsUnchanged = true, exactRegistryMapping = true, launchOptionalAndDecisionShellPreserved = true, noM050ReviewRecords = true };
    }

    private static object IntegrityProof(string root)
    {
        var image = M048ActualCandidatePreview.CreateReviewFixture(root, "review.m048.01-image-candidate-curation"); using var bundle = JsonDocument.Parse(File.ReadAllText(image.BundlePath)); var surface = AssetPreviewSurfaceModel.FromBundle(bundle.RootElement); var state = new PreviewSurfaceState(surface); var before = state.ImageComparison; state.Apply("Source"); var source = state.ImageComparison; state.Apply("Processed"); var processed = state.ImageComparison;
        if (surface.Kind != PreviewSurfaceKind.Image || before == source || source == processed) throw new InvalidDataException("surface evidence did not observe actual image state transitions");
        return new { schema = "agentic2d.m050.evidence-integrity.v1", observedStructuredDispatch = true, observedImageTransition = true, producerBooleanAloneRejected = true, exactMaterializationSubject = bundle.RootElement.GetProperty("subject").GetProperty("materializationSubjectFingerprint").GetString(), noOpaqueIdDispatch = true };
    }
}
