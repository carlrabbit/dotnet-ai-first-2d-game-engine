using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.ScenarioRunner;
using Agentic2D.Validation;
using Agentic2D.Rendering;
using Agentic2D.Animation;
using ScenarioRunnerEngine = Agentic2D.ScenarioRunner.ScenarioRunner;
using Agentic2D.Workspaces;
using Agentic2D.Metrics;
using System.Text.Json;

namespace Agentic2D.Tools;

public static class ToolsCli
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        var export = await ExportCommands.RunAsync(args, output, error);
        if (export >= 0) return export;
        var m021 = await M021PresentationCommands.RunAsync(args, output, error);
        if (m021 >= 0) return m021;
        var m020 = await M020Commands.RunAsync(args, output, error);
        if (m020 >= 0) return m020;
        var m019Sound = await M019SoundCommands.RunAsync(args, output, error);
        if (m019Sound >= 0) return m019Sound;
        var m019Items = await M019ItemCommands.RunAsync(args, output, error);
        if (m019Items >= 0) return m019Items;
        var m019Gameplay = await M019GameplayCommands.RunAsync(args, output, error);
        if (m019Gameplay >= 0) return m019Gameplay;
        var m019Unified = await M019UnifiedCommands.RunAsync(args, output, error);
        if (m019Unified >= 0) return m019Unified;
        var m018 = await WorkspaceCommands.RunAsync(args, output, error);
        if (m018 >= 0) return m018;
        var m016 = await M016InputCommands.RunAsync(args, output, error);
        if (m016 >= 0) return m016;
        var m017 = await M017AnimationCommands.RunAsync(args, output, error);
        if (m017 >= 0) return m017;
        if (args is ["--help"] or ["-h"])
        {
            await output.WriteLineAsync(
                """
                agentic2d

                Usage:
                  agentic2d --help
                  agentic2d --version
                  agentic2d runtime smoke --output <directory>
                  agentic2d runtime smoke [--ticks <count>] [--metrics off|summary|per-tick] --output <directory>
                  agentic2d runtime inspect --scenario <scenario-id-or-path> [--map <map-id-or-path>] --output <directory>
                  agentic2d validate --output <directory>
                  agentic2d scenario run <scenario-id-or-path> --output <directory>
                  agentic2d content validate <scope-or-path> --output <directory>
                  agentic2d input inspect <sequence-id> --input-map <map-id> --output <directory>
                  agentic2d input replay --scenario <scenario-id> --recording <recording> --output <directory>
                  agentic2d animation inspect <animation-id-or-path> --output <directory>
                  agentic2d animation project --scenario <scenario-id> --output <directory>
                  agentic2d presentation inspect --project <project-or-workspace> --scenario <scenario-id> --output <directory>
                  agentic2d effect inspect <effect-id-or-path> --output <directory>
                  agentic2d camera inspect --project <project-or-workspace> --scenario <scenario-id> --output <directory>
                  agentic2d ui inspect <ui-id-or-path> --project <project-or-workspace> [--scenario <scenario-id>] --output <directory>
                  agentic2d sound inspect <sound-id-or-path> --output <directory>
                  agentic2d sound project --project <project-or-workspace> --scenario <scenario-id> --output <directory>
                  agentic2d gameplay inspect --project <project-or-workspace> --scenario <scenario-id> --output <directory>
                  agentic2d asset inspect <asset-id-or-path> --output <directory>
                  agentic2d asset perceive <asset-id-or-path> --output <directory>
                  agentic2d asset review apply --decisions <review-file> [--dry-run] --output <directory>
                  agentic2d map inspect <map-id-or-path> --output <directory>
                  agentic2d review pack --input <artifact-root> --output <directory>
                  agentic2d asset curate --asset <asset-id-or-path> --review-pack <review-pack-path> --output <directory>
                  agentic2d workspace create <target> --template minimal-game (--engine-directory <path> --engine-placement reference|copy | --engine-git <url-or-path> --engine-revision <revision>) --output <directory>
                  agentic2d save create --project <project-or-workspace> --run <run-directory> --tick <tick-or-final> --save-id <stable-id> --output <directory>
                  agentic2d save inspect <save-path> --output <directory>
                  agentic2d save validate <save-path> --project <project-or-workspace> --output <directory>
                  agentic2d project resume <project-or-workspace> --save <save-path> [--recording <semantic-input-recording>] --output <run-directory>
                  agentic2d content validate flags --output <directory>
                  agentic2d workspace validate <workspace> --output <directory>
                  agentic2d project validate <project-or-workspace> --output <directory>
                  agentic2d project run <project-or-workspace> --scenario <scenario-id> --output <run-directory>
                  agentic2d project export <project-or-workspace> [--target linux-x64] --output <directory>
                  agentic2d export inspect <export-directory> --output <directory>
                  agentic2d export validate <export-directory> --output <directory>
                  agentic2d run inspect <run-directory> --output <directory>
                  agentic2d run review <run-directory> --output <directory>

                Exit codes:
                  0  Command completed and validation passed.
                  1  Command completed and validation failed.
                  2  Invalid command-line usage or invalid content input/scope.
                  3  Runtime execution, artifact writing, or unhandled command failure.
                """);
            return 0;
        }

        if (args is ["--version"])
        {
            var version = typeof(ToolsCli).Assembly.GetName().Version?.ToString() ?? "0.0.0-dev";
            await output.WriteLineAsync($"agentic2d {version}");
            return 0;
        }

        var parseResult = ToolsCliParser.TryParse(args);

        if (!parseResult.IsSuccess)
        {
            await error.WriteLineAsync(parseResult.Error);
            return 2;
        }

        return await RunArtifactCommandAsync(parseResult.Command, output, error);
    }

    private static async Task<int> RunArtifactCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        if (command.Name == "render project")
        {
            return await RunRenderProjectCommandAsync(command, output, error);
        }

        if (command.Name == "scenario run")
        {
            return await RunScenarioCommandAsync(command, output, error);
        }

        if (command.Name == "runtime inspect")
        {
            return await RunRuntimeInspectCommandAsync(command, output, error);
        }

        if (command.Name == "content validate")
        {
            return await RunContentValidateCommandAsync(command, output, error);
        }

        if (command.Name == "asset inspect")
        {
            return await RunAssetInspectCommandAsync(command, output, error);
        }

        if (command.Name == "asset perceive")
        {
            return await RunAssetPerceiveCommandAsync(command, output, error);
        }

        if (command.Name == "asset review apply")
        {
            return await RunAssetReviewApplyCommandAsync(command, output, error);
        }

        if (command.Name == "map inspect")
        {
            return await RunMapInspectCommandAsync(command, output, error);
        }

        if (command.Name == "review pack")
        {
            return await RunReviewPackCommandAsync(command, output, error);
        }

        if (command.Name == "asset curate")
        {
            return await RunAssetCurateCommandAsync(command, output, error);
        }

        var outputPath = Path.Combine(command.OutputDirectory, "result.json");
        RuntimeResult result;

        try
        {
            result = RuntimeSmokeScenario.Run(command.Ticks, command.MetricsMode);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            return await WriteErrorResultAsync(command, outputPath, exception.Message, output, error);
        }

        var productResult = ProductCliResultJson.FromRuntimeResult(command.Name, result);

        try
        {
            Directory.CreateDirectory(command.OutputDirectory);
            await ProductCliResultJson.WriteAsync(outputPath, productResult);
            await WriteMetricsArtifactsAsync(command, command.OutputDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write result artifact: {exception.Message}");
            return 3;
        }

        await output.WriteLineAsync($"{command.Name}: {productResult.Status}; result: {outputPath}");
        return productResult.ExitCode;
    }

    internal static async Task WriteMetricsArtifactsAsync(CliCommand command, string outputDirectory)
    {
        if (command.MetricsMode == MetricsCollectionMode.Off) return;
        var snapshot = RuntimeSmokeScenario.RunWithMetrics(command.Ticks, command.MetricsMode);
        var options = new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var summary = new
        {
            schema = "agentic2d.runtime-metrics-summary.v1",
            mode = MetricsModeId(snapshot.Mode),
            tickCount = snapshot.TickCount,
            recentTickCapacity = snapshot.RecentCapacity,
            effectiveTicksPerSecond = snapshot.EffectiveTicksPerSecond,
            recentP95TickDurationMilliseconds = snapshot.RecentP95TickDurationMilliseconds,
            metrics = snapshot.Summary,
            limitations = new[] { "Timing is observational and is not a deterministic artifact fingerprint.", "The recent tick window is bounded; per-tick output contains at most its fixed capacity." },
        };
        await File.WriteAllTextAsync(Path.Combine(outputDirectory, "metrics-summary.json"), JsonSerializer.Serialize(summary, options));
        if (command.MetricsMode == MetricsCollectionMode.PerTick)
        {
            var lines = snapshot.RecentTicks.Select(tick => JsonSerializer.Serialize(new { schema = "agentic2d.runtime-metrics-tick.v1", tick = tick.Tick, values = tick.Values }));
            await File.WriteAllTextAsync(Path.Combine(outputDirectory, "metrics-ticks.jsonl"), string.Join(Environment.NewLine, lines) + (snapshot.RecentTicks.Count == 0 ? string.Empty : Environment.NewLine));
        }
    }

    private static string MetricsModeId(MetricsCollectionMode mode) => mode switch
    {
        MetricsCollectionMode.Off => "off",
        MetricsCollectionMode.Summary => "summary",
        MetricsCollectionMode.PerTick => "per-tick",
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static async Task<int> RunRenderProjectCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        try
        {
            var result = new RenderProjectionService().ProjectScenario(command.ScenarioReference ?? string.Empty);
            await RenderArtifactWriter.WriteAsync(command.OutputDirectory, result);
            await output.WriteLineAsync($"render project: passed; result: {Path.Combine(command.OutputDirectory, "render-result.json")}");
            return 0;
        }
        catch (Exception exception)
        {
            await error.WriteLineAsync($"render project failed: {exception.Message}");
            return 3;
        }
    }

    private static async Task<int> RunContentValidateCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        var validator = new ContentValidator();
        var result = validator.Validate(command.ContentTarget ?? string.Empty);

        try
        {
            await ContentValidationArtifactWriter.WriteAsync(command.OutputDirectory, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write content validation artifacts: {exception.Message}");
            return 3;
        }

        var resultPath = Path.Combine(command.OutputDirectory, "result.json");
        await output.WriteLineAsync($"{command.Name}: {result.Result.Status}; result: {resultPath}");
        return result.Result.ExitCode;
    }

    private static async Task<int> RunAssetInspectCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        var inspector = new AssetInspector();
        var result = inspector.Inspect(command.AssetTarget ?? string.Empty);

        try
        {
            await AssetInspectionArtifactWriter.WriteAsync(command.OutputDirectory, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write asset inspection artifacts: {exception.Message}");
            return 3;
        }

        var resultPath = Path.Combine(command.OutputDirectory, "result.json");
        await output.WriteLineAsync($"{command.Name}: {result.Result.Status}; result: {resultPath}");
        return result.Result.ExitCode;
    }

    private static async Task<int> RunAssetPerceiveCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        var perceiver = new AssetPerceiver();
        var result = perceiver.Perceive(command.AssetTarget ?? string.Empty);

        try
        {
            await AssetPerceptionArtifactWriter.WriteAsync(command.OutputDirectory, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write asset perception artifacts: {exception.Message}");
            return 3;
        }

        var resultPath = Path.Combine(command.OutputDirectory, "result.json");
        await output.WriteLineAsync($"{command.Name}: {result.Result.Status}; result: {resultPath}");
        return result.Result.ExitCode;
    }

    private static async Task<int> RunAssetReviewApplyCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        var applier = new AssetReviewApplier();
        var result = applier.Apply(command.DecisionPath ?? string.Empty, command.DryRun);

        try
        {
            await AssetReviewApplyArtifactWriter.WriteAsync(command.OutputDirectory, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write asset review artifacts: {exception.Message}");
            return 3;
        }

        var resultPath = Path.Combine(command.OutputDirectory, "result.json");
        await output.WriteLineAsync($"{command.Name}: {result.Result.Status}; result: {resultPath}");
        return result.Result.ExitCode;
    }

    private static async Task<int> RunMapInspectCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        var inspector = new MapInspector();
        var result = inspector.Inspect(command.MapTarget ?? string.Empty);

        try
        {
            await MapInspectionArtifactWriter.WriteAsync(command.OutputDirectory, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write map inspection artifacts: {exception.Message}");
            return 3;
        }

        var resultPath = Path.Combine(command.OutputDirectory, "result.json");
        await output.WriteLineAsync($"{command.Name}: {result.Result.Status}; result: {resultPath}");
        return result.Result.ExitCode;
    }

    private static async Task<int> RunReviewPackCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        var generator = new ReviewPackGenerator();
        var result = generator.Generate(command.InputDirectory ?? string.Empty);

        try
        {
            await ReviewPackArtifactWriter.WriteAsync(command.OutputDirectory, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write review pack artifacts: {exception.Message}");
            return 3;
        }

        var manifestPath = Path.Combine(command.OutputDirectory, "review-manifest.json");
        await output.WriteLineAsync($"{command.Name}: {result.Manifest.Status}; manifest: {manifestPath}");
        return result.Manifest.ExitCode;
    }

    private static async Task<int> RunAssetCurateCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        var generator = new AssetCurationWorkbenchGenerator();
        var result = generator.Generate(command.AssetTarget ?? string.Empty, command.ReviewPackPath ?? string.Empty);

        try
        {
            await AssetCurationWorkbenchArtifactWriter.WriteAsync(command.OutputDirectory, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write asset curation workbench artifacts: {exception.Message}");
            return 3;
        }

        var dataPath = Path.Combine(command.OutputDirectory, "review-data.json");
        await output.WriteLineAsync($"{command.Name}: {result.ReviewData.Status}; review data: {dataPath}");
        return result.ReviewData.ExitCode;
    }

    private static async Task<int> RunScenarioCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        var runner = new ScenarioRunnerEngine();
        var result = runner.Run(command.ScenarioReference ?? string.Empty);

        try
        {
            await ScenarioArtifactWriter.WriteAsync(command.OutputDirectory, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write scenario artifacts: {exception.Message}");
            return 3;
        }

        var resultPath = Path.Combine(command.OutputDirectory, "result.json");
        await output.WriteLineAsync($"{command.Name}: {result.Result.Status}; result: {resultPath}");
        return result.Result.ExitCode;
    }

    private static async Task<int> RunRuntimeInspectCommandAsync(CliCommand command, TextWriter output, TextWriter error)
    {
        var inspector = new RuntimeInspector();
        var result = inspector.Inspect(command.ScenarioReference ?? string.Empty, command.MapTarget);

        try
        {
            await RuntimeInspectionArtifactWriter.WriteAsync(command.OutputDirectory, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write runtime inspection artifacts: {exception.Message}");
            return 3;
        }

        var resultPath = Path.Combine(command.OutputDirectory, "result.json");
        await output.WriteLineAsync($"{command.Name}: {result.Result.Status}; result: {resultPath}");
        return result.Result.ExitCode;
    }

    private static async Task<int> WriteErrorResultAsync(
        CliCommand command,
        string outputPath,
        string message,
        TextWriter output,
        TextWriter error)
    {
        var productResult = ProductCliResultJson.Error(command.Name, "CLI0003", message);

        try
        {
            Directory.CreateDirectory(command.OutputDirectory);
            await ProductCliResultJson.WriteAsync(outputPath, productResult);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"runtime execution failed and diagnostic artifact could not be written: {exception.Message}");
            await error.WriteLineAsync(message);
            return 3;
        }

        await error.WriteLineAsync(message);
        await output.WriteLineAsync($"{command.Name}: error; result: {outputPath}");
        return 3;
    }
}
