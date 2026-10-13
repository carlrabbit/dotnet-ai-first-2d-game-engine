using Raylib_cs;
using RaylibApi = Raylib_cs.Raylib;
using System.Text.Json;
using System.Runtime.InteropServices;
using Agentic2D.Tools;
using Agentic2D.UI;

namespace Agentic2D.DebugClient.Raylib;

/// <summary>Minimal product-facing window owned by the isolated raylib adapter.</summary>
public static class RaylibGameWindow
{
    public sealed record ReviewWorkbenchItem(string Id, string Subject, string Status);

    public static Task<int> ShowReviewWorkbench(string milestone, IReadOnlyList<ReviewWorkbenchItem> items, int? autoCloseAfterFrames = null, string? capturePath = null, string? inputProbeReportPath = null) => ShowM053ReviewWorkbench(milestone, items, autoCloseAfterFrames, capturePath, inputProbeReportPath);

    private static async Task<int> ShowM053ReviewWorkbench(string milestone, IReadOnlyList<ReviewWorkbenchItem> items, int? autoCloseAfterFrames, string? capturePath, string? inputProbeReportPath)
    {
        if (items.Count == 0) return 0;
        var root = FindRepositoryRoot();
        var queue = new ReviewDecisionQueue(items.Count, operation => ExecuteDecisionAsync(root, operation));
        var local = items.Select(item => new LocalReview(item)).ToArray();
        var index = Array.FindIndex(local, item => item.IsUnresolved); if (index < 0) index = 0;
        var finalPage = !local.Any(item => item.IsUnresolved); var resetting = false; var resetError = string.Empty; var lastAction = "No decision yet"; var closeLifecycle = new ReviewWorkbenchCloseLifecycle();
        Task<ReviewResetResult>? resetTask = null;
        InitDesktopWindow("Agentic2D — Review Workbench");
        if (!RaylibApi.IsWindowReady()) { Console.Error.WriteLine("review workbench could not initialize the Raylib window"); return 1; }
        try
        {
            RaylibApi.SetTargetFPS(60);
            RaylibApi.SetExitKey(KeyboardKey.Null);
            var viewport = CurrentViewport();
            var definition = Compose();
            var session = new UiSession(definition, viewport);
            var observedActions = new List<string>();
            void WriteProbe(string status)
            {
                if (inputProbeReportPath is null) return;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(inputProbeReportPath))!);
                File.WriteAllText(inputProbeReportPath, JsonSerializer.Serialize(new
                {
                    status,
                    processId = Environment.ProcessId,
                    windowHandle = CurrentWindowHandle(),
                    viewport = session.Layout.Viewport,
                    currentItem = local[index].Item.Id,
                    currentOrdinal = index + 1,
                    observedActions,
                    mousePosition = RaylibApi.GetMousePosition(),
                    windowFocused = RaylibApi.IsWindowFocused(),
                    controls = session.Layout.Elements.Where(element => element.Enabled && element.ActionId is "review.previous" or "review.next")
                        .Select(element => new { element.Id, element.ActionId, element.PhysicalBounds }).ToArray()
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
            WriteProbe("ready");
            for (var frame = 0; RaylibApi.IsWindowReady() && (!autoCloseAfterFrames.HasValue || frame < autoCloseAfterFrames.Value); frame++)
            {
                if (RaylibApi.WindowShouldClose()) closeLifecycle.RequestClose();
                var currentViewport = CurrentViewport();
                if (currentViewport != viewport) { viewport = currentViewport; session.Recompose(Compose(), viewport); }
                foreach (var completion in queue.TakeCompletions())
                {
                    var target = local.FirstOrDefault(item => item.Item.Id == completion.Id);
                    if (target is not null)
                    {
                        target.Persistence = completion.Success ? "saved" : "failed: " + completion.Message;
                        target.Error = completion.Success ? string.Empty : completion.Message;
                        if (completion.Success && target.Decision is not null) target.CurrentStatus = target.Decision == "Accepted" ? "approved" : "changes-requested";
                    }
                }
                foreach (var item in local.Where(item => item.PreviewTask is { IsCompleted: true }))
                {
                    var result = item.PreviewTask!.Result; item.PreviewStatus = result.Success ? result.Message : "failed: " + result.Message; item.PreviewTask = null;
                }
                if (resetTask is { IsCompleted: true })
                {
                    var result = resetTask.Result; resetTask = null; resetting = false;
                    if (!result.Success) { resetError = result.Message; closeLifecycle.CancelRequest(); lastAction = "Review reset failed"; }
                    else
                    {
                        local = result.Items.Select(item => new LocalReview(item)).ToArray();
                        if (local.Length == 0) { resetError = "Canonical review reload returned no registered reviews."; closeLifecycle.CancelRequest(); }
                        else
                        {
                            await queue.CompleteAsync();
                            queue = new ReviewDecisionQueue(local.Length, operation => ExecuteDecisionAsync(root, operation));
                            index = Array.FindIndex(local, item => item.IsUnresolved); if (index < 0) index = 0; finalPage = !local.Any(item => item.IsUnresolved); resetError = string.Empty; lastAction = "Review set reset; question 1 of " + local.Length; session = new UiSession(Compose(), viewport);
                        }
                    }
                }
                if (closeLifecycle.ShouldExit(queue.PendingCount, queue.FailedOperations.Count, resetting)) break;
                if (!RaylibApi.IsWindowFocused() && session.CapturedId is not null) session.Dispatch(new UiInputEvent("pointer-cancel"));
                session.Dispatch(new UiInputEvent("time", Delta: RaylibApi.GetFrameTime()));
                var escapePressed = RaylibApi.IsKeyPressed(KeyboardKey.Escape);
                if (escapePressed && !session.Dispatch(new UiInputEvent("key", Key: "Escape"))) closeLifecycle.RequestClose();
                RaylibWorkbenchInput.Dispatch(session, allowEscape: false);
                foreach (var request in session.DrainActions())
                {
                    observedActions.Add(request.ActionId + "@" + local[index].Item.Id);
                    switch (request.ActionId)
                    {
                        case "review.previous": if (!finalPage && !resetting && !closeLifecycle.BlocksNewActions) index = (index + local.Length - 1) % local.Length; break;
                        case "review.next": if (!finalPage && !resetting && !closeLifecycle.BlocksNewActions) index = (index + 1) % local.Length; break;
                        case "review.restart":
                            if (!resetting && !closeLifecycle.BlocksNewActions) { resetting = true; finalPage = false; resetError = string.Empty; lastAction = "Restarting review and draining pending decisions"; resetTask = ResetAsync(); }
                            break;
                        case "review.preview":
                            if (!finalPage && !resetting && !closeLifecycle.BlocksNewActions && milestone is "M048" or "M053") { var item = local[index]; if (item.PreviewTask is null) { item.PreviewStatus = "launching actual candidate preview"; item.PreviewTask = milestone == "M048" ? LaunchM048PreviewAsync(item.Item.Id) : LaunchM053ExperienceAsync(item.Item.Id); } }
                            break;
                        case "review.stress":
                            if (!finalPage && !resetting && !closeLifecycle.BlocksNewActions && milestone == "M052") { var item = local[index]; if (item.PreviewTask is null) { item.PreviewStatus = "launching actual UI stress screen"; item.PreviewTask = LaunchUiStressWindowAsync(); } }
                            break;
                        case "review.retry":
                            if (!resetting)
                            {
                                var retryIds = queue.FailedOperations.Select(operation => operation.Id).ToHashSet(StringComparer.Ordinal);
                                var retryCount = queue.RetryFailed();
                                foreach (var item in local.Where(item => retryIds.Contains(item.Item.Id))) item.Persistence = "retrying";
                                lastAction = $"Retrying {retryCount} failed decision saves";
                            }
                            break;
                        case "review.close":
                            closeLifecycle.RequestClose();
                            break;
                        case "review.accept":
                        case "review.reject":
                            if (!resetting && !closeLifecycle.BlocksNewActions && !finalPage && local[index].IsUnresolved && local[index].Decision is null)
                            {
                                var item = local[index]; item.Decision = request.ActionId == "review.accept" ? "Accepted" : "Rejected"; item.Persistence = "saving";
                                lastAction = $"Question {index + 1}: {item.Decision} — saving";
                                if (!queue.TryEnqueue(new ReviewDecisionOperation(item.Item.Id, item.Decision == "Accepted" ? "approved" : "changes-requested"))) { item.Persistence = "failed: Decision queue is full or the item already has an accepted operation."; item.Error = "Decision queue is full or the item already has an accepted operation."; }
                                var next = NextUndecided(local, index); if (next < 0) finalPage = true; else index = next;
                            }
                            break;
                    }
                }
                definition = Compose(); session.Recompose(definition, currentViewport);
                WriteProbe("ready");
                RaylibWorkbenchInput.Render(session);
                if (capturePath is not null && frame == 2)
                {
                    var absoluteCapture = Path.GetFullPath(capturePath); Directory.CreateDirectory(Path.GetDirectoryName(absoluteCapture)!);
                    RaylibApi.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), absoluteCapture));
                }
            }
            return 0;
        }
        finally
        {
            await queue.CompleteAsync();
            if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow();
        }

