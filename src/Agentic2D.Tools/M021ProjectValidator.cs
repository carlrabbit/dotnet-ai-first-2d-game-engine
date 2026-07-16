using Agentic2D.Presentation;
using Agentic2D.UI;

namespace Agentic2D.Tools;

internal static class M021ProjectValidator
{
    public static async Task<int> ValidateAsync(string project, string? output, TextWriter writer, TextWriter error)
    {
        if (project != "." || output is null) return -1;
        var root = Agentic2D.Validation.ContentTargetResolver.FindRepositoryRoot();
        var effects = EffectCatalog.Load(Path.Combine(root, "game", "effects"), out var effectDiagnostics);
        var diagnostics = effectDiagnostics.ToList();
        try
        {
            _ = CameraCatalog.Load(Path.Combine(root, "game", "cameras", "camera.player-follow.json"));
            _ = AuthoredUiCatalog.Load(Path.Combine(root, "game", "ui", "ui.player-hud.json"));
            _ = AuthoredUiCatalog.LoadText(Path.Combine(root, "game", "text"));
            _ = AuthoredUiCatalog.LoadFonts(Path.Combine(root, "game", "fonts"));
        }
        catch (InvalidOperationException exception) { diagnostics.Add(exception.Message); }
        Directory.CreateDirectory(output);
        await File.WriteAllTextAsync(Path.Combine(output, "project-validation.json"), System.Text.Json.JsonSerializer.Serialize(new { schema = "agentic2d.project-validation.v2", status = diagnostics.Count == 0 ? "passed" : "failed", effects = effects.Select(x => x.Id), diagnostics }));
        await writer.WriteLineAsync("project validate: " + (diagnostics.Count == 0 ? "passed" : "failed"));
        return diagnostics.Count == 0 ? 0 : 1;
    }
}
