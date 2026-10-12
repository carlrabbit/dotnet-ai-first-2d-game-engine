using Raylib_cs;
using RaylibApi = Raylib_cs.Raylib;
using System.Text.Json;
using System.Collections.Concurrent;
using System.Threading.Channels;
using System.Runtime.InteropServices;
using Agentic2D.Tools;
using Agentic2D.UI;

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
        InitDesktopWindow("Agentic2D — Review Workbench");
        if (!RaylibApi.IsWindowReady()) { Console.Error.WriteLine("review workbench could not initialize the Raylib window"); return 1; }
        try
        {
            RaylibApi.SetTargetFPS(60);
            while (!RaylibApi.WindowShouldClose() && (!autoCloseAfterFrames.HasValue || frame++ < autoCloseAfterFrames.Value))
            {
                foreach (var completion in queue.TakeCompletions())
                {
                    var target = local.FirstOrDefault(item => item.Item.Id == completion.Id);
                    if (target is not null) { target.Persistence = completion.Success ? "saved" : "failed: " + completion.Message; if (completion.Success && target.Decision is not null) target.CurrentStatus = target.Decision == "Accepted" ? "approved" : "changes-requested"; }
                    if (!completion.Success) resetError = completion.Message;
                    else if (!local.Any(item => item.Persistence.StartsWith("failed", StringComparison.Ordinal))) resetError = string.Empty;
                }
                foreach (var item in local.Where(item => item.PreviewTask is { IsCompleted: true }))
                {
                    var result = item.PreviewTask!.Result;
                    item.PreviewStatus = result.Success ? result.Message : "failed: " + result.Message;
                    item.PreviewTask = null;
                }
                if (resetTask is { IsCompleted: true })
                {
                    var result = resetTask.Result; resetTask = null; resetting = false;
                    if (!result.Success) resetError = result.Message;
                    else { foreach (var item in local) { item.Decision = null; item.Persistence = "none"; } index = 0; finalPage = false; lastAction = "Review set reset; question 1 of " + local.Length; }
                }

                var mouse = ToLegacyCoordinates(RaylibApi.GetMousePosition()); var mouseDown = RaylibApi.IsMouseButtonDown(MouseButton.Left); var click = RaylibApi.IsMouseButtonPressed(MouseButton.Left) || (mouseDown && !mouseWasDown); mouseWasDown = mouseDown;
                var left = !resetting && !finalPage && RaylibApi.IsKeyPressed(KeyboardKey.Left); var right = !resetting && !finalPage && RaylibApi.IsKeyPressed(KeyboardKey.Right);
                if (left) index = (index + local.Length - 1) % local.Length; if (right) index = (index + 1) % local.Length;
                var restart = click && Hit(mouse, 35, 600, 300, 90); var reject = click && !resetting && !finalPage && Hit(mouse, 410, 600, 300, 90); var accept = click && !resetting && !finalPage && Hit(mouse, 780, 600, 300, 90);
                var launchPreview = click && !resetting && !finalPage && (milestone is "M048" or "M052") && Hit(mouse, 350, 435, 420, 72);
                var retry = click && finalPage && Hit(mouse, 380, 600, 340, 72); var close = click && finalPage && queue.PendingCount == 0 && !local.Any(item => item.Persistence.StartsWith("failed", StringComparison.Ordinal)) && Hit(mouse, 780, 600, 300, 72);
                if (restart && !resetting)
                {
                    resetting = true; finalPage = false; resetError = string.Empty; lastAction = "RESETTING REVIEW — draining queued decisions";
                    resetTask = ResetAsync(queue, milestone);
                }
                else if (retry) foreach (var item in local.Where(item => item.Persistence.StartsWith("failed", StringComparison.Ordinal))) Enqueue(item, queue, ref lastAction);
                else if (close) return 0;
                else if (launchPreview)
                {
                    var item = local[index];
                    if (item.PreviewTask is null || item.PreviewTask.IsCompleted)
                    {
                        item.PreviewStatus = milestone == "M052" ? "launching actual UI stress screen…" : "launching actual candidate preview…";
                        item.PreviewTask = milestone == "M052" ? LaunchUiStressAsync() : LaunchPreviewAsync(item.Item.Id);
                    }
                }
                else if (reject || accept)
                {
                    var item = local[index]; item.Decision = accept ? "Accepted" : "Rejected"; item.Persistence = "saving"; lastAction = $"Question {index + 1}: {item.Decision} — saving"; Enqueue(item, queue, ref lastAction);
                    var next = NextUndecided(local, index); if (next < 0) finalPage = true; else index = next;
                }

                RaylibApi.BeginDrawing(); RaylibApi.ClearBackground(new Color(14, 22, 38, 255)); RaylibApi.BeginMode2D(LegacyCamera());
                RaylibApi.DrawText("SIMPLE REVIEW WORKBENCH", 50, 28, 28, Color.White); RaylibApi.DrawText(milestone, 50, 66, 16, new Color(155, 173, 198, 255));
                if (resetting) { RaylibApi.DrawText("RESETTING REVIEW", 50, 150, 30, Color.Gold); RaylibApi.DrawText(lastAction + "  " + ActivityFrame(frame), 50, 210, 21, Color.White); }
                else if (finalPage) DrawFinal(local, queue, lastAction, resetError, mouse);
                else DrawQuestion(milestone, local, index, lastAction, queue, mouse);
                RaylibApi.EndMode2D(); RaylibApi.EndDrawing();
                if (capturePath is not null && frame == 2)
                {
                    var absoluteCapture = Path.GetFullPath(capturePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(absoluteCapture)!);
                    RaylibApi.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), absoluteCapture));
                }
            }
            return 0;
        }
        finally { if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow(); }

        static int NextUndecided(IReadOnlyList<LocalReview> reviews, int current) { for (var offset = 1; offset <= reviews.Count; offset++) { var candidate = (current + offset) % reviews.Count; if (reviews[candidate].IsUnresolved && reviews[candidate].Decision is null) return candidate; } return -1; }
        static void Enqueue(LocalReview item, ReviewDecisionQueue queue, ref string lastAction) { queue.Enqueue(item.Item.Id, item.Decision == "Accepted" ? "approved" : "changes-requested", item.CurrentStatus == "approved"); lastAction = $"Question {item.Item.Id}: {item.Decision} — saving {Activity(queue.PendingCount)}"; }
        static async Task<DecisionResult> ResetAsync(ReviewDecisionQueue queue, string milestone) { await queue.DrainAsync(); return await queue.RunControlAsync(["review", "reset", "--milestone", milestone]); }
        static async Task<PreviewLaunchResult> LaunchPreviewAsync(string reviewId)
        {
            var root = FindRepositoryRoot();
            if (!M048ReviewExperienceRegistry.TryResolve(reviewId, root, out var fixture, out var error)) return new(false, error);
            var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib");
            var start = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("run"); start.ArgumentList.Add("--no-build"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(project); start.ArgumentList.Add("--"); start.ArgumentList.Add("asset-preview"); start.ArgumentList.Add("--scene"); start.ArgumentList.Add(fixture!.ScenePath);
            try
            {
                using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("asset-preview process did not start");
                var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
                await Task.WhenAll(process.WaitForExitAsync(), stdout, stderr);
                if (process.ExitCode != 0) return new(false, "actual candidate preview exited abnormally: " + stderr.Result.Trim());
                return new(true, "launched / completed");
            }
            catch (Exception exception) { return new(false, "actual candidate preview could not be launched: " + exception.Message); }
        }
        static async Task<PreviewLaunchResult> LaunchUiStressAsync()
        {
            var root = FindRepositoryRoot();
            var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib");
            var start = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
            start.ArgumentList.Add("run"); start.ArgumentList.Add("--no-build"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(project); start.ArgumentList.Add("--"); start.ArgumentList.Add("ui-stress");
            try
            {
                using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("UI stress screen process did not start");
                var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
                await Task.WhenAll(process.WaitForExitAsync(), stdout, stderr);
                if (process.ExitCode != 0) return new(false, "actual UI stress screen exited abnormally: " + stderr.Result.Trim());
                return new(true, "launched / completed");
            }
            catch (Exception exception) { return new(false, "actual UI stress screen could not be launched: " + exception.Message); }
        }
        static string FindRepositoryRoot()
        {
            var directory = AppContext.BaseDirectory;
            while (!string.IsNullOrWhiteSpace(directory)) { if (File.Exists(Path.Combine(directory, "dotnet-ai-first-2d-game-engine.slnx"))) return directory; directory = Directory.GetParent(directory)?.FullName; }
            return Directory.GetCurrentDirectory();
        }
        static string Activity(int value) => value > 0 ? "◌" + new string('.', (value % 3) + 1) : "saved";
        static string ActivityFrame(int frame) => "◌" + new string('.', (frame % 3) + 1);
        static bool Hit(System.Numerics.Vector2 p, int x, int y, int w, int h) => p.X >= x && p.X <= x + w && p.Y >= y && p.Y <= y + h;
        static void DrawQuestion(string milestone, IReadOnlyList<LocalReview> reviews, int index, string lastAction, ReviewDecisionQueue queue, System.Numerics.Vector2 mouse)
        {
            var item = reviews[index]; var question = ReviewSurfaceLayout.WorkbenchQuestion(item.Item.Subject); var content = ReviewSurfaceLayout.WorkbenchContent;
            RaylibApi.DrawText("<", 50, 110, 30, Color.White); RaylibApi.DrawText($"Question {index + 1} / {reviews.Count}", 470, 110, 22, Color.White); RaylibApi.DrawText(">", 1040, 110, 30, Color.White);
            RaylibApi.DrawText("Status: " + item.CurrentStatus.ToUpperInvariant(), 50, 114, 16, StatusColor(item.CurrentStatus));
            DrawWrappedLines(question.Lines, question.Bounds.X, question.Bounds.Y, question.LineHeight, question.FontSize, Color.White);
            RaylibApi.DrawRectangle(content.X, content.Y, content.Width, content.Height, new Color(27, 45, 68, 255)); RaylibApi.DrawRectangleLines(content.X, content.Y, content.Width, content.Height, new Color(76, 112, 143, 255));
            if (milestone == "M048") DrawReviewSubjectPanel(item, mouse);
            else if (milestone == "M052") DrawM052ReviewPanel(item, mouse);
            else { RaylibApi.DrawCircle(555, 380, 70, new Color(54, 217, 232, 255)); RaylibApi.DrawCircleLines(555, 380, 70, Color.White); RaylibApi.DrawText("LIVE REVIEW CONTENT", 410, 475, 22, Color.White); }
            RaylibApi.DrawText($"Current decision: {item.Decision ?? "none"}   {item.Persistence}", 50, 556, 17, Color.White); RaylibApi.DrawText($"Last decision: {lastAction}   pending {queue.PendingCount} {Activity(queue.PendingCount)}", 50, 581, 15, new Color(193, 207, 225, 255));
            DrawButton(35, 620, 300, 72, "Restart", new Color(64, 91, 125, 255), Hit(mouse, 35, 600, 300, 90)); DrawButton(410, 620, 300, 72, "Reject", new Color(156, 82, 76, 255), Hit(mouse, 410, 600, 300, 90)); DrawButton(780, 620, 300, 72, "Accept", new Color(63, 143, 91, 255), Hit(mouse, 780, 600, 300, 90));
        }
        static void DrawReviewSubjectPanel(LocalReview item, System.Numerics.Vector2 mouse)
        {
            RaylibApi.DrawText("M048 CANDIDATE PREVIEW", 82, 275, 22, Color.White);
            RaylibApi.DrawText("The actual candidate is available in the separate Raylib asset-preview window.", 82, 315, 17, new Color(178, 198, 220, 255));
            RaylibApi.DrawText("Preview: " + item.PreviewStatus, 82, 360, 17, item.PreviewStatus.StartsWith("failed", StringComparison.Ordinal) ? Color.Red : Color.LightGray);
            DrawButton(350, 430, 420, 60, item.PreviewStatus.StartsWith("launched", StringComparison.Ordinal) ? "Launch again" : "Launch candidate preview", new Color(72, 104, 158, 255), Hit(mouse, 350, 435, 420, 72));
            RaylibApi.DrawText("Launching is optional; Accept and Reject remain available.", 82, 510, 16, new Color(193, 207, 225, 255));
        }
        static void DrawM052ReviewPanel(LocalReview item, System.Numerics.Vector2 mouse)
        {
            RaylibApi.DrawText("M052 AGENT-ORIENTED UI", 82, 265, 24, Color.White);
            RaylibApi.DrawText("Launch the actual Product Shell stress screen to inspect layout, readable content, focus, feedback and input.", 82, 305, 17, new Color(178, 198, 220, 255));
            RaylibApi.DrawText("Preview: " + item.PreviewStatus, 82, 355, 17, item.PreviewStatus.StartsWith("failed", StringComparison.Ordinal) ? Color.Red : Color.LightGray);
            DrawButton(350, 430, 420, 60, item.PreviewStatus.StartsWith("launched", StringComparison.Ordinal) ? "Launch UI stress screen again" : "Launch actual UI stress screen", new Color(72, 104, 158, 255), Hit(mouse, 350, 435, 420, 72));
            RaylibApi.DrawText("The synthetic content does not write review decisions or modify review persistence.", 82, 510, 16, new Color(193, 207, 225, 255));
        }
        static void DrawFinal(IReadOnlyList<LocalReview> reviews, ReviewDecisionQueue queue, string lastAction, string error, System.Numerics.Vector2 mouse)
        {
            RaylibApi.DrawText("REVIEW PASS COMPLETE", 50, 150, 30, Color.White); var y = 220; for (var i = 0; i < reviews.Count; i++) { RaylibApi.DrawText($"{i + 1}   {reviews[i].Decision}", 80, y, 22, reviews[i].Decision == "Accepted" ? new Color(130, 230, 150, 255) : Color.Orange); y += 42; }
            RaylibApi.DrawText($"{lastAction}   pending {queue.PendingCount} {Activity(queue.PendingCount)}", 50, 410, 18, Color.White); if (!string.IsNullOrWhiteSpace(error)) { RaylibApi.DrawText("Persistence failed — Retry", 50, 455, 20, Color.Red); DrawButton(380, 600, 340, 72, "Retry", new Color(180, 126, 50, 255), Hit(mouse, 380, 600, 340, 72)); }
            var enabled = queue.PendingCount == 0 && string.IsNullOrWhiteSpace(error) && !reviews.Any(item => item.Persistence.StartsWith("failed", StringComparison.Ordinal)); DrawButton(780, 600, 300, 72, enabled ? "Close" : "Saving…", new Color(64, 91, 125, 255), enabled && Hit(mouse, 780, 600, 300, 72));
        }
        static void DrawButton(int x, int y, int w, int h, string text, Color color, bool hovered) { var fill = hovered ? new Color(Math.Min(color.R + 25, 255), Math.Min(color.G + 25, 255), Math.Min(color.B + 25, 255), 255) : color; RaylibApi.DrawRectangle(x, y, w, h, fill); RaylibApi.DrawRectangleLinesEx(new Rectangle(x, y, w, h), hovered ? 3 : 1, Color.White); RaylibApi.DrawText(text, x + (w - RaylibApi.MeasureText(text, 26)) / 2, y + 22, 26, Color.White); }
        static Color StatusColor(string status) => status switch { "approved" => new Color(130, 230, 150, 255), "changes-requested" => Color.Orange, _ => Color.Gold };

        static void DrawWrappedLines(IReadOnlyList<string> lines, int x, int y, int lineHeight, int fontSize, Color color) { for (var row = 0; row < lines.Count; row++) RaylibApi.DrawText(lines[row], x, y + row * lineHeight, fontSize, color); }
    }

    public static void ShowProductShell(string title, IReadOnlyList<MenuItem> menu, string output, Action<MenuItem>? dispatchAction = null, int? autoCloseAfterFrames = null, string? capturePath = null, string? inputProbeReportPath = null)
    {
        InitDesktopWindow(title + " — Main Menu");
        if (!RaylibApi.IsWindowReady()) throw new InvalidOperationException("Raylib failed to create a graphical Product Shell window.");
        var definition = M052UiScreens.ProductShell(menu);
        var session = new UiSession(definition, CurrentViewport());
        var lastViewport = CurrentViewport();
        var shouldExit = false;
        var observed = new List<UiActionRequest>();
        if (inputProbeReportPath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(inputProbeReportPath))!);
            File.WriteAllText(inputProbeReportPath, JsonSerializer.Serialize(new
            {
                status = "ready",
                viewport = session.Layout.Viewport,
                layoutFingerprint = session.Layout.Fingerprint,
                menu = menu.Select(x => new { x.Id, x.Label, x.Available }).ToArray(),
                controls = session.Layout.Elements.Where(x => x.Focusable && x.Enabled).OrderBy(x => x.LogicalBounds.Y).ThenBy(x => x.Order).Select(x => new { x.Id, x.ActionId, x.PhysicalBounds }).ToArray()
            }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
        try
        {
            RaylibApi.SetTargetFPS(60);
            for (var frame = 0; !RaylibApi.WindowShouldClose() && !shouldExit && (!autoCloseAfterFrames.HasValue || frame < autoCloseAfterFrames.Value); frame++)
            {
                var currentViewport = CurrentViewport();
                if (currentViewport != lastViewport) { lastViewport = currentViewport; session.Recompose(definition, currentViewport); }
                if (!RaylibApi.IsWindowFocused() && session.CapturedId is not null) session.Dispatch(new UiInputEvent("pointer-cancel"));
                var mouse = RaylibApi.GetMousePosition(); var dpi = CurrentViewport().OsDpiScale; mouse *= (float)dpi;
                session.Dispatch(new UiInputEvent("pointer-move", mouse.X, mouse.Y));
                if (RaylibApi.IsMouseButtonPressed(MouseButton.Left)) session.Dispatch(new UiInputEvent("pointer-down", mouse.X, mouse.Y));
                if (RaylibApi.IsMouseButtonReleased(MouseButton.Left)) session.Dispatch(new UiInputEvent("pointer-up", mouse.X, mouse.Y));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Down)) session.Dispatch(new UiInputEvent("key", Key: "Down"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Up)) session.Dispatch(new UiInputEvent("key", Key: "Up"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Tab)) session.Dispatch(new UiInputEvent("key", Key: RaylibApi.IsKeyDown(KeyboardKey.LeftShift) ? "Up" : "Tab"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Enter) || RaylibApi.IsKeyPressed(KeyboardKey.KpEnter)) session.Dispatch(new UiInputEvent("key", Key: "Enter"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Space)) session.Dispatch(new UiInputEvent("key", Key: "Space"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Left)) session.Dispatch(new UiInputEvent("key", Key: "Left"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Right)) session.Dispatch(new UiInputEvent("key", Key: "Right"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Backspace)) session.Dispatch(new UiInputEvent("key", Key: "Backspace"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Delete)) session.Dispatch(new UiInputEvent("key", Key: "Delete"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Escape)) shouldExit = true;
                foreach (var request in session.DrainActions())
                {
                    observed.Add(request);
                    if (!request.ActionId.StartsWith("shell.", StringComparison.Ordinal)) continue;
                    var id = request.ActionId["shell.".Length..];
                    var current = menu.FirstOrDefault(item => item.Id == id);
                    if (current is null || !current.Available) continue;
                    if (id == "quit") shouldExit = true;
                    else dispatchAction?.Invoke(current);
                }
                if (inputProbeReportPath is not null && observed.Count > 0)
                    File.WriteAllText(inputProbeReportPath, JsonSerializer.Serialize(new { status = "observed", viewport = session.Layout.Viewport, layoutFingerprint = session.Layout.Fingerprint, focus = session.FocusedId, requests = observed }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
                RaylibApi.BeginDrawing(); RaylibApi.ClearBackground(ParseThemeColor(definition.Theme.Background));
                RaylibAgentUiRenderer.Render(UiDrawProjection.Create(session), definition.Theme);
                RaylibApi.EndDrawing();
                if (capturePath is not null && frame == 1) { Directory.CreateDirectory(output); var screenshotPath = Path.IsPathRooted(capturePath) ? capturePath : Path.Combine(output, capturePath); var raylibScreenshotPath = Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(screenshotPath)); RaylibApi.TakeScreenshot(raylibScreenshotPath); }
            }
        }
        finally { if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow(); }
    }

    public static void ShowUiStress(string title, int? autoCloseAfterFrames = null, string? capturePath = null)
    {
        InitDesktopWindow(title);
        if (!RaylibApi.IsWindowReady()) throw new InvalidOperationException("Raylib failed to create a graphical UI stress window.");
        var operationController = new M052SyntheticReviewOperationController();
        var modalVisible = false;
        var definition = M052UiScreens.ReviewStress(operationController.Projection, modalVisible);
        var session = new UiSession(definition, CurrentViewport());
        var lastViewport = CurrentViewport();
        try
        {
            RaylibApi.SetTargetFPS(60);
            for (var frame = 0; !RaylibApi.WindowShouldClose() && (!autoCloseAfterFrames.HasValue || frame < autoCloseAfterFrames.Value); frame++)
            {
                var currentViewport = CurrentViewport();
                if (currentViewport != lastViewport) { lastViewport = currentViewport; session.Recompose(definition, currentViewport); }
                session.Dispatch(new UiInputEvent("time", Delta: RaylibApi.GetFrameTime()));
                var operationChanged = operationController.Advance(RaylibApi.GetFrameTime());
                if (!RaylibApi.IsWindowFocused() && session.CapturedId is not null) session.Dispatch(new UiInputEvent("pointer-cancel"));
                var mouse = RaylibApi.GetMousePosition(); var dpi = CurrentViewport().OsDpiScale; mouse *= (float)dpi; session.Dispatch(new UiInputEvent("pointer-move", mouse.X, mouse.Y));
                if (RaylibApi.IsMouseButtonPressed(MouseButton.Left)) session.Dispatch(new UiInputEvent("pointer-down", mouse.X, mouse.Y));
                if (RaylibApi.IsMouseButtonReleased(MouseButton.Left)) session.Dispatch(new UiInputEvent("pointer-up", mouse.X, mouse.Y));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Tab)) session.Dispatch(new UiInputEvent("key", Key: "Tab"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Enter)) session.Dispatch(new UiInputEvent("key", Key: "Enter"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Space)) session.Dispatch(new UiInputEvent("key", Key: "Space"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Backspace)) session.Dispatch(new UiInputEvent("key", Key: "Backspace"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Delete)) session.Dispatch(new UiInputEvent("key", Key: "Delete"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Left)) session.Dispatch(new UiInputEvent("key", Key: "Left"));
                if (RaylibApi.IsKeyPressed(KeyboardKey.Right)) session.Dispatch(new UiInputEvent("key", Key: "Right"));
                var codepoint = RaylibApi.GetCharPressed();
                while (codepoint > 0) { if (codepoint >= 32) session.Dispatch(new UiInputEvent("text", Text: char.ConvertFromUtf32(codepoint))); codepoint = RaylibApi.GetCharPressed(); }
                var wheel = RaylibApi.GetMouseWheelMove(); if (wheel != 0) session.Dispatch(new UiInputEvent("scroll", mouse.X, mouse.Y, Delta: -wheel * 40));
                foreach (var request in session.DrainActions())
                {
                    if (request.ActionId == "stress.dialog") modalVisible = true;
                    else if (request.ActionId == "stress.modal.close") modalVisible = false;
                    else if (request.ActionId == "stress.start") operationChanged |= operationController.Start();
                    else if (request.ActionId == "stress.cancel") operationChanged |= operationController.Cancel();
                    else if (request.ActionId == "stress.retry") operationChanged |= operationController.Retry();
                }
                var wasModalVisible = definition.Root.Items.SelectMany(Flatten).Any(x => x.Kind == AgentUiKind.Modal && x.Visible);
                if (modalVisible != wasModalVisible || operationChanged)
                {
                    definition = M052UiScreens.ReviewStress(operationController.Projection, modalVisible);
                    session.Recompose(definition, CurrentViewport());
                }
                RaylibApi.BeginDrawing(); RaylibApi.ClearBackground(ParseThemeColor(definition.Theme.Background));
                RaylibAgentUiRenderer.Render(UiDrawProjection.Create(session), definition.Theme);
                RaylibApi.EndDrawing();
                if (capturePath is not null && frame == 1) RaylibApi.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(capturePath)));
            }
        }
        finally { if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow(); }

        static IEnumerable<UiElementDefinition> Flatten(UiElementDefinition item) { yield return item; foreach (var child in item.Items) foreach (var nested in Flatten(child)) yield return nested; }
    }

    private static unsafe void InitDesktopWindow(string title)
    {
        RaylibApi.SetConfigFlags(ConfigFlags.ResizableWindow);
        var work = GetWorkArea();
        var monitor = RaylibApi.GetCurrentMonitor();
        var systemDpi = OperatingSystem.IsWindows() ? Math.Max(96, GetDpiForSystem()) / 96d : 96d / 96d;
        var workWidth = work.Width > 0 ? work.Width / systemDpi : RaylibApi.GetMonitorWidth(monitor) / systemDpi;
        var workHeight = work.Height > 0 ? work.Height / systemDpi : RaylibApi.GetMonitorHeight(monitor) / systemDpi;
        var width = (int)Math.Max(1, Math.Round(Math.Min(1920, workWidth)));
        var height = (int)Math.Max(1, Math.Round(Math.Min(1080, workHeight)));
        RaylibApi.InitWindow(width, height, title);
        if (!RaylibApi.IsWindowReady() || work.Width <= 0 || work.Height <= 0) return;
        var handle = (nint)RaylibApi.GetWindowHandle();
        if (GetWindowRect(handle, out var windowRect) && GetClientRect(handle, out var clientRect))
        {
            var decorationWidth = Math.Max(0, (windowRect.Right - windowRect.Left) - (clientRect.Right - clientRect.Left));
            var decorationHeight = Math.Max(0, (windowRect.Bottom - windowRect.Top) - (clientRect.Bottom - clientRect.Top));
            var scale = RaylibApi.GetWindowScaleDPI(); var dpiX = float.IsFinite(scale.X) && scale.X > 0 ? scale.X : 1; var dpiY = float.IsFinite(scale.Y) && scale.Y > 0 ? scale.Y : 1;
            var logicalWorkWidth = work.Width / dpiX; var logicalWorkHeight = work.Height / dpiY;
            var logicalDecorationWidth = decorationWidth / dpiX; var logicalDecorationHeight = decorationHeight / dpiY;
            var clientWidth = Math.Min(1920, Math.Max(1, (int)Math.Floor(logicalWorkWidth - logicalDecorationWidth)));
            var clientHeight = Math.Min(1080, Math.Max(1, (int)Math.Floor(logicalWorkHeight - logicalDecorationHeight)));
            RaylibApi.SetWindowSize(clientWidth, clientHeight);
            var logicalLeft = work.Left / dpiX; var logicalTop = work.Top / dpiY;
            RaylibApi.SetWindowPosition((int)Math.Round(logicalLeft + Math.Max(0, (logicalWorkWidth - clientWidth - logicalDecorationWidth) / 2)), (int)Math.Round(logicalTop + Math.Max(0, (logicalWorkHeight - clientHeight - logicalDecorationHeight) / 2)));
        }
    }

    private static UiViewport CurrentViewport()
    {
        var dpi = RaylibApi.GetWindowScaleDPI();
        var dpiScale = Math.Max(dpi.X, dpi.Y);
        var renderWidth = RaylibApi.GetRenderWidth();
        var renderHeight = RaylibApi.GetRenderHeight();
        var screenWidth = RaylibApi.GetScreenWidth();
        var screenHeight = RaylibApi.GetScreenHeight();
        var effectiveDpi = float.IsFinite(dpiScale) && dpiScale > 0 ? (double)dpiScale : 1;
        if (renderWidth > 0 && screenWidth > 0) effectiveDpi = renderWidth / (double)screenWidth;
        return new(renderWidth > 0 ? renderWidth : screenWidth, renderHeight > 0 ? renderHeight : screenHeight, 1, effectiveDpi);
    }

    private static System.Numerics.Vector2 ToLegacyCoordinates(System.Numerics.Vector2 point)
    {
        var camera = LegacyCamera();
        var scale = camera.Zoom;
        return (point - camera.Offset) / scale;
    }

    private static Camera2D LegacyCamera()
    {
        var screenWidth = RaylibApi.GetScreenWidth(); var screenHeight = RaylibApi.GetScreenHeight();
        var scale = Math.Min(screenWidth / 1120f, screenHeight / 720f);
        var x = (screenWidth - 1120f * scale) / 2; var y = (screenHeight - 720f * scale) / 2;
        return new Camera2D { Offset = new(x, y), Target = System.Numerics.Vector2.Zero, Rotation = 0, Zoom = scale };
    }

    private static WorkArea GetWorkArea()
    {
        if (!OperatingSystem.IsWindows()) return default;
        var area = new NativeRect();
        return SystemParametersInfo(0x0030, 0, ref area, 0) ? new(area.Left, area.Top, area.Right - area.Left, area.Bottom - area.Top) : default;
    }

    private readonly record struct WorkArea(int Left, int Top, int Width, int Height);
    [StructLayout(LayoutKind.Sequential)] private struct NativeRect { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SystemParametersInfo(uint action, uint parameter, ref NativeRect value, uint flags);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetWindowRect(nint handle, out NativeRect rectangle);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool GetClientRect(nint handle, out NativeRect rectangle);
    [DllImport("user32.dll")] private static extern uint GetDpiForSystem();

    internal static void DrawUiCommand(UiDrawCommand item, UiTheme theme)
    {
        var b = item.Bounds; var x = (int)Math.Round(b.X); var y = (int)Math.Round(b.Y); var width = Math.Max(1, (int)Math.Round(b.Width)); var height = Math.Max(1, (int)Math.Round(b.Height));
        var interactive = item.Kind is "Button" or "Toggle" or "Slider" or "TextField" or "Select" or "List";
        if (interactive)
        {
            var fill = !item.Enabled ? new Color(54, 65, 82, 255) : item.Focused || item.Variant == "hovered" ? ParseThemeColor(theme.Accent) : ParseThemeColor(theme.Surface);
            RaylibApi.DrawRectangle(x, y, width, height, fill); RaylibApi.DrawRectangleLinesEx(new Rectangle(x, y, width, height), item.Focused ? (float)theme.FocusWidth : 1, item.Focused ? ParseThemeColor(theme.Foreground) : new Color(75, 91, 112, 255));
        }
        else if (item.Kind is "ContentSlot" or "Image")
        {
            RaylibApi.DrawRectangle(x, y, width, height, ParseThemeColor(theme.Surface)); RaylibApi.DrawRectangleLines(x, y, width, height, new Color(86, 117, 148, 255));
            RaylibApi.DrawText(item.ContentReference ?? "Preview", x + 14, y + 14, 18, Color.LightGray);
        }
        else if (item.Kind == "Modal")
        {
            RaylibApi.DrawRectangle(x, y, width, height, new Color(6, 12, 22, 235));
            var panel = item.ModalPanelBounds ?? item.Bounds; var panelX = (int)Math.Round(panel.X); var panelY = (int)Math.Round(panel.Y); var panelWidth = Math.Max(0, (int)Math.Round(panel.Width)); var panelHeight = Math.Max(0, (int)Math.Round(panel.Height));
            RaylibApi.DrawRectangle(panelX, panelY, panelWidth, panelHeight, ParseThemeColor(theme.Surface));
            RaylibApi.DrawRectangleLines(panelX, panelY, panelWidth, panelHeight, new Color(106, 139, 174, 255));
        }
        else if (item.Kind == "Separator") RaylibApi.DrawRectangle(x, y, width, height, new Color(75, 91, 112, 255));
        if (!string.IsNullOrWhiteSpace(item.Text))
        {
            var fontSize = Math.Max(1, (int)Math.Round(item.FontSize));
            var textColor = item.Variant == "error" ? ParseThemeColor(theme.Error) : item.Enabled ? ParseThemeColor(theme.Foreground) : Color.LightGray;
            var lines = item.Text.Split('\n'); var lineHeight = Math.Max(fontSize, (int)Math.Round(item.LineHeight));
            var textY = y + (interactive ? Math.Max(8, (height - lines.Length * lineHeight) / 2) : 0);
            for (var line = 0; line < lines.Length; line++) RaylibApi.DrawTextEx(RaylibApi.GetFontDefault(), lines[line], new System.Numerics.Vector2(x + (interactive ? 18 : 0), textY + line * lineHeight), fontSize, (float)item.LetterSpacing, textColor);
        }
        if (item.Kind == "Progress" && item.Value is { } progress) { RaylibApi.DrawRectangle(x, y + height - 8, width, 6, new Color(62, 75, 92, 255)); RaylibApi.DrawRectangle(x, y + height - 8, (int)(width * progress), 6, ParseThemeColor(theme.Accent)); }
        if (item.Kind == "Busy") { var pulse = item.Variant == "busy" ? item.Pulse : 1; RaylibApi.DrawCircle(x + 9, y + 10, 6 * (float)pulse, item.Variant == "error" ? Color.Orange : Color.Gold); }
    }

    private static Color ParseThemeColor(string value)
    {
        if (value.Length != 7 || value[0] != '#') return Color.White;
        return new(Convert.ToInt32(value.Substring(1, 2), 16), Convert.ToInt32(value.Substring(3, 2), 16), Convert.ToInt32(value.Substring(5, 2), 16), 255);
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
        public LocalReview(ReviewWorkbenchItem item) { Item = item; CurrentStatus = item.Status; }
        public ReviewWorkbenchItem Item { get; }
        public string CurrentStatus { get; set; } = string.Empty;
        public string? Decision { get; set; }
        public string Persistence { get; set; } = "none";
        public string PreviewStatus { get; set; } = "not launched from this review session";
        public Task<PreviewLaunchResult>? PreviewTask { get; set; }
        public bool IsUnresolved => Item.Status is "pending" or "changes-requested";
    }

    private sealed record DecisionCompletion(string Id, bool Success, string Message);
    private sealed record DecisionJob(string Id, string Decision, bool ReopenFirst);
    private sealed record DecisionResult(bool Success, string Message);
    private sealed record PreviewLaunchResult(bool Success, string Message);

    private sealed class ReviewDecisionQueue : IDisposable
    {
        private readonly string root;
        private readonly Channel<DecisionJob> jobs = Channel.CreateUnbounded<DecisionJob>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });
        private readonly ConcurrentQueue<DecisionCompletion> completions = new();
        private readonly Task worker;
        private int pending;

        public ReviewDecisionQueue(string root) { this.root = root; worker = ConsumeAsync(); }
        public int PendingCount => Volatile.Read(ref pending);
        public void Enqueue(string id, string decision, bool reopenFirst) { Interlocked.Increment(ref pending); jobs.Writer.TryWrite(new DecisionJob(id, decision, reopenFirst)); }
        public IEnumerable<DecisionCompletion> TakeCompletions() { while (completions.TryDequeue(out var completion)) yield return completion; }
        public async Task DrainAsync() { while (PendingCount > 0) await Task.Delay(15); }
        public Task<DecisionResult> RunControlAsync(IReadOnlyList<string> args) => RunEngineeringAsync(args);

        private async Task ConsumeAsync()
        {
            await foreach (var job in jobs.Reader.ReadAllAsync())
            {
                var result = job.ReopenFirst ? await ReopenAndRecordAsync(job) : await RunEngineeringAsync(["review", "record", job.Id, job.Decision]);
                completions.Enqueue(new DecisionCompletion(job.Id, result.Success, result.Message));
                Interlocked.Decrement(ref pending);
            }
        }

        private async Task<DecisionResult> ReopenAndRecordAsync(DecisionJob job)
        {
            var reopened = await RunEngineeringAsync(["review", "reopen", job.Id, "--reason", "explicit decision from current milestone review workbench"]);
            return reopened.Success ? await RunEngineeringAsync(["review", "record", job.Id, job.Decision]) : reopened;
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
