using System.Text.Json;
using System.Diagnostics;
using Agentic2D.ScenarioRunner;
using Agentic2D.Tools;
using Agentic2D.Validation;
using ScenarioRunnerEngine = Agentic2D.ScenarioRunner.ScenarioRunner;

namespace Agentic2D.Tests.Unit;

public sealed class ReviewPackAndCurationTests
{
    [Test]
    public async Task ReviewPackGenerationIncludesCurrentSmokeArtifactFamilies()
    {
        var artifactRoot = await CreateSmokeArtifactsAsync();

        var run = new ReviewPackGenerator().Generate(artifactRoot);

        await Assert.That(run.Manifest.Schema).IsEqualTo("agentic2d.review-pack.manifest.v1");
        await Assert.That(run.Manifest.Status).IsEqualTo("passed");
        await Assert.That(run.Manifest.ArtifactGroups.Select(static group => group.Kind)).Contains("scenario-runner");
        await Assert.That(run.Manifest.ArtifactGroups.Select(static group => group.Kind)).Contains("content-validation");
        await Assert.That(run.Manifest.ArtifactGroups.Select(static group => group.Kind)).Contains("asset-inspection");
        await Assert.That(run.Manifest.SourceItems.Select(static item => item.Id)).Contains("asset.tile-atlas-smoke");
        await Assert.That(run.Manifest.ReviewQuestions.Select(static question => question.Id)).Contains("review.asset.tile-atlas-smoke.semantic-proposals");
    }

