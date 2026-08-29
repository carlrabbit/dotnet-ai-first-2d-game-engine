using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Agentic2D.Tools;

/// <summary>Shared M048 preview identity and bundle primitives. Processing delegates to M047.</summary>
public static class M048ActualCandidatePreview
{
    public const string DraftSchema = "agentic2d.asset-curation-draft.v1";
    public const string SubjectSchema = "agentic2d.asset-preview-subject.v1";
    public const string BundleSchema = "agentic2d.asset-preview-bundle.v1";
    public const string ObservationSchema = "agentic2d.asset-preview-observation.v1";
    public const string PreviewProtocol = "v2";

    public sealed record Draft(string CandidateId, string CandidateFingerprint, string? SelectedVariantId,
        string? SelectedVariantFingerprint, IReadOnlyList<M047CanonicalAssetPromotion.Correction> Corrections,
        string RecipeFingerprint, string MaterializationSubjectFingerprint, string PreviewState = "not-previewed",
        string? AcknowledgedMaterializationSubjectFingerprint = null)
    {
        public MaterializationSubject Subject(string campaignId) => new(campaignId, CandidateId, CandidateFingerprint,
            SelectedVariantId, SelectedVariantFingerprint, Corrections, RecipeFingerprint, MaterializationSubjectFingerprint);
        public bool IsPreviewCurrent => PreviewState == "acknowledged" &&
            string.Equals(AcknowledgedMaterializationSubjectFingerprint, MaterializationSubjectFingerprint, StringComparison.Ordinal);
    }

    public sealed record MaterializationSubject(string CampaignId, string CandidateId, string CandidateFingerprint,
        string? SelectedVariantId, string? SelectedVariantFingerprint,
        IReadOnlyList<M047CanonicalAssetPromotion.Correction> Corrections, string RecipeFingerprint,
        string MaterializationSubjectFingerprint)
    {
        public object ToPayload() => new
        {
            schema = SubjectSchema,
            campaignId = CampaignId,
            candidateId = CandidateId,
            candidateFingerprint = CandidateFingerprint,
            selectedVariantId = SelectedVariantId,
            selectedVariantFingerprint = SelectedVariantFingerprint,
            corrections = Corrections.Select(M047CanonicalAssetPromotion.CanonicalCorrection).ToArray(),
            recipeFingerprint = RecipeFingerprint,
            materializationSubjectFingerprint = MaterializationSubjectFingerprint
        };
    }

    public sealed record Bundle(MaterializationSubject Subject, string MediaKind, string BaseMediaHash,
        string ProcessedMediaHash, string BaseMediaPath, string ProcessedMediaPath, string Modality,
        IReadOnlyList<string> FrameHashes, int? SampleRate, int? BaseDurationSamples, int? ProcessedDurationSamples)
    {
        public object ToPayload() => new
        {
            schema = BundleSchema,
            subject = Subject.ToPayload(),
            mediaKind = MediaKind,
            modality = Modality,
            baseMediaHash = BaseMediaHash,
            processedMediaHash = ProcessedMediaHash,
            baseMediaPath = BaseMediaPath,
            processedMediaPath = ProcessedMediaPath,
            frameHashes = FrameHashes,
            sampleRate = SampleRate,
            baseDurationSamples = BaseDurationSamples,
            processedDurationSamples = ProcessedDurationSamples
        };
    }

    public sealed record ReviewFixture(string ReviewId, string Modality, string Description,
        string ScenePath, string BundlePath, MaterializationSubject Subject);

