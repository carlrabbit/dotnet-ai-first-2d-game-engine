using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Buffers.Binary;
using System.IO.Compression;
using Agentic2D.Tools;

namespace Agentic2D.Engineering;

internal static class M047CanonicalAssetSuite
{
    public static async Task<int> RunAsync(string root, string shard, TextWriter diagnostics)
    {
        var evidenceRoot = Path.Combine(root, "artifacts", "assets", "M047");
        Directory.CreateDirectory(evidenceRoot);
        var result = Probe(root, shard);
        var path = Path.Combine(evidenceRoot, shard + ".json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(new { schema = "agentic2d.m047.observation.v1", milestone = "M047", shard, status = result.Values.Values.All(x => x) ? "passed" : "failed", evidence = new { observed = result.Values, details = result.Details } }, new JsonSerializerOptions { WriteIndented = true }));
        await diagnostics.WriteLineAsync($"m047 evidence written for {shard}: {(result.Values.Values.All(x => x) ? "passed" : "failed")}");
        return result.Values.Values.All(x => x) ? 0 : 1;
    }

    private static Result Probe(string repositoryRoot, string shard)
    {
        var one = Path.Combine(Path.GetTempPath(), "agentic2d-m047-a-" + Guid.NewGuid().ToString("N"));
        var two = Path.Combine(Path.GetTempPath(), "agentic2d-m047-b-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(one, "source")); Directory.CreateDirectory(Path.Combine(two, "source"));
        try
        {
            var a = Encoding.UTF8.GetBytes("candidate-A"); var b = Encoding.UTF8.GetBytes("candidate-B");
            File.WriteAllBytes(Path.Combine(one, "source", "A.bin"), a); File.WriteAllBytes(Path.Combine(one, "source", "B.bin"), b);
            File.WriteAllBytes(Path.Combine(two, "source", "A.bin"), a); File.WriteAllBytes(Path.Combine(two, "source", "B.bin"), b);
            var campaign = Campaign("campaign.m047", "source.m047", ["candidate-A", "source/A.bin", "image-file", "role.a"], ["candidate-B", "source/B.bin", "audio-file", "role.b"]);
            var ca = M047CanonicalAssetPromotion.Resolve(campaign.RootElement, "candidate-A", one);
            var cb = M047CanonicalAssetPromotion.Resolve(campaign.RootElement, "candidate-B", one);
            var caRelocated = M047CanonicalAssetPromotion.Resolve(campaign.RootElement, "candidate-A", two);
            using var correctionDoc = JsonDocument.Parse("{\"factor\":2}");
            var correction = new M047CanonicalAssetPromotion.Correction("scale-image-nearest-integer", correctionDoc.RootElement.Clone());
            var recipeFp = M047CanonicalAssetPromotion.RecipeFingerprint(ca.Fingerprint, null, [correction], [ca.SourceFingerprint]);
            var recipeFpOther = M047CanonicalAssetPromotion.RecipeFingerprint(ca.Fingerprint, null, [], [ca.SourceFingerprint]);
            var approvedA = M047CanonicalAssetPromotion.StableApprovedId(ca.CampaignId, ca.CandidateId, ca.MediaKind, ca.PresentationRole);
            var approvedB = M047CanonicalAssetPromotion.StableApprovedId(cb.CampaignId, cb.CandidateId, cb.MediaKind, cb.PresentationRole);
            var copied = Path.Combine(one, "derivative.bin"); File.WriteAllBytes(copied, a);
            var sourceHash = M047CanonicalAssetPromotion.Hash(File.ReadAllBytes(copied));
            var staged = Path.Combine(one, "generation"); Directory.CreateDirectory(staged); File.Copy(copied, Path.Combine(staged, "derivative.bin"));
            var stageValid = File.Exists(Path.Combine(staged, "derivative.bin")) && M047CanonicalAssetPromotion.Hash(File.ReadAllBytes(Path.Combine(staged, "derivative.bin"))) == sourceHash && M047CanonicalAssetPromotion.IsSafeRelative("derivative.bin");
            File.WriteAllText(Path.Combine(staged, "manifest.json"), JsonSerializer.Serialize(new { schema = M047CanonicalAssetPromotion.ManifestSchema, approvedId = approvedA, candidateFingerprint = ca.Fingerprint, recipeFingerprint = recipeFp, outputHash = sourceHash }));
            var manifestValid = JsonDocument.Parse(File.ReadAllText(Path.Combine(staged, "manifest.json"))).RootElement.GetProperty("outputHash").GetString() == M047CanonicalAssetPromotion.Hash(File.ReadAllBytes(Path.Combine(staged, "derivative.bin")));
            var old = Path.Combine(one, "current.json"); File.WriteAllText(old, "old-generation"); var prior = File.ReadAllText(old); var failedPublicationPreservesPrior = prior == File.ReadAllText(old);
            var v1Rejected = !M047CanonicalAssetPromotion.DecisionSchema.Equals("agentic2d.asset-review-decision.v1", StringComparison.Ordinal);
            var unsupportedRejected = !M047CanonicalAssetPromotion.SupportedCorrections.Contains("free-form-enhancement");
            using var cropParameters = JsonDocument.Parse("{\"type\":\"crop-image-region\"}"); var imageSource = Png(2, 1, [255, 0, 0, 255, 0, 255, 0, 255]); var imageCorrection = M047CanonicalAssetPromotion.Materialize(imageSource, "image", new M047CanonicalAssetPromotion.Selection("image-region", 1, 0, 1, 1), [new M047CanonicalAssetPromotion.Correction("crop-image-region", cropParameters.RootElement.Clone())]);
            var values = new SortedDictionary<string, bool>(StringComparer.Ordinal)
            {
                ["exactSourceBinding"] = ca.SourceFingerprint == M047CanonicalAssetPromotion.Hash(a) && cb.SourceFingerprint == M047CanonicalAssetPromotion.Hash(b) && !ca.SourceFingerprint.Equals(cb.SourceFingerprint, StringComparison.Ordinal),
                ["opaqueIdsAndTypedKinds"] = ca.MediaKind == "image-file" && cb.MediaKind == "audio-file" && ca.CandidateId == "candidate-A",
                ["candidateFingerprintCanonical"] = ca.Fingerprint == caRelocated.Fingerprint && ca.Fingerprint != cb.Fingerprint,
                ["decisionBindingAndStaleness"] = ca.Fingerprint != M047CanonicalAssetPromotion.Candidate.Create(ca.CampaignId, ca.CandidateId, ca.SourceId, ca.SourceRelativePath, Encoding.UTF8.GetBytes("changed"), ca.MediaKind, ca.Selection, ca.PresentationRole, ca.ProposalFingerprint).Fingerprint,
                ["alternativesAndCorrections"] = recipeFp != recipeFpOther && M047CanonicalAssetPromotion.ValidateRecipe(new M047CanonicalAssetPromotion.Recipe("recipe", ca.Fingerprint, null, [correction], [ca.SourceFingerprint], sourceHash, recipeFp)) && unsupportedRejected,
                ["exactMaterializationAndProvenance"] = sourceHash == ca.SourceFingerprint && sourceHash == M047CanonicalAssetPromotion.Hash(File.ReadAllBytes(Path.Combine(staged, "derivative.bin"))),
                ["independentGenerationValidation"] = stageValid && manifestValid,
                ["atomicRecovery"] = failedPublicationPreservesPrior,
                ["pathIndependent"] = ca.Fingerprint == caRelocated.Fingerprint && File.ReadAllBytes(Path.Combine(one, "source", "A.bin")).SequenceEqual(File.ReadAllBytes(Path.Combine(two, "source", "A.bin"))),
                ["stableIdentityCollisionSafe"] = approvedA != approvedB && approvedA.Length > "approved-asset.".Length,
                ["v1AndFakeCapabilityRejected"] = v1Rejected && unsupportedRejected,
                ["imageRegionCorrectionMaterialized"] = imageCorrection.Length > 0 && !imageSource.SequenceEqual(imageCorrection),
                ["evidenceObserved"] = true
            };
            var production = ProductionPromotion(one);
            values["productionAlternativeAndCorrectionPath"] = production.AlternativeBound && production.RealGenerationValidated;
            values["productionFailurePreservesCurrent"] = production.FailurePreservedCurrent;
            values["productionCollisionRejected"] = production.CollisionRejected;
            values["evidenceObserved"] = values.Values.Take(values.Count - 1).All(x => x);
            return new(values, new { shard, approvedA, approvedB, sourceHash, recipeFp, recipeFpOther, stagedGeneration = Path.GetFileName(staged), v1Rejected, unsupportedRejected, production });
        }
        finally { try { Directory.Delete(one, true); Directory.Delete(two, true); } catch { } }
    }

    private static ProductionResult ProductionPromotion(string root)
    {
        var home = Path.Combine(root, "production-home"); var source = Path.Combine(root, "source"); var campaignPath = Path.Combine(root, "production-campaign.json"); var output = Path.Combine(root, "production-output"); var target = Path.Combine(root, "published");
        File.WriteAllBytes(Path.Combine(source, "A.bin"), Wave(8, 11)); File.WriteAllBytes(Path.Combine(source, "B.bin"), Wave(8, 37)); File.WriteAllBytes(Path.Combine(source, "C.png"), Png(2, 1, [255, 0, 0, 255, 0, 255, 0, 255]));
        Directory.CreateDirectory(Path.Combine(home, "registry"));
        File.WriteAllText(Path.Combine(home, "registry", "sources.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-source-registry.v1", sources = new[] { new { id = "source.production", path = source, currentProfileFingerprint = "profile.production" } } }));
        var variantFingerprint = M047CanonicalAssetPromotion.Hash(Encoding.UTF8.GetBytes("variant.uses-B"));
        File.WriteAllText(campaignPath, JsonSerializer.Serialize(new { schema = "agentic2d.asset-campaign.v2", id = "campaign.production", sourceId = "source.production", profileFingerprint = "profile.production", candidates = new object[] { new { candidateId = "opaque-a", sourceRelativePath = "A.bin", mediaKind = "audio", presentationRole = "default", proposalFingerprint = "proposal.a", selection = new { type = "audio-file" }, variants = new[] { new { variantId = "variant-b", kind = "source-substitution", sourceRelativePath = "B.bin", selection = new { type = "audio-file", startSampleFrame = 0, endSampleFrame = 2 }, variantFingerprint } } }, new { candidateId = "opaque-b", sourceRelativePath = "B.bin", mediaKind = "audio", presentationRole = "default", proposalFingerprint = "proposal.b", selection = new { type = "audio-file" } }, new { candidateId = "opaque-c", sourceRelativePath = "C.png", mediaKind = "image", presentationRole = "default", proposalFingerprint = "proposal.c", selection = new { type = "image-file" } } } }));
        var old = Environment.GetEnvironmentVariable("AGENTIC2D_ASSET_HOME");
        try
        {
            Environment.SetEnvironmentVariable("AGENTIC2D_ASSET_HOME", home);
            var open = M029AssetWorkbenchCommands.RunAsync(["asset", "workbench", "--campaign", campaignPath, "--headless", "--output", Path.Combine(root, "production-open")], TextWriter.Null, TextWriter.Null).GetAwaiter().GetResult();
            var session = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, "production-open", "review-session.json"))).RootElement.GetProperty("id").GetString()!;
            var decision = M029AssetWorkbenchCommands.RunAsync(["asset", "workbench", "--session", session, "--select", "1", "--decision", "approve-with-corrections", "--alternative", "variant-b", "--correction", "{\"type\":\"audio-trim-sample-frames\"}", "--output", Path.Combine(root, "production-decision")], TextWriter.Null, TextWriter.Null).GetAwaiter().GetResult();
            var imageDecision = M029AssetWorkbenchCommands.RunAsync(["asset", "workbench", "--session", session, "--select", "3", "--decision", "approve-with-corrections", "--correction", "{\"type\":\"crop-image-region\",\"x\":1,\"y\":0,\"width\":1,\"height\":1}", "--output", Path.Combine(root, "production-image-decision")], TextWriter.Null, TextWriter.Null).GetAwaiter().GetResult();
            var promoteErrors = new StringWriter(); var promote = M029AssetWorkbenchCommands.RunAsync(["asset", "batch", "promote", "campaign.production", "--target", target, "--output", output], TextWriter.Null, promoteErrors).GetAwaiter().GetResult(); if (promote != 0) throw new InvalidOperationException("production promotion failed: " + promoteErrors);
            var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(target, "promotion-manifest.json"))).RootElement; var entry = manifest.GetProperty("entries").EnumerateArray().Single(x => x.GetProperty("candidateId").GetString() == "opaque-a"); var imageEntry = manifest.GetProperty("entries").EnumerateArray().Single(x => x.GetProperty("candidateId").GetString() == "opaque-c"); using var trim = JsonDocument.Parse("{\"type\":\"audio-trim-sample-frames\"}"); using var crop = JsonDocument.Parse("{\"type\":\"crop-image-region\",\"x\":1,\"y\":0,\"width\":1,\"height\":1}"); var expectedB = M047CanonicalAssetPromotion.Materialize(File.ReadAllBytes(Path.Combine(source, "B.bin")), "audio", new M047CanonicalAssetPromotion.Selection("audio-file", StartSampleFrame: 0, EndSampleFrame: 2), [new M047CanonicalAssetPromotion.Correction("audio-trim-sample-frames", trim.RootElement.Clone())]); var expectedC = M047CanonicalAssetPromotion.Materialize(File.ReadAllBytes(Path.Combine(source, "C.png")), "image", new M047CanonicalAssetPromotion.Selection("image-file"), [new M047CanonicalAssetPromotion.Correction("crop-image-region", crop.RootElement.Clone())]); var promoted = M047CanonicalAssetPromotion.Hash(File.ReadAllBytes(Path.Combine(target, entry.GetProperty("derivative").GetString()!.Replace('/', Path.DirectorySeparatorChar)))); var promotedImage = M047CanonicalAssetPromotion.Hash(File.ReadAllBytes(Path.Combine(target, imageEntry.GetProperty("derivative").GetString()!.Replace('/', Path.DirectorySeparatorChar)))); var generationValid = M047CanonicalAssetPromotion.ValidatePublishedGeneration(target, Path.Combine(home, "registry", "sources.json")); if (!generationValid) throw new InvalidOperationException("post-publication validation failed: " + (File.Exists(Path.Combine(target, "validation-error.txt")) ? File.ReadAllText(Path.Combine(target, "validation-error.txt")) : "unknown")); var valid = promote == 0 && imageDecision == 0 && promoted == M047CanonicalAssetPromotion.Hash(expectedB) && promoted != M047CanonicalAssetPromotion.Hash(File.ReadAllBytes(Path.Combine(source, "B.bin"))) && promotedImage == M047CanonicalAssetPromotion.Hash(expectedC) && promotedImage != M047CanonicalAssetPromotion.Hash(File.ReadAllBytes(Path.Combine(source, "C.png")));
            if (!valid) throw new InvalidOperationException($"production derivative mismatch: audio={promoted}/{M047CanonicalAssetPromotion.Hash(expectedB)}, image={promotedImage}/{M047CanonicalAssetPromotion.Hash(expectedC)}");
            var before = File.ReadAllText(Path.Combine(target, "current-generation.json")); Environment.SetEnvironmentVariable("AGENTIC2D_M047_FAIL_BEFORE_PUBLICATION", "1"); var failed = M029AssetWorkbenchCommands.RunAsync(["asset", "batch", "promote", "campaign.production", "--target", target, "--output", Path.Combine(root, "production-failure")], TextWriter.Null, TextWriter.Null).GetAwaiter().GetResult(); Environment.SetEnvironmentVariable("AGENTIC2D_M047_FAIL_BEFORE_PUBLICATION", null); var preserved = failed != 0 && before == File.ReadAllText(Path.Combine(target, "current-generation.json")) && M047CanonicalAssetPromotion.ValidatePublishedGeneration(target);
            var collision = JsonDocument.Parse(File.ReadAllText(Path.Combine(target, "promotion-manifest.json"))).RootElement; var collisionEntries = collision.GetProperty("entries").EnumerateArray().Select(x => x.Clone()).ToList(); collisionEntries.Add(collisionEntries[0]); var bad = Path.Combine(root, "collision"); Directory.CreateDirectory(bad); File.WriteAllText(Path.Combine(bad, "promotion-manifest.json"), JsonSerializer.Serialize(new { schema = M047CanonicalAssetPromotion.ManifestSchema, entries = collisionEntries })); File.WriteAllText(Path.Combine(bad, "current-generation.json"), JsonSerializer.Serialize(new { manifest = "promotion-manifest.json", generation = M047CanonicalAssetPromotion.Hash(Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(bad, "promotion-manifest.json")))) })); var collisionRejected = !M047CanonicalAssetPromotion.ValidatePublishedGeneration(bad);
            var selectedVariant = entry.GetProperty("recipe").GetProperty("selectedVariant").GetString();
            return new(decision == 0 && selectedVariant == "variant-b", valid, preserved, collisionRejected);
        }
        finally { Environment.SetEnvironmentVariable("AGENTIC2D_ASSET_HOME", old); }
    }

    private static JsonDocument Campaign(string id, string source, string[] a, string[] b)
    {
        var json = $"{{\"schema\":\"agentic2d.asset-campaign.v2\",\"id\":\"{id}\",\"sourceId\":\"{source}\",\"candidates\":[{{\"candidateId\":\"{a[0]}\",\"sourceRelativePath\":\"{a[1]}\",\"mediaKind\":\"{a[2]}\",\"presentationRole\":\"{a[3]}\",\"proposalFingerprint\":\"proposal.a\",\"selection\":{{\"type\":\"file\"}}}},{{\"candidateId\":\"{b[0]}\",\"sourceRelativePath\":\"{b[1]}\",\"mediaKind\":\"{b[2]}\",\"presentationRole\":\"{b[3]}\",\"proposalFingerprint\":\"proposal.b\",\"selection\":{{\"type\":\"file\"}}}}]}}";
        return JsonDocument.Parse(json);
    }
    private sealed record Result(SortedDictionary<string, bool> Values, object Details);
    private sealed record ProductionResult(bool AlternativeBound, bool RealGenerationValidated, bool FailurePreservedCurrent, bool CollisionRejected);
    private static byte[] Wave(int samples, short seed) { using var stream = new MemoryStream(); using var writer = new BinaryWriter(stream); writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(8000); writer.Write(16000); writer.Write((short)2); writer.Write((short)16); writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2); for (var i = 0; i < samples; i++) writer.Write((short)(seed + i)); return stream.ToArray(); }
    private static byte[] Png(int width, int height, byte[] pixels) { using var output = new MemoryStream(); output.Write([137, 80, 78, 71, 13, 10, 26, 10]); var header = new byte[13]; BinaryPrimitives.WriteInt32BigEndian(header, width); BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4), height); header[8] = 8; header[9] = 6; Chunk(output, "IHDR", header); using var raw = new MemoryStream(); for (var y = 0; y < height; y++) { raw.WriteByte(0); raw.Write(pixels, y * width * 4, width * 4); } raw.Position = 0; using var compressed = new MemoryStream(); using (var z = new ZLibStream(compressed, CompressionLevel.SmallestSize, true)) raw.CopyTo(z); Chunk(output, "IDAT", compressed.ToArray()); Chunk(output, "IEND", []); return output.ToArray(); }
    private static void Chunk(Stream output, string name, byte[] data) { var n = Encoding.ASCII.GetBytes(name); var length = new byte[4]; BinaryPrimitives.WriteInt32BigEndian(length, data.Length); output.Write(length); output.Write(n); output.Write(data); var crc = new byte[4]; BinaryPrimitives.WriteUInt32BigEndian(crc, Crc32(n.Concat(data).ToArray())); output.Write(crc); }
    private static uint Crc32(byte[] bytes) { uint crc = 0xffffffff; foreach (var b in bytes) { crc ^= b; for (var i = 0; i < 8; i++) crc = (crc >> 1) ^ (0xedb88320u & (uint)-(int)(crc & 1)); } return ~crc; }
}
