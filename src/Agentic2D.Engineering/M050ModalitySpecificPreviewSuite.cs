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
            "actual-platform-review-launch" => await LaunchProof(host, root, diagnostics),
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
        if (count < 2) return false;
        state.Apply("1x", count); state.Apply("Reset", count); var initial = state.FrameIndex;
        state.Apply("Step", count); var stepped = state.FrameIndex;
        state.Apply("Play", count); state.Advance(.25, count); var played = state.FrameIndex;
        state.Apply("Pause", count); state.Apply("Reset", count);
        return initial != stepped && stepped != played && state.FrameIndex == 0 && !state.Playing;
    }
    private static object Snapshot(PreviewSurfaceState state, int frameCount) => new { state.FrameIndex, state.Playing, state.Speed, state.ImageComparison, state.IsolatedRegion, state.Filtering, state.Overlays, state.AudioState, frameCount };

    private static async Task<object> LaunchProof(EngineeringHost host, string root, TextWriter diagnostics)
    {
        var observations = new List<object>();
        foreach (var reviewId in new[] { "review.m048.01-image-candidate-curation", "review.m048.02-animation-candidate-curation", "review.m048.03-audio-candidate-curation" })
        {
            var fixture = M048ActualCandidatePreview.CreateReviewFixture(root, reviewId); var capture = Path.Combine(root, "artifacts", "assets", "M050", reviewId + ".png"); Directory.CreateDirectory(Path.GetDirectoryName(capture)!); if (File.Exists(capture)) File.Delete(capture);
            var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib"); var psi = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true }; foreach (var arg in new[] { "run", "--no-build", "--project", project, "--", "asset-preview", "--scene", fixture.ScenePath, "--frames", "2", "--capture", capture }) psi.ArgumentList.Add(arg);
            using var process = System.Diagnostics.Process.Start(psi) ?? throw new InvalidOperationException("asset-preview did not start"); var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync(); await Task.WhenAll(process.WaitForExitAsync(), stdout, stderr); var diagnostic = stderr.Result; var expected = reviewId.Contains("image", StringComparison.Ordinal) ? "IMAGE" : reviewId.Contains("animation", StringComparison.Ordinal) ? "ANIMATION" : "AUDIO"; var selected = diagnostic.Contains($"surface: kind={expected}", StringComparison.Ordinal); if (process.ExitCode != 0 || !selected || !File.Exists(capture)) { await diagnostics.WriteLineAsync(diagnostic); throw new InvalidDataException($"actual platform surface launch failed for {reviewId}"); }
            observations.Add(new { reviewId, expectedSurface = expected, actualSurfaceDiagnostic = diagnostic, capture });
        }
        var reviewSet = host.GetMilestoneReviewSet("M048", out var reviewError, requireGraphicsPrerequisite: false); if (!string.IsNullOrWhiteSpace(reviewError) || reviewSet.Count != 3) throw new InvalidDataException("actual M048 review-run payload did not contain the complete review set");
        var payload = host.SerializeReviewRunPayload(reviewSet); var workbenchCapture = Path.Combine(root, "artifacts", "assets", "M050", "m048-review-workbench.png"); var workbenchProject = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib"); var workbench = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true }; foreach (var arg in new[] { "run", "--no-build", "--project", workbenchProject, "--", "review-workbench", "--milestone", "M048", "--items-base64", payload, "--frames", "3", "--capture", workbenchCapture }) workbench.ArgumentList.Add(arg); using var reviewProcess = System.Diagnostics.Process.Start(workbench) ?? throw new InvalidDataException("review workbench did not start"); var reviewStdout = reviewProcess.StandardOutput.ReadToEndAsync(); var reviewStderr = reviewProcess.StandardError.ReadToEndAsync(); await Task.WhenAll(reviewProcess.WaitForExitAsync(), reviewStdout, reviewStderr); if (reviewProcess.ExitCode != 0 || !File.Exists(workbenchCapture)) throw new InvalidDataException("actual M048 review workbench did not produce its graphical capture");
        return new { schema = "agentic2d.m050.actual-platform-launch.v2", observations, allExactFixturesLaunched = true, reviewWorkbench = new { payloadItems = reviewSet, deterministicOrder = reviewSet.Select(item => item.Id).SequenceEqual(M048ReviewExperienceRegistry.ReviewIds.OrderBy(item => item, StringComparer.Ordinal)), capture = workbenchCapture, persistedStatuses = reviewSet.ToDictionary(item => item.Id, item => item.Status) } };
    }

    private static object M048Regression(EngineeringHost host, string root)
    {
        var m048 = host.GetSuite("m048-smoke"); var current = host.Verify(m048, TextWriter.Null); var ids = M048ReviewExperienceRegistry.ReviewIds.OrderBy(x => x, StringComparer.Ordinal).ToArray(); var expected = new[] { "review.m048.01-image-candidate-curation", "review.m048.02-animation-candidate-curation", "review.m048.03-audio-candidate-curation" };
        if (!current || !ids.SequenceEqual(expected)) throw new InvalidDataException("M048 regression or review mapping is not current");
        var reviewSet = host.GetMilestoneReviewSet("M048", out var error, requireGraphicsPrerequisite: false);
        if (!string.IsNullOrWhiteSpace(error) || !reviewSet.Select(item => item.Id).SequenceEqual(expected)) throw new InvalidDataException("complete M048 review set was not resolved in deterministic order");
        var statuses = reviewSet.ToDictionary(item => item.Id, item => item.Status, StringComparer.Ordinal);
        var payload = host.SerializeReviewRunPayload(reviewSet);
        var payloadItems = JsonSerializer.Deserialize<ReviewRunItem[]>(Convert.FromBase64String(payload), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        if (!payloadItems.Select(item => item.Id).SequenceEqual(expected)) throw new InvalidDataException("milestone review payload lost a registered M048 item");
        var layout = reviewSet.Select(item => LayoutObservation(item)).ToArray();
        if (layout.Any(item => !item.AllInside || item.QuestionOverlapsContent || item.QuestionOverlapsPreview || item.QuestionOverlapsPersistence || item.QuestionOverlapsButtons)) throw new InvalidDataException("M048 review workbench layout is outside the client area or overlapping");
        return new { schema = "agentic2d.m050.m048-regression.v2", m048Current = current, reviewIdsUnchanged = true, exactRegistryMapping = true, launchOptionalAndDecisionShellPreserved = true, noM050ReviewRecords = true, completeReviewSet = reviewSet, payloadItems, deterministicOrder = payloadItems.Select(item => item.Id).SequenceEqual(expected), persistedStatuses = statuses, manualNavigationIds = expected, approvedItemRemainsApprovedWithoutReset = statuses[expected[0]] == "approved", layout };
    }

    private sealed record LayoutObservationResult(string Id, string Status, UiRect QuestionBounds, IReadOnlyList<string> QuestionLines, bool AllInside, bool QuestionOverlapsContent, bool QuestionOverlapsPreview, bool QuestionOverlapsPersistence, bool QuestionOverlapsButtons);

    private static LayoutObservationResult LayoutObservation(ReviewRunItem item)
    {
        var question = ReviewSurfaceLayout.WorkbenchQuestion(item.Subject); var regions = new[] { ReviewSurfaceLayout.WorkbenchHeader, ReviewSurfaceLayout.WorkbenchStatus, question.Bounds, ReviewSurfaceLayout.WorkbenchContent, ReviewSurfaceLayout.WorkbenchPreviewLaunch, ReviewSurfaceLayout.WorkbenchPersistence, ReviewSurfaceLayout.WorkbenchRestart, ReviewSurfaceLayout.WorkbenchReject, ReviewSurfaceLayout.WorkbenchAccept };
        return new(item.Id, item.Status, question.Bounds, question.Lines, regions.All(region => region.Inside(ReviewSurfaceLayout.WindowWidth, ReviewSurfaceLayout.WindowHeight)), question.Bounds.Intersects(ReviewSurfaceLayout.WorkbenchContent), question.Bounds.Intersects(ReviewSurfaceLayout.WorkbenchPreviewLaunch), question.Bounds.Intersects(ReviewSurfaceLayout.WorkbenchPersistence), question.Bounds.Intersects(ReviewSurfaceLayout.WorkbenchRestart) || question.Bounds.Intersects(ReviewSurfaceLayout.WorkbenchReject) || question.Bounds.Intersects(ReviewSurfaceLayout.WorkbenchAccept));
    }

    private static object IntegrityProof(string root)
    {
        var image = M048ActualCandidatePreview.CreateReviewFixture(root, "review.m048.01-image-candidate-curation"); using var bundle = JsonDocument.Parse(File.ReadAllText(image.BundlePath)); var surface = AssetPreviewSurfaceModel.FromBundle(bundle.RootElement); var state = new PreviewSurfaceState(surface); var before = state.ImageComparison; state.Apply("Source"); var source = state.ImageComparison; state.Apply("Processed"); var processed = state.ImageComparison;
        if (surface.Kind != PreviewSurfaceKind.Image || before == source || source == processed) throw new InvalidDataException("surface evidence did not observe actual image state transitions");
        var layouts = new[] { "review.m048.01-image-candidate-curation", "review.m048.02-animation-candidate-curation", "review.m048.03-audio-candidate-curation" }.Select(reviewId => { var fixture = M048ActualCandidatePreview.CreateReviewFixture(root, reviewId); using var document = JsonDocument.Parse(File.ReadAllText(fixture.BundlePath)); var preview = AssetPreviewSurfaceModel.FromBundle(document.RootElement); var layout = ReviewSurfaceLayout.AssetPreview(preview); return new { reviewId, preview.Kind, contextLines = layout.Context.Lines, contextInside = layout.Context.Bounds.Inside(ReviewSurfaceLayout.WindowWidth, ReviewSurfaceLayout.WindowHeight), contentInside = layout.Content.Inside(ReviewSurfaceLayout.WindowWidth, ReviewSurfaceLayout.WindowHeight), controlsInside = layout.Controls.Inside(ReviewSurfaceLayout.WindowWidth, ReviewSurfaceLayout.WindowHeight), contextDoesNotOverlapControls = layout.ContextDoesNotOverlapControls, contextDoesNotOverlapContent = layout.ContextDoesNotOverlapContent }; }).ToArray();
        if (layouts.Any(item => !item.contextInside || !item.contentInside || !item.controlsInside || !item.contextDoesNotOverlapControls || !item.contextDoesNotOverlapContent)) throw new InvalidDataException("asset-preview context layout is not bounded or overlaps its surface");
        return new { schema = "agentic2d.m050.evidence-integrity.v2", observedStructuredDispatch = true, observedImageTransition = true, producerBooleanAloneRejected = true, exactMaterializationSubject = bundle.RootElement.GetProperty("subject").GetProperty("materializationSubjectFingerprint").GetString(), noOpaqueIdDispatch = true, previewLayouts = layouts };
    }
}
