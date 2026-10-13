using Agentic2D.UI;
using Agentic2D.DebugClient.Raylib;
using Raylib_cs;
using RaylibApi = Raylib_cs.Raylib;

namespace Agentic2D.DebugClient;

internal static class RaylibWorkbenchInput
{
    public static string Dispatch(UiSession session, bool allowEscape = true)
    {
        var source = string.Empty;
        var mouse = RaylibApi.GetMousePosition();
        var dpi = RaylibGameWindow.CurrentViewport().OsDpiScale;
        mouse *= (float)dpi;
        session.Dispatch(new UiInputEvent("pointer-move", mouse.X, mouse.Y));
        if (RaylibApi.IsMouseButtonPressed(MouseButton.Left)) session.Dispatch(new UiInputEvent("pointer-down", mouse.X, mouse.Y));
        if (RaylibApi.IsMouseButtonReleased(MouseButton.Left)) { session.Dispatch(new UiInputEvent("pointer-up", mouse.X, mouse.Y)); source = "mouse-touch"; }
        if (RaylibApi.IsMouseButtonPressed(MouseButton.Left) && !RaylibApi.IsWindowFocused()) session.Dispatch(new UiInputEvent("pointer-cancel"));
        if (RaylibApi.IsKeyPressed(KeyboardKey.Tab)) session.Dispatch(new UiInputEvent("key", Key: RaylibApi.IsKeyDown(KeyboardKey.LeftShift) ? "Up" : "Tab"));
        if (RaylibApi.IsKeyPressed(KeyboardKey.Enter) || RaylibApi.IsKeyPressed(KeyboardKey.KpEnter)) { session.Dispatch(new UiInputEvent("key", Key: "Enter")); source = "enter"; }
        if (RaylibApi.IsKeyPressed(KeyboardKey.Space)) session.Dispatch(new UiInputEvent("key", Key: "Space"));
        if (RaylibApi.IsKeyPressed(KeyboardKey.Up)) session.Dispatch(new UiInputEvent("key", Key: "Up"));
        if (RaylibApi.IsKeyPressed(KeyboardKey.Down)) session.Dispatch(new UiInputEvent("key", Key: "Down"));
        if (RaylibApi.IsKeyPressed(KeyboardKey.Left)) session.Dispatch(new UiInputEvent("key", Key: "Left"));
        if (RaylibApi.IsKeyPressed(KeyboardKey.Right)) session.Dispatch(new UiInputEvent("key", Key: "Right"));
        if (RaylibApi.IsKeyPressed(KeyboardKey.Backspace)) session.Dispatch(new UiInputEvent("key", Key: "Backspace"));
        if (RaylibApi.IsKeyPressed(KeyboardKey.Delete)) session.Dispatch(new UiInputEvent("key", Key: "Delete"));
        if (allowEscape && RaylibApi.IsKeyPressed(KeyboardKey.Escape)) session.Dispatch(new UiInputEvent("key", Key: "Escape"));
        var codepoint = RaylibApi.GetCharPressed();
        while (codepoint > 0)
        {
            if (codepoint >= 32) session.Dispatch(new UiInputEvent("text", Text: char.ConvertFromUtf32(codepoint)));
            codepoint = RaylibApi.GetCharPressed();
        }
        var wheel = RaylibApi.GetMouseWheelMove();
        if (wheel != 0) session.Dispatch(new UiInputEvent("scroll", mouse.X, mouse.Y, Delta: -wheel * 40));
        return source;
    }

    public static void Render(UiSession session)
    {
        RaylibApi.BeginDrawing();
        RaylibApi.ClearBackground(new Color(17, 24, 39, 255));
        RaylibAgentUiRenderer.Instance.Render(UiDrawProjection.Create(session), session.Definition.Theme, session.Layout.Viewport, session.PresentationTime, session.Definition.ReducedMotion);
        RaylibApi.EndDrawing();
    }
}
