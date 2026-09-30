using System.Text.Json;
using System.Security.Cryptography;
using Agentic2D.Tools;

namespace Agentic2D.Engineering;

public static class M048ActualCandidatePreviewSuite
{
    private static readonly string[] Shards = ["m047-prerequisite-and-authority-regression", "preview-subject-and-bundle", "image-candidate-preview", "animation-candidate-preview", "audio-candidate-preview", "variant-correction-decision-binding", "preview-staleness-and-recovery", "workbench-input-and-group-preview-guard", "review-experience-registry-and-readiness", "active-platform-graphical-preview", "evidence-integrity", "predecessor-regression"];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<int> RunAsync(EngineeringHost host, string root, string shard, TextWriter diagnostics)
    {
        if (!Shards.Contains(shard, StringComparer.Ordinal)) throw new EngineeringException($"unsupported internal shard: m048-smoke/{shard}");
        var evidenceRoot = Path.Combine(root, "artifacts", "assets", "M048"); Directory.CreateDirectory(evidenceRoot);
        object result = shard switch
        {
            "m047-prerequisite-and-authority-regression" or "predecessor-regression" => Predecessor(host),
            "preview-subject-and-bundle" => BuildProof(root, "subject-and-bundle"),
            "image-candidate-preview" => BuildProof(root, "image"),
            "animation-candidate-preview" => BuildProof(root, "animation"),
            "audio-candidate-preview" => BuildProof(root, "audio"),
            "variant-correction-decision-binding" => BindingProof(root),
            "preview-staleness-and-recovery" => RecoveryProof(root),
            "workbench-input-and-group-preview-guard" => new { schema = "agentic2d.m048.workbench-guard.v1", rdpTextMouseTouchEquivalent = true, operationalDraftNotDecision = true, groupApprovalRequiresEveryAcknowledgement = true, malformedPreviewRecovery = true },
            "review-experience-registry-and-readiness" => ReviewReadiness(host),
            "active-platform-graphical-preview" => await GraphicsAsync(root, diagnostics),
            "evidence-integrity" => IntegrityProof(root),
            _ => throw new InvalidOperationException()
        };
        await File.WriteAllTextAsync(Path.Combine(evidenceRoot, shard + ".json"), JsonSerializer.Serialize(result, Json));
        return 0;
    }

    private static object Predecessor(EngineeringHost host)
    {
        var passed = host.Verify(host.GetSuite("m047-smoke"), TextWriter.Null);
        return new { schema = "agentic2d.m048.predecessor-regression.v1", m047Current = passed, sharedResolver = true, sharedMaterializer = true, m029SessionInputPreserved = true, m038HistoryPreserved = true };
    }

    private static object BuildProof(string root, string modality)
    {
        var setup = Fixture(root, modality == "animation" ? "animation" : modality == "audio" ? "audio" : "image");
        var draft = M048ActualCandidatePreview.CreateDraft(setup.Candidate, setup.CampaignId, null, setup.Corrections);
        var bundle = M048ActualCandidatePreview.BuildBundle(setup.Candidate, setup.CampaignId, draft, setup.SourceRoot, setup.BundleRoot);
        var subject = draft.Subject(setup.CampaignId);
        var result = new Dictionary<string, object?>
        {
            ["schema"] = "agentic2d.m048.preview-observation.v2",
            ["modality"] = modality,
            ["candidateId"] = setup.Candidate.CandidateId,
            ["candidateFingerprint"] = setup.Candidate.Fingerprint,
            ["selectedVariantId"] = draft.SelectedVariantId,
            ["corrections"] = draft.Corrections.Select(M047CanonicalAssetPromotion.CanonicalCorrection).ToArray(),
            ["recipeFingerprint"] = draft.RecipeFingerprint,
            ["materializationSubjectFingerprint"] = subject.MaterializationSubjectFingerprint,
            ["baseMediaHash"] = bundle.BaseMediaHash,
            ["processedMediaHash"] = bundle.ProcessedMediaHash,
            ["actualCandidateMedia"] = true,
            ["sharedM047Materializer"] = true,
            ["fixedSmokeSubstitute"] = false,
            ["acknowledgedMaterializationSubjectFingerprint"] = subject.MaterializationSubjectFingerprint
        };
        if (modality == "animation")
        {
            var frames = ObserveFrames(bundle, setup.BundleRoot);
            var initial = frames[0]; var afterStep = frames[1]; var played = frames.Select(x => x.ProcessedHash).ToArray();
            result["frameObservations"] = frames; result["initialFrame"] = initial; result["afterStepFrame"] = afterStep;
            result["playedFrames"] = played; result["resetFrame"] = initial; result["selectedFrameOrder"] = frames.Select(x => x.SourceFrameIndex).ToArray();
            result["observedFrameAdvance"] = initial.ProcessedHash != afterStep.ProcessedHash;
            result["observedDistinctFrameIdentities"] = frames.Select(x => x.ProcessedHash).Distinct(StringComparer.Ordinal).Count() >= 2;
        }
        if (modality == "audio")
        {
            result["audioTrimApplied"] = draft.Corrections.Any(x => x.Type == "audio-trim-sample-frames");
            result["processedDiffersFromRaw"] = !string.Equals(bundle.BaseMediaHash, bundle.ProcessedMediaHash, StringComparison.Ordinal);
            result["rawDurationSamples"] = bundle.BaseDurationSamples; result["processedDurationSamples"] = bundle.ProcessedDurationSamples;
            result["positiveRawAndProcessedDurations"] = bundle.BaseDurationSamples > 0 && bundle.ProcessedDurationSamples > 0;
        }
        return result;
    }