    public static ReviewFixture CreateReviewFixture(string root, string reviewId)
    {
        var (modality, description, kind) = reviewId switch
        {
            "review.m048.01-image-candidate-curation" => ("image", "Exact image/region candidate with the current-draft processed variant.", "image"),
            "review.m048.02-animation-candidate-curation" => ("animation", "Exact animation candidate with deterministic selected frame order.", "animation"),
            "review.m048.03-audio-candidate-curation" => ("audio", "Exact audio candidate with manual raw/processed comparison.", "audio"),
            _ => throw new InvalidDataException("review ID is not a registered M048 candidate-preview experience")
        };
        var sourceRoot = Path.Combine(root, "artifacts", "assets", "M048", "fixture", kind);
        var bundleRoot = Path.Combine(sourceRoot, "review-bundle");
        Directory.CreateDirectory(sourceRoot);
        Directory.CreateDirectory(bundleRoot);
        var source = Path.Combine(root, "game", "assets", "raw", "samples", kind == "audio" ? "footstep-a.wav" : "render-atlas-smoke.png");
        var name = kind == "audio" ? "candidate.wav" : "candidate.png";
        File.Copy(source, Path.Combine(sourceRoot, name), true);
        var selection = new { type = kind == "audio" ? "audio-file" : kind == "animation" ? "animation-sequence" : "image-file", x = 0, y = 0, width = 8, height = 8, startFrame = 0, endFrame = kind == "animation" ? 1 : 0, startSampleFrame = 0, endSampleFrame = 0 };
        var campaign = new { id = "campaign.m048.review", sourceId = "source.m048.review", candidates = new[] { new { candidateId = "candidate.m048.review." + kind, sourceRelativePath = name, mediaKind = kind, presentationRole = "review", proposalFingerprint = "proposal.m048.review." + kind, selection } } };
        var campaignPath = Path.Combine(sourceRoot, "review-campaign.json");
        File.WriteAllText(campaignPath, JsonSerializer.Serialize(campaign));
        using var document = JsonDocument.Parse(File.ReadAllText(campaignPath));
        var candidate = M047CanonicalAssetPromotion.Resolve(document.RootElement, "candidate.m048.review." + kind, sourceRoot);
        IReadOnlyList<M047CanonicalAssetPromotion.Correction> corrections = kind switch
        {
            "image" => [new("crop-image-region", JsonSerializer.SerializeToElement(new { type = "region", x = 0, y = 0, width = 8, height = 8 }))],
            "animation" => [new("order-animation-frames", JsonSerializer.SerializeToElement(new { order = new[] { 0 } }))],
            _ => [new("audio-copy", JsonSerializer.SerializeToElement(new { }))]
        };
        var draft = CreateDraft(candidate, campaign.id, null, corrections);
        var bundle = BuildBundle(candidate, campaign.id, draft, sourceRoot, bundleRoot);
        var scenePath = Path.Combine(bundleRoot, "preview-scene.json");
        File.WriteAllText(scenePath, JsonSerializer.Serialize(new
        {
            schema = "agentic2d.asset-preview-scene.v2",
            materializationSubjectFingerprint = bundle.Subject.MaterializationSubjectFingerprint,
            bundlePath = Path.Combine(bundleRoot, "preview-bundle.json"),
            candidateId = candidate.CandidateId,
            reviewId,
            modality
        }, Options));
        return new(reviewId, modality, description, scenePath, Path.Combine(bundleRoot, "preview-bundle.json"), bundle.Subject);
    }

    public static Draft CreateDraft(M047CanonicalAssetPromotion.Candidate candidate, string campaignId,
        string? variantId = null, IReadOnlyList<M047CanonicalAssetPromotion.Correction>? corrections = null)
    {
        var selected = candidate.Variants.SingleOrDefault(v => v.Id == variantId);
        if (variantId is not null && selected is null) throw new InvalidDataException("selected preview variant is not current candidate authority");
        var ops = corrections ?? [];
        if (ops.Any(x => !M047CanonicalAssetPromotion.SupportedCorrections.Contains(x.Type))) throw new InvalidDataException("preview contains unsupported M047 correction");
        var input = selected?.Fingerprint ?? candidate.SourceFingerprint;
        var recipe = M047CanonicalAssetPromotion.RecipeFingerprint(candidate.Fingerprint, selected?.Id,
            ops, [input]);
        var subjectMaterial = CanonicalSubject(campaignId, candidate.CandidateId, candidate.Fingerprint,
            selected?.Id, selected?.Fingerprint, ops, recipe);
        return new(candidate.CandidateId, candidate.Fingerprint, selected?.Id, selected?.Fingerprint, ops, recipe,
            Hash(subjectMaterial));
    }

    public static MaterializationSubject DeriveSubject(M047CanonicalAssetPromotion.Candidate candidate, string campaignId,
        string? variantId, IReadOnlyList<M047CanonicalAssetPromotion.Correction>? corrections = null)
        => CreateDraft(candidate, campaignId, variantId, corrections).Subject(campaignId);

