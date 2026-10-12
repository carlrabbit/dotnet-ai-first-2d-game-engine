using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using Agentic2D.UI;
using Agentic2D.Tools;

namespace Agentic2D.Engineering;

public static class M052AgentOrientedUiSuite
{
    public static readonly string[] Shards = ["headless-layout", "headless-interaction", "windows-raylib", "compatibility"];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<int> RunAsync(EngineeringHost host, string root, string shard, TextWriter diagnostics)
    {
        if (!Shards.Contains(shard, StringComparer.Ordinal)) throw new EngineeringException($"unsupported internal shard: m052-ui/{shard}");
        var output = Path.Combine(root, "artifacts", "ui", "M052"); Directory.CreateDirectory(output);
        object evidence = shard switch
        {
            "headless-layout" => await LayoutAsync(root, output),
            "headless-interaction" => Interaction(output),
            "windows-raylib" => await RaylibAsync(root, output, diagnostics),
            "compatibility" => Compatibility(),
            _ => throw new InvalidOperationException()
        };
        var path = Path.Combine(output, shard + ".json"); await File.WriteAllTextAsync(path, JsonSerializer.Serialize(evidence, Json));
        return 0;
    }

    private static async Task<object> LayoutAsync(string root, string output)
    {
        var consumer = Path.Combine(root, "consumers", "examples", "Agentic2D.UiConsumer", "Agentic2D.UiConsumer.csproj");
        var compile = await Run(root, "dotnet", ["build", consumer, "--configuration", "Debug", "--nologo"]);
        if (compile.ExitCode != 0) throw new InvalidDataException("Independent typed UI consumer example did not compile: " + compile.Output);
        var uiProject = Path.Combine(root, "src", "Agentic2D.UI", "Agentic2D.UI.csproj");
        var uiProjectText = await File.ReadAllTextAsync(uiProject);
        var raylibReferences = typeof(UiDefinition).Assembly.GetReferencedAssemblies().Where(x => x.Name?.Contains("raylib", StringComparison.OrdinalIgnoreCase) == true).Select(x => x.Name).ToArray();
        if (uiProjectText.Contains("raylib-cs", StringComparison.OrdinalIgnoreCase) || raylibReferences.Length != 0) throw new InvalidDataException("The shared UI project references native Raylib code.");
        var menu = M052UiScreens.ProductShell(ProductMenus.Main(false));
        var operation = new UiOperationProjection("operation.review", AgentUiOperationStatus.Working, "Rendering candidate preview");
        var stress = M052UiScreens.ReviewStress(operation);
        var matrix = new (int Width, int Height, double Scale, double Dpi, string Label)[]
        {
            (1280, 720, 1, 1, "hd-minimum"), (1920, 1080, 1, 1, "full-hd-reference"),
            (2560, 1440, 1.25, 1, "qhd-fractional-scale"), (3840, 2160, 2, 1, "4k-200-percent"),
            (3440, 1440, 1.5, 1, "ultrawide-fractional-scale"), (2560, 1440, 1, 1.25, "qhd-os-dpi")
        };
        var results = new List<object>(); var fingerprints = new List<string>();
        foreach (var item in matrix)
        {
            var viewport = new UiViewport(item.Width, item.Height, item.Scale, item.Dpi);
            var menuLayout = UiLayoutEngine.Resolve(menu, viewport); var stressLayout = UiLayoutEngine.Resolve(stress, viewport);
            if (!menuLayout.IsValid || !stressLayout.IsValid) throw new InvalidDataException($"Supported viewport {item.Label} produced diagnostics: {string.Join(";", menuLayout.Diagnostics.Concat(stressLayout.Diagnostics).Select(x => $"{x.Code}:{x.ElementId} measured={x.Measured} available={x.Available}"))}");
            var menuSemantics = menuLayout.Elements.Where(x => x.Focusable).Select(x => new { x.Id, x.ActionId, x.LogicalBounds.Width, x.LogicalBounds.Height }).ToArray();
            results.Add(new { item.Label, viewport, productShell = menuLayout, stress = stressLayout, focusableControlCount = menuSemantics.Length });
            fingerprints.Add(menuLayout.Fingerprint);
        }
        var repeatA = UiLayoutEngine.Resolve(menu, new UiViewport(1920, 1080)); var repeatB = UiLayoutEngine.Resolve(menu, new UiViewport(1920, 1080));
        if (repeatA.Fingerprint != repeatB.Fingerprint) throw new InvalidDataException("Equivalent definitions did not produce the same normalized layout fingerprint.");
        var metrics = Ui.Theme.DefaultMetrics;
        var metricEvidence = new { miAt10 = metrics.Measure("Mi", 10), wwAt10 = metrics.Measure("WW", 10), unsupportedGlyphMatchesFallback = metrics.Measure("🚀", 18) == metrics.Measure("?", 18), multilineMaximum = metrics.Measure("WW\ni", 10), wrappedUnicode = metrics.Wrap("WWWW\ni🚀", 15, 10) };
        if (metricEvidence.miAt10 != 9 || metricEvidence.wwAt10 != 15 || !metricEvidence.unsupportedGlyphMatchesFallback || metricEvidence.multilineMaximum != 15 || metricEvidence.wrappedUnicode.Count != 3 || metricEvidence.wrappedUnicode[2] != "i?") throw new InvalidDataException("Pinned Raylib 6.0 font metrics/fallback/wrap behavior diverged from native font projection policy.");
        var broken = UiDefinition.Normalize("negative.small", Ui.Screen("small.root", Ui.Text("small.text", "This desktop interface cannot fit a phone viewport.")));
        var negative = UiLayoutEngine.Resolve(broken, new UiViewport(640, 360));
        if (negative.IsValid || !negative.Diagnostics.Any(x => x.Code == "UI2002")) throw new InvalidDataException("Undersized negative viewport was not diagnosed.");
        var overlap = UiDefinition.Normalize("negative.overlap", Ui.Screen("overlap.root", Ui.Grid("overlap.grid", [new(UiLength.Grow())], [new(UiLength.Grow())], Ui.Button("overlap.first", "First", "overlap.first"), Ui.Button("overlap.second", "Second", "overlap.second"))));
        var overlapA = overlap.Root.Items[0].Items[0] with { Column = 0, Row = 0 };
        var overlapB = overlap.Root.Items[0].Items[1] with { Column = 0, Row = 0 };
        overlap = UiDefinition.Normalize("negative.overlap", Ui.Screen("overlap.root", Ui.Grid("overlap.grid", [new(UiLength.Grow())], [new(UiLength.Grow())], overlapA, overlapB)));
        var overlapResult = UiLayoutEngine.Resolve(overlap, new UiViewport(1920, 1080));
        if (!overlapResult.Diagnostics.Any(x => x.Code == "UI2001" && x.ElementId == "overlap.second")) throw new InvalidDataException("Unintended interactive overlap was not attributed to its element.");
        var clipped = UiDefinition.Normalize("positive.clip", Ui.Screen("clip.root", Ui.Scroll("clip.scroll", Ui.Column("clip.content", Ui.Text("clip.large", "Scrollable text", new UiSizeSpec(UiLength.Grow(), UiLength.Fixed(900), MinHeight: 900))))));
        var clippedResult = UiLayoutEngine.Resolve(clipped, new UiViewport(1280, 720));
        if (!clippedResult.IsValid || clippedResult.Elements.Single(x => x.Id == "clip.large").VisibleBounds.Height >= clippedResult.Elements.Single(x => x.Id == "clip.large").LogicalBounds.Height) throw new InvalidDataException("Intentional scroll clipping was not preserved as valid clipped geometry.");
        var overlayDefinition = UiDefinition.Normalize("positive.overlay", Ui.Screen("overlay.root", Ui.Overlay("overlay.stack", Ui.Button("overlay.first", "First", "overlay.first"), Ui.Button("overlay.second", "Second", "overlay.second"))));
        var overlayResult = UiLayoutEngine.Resolve(overlayDefinition, new UiViewport(1920, 1080));
        if (overlayResult.Diagnostics.Any(x => x.Code == "UI2001")) throw new InvalidDataException("Declared overlay overlap was classified as an error.");
        var infeasible = UiDefinition.Normalize("negative.overflow", Ui.Screen("overflow.root", Ui.Column("overflow.column", Ui.Text("overflow.essential", "Essential content", new UiSizeSpec(UiLength.Grow(), UiLength.Fixed(900), MinHeight: 900)) with { Essential = true }, Ui.Button("overflow.a", "One", "overflow.one"))));
        var overflow = UiLayoutEngine.Resolve(infeasible, new UiViewport(1280, 720));
        if (overflow.IsValid || !overflow.Diagnostics.Any(x => x.Code is "UI2004" or "UI2006")) throw new InvalidDataException("Infeasible supported viewport constraints were accepted.");
        var artifact = new { schema = "agentic2d.m052.headless-layout.v1", compiledConsumer = consumer, compilerOutput = compile.Output, resolutionMatrix = results, equivalentFingerprint = repeatA.Fingerprint, deterministicRepeat = repeatA.Fingerprint == repeatB.Fingerprint, metricEvidence, undersizedNegative = negative, infeasibleSupportedLayout = overflow, unintendedOverlap = overlapResult, intentionalOverlay = overlayResult, intentionalScrollClip = clippedResult, headlessOnly = true, noRaylibDependencyInUiProject = raylibReferences.Length == 0 && !uiProjectText.Contains("raylib-cs", StringComparison.OrdinalIgnoreCase) };
        await File.WriteAllTextAsync(Path.Combine(output, "headless-layout-detail.json"), JsonSerializer.Serialize(artifact, Json));
        return artifact;
    }