    [Test]
    public async Task ReviewPackArtifactsUseContractShapeAndSummarySections()
    {
        var artifactRoot = await CreateSmokeArtifactsAsync();
        var outputDirectory = CreateTempDirectory();
        var run = new ReviewPackGenerator().Generate(artifactRoot);

        var exitCode = await ReviewPackArtifactWriter.WriteAsync(outputDirectory, run);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "review-summary.md"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "review-manifest.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();

        var summary = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "review-summary.md"));
        await Assert.That(summary).Contains("# Review Pack");
        await Assert.That(summary).Contains("## Included artifact groups");
        await Assert.That(summary).Contains("## Human review questions");

        using var manifest = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "review-manifest.json")));
        await Assert.That(manifest.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.review-pack.manifest.v1");
        await Assert.That(manifest.RootElement.GetProperty("summary").GetProperty("artifactGroupsIncluded").GetInt32()).IsEqualTo(4);
    }

    [Test]
    public async Task ReviewPackReportsMalformedKnownArtifactGroup()
    {
        var artifactRoot = CreateTempDirectory();
        var scenarioDirectory = Path.Combine(artifactRoot, "scenarios", "broken");
        Directory.CreateDirectory(scenarioDirectory);
        await File.WriteAllTextAsync(Path.Combine(scenarioDirectory, "result.json"), "{ not json");

        var run = new ReviewPackGenerator().Generate(artifactRoot);

        await Assert.That(run.Manifest.Status).IsEqualTo("failed");
        await Assert.That(run.Manifest.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("REVIEW0002");
    }

    [Test]
    public async Task AssetCurationWorkbenchGenerationPreservesReviewStateBoundaries()
    {
        var reviewPackDirectory = await CreateReviewPackAsync();

        var run = new AssetCurationWorkbenchGenerator().Generate("asset.tile-atlas-smoke", reviewPackDirectory);

        await Assert.That(run.ReviewData.Schema).IsEqualTo("agentic2d.asset-curation-workbench.review-data.v1");
        await Assert.That(run.ReviewData.Status).IsEqualTo("passed");
        await Assert.That(run.ReviewData.Asset.Id).IsEqualTo("asset.tile-atlas-smoke");
        var visualReviewStates = run.ReviewData.Tiles.SelectMany(static tile => tile.VisualLabels).Select(static label => label.ReviewState).Distinct().ToArray();
        await Assert.That(run.ReviewData.Tiles.Count).IsEqualTo(4);
        await Assert.That(visualReviewStates.Single()).IsEqualTo("proposed");
        await Assert.That(run.ReviewData.Tiles.SelectMany(static tile => tile.PhysicalBehaviors)).IsEmpty();
    }

    [Test]
    public async Task AssetCurationWorkbenchWritesStaticArtifacts()
    {
        var reviewPackDirectory = await CreateReviewPackAsync();
        var outputDirectory = CreateTempDirectory();
        var run = new AssetCurationWorkbenchGenerator().Generate("asset.tile-atlas-smoke", reviewPackDirectory);

        var exitCode = await AssetCurationWorkbenchArtifactWriter.WriteAsync(outputDirectory, run);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "index.html"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "review-data.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();

        using var data = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "review-data.json")));
        await Assert.That(data.RootElement.GetProperty("asset").GetProperty("id").GetString()).IsEqualTo("asset.tile-atlas-smoke");
        await Assert.That(data.RootElement.GetProperty("tiles")[0].GetProperty("visualLabels")[0].GetProperty("reviewState").GetString()).IsEqualTo("proposed");
    }

    [Test]
    public async Task ReviewPackCommandWritesArtifacts()
    {
        var artifactRoot = await CreateSmokeArtifactsAsync();
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["review", "pack", "--input", artifactRoot, "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr.ToString()).IsEqualTo(string.Empty);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "review-manifest.json"))).IsTrue();
    }

    [Test]
    public async Task AssetCurateCommandWritesArtifacts()
    {
        var reviewPackDirectory = await CreateReviewPackAsync();
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["asset", "curate", "--asset", "asset.tile-atlas-smoke", "--review-pack", reviewPackDirectory, "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr.ToString()).IsEqualTo(string.Empty);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "index.html"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "review-data.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();
    }

    [Test]
    public async Task ReviewPackSmokeWrapperFailsWhenProductCommandFails()
    {
        var exitCode = await RunWrapperWithFailingDotnetAsync("eng/review-pack-smoke.sh");

        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    [Test]
    public async Task AssetCurationSmokeWrapperFailsWhenProductCommandFails()
    {
        var exitCode = await RunWrapperWithFailingDotnetAsync("eng/asset-curation-smoke.sh");

        await Assert.That(exitCode).IsNotEqualTo(0);
    }

    private static async Task<string> CreateReviewPackAsync()
    {
        var artifactRoot = await CreateSmokeArtifactsAsync();
        var outputDirectory = CreateTempDirectory();
        var run = new ReviewPackGenerator().Generate(artifactRoot);
        await ReviewPackArtifactWriter.WriteAsync(outputDirectory, run);
        return outputDirectory;
    }

    private static async Task<string> CreateSmokeArtifactsAsync()
    {
        var root = CreateTempDirectory();
        await ScenarioArtifactWriter.WriteAsync(
            Path.Combine(root, "scenarios", "runtime-smoke"),
            new ScenarioRunnerEngine().Run("game/scenarios/smoke/runtime-smoke.json"));
        await ContentValidationArtifactWriter.WriteAsync(
            Path.Combine(root, "content", "scenarios"),
            new ContentValidator().Validate("scenarios"));
        await ContentValidationArtifactWriter.WriteAsync(
            Path.Combine(root, "content", "assets"),
            new ContentValidator().Validate("assets"));
        await AssetInspectionArtifactWriter.WriteAsync(
            Path.Combine(root, "assets", "tile-atlas-smoke"),
            new AssetInspector().Inspect("asset.tile-atlas-smoke"));
        return root;
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "agentic2d-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static async Task<int> RunWrapperWithFailingDotnetAsync(string scriptPath)
    {
        var binDirectory = CreateTempDirectory();
        var fakeDotnetPath = Path.Combine(binDirectory, "dotnet");
        await File.WriteAllTextAsync(
            fakeDotnetPath,
            """
            #!/usr/bin/env bash
            exit 42
            """);
        using (var chmod = Process.Start("chmod", $"+x {fakeDotnetPath}") ?? throw new InvalidOperationException("Could not chmod fake dotnet."))
        {
            await chmod.WaitForExitAsync();
        }

        var repoRoot = ContentTargetResolver.FindRepositoryRoot();
        var startInfo = new ProcessStartInfo
        {
            FileName = "bash",
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        startInfo.ArgumentList.Add(scriptPath);
        startInfo.Environment["PATH"] = $"{binDirectory}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}";

        using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Could not start wrapper process.");
        await process.WaitForExitAsync();
        return process.ExitCode;
    }
}