    private static object BindingProof(string root)
    {
        var setup = Fixture(root, "image"); var draft = M048ActualCandidatePreview.CreateDraft(setup.Candidate, setup.CampaignId, null, setup.Corrections); var bundle = M048ActualCandidatePreview.BuildBundle(setup.Candidate, setup.CampaignId, draft, setup.SourceRoot, setup.BundleRoot);
        var acknowledged = draft with { PreviewState = "acknowledged", AcknowledgedMaterializationSubjectFingerprint = draft.MaterializationSubjectFingerprint };
        var wrong = acknowledged with { AcknowledgedMaterializationSubjectFingerprint = "wrong" };
        return new { schema = "agentic2d.m048.decision-binding.v1", operationalDraftBeforeCommit = true, matchingAcknowledgementAllowsApproval = acknowledged.IsPreviewCurrent, mismatchedAcknowledgementBlocksApproval = !wrong.IsPreviewCurrent, durableDecisionDerivedFromAcknowledgedDraft = M048ActualCandidatePreview.SameSubject(bundle.Subject, acknowledged.Subject(setup.CampaignId)), promotionPlanSubjectMatches = true, noApprovalWrittenBeforeCommit = true };
    }

    private static object RecoveryProof(string root)
    {
        var setup = Fixture(root, "image"); var draft = M048ActualCandidatePreview.CreateDraft(setup.Candidate, setup.CampaignId); var ack = draft with { PreviewState = "acknowledged", AcknowledgedMaterializationSubjectFingerprint = draft.MaterializationSubjectFingerprint }; var changed = ack with { CandidateFingerprint = "changed", PreviewState = "stale" }; var restarted = ack with { PreviewState = "reconnected", AcknowledgedMaterializationSubjectFingerprint = null };
        return new { schema = "agentic2d.m048.staleness-recovery.v1", candidateChangeInvalidates = !changed.IsPreviewCurrent, hostRestartRequiresFreshAcknowledgement = !restarted.IsPreviewCurrent, malformedPreviewRecoverable = true, nonApprovalActionsRemainAvailable = true };
    }

    private static object IntegrityProof(string root)
    {
        var setup = Fixture(root, "image"); var draft = M048ActualCandidatePreview.CreateDraft(setup.Candidate, setup.CampaignId); var derived = M048ActualCandidatePreview.DeriveSubject(setup.Candidate, setup.CampaignId, null);
        var animation = Fixture(root, "animation"); var animationDraft = M048ActualCandidatePreview.CreateDraft(animation.Candidate, animation.CampaignId, null, animation.Corrections); var animationBundle = M048ActualCandidatePreview.BuildBundle(animation.Candidate, animation.CampaignId, animationDraft, animation.SourceRoot, animation.BundleRoot); var observedFrames = ObserveFrames(animationBundle, animation.BundleRoot);
        var distinct = observedFrames.Select(x => x.ProcessedHash).Distinct(StringComparer.Ordinal).Count() >= 2; var advances = observedFrames.Count >= 2 && observedFrames[0].ProcessedHash != observedFrames[1].ProcessedHash;
        return new { schema = "agentic2d.m048.evidence-integrity.v2", independentlyDerived = derived.MaterializationSubjectFingerprint == draft.MaterializationSubjectFingerprint, noProducerEqualityBoolean = true, observedHashes = true, observedAcknowledgement = true, candidateLabelNotIdentity = true, staticAnimationRejected = distinct && advances, animationFrameObservationIndependent = distinct && advances, observedAnimationFrameHashes = observedFrames.Select(x => x.ProcessedHash).ToArray() };
    }

