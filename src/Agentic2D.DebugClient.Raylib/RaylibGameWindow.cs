using Raylib_cs;
using RaylibApi = Raylib_cs.Raylib;
using System.Text.Json;

namespace Agentic2D.DebugClient.Raylib;

/// <summary>Minimal product-facing window owned by the isolated raylib adapter.</summary>
public static class RaylibGameWindow
{
    public static void Show(string title, string scenarioId, int finalTick, int? autoCloseAfterFrames = null)
    {
        RaylibApi.InitWindow(960, 540, title);
        try
        {
            RaylibApi.SetTargetFPS(60);
            for (var frame = 0; !RaylibApi.WindowShouldClose() && (!autoCloseAfterFrames.HasValue || frame < autoCloseAfterFrames.Value); frame++)
            {
                RaylibApi.BeginDrawing();
                RaylibApi.ClearBackground(Color.Black);
                RaylibApi.DrawText("Agentic2D", 36, 36, 32, Color.White);
                RaylibApi.DrawText("Scenario: " + scenarioId, 36, 88, 20, Color.LightGray);
                RaylibApi.DrawText("Validated tick: " + finalTick, 36, 118, 20, Color.LightGray);
                RaylibApi.DrawText(autoCloseAfterFrames.HasValue ? "Validation closes automatically; Escape closes now" : "Press Escape to close", 36, 178, 18, Color.SkyBlue);
                RaylibApi.EndDrawing();
            }
        }
        finally
        {
            if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow();
        }
    }

    /// <summary>Small content-driven interactive prototype used by a consumer workspace before a stable game-extension API exists.</summary>
    public static void ShowPlayableContent(string title, JsonElement playable, string output, int? autoCloseAfterFrames = null, string? capturePath = null)
    {
        var world = PlayableDefinition.Load(playable); var state = PlayableState.Create(world);
        RaylibApi.InitWindow(960, 540, title);
        try
        {
            RaylibApi.SetTargetFPS(60);
            for (var frame = 0; !RaylibApi.WindowShouldClose() && (!autoCloseAfterFrames.HasValue || frame < autoCloseAfterFrames.Value); frame++)
            {
                var delta = Math.Min(RaylibApi.GetFrameTime(), .05f);
                var direction = new System.Numerics.Vector2((RaylibApi.IsKeyDown(KeyboardKey.Right) || RaylibApi.IsKeyDown(KeyboardKey.D) ? 1 : 0) - (RaylibApi.IsKeyDown(KeyboardKey.Left) || RaylibApi.IsKeyDown(KeyboardKey.A) ? 1 : 0), (RaylibApi.IsKeyDown(KeyboardKey.Down) || RaylibApi.IsKeyDown(KeyboardKey.S) ? 1 : 0) - (RaylibApi.IsKeyDown(KeyboardKey.Up) || RaylibApi.IsKeyDown(KeyboardKey.W) ? 1 : 0));
                var pressedDirection = new System.Numerics.Vector2((RaylibApi.IsKeyPressed(KeyboardKey.Right) || RaylibApi.IsKeyPressed(KeyboardKey.D) ? 1 : 0) - (RaylibApi.IsKeyPressed(KeyboardKey.Left) || RaylibApi.IsKeyPressed(KeyboardKey.A) ? 1 : 0), (RaylibApi.IsKeyPressed(KeyboardKey.Down) || RaylibApi.IsKeyPressed(KeyboardKey.S) ? 1 : 0) - (RaylibApi.IsKeyPressed(KeyboardKey.Up) || RaylibApi.IsKeyPressed(KeyboardKey.W) ? 1 : 0));
                if (direction.LengthSquared() > 0) state.Player += System.Numerics.Vector2.Normalize(direction) * (3f * delta);
                if (pressedDirection.LengthSquared() > 0) state.Player += System.Numerics.Vector2.Normalize(pressedDirection) * .45f;
                state.Player.X = Math.Clamp(state.Player.X, 1f, world.Width - 1f); state.Player.Y = Math.Clamp(state.Player.Y, 1f, world.Height - 1f);
                state.Tick(delta); state.UpdateWorld(world);
                if (RaylibApi.IsKeyPressed(KeyboardKey.E)) state.Interact(world);
                if (RaylibApi.IsKeyPressed(KeyboardKey.F5)) state.Save(world, output);
                if (RaylibApi.IsKeyPressed(KeyboardKey.F9)) state.Load(world, output);
                DrawPlayable(world, state); if (capturePath is not null && frame == 1) RaylibApi.TakeScreenshot(capturePath);
            }
        }
        finally { if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow(); }
    }

