using System.Text.Json;
using Agentic2D.Tools;
using Agentic2D.Validation;

namespace Agentic2D.Tests.Unit;

public sealed class ContentValidationTests
{
    private const string SmokeScenarioPath = "game/scenarios/smoke/runtime-smoke.json";
    private const string SmokeAssetPath = "game/assets/metadata/tile-atlas-smoke.asset.json";
    private const string SmokeMapPath = "game/maps/smoke/map-smoke.map.json";

    [Test]
    public async Task ScenariosScopeValidatesAuthoredSmokeScenario()
    {
        var result = new ContentValidator().Validate("scenarios");

        await Assert.That(result.Result.Status).IsEqualTo("passed");
        await Assert.That(result.Result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Result.Summary.ItemsValidated).IsEqualTo(3);
        await Assert.That(result.Result.Summary.Errors).IsEqualTo(0);
        await Assert.That(result.ValidatedItemsDocument.Items.Select(static item => item.Id)).Contains("runtime.smoke");
        await Assert.That(result.ValidatedItemsDocument.Items.Select(static item => item.Id)).Contains("behavior.grid-movement-smoke");
        await Assert.That(result.ValidatedItemsDocument.Items.Select(static item => item.Id)).Contains("behavior.grid-movement-rejected-smoke");
    }

    [Test]
    public async Task SingleScenarioPathValidatesAuthoredSmokeScenario()
    {
        var result = new ContentValidator().Validate(SmokeScenarioPath);

        await Assert.That(result.Result.Status).IsEqualTo("passed");
        await Assert.That(result.Result.ExitCode).IsEqualTo(0);
        await Assert.That(result.ValidatedItemsDocument.Items.Single().Status).IsEqualTo("passed");
    }

