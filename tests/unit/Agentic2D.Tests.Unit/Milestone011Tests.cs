using System.Text.Json;
using System.Text.Json.Nodes;
using Agentic2D.ScenarioRunner;
using Agentic2D.Tools;
using Agentic2D.Validation;

namespace Agentic2D.Tests.Unit;

public sealed class Milestone011Tests
{
    [Test]
    public async Task AssetPerceptionProducesDeterministicFeaturesAndProposals()
    {
        var perceiver = new AssetPerceiver();

        var first = perceiver.Perceive("asset.tile-atlas-smoke");
        var second = perceiver.Perceive("asset.tile-atlas-smoke");

        await Assert.That(first.Result.Status).IsEqualTo("passed");
        await Assert.That(first.TileFeatures.Tiles.Count).IsEqualTo(4);
        await Assert.That(first.TileFeatures.Tiles.All(static tile => tile.FeatureFingerprint.StartsWith("sha256:", StringComparison.Ordinal))).IsTrue();
        await Assert.That(JsonSerializer.Serialize(first.TileFeatures, ContentValidationJson.Options))
            .IsEqualTo(JsonSerializer.Serialize(second.TileFeatures, ContentValidationJson.Options));
        await Assert.That(first.SemanticProposals.Proposals.All(static proposal => proposal.State == "proposed")).IsTrue();
    }

    [Test]
    public async Task AssetReviewApplyDryRunKeepsSourceUnchangedAndRealApplyPreservesUnrelatedMetadata()
    {
        var sourceMetadataPath = RepositoryPath("game/assets/metadata/tile-atlas-smoke.asset.json");
        var before = await File.ReadAllTextAsync(sourceMetadataPath);

        var dryRun = new AssetReviewApplier().Apply("game/assets/reviews/tile-atlas-smoke.review.json", dryRun: true);

        await Assert.That(dryRun.Result.Status).IsEqualTo("passed");
        await Assert.That(dryRun.ProposedMetadata).IsNotNull();
        await Assert.That(await File.ReadAllTextAsync(sourceMetadataPath)).IsEqualTo(before);

        var workspace = CreateArtifactWorkspace();
        var metadataPath = Path.Combine(workspace, "tile-atlas-smoke.asset.json");
        var reviewPath = Path.Combine(workspace, "tile-atlas-smoke.review.json");
        await File.WriteAllTextAsync(metadataPath, before);
        await File.WriteAllTextAsync(reviewPath, await File.ReadAllTextAsync(RepositoryPath("game/assets/reviews/tile-atlas-smoke.review.json")));

        await AddUnrelatedMetadataAsync(metadataPath);
        await RepointReviewFixtureAsync(reviewPath, ContentTargetResolver.ToRepositoryRelativePath(metadataPath));

        var applied = new AssetReviewApplier().Apply(ContentTargetResolver.ToRepositoryRelativePath(reviewPath), dryRun: false);

        await Assert.That(applied.Result.Status).IsEqualTo("passed");
        var appliedJson = JsonNode.Parse(await File.ReadAllTextAsync(metadataPath))!.AsObject();
        await Assert.That(appliedJson["unrelatedMetadata"]!["preserveMe"]!.GetValue<string>()).IsEqualTo("yes");
        await Assert.That(appliedJson["tiles"]![0]!["physicalBehaviorsApproved"]!.AsArray().Select(static value => value!.GetValue<string>())).Contains("walkable");
    }

