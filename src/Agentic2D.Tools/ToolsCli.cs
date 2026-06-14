using Agentic2D.Contracts;
using Agentic2D.Engine;
using Agentic2D.ScenarioRunner;
using Agentic2D.Validation;
using ScenarioRunnerEngine = Agentic2D.ScenarioRunner.ScenarioRunner;

namespace Agentic2D.Tools;

public static class ToolsCli
{
    public static async Task<int> RunAsync(string[] args, TextWriter output, TextWriter error)
    {
        if (args is ["--help"] or ["-h"])
        {
            await output.WriteLineAsync(
                """
                agentic2d

                Usage:
                  agentic2d --help
                  agentic2d --version
                  agentic2d runtime smoke --output <directory>
                  agentic2d validate --output <directory>
                  agentic2d scenario run <scenario-id-or-path> --output <directory>
                  agentic2d content validate <scope-or-path> --output <directory>
                  agentic2d asset inspect <asset-id-or-path> --output <directory>

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
        if (command.Name == "scenario run")
        {
            return await RunScenarioCommandAsync(command, output, error);
        }

        if (command.Name == "content validate")
        {
            return await RunContentValidateCommandAsync(command, output, error);
        }

        if (command.Name == "asset inspect")
        {
            return await RunAssetInspectCommandAsync(command, output, error);
        }

        var outputPath = Path.Combine(command.OutputDirectory, "result.json");
        RuntimeResult result;

        try
        {
            result = RuntimeSmokeScenario.Run(command.Ticks);
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
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write result artifact: {exception.Message}");
            return 3;
        }

        await output.WriteLineAsync($"{command.Name}: {productResult.Status}; result: {outputPath}");
        return productResult.ExitCode;
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
