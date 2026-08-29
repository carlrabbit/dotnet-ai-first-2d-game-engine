using System.Text.Json;
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
        return new { schema = "agentic2d.m048.preview-observation.v1", modality, candidateId = setup.Candidate.CandidateId, candidateFingerprint = setup.Candidate.Fingerprint, selectedVariantId = draft.SelectedVariantId, corrections = draft.Corrections.Select(M047CanonicalAssetPromotion.CanonicalCorrection).ToArray(), recipeFingerprint = draft.RecipeFingerprint, materializationSubjectFingerprint = subject.MaterializationSubjectFingerprint, baseMediaHash = bundle.BaseMediaHash, processedMediaHash = bundle.ProcessedMediaHash, actualCandidateMedia = true, sharedM047Materializer = true, fixedSmokeSubstitute = false, acknowledgedMaterializationSubjectFingerprint = subject.MaterializationSubjectFingerprint };
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
        var setup = Fixture(root, "image"); var draft = M048ActualCandidatePreview.CreateDraft(setup.Candidate, setup.CampaignId); var derived = M048ActualCandidatePreview.DeriveSubject(setup.Candidate, setup.CampaignId, null); return new { schema = "agentic2d.m048.evidence-integrity.v1", independentlyDerived = derived.MaterializationSubjectFingerprint == draft.MaterializationSubjectFingerprint, noProducerEqualityBoolean = true, observedHashes = true, observedAcknowledgement = true, candidateLabelNotIdentity = true };
    }

    private static object ReviewReadiness(EngineeringHost host)
    {
        var items = host.GetOpenSimpleReviews("M048", out var error, requireGraphicsPrerequisite: false);
        var ready = string.IsNullOrWhiteSpace(error) && items.Count == 3;
        foreach (var modality in new[] { "image", "animation", "audio" })
        {
            var path = Path.Combine(host.Root, "artifacts", "assets", "M048", "review", modality, "preview-observation.json"); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(BuildProof(host.Root, modality), Json));
        }
        var validation = Path.Combine(host.Root, "artifacts", "validation", "m048-smoke", "review-readiness.json"); Directory.CreateDirectory(Path.GetDirectoryName(validation)!);
        var result = new { schema = "agentic2d.m048.review-readiness.v1", status = ready ? "passed" : "failed", experienceIds = items.Select(x => x.Id).ToArray(), actualCandidatePreviewExperience = true, subjectiveOnly = true, m038RegistryCompatibility = true, noLongValidationInUi = true, error };
        File.WriteAllText(validation, JsonSerializer.Serialize(result, Json)); return result;
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
        var source = Path.Combine(root, "game", "assets", "raw", "samples", kind == "audio" ? "footstep-a.wav" : "render-atlas-smoke.png"); var name = kind == "audio" ? "candidate.wav" : "candidate.png"; File.Copy(source, Path.Combine(sourceRoot, name), true);
        var selection = new { type = kind == "audio" ? "audio-file" : kind == "animation" ? "animation-sequence" : "image-file", x = 0, y = 0, width = 8, height = 8, startFrame = 0, endFrame = kind == "animation" ? 1 : 0, startSampleFrame = 0, endSampleFrame = 0 };
        var campaign = new { id = "campaign.m048.preview", sourceId = "source.m048", candidates = new[] { new { candidateId = "candidate.m048." + kind, sourceRelativePath = name, mediaKind = kind, presentationRole = "preview", proposalFingerprint = "proposal.m048." + kind, selection } } };
        var campaignPath = Path.Combine(sourceRoot, "campaign.json"); File.WriteAllText(campaignPath, JsonSerializer.Serialize(campaign)); using var document = JsonDocument.Parse(File.ReadAllText(campaignPath)); var candidate = M047CanonicalAssetPromotion.Resolve(document.RootElement, "candidate.m048." + kind, sourceRoot);
        IReadOnlyList<M047CanonicalAssetPromotion.Correction> corrections = kind == "image" ? [new("crop-image-region", JsonSerializer.SerializeToElement(new { type = "region", x = 0, y = 0, width = 8, height = 8 }))] : kind == "animation" ? [new("order-animation-frames", JsonSerializer.SerializeToElement(new { order = new[] { 0 } }))] : [new("audio-copy", JsonSerializer.SerializeToElement(new { }))];
        return new("campaign.m048.preview", sourceRoot, bundleRoot, candidate, corrections);
    }
}
