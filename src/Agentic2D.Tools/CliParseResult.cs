namespace Agentic2D.Tools;

public sealed class CliParseResult
{
    private readonly CliCommand? command;

    private CliParseResult(CliCommand? command, string? error)
    {
        this.command = command;
        Error = error;
    }

    public bool IsSuccess => command is not null;

    public string? Error { get; }

    public CliCommand Command => command ?? throw new InvalidOperationException("Parse result does not contain a command.");

    public static CliParseResult Success(CliCommand command)
    {
        return new CliParseResult(command, error: null);
    }

    public static CliParseResult Failure(string error)
    {
        return new CliParseResult(command: null, error);
    }
}