    [Test]
    public async Task ScenarioContentValidationCatchesMissingRequiredField()
    {
        var path = await WriteScenarioAsync(RemoveProperty(ValidScenario(), "id"));

        var result = new ContentValidator().Validate(path);

        await Assert.That(result.Result.Status).IsEqualTo("failed");
        await Assert.That(result.Result.ExitCode).IsEqualTo(1);
        await Assert.That(result.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("CONTENT0001");
    }

    [Test]
    public async Task ScenarioContentValidationCatchesInvalidSchemaValue()
    {
        var scenario = ValidScenario();
        scenario["schema"] = "agentic2d.scenario.v2";
        var path = await WriteScenarioAsync(scenario);

        var result = new ContentValidator().Validate(path);

        await Assert.That(result.Result.Status).IsEqualTo("failed");
        await Assert.That(result.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("CONTENT0002");
    }

    [Test]
    public async Task ScenarioContentValidationCatchesDuplicateEntityIds()
    {
        var scenario = ValidScenario();
        scenario["initialState"] = new Dictionary<string, object?>
        {
            ["entities"] = new object[]
            {
                new Dictionary<string, object?> { ["id"] = "entity.player", ["position"] = 0 },
                new Dictionary<string, object?> { ["id"] = "entity.player", ["position"] = 1 },
            },
        };
        var path = await WriteScenarioAsync(scenario);

        var result = new ContentValidator().Validate(path);

        await Assert.That(result.Result.Status).IsEqualTo("failed");
        await Assert.That(result.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("CONTENT0004");
    }

    [Test]
    public async Task ScenarioContentValidationCatchesUnsupportedCommandType()
    {
        var scenario = ValidScenario();
        scenario["steps"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = "step.move-player",
                ["command"] = new Dictionary<string, object?> { ["type"] = "teleport", ["entityId"] = "entity.player", ["amount"] = 1 },
            },
        };
        var path = await WriteScenarioAsync(scenario);

        var result = new ContentValidator().Validate(path);

        await Assert.That(result.Result.Status).IsEqualTo("failed");
        await Assert.That(result.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("CONTENT0006");
    }

    [Test]
    public async Task ScenarioContentValidationCatchesMissingEntityReferences()
    {
        var scenario = ValidScenario();
        scenario["steps"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = "step.move-player",
                ["command"] = new Dictionary<string, object?> { ["type"] = "move", ["entityId"] = "entity.missing", ["amount"] = 1 },
            },
        };
        scenario["assertions"] = new object[]
        {
            new Dictionary<string, object?> { ["id"] = "assert.playerPosition", ["type"] = "entityPositionEquals", ["entityId"] = "entity.missing", ["position"] = 1 },
        };
        var path = await WriteScenarioAsync(scenario);

        var result = new ContentValidator().Validate(path);

        await Assert.That(result.Result.Status).IsEqualTo("failed");
        await Assert.That(result.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("CONTENT0005");
    }

    [Test]
    public async Task ContentValidationArtifactsAreWrittenWithContractShapeForPassingValidation()
    {
        var outputDirectory = CreateTempDirectory();
        var result = new ContentValidator().Validate(SmokeScenarioPath);

        var exitCode = await ContentValidationArtifactWriter.WriteAsync(outputDirectory, result);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "result.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "validated-items.json"))).IsTrue();

        using var resultDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "result.json")));
        await Assert.That(resultDocument.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.content-validation.result.v1");
        await Assert.That(resultDocument.RootElement.GetProperty("command").GetString()).IsEqualTo("content validate");
        await Assert.That(resultDocument.RootElement.GetProperty("artifacts")[0].GetProperty("path").GetString()).IsEqualTo("diagnostics.json");

        using var diagnosticsDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "diagnostics.json")));
        await Assert.That(diagnosticsDocument.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.content-validation.diagnostics.v1");

        using var itemsDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "validated-items.json")));
        await Assert.That(itemsDocument.RootElement.GetProperty("items")[0].GetProperty("id").GetString()).IsEqualTo("runtime.smoke");
    }

    [Test]
    public async Task ContentValidationArtifactsAreWrittenWithContractShapeForFailingValidation()
    {
        var path = await WriteScenarioAsync(RemoveProperty(ValidScenario(), "id"));
        var outputDirectory = CreateTempDirectory();
        var result = new ContentValidator().Validate(path);

        var exitCode = await ContentValidationArtifactWriter.WriteAsync(outputDirectory, result);

        await Assert.That(exitCode).IsEqualTo(1);
        using var resultDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "result.json")));
        await Assert.That(resultDocument.RootElement.GetProperty("status").GetString()).IsEqualTo("failed");
        await Assert.That(resultDocument.RootElement.GetProperty("diagnostics")[0].GetProperty("id").GetString()).IsEqualTo("CONTENT0001");
    }

    [Test]
    public async Task ContentValidateCommandWritesArtifactsAndReturnsSuccess()
    {
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["content", "validate", "scenarios", "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "result.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "validated-items.json"))).IsTrue();
        await Assert.That(stderr.ToString()).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task ContentValidateCommandReturnsUsageExitCodeWhenOutputIsMissing()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["content", "validate", "scenarios"], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(stdout.ToString()).IsEqualTo(string.Empty);
        await Assert.That(stderr.ToString()).Contains("missing required --output");
    }

    [Test]
    public async Task ContentValidateCommandWritesDiagnosticsForUnsupportedScope()
    {
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["content", "validate", "widgets", "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();

        using var diagnosticsDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "diagnostics.json")));
        await Assert.That(diagnosticsDocument.RootElement.GetProperty("diagnostics")[0].GetProperty("id").GetString()).IsEqualTo("CONTENT0010");
    }

    [Test]
    public async Task MapsScopeValidatesAuthoredSmokeMap()
    {
        var result = new ContentValidator().Validate("maps");

        await Assert.That(result.Result.Status).IsEqualTo("passed");
        await Assert.That(result.Result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Result.Summary.MapsValidated).IsEqualTo(1);
        await Assert.That(result.ValidatedItemsDocument.Items.Single().Id).IsEqualTo("map.smoke");
        await Assert.That(result.ValidatedItemsDocument.Items.Single().Kind).IsEqualTo("map");
        await Assert.That(result.ValidatedItemsDocument.Items.Single().Path).IsEqualTo(SmokeMapPath);
    }

    [Test]
    public async Task SingleMapPathValidatesAuthoredSmokeMap()
    {
        var result = new ContentValidator().Validate(SmokeMapPath);

        await Assert.That(result.Result.Status).IsEqualTo("passed");
        await Assert.That(result.Result.ExitCode).IsEqualTo(0);
        await Assert.That(result.ValidatedItemsDocument.Items.Single().Status).IsEqualTo("passed");
    }

    [Test]
    public async Task AssetsScopeValidatesAuthoredSmokeAssetMetadata()
    {
        var result = new ContentValidator().Validate("assets");

        await Assert.That(result.Result.Status).IsEqualTo("passed");
        await Assert.That(result.Result.ExitCode).IsEqualTo(0);
        await Assert.That(result.ValidatedItemsDocument.Items.Single().Id).IsEqualTo("asset.tile-atlas-smoke");
        await Assert.That(result.ValidatedItemsDocument.Items.Single().Kind).IsEqualTo("asset");
        await Assert.That(result.ValidatedItemsDocument.Items.Single().Path).IsEqualTo(SmokeAssetPath);
    }

    [Test]
    public async Task SingleAssetMetadataPathValidatesAuthoredSmokeAssetMetadata()
    {
        var result = new ContentValidator().Validate(SmokeAssetPath);

        await Assert.That(result.Result.Status).IsEqualTo("passed");
        await Assert.That(result.Result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Result.Summary.AssetsValidated).IsEqualTo(1);
    }

    [Test]
    public async Task AssetMetadataValidationCatchesMissingRawSource()
    {
        var asset = ValidAsset();
        asset["source"] = new Dictionary<string, object?>
        {
            ["path"] = "game/assets/raw/samples/missing.png",
            ["mediaType"] = "image/png",
        };
        var path = await WriteAssetAsync(asset);

        var result = new ContentValidator().Validate(path);

        await Assert.That(result.Result.Status).IsEqualTo("failed");
        await Assert.That(result.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("ASSET0002");
    }

    [Test]
    public async Task AssetMetadataValidationCatchesInvalidTileGrid()
    {
        var asset = ValidAsset();
        asset["tileAtlas"] = new Dictionary<string, object?>
        {
            ["tileWidth"] = 8,
            ["tileHeight"] = 8,
            ["columns"] = 0,
            ["rows"] = 2,
        };
        var path = await WriteAssetAsync(asset);

        var result = new ContentValidator().Validate(path);

        await Assert.That(result.Result.Status).IsEqualTo("failed");
        await Assert.That(result.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("ASSET0003");
    }

    [Test]
    public async Task AssetMetadataValidationCatchesDuplicateTileId()
    {
        var asset = ValidAsset();
        asset["tiles"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = "tile.smoke.duplicate",
                ["x"] = 0,
                ["y"] = 0,
                ["visualLabelsProposed"] = new[] { "grass" },
                ["physicalBehaviorsApproved"] = Array.Empty<string>(),
            },
            new Dictionary<string, object?>
            {
                ["id"] = "tile.smoke.duplicate",
                ["x"] = 1,
                ["y"] = 0,
                ["visualLabelsProposed"] = new[] { "stone" },
                ["physicalBehaviorsApproved"] = Array.Empty<string>(),
            },
        };
        var path = await WriteAssetAsync(asset);

        var result = new ContentValidator().Validate(path);

        await Assert.That(result.Result.Status).IsEqualTo("failed");
        await Assert.That(result.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("ASSET0004");
    }

    [Test]
    public async Task AssetMetadataValidationCatchesApprovedSemanticsWithoutReviewEvidence()
    {
        var asset = ValidAsset();
        asset["tiles"] = new object[]
        {
            new Dictionary<string, object?>
            {
                ["id"] = "tile.smoke.grass",
                ["x"] = 0,
                ["y"] = 0,
                ["visualLabelsProposed"] = new[] { "grass" },
                ["physicalBehaviorsApproved"] = new[] { "walkable" },
            },
        };
        var path = await WriteAssetAsync(asset);

        var result = new ContentValidator().Validate(path);

        await Assert.That(result.Result.Status).IsEqualTo("failed");
        await Assert.That(result.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("ASSET0005");
    }

    [Test]
    public async Task AssetInspectCommandWritesDeterministicArtifactsForAssetId()
    {
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["asset", "inspect", "asset.tile-atlas-smoke", "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr.ToString()).IsEqualTo(string.Empty);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "result.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "asset-summary.json"))).IsTrue();
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "tiles.json"))).IsTrue();

        using var resultDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "result.json")));
        await Assert.That(resultDocument.RootElement.GetProperty("schema").GetString()).IsEqualTo("agentic2d.asset-inspection.result.v1");
        await Assert.That(resultDocument.RootElement.GetProperty("target").GetString()).IsEqualTo("asset.tile-atlas-smoke");
        await Assert.That(resultDocument.RootElement.GetProperty("summary").GetProperty("tilesDeclared").GetInt32()).IsEqualTo(4);

        using var summaryDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "asset-summary.json")));
        await Assert.That(summaryDocument.RootElement.GetProperty("asset").GetProperty("id").GetString()).IsEqualTo("asset.tile-atlas-smoke");
        await Assert.That(summaryDocument.RootElement.GetProperty("image").GetProperty("width").GetInt32()).IsEqualTo(16);
        await Assert.That(summaryDocument.RootElement.GetProperty("image").GetProperty("height").GetInt32()).IsEqualTo(16);
        await Assert.That(summaryDocument.RootElement.GetProperty("semantics").GetProperty("reviewRequiredForApprovedPhysicalBehaviors").GetBoolean()).IsTrue();

        using var tilesDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "tiles.json")));
        await Assert.That(tilesDocument.RootElement.GetProperty("tiles").GetArrayLength()).IsEqualTo(4);
        await Assert.That(tilesDocument.RootElement.GetProperty("tiles")[0].GetProperty("reviewStatus").GetString()).IsEqualTo("not-required-for-proposals");
    }

    [Test]
    public async Task AssetInspectCommandSupportsRepositoryRelativeMetadataPath()
    {
        var outputDirectory = CreateTempDirectory();
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["asset", "inspect", SmokeAssetPath, "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(0);
        await Assert.That(stderr.ToString()).IsEqualTo(string.Empty);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "result.json"))).IsTrue();
    }

    [Test]
    public async Task AssetInspectCommandReturnsUsageExitCodeWhenOutputIsMissing()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var exitCode = await ToolsCli.RunAsync(["asset", "inspect", "asset.tile-atlas-smoke"], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(stdout.ToString()).IsEqualTo(string.Empty);
        await Assert.That(stderr.ToString()).Contains("missing required --output");
    }

    [Test]
    public async Task AssetInspectionCatchesPngGridMismatch()
    {
        var asset = ValidAsset();
        asset["tileAtlas"] = new Dictionary<string, object?>
        {
            ["tileWidth"] = 7,
            ["tileHeight"] = 8,
            ["columns"] = 2,
            ["rows"] = 2,
        };
        var path = await WriteAssetAsync(asset);

        var result = new AssetInspector().Inspect(path);

        await Assert.That(result.Result.Status).IsEqualTo("failed");
        await Assert.That(result.Result.ExitCode).IsEqualTo(1);
        await Assert.That(result.Result.Diagnostics.Select(static diagnostic => diagnostic.Id)).Contains("ASSET0003");
    }

    private static Dictionary<string, object?> ValidScenario()
    {
        return new Dictionary<string, object?>
        {
            ["schema"] = "agentic2d.scenario.v1",
            ["id"] = "runtime.smoke",
            ["category"] = "smoke",
            ["title"] = "Runtime smoke",
            ["purpose"] = "Validate deterministic runtime execution through the scenario runner.",
            ["seedPolicy"] = "none",
            ["runtime"] = new Dictionary<string, object?> { ["ticks"] = 3 },
            ["initialState"] = new Dictionary<string, object?>
            {
                ["entities"] = new object[]
                {
                    new Dictionary<string, object?> { ["id"] = "entity.player", ["position"] = 0 },
                },
            },
            ["steps"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["id"] = "step.move-player",
                    ["command"] = new Dictionary<string, object?> { ["type"] = "move", ["entityId"] = "entity.player", ["amount"] = 1 },
                },
            },
            ["expectedEvents"] = new[] { "runtime.started", "entity.created", "command.accepted", "entity.moved", "runtime.completed" },
            ["assertions"] = new object[]
            {
                new Dictionary<string, object?> { ["id"] = "assert.finalTick", ["type"] = "finalTickEqualsRequested" },
                new Dictionary<string, object?> { ["id"] = "assert.playerPosition", ["type"] = "entityPositionEquals", ["entityId"] = "entity.player", ["position"] = 1 },
                new Dictionary<string, object?> { ["id"] = "assert.runtimeStartedEvent", ["type"] = "eventOccurred", ["eventType"] = "runtime.started" },
            },
            ["artifacts"] = new Dictionary<string, object?> { ["result"] = "result.json", ["events"] = "events.jsonl", ["diagnostics"] = "diagnostics.json" },
            ["humanReview"] = new Dictionary<string, object?> { ["required"] = false },
        };
    }

    private static Dictionary<string, object?> ValidAsset()
    {
        return new Dictionary<string, object?>
        {
            ["schema"] = "agentic2d.asset-metadata.v1",
            ["id"] = "asset.tile-atlas-smoke",
            ["kind"] = "tile-atlas",
            ["title"] = "Tile atlas smoke asset",
            ["purpose"] = "Validate structural asset metadata and tile atlas inspection.",
            ["source"] = new Dictionary<string, object?>
            {
                ["path"] = "game/assets/raw/samples/tile-atlas-smoke.png",
                ["mediaType"] = "image/png",
            },
            ["tileAtlas"] = new Dictionary<string, object?>
            {
                ["tileWidth"] = 8,
                ["tileHeight"] = 8,
                ["columns"] = 2,
                ["rows"] = 2,
            },
            ["tiles"] = new object[]
            {
                new Dictionary<string, object?>
                {
                    ["id"] = "tile.smoke.0",
                    ["x"] = 0,
                    ["y"] = 0,
                    ["visualLabelsProposed"] = new[] { "grass" },
                    ["physicalBehaviorsApproved"] = Array.Empty<string>(),
                },
            },
            ["provenance"] = new Dictionary<string, object?>
            {
                ["sourceKind"] = "repository-fixture",
                ["createdBy"] = "milestone-007",
                ["notes"] = "Synthetic fixture for structural validation only.",
            },
            ["semantics"] = new Dictionary<string, object?>
            {
                ["visualLabelsProposed"] = new[] { "grass" },
                ["physicalBehaviorsApproved"] = Array.Empty<string>(),
            },
            ["humanReview"] = new Dictionary<string, object?>
            {
                ["requiredForApprovedPhysicalBehaviors"] = true,
                ["approvals"] = Array.Empty<object>(),
            },
        };
    }

    private static Dictionary<string, object?> RemoveProperty(Dictionary<string, object?> source, string propertyName)
    {
        source.Remove(propertyName);
        return source;
    }

    private static async Task<string> WriteScenarioAsync(Dictionary<string, object?> scenario)
    {
        var root = Path.Combine(Path.GetTempPath(), "agentic2d-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "scenario.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(scenario, ContentValidationJson.Options));
        return path;
    }

    private static async Task<string> WriteAssetAsync(Dictionary<string, object?> asset)
    {
        var root = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "artifacts", "tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "asset.asset.json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(asset, ContentValidationJson.Options));
        return ContentTargetResolver.ToRepositoryRelativePath(path);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "agentic2d-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