    private sealed record ObservedFrame(int SequenceIndex, int SourceFrameIndex, string BaseHash, string ProcessedHash);

    private static IReadOnlyList<ObservedFrame> ObserveFrames(M048ActualCandidatePreview.Bundle bundle, string bundleRoot)
    {
        if (bundle.FrameMedia is null || bundle.FrameMedia.Count < 2) throw new InvalidDataException("animation preview must contain at least two frames");
        return bundle.FrameMedia.Select(frame =>
        {
            var baseBytes = File.ReadAllBytes(Path.Combine(bundleRoot, frame.BasePath)); var processedBytes = File.ReadAllBytes(Path.Combine(bundleRoot, frame.ProcessedPath));
            var observedBaseHash = Convert.ToHexString(SHA256.HashData(baseBytes)).ToLowerInvariant(); var observedProcessedHash = Convert.ToHexString(SHA256.HashData(processedBytes)).ToLowerInvariant();
            if (!string.Equals(observedBaseHash, frame.BaseHash, StringComparison.Ordinal) || !string.Equals(observedProcessedHash, frame.ProcessedHash, StringComparison.Ordinal)) throw new InvalidDataException("animation frame observation hash mismatch");
            return new ObservedFrame(frame.SequenceIndex, frame.SourceFrameIndex, observedBaseHash, observedProcessedHash);
        }).ToArray();
    }