        UiDefinition Compose()
        {
            var item = local[index];
            var failed = queue.FailedOperations.ToDictionary(operation => operation.Id, StringComparer.Ordinal);
            var summaries = local.Select((entry, ordinal) => new M053WorkbenchScreens.ReviewDecisionSummary(ordinal + 1,
                entry.Decision ?? (entry.CurrentStatus == "approved" ? "Accepted" : entry.CurrentStatus == "changes-requested" ? "Rejected" : ""),
                entry.Persistence == "saving" || entry.Persistence == "retrying" ? $"Saving ({queue.PendingCount} pending)" : entry.Persistence switch { "saved" => "Saved", _ when entry.Persistence.StartsWith("failed", StringComparison.Ordinal) => "Failed", _ => "Not written" },
                entry.Error)).ToArray();
            var allSaved = queue.PendingCount == 0 && failed.Count == 0 && local.Where(entry => entry.Decision is not null).All(entry => entry.Persistence == "saved");
            var status = failed.Count > 0 ? "failed" : item.Persistence;
            var feedback = resetting ? (closeLifecycle.IsRequested ? "Waiting for the active review reset to finish before closing." : lastAction) : !string.IsNullOrWhiteSpace(resetError) ? "Review reset failed: " + resetError : failed.Count > 0 ? string.Join("; ", failed.Select(pair => $"{pair.Key}: {local.First(entry => entry.Item.Id == pair.Key).Error}")) : $"{lastAction}; pending {queue.PendingCount}; current item {item.Persistence}.";
            return M053WorkbenchScreens.Review(new(milestone, item.Item.Id, item.Item.Subject, index + 1, local.Length,
                item.CurrentStatus, status, feedback, item.IsUnresolved && item.Decision is null && !resetting && !closeLifecycle.BlocksNewActions,
                failed.Count > 0, finalPage && allSaved,
                milestone == "M048" || milestone == "M053" && (item.Item.Id.EndsWith("asset-workbench", StringComparison.Ordinal) || item.Item.Id.Contains("preview", StringComparison.Ordinal)), milestone == "M052", finalPage, resetting,
                summaries, queue.PendingCount, closeLifecycle.IsRequested));
        }
        int NextUndecided(IReadOnlyList<LocalReview> reviews, int current)
        {
            for (var offset = 1; offset <= reviews.Count; offset++) { var candidate = (current + offset) % reviews.Count; if (reviews[candidate].IsUnresolved && reviews[candidate].Decision is null) return candidate; }
            return -1;
        }
        async Task<ReviewResetResult> ResetAsync()
        {
            await queue.WaitForIdleAsync();
            if (queue.FailedOperations.Count > 0) return new(false, "Review reset stopped because decision persistence failed. Retry failed saves before restarting.", []);
            var reset = await RunEngineeringCommandAsync(root, ["review", "reset", "--milestone", milestone]);
            if (!reset.Success) return new(false, reset.Error, []);
            var snapshot = await RunEngineeringCommandAsync(root, ["review", "snapshot", "--milestone", milestone]);
            if (!snapshot.Success) return new(false, snapshot.Error, []);
            try
            {
                var reloaded = JsonSerializer.Deserialize<List<ReviewWorkbenchItem>>(snapshot.Output, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
                if (reloaded.Count == 0 || reloaded.Any(review => string.IsNullOrWhiteSpace(review.Id) || review.Status is not ("pending" or "changes-requested"))) return new(false, "Canonical review reload returned an invalid or non-undecided review set.", []);
                return new(true, "Active milestone review state reset and reloaded.", reloaded);
            }
            catch (JsonException exception) { return new(false, "Could not parse canonical review reload: " + exception.Message, []); }
        }
    }