    private static object Interaction(string output)
    {
        var baseScreen = InteractionDefinition(false, true);
        var session = new UiSession(baseScreen, new UiViewport(1920, 1080));
        var elements = session.Layout.Elements.ToDictionary(x => x.Id, StringComparer.Ordinal);
        var field = elements["interaction.notes"]; Click(session, field);
        session.Dispatch(new UiInputEvent("text", Text: "draft"));
        var scroll = session.Layout.Elements.Single(x => x.Id == "interaction.scroll"); var p = scroll.LogicalBounds; session.Dispatch(new UiInputEvent("scroll", p.X + 1, p.Y + 1, Delta: 30));
        var initialFocus = session.FocusedId; var before = session.Snapshot();
        var recompose = InteractionDefinition(false, true) with { Id = "interaction.recomposed" };
        session.Recompose(recompose, new UiViewport(1920, 1080));
        var after = session.Snapshot();
        if (after.FocusedId != initialFocus || after.Drafts.GetValueOrDefault("interaction.notes") != "draft" || after.ScrollOffsets.GetValueOrDefault("interaction.scroll") != before.ScrollOffsets.GetValueOrDefault("interaction.scroll")) throw new InvalidDataException("Compatible recomposition lost focus, draft or scroll state.");
        var button = session.Layout.Elements.Single(x => x.Id == "interaction.action"); Click(session, button);
        var firstActions = session.DrainActions(); if (firstActions.Count != 1 || firstActions[0].ActionId != "semantic.save" || firstActions[0].SourceId != "interaction.action") throw new InvalidDataException("Pointer press/release did not emit exactly one typed action.");
        session.Dispatch(new UiInputEvent("time", Delta: .5)); var time = session.PresentationTime;
        var captured = session.Layout.Elements.Single(x => x.Id == "interaction.cancel-target"); ClickDown(session, captured); session.Dispatch(new UiInputEvent("pointer-cancel"));
        var invalid = InteractionDefinition(false, false) with { Id = "interaction.disabled" }; session.Recompose(invalid, new UiViewport(1920, 1080));
        if (session.FocusedId == "interaction.action" || session.CapturedId is not null) throw new InvalidDataException("Disabled/replaced controls retained invalid focus or pointer capture.");
        var modal = InteractionDefinition(true, true) with { Id = "interaction.modal" }; session.Recompose(modal, new UiViewport(1920, 1080));
        var modalButton = session.Layout.Elements.Single(x => x.Id == "interaction.modal-confirm"); Click(session, modalButton);
        var modalActions = session.DrainActions();
        if (!modalActions.Any(x => x.ActionId == "semantic.confirm") || modalActions.Any(x => x.ActionId == "semantic.save")) throw new InvalidDataException("Modal input did not activate only its projected control.");
        session.Dispatch(new UiInputEvent("key", Key: "Escape"));
        if (session.DrainActions().SingleOrDefault()?.ActionId != "semantic.modal-cancel") throw new InvalidDataException("Modal Escape did not emit the application cancel request.");
        var expectedOperation = new UiOperationProjection("save", AgentUiOperationStatus.Working, "Saving");
        var operationWorking = UiDefinition.Normalize("operation.working", Ui.Screen("operation.root", Ui.Busy("operation.status", "Save status", expectedOperation), Ui.Progress("operation.progress", .7, "Saving")));
        var operationFailed = UiDefinition.Normalize("operation.failed", Ui.Screen("operation.root", Ui.Busy("operation.status", "Save status", expectedOperation with { Status = AgentUiOperationStatus.Failed, Message = "Disk unavailable", RetryActionId = "save.retry" }), Ui.Button("operation.retry", "Retry", "save.retry")));
        var statusProjection = new[] { UiLayoutEngine.Resolve(operationWorking, new UiViewport(1920, 1080)), UiLayoutEngine.Resolve(operationFailed, new UiViewport(1920, 1080)) };
        var operationController = new M052SyntheticReviewOperationController([AgentUiOperationStatus.Failed, AgentUiOperationStatus.Succeeded]);
        if (!operationController.Start() || operationController.Start() || operationController.Retry() || operationController.Status != AgentUiOperationStatus.Working || operationController.Attempt != 1 || !operationController.Advance(.35) || operationController.Status != AgentUiOperationStatus.Failed || !operationController.Retry() || !operationController.Advance(.35) || operationController.Status != AgentUiOperationStatus.Succeeded || operationController.Retry()) throw new InvalidDataException("Application-owned duplicate-safe failure/retry/success operation path did not complete as confirmed.");
        var cancelledOperation = new M052SyntheticReviewOperationController([AgentUiOperationStatus.Succeeded]);
        if (!cancelledOperation.Start() || !cancelledOperation.Cancel() || cancelledOperation.Status != AgentUiOperationStatus.Cancelled || cancelledOperation.Advance(.5) || !cancelledOperation.Retry() || !cancelledOperation.Advance(.35) || cancelledOperation.Status != AgentUiOperationStatus.Succeeded) throw new InvalidDataException("Cancellation and retry did not follow confirmed application-owned operation states.");
        var exhaustedOperation = new M052SyntheticReviewOperationController([AgentUiOperationStatus.Failed]);
        if (!exhaustedOperation.Start() || !exhaustedOperation.Advance(.35) || exhaustedOperation.Status != AgentUiOperationStatus.Failed || exhaustedOperation.Retry() || exhaustedOperation.Projection.RetryActionId is not null) throw new InvalidDataException("Exhausted failed operation exposed a stale retry action or false success.");
        var reduced = UiDefinition.Normalize("operation.reduced", Ui.Screen("operation.root", Ui.Busy("operation.status", "Save status", operationController.Projection)), reducedMotion: true);
        var reducedSession = new UiSession(reduced, new UiViewport(1920, 1080)); var reducedBefore = reducedSession.Snapshot(); reducedSession.Dispatch(new UiInputEvent("time", Delta: .5));
        var reducedMotionPreserved = reducedSession.PresentationSamples().Single(x => x.ElementId == "operation.status").Pulse == 1 && reducedSession.Snapshot().Actions.Count == reducedBefore.Actions.Count && operationController.Status == AgentUiOperationStatus.Succeeded;
        if (!reducedMotionPreserved) throw new InvalidDataException("Reduced-motion presentation changed operation state or emitted an operation action.");
        var report = new { schema = "agentic2d.m052.headless-interaction.v2", stableFocusAndDraftOnRecompose = after.FocusedId == initialFocus && after.Drafts.GetValueOrDefault("interaction.notes") == "draft", emittedAction = firstActions[0], captureCancelled = session.CapturedId is null, disabledFocusReleased = session.FocusedId != "interaction.action", modalActions, modalCancelRequest = "semantic.modal-cancel", scrollOffset = after.ScrollOffsets.GetValueOrDefault("interaction.scroll"), injectedPresentationTime = time, reducedMotionPreservedConfirmedOperation = reducedMotionPreserved, operationAttempts = operationController.Attempt, operationStates = statusProjection, operationLifecycle = new { failureThenRetrySuccess = operationController.Status, duplicateStartRejected = operationController.Attempt == 2, cancelledThenRetried = cancelledOperation.Status, exhaustedRetryRejected = exhaustedOperation.Projection.RetryActionId is null }, trace = session.Snapshot().Trace, final = session.ToEvidence() };
        File.WriteAllText(Path.Combine(output, "interaction-trace.json"), JsonSerializer.Serialize(report, Json));
        return report;
    }

