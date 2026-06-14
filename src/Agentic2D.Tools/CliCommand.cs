namespace Agentic2D.Tools;

public sealed record CliCommand(
    string Name,
    string OutputDirectory,
    int Ticks = 3,
    string? ScenarioReference = null,
    string? ContentTarget = null,
    string? AssetTarget = null,
    string? InputDirectory = null,
    string? ReviewPackPath = null);
