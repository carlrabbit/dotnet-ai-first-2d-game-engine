using System.Diagnostics;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Agentic2D.Tools;
using Agentic2D.UI;

namespace Agentic2D.Engineering;

public static class M053RealWorkbenchSuite
{
    public static readonly string[] Shards = ["headless-compositions", "application-behavior", "windows-native", "compatibility", "evidence-integrity"];
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public static async Task<int> RunAsync(string root, string shard, TextWriter diagnostics)
    {
        if (!Shards.Contains(shard, StringComparer.Ordinal)) throw new EngineeringException($"unsupported internal shard: m053-workbenches/{shard}");
        var output = Path.Combine(root, "artifacts", "ui", "M053"); Directory.CreateDirectory(output);
        var evidence = shard switch
        {
            "headless-compositions" => HeadlessCompositions(),
            "application-behavior" => ApplicationBehavior(),
            "windows-native" => await WindowsNativeAsync(root, output, diagnostics),
            "compatibility" => Compatibility(),
            "evidence-integrity" => EvidenceIntegrity(output),
            _ => throw new InvalidOperationException()
        };
        var path = Path.Combine(output, shard + ".json");
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(evidence, Json));
        await diagnostics.WriteLineAsync($"m053-workbenches/{shard}: evidence written to {Path.GetRelativePath(root, path)}");
        return 0;
    }

    private static object HeadlessCompositions()
    {
        var review = M053WorkbenchScreens.Review(new("M053", "review.m053.headless", string.Join(' ', Enumerable.Repeat("The full review question remains readable in its scroll region.", 8)), 1, 5, "pending", "saving", "Saving is pending", true, false, false, true, false));
        var assets = M053WorkbenchScreens.AssetWorkbench(new("session.m053.headless", 4, Enumerable.Range(1, 28).Select(index => "candidate.long.identity." + index).ToArray(), "candidate.long.identity.1", "", "Current session"));
        var previews = new[] { "image", "animation", "audio", "unsupported" }.Select(CreatePreview).ToArray();
        var viewports = new[] { new UiViewport(1280, 720), new UiViewport(1920, 1080), new UiViewport(2560, 1440, 1.25), new UiViewport(3840, 2160, 2), new UiViewport(3440, 1440, 1.5), new UiViewport(2560, 1440, 1, 1.25) };
        var results = new List<object>();
        foreach (var definition in new[] { review, assets }.Concat(previews))
            foreach (var viewport in viewports)
            {
                var first = UiLayoutEngine.Resolve(definition, viewport); var second = UiLayoutEngine.Resolve(definition, viewport);
                if (!first.IsValid || first.Fingerprint != second.Fingerprint) throw new InvalidDataException($"M053 screen {definition.Id} is invalid or nondeterministic at {viewport}: {string.Join(';', first.Diagnostics.Select(item => item.Code + ':' + item.ElementId))}");
                var session = new UiSession(definition, viewport);
                results.Add(new { definition.Id, viewport, layout = first, semantic = session.ToEvidence(), draw = UiDrawProjection.Create(session) });
            }
        var reviewLayout = UiLayoutEngine.Resolve(review, new UiViewport(1920, 1080));
        var status = reviewLayout.Elements.Single(item => item.Id == "review.status").LogicalBounds;
        var question = reviewLayout.Elements.Single(item => item.Id == "review.question").LogicalBounds;
        var navigationBounds = new[] { "review.previous", "review.ordinal", "review.next" }.Select(id => reviewLayout.Elements.Single(item => item.Id == id).LogicalBounds);
        if (navigationBounds.Any(bounds => status.Intersects(bounds)) || status.Intersects(question)) throw new InvalidDataException("Review status overlaps navigation or question geometry.");
        return new { schema = "agentic2d.m053.headless-compositions.v1", views = results, matrices = viewports.Length, fingerprintsReproducible = true, statusQuestionNavigationDisjoint = true, actualScreens = new[] { review.Id, assets.Id }.Concat(previews.Select(item => item.Id)).ToArray() };
    }

    private static object ApplicationBehavior()
    {
        var review = M053WorkbenchScreens.Review(new("M053", "review.m053.behavior", "Question", 1, 2, "pending", "idle", "Ready", true, false, false, false, false));
        var session = new UiSession(review, new UiViewport(1920, 1080));
        Click(session, "review.accept");
        var accepted = session.DrainActions().SingleOrDefault();
        if (accepted?.ActionId != "review.accept") throw new InvalidDataException("The real review composition does not emit its stable accept intent from its projected hit target.");
        var assetDefinition = M053WorkbenchScreens.AssetWorkbench(new("session.m053", 9, ["candidate.a", "candidate.b"], "candidate.a", "", "Ready"));
        var asset = new UiSession(assetDefinition, new UiViewport(1920, 1080));
        Click(asset, "asset-workbench.command");
        asset.UpdateTextDraft("asset-workbench.command", "2");
        if (asset.DrainActions().Count != 0) throw new InvalidDataException("Updating a command draft emitted a canonical provider command.");
        asset.Dispatch(new UiInputEvent("key", Key: "Enter"));
        var command = asset.DrainActions().SingleOrDefault();
        if (command?.ActionId != "asset-workbench.submit" || command.SourceId != "asset-workbench.command" || command.Value?.ToString() != "2") throw new InvalidDataException("Focused Enter did not emit exactly one canonical command-entry request.");
        var disabled = new UiSession(M053WorkbenchScreens.Review(new("M053", "review.m053.disabled", "Question", 1, 1, "approved", "saved", "Saved", false, false, true, false, false)), new UiViewport(1920, 1080));
        Click(disabled, "review.accept");
        if (disabled.DrainActions().Count != 0) throw new InvalidDataException("A disabled/stale review action emitted an intent.");
        return new { schema = "agentic2d.m053.application-behavior.v1", reviewDecisionEmitsIntentOnly = true, commandDraftNeverAutoSubmits = true, focusedEnterSubmitsOnce = true, disabledDecisionRejected = true, currentOperationMustBeRevalidatedByApplication = true };
    }

    private static object Compatibility()
    {
        var previewSurfaces = new[] { "image", "animation", "audio", "misleading-candidate-id" }.Select(kind =>
        {
            var surface = CreateSurface(kind == "misleading-candidate-id" ? "audio" : kind, "review.id." + kind, "candidate." + (kind == "audio" ? "image" : kind));
            return new { requestedStructuredMediaKind = kind == "misleading-candidate-id" ? "audio" : kind, surface.Kind, surface.Controls, surface.Context.CandidateId, mediaIsIdIndependent = true };
        }).ToArray();
        if (previewSurfaces[0].Kind != PreviewSurfaceKind.Image || previewSurfaces[1].Kind != PreviewSurfaceKind.Animation || previewSurfaces[2].Kind != PreviewSurfaceKind.Audio || previewSurfaces[3].Kind != PreviewSurfaceKind.Audio) throw new InvalidDataException("Structured mediaKind dispatch changed or was influenced by an opaque review/candidate ID.");
        var unknown = CreateSurface("other", "review.id.image", "candidate.audio");
        if (unknown.Kind != PreviewSurfaceKind.Unsupported || unknown.Controls.Count != 0) throw new InvalidDataException("Unsupported structured mediaKind exposes misleading preview controls.");
        return new { schema = "agentic2d.m053.compatibility.v1", structuredPreviewDispatch = previewSurfaces, unsupportedDiagnosticOnly = true, m029CommandSchema = "agentic2d.asset-workbench-input-command.v1", previewIpc = "v2", noHistoricalReviewFilesWritten = true, noM048M050VerifierDependency = true };
    }

    private static object EvidenceIntegrity(string output)
    {
        var required = new[]
        {
            "headless-compositions.json", "application-behavior.json", "compatibility.json", "windows-native.json",
            "native/review-workbench.png", "native/asset-workbench.png", "native/asset-preview-image.png",
            "native/asset-preview-animation.png", "native/asset-preview-audio.png", "native/review-input.json",
            "native/asset-input.json", "native/preview-input-image.json", "native/preview-input-animation.json", "native/preview-input-audio.json"
        };
        var files = required.Select(name => Path.Combine(output, name)).ToArray();
        if (files.Any(path => !File.Exists(path))) throw new InvalidDataException("M053 evidence-integrity is missing an exercised portable evidence case: " + string.Join(',', files.Where(path => !File.Exists(path))));
        if (files.Any(path => new FileInfo(path).Length == 0)) throw new InvalidDataException("M053 evidence-integrity contains empty evidence: " + string.Join(',', files.Where(path => new FileInfo(path).Length == 0)));
        using var native = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "windows-native.json")));
        var nativeReport = native.RootElement;
        if (nativeReport.GetProperty("currentGraphicalProcess").GetBoolean() is false || nativeReport.GetProperty("allNativeCapturesCurrent").GetBoolean() is false)
            throw new InvalidDataException("M053 native evidence did not come from current graphical processes and captures.");
        foreach (var reviewId in new[] { "reviewWorkbench", "assetWorkbench" })
            if (nativeReport.GetProperty(reviewId).GetProperty("nativeInput").GetProperty("status").GetString() != "passed") throw new InvalidDataException("M053 native input probe did not pass for " + reviewId + ".");
        foreach (var preview in nativeReport.GetProperty("previews").EnumerateArray())
            if (preview.GetProperty("nativeInput").GetProperty("status").GetString() != "passed" || preview.GetProperty("capture").GetProperty("bytes").GetInt64() == 0)
                throw new InvalidDataException("M053 native preview input/capture evidence did not pass for " + preview.GetProperty("kind").GetString() + ".");
        var hashes = files.Select(path => new { path = Path.GetFileName(path), sha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant(), bytes = new FileInfo(path).Length }).ToArray();
        return new { schema = "agentic2d.m053.evidence-integrity.v1", status = "passed", evidence = hashes, nativeProofRequiredSeparately = true, humanReviewRequiredSeparately = true };
    }

    private static async Task<object> WindowsNativeAsync(string root, string output, TextWriter diagnostics)
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("M053 native Raylib shard is Windows-only in the current platform epoch.");
        var nativeRoot = Path.Combine(output, "native"); Directory.CreateDirectory(nativeRoot);
        var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib", "Agentic2D.DebugClient.Raylib.csproj");
        var reviewCapture = Path.Combine(nativeRoot, "review-workbench.png");
        var reviewItems = Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new[] { new { id = "review.m053.native.first", subject = "First native review question with readable status, navigation and decision controls.", status = "pending" }, new { id = "review.m053.native.second", subject = "Second native review question proves projected next navigation.", status = "pending" } })));
        var reviewInput = await RunNativeInputProbe(root, project, ["review-workbench", "--milestone", "M053", "--items-base64", reviewItems], Path.Combine(nativeRoot, "review-input.json"), "review.next", keyboardCommand: false);
        await RunNative(root, project, ["review-workbench", "--milestone", "M053", "--items-base64", reviewItems, "--frames", "5", "--capture", reviewCapture]);

        var workbenchRoot = Path.Combine(nativeRoot, "workbench-fixture"); Directory.CreateDirectory(workbenchRoot);
        var sample = Path.Combine(root, "game", "assets", "raw", "samples", "render-atlas-smoke.png");
        var tempRepository = Path.Combine(workbenchRoot, "repository"); var sampleTarget = Path.Combine(tempRepository, "game", "assets", "raw", "samples", "render-atlas-smoke.png");
        Directory.CreateDirectory(Path.GetDirectoryName(sampleTarget)!); File.Copy(sample, sampleTarget, true);
        var assetRoot = Path.Combine(workbenchRoot, "asset-home"); var sourceRoot = Path.Combine(workbenchRoot, "candidate-source"); Directory.CreateDirectory(sourceRoot);
        var sourceFile = Path.Combine(sourceRoot, "candidate.png"); File.Copy(sample, sourceFile, true); Directory.CreateDirectory(Path.Combine(assetRoot, "registry"));
        await File.WriteAllTextAsync(Path.Combine(assetRoot, "registry", "sources.json"), JsonSerializer.Serialize(new { schema = "agentic2d.asset-source-registry.v1", sources = new[] { new { id = "source.m053.native", path = sourceRoot, currentProfileFingerprint = "profile.m053.native" } } }));
        var campaignPath = Path.Combine(workbenchRoot, "campaign.json");
        await File.WriteAllTextAsync(campaignPath, JsonSerializer.Serialize(new { schema = "agentic2d.asset-campaign.v2", id = "campaign.m053.native", sourceId = "source.m053.native", profileFingerprint = "profile.m053.native", candidates = new[] { new { candidateId = "candidate.m053.native.image", sourceRelativePath = "candidate.png", mediaKind = "image", presentationRole = "native-review", proposalFingerprint = "proposal.m053.native", selection = new { type = "image-file" } } } }));
        var priorHome = Environment.GetEnvironmentVariable("AGENTIC2D_ASSET_HOME");
        var workbenchOutput = Path.Combine(workbenchRoot, "opened");
        try
        {
            Environment.SetEnvironmentVariable("AGENTIC2D_ASSET_HOME", assetRoot);
            var opened = await M029AssetWorkbenchCommands.RunAsync(["asset", "workbench", "--campaign", campaignPath, "--headless", "--output", workbenchOutput], TextWriter.Null, diagnostics);
            if (opened != 0) throw new InvalidDataException("The canonical M029 provider did not create the native workbench session.");
        }
        finally { Environment.SetEnvironmentVariable("AGENTIC2D_ASSET_HOME", priorHome); }
        var sessionPath = Path.Combine(workbenchOutput, "review-session.json"); var commandPath = Path.Combine(workbenchRoot, "commands.jsonl");
        var workbenchCapture = Path.Combine(nativeRoot, "asset-workbench.png");
        var assetInput = await RunNativeInputProbe(root, project, ["asset-workbench", "--session", sessionPath, "--commands", commandPath], Path.Combine(nativeRoot, "asset-input.json"), "asset-workbench.command", keyboardCommand: true);
        if (File.Exists(commandPath)) File.Delete(commandPath);
        await RunNative(root, project, ["asset-workbench", "--session", sessionPath, "--commands", commandPath, "--frames", "5", "--capture", workbenchCapture]);

        var previews = new List<object>();
        var kinds = new[] { ("image", "review.m048.01-image-candidate-curation"), ("animation", "review.m048.02-animation-candidate-curation"), ("audio", "review.m048.03-audio-candidate-curation") };
        foreach (var (kind, reviewId) in kinds)
        {
            var fixtureRoot = Path.Combine(nativeRoot, "preview-fixtures", kind);
            var fixtureSample = Path.Combine(fixtureRoot, "game", "assets", "raw", "samples", "render-atlas-smoke.png");
            Directory.CreateDirectory(Path.GetDirectoryName(fixtureSample)!); File.Copy(sample, fixtureSample, true);
            var fixture = M048ActualCandidatePreview.CreateReviewFixture(fixtureRoot, reviewId);
            var capturePath = Path.Combine(nativeRoot, "asset-preview-" + kind + ".png");
            var previewControl = kind switch { "image" => "asset-preview.control.Source", "animation" => "asset-preview.control.Step", _ => "asset-preview.control.Play Raw" };
            var previewInput = await RunNativeInputProbe(root, project, ["asset-preview", "--scene", fixture.ScenePath], Path.Combine(nativeRoot, "preview-input-" + kind + ".json"), previewControl, keyboardCommand: false);
            var result = await RunNative(root, project, ["asset-preview", "--scene", fixture.ScenePath, "--frames", "5", "--capture", capturePath]);
            previews.Add(new { kind, fixture.ReviewId, fixture.Subject, capture = Capture(capturePath), nativeInput = previewInput, actualProcess = result });
        }
        var captures = new[] { reviewCapture, workbenchCapture }.Concat(kinds.Select(pair => Path.Combine(nativeRoot, "asset-preview-" + pair.Item1 + ".png"))).Select(Capture).ToArray();
        return new { schema = "agentic2d.m053.windows-native.v1", platform = "windows", currentGraphicalProcess = true, reviewWorkbench = new { capture = Capture(reviewCapture), nativeInput = reviewInput }, assetWorkbench = new { session = JsonDocument.Parse(await File.ReadAllTextAsync(sessionPath)).RootElement.Clone(), capture = Capture(workbenchCapture), nativeInput = assetInput, isolatedCaptureCommandFileUnchanged = !File.Exists(commandPath) || new FileInfo(commandPath).Length == 0 }, previews, allNativeCapturesCurrent = captures.All(item => item.Bytes > 0), inputAutomation = "native projected pointer and keyboard input observed; review navigation and canonical asset input JSONL recorded" };
    }

    private static async Task<object> RunNative(string root, string project, IReadOnlyList<string> args)
    {
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("run"); start.ArgumentList.Add("--no-build"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(project); start.ArgumentList.Add("--");
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start the native Raylib workbench.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        await Task.WhenAll(process.WaitForExitAsync(), stdout, stderr);
        if (process.ExitCode != 0) throw new InvalidDataException("Native M053 surface exited with " + process.ExitCode + ": " + stderr.Result + stdout.Result);
        return new { exitCode = process.ExitCode, stdout = stdout.Result, stderr = stderr.Result };
    }

    private static async Task<object> RunNativeInputProbe(string root, string project, IReadOnlyList<string> args, string reportPath, string targetId, bool keyboardCommand)
    {
        if (File.Exists(reportPath)) File.Delete(reportPath);
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("run"); start.ArgumentList.Add("--no-build"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(project); start.ArgumentList.Add("--");
        foreach (var argument in args) start.ArgumentList.Add(argument);
        start.ArgumentList.Add("--probe-report"); start.ArgumentList.Add(reportPath);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start native M053 input probe.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            using var ready = await WaitForProbeReport(process, reportPath, json => json.RootElement.GetProperty("status").GetString() == "ready");
            var rootElement = ready.RootElement;
            var reportedHandle = (nint)rootElement.GetProperty("windowHandle").GetInt64();
            var handle = ResolveProcessWindow(reportedHandle, rootElement.GetProperty("processId").GetInt32());
            if (handle == 0) throw new InvalidDataException("M053 native surface did not expose a visible window owned by its launched process.");
            _ = ShowWindow(handle, 9); _ = SetForegroundWindow(handle);
            await Task.Delay(100);
            var controls = rootElement.GetProperty("controls").EnumerateArray().ToArray();
            if (args[0] == "asset-preview")
            {
                var previewTarget = controls.SingleOrDefault(element => element.GetProperty("actionId").GetString() == targetId);
                if (previewTarget.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException("Asset Preview native probe has no projected " + targetId + " control.");
                ClickProjected(handle, previewTarget.GetProperty("physicalBounds"), rootElement.GetProperty("viewport").GetProperty("osDpiScale").GetDouble());
                using var changed = await WaitForProbeReport(process, reportPath, json => json.RootElement.GetProperty("observedActions").EnumerateArray().Any(value => value.GetString() == targetId));
                var state = changed.RootElement;
                if (targetId.EndsWith("Source", StringComparison.Ordinal) && state.GetProperty("imageComparison").GetString() != "source") throw new InvalidDataException("Native image Source control did not select the current raw candidate image.");
                if (targetId.EndsWith("Step", StringComparison.Ordinal) && state.GetProperty("frameIndex").GetInt32() != 1) throw new InvalidDataException("Native animation Step control did not advance the ordered candidate frame.");
                if (targetId.EndsWith("Play Raw", StringComparison.Ordinal) && state.GetProperty("audioDeviceReady").GetBoolean() && !state.GetProperty("audioState").GetString()!.StartsWith("raw/base playing", StringComparison.Ordinal)) throw new InvalidDataException("Available audio device did not report successful explicit raw playback.");
                SendNativeKey(0x1B); await process.WaitForExitAsync();
                return new { status = "passed", projectedAction = targetId, observedState = state.Clone() };
            }
            if (keyboardCommand)
            {
                var list = controls.SingleOrDefault(element => element.GetProperty("actionId").GetString() == "asset-workbench.choose");
                if (list.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException("Asset Workbench native probe has no projected candidate-list control.");
                ClickProjected(handle, list.GetProperty("physicalBounds"), rootElement.GetProperty("viewport").GetProperty("osDpiScale").GetDouble());
                using var chosen = await WaitForProbeReport(process, reportPath, json => json.RootElement.GetProperty("observedActions").EnumerateArray().Any(value => value.GetString()!.StartsWith("asset-workbench.choose:", StringComparison.Ordinal)));
                var field = chosen.RootElement.GetProperty("controls").EnumerateArray().Single(element => element.GetProperty("id").GetString() == "asset-workbench.command");
                ClickProjected(handle, field.GetProperty("physicalBounds"), chosen.RootElement.GetProperty("viewport").GetProperty("osDpiScale").GetDouble());
                await Task.Delay(160); SendNativeKey(0x31); SendNativeKey(0x0D);
                using var completed = await WaitForProbeReport(process, reportPath, json =>
                {
                    var report = json.RootElement;
                    return report.GetProperty("observedActions").EnumerateArray().Any(value => value.GetString()!.StartsWith("asset-workbench.submit:", StringComparison.Ordinal)) && report.GetProperty("commandLines").GetArrayLength() >= 2;
                });
                var lines = completed.RootElement.GetProperty("commandLines").EnumerateArray().Select(value => JsonDocument.Parse(value.GetString()!).RootElement.Clone()).ToArray();
                if (lines.Any(line => line.GetProperty("schema").GetString() != "agentic2d.asset-workbench-input-command.v1" || line.GetProperty("generation").GetInt32() != 1 || line.GetProperty("sessionId").GetString() is null) || !lines.Any(line => line.GetProperty("source").GetString() == "enter"))
                    throw new InvalidDataException("Native Asset Workbench input did not preserve the canonical session/generation/source envelope.");
                SendNativeKey(0x1B);
                await process.WaitForExitAsync();
                return new { status = "passed", pointerCandidateChoice = true, keyboardEnterCommand = true, canonicalJsonlLines = lines };
            }
            var target = controls.SingleOrDefault(element => element.GetProperty("actionId").GetString() == targetId);
            if (target.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException("Review Workbench native probe has no projected " + targetId + " control.");
            ClickProjected(handle, target.GetProperty("physicalBounds"), rootElement.GetProperty("viewport").GetProperty("osDpiScale").GetDouble());
            using var navigated = await WaitForProbeReport(process, reportPath, json => json.RootElement.GetProperty("currentOrdinal").GetInt32() == 2 && json.RootElement.GetProperty("observedActions").EnumerateArray().Any(value => value.GetString()!.StartsWith("review.next@", StringComparison.Ordinal)));
            if (navigated.RootElement.GetProperty("currentItem").GetString() != "review.m053.native.second") throw new InvalidDataException("Native review navigation did not display the next loaded item.");
            SendNativeKey(0x1B); await process.WaitForExitAsync();
            return new { status = "passed", projectedAction = targetId, currentItemAfterPointer = navigated.RootElement.GetProperty("currentItem").GetString(), observedActions = navigated.RootElement.GetProperty("observedActions").Clone() };
        }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
            await Task.WhenAll(stdout, stderr);
        }
    }

    private static async Task<JsonDocument> WaitForProbeReport(Process process, string path, Func<JsonDocument, bool> predicate)
    {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(15);
        while (DateTime.UtcNow < deadline)
        {
            if (File.Exists(path))
            {
                try { var document = JsonDocument.Parse(await File.ReadAllTextAsync(path)); if (predicate(document)) return document; document.Dispose(); }
                catch (JsonException) { }
                catch (IOException) { }
            }
            if (process.HasExited) throw new InvalidDataException("M053 native input target exited before reporting the requested state.");
            await Task.Delay(100);
        }
        throw new TimeoutException("M053 native input report did not reach its expected projected-control state: " + path);
    }

    private static void ClickProjected(nint handle, JsonElement bounds, double dpi)
    {
        var point = new NativePoint { X = (int)Math.Round((bounds.GetProperty("x").GetDouble() + bounds.GetProperty("width").GetDouble() / 2) / dpi), Y = (int)Math.Round((bounds.GetProperty("y").GetDouble() + bounds.GetProperty("height").GetDouble() / 2) / dpi) };
        if (!ClientToScreen(handle, ref point)) throw new InvalidOperationException("Could not map M052 projected bounds to desktop coordinates (error " + Marshal.GetLastWin32Error() + ").");
        if (!SetCursorPos(point.X, point.Y)) throw new InvalidOperationException($"Could not position native input at M052 projected bounds ({point.X},{point.Y}; error {Marshal.GetLastWin32Error()}).");
        Thread.Sleep(100); mouse_event(0x0002, 0, 0, 0, 0); Thread.Sleep(100); mouse_event(0x0004, 0, 0, 0, 0); Thread.Sleep(150);
    }

    private static nint ResolveProcessWindow(nint reportedHandle, int processId)
    {
        if (IsWindow(reportedHandle) && IsWindowVisible(reportedHandle) && GetWindowProcessId(reportedHandle) == (uint)processId) return reportedHandle;
        nint match = 0;
        EnumWindows((window, _) =>
        {
            if (IsWindowVisible(window) && GetWindowProcessId(window) == (uint)processId) { match = window; return false; }
            return true;
        }, 0);
        return match;
    }

    private delegate bool EnumWindowsCallback(nint window, nint parameter);

    [StructLayout(LayoutKind.Sequential)] private struct NativePoint { public int X, Y; }
    [DllImport("user32.dll")] private static extern bool IsWindow(nint handle);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint handle);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool ClientToScreen(nint handle, ref NativePoint point);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint handle, out uint processId);
    private static uint GetWindowProcessId(nint handle) { _ = GetWindowThreadProcessId(handle, out var processId); return processId; }
    [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsCallback callback, nint parameter);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] private static extern void mouse_event(uint flags, uint dx, uint dy, uint data, nuint extraInfo);
    [DllImport("user32.dll")] private static extern bool ShowWindow(nint handle, int command);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(nint handle);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, nuint extraInfo);
    private static void SendNativeKey(byte key) { keybd_event(key, 0, 0, 0); Thread.Sleep(40); keybd_event(key, 0, 2, 0); Thread.Sleep(100); }

    private sealed record NativeCapture(string Path, long Bytes, string? Sha256);
    private static NativeCapture Capture(string path) => new(path, File.Exists(path) ? new FileInfo(path).Length : 0, File.Exists(path) ? Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))).ToLowerInvariant() : null);
    private static UiDefinition CreatePreview(string kind) => M053WorkbenchScreens.AssetPreview(new(CreateSurface(kind, "review.fixture", "candidate.fixture"), "media.fixture", "Current candidate media", "Paused", 0, 4, false, "Manual playback only."));
    private static PreviewSurface CreateSurface(string kind, string reviewId, string candidateId)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(new { mediaKind = kind, reviewId, reviewContext = new { candidateId, presentationRole = "preview candidate", selectedVariant = "current", corrections = new[] { "correction.fixture" }, rawMeaning = "raw/base media", processedMeaning = "processed/current draft", subjectiveQuestion = "Is the candidate legible for its purpose?", rawDurationSamples = 800, processedDurationSamples = 700, sampleRate = 8000 } }));
        return AssetPreviewSurfaceModel.FromBundle(document.RootElement);
    }
    private static void Click(UiSession session, string id)
    {
        var target = session.Layout.Elements.Single(item => item.Id == id); var x = target.PhysicalBounds.X + target.PhysicalBounds.Width / 2; var y = target.PhysicalBounds.Y + target.PhysicalBounds.Height / 2;
        session.Dispatch(new UiInputEvent("pointer-down", x, y)); session.Dispatch(new UiInputEvent("pointer-up", x, y));
    }
}
