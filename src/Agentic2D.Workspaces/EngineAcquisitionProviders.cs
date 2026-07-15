namespace Agentic2D.Workspaces;

/// <summary>
/// Internal boundary for source acquisition. M018 intentionally registers only
/// built-ins; it never probes assemblies or discovers third-party providers.
/// </summary>
internal interface IEngineAcquisitionProvider
{
    string Id { get; }
}

internal sealed record BuiltInEngineAcquisitionProvider(string Id) : IEngineAcquisitionProvider;

internal static class EngineAcquisitionProviderRegistry
{
    internal static readonly IReadOnlyDictionary<string, IEngineAcquisitionProvider> BuiltIns =
        new Dictionary<string, IEngineAcquisitionProvider>(StringComparer.Ordinal)
        {
            ["directory-reference"] = new BuiltInEngineAcquisitionProvider("directory-reference"),
            ["directory-copy"] = new BuiltInEngineAcquisitionProvider("directory-copy"),
            ["git-clone"] = new BuiltInEngineAcquisitionProvider("git-clone"),
        };

    internal const string ReservedUnsupportedProvider = "portable-sdk";
}
