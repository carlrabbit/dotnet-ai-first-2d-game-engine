using Raylib_cs;
using RaylibApi = Raylib_cs.Raylib;

namespace Agentic2D.DebugClient.Raylib;

/// <summary>Consumer-specific raylib surface; all game state remains owned by the consumer callback.</summary>
public static class TicTacToeWindow
{
    public static void Show(string title, Func<TicTacToeWindowSnapshot> snapshot, Action advanceOneTick, Action<string, int?> apply, int? autoCloseAfterFrames = null)
    {
        RaylibApi.InitWindow(900, 680, title);
        try
        {
            RaylibApi.SetTargetFPS(60);
            for (var frame = 0; !RaylibApi.WindowShouldClose() && (!autoCloseAfterFrames.HasValue || frame < autoCloseAfterFrames.Value); frame++)
            {
                var state = snapshot();
                if (RaylibApi.IsKeyPressed(KeyboardKey.X)) apply("participant.take-x", null);
                if (RaylibApi.IsKeyPressed(KeyboardKey.O)) apply("participant.take-o", null);
                if (RaylibApi.IsKeyPressed(KeyboardKey.One)) apply("participant.release-x", null);
                if (RaylibApi.IsKeyPressed(KeyboardKey.Two)) apply("participant.release-o", null);
                if (RaylibApi.IsKeyPressed(KeyboardKey.R)) apply("round.restart", null);
                if (RaylibApi.IsMouseButtonPressed(MouseButton.Left))
                {
                    var cell = CellAt(RaylibApi.GetMousePosition());
                    if (cell is not null) apply("board.select-cell", cell);
                }
                advanceOneTick();
                Draw(snapshot());
            }
        }
        finally { if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow(); }
    }

    private static int? CellAt(System.Numerics.Vector2 point)
    {
        const int left = 180, top = 135, cell = 150;
        if (point.X < left || point.X >= left + cell * 3 || point.Y < top || point.Y >= top + cell * 3) return null;
        return (int)((point.Y - top) / cell) * 3 + (int)((point.X - left) / cell);
    }

    private static void Draw(TicTacToeWindowSnapshot state)
    {
        const int left = 180, top = 135, cell = 150;
        RaylibApi.BeginDrawing(); RaylibApi.ClearBackground(new Color(20, 31, 48, 255));
        RaylibApi.DrawText("AUTONOMOUS TIC-TAC-TOE", 180, 34, 30, new Color(204, 232, 244, 255));
        RaylibApi.DrawText($"Round {state.RoundNumber}   X {state.ScoreX}  —  O {state.ScoreO}   Draws {state.DrawCount}", 180, 78, 22, new Color(244, 211, 94, 255));
        for (var index = 0; index < 9; index++)
        {
            var x = left + index % 3 * cell; var y = top + index / 3 * cell;
            var selected = state.Phase == "awaiting-human-input" && state.Cells[index] == "";
            RaylibApi.DrawRectangle(x + 4, y + 4, cell - 8, cell - 8, selected ? new Color(54, 83, 106, 255) : new Color(27, 38, 56, 255));
            RaylibApi.DrawRectangleLinesEx(new Rectangle(x + 4, y + 4, cell - 8, cell - 8), 3, new Color(136, 168, 205, 255));
            var animated = index == state.LastPlacedCell;
            var scale = animated ? (float)state.MarkScale : 1f;
            var opacity = animated ? (byte)Math.Clamp((int)Math.Round(state.MarkOpacity * 255), 0, 255) : (byte)255;
            if (state.Cells[index] == "X") DrawX(x + cell / 2, y + cell / 2, new Color((byte)103, (byte)232, (byte)161, opacity), scale);
            if (state.Cells[index] == "O") RaylibApi.DrawCircleLines(x + cell / 2, y + cell / 2, 43 * scale, new Color((byte)244, (byte)211, (byte)94, opacity));
        }
        foreach (var particle in state.RoundStartParticles)
            RaylibApi.DrawCircle(left + (int)(particle.X * cell), top + (int)(particle.Y * cell), Math.Max(1, (int)(3 * particle.Scale)), new Color((byte)255, (byte)225, (byte)120, particle.Opacity));
        DrawWinnerLine(state.Winner, state.Cells);
        RaylibApi.DrawText($"X: {state.XController}    O: {state.OController}    Turn: {state.CurrentMark}", 180, 610, 20, Color.White);
        RaylibApi.DrawText(state.Message, 180, 642, 16, new Color(222, 212, 245, 255));
        RaylibApi.DrawText("Click a cell when human-controlled. X/O take control; 1/2 release; R restarts.", 180, 575, 16, new Color(136, 168, 205, 255));
        RaylibApi.EndDrawing();
    }
    private static void DrawWinnerLine(string winner, string[] cells)
    {
        if (winner is not ("X" or "O")) return;
        var lines = new[] { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, new[] { 0, 4, 8 }, new[] { 2, 4, 6 } };
        var line = lines.FirstOrDefault(candidate => cells[candidate[0]] == winner && cells[candidate[1]] == winner && cells[candidate[2]] == winner);
        if (line is null) return;
        const int left = 180, top = 135, cell = 150;
        var start = new System.Numerics.Vector2(left + (line[0] % 3 + .5f) * cell, top + (line[0] / 3 + .5f) * cell);
        var end = new System.Numerics.Vector2(left + (line[2] % 3 + .5f) * cell, top + (line[2] / 3 + .5f) * cell);
        RaylibApi.DrawLineEx(start, end, 10, new Color(255, 120, 130, 255));
    }
    private static void DrawX(int x, int y, Color color, float scale) { var span = (int)(40 * scale); RaylibApi.DrawLineEx(new(x - span, y - span), new(x + span, y + span), 8 * scale, color); RaylibApi.DrawLineEx(new(x + span, y - span), new(x - span, y + span), 8 * scale, color); }
}

public sealed record TicTacToeWindowSnapshot(string[] Cells, string CurrentMark, string XController, string OController, string Phase, string Winner, int RoundNumber, int ScoreX, int ScoreO, int DrawCount, string Message, int Tick, int LastPlacedCell, double MarkOpacity, double MarkScale, IReadOnlyList<TicTacToeBurstParticle> RoundStartParticles);
public sealed record TicTacToeBurstParticle(double X, double Y, double Scale, byte Opacity);
