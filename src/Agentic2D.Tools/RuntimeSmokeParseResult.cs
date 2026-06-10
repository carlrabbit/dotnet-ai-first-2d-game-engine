namespace Agentic2D.Tools;

public sealed class RuntimeSmokeParseResult
{
    private readonly RuntimeSmokeCommand? command;

    private RuntimeSmokeParseResult(RuntimeSmokeCommand? command, string? error)
    {
        this.command = command;
        Error = error;
    }

    public bool IsSuccess => command is not null;

    public string? Error { get; }

    public int Ticks => command?.Ticks ?? throw new InvalidOperationException("Parse result does not contain a command.");

    public string OutputDirectory => command?.OutputDirectory ?? throw new InvalidOperationException("Parse result does not contain a command.");

    public static RuntimeSmokeParseResult Success(RuntimeSmokeCommand command)
    {
        return new RuntimeSmokeParseResult(command, error: null);
    }

    public static RuntimeSmokeParseResult Failure(string error)
    {
        return new RuntimeSmokeParseResult(command: null, error);
    }
}