    private static async Task<object> RaylibAsync(string root, string output, TextWriter diagnostics)
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("M052 native integration is required on active Windows; this run is not Windows.");
        var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib");
        var shellCapture = Path.Combine(output, "product-shell.png"); var stressCapture = Path.Combine(output, "review-stress.png");
        var pointerReport = Path.Combine(output, "product-shell-pointer-input.json");
        var keyboardReport = Path.Combine(output, "product-shell-keyboard-input.json");
        var pointer = await RunNativeShellProbe(root, project, shellCapture, pointerReport, pointer: true);
        var keyboard = await RunNativeShellProbe(root, project, shellCapture, keyboardReport, pointer: false);
        var stress = await Run(root, "dotnet", ["run", "--no-build", "--project", project, "--", "ui-stress", "--frames", "90", "--capture", stressCapture]);
        if (!File.Exists(stressCapture) || new FileInfo(stressCapture).Length < 128) { await diagnostics.WriteLineAsync(stress.Output); throw new InvalidDataException("Actual synthetic stress Raylib launch/capture failed."); }
        if (!pointer.Success || !keyboard.Success) throw new InvalidDataException("Native Product Shell input probe did not emit the expected projected action request.");
        return new { schema = "agentic2d.m052.windows-raylib.v2", platform = "windows", activePlatform = true, pointerProbe = pointer.Report, keyboardProbe = keyboard.Report, productShellCapture = new { path = shellCapture, bytes = new FileInfo(shellCapture).Length }, stressCapture = new { path = stressCapture, bytes = new FileInfo(stressCapture).Length, processOutput = stress.Output }, capturedFromRaylib = true, syntheticStress = new { operationSurfaceIsSynthetic = true, reviewPersistenceTouched = false } };
    }

    private static async Task<(bool Success, JsonElement Report)> RunNativeShellProbe(string root, string project, string capturePath, string reportPath, bool pointer)
    {
        if (File.Exists(reportPath)) File.Delete(reportPath);
        // Keep the Raylib process attached to the interactive desktop. CreateNoWindow can
        // place native windows outside the user's visible desktop even when WGL succeeds.
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var argument in new[] { "run", "--no-build", "--no-restore", "--project", project, "--", "shell", "--capture", capturePath, "--probe-report", reportPath }) start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Raylib Product Shell probe process did not start.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        var started = DateTime.UtcNow;
        while (DateTime.UtcNow - started < TimeSpan.FromSeconds(20))
        {
            if (process.HasExited) { await Task.WhenAll(stdout, stderr); throw new InvalidDataException("Raylib window exited before native input: " + stdout.Result + stderr.Result); }
            if (File.Exists(reportPath))
            {
                using var initialDocument = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
                if (initialDocument.RootElement.GetProperty("status").GetString() == "ready")
                {
                    var handle = (nint)initialDocument.RootElement.GetProperty("windowHandle").GetInt64();
                    if (handle == 0 || !IsWindow(handle) || !IsWindowVisible(handle)) throw new InvalidDataException("Raylib Product Shell reported a native handle that is not a visible desktop window.");
                    var controls = initialDocument.RootElement.GetProperty("controls").EnumerateArray().ToArray();
                    var targetIndex = Array.FindIndex(controls, item => item.GetProperty("actionId").GetString() == "shell.quit");
                    if (targetIndex < 0) throw new InvalidDataException("Native Product Shell projection has no enabled Quit control.");
                    var target = controls[targetIndex];
                    ShowWindow(handle, 9);
                    BringWindowToTop(handle);
                    if (!ActivateNativeWindow(handle)) throw new InvalidDataException("Windows could not activate the visible Product Shell window for native input.");
                    await Task.Delay(450);
                    if (pointer)
                    {
                        var bounds = target.GetProperty("physicalBounds");
                        var dpi = initialDocument.RootElement.GetProperty("viewport").GetProperty("osDpiScale").GetDouble();
                        var x = (int)Math.Round((bounds.GetProperty("x").GetDouble() + bounds.GetProperty("width").GetDouble() / 2) / dpi);
                        var y = (int)Math.Round((bounds.GetProperty("y").GetDouble() + bounds.GetProperty("height").GetDouble() / 2) / dpi);
                        var screen = new NativePoint { X = x, Y = y };
                        if (!ClientToScreen(handle, ref screen)) throw new InvalidOperationException("Could not map projected pointer position to the desktop.");
                        SetCursorPos(screen.X, screen.Y); await Task.Delay(150);
                        mouse_event(0x0002, 0, 0, 0, 0); await Task.Delay(150);
                        var releaseObserved = false;
                        for (var attempt = 0; attempt < 5 && !releaseObserved; attempt++)
                        {
                            mouse_event(0x0004, 0, 0, 0, 0); await Task.Delay(100);
                            if (!File.Exists(reportPath)) continue;
                            try
                            {
                                using var pointerDocument = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
                                releaseObserved = pointerDocument.RootElement.TryGetProperty("inputTrace", out var pointerTrace) && pointerTrace.EnumerateArray().Any(entry => entry.TryGetProperty("kind", out var kind) && kind.GetString() == "pointer-up");
                            }
                            catch (JsonException) { }
                        }
                        if (!releaseObserved) throw new InvalidDataException("Raylib did not observe pointer release after the projected target press.");
                    }
                    else
                    {
                        // Activate the native input queue on empty client space. Clicking
                        // a product action here would start a business operation before
                        // the keyboard traversal probe begins.
                        var neutralPoint = new NativePoint { X = 8, Y = 8 };
                        if (!ClientToScreen(handle, ref neutralPoint)) throw new InvalidOperationException("Could not map neutral keyboard-probe input to the desktop.");
                        SetCursorPos(neutralPoint.X, neutralPoint.Y); await Task.Delay(100); mouse_event(0x0002, 0, 0, 0, 0); await Task.Delay(100); mouse_event(0x0004, 0, 0, 0, 0);
                        await Task.Delay(200);
                        using var focusedDocument = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
                        var expectedFocus = focusedDocument.RootElement.GetProperty("focus").GetString();
                        for (var attempt = 0; attempt < controls.Length * 2 && expectedFocus != target.GetProperty("id").GetString(); attempt++)
                        {
                            if (GetForegroundWindow() != handle && !ActivateNativeWindow(handle)) throw new InvalidDataException("Windows lost the Product Shell foreground before native Tab traversal.");
                            SendNativeKey(0x09, keyUp: false); await Task.Delay(180); SendNativeKey(0x09, keyUp: true);
                            var focusDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(1);
                            while (DateTime.UtcNow < focusDeadline)
                            {
                                if (File.Exists(reportPath))
                                {
                                    using var focusDocument = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
                                    if (focusDocument.RootElement.TryGetProperty("focus", out var focusValue) && focusValue.ValueKind == JsonValueKind.String)
                                    {
                                        var observedFocus = focusValue.GetString();
                                        if (observedFocus != expectedFocus) { expectedFocus = observedFocus; break; }
                                    }
                                }
                                await Task.Delay(30);
                            }
                            await Task.Delay(120);
                        }
                        if (expectedFocus != target.GetProperty("id").GetString()) throw new InvalidDataException($"Native keyboard Tab traversal reached '{expectedFocus}' instead of '{target.GetProperty("id").GetString()}'.");
                        SendNativeKey(0x0d, keyUp: false); await Task.Delay(180); SendNativeKey(0x0d, keyUp: true);
                    }
                    var inputStarted = DateTime.UtcNow;
                    while (DateTime.UtcNow - inputStarted < TimeSpan.FromSeconds(8))
                    {
                        if (File.Exists(reportPath))
                        {
                            using var reportDocument = JsonDocument.Parse(await File.ReadAllTextAsync(reportPath));
                            var report = reportDocument.RootElement.Clone();
                            if (report.GetProperty("status").GetString() == "observed")
                            {
                                var requests = report.GetProperty("requests").EnumerateArray().ToArray();
                                var sourceId = pointer ? "menu.quit" : "menu.quit";
                                var valid = requests.Any(request => request.GetProperty("actionId").GetString() == "shell.quit" && request.GetProperty("sourceId").GetString() == sourceId);
                                if (valid)
                                {
                                    var closeStarted = DateTime.UtcNow;
                                    while (IsWindow(handle) && IsWindowVisible(handle) && DateTime.UtcNow - closeStarted < TimeSpan.FromSeconds(3)) await Task.Delay(50);
                                    valid &= !IsWindow(handle) || !IsWindowVisible(handle);
                                    if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
                                    await Task.WhenAll(stdout, stderr);
                                    return (valid, report);
                                }
                            }
                        }
                        if (process.HasExited) break;
                        await Task.Delay(100);
                    }
                    break;
                }
            }
            await Task.Delay(100);
        }
        if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
        await Task.WhenAll(stdout, stderr);
        var lastReport = File.Exists(reportPath) ? await File.ReadAllTextAsync(reportPath) : "<no report written>";
        throw new InvalidDataException("Raylib Product Shell did not expose an observed native input receipt (processExited=" + process.HasExited + ", lastReport=" + lastReport + "). " + stdout.Result + stderr.Result);
    }

    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
    [DllImport("user32.dll")] private static extern bool IsWindow(nint handle);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    [DllImport("user32.dll")] private static extern bool ClientToScreen(nint handle, ref NativePoint point);
    [DllImport("user32.dll")] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, nuint extraInfo);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint handle, int command);
    [DllImport("user32.dll")] private static extern bool BringWindowToTop(nint handle);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")] private static extern bool SetActiveWindow(nint handle);
    [DllImport("user32.dll")] private static extern nint SetFocus(nint handle);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint processId);
    [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint attach, uint attachTo, bool attachState);
    [DllImport("user32.dll")] private static extern bool AllowSetForegroundWindow(uint processId);
    [StructLayout(LayoutKind.Sequential)] private struct NativeKeyboardInput { public ushort VirtualKey, ScanCode; public uint Flags, Time; public nuint ExtraInfo; }
    [StructLayout(LayoutKind.Explicit, Size = 40)] private struct NativeInput { [FieldOffset(0)] public uint Type; [FieldOffset(8)] public NativeKeyboardInput Keyboard; }
    [DllImport("user32.dll", SetLastError = true)] private static extern uint SendInput(uint count, [In] NativeInput[] inputs, int size);

    private static void SendNativeKey(ushort virtualKey, bool keyUp)
    {
        var input = new NativeInput { Type = 1, Keyboard = new NativeKeyboardInput { VirtualKey = virtualKey, Flags = keyUp ? 0x0002u : 0u } };
        if (SendInput(1, [input], Marshal.SizeOf<NativeInput>()) != 1) throw new InvalidOperationException("Windows did not accept native keyboard input.");
    }

    private static bool ActivateNativeWindow(nint handle)
    {
        var targetThread = GetWindowThreadProcessId(handle, out var targetProcess);
        _ = AllowSetForegroundWindow(targetProcess);
        var currentThread = GetCurrentThreadId();
        if (targetThread == 0) return false;
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var foreground = GetForegroundWindow();
            var foregroundThread = foreground == 0 ? 0 : GetWindowThreadProcessId(foreground, out _);
            var attached = foregroundThread != 0 && foregroundThread != currentThread && AttachThreadInput(currentThread, foregroundThread, true);
            try
            {
                ShowWindow(handle, 9); BringWindowToTop(handle);
                _ = SetForegroundWindow(handle); _ = SetActiveWindow(handle); _ = SetFocus(handle);
            }
            finally { if (attached) AttachThreadInput(currentThread, foregroundThread, false); }
            if (GetForegroundWindow() == handle) return true;
            Thread.Sleep(100);
        }
        return false;
    }

    private static object Compatibility()
    {
        var legacyDefinition = new UiElement("legacy.root", "root", null, "top-left", 0, 0, 100, 80, 0, 0, 0, [new UiElement("legacy.text", "text", "player.health.current", "top-left", 0, 0, 50, 20, 0, 0, 0, [], "text.health", "font.default")]);
        var legacy = UiProjection.Layout(legacyDefinition, new PreparedPresentationState(7, 10, 2, new Dictionary<string, int>(), false, false, null, null, false, null, "saved", new Dictionary<string, string>(), new Dictionary<string, string>()), 320, 180);
        var host = new UiHost(new UiScreen("legacy.screen")); var legacyButton = new UiButton("legacy.button", "Continue"); var callbackCount = 0; legacyButton.Activated += _ => callbackCount++; host.Screen.Add(legacyButton); var legacySession = host.Project(1280, 720);
        var legacyButtonBounds = legacySession.Single(x => x.ElementId == "legacy.button");
        host.Dispatch(new UiInput(UiInputKind.Pointer, X: legacyButtonBounds.X + 4, Y: legacyButtonBounds.Y + 4));
        host.Focus(legacyButton); host.Dispatch(new UiInput(UiInputKind.Confirm));
        if (callbackCount != 2) throw new InvalidDataException("M037 legacy pointer and keyboard confirmation no longer dispatch callbacks.");
        var menu = M052UiScreens.ProductShell(ProductMenus.Main(false)); var newLayout = UiLayoutEngine.Resolve(menu, new UiViewport(1920, 1080));
        var review = new ReviewState("agentic2d.engineering.review-request.v2", "review.m052.typed-ui-foundation-native-readability-and-interaction", "M052", "docs/milestones/MILESTONE-052-agent-oriented-ui-foundation.md", "Judge readability and interaction.", ["UX"], "required", "human reviewer", "pending", ["artifacts/ui/M052/review-stress.png"], ["Readability", "Focus visibility", "Interaction usability"], ["approved"], "No implicit waiver.", string.Empty, string.Empty, string.Empty, [], null, [], string.Empty, ".review/pending/review.m052.typed-ui-foundation-native-readability-and-interaction.json");
        var reviewExperience = M038ReviewPolicy.IsRegistered(review) && M038ReviewPolicy.ExperienceFor(review) == "m052-ui-stress:" + review.Id;
        if (legacy.Count != 2 || legacySession.Count != 2 || !newLayout.IsValid || !reviewExperience) throw new InvalidDataException("M021/M037 supported compatibility or M052 review experience registration changed.");
        return new { schema = "agentic2d.m052.compatibility.v2", legacyM021Projection = legacy, legacyM037PointerAndConfirmCallbacks = callbackCount, legacyM037ResizedProjection = host.Project(1600, 900), newModelLayoutFingerprint = newLayout.Fingerprint, m052ReviewExperienceRegistered = reviewExperience };
    }

    private static UiDefinition InteractionDefinition(bool modal, bool enabled)
    {
        var content = Ui.Column("interaction.content", Ui.TextField("interaction.notes", "Notes", "semantic.notes", ""), Ui.Button("interaction.action", "Save", "semantic.save", enabled), Ui.Scroll("interaction.scroll", Ui.Column("interaction.scroll-content", Ui.Text("interaction.scroll-text", string.Join(" ", Enumerable.Repeat("Scrollable content with a deterministic viewport and retained offset.", 8)), new UiSizeSpec(UiLength.Grow(), UiLength.Fixed(900), MinHeight: 900)))), Ui.Button("interaction.cancel-target", "Cancel", "semantic.cancel"));
        var dialog = Ui.Modal("interaction.dialog", "Confirm", Ui.Button("interaction.modal-confirm", "Confirm", "semantic.confirm"), "semantic.modal-cancel") with { Visible = modal };
        return UiDefinition.Normalize("interaction.screen", Ui.Screen("interaction.root", Ui.Overlay("interaction.stack", content, dialog)));
    }

    private static void Click(UiSession session, UiLayoutItem item) { ClickDown(session, item); session.Dispatch(new UiInputEvent("pointer-up", item.PhysicalBounds.X + item.PhysicalBounds.Width / 2, item.PhysicalBounds.Y + item.PhysicalBounds.Height / 2)); }
    private static void ClickDown(UiSession session, UiLayoutItem item) => session.Dispatch(new UiInputEvent("pointer-down", item.PhysicalBounds.X + item.PhysicalBounds.Width / 2, item.PhysicalBounds.Y + item.PhysicalBounds.Height / 2));

    private static async Task<(int ExitCode, string Output)> Run(string workingDirectory, string file, IReadOnlyList<string> args)
    {
        var start = new ProcessStartInfo(file) { WorkingDirectory = workingDirectory, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException($"Could not start {file}.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(process.WaitForExitAsync(), stdout, stderr);
        return (process.ExitCode, stdout.Result + stderr.Result);
    }
}