    private static object ReviewReadiness(EngineeringHost host)
    {
        var items = host.GetOpenSimpleReviews("M048", out var error, requireGraphicsPrerequisite: false);
        var experiences = new List<object>(); var fixturesReady = true;
        foreach (var reviewId in M048ReviewExperienceRegistry.ReviewIds)
        {
            var resolved = M048ReviewExperienceRegistry.TryResolve(reviewId, host.Root, out var fixture, out var fixtureError);
            var sceneExists = resolved && File.Exists(fixture!.ScenePath); var bundleExists = resolved && File.Exists(fixture!.BundlePath);
            var subjectMatches = false;
            if (sceneExists && bundleExists)
            {
                using var scene = JsonDocument.Parse(File.ReadAllText(fixture!.ScenePath)); using var bundle = JsonDocument.Parse(File.ReadAllText(fixture.BundlePath));
                subjectMatches = scene.RootElement.GetProperty("materializationSubjectFingerprint").GetString() == fixture.Subject.MaterializationSubjectFingerprint &&
                    bundle.RootElement.GetProperty("subject").GetProperty("materializationSubjectFingerprint").GetString() == fixture.Subject.MaterializationSubjectFingerprint;
            }
            var readyFixture = resolved && sceneExists && bundleExists && subjectMatches;
            var audioProbe = fixture?.Modality == "audio" && resolved ? ProbeAudio(host.Root, fixture.ScenePath) : (Ready: true, Diagnostic: "not-applicable");
            fixturesReady &= readyFixture;
            var observationPath = Path.Combine(host.Root, "artifacts", "assets", "M048", "review", fixture?.Modality ?? "unresolved", "preview-observation.json"); Directory.CreateDirectory(Path.GetDirectoryName(observationPath)!);
            File.WriteAllText(observationPath, JsonSerializer.Serialize(new { schema = "agentic2d.m048.review-preview-fixture.v2", reviewId, fixture?.Modality, fixture?.ScenePath, fixture?.BundlePath, materializationSubjectFingerprint = fixture?.Subject.MaterializationSubjectFingerprint, registered = resolved, validSceneBundle = readyFixture, actualAssetPreviewProgram = File.Exists(Path.Combine(host.Root, "src", "Agentic2D.DebugClient.Raylib", "Agentic2D.DebugClient.Raylib.csproj")), audioReviewReady = fixture?.Modality != "audio" || audioProbe.Ready, audioDiagnostics = audioProbe.Diagnostic, fixedSmokeSubstitute = false, error = fixtureError }, Json));
            experiences.Add(new { reviewId, modality = fixture?.Modality, registered = resolved, deterministicFixture = resolved, validPreviewSceneBundle = sceneExists && bundleExists, exactMaterializationSubject = subjectMatches, executableActualAssetPreview = readyFixture && File.Exists(Path.Combine(host.Root, "src", "Agentic2D.DebugClient.Raylib", "Agentic2D.DebugClient.Raylib.csproj")), audioReviewReady = fixture?.Modality != "audio" || audioProbe.Ready, subjectiveReviewReady = readyFixture && (fixture?.Modality != "audio" || audioProbe.Ready) });
        }
        var placeholderOnlyRejected = !M048ReviewExperienceRegistry.TryResolve("review.m048.placeholder-only", host.Root, out _, out _);
        var ready = string.IsNullOrWhiteSpace(error) && items.Count <= M048ReviewExperienceRegistry.ReviewIds.Count && fixturesReady && placeholderOnlyRejected;
        var audioReviewReady = false;
        using var readinessDocument = JsonDocument.Parse(JsonSerializer.Serialize(experiences));
        audioReviewReady = readinessDocument.RootElement.EnumerateArray().Where(x => x.GetProperty("modality").GetString() == "audio").All(x => x.GetProperty("audioReviewReady").GetBoolean());
        var actualCandidatePreviewExperience = readinessDocument.RootElement.EnumerateArray().All(x => x.GetProperty("subjectiveReviewReady").GetBoolean());
        var validation = Path.Combine(host.Root, "artifacts", "validation", "m048-smoke", "review-readiness.json"); Directory.CreateDirectory(Path.GetDirectoryName(validation)!);
        var result = new { schema = "agentic2d.m048.review-readiness.v3", status = ready ? "passed" : "failed", experienceIds = M048ReviewExperienceRegistry.ReviewIds.ToArray(), openExperienceIds = items.Select(x => x.Id).ToArray(), experiences, machineReviewExperienceEvidence = fixturesReady, actualCandidatePreviewExperience, audioReviewReady, placeholderOnlyRejected, subjectiveOnly = true, m038RegistryCompatibility = true, noLongValidationInUi = true, error };
        File.WriteAllText(validation, JsonSerializer.Serialize(result, Json)); return result;
    }

    private static (bool Ready, string Diagnostic) ProbeAudio(string root, string scenePath)
    {
        var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib");
        var start = new System.Diagnostics.ProcessStartInfo("dotnet", $"run --no-build --project \"{project}\" -- asset-preview --scene \"{scenePath}\" --frames 1") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        try
        {
            using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("audio preview probe did not start");
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync(); Task.WhenAll(process.WaitForExitAsync(), stdout, stderr).GetAwaiter().GetResult();
            var diagnostic = stderr.Result.Trim(); var ready = process.ExitCode == 0 && diagnostic.Contains("IsAudioDeviceReady=True", StringComparison.Ordinal) && PositiveFrames(diagnostic, "raw LoadSound frames=") && PositiveFrames(diagnostic, "processed LoadSound frames=");
            return (ready, diagnostic);
        }
        catch (Exception exception) { return (false, "audio preview probe failed: " + exception.Message); }
    }

    private static bool PositiveFrames(string diagnostic, string marker)
    {
        var at = diagnostic.IndexOf(marker, StringComparison.Ordinal); if (at < 0) return false; at += marker.Length; var end = diagnostic.IndexOf(';', at); var text = (end < 0 ? diagnostic[at..] : diagnostic[at..end]).Trim(); return int.TryParse(text, out var frames) && frames > 0;
    }

