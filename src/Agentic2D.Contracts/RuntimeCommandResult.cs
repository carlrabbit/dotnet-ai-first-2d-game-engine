namespace Agentic2D.Contracts;

public sealed record RuntimeCommandResult(
    string CommandId,
    string Status,
    string Message);