    private static void DrawPlayable(PlayableDefinition world, PlayableState state)
    {
        const float scale = 52f, left = 64f, top = 68f;
        RaylibApi.BeginDrawing(); RaylibApi.ClearBackground(new Color(16, 26, 43, 255));
        RaylibApi.DrawRectangle((int)left, (int)top, (int)(world.Width * scale), (int)(world.Height * scale), new Color(23, 37, 57, 255));
        for (var x = 0; x <= world.Width; x++) RaylibApi.DrawRectangle((int)(left + x * scale), (int)top, 3, (int)(world.Height * scale), new Color(82, 101, 122, 255));
        for (var y = 0; y <= world.Height; y++) RaylibApi.DrawRectangle((int)left, (int)(top + y * scale), (int)(world.Width * scale), 3, new Color(82, 101, 122, 255));
        foreach (var container in world.Containers) { var p = ToScreen(container.Position); Diamond(p, 20, state.OpenedContainers.Contains(container.Id) ? new Color(160, 92, 42, 255) : new Color(239, 140, 58, 255)); }
        foreach (var fragment in world.Fragments.Where(x => !state.CollectedFragments.Contains(x.Id))) Polygon(ToScreen(fragment.Position), 16, 6, new Color(244, 211, 94, 255), 20);
        foreach (var hazard in world.Hazards) Triangle(ToScreen(hazard), 21, new Color(233, 80, 80, 255));
        var switchPoint = ToScreen(world.Switch); RaylibApi.DrawRectangle((int)switchPoint.X - 18, (int)switchPoint.Y - 18, 36, 36, new Color(168, 121, 214, 255)); if (state.MechanismActive) RaylibApi.DrawCircle((int)switchPoint.X, (int)switchPoint.Y, 8, new Color(235, 215, 255, 255));
        var zone = ToScreen(world.Zone); RaylibApi.DrawRing(zone, 24, 29, 0, 360, 32, new Color(85, 194, 113, 255));
        var gate = ToScreen(world.Gate); if (!state.ExitOpen) RaylibApi.DrawRectangle((int)gate.X - 18, (int)gate.Y - 32, 36, 64, new Color(85, 194, 113, 210)); else { RaylibApi.DrawRectangle((int)gate.X - 28, (int)gate.Y - 32, 10, 64, new Color(85, 194, 113, 255)); RaylibApi.DrawRectangle((int)gate.X + 18, (int)gate.Y - 32, 10, 64, new Color(85, 194, 113, 255)); }
        var player = ToScreen(state.Player); RaylibApi.DrawCircle((int)player.X, (int)player.Y, 16, new Color(54, 217, 232, 255)); RaylibApi.DrawCircleLines((int)player.X, (int)player.Y, 16, Color.White);
        RaylibApi.DrawText("SIGNAL PASSAGE", 64, 20, 28, new Color(204, 232, 244, 255));
        RaylibApi.DrawText($"HEALTH  {state.Health}/3", 650, 24, 20, state.Health > 1 ? new Color(204, 232, 244, 255) : new Color(233, 80, 80, 255));
        RaylibApi.DrawText($"FRAGMENTS  {state.CollectedFragments.Count}/3", 650, 48, 18, new Color(244, 211, 94, 255));
        RaylibApi.DrawText(state.Completed ? "PASSAGE COMPLETE — press Escape" : "Collect 3 fragments → activate mechanism → reach green ring", 64, 500, 18, state.Completed ? new Color(85, 194, 113, 255) : new Color(204, 232, 244, 255));
        RaylibApi.DrawText(state.Message, 64, 528, 16, new Color(222, 212, 245, 255)); RaylibApi.EndDrawing();
        System.Numerics.Vector2 ToScreen(System.Numerics.Vector2 p) => new(left + p.X * scale, top + p.Y * scale);
    }
    private static void Diamond(System.Numerics.Vector2 p, float r, Color c) { RaylibApi.DrawTriangle(new(p.X, p.Y - r), new(p.X - r, p.Y), new(p.X + r, p.Y), c); RaylibApi.DrawTriangle(new(p.X, p.Y + r), new(p.X - r, p.Y), new(p.X + r, p.Y), c); }
    private static void Triangle(System.Numerics.Vector2 p, float r, Color c) => RaylibApi.DrawTriangle(new(p.X, p.Y - r), new(p.X - r, p.Y + r), new(p.X + r, p.Y + r), c);
    private static void Polygon(System.Numerics.Vector2 p, float r, int sides, Color c, float rotation) => RaylibApi.DrawPoly(p, sides, r, rotation, c);