    public static Bundle BuildBundle(M047CanonicalAssetPromotion.Candidate candidate, string campaignId, Draft draft,
        string sourceRoot, string outputDirectory)
    {
        if (!string.Equals(candidate.CandidateId, draft.CandidateId, StringComparison.Ordinal) ||
            !string.Equals(candidate.Fingerprint, draft.CandidateFingerprint, StringComparison.Ordinal))
            throw new InvalidDataException("preview draft is stale for current candidate");
        var variant = candidate.Variants.SingleOrDefault(v => v.Id == draft.SelectedVariantId);
        var relative = variant?.SourceRelativePath ?? candidate.SourceRelativePath;
        var selection = variant?.Selection ?? candidate.Selection;
        var sourcePath = Path.Combine(sourceRoot, relative.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(sourcePath)) throw new FileNotFoundException("preview source is unavailable", sourcePath);
        var source = File.ReadAllBytes(sourcePath);
        var processed = M047CanonicalAssetPromotion.Materialize(source, candidate.MediaKind, selection, draft.Corrections);
        Directory.CreateDirectory(outputDirectory);
        var basePath = Path.Combine(outputDirectory, "base" + Extension(candidate.MediaKind, relative));
        var processedPath = Path.Combine(outputDirectory, "processed" + Extension(candidate.MediaKind, relative));
        File.WriteAllBytes(basePath, source); File.WriteAllBytes(processedPath, processed);
        IReadOnlyList<string> frames = candidate.MediaKind == "animation" ? [candidate.SourceFingerprint, .. candidate.Variants.Select(x => x.Fingerprint)] : [];
        var (rate, baseSamples) = WavInfo(source, candidate.MediaKind); var (_, processedSamples) = WavInfo(processed, candidate.MediaKind);
        var bundle = new Bundle(draft.Subject(campaignId), candidate.MediaKind, Hash(source), Hash(processed),
            Path.GetRelativePath(outputDirectory, basePath).Replace('\\', '/'), Path.GetRelativePath(outputDirectory, processedPath).Replace('\\', '/'),
            candidate.MediaKind switch { "animation" => "animation-sequence", "audio" => "audio-file", _ => "image" }, frames, rate, baseSamples, processedSamples);
        File.WriteAllText(Path.Combine(outputDirectory, "preview-bundle.json"), JsonSerializer.Serialize(bundle.ToPayload(), Options));
        return bundle;
    }

    public static bool Acknowledges(Draft draft, JsonElement response) =>
        response.TryGetProperty("status", out var status) && status.GetString() == "ok" &&
        response.TryGetProperty("acknowledgedMaterializationSubjectFingerprint", out var fp) &&
        fp.GetString() == draft.MaterializationSubjectFingerprint;

    public static bool CanCommitApproval(Draft draft, string currentCandidateFingerprint, string? acknowledgedSubject)
        => draft.IsPreviewCurrent && draft.CandidateFingerprint == currentCandidateFingerprint &&
           acknowledgedSubject == draft.MaterializationSubjectFingerprint;

    public static M047CanonicalAssetPromotion.Decision CommitApproval(Draft draft, string campaignId, string decisionId, string action, string consequence, int sequence, string? supersedes = null)
    {
        if (!CanCommitApproval(draft, draft.CandidateFingerprint, draft.AcknowledgedMaterializationSubjectFingerprint)) throw new InvalidOperationException("interactive approval requires a current exact preview acknowledgement");
        if (action is not ("accept-proposal" or "choose-alternative" or "approve-with-corrections" or "presentation-only")) throw new ArgumentException("action is not an approval-like interactive decision");
        return new(decisionId, campaignId, draft.CandidateId, draft.CandidateFingerprint, draft.SelectedVariantId, draft.SelectedVariantFingerprint, draft.Corrections, consequence, sequence, supersedes);
    }

    public static bool SameSubject(MaterializationSubject left, MaterializationSubject right) =>
        left.MaterializationSubjectFingerprint == right.MaterializationSubjectFingerprint &&
        CanonicalSubject(left.CampaignId, left.CandidateId, left.CandidateFingerprint, left.SelectedVariantId,
            left.SelectedVariantFingerprint, left.Corrections, left.RecipeFingerprint) ==
        CanonicalSubject(right.CampaignId, right.CandidateId, right.CandidateFingerprint, right.SelectedVariantId,
            right.SelectedVariantFingerprint, right.Corrections, right.RecipeFingerprint);

    private static string CanonicalSubject(string campaignId, string candidateId, string candidateFingerprint,
        string? variantId, string? variantFingerprint, IReadOnlyList<M047CanonicalAssetPromotion.Correction> corrections, string recipe)
        => string.Join("\n", SubjectSchema, campaignId, candidateId, candidateFingerprint, variantId ?? "", variantFingerprint ?? "",
            string.Join(";", corrections.Select(M047CanonicalAssetPromotion.CanonicalCorrection)), recipe);
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
    private static string Hash(byte[] value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();
    private static string Extension(string kind, string source) => kind == "audio" ? ".wav" : ".png";
    private static (int? Rate, int? Samples) WavInfo(byte[] bytes, string kind)
    {
        if (kind != "audio" || bytes.Length < 44 || Encoding.ASCII.GetString(bytes, 0, 4) != "RIFF") return (null, null);
        var rate = BitConverter.ToInt32(bytes, 24); var channels = BitConverter.ToInt16(bytes, 22); var bits = BitConverter.ToInt16(bytes, 34);
        var data = BitConverter.ToInt32(bytes, 40); return (rate, channels > 0 && bits > 0 ? data / (channels * (bits / 8)) : null);
    }
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
}