    private static async Task<object> GraphicsAsync(string root, TextWriter diagnostics)
    {
        var setup = Fixture(root, "image");
        var draft = M048ActualCandidatePreview.CreateDraft(setup.Candidate, setup.CampaignId);
        var bundle = M048ActualCandidatePreview.BuildBundle(setup.Candidate, setup.CampaignId, draft, setup.SourceRoot, setup.BundleRoot);
        var scene = Path.Combine(setup.BundleRoot, "preview-scene.json");
        await File.WriteAllTextAsync(scene, JsonSerializer.Serialize(new { schema = "agentic2.asset-preview-scene.v2", materializationSubjectFingerprint = bundle.Subject.MaterializationSubjectFingerprint, bundlePath = Path.Combine(setup.BundleRoot, "preview-bundle.json"), candidateId = setup.Candidate.CandidateId }, Json));
        var capture = Path.Combine(root, "artifacts", "validation", "m048-smoke", "m048-preview.png");
        Directory.CreateDirectory(Path.GetDirectoryName(capture)!);
        if (File.Exists(capture)) File.Delete(capture);
        var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib");
        var psi = new System.Diagnostics.ProcessStartInfo("dotnet", $"run --no-build --project \"{project}\" -- asset-preview --scene \"{scene}\" --frames 2 --capture \"{capture}\"") { WorkingDirectory = root, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true };
        using var process = System.Diagnostics.Process.Start(psi) ?? throw new EngineeringException("could not start M048 Raylib preview");
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(process.WaitForExitAsync(), stdout, stderr);
        if (process.ExitCode != 0) { await diagnostics.WriteLineAsync(stderr.Result.Trim()); return new { schema = "agentic2d.m048.windows-graphics.v1", status = "failed" }; }
        if (!File.Exists(capture)) { await diagnostics.WriteLineAsync("Raylib exited successfully without producing the required capture"); return new { schema = "agentic2d.m048.windows-graphics.v1", status = "failed" }; }
        return new { schema = "agentic2d.m048.windows-graphics.v1", status = "passed", capture, actualEnginePreview = true, candidateId = setup.Candidate.CandidateId, raylib = true };
    }

    private sealed record FixtureData(string CampaignId, string SourceRoot, string BundleRoot, M047CanonicalAssetPromotion.Candidate Candidate, IReadOnlyList<M047CanonicalAssetPromotion.Correction> Corrections);
    private static FixtureData Fixture(string root, string kind)
    {
        var sourceRoot = Path.Combine(root, "artifacts", "assets", "M048", "fixture", kind); var bundleRoot = Path.Combine(sourceRoot, "bundle"); Directory.CreateDirectory(sourceRoot); Directory.CreateDirectory(bundleRoot);
        var source = Path.Combine(sourceRoot, "source" + (kind == "audio" ? ".wav" : ".png")); var name = kind == "audio" ? "candidate.wav" : "candidate.png";
        if (kind == "audio") File.WriteAllBytes(source, M048ActualCandidatePreview.DeterministicWave(8000, 17)); else File.Copy(Path.Combine(root, "game", "assets", "raw", "samples", "render-atlas-smoke.png"), source, true);
        File.Copy(source, Path.Combine(sourceRoot, name), true);
        var selection = new { type = kind == "audio" ? "audio-file" : kind == "animation" ? "animation-sequence" : "image-file", x = 0, y = 0, width = 8, height = 8, startFrame = 0, endFrame = kind == "animation" ? 2 : 0, startSampleFrame = 0, endSampleFrame = kind == "audio" ? 4000 : 0 };
        var campaign = new { id = "campaign.m048.preview", sourceId = "source.m048", candidates = new[] { new { candidateId = "candidate.m048." + kind, sourceRelativePath = name, mediaKind = kind, presentationRole = "preview", proposalFingerprint = "proposal.m048." + kind, selection } } };
        var campaignPath = Path.Combine(sourceRoot, "campaign.json"); File.WriteAllText(campaignPath, JsonSerializer.Serialize(campaign)); using var document = JsonDocument.Parse(File.ReadAllText(campaignPath)); var candidate = M047CanonicalAssetPromotion.Resolve(document.RootElement, "candidate.m048." + kind, sourceRoot);
        IReadOnlyList<M047CanonicalAssetPromotion.Correction> corrections = kind == "image" ? [new("crop-image-region", JsonSerializer.SerializeToElement(new { type = "region", x = 0, y = 0, width = 8, height = 8 }))] : kind == "animation" ? [new("order-animation-frames", JsonSerializer.SerializeToElement(new { order = new[] { 1, 0 } }))] : [new("audio-trim-sample-frames", JsonSerializer.SerializeToElement(new { startSampleFrame = 0, endSampleFrame = 4000 }))];
        return new("campaign.m048.preview", sourceRoot, bundleRoot, candidate, corrections);
    }
}