    [Test]
    public async Task AssetReviewApplyRejectsStaleFingerprintWithoutMutation()
    {
        var workspace = CreateArtifactWorkspace();
        var metadataPath = Path.Combine(workspace, "tile-atlas-smoke.asset.json");
        var reviewPath = Path.Combine(workspace, "tile-atlas-smoke.review.json");
        await File.WriteAllTextAsync(metadataPath, await File.ReadAllTextAsync(RepositoryPath("game/assets/metadata/tile-atlas-smoke.asset.json")));
        await File.WriteAllTextAsync(reviewPath, await File.ReadAllTextAsync(RepositoryPath("game/assets/reviews/tile-atlas-smoke.review.json")));

        await RepointReviewFixtureAsync(reviewPath, ContentTargetResolver.ToRepositoryRelativePath(metadataPath), staleFingerprint: true);
        var before = await File.ReadAllTextAsync(metadataPath);

        var run = new AssetReviewApplier().Apply(ContentTargetResolver.ToRepositoryRelativePath(reviewPath), dryRun: false);

        await Assert.That(run.Result.Status).IsEqualTo("failed");
        await Assert.That(run.Result.ExitCode).IsEqualTo(1);
        await Assert.That(run.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("REVIEW0003");
        await Assert.That(await File.ReadAllTextAsync(metadataPath)).IsEqualTo(before);
    }

    [Test]
    public async Task MapInspectCommandWritesStructuredArtifacts()
    {
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["map", "inspect", "map.smoke", "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr.ToString()).IsEqualTo(string.Empty);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "map-summary.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "layers.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "resolved-references.json"))).IsTrue();

        using var summary = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "map-summary.json")));
        await Assert.That(summary.RootElement.GetProperty("map").GetProperty("id").GetString()).IsEqualTo("map.smoke");
    }

    [Test]
    public async Task RuntimeInspectCommandWritesStructuredStateEvidence()
    {
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["runtime", "inspect", "--scenario", "runtime.smoke", "--map", "map.smoke", "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr.ToString()).IsEqualTo(string.Empty);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "commands.jsonl"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "events.jsonl"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "content-references.json"))).IsTrue();

        using var result = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "result.json")));
        await Assert.That(result.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.runtime-inspection.result.v1");
        await Assert.That(result.RootElement.GetProperty("mapId").GetString()).IsEqualTo("map.smoke");
    }

    [Test]
    public async Task ReviewPackIncludesMilestone011ArtifactFamiliesAndWorkbenchLoadsPerceptionEvidence()
    {
        var artifactRoot = await CreateMilestone011ArtifactsAsync();

        var reviewPack = new ReviewPackGenerator().Generate(artifactRoot);

        await Assert.That(reviewPack.Manifest.ArtifactGroups.Select(static group => group.Kind)).Contains("asset-perception");
        await Assert.That(reviewPack.Manifest.ArtifactGroups.Select(static group => group.Kind)).Contains("asset-review-apply");
        await Assert.That(reviewPack.Manifest.ArtifactGroups.Select(static group => group.Kind)).Contains("map-inspection");
        await Assert.That(reviewPack.Manifest.ArtifactGroups.Select(static group => group.Kind)).Contains("runtime-inspection");

        var reviewPackDirectory = CreateTempDirectory();
        await ReviewPackArtifactWriter.WriteAsync(reviewPackDirectory, reviewPack);
        var workbench = new AssetCurationWorkbenchGenerator().Generate("asset.tile-atlas-smoke", reviewPackDirectory);

        await Assert.That(workbench.ReviewData.Status).IsEqualTo("passed");
        await Assert.That(workbench.ReviewData.Tiles.Any(static tile => tile.Perception is not null)).IsTrue();
        await Assert.That(workbench.ReviewData.Tiles.SelectMany(static tile => tile.ReviewQuestions).Select(static question => question.Id))
            .Contains("review.tile.smoke.0.perception-proposals");
    }

    private static async Task<string> CreateMilestone011ArtifactsAsync()
    {
        var root = CreateTempDirectory();

        await ScenarioArtifactWriter.WriteAsync(
            Path.Combine(root, "scenarios", "runtime-smoke"),
            new Agentic2D.ScenarioRunner.ScenarioRunner().Run("runtime.smoke"));
        await ContentValidationArtifactWriter.WriteAsync(
            Path.Combine(root, "content", "scenarios"),
            new ContentValidator().Validate("scenarios"));
        await ContentValidationArtifactWriter.WriteAsync(
            Path.Combine(root, "content", "assets"),
            new ContentValidator().Validate("assets"));
        await ContentValidationArtifactWriter.WriteAsync(
            Path.Combine(root, "content", "maps"),
            new ContentValidator().Validate("maps"));
        await AssetInspectionArtifactWriter.WriteAsync(
            Path.Combine(root, "assets", "tile-atlas-smoke"),
            new AssetInspector().Inspect("asset.tile-atlas-smoke"));
        await AssetPerceptionArtifactWriter.WriteAsync(
            Path.Combine(root, "assets", "perception", "tile-atlas-smoke"),
            new AssetPerceiver().Perceive("asset.tile-atlas-smoke"));
        await AssetReviewApplyArtifactWriter.WriteAsync(
            Path.Combine(root, "asset-review", "dry-run"),
            new AssetReviewApplier().Apply("game/assets/reviews/tile-atlas-smoke.review.json", dryRun: true));
        await MapInspectionArtifactWriter.WriteAsync(
            Path.Combine(root, "maps", "map-smoke"),
            new MapInspector().Inspect("map.smoke"));
        await RuntimeInspectionArtifactWriter.WriteAsync(
            Path.Combine(root, "runtime", "inspect"),
            new RuntimeInspector().Inspect("runtime.smoke", "map.smoke"));

        return root;
    }

    private static async Task AddUnrelatedMetadataAsync(string metadataPath)
    {
        var json = JsonNode.Parse(await File.ReadAllTextAsync(metadataPath))!.AsObject();
        json["unrelatedMetadata"] = new JsonObject
        {
            ["preserveMe"] = "yes",
        };
        await File.WriteAllTextAsync(metadataPath, json.ToJsonString(ContentValidationJson.Options));
    }

    private static async Task RepointReviewFixtureAsync(string reviewPath, string metadataPath, bool staleFingerprint = false)
    {
        var json = JsonNode.Parse(await File.ReadAllTextAsync(reviewPath))!.AsObject();
        json["metadataPath"] = metadataPath;
        json["expectedSourceFingerprint"] = staleFingerprint
            ? "sha256:0000000000000000000000000000000000000000000000000000000000000000"
            : AssetFingerprint.FromBytes(await File.ReadAllBytesAsync(Path.Combine(ContentTargetResolver.FindRepositoryRoot(), metadataPath)));
        await File.WriteAllTextAsync(reviewPath, json.ToJsonString(ContentValidationJson.Options));
    }

    private static string CreateArtifactWorkspace()
    {
        var path = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "artifacts", "tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "agentic2d-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static string RepositoryPath(string relativePath)
    {
        return Path.Combine(ContentTargetResolver.FindRepositoryRoot(), relativePath);
    }
}