    private static unsafe long CurrentWindowHandle() => (long)(nint)RaylibApi.GetWindowHandle();

    private static async Task<PreviewLaunchResult> LaunchM048PreviewAsync(string reviewId)
    {
        var root = FindRepositoryRoot();
        if (!M048ReviewExperienceRegistry.TryResolve(reviewId, root, out var fixture, out var error)) return new(false, error);
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("run"); start.ArgumentList.Add("--no-build"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib")); start.ArgumentList.Add("--"); start.ArgumentList.Add("asset-preview"); start.ArgumentList.Add("--scene"); start.ArgumentList.Add(fixture!.ScenePath);
        return await RunPreviewProcessAsync(start, "actual candidate preview");
    }

    private static async Task<PreviewLaunchResult> LaunchM053ExperienceAsync(string reviewId)
    {
        var root = FindRepositoryRoot();
        var nativeRoot = Path.Combine(root, "artifacts", "ui", "M053", "native");
        var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib");
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("run"); start.ArgumentList.Add("--no-build"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(project); start.ArgumentList.Add("--");
        if (reviewId.Contains("asset-workbench", StringComparison.Ordinal))
        {
            var session = Path.Combine(nativeRoot, "workbench-fixture", "opened", "review-session.json");
            if (!File.Exists(session)) return new(false, "M053 asset-workbench fixture is unavailable; rerun m053-workbenches native validation.");
            start.ArgumentList.Add("asset-workbench"); start.ArgumentList.Add("--session"); start.ArgumentList.Add(session); start.ArgumentList.Add("--commands"); start.ArgumentList.Add(Path.Combine(nativeRoot, "workbench-fixture", "review-commands.jsonl"));
            return await RunPreviewProcessAsync(start, "M053 asset workbench experience");
        }
        var kind = reviewId.Contains("image", StringComparison.Ordinal) ? "image" : reviewId.Contains("animation", StringComparison.Ordinal) ? "animation" : reviewId.Contains("audio", StringComparison.Ordinal) ? "audio" : null;
        if (kind is null) return new(false, "No M053 application experience is registered for this review item.");
        var scene = Path.Combine(nativeRoot, "preview-fixtures", kind, "artifacts", "assets", "M048", "fixture", kind, "review-bundle", "preview-scene.json");
        if (!File.Exists(scene)) return new(false, "M053 " + kind + " preview fixture is unavailable; rerun m053-workbenches native validation.");
        start.ArgumentList.Add("asset-preview"); start.ArgumentList.Add("--scene"); start.ArgumentList.Add(scene);
        return await RunPreviewProcessAsync(start, "M053 " + kind + " candidate preview");
    }

    private static async Task<PreviewLaunchResult> LaunchUiStressWindowAsync()
    {
        var root = FindRepositoryRoot();
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("run"); start.ArgumentList.Add("--no-build"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib")); start.ArgumentList.Add("--"); start.ArgumentList.Add("ui-stress");
        return await RunPreviewProcessAsync(start, "actual UI stress screen");
    }

    private static async Task<PreviewLaunchResult> RunPreviewProcessAsync(System.Diagnostics.ProcessStartInfo start, string label)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException(label + " process did not start");
            var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
            await Task.WhenAll(process.WaitForExitAsync(), stdout, stderr);
            return process.ExitCode == 0 ? new(true, "launched / completed") : new(false, label + " exited abnormally: " + stderr.Result.Trim());
        }
        catch (Exception exception) { return new(false, label + " could not be launched: " + exception.Message); }
    }

    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory)) { if (File.Exists(Path.Combine(directory, "dotnet-ai-first-2d-game-engine.slnx"))) return directory; directory = Directory.GetParent(directory)?.FullName; }
        return Directory.GetCurrentDirectory();
    }

    public static unsafe void ShowProductShell(string title, IReadOnlyList<MenuItem> menu, string output, Action<MenuItem>? dispatchAction = null, int? autoCloseAfterFrames = null, string? capturePath = null, string? inputProbeReportPath = null)
    {
        InitDesktopWindow(title + " — Main Menu");
        if (!RaylibApi.IsWindowReady()) throw new InvalidOperationException("Raylib failed to create a graphical Product Shell window.");
        var nativeWindowHandle = (long)(nint)RaylibApi.GetWindowHandle();
        var definition = M052UiScreens.ProductShell(menu);
        var session = new UiSession(definition, CurrentViewport());
        var lastViewport = CurrentViewport();
        var shouldExit = false;
        var observed = new List<UiActionRequest>();
        var inputTrace = new List<object>();
        if (inputProbeReportPath is not null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(inputProbeReportPath))!);
            File.WriteAllText(inputProbeReportPath, JsonSerializer.Serialize(new
            {
                status = "ready",
                windowHandle = nativeWindowHandle,
                viewport = session.Layout.Viewport,
                layoutFingerprint = session.Layout.Fingerprint,
                focus = session.FocusedId,
                captured = session.CapturedId,
                menu = menu.Select(x => new { x.Id, x.Label, x.Available }).ToArray(),
                controls = session.Layout.Elements.Where(x => x.Focusable && x.Enabled).OrderBy(x => x.LogicalBounds.Y).ThenBy(x => x.Order).Select(x => new { x.Id, x.ActionId, x.PhysicalBounds }).ToArray(),
                inputTrace
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
                if (RaylibApi.IsMouseButtonPressed(MouseButton.Left)) { session.Dispatch(new UiInputEvent("pointer-down", mouse.X, mouse.Y)); inputTrace.Add(new { kind = "pointer-down", x = mouse.X, y = mouse.Y, session.CapturedId }); }
                if (RaylibApi.IsMouseButtonReleased(MouseButton.Left)) { session.Dispatch(new UiInputEvent("pointer-up", mouse.X, mouse.Y)); inputTrace.Add(new { kind = "pointer-up", x = mouse.X, y = mouse.Y, session.FocusedId, session.CapturedId }); }
                if (RaylibApi.IsKeyPressed(KeyboardKey.Down)) { session.Dispatch(new UiInputEvent("key", Key: "Down")); inputTrace.Add(new { kind = "key", key = "Down", session.FocusedId }); }
                if (RaylibApi.IsKeyPressed(KeyboardKey.Up)) { session.Dispatch(new UiInputEvent("key", Key: "Up")); inputTrace.Add(new { kind = "key", key = "Up", session.FocusedId }); }
                if (RaylibApi.IsKeyPressed(KeyboardKey.Tab)) { session.Dispatch(new UiInputEvent("key", Key: RaylibApi.IsKeyDown(KeyboardKey.LeftShift) ? "Up" : "Tab")); inputTrace.Add(new { kind = "key", key = "Tab", session.FocusedId }); }
                if (RaylibApi.IsKeyPressed(KeyboardKey.Enter) || RaylibApi.IsKeyPressed(KeyboardKey.KpEnter)) { session.Dispatch(new UiInputEvent("key", Key: "Enter")); inputTrace.Add(new { kind = "key", key = "Enter", session.FocusedId }); }
                if (RaylibApi.IsKeyPressed(KeyboardKey.Space)) { session.Dispatch(new UiInputEvent("key", Key: "Space")); inputTrace.Add(new { kind = "key", key = "Space", session.FocusedId }); }
                if (RaylibApi.IsKeyPressed(KeyboardKey.Left)) { session.Dispatch(new UiInputEvent("key", Key: "Left")); inputTrace.Add(new { kind = "key", key = "Left", session.FocusedId }); }
                if (RaylibApi.IsKeyPressed(KeyboardKey.Right)) { session.Dispatch(new UiInputEvent("key", Key: "Right")); inputTrace.Add(new { kind = "key", key = "Right", session.FocusedId }); }
                if (RaylibApi.IsKeyPressed(KeyboardKey.Backspace)) { session.Dispatch(new UiInputEvent("key", Key: "Backspace")); inputTrace.Add(new { kind = "key", key = "Backspace", session.FocusedId }); }
                if (RaylibApi.IsKeyPressed(KeyboardKey.Delete)) { session.Dispatch(new UiInputEvent("key", Key: "Delete")); inputTrace.Add(new { kind = "key", key = "Delete", session.FocusedId }); }
                if (RaylibApi.IsKeyPressed(KeyboardKey.Escape)) { shouldExit = true; inputTrace.Add(new { kind = "key", key = "Escape", session.FocusedId }); }
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
                if (inputProbeReportPath is not null && (inputTrace.Count > 0 || observed.Count > 0))
                    File.WriteAllText(inputProbeReportPath, JsonSerializer.Serialize(new { status = observed.Count > 0 ? "observed" : "ready", windowHandle = nativeWindowHandle, viewport = session.Layout.Viewport, layoutFingerprint = session.Layout.Fingerprint, focus = session.FocusedId, captured = session.CapturedId, inputTrace, requests = observed }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
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

    internal static unsafe void InitDesktopWindow(string title)
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

    internal static UiViewport CurrentViewport()
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
        public LocalReview(ReviewWorkbenchItem item) { Item = item; CurrentStatus = item.Status; if (item.Status == "approved") { Decision = "Accepted"; Persistence = "saved"; } }
        public ReviewWorkbenchItem Item { get; }
        public string CurrentStatus { get; set; } = string.Empty;
        public string? Decision { get; set; }
        public string Persistence { get; set; } = "none";
        public string Error { get; set; } = string.Empty;
        public string PreviewStatus { get; set; } = "not launched from this review session";
        public Task<PreviewLaunchResult>? PreviewTask { get; set; }
        public bool IsUnresolved => CurrentStatus is "pending" or "changes-requested";
    }

    private sealed record ReviewResetResult(bool Success, string Message, IReadOnlyList<ReviewWorkbenchItem> Items);
    private sealed record EngineeringCommandResult(bool Success, string Output, string Error);
    private sealed record PreviewLaunchResult(bool Success, string Message);

    private static async Task<ReviewDecisionOperationResult> ExecuteDecisionAsync(string root, ReviewDecisionOperation operation)
    {
        var decision = await RunEngineeringCommandAsync(root, ["review", "decide", operation.Id, operation.Decision]);
        return new ReviewDecisionOperationResult(decision.Success, decision.Success ? decision.Output.Trim() : decision.Error.Trim());
    }

    private static async Task<EngineeringCommandResult> RunEngineeringCommandAsync(string root, IReadOnlyList<string> args)
    {
        var project = Path.Combine(root, "src", "Agentic2D.Engineering");
        var start = new System.Diagnostics.ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("run"); start.ArgumentList.Add("--no-build"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(project); start.ArgumentList.Add("--");
        foreach (var arg in args) start.ArgumentList.Add(arg);
        try
        {
            using var process = System.Diagnostics.Process.Start(start) ?? throw new InvalidOperationException("engineering process did not start");
            var output = process.StandardOutput.ReadToEndAsync(); var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync().ConfigureAwait(false); await Task.WhenAll(output, error).ConfigureAwait(false);
            return process.ExitCode == 0 ? new(true, output.Result, string.Empty) : new(false, output.Result, error.Result.Trim());
        }
        catch (Exception exception) { return new(false, string.Empty, exception.Message); }
    }
}
