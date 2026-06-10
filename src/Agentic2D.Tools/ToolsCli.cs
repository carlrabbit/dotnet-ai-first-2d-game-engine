using Agentic2D.Contracts;
using Agentic2D.Engine;

namespace Agentic2D.Tools;

public static class ToolsCli
{
    public static async Task<int> RunAsync(string[] args, TextWriter error)
    {
        var parseResult = RuntimeSmokeCommand.TryParse(args);

        if (!parseResult.IsSuccess)
        {
            await error.WriteLineAsync(parseResult.Error);
            return 2;
        }

        RuntimeResult result;

        try
        {
            result = RuntimeSmokeScenario.Run(parseResult.Ticks);
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or OverflowException)
        {
            await error.WriteLineAsync(exception.Message);
            return 3;
        }

        var outputPath = Path.Combine(parseResult.OutputDirectory, "result.json");

        try
        {
            Directory.CreateDirectory(parseResult.OutputDirectory);
            await RuntimeResultJson.WriteAsync(outputPath, result);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            await error.WriteLineAsync($"failed to write result artifact: {exception.Message}");
            return 3;
        }

        return result.Status switch
        {
            RuntimeStatus.Passed => 0,
            RuntimeStatus.Failed => 1,
            _ => 3,
        };
    }
}
