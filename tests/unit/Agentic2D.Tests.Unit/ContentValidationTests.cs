using System.Text.Json;
using Agentic2D.Tools;
using Agentic2D.Validation;

namespace Agentic2D.Tests.Unit;

public sealed class ContentValidationTests
{
    private const string SmokeScenarioPath = "game/scenarios/smoke/runtime-smoke.json";

    [Test]
    public async Task ScenariosScopeValidatesAuthoredSmokeScenario()
    {
        var result = new ContentValidator().Validate("scenarios");

        await Assert.That(result.Result.Status).IsEqualTo("passed");
        await Assert.That(result.Result.ExitCode).IsEqualTo(0);
        await Assert.That(result.Result.Summary.ItemsValidated).IsEqualTo(1);
        await Assert.That(result.Result.Summary.Errors).IsEqualTo(0);
        await Assert.That(result.ValidatedItemsDocument.Items.Single().Id).IsEqualTo("runtime.smoke");
        await Assert.That(result.ValidatedItemsDocument.Items.Single().Path).IsEqualTo(SmokeScenarioPath);
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

        var exitCode = await ToolsCli.RunAsync(["content", "validate", "assets", "--output", outputDirectory], stdout, stderr);

        await Assert.That(exitCode).IsEqualTo(2);
        await Assert.That(File.Exists(Path.Combine(outputDirectory, "diagnostics.json"))).IsTrue();

        using var diagnosticsDocument = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(outputDirectory, "diagnostics.json")));
        await Assert.That(diagnosticsDocument.RootElement.GetProperty("diagnostics")[0].GetProperty("id").GetString()).IsEqualTo("CONTENT0010");
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

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "agentic2d-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
