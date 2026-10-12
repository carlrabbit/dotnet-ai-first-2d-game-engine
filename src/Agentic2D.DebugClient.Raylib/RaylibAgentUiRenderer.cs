using Agentic2D.UI;

namespace Agentic2D.DebugClient.Raylib;

/// <summary>Raylib adapter for the renderer-independent UI draw projection.</summary>
public sealed class RaylibAgentUiRenderer : IAgentUiRenderer
{
    public static RaylibAgentUiRenderer Instance { get; } = new();

    public void Render(IReadOnlyList<UiDrawCommand> commands, UiTheme theme, UiViewport viewport, double presentationTime, bool reducedMotion)
    {
        foreach (var command in commands) RaylibGameWindow.DrawUiCommand(command, theme);
    }

    public static void Render(IReadOnlyList<UiDrawCommand> commands, UiTheme theme) => Instance.Render(commands, theme, new UiViewport(1, 1), 0, false);
}
