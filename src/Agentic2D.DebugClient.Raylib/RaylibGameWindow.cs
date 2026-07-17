using Raylib_cs;
using RaylibApi = Raylib_cs.Raylib;

namespace Agentic2D.DebugClient.Raylib;

/// <summary>Minimal product-facing window owned by the isolated raylib adapter.</summary>
public static class RaylibGameWindow
{
    public static void Show(string title, string scenarioId, int finalTick)
    {
        RaylibApi.InitWindow(960, 540, title);
        try
        {
            RaylibApi.SetTargetFPS(60);
            for (var frame = 0; !RaylibApi.WindowShouldClose() && frame < 120; frame++)
            {
                RaylibApi.BeginDrawing();
                RaylibApi.ClearBackground(Color.Black);
                RaylibApi.DrawText("Agentic2D", 36, 36, 32, Color.White);
                RaylibApi.DrawText("Scenario: " + scenarioId, 36, 88, 20, Color.LightGray);
                RaylibApi.DrawText("Validated tick: " + finalTick, 36, 118, 20, Color.LightGray);
                RaylibApi.DrawText("Escape closes the exported game; validation closes after two seconds", 36, 178, 18, Color.SkyBlue);
                RaylibApi.EndDrawing();
            }
        }
        finally
        {
            if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow();
        }
    }
}
