using Raylib_cs;
using RaylibApi = Raylib_cs.Raylib;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace Agentic2D.DebugClient.Raylib;

/// <summary>Minimal product-facing window owned by the isolated raylib adapter.</summary>
public static class RaylibGameWindow
{
    public sealed record ReviewWorkbenchItem(string Id, string Subject, string Status);

    public static int ShowReviewWorkbench(string milestone, IReadOnlyList<ReviewWorkbenchItem> items, int? autoCloseAfterFrames = null, string? capturePath = null)
    {
        using var queue = new ReviewDecisionQueue(Directory.GetCurrentDirectory());
        var local = items.Select(item => new LocalReview(item)).ToArray();
        var index = 0; var frame = 0; var mouseWasDown = false; var finalPage = false; var resetting = false; var resetError = string.Empty; var lastAction = "No decision yet"; var resetTask = (Task<DecisionResult>?)null;
        RaylibApi.InitWindow(1120, 720, "Agentic2D — Review Workbench");
        try
        {
            RaylibApi.SetTargetFPS(60);
            while (!RaylibApi.WindowShouldClose() && (!autoCloseAfterFrames.HasValue || frame++ < autoCloseAfterFrames.Value))
            {
                foreach (var completion in queue.TakeCompletions())
                {
                    var target = local.FirstOrDefault(item => item.Item.Id == completion.Id);
                    if (target is not null) target.Persistence = completion.Success ? "saved" : "failed: " + completion.Message;
                    if (!completion.Success) resetError = completion.Message;
                    else if (!local.Any(item => item.Persistence.StartsWith("failed", StringComparison.Ordinal))) resetError = string.Empty;
                }
                if (resetTask is { IsCompleted: true })
                {
                    var result = resetTask.Result; resetTask = null; resetting = false;
                    if (!result.Success) resetError = result.Message;
                    else { foreach (var item in local) { item.Decision = null; item.Persistence = "none"; } index = 0; finalPage = false; lastAction = "Review set reset; question 1 of " + local.Length; }
                }

                var mouse = RaylibApi.GetMousePosition(); var mouseDown = RaylibApi.IsMouseButtonDown(MouseButton.Left); var click = RaylibApi.IsMouseButtonPressed(MouseButton.Left) || (mouseDown && !mouseWasDown); mouseWasDown = mouseDown;
                var left = !resetting && !finalPage && RaylibApi.IsKeyPressed(KeyboardKey.Left); var right = !resetting && !finalPage && RaylibApi.IsKeyPressed(KeyboardKey.Right);
                if (left) index = (index + local.Length - 1) % local.Length; if (right) index = (index + 1) % local.Length;
                var restart = click && Hit(mouse, 35, 600, 300, 90); var reject = click && !resetting && !finalPage && Hit(mouse, 410, 600, 300, 90); var accept = click && !resetting && !finalPage && Hit(mouse, 780, 600, 300, 90);
                var retry = click && finalPage && Hit(mouse, 380, 600, 340, 72); var close = click && finalPage && queue.PendingCount == 0 && !local.Any(item => item.Persistence.StartsWith("failed", StringComparison.Ordinal)) && Hit(mouse, 780, 600, 300, 72);
                if (restart && !resetting)
                {
                    resetting = true; finalPage = false; resetError = string.Empty; lastAction = "RESETTING REVIEW — draining queued decisions";
                    resetTask = ResetAsync(queue, milestone);
                }
                else if (retry) foreach (var item in local.Where(item => item.Persistence.StartsWith("failed", StringComparison.Ordinal))) Enqueue(item, queue, ref lastAction);
                else if (close) return 0;
                else if (reject || accept)
                {
                    var item = local[index]; item.Decision = accept ? "Accepted" : "Rejected"; item.Persistence = "saving"; lastAction = $"Question {index + 1}: {item.Decision} — saving"; Enqueue(item, queue, ref lastAction);
                    var next = NextUndecided(local, index); if (next < 0) finalPage = true; else index = next;
                }

                RaylibApi.BeginDrawing(); RaylibApi.ClearBackground(new Color(14, 22, 38, 255));
                RaylibApi.DrawText("SIMPLE REVIEW WORKBENCH", 50, 28, 28, Color.White); RaylibApi.DrawText(milestone, 50, 66, 16, new Color(155, 173, 198, 255));
                if (resetting) { RaylibApi.DrawText("RESETTING REVIEW", 50, 150, 30, Color.Gold); RaylibApi.DrawText(lastAction + "  " + ActivityFrame(frame), 50, 210, 21, Color.White); }
                else if (finalPage) DrawFinal(local, queue, lastAction, resetError, mouse);
                else DrawQuestion(local, index, lastAction, queue, mouse);
                RaylibApi.EndDrawing();
                if (capturePath is not null && frame == 2) RaylibApi.TakeScreenshot(capturePath);
            }
            return 0;
        }
        finally { if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow(); }

        static int NextUndecided(IReadOnlyList<LocalReview> reviews, int current) { for (var offset = 1; offset <= reviews.Count; offset++) { var candidate = (current + offset) % reviews.Count; if (reviews[candidate].Decision is null) return candidate; } return -1; }
        static void Enqueue(LocalReview item, ReviewDecisionQueue queue, ref string lastAction) { queue.Enqueue(item.Item.Id, item.Decision == "Accepted" ? "approved" : "changes-requested"); lastAction = $"Question {item.Item.Id}: {item.Decision} — saving {Activity(queue.PendingCount)}"; }
        static async Task<DecisionResult> ResetAsync(ReviewDecisionQueue queue, string milestone) { await queue.DrainAsync(); return await queue.RunControlAsync(["review", "reset", "--milestone", milestone]); }
        static string Activity(int value) => value > 0 ? "◌" + new string('.', (value % 3) + 1) : "saved";
        static string ActivityFrame(int frame) => "◌" + new string('.', (frame % 3) + 1);
        static bool Hit(System.Numerics.Vector2 p, int x, int y, int w, int h) => p.X >= x && p.X <= x + w && p.Y >= y && p.Y <= y + h;
        static void DrawQuestion(IReadOnlyList<LocalReview> reviews, int index, string lastAction, ReviewDecisionQueue queue, System.Numerics.Vector2 mouse)
        {
            var item = reviews[index]; RaylibApi.DrawText("<", 50, 110, 30, Color.White); RaylibApi.DrawText($"Question {index + 1} / {reviews.Count}", 470, 110, 22, Color.White); RaylibApi.DrawText(">", 1040, 110, 30, Color.White); DrawWrapped(item.Item.Subject, 50, 150, 1010, 22, Color.White);
            RaylibApi.DrawRectangle(50, 245, 1010, 300, new Color(27, 45, 68, 255)); RaylibApi.DrawRectangleLines(50, 245, 1010, 300, new Color(76, 112, 143, 255)); RaylibApi.DrawCircle(555, 380, 70, new Color(54, 217, 232, 255)); RaylibApi.DrawCircleLines(555, 380, 70, Color.White); RaylibApi.DrawText("LIVE REVIEW CONTENT", 410, 475, 22, Color.White); RaylibApi.DrawText($"Current decision: {item.Decision ?? "none"}   {item.Persistence}", 50, 570, 18, Color.White); RaylibApi.DrawText($"Last decision: {lastAction}   pending {queue.PendingCount} {Activity(queue.PendingCount)}", 50, 595, 16, new Color(193, 207, 225, 255));
            DrawButton(35, 620, 300, 72, "Restart", new Color(64, 91, 125, 255), Hit(mouse, 35, 600, 300, 90)); DrawButton(410, 620, 300, 72, "Reject", new Color(156, 82, 76, 255), Hit(mouse, 410, 600, 300, 90)); DrawButton(780, 620, 300, 72, "Accept", new Color(63, 143, 91, 255), Hit(mouse, 780, 600, 300, 90));
        }
        static void DrawFinal(IReadOnlyList<LocalReview> reviews, ReviewDecisionQueue queue, string lastAction, string error, System.Numerics.Vector2 mouse)
        {
            RaylibApi.DrawText("REVIEW PASS COMPLETE", 50, 150, 30, Color.White); var y = 220; for (var i = 0; i < reviews.Count; i++) { RaylibApi.DrawText($"{i + 1}   {reviews[i].Decision}", 80, y, 22, reviews[i].Decision == "Accepted" ? new Color(130, 230, 150, 255) : Color.Orange); y += 42; }
            RaylibApi.DrawText($"{lastAction}   pending {queue.PendingCount} {Activity(queue.PendingCount)}", 50, 410, 18, Color.White); if (!string.IsNullOrWhiteSpace(error)) { RaylibApi.DrawText("Persistence failed — Retry", 50, 455, 20, Color.Red); DrawButton(380, 600, 340, 72, "Retry", new Color(180, 126, 50, 255), Hit(mouse, 380, 600, 340, 72)); }
            var enabled = queue.PendingCount == 0 && string.IsNullOrWhiteSpace(error) && !reviews.Any(item => item.Persistence.StartsWith("failed", StringComparison.Ordinal)); DrawButton(780, 600, 300, 72, enabled ? "Close" : "Saving…", new Color(64, 91, 125, 255), enabled && Hit(mouse, 780, 600, 300, 72));
        }
        static void DrawButton(int x, int y, int w, int h, string text, Color color, bool hovered) { var fill = hovered ? new Color(Math.Min(color.R + 25, 255), Math.Min(color.G + 25, 255), Math.Min(color.B + 25, 255), 255) : color; RaylibApi.DrawRectangle(x, y, w, h, fill); RaylibApi.DrawRectangleLinesEx(new Rectangle(x, y, w, h), hovered ? 3 : 1, Color.White); RaylibApi.DrawText(text, x + (w - RaylibApi.MeasureText(text, 26)) / 2, y + 22, 26, Color.White); }
        static void DrawWrapped(string text, int x, int y, int maxWidth, int fontSize, Color color) { var line = string.Empty; var row = 0; foreach (var word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries)) { var candidate = string.IsNullOrEmpty(line) ? word : line + " " + word; if (RaylibApi.MeasureText(candidate, fontSize) > maxWidth && line.Length > 0) { RaylibApi.DrawText(line, x, y + row++ * (fontSize + 6), fontSize, color); line = word; } else line = candidate; } if (line.Length > 0) RaylibApi.DrawText(line, x, y + row * (fontSize + 6), fontSize, color); }
    }

    public static void ShowProductShell(string title, IReadOnlyList<string> menu, string output, int? autoCloseAfterFrames = null, string? capturePath = null)
    {
        RaylibApi.InitWindow(960, 540, title + " — Main Menu");
        var selected = 0;
        try
        {
            RaylibApi.SetTargetFPS(60);
            for (var frame = 0; !RaylibApi.WindowShouldClose() && (!autoCloseAfterFrames.HasValue || frame < autoCloseAfterFrames.Value); frame++)
            {
                if (RaylibApi.IsKeyPressed(KeyboardKey.Down)) selected = (selected + 1) % menu.Count;
                if (RaylibApi.IsKeyPressed(KeyboardKey.Up)) selected = (selected + menu.Count - 1) % menu.Count;
                var mouse = RaylibApi.GetMousePosition();
                for (var index = 0; index < menu.Count; index++) if (new System.Numerics.Vector2(320, 130 + index * 48).X <= mouse.X && mouse.X <= 640 && mouse.Y >= 130 + index * 48 && mouse.Y <= 170 + index * 48 && RaylibApi.IsMouseButtonPressed(MouseButton.Left)) selected = index;
                RaylibApi.BeginDrawing(); RaylibApi.ClearBackground(new Color(17, 24, 39, 255));
                RaylibApi.DrawText("AGENTIC2D", 320, 44, 34, Color.White); RaylibApi.DrawText("Endless settlement", 364, 84, 18, new Color(177, 190, 209, 255));
                for (var index = 0; index < menu.Count; index++) { var focused = selected == index; var y = 130 + index * 48; RaylibApi.DrawRectangle(320, y, 320, 40, focused ? new Color(45, 125, 184, 255) : new Color(34, 48, 70, 255)); RaylibApi.DrawRectangleLines(320, y, 320, 40, focused ? Color.White : new Color(86, 104, 130, 255)); RaylibApi.DrawText(menu[index], 344, y + 10, 18, Color.White); }
                RaylibApi.DrawText("Use pointer or Up/Down; Escape closes", 320, 492, 16, new Color(177, 190, 209, 255)); RaylibApi.EndDrawing();
                if (capturePath is not null && frame == 1) { Directory.CreateDirectory(output); var screenshotPath = Path.IsPathRooted(capturePath) ? capturePath : Path.Combine(output, capturePath); var raylibScreenshotPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(screenshotPath)); RaylibApi.TakeScreenshot(raylibScreenshotPath); }
            }
        }
        finally { if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow(); }
    }

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

    private sealed class LocalReview
    {
        public LocalReview(ReviewWorkbenchItem item) => Item = item;
        public ReviewWorkbenchItem Item { get; }
        public string? Decision { get; set; }
        public string Persistence { get; set; } = "none";
    }

    private sealed record DecisionCompletion(string Id, bool Success, string Message);
    private sealed record DecisionJob(string Id, string Decision);
    private sealed record DecisionResult(bool Success, string Message);

    private sealed class ReviewDecisionQueue : IDisposable
    {
        private readonly string root;
        private readonly Channel<DecisionJob> jobs = Channel.CreateUnbounded<DecisionJob>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        private readonly ConcurrentQueue<DecisionCompletion> completions = new();
        private readonly Task worker;
        private int pending;

        public ReviewDecisionQueue(string root) { this.root = root; worker = ConsumeAsync(); }
        public int PendingCount => Volatile.Read(ref pending);
        public void Enqueue(string id, string decision) { Interlocked.Increment(ref pending); jobs.Writer.TryWrite(new DecisionJob(id, decision)); }
        public IEnumerable<DecisionCompletion> TakeCompletions() { while (completions.TryDequeue(out var completion)) yield return completion; }
        public async Task DrainAsync() { while (PendingCount > 0) await Task.Delay(15); }
        public Task<DecisionResult> RunControlAsync(IReadOnlyList<string> args) => RunEngineeringAsync(args);

        private async Task ConsumeAsync()
        {
            await foreach (var job in jobs.Reader.ReadAllAsync())
            {
                var result = await RunEngineeringAsync(["review", "record", job.Id, job.Decision]);
                completions.Enqueue(new DecisionCompletion(job.Id, result.Success, result.Message));
                Interlocked.Decrement(ref pending);
            }
        }

        private async Task<DecisionResult> RunEngineeringAsync(IReadOnlyList<string> args)
        {
            var project = Path.Combine(root, "src", "Agentic2D.Engineering");
            var start = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("run"); start.ArgumentList.Add("--no-build"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(project); start.ArgumentList.Add("--");
            foreach (var arg in args) start.ArgumentList.Add(arg);
            try
            {
                using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("engineering process did not start");
                var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync(); await Task.WhenAll(output, error);
                return process.ExitCode == 0 ? new DecisionResult(true, "saved") : new DecisionResult(false, (await error).Trim());
            }
            catch (Exception exception) { return new DecisionResult(false, exception.Message); }
        }

        public void Dispose() { jobs.Writer.TryComplete(); }
    }
}