    private sealed record PlayableObject(string Id, System.Numerics.Vector2 Position);
    private sealed record PlayableDefinition(int Width, int Height, IReadOnlyList<PlayableObject> Containers, IReadOnlyList<PlayableObject> Fragments, IReadOnlyList<System.Numerics.Vector2> Hazards, System.Numerics.Vector2 Switch, System.Numerics.Vector2 Gate, System.Numerics.Vector2 Zone)
    {
        public static PlayableDefinition Load(JsonElement element)
        {
            System.Numerics.Vector2 Point(JsonElement x) => new(x.GetProperty("x").GetSingle(), x.GetProperty("y").GetSingle());
            var containers = element.GetProperty("containers").EnumerateArray().Select(x => new PlayableObject(x.GetProperty("id").GetString()!, Point(x))).ToArray(); var fragments = element.GetProperty("fragments").EnumerateArray().Select(x => new PlayableObject(x.GetProperty("id").GetString()!, Point(x))).ToArray();
            return new(element.GetProperty("width").GetInt32(), element.GetProperty("height").GetInt32(), containers, fragments, element.GetProperty("hazards").EnumerateArray().Select(Point).ToArray(), Point(element.GetProperty("switch")), Point(element.GetProperty("gate")), Point(element.GetProperty("zone")));
        }
    }
    private sealed class PlayableState
    {
        public System.Numerics.Vector2 Player = new(1, 1); public int Health = 3; public bool MechanismActive; public bool ExitOpen; public bool Completed; public string Message = "Move with WASD or arrows. E interacts. F5 saves; F9 loads."; public readonly HashSet<string> CollectedFragments = []; public readonly HashSet<string> OpenedContainers = []; private float invulnerability;
        public static PlayableState Create(PlayableDefinition _) => new();
        public void Tick(float delta) { invulnerability = Math.Max(0, invulnerability - delta); }
        public void Interact(PlayableDefinition world)
        {
            var container = world.Containers.FirstOrDefault(x => Near(x.Position)); if (container is not null && OpenedContainers.Add(container.Id)) { Message = "Container opened — signal route revealed."; return; }
            if (Near(world.Switch)) { if (CollectedFragments.Count < 3) Message = "Mechanism rejected: collect all three fragments."; else { MechanismActive = ExitOpen = true; Message = "Mechanism activated. Exit is open."; } return; }
            Message = "Nothing to interact with here.";
        }
        public void Save(PlayableDefinition _, string output) { Directory.CreateDirectory(output); File.WriteAllText(Path.Combine(output, "signal-passage-live-save.json"), JsonSerializer.Serialize(new { health = Health, fragments = CollectedFragments.Order(), opened = OpenedContainers.Order(), mechanism = MechanismActive, exit = ExitOpen, completed = Completed })); Message = "Saved."; }
        public void Load(PlayableDefinition _, string output) { var path = Path.Combine(output, "signal-passage-live-save.json"); if (!File.Exists(path)) { Message = "No save exists yet."; return; } using var doc = JsonDocument.Parse(File.ReadAllText(path)); var x = doc.RootElement; Health = x.GetProperty("health").GetInt32(); CollectedFragments.Clear(); foreach (var item in x.GetProperty("fragments").EnumerateArray()) CollectedFragments.Add(item.GetString()!); OpenedContainers.Clear(); foreach (var item in x.GetProperty("opened").EnumerateArray()) OpenedContainers.Add(item.GetString()!); MechanismActive = x.GetProperty("mechanism").GetBoolean(); ExitOpen = x.GetProperty("exit").GetBoolean(); Completed = x.GetProperty("completed").GetBoolean(); Message = "Loaded. Transient feedback was not restored."; }
        public bool Near(System.Numerics.Vector2 point) => System.Numerics.Vector2.Distance(Player, point) < .7f;
        public void UpdateWorld(PlayableDefinition world) { foreach (var fragment in world.Fragments.Where(x => !CollectedFragments.Contains(x.Id) && Near(x.Position)).ToArray()) { CollectedFragments.Add(fragment.Id); Message = $"Fragment collected ({CollectedFragments.Count}/3)."; } if (invulnerability == 0 && world.Hazards.Any(Near)) { Health = Math.Max(0, Health - 1); invulnerability = 1.2f; Message = "Signal damage received."; } if (ExitOpen && Near(world.Zone)) { Completed = true; Message = "Signal Passage complete."; } }
    }
}
