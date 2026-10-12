using Agentic2D.UI;

namespace Agentic2D.DebugClient.Raylib;

/// <summary>Raylib adapter for the renderer-independent UI draw projection.</summary>
public sealed class RaylibAgentUiRenderer : IAgentUiRenderer
{
    public static RaylibAgentUiRenderer Instance { get; } = new();

    public void Render(IReadOnlyList<UiDrawCommand> commands, UiTheme theme, UiViewport viewport, double presentationTime, bool reducedMotion)
    {
        // The projection is in framebuffer pixels. Raylib's default drawing coordinates are
        // window client units, so scale the camera by the inverse framebuffer/client ratio.
        var dpi = (float)Math.Max(.01, viewport.OsDpiScale);
        global::Raylib_cs.Raylib.BeginMode2D(new global::Raylib_cs.Camera2D
        {
            Offset = System.Numerics.Vector2.Zero,
            Target = System.Numerics.Vector2.Zero,
            Rotation = 0,
            Zoom = 1 / dpi
        });
        foreach (var command in commands)
        {
            var clip = command.VisibleBounds;
            if (clip.Width <= 0 || clip.Height <= 0) continue;
            global::Raylib_cs.Raylib.BeginScissorMode((int)Math.Floor(clip.X), (int)Math.Floor(clip.Y), Math.Max(0, (int)Math.Ceiling(clip.Width)), Math.Max(0, (int)Math.Ceiling(clip.Height)));
            RaylibGameWindow.DrawUiCommand(command, theme);
            global::Raylib_cs.Raylib.EndScissorMode();
        }
        global::Raylib_cs.Raylib.EndMode2D();
    }

    public static void Render(IReadOnlyList<UiDrawCommand> commands, UiTheme theme) => Instance.Render(commands, theme, new UiViewport(1, 1), 0, false);
}
