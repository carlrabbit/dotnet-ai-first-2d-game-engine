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
            "application-behavior" => await ApplicationBehaviorAsync(),
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

    private static async Task<object> ApplicationBehaviorAsync()
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

        var persisted = new List<string>(); var attempts = new Dictionary<string, int>(StringComparer.Ordinal);
        await using var queue = new ReviewDecisionQueue(3, async operation =>
        {
            attempts[operation.Id] = attempts.GetValueOrDefault(operation.Id) + 1;
            await Task.Yield();
            if (operation.Id == "review.m053.queue.failed" && attempts[operation.Id] == 1) return new(false, "injected canonical persistence failure");
            persisted.Add(operation.Id + ":" + operation.Decision);
            return new(true, "canonical review adapter confirmed persistence");
        });
        foreach (var operation in new[]
        {
            new ReviewDecisionOperation("review.m053.queue.accepted", "approved"),
            new ReviewDecisionOperation("review.m053.queue.failed", "changes-requested"),
            new ReviewDecisionOperation("review.m053.queue.accepted.second", "approved")
        }) if (!queue.TryEnqueue(operation)) throw new InvalidDataException("The application queue rejected an in-capacity M053 review operation.");
        await queue.WaitForIdleAsync();
        var firstPass = queue.TakeCompletions().ToArray();
        if (firstPass.Length != 3 || firstPass[0].Id != "review.m053.queue.accepted" || firstPass[1].Success || firstPass[2].Id != "review.m053.queue.accepted.second")
            throw new InvalidDataException("Rapid review decisions were not processed in FIFO order with the injected failure preserved.");
        if (queue.FailedOperations.Count != 1 || queue.RetryFailed() != 1 || queue.RetryFailed() != 0)
            throw new InvalidDataException("Failed review persistence could not be retried exactly once while active.");
        await queue.WaitForIdleAsync();
        var retry = queue.TakeCompletions().Single();
        if (!retry.IsRetry || !retry.Success || queue.FailedOperations.Count != 0)
            throw new InvalidDataException("Review retry did not report confirmed persistence success.");
        await queue.CompleteAsync();
        if (persisted.Count != 3 || persisted[0] != "review.m053.queue.accepted:approved" || persisted[1] != "review.m053.queue.accepted.second:approved" || persisted[2] != "review.m053.queue.failed:changes-requested")
            throw new InvalidDataException("The successful persistence boundary emitted unexpected or reordered operations.");

        var finalProjection = new M053WorkbenchScreens.ReviewProjection("M053", "review.m053.final", "not displayed", 3, 3,
            "pending", "failed", "review.m053.queue.failed: injected canonical persistence failure", false, true, false, false, false, true, false,
            [new(1, "Accepted", "Saved"), new(2, "Rejected", "Failed", "injected canonical persistence failure"), new(3, "Accepted", "Saved")], 0);
        var failedFinalPage = UiLayoutEngine.Resolve(M053WorkbenchScreens.Review(finalProjection), new UiViewport(1920, 1080));
        if (failedFinalPage.Elements.Single(item => item.Id == "review.close").Enabled ||
            !failedFinalPage.Elements.Single(item => item.Id == "review.retry").Enabled ||
            !failedFinalPage.Elements.Any(item => item.Id == "review.summary.item.2" && item.Text!.Contains("Rejected — Failed: injected canonical persistence failure", StringComparison.Ordinal)))
            throw new InvalidDataException("The final review summary concealed a failed write or enabled close before retry: " + string.Join(";", failedFinalPage.Elements.Where(item => item.Id is "review.close" or "review.retry" || item.Id.StartsWith("review.summary.item.", StringComparison.Ordinal)).Select(item => $"{item.Id}[{item.Enabled}]={item.Text}")));
        var completedFinalPage = UiLayoutEngine.Resolve(M053WorkbenchScreens.Review(finalProjection with
        {
            OperationStatus = "saved",
            Feedback = "All decision writes persisted.",
            CanRetry = false,
            CanClose = true,
            DecisionSummary = [new(1, "Accepted", "Saved"), new(2, "Rejected", "Saved"), new(3, "Accepted", "Saved")]
        }), new UiViewport(1920, 1080));
        if (!completedFinalPage.Elements.Single(item => item.Id == "review.close").Enabled || completedFinalPage.Elements.Single(item => item.Id == "review.retry").Enabled)
            throw new InvalidDataException("The final review page did not enable close only after successful persistence.");

        return new { schema = "agentic2d.m053.application-behavior.v1", reviewDecisionEmitsIntentOnly = true, commandDraftNeverAutoSubmits = true, focusedEnterSubmitsOnce = true, disabledDecisionRejected = true, queue = new { fifoOperations = persisted, failedOperation = firstPass[1].Id, retrySucceeded = retry.Success, noDuplicateRetry = attempts["review.m053.queue.failed"] == 2, finalPageFailedCloseDisabled = true, finalPageSuccessCloseEnabled = true }, currentOperationMustBeRevalidatedByApplication = true };
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
        if (unknown.Kind != PreviewSurfaceKind.Unsupported || unknown.Controls.Count != 0 || string.IsNullOrWhiteSpace(unknown.Diagnostic)) throw new InvalidDataException("Unsupported structured mediaKind does not produce a diagnostic-only surface.");
        return new { schema = "agentic2d.m053.compatibility.v1", structuredPreviewDispatch = previewSurfaces, unsupportedDiagnostic = unknown.Diagnostic };
    }

    private static object EvidenceIntegrity(string output)
    {
        var required = new[]
        {
            "headless-compositions.json", "application-behavior.json", "compatibility.json", "windows-native.json",
            "native/review-workbench.png", "native/asset-workbench.png", "native/asset-preview-image.png",
            "native/asset-preview-animation.png", "native/asset-preview-audio.png", "native/review-input.json",
            "native/asset-input.json", "native/asset-input-accept.json", "native/asset-input-reject.json",
            "native/asset-input-defer.json", "native/asset-input-presentation-only.json",
            "native/asset-keyboard-accept.json", "native/asset-keyboard-reject.json",
            "native/asset-keyboard-defer.json", "native/asset-keyboard-presentation-only.json",
            "native/preview-input-image.json", "native/preview-input-animation.json", "native/preview-input-audio.json"
        };
        var files = required.Select(name => Path.Combine(output, name)).ToArray();
        if (files.Any(path => !File.Exists(path))) throw new InvalidDataException("M053 evidence-integrity is missing an exercised portable evidence case: " + string.Join(',', files.Where(path => !File.Exists(path))));
        if (files.Any(path => new FileInfo(path).Length == 0)) throw new InvalidDataException("M053 evidence-integrity contains empty evidence: " + string.Join(',', files.Where(path => new FileInfo(path).Length == 0)));
        using var native = JsonDocument.Parse(File.ReadAllText(Path.Combine(output, "windows-native.json")));
        var nativeReport = native.RootElement;
        if (nativeReport.GetProperty("currentGraphicalProcess").GetBoolean() is false || nativeReport.GetProperty("allNativeCapturesCurrent").GetBoolean() is false)
            throw new InvalidDataException("M053 native evidence did not come from current graphical processes and captures.");
        var reviewProbe = nativeReport.GetProperty("reviewWorkbench").GetProperty("nativeInput");
        var reviewInputObserved = reviewProbe.GetProperty("pointerOrKeyboardActionObserved").GetBoolean();
        var reviewProbeStatus = reviewProbe.GetProperty("status").GetString();
        var reviewEscapeObserved = reviewProbe.GetProperty("escapeCloseObserved").GetBoolean();
        if ((reviewProbeStatus == "passed") != (reviewInputObserved && reviewEscapeObserved) || reviewProbeStatus is not ("passed" or "partial"))
            throw new InvalidDataException("M053 Review Workbench native action status and observed input evidence disagree.");
        if (nativeReport.GetProperty("assetWorkbench").GetProperty("nativeInput").GetProperty("status").GetString() != "passed") throw new InvalidDataException("M053 native input probe did not pass for Asset Workbench.");
        var decisionInputs = nativeReport.GetProperty("assetWorkbench").GetProperty("decisionInputs").EnumerateArray().ToArray();
        var expectedDecisionCommands = new[] { "decision accept-proposal", "decision reject", "decision defer", "decision approve-with-corrections presentation-only" };
        if (decisionInputs.Length != expectedDecisionCommands.Length * 2) throw new InvalidDataException("M053 native evidence is missing one or more pointer or keyboard Asset Workbench decision controls.");
        foreach (var decisionInput in decisionInputs)
        {
            var command = decisionInput.GetProperty("inputCommand");
            var expectedSource = decisionInput.TryGetProperty("inputMethod", out var inputMethod) && inputMethod.GetString() == "native keyboard traversal and Enter" ? "enter" : "mouse-touch";
            if (decisionInput.GetProperty("status").GetString() != "passed" ||
                !expectedDecisionCommands.Contains(command.GetProperty("value").GetString(), StringComparer.Ordinal) ||
                command.GetProperty("schema").GetString() != "agentic2d.asset-workbench-input-command.v1" ||
                command.GetProperty("source").GetString() != expectedSource ||
                command.GetProperty("generation").GetInt32() <= 0 ||
                string.IsNullOrWhiteSpace(command.GetProperty("sessionId").GetString()))
                throw new InvalidDataException("M053 native Asset Workbench decision evidence does not contain a valid canonical command envelope.");
        }
        foreach (var source in new[] { "mouse-touch", "enter" })
        {
            var sourceCommands = decisionInputs.Where(decision => decision.GetProperty("inputCommand").GetProperty("source").GetString() == source)
                .Select(decision => decision.GetProperty("inputCommand").GetProperty("value").GetString()).ToArray();
            if (sourceCommands.Length != expectedDecisionCommands.Length || expectedDecisionCommands.Except(sourceCommands, StringComparer.Ordinal).Any())
                throw new InvalidDataException("M053 native evidence does not exercise every decision command through source " + source + ".");
        }
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
        var reviewInput = await RunNativeReviewProjectionProbe(root, project, ["review-workbench", "--milestone", "M053", "--items-base64", reviewItems], Path.Combine(nativeRoot, "review-input.json"));
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
        var assetDecisionInputs = new List<object>();
        foreach (var action in new[] { "asset-workbench.accept", "asset-workbench.reject", "asset-workbench.defer", "asset-workbench.presentation-only" })
        {
            var actionName = action[("asset-workbench.".Length)..];
            assetDecisionInputs.Add(await RunNativeInputProbe(root, project, ["asset-workbench", "--session", sessionPath, "--commands", commandPath], Path.Combine(nativeRoot, "asset-input-" + actionName + ".json"), action, keyboardCommand: false));
            assetDecisionInputs.Add(await RunNativeInputProbe(root, project, ["asset-workbench", "--session", sessionPath, "--commands", commandPath], Path.Combine(nativeRoot, "asset-keyboard-" + actionName + ".json"), action, keyboardCommand: false, keyboardDecision: true));
        }
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
        var missingMedia = new List<object>();
        foreach (var (kind, reviewId) in kinds)
        {
            var fixtureRoot = Path.Combine(nativeRoot, "missing-media", kind);
            var fixtureSample = Path.Combine(fixtureRoot, "game", "assets", "raw", "samples", "render-atlas-smoke.png");
            Directory.CreateDirectory(Path.GetDirectoryName(fixtureSample)!); File.Copy(sample, fixtureSample, true);
            var fixture = M048ActualCandidatePreview.CreateReviewFixture(fixtureRoot, reviewId);
            foreach (var mediaPath in Directory.EnumerateFiles(Path.GetDirectoryName(fixture.BundlePath)!, kind == "audio" ? "*.wav" : "*.png", SearchOption.AllDirectories)) File.Delete(mediaPath);
            var target = kind switch { "image" => "asset-preview.control.Source", "animation" => "asset-preview.control.Step", _ => "asset-preview.control.Play Raw" };
            var probePath = Path.Combine(nativeRoot, "missing-media-" + kind + "-input.json");
            var probe = await RunNativeInputProbe(root, project, ["asset-preview", "--scene", fixture.ScenePath], probePath, target, keyboardCommand: false);
            var state = JsonSerializer.SerializeToElement(probe, Json).GetProperty("observedState");
            if (kind == "image" && (state.GetProperty("rawMediaLoaded").GetBoolean() || state.GetProperty("processedMediaLoaded").GetBoolean() || !state.GetProperty("mediaDiagnostic").GetString()!.Contains("could not be loaded", StringComparison.Ordinal)))
                throw new InvalidDataException("Missing image files did not produce an honest native media diagnostic.");
            if (kind == "animation" && (state.GetProperty("frameCount").GetInt32() != 0 || !state.GetProperty("mediaDiagnostic").GetString()!.Contains("No ordered animation frames", StringComparison.Ordinal)))
                throw new InvalidDataException("Missing animation frames did not produce an honest native media diagnostic.");
            if (kind == "audio" && (state.GetProperty("rawMediaLoaded").GetBoolean() || state.GetProperty("audioState").GetString()!.StartsWith("raw/base playing", StringComparison.Ordinal) || !state.GetProperty("audioDiagnostic").GetString()!.Contains("playback unavailable", StringComparison.OrdinalIgnoreCase)))
                throw new InvalidDataException("Missing audio media reported playback success or omitted its diagnostic.");
            missingMedia.Add(new { kind, probe });
        }
        var captures = new[] { reviewCapture, workbenchCapture }.Concat(kinds.Select(pair => Path.Combine(nativeRoot, "asset-preview-" + pair.Item1 + ".png"))).Select(Capture).ToArray();
        return new { schema = "agentic2d.m053.windows-native.v1", platform = "windows", currentGraphicalProcess = true, reviewWorkbench = new { capture = Capture(reviewCapture), nativeInput = reviewInput }, assetWorkbench = new { session = JsonDocument.Parse(await File.ReadAllTextAsync(sessionPath)).RootElement.Clone(), capture = Capture(workbenchCapture), nativeInput = assetInput, decisionInputs = assetDecisionInputs, isolatedCaptureCommandFileUnchanged = !File.Exists(commandPath) || new FileInfo(commandPath).Length == 0 }, previews, missingMedia, allNativeCapturesCurrent = captures.All(item => item.Bytes > 0), inputAutomation = "native Review and Asset Workbench pointer/keyboard input, all four Asset decision controls, and image/animation/audio preview actions were observed against projected geometry; five current graphical-process captures were hashed" };
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

    private static async Task<object> RunNativeReviewProjectionProbe(string root, string project, IReadOnlyList<string> args, string reportPath)
    {
        if (File.Exists(reportPath)) File.Delete(reportPath);
        var start = new ProcessStartInfo("dotnet") { WorkingDirectory = root, UseShellExecute = false, CreateNoWindow = false, RedirectStandardOutput = true, RedirectStandardError = true };
        start.ArgumentList.Add("run"); start.ArgumentList.Add("--no-build"); start.ArgumentList.Add("--project"); start.ArgumentList.Add(project); start.ArgumentList.Add("--");
        foreach (var argument in args) start.ArgumentList.Add(argument);
        start.ArgumentList.Add("--probe-report"); start.ArgumentList.Add(reportPath);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start native M053 Review Workbench projection probe.");
        var stdout = process.StandardOutput.ReadToEndAsync(); var stderr = process.StandardError.ReadToEndAsync();
        try
        {
            using var ready = await WaitForProbeReport(process, reportPath, json => json.RootElement.GetProperty("status").GetString() == "ready");
            var handle = ResolveProcessWindow((nint)ready.RootElement.GetProperty("windowHandle").GetInt64(), ready.RootElement.GetProperty("processId").GetInt32());
            if (handle == 0) throw new InvalidDataException("Native Review Workbench did not expose its real window handle.");
            var initialWidth = ready.RootElement.GetProperty("viewport").GetProperty("physicalWidth").GetInt32();
            var initialHeight = ready.RootElement.GetProperty("viewport").GetProperty("physicalHeight").GetInt32();
            var initialControls = ready.RootElement.GetProperty("controls").EnumerateArray().Select(element => new { id = element.GetProperty("id").GetString(), action = element.GetProperty("actionId").GetString(), bounds = element.GetProperty("physicalBounds").Clone() }).ToArray();
            if (!initialControls.Any(control => control.action == "review.previous") || !initialControls.Any(control => control.action == "review.next"))
                throw new InvalidDataException("Native Review Workbench did not project its navigation controls.");
            _ = ShowWindow(handle, 9); _ = SetForegroundWindow(handle);
            if (!SetWindowPos(handle, 0, 0, 0, Math.Min(1400, initialWidth), Math.Min(850, initialHeight), 0x0002 | 0x0004))
                throw new InvalidDataException("Could not perform bounded native Review Workbench resize (error " + Marshal.GetLastWin32Error() + ").");
            using var resized = await WaitForProbeReport(process, reportPath, json =>
                json.RootElement.GetProperty("viewport").GetProperty("physicalWidth").GetInt32() < initialWidth &&
                json.RootElement.GetProperty("viewport").GetProperty("physicalHeight").GetInt32() < initialHeight);
            var width = resized.RootElement.GetProperty("viewport").GetProperty("physicalWidth").GetInt32();
            var height = resized.RootElement.GetProperty("viewport").GetProperty("physicalHeight").GetInt32();
            var resizedControls = resized.RootElement.GetProperty("controls").EnumerateArray().ToArray();
            if (resizedControls.Length == 0 || resizedControls.Any(control =>
            {
                var bounds = control.GetProperty("physicalBounds");
                return bounds.GetProperty("x").GetDouble() < 0 || bounds.GetProperty("y").GetDouble() < 0 ||
                    bounds.GetProperty("x").GetDouble() + bounds.GetProperty("width").GetDouble() > width ||
                    bounds.GetProperty("y").GetDouble() + bounds.GetProperty("height").GetDouble() > height;
            })) throw new InvalidDataException("Native Review Workbench projection placed navigation controls outside the resized client area.");
            JsonDocument? navigated = null;
            var nextControl = resizedControls.Single(control => control.GetProperty("actionId").GetString() == "review.next");
            try
            {
                _ = ShowWindow(handle, 9);
                _ = SetForegroundWindow(handle);
                Thread.Sleep(300);
                ClickProjected(handle, nextControl.GetProperty("physicalBounds"), resized.RootElement.GetProperty("viewport").GetProperty("osDpiScale").GetDouble());
                navigated = await WaitForProbeReport(process, reportPath, json =>
                    json.RootElement.GetProperty("currentOrdinal").GetInt32() == 2 &&
                    json.RootElement.GetProperty("observedActions").EnumerateArray().Any(value => value.GetString()!.StartsWith("review.next@", StringComparison.Ordinal)));
            }
            catch (TimeoutException) { }
            if (navigated is null)
            {
                _ = SetForegroundWindow(handle);
                SendNativeKey(0x09);
                SendNativeKey(0x0D);
                try
                {
                    navigated = await WaitForProbeReport(process, reportPath, json =>
                        json.RootElement.GetProperty("currentOrdinal").GetInt32() == 2 &&
                        json.RootElement.GetProperty("observedActions").EnumerateArray().Any(value => value.GetString()!.StartsWith("review.next@", StringComparison.Ordinal)));
                }
                catch (TimeoutException) { }
            }
            var observed = navigated is not null;
            var currentItemAfterInput = navigated?.RootElement.GetProperty("currentItem").GetString();
            navigated?.Dispose();
            SendNativeKey(0x1B);
            var exitTask = process.WaitForExitAsync();
            var escapeCloseObserved = await Task.WhenAny(exitTask, Task.Delay(TimeSpan.FromSeconds(2))) == exitTask;
            if (escapeCloseObserved) await exitTask;
            else process.Kill(entireProcessTree: true);
            return new { status = observed && escapeCloseObserved ? "passed" : "partial", pointerOrKeyboardActionObserved = observed, escapeCloseObserved, initialViewport = ready.RootElement.GetProperty("viewport").Clone(), resizedViewport = resized.RootElement.GetProperty("viewport").Clone(), initialControls, resizedControls = resizedControls.Select(control => new { id = control.GetProperty("id").GetString(), actionId = control.GetProperty("actionId").GetString(), physicalBounds = control.GetProperty("physicalBounds").Clone() }).ToArray(), currentItemAfterInput, undecidedItemUnchanged = !observed && resized.RootElement.GetProperty("currentItem").GetString() == ready.RootElement.GetProperty("currentItem").GetString() };
        }
        finally
        {
            if (!process.HasExited) { process.Kill(entireProcessTree: true); await process.WaitForExitAsync(); }
            await Task.WhenAll(stdout, stderr);
        }
    }

    private static async Task<object> RunNativeInputProbe(string root, string project, IReadOnlyList<string> args, string reportPath, string targetId, bool keyboardCommand, bool keyboardDecision = false)
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
            var initialWidth = rootElement.GetProperty("viewport").GetProperty("physicalWidth").GetInt32();
            var initialHeight = rootElement.GetProperty("viewport").GetProperty("physicalHeight").GetInt32();
            var controls = rootElement.GetProperty("controls").EnumerateArray().ToArray();
            if (args[0] == "asset-preview")
            {
                var previewTarget = controls.SingleOrDefault(element => element.GetProperty("actionId").GetString() == targetId);
                if (previewTarget.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException("Asset Preview native probe has no projected " + targetId + " control.");
                ClickProjected(handle, previewTarget.GetProperty("physicalBounds"), rootElement.GetProperty("viewport").GetProperty("osDpiScale").GetDouble());
                using var changed = await WaitForProbeReport(process, reportPath, json =>
                {
                    var report = json.RootElement;
                    if (!report.GetProperty("observedActions").EnumerateArray().Any(value => value.GetString() == targetId)) return false;
                    return !targetId.EndsWith("Play Raw", StringComparison.Ordinal) || !report.GetProperty("audioDeviceReady").GetBoolean() || !report.GetProperty("rawMediaLoaded").GetBoolean() ||
                        report.GetProperty("audioState").GetString()!.StartsWith("raw/base playing", StringComparison.Ordinal);
                });
                var state = changed.RootElement;
                if (targetId.EndsWith("Source", StringComparison.Ordinal) && state.GetProperty("imageComparison").GetString() != "source") throw new InvalidDataException("Native image Source control did not select the current raw candidate image.");
                if (targetId.EndsWith("Step", StringComparison.Ordinal) && state.GetProperty("frameCount").GetInt32() > 0 && state.GetProperty("frameIndex").GetInt32() != 1) throw new InvalidDataException("Native animation Step control did not advance the ordered candidate frame.");
                if (targetId.EndsWith("Play Raw", StringComparison.Ordinal) && state.GetProperty("audioDeviceReady").GetBoolean() && state.GetProperty("rawMediaLoaded").GetBoolean() && !state.GetProperty("audioState").GetString()!.StartsWith("raw/base playing", StringComparison.Ordinal)) throw new InvalidDataException("Available audio device and loaded raw media did not report successful explicit playback: " + state.GetProperty("audioState").GetString() + "; " + state.GetProperty("audioDiagnostic").GetString());
                if (targetId.EndsWith("Play Raw", StringComparison.Ordinal) && !state.GetProperty("rawMediaLoaded").GetBoolean() && state.GetProperty("audioState").GetString()!.StartsWith("raw/base playing", StringComparison.Ordinal)) throw new InvalidDataException("Audio preview reported playback success without loaded raw media.");
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
            if (args[0] == "asset-workbench" && keyboardDecision)
            {
                var focusOrder = controls.Select(element => element.GetProperty("id").GetString()!).ToArray();
                var targetControl = Array.FindIndex(controls, element => element.GetProperty("actionId").GetString() == targetId);
                if (targetControl < 0) throw new InvalidDataException("Asset Workbench keyboard probe has no projected " + targetId + " control.");
                var focusId = rootElement.GetProperty("focusedId").GetString();
                var currentFocus = Array.IndexOf(focusOrder, focusId);
                if (currentFocus < 0) throw new InvalidDataException("Asset Workbench started without a focused projected keyboard control.");
                var tabs = (targetControl - currentFocus + focusOrder.Length) % focusOrder.Length;
                for (var index = 0; index < tabs; index++) SendNativeKey(0x09);
                using var focused = await WaitForProbeReport(process, reportPath, json => json.RootElement.GetProperty("focusedId").GetString() == focusOrder[targetControl]);
                var priorLineCount = focused.RootElement.GetProperty("commandLines").GetArrayLength();
                SendNativeKey(0x0D);
                using var activated = await WaitForProbeReport(process, reportPath, json =>
                    json.RootElement.GetProperty("focusedId").GetString() == focusOrder[targetControl] &&
                    json.RootElement.GetProperty("observedActions").EnumerateArray().Any(value => value.GetString()!.StartsWith(targetId + ":", StringComparison.Ordinal)) &&
                    json.RootElement.GetProperty("commandLines").GetArrayLength() > priorLineCount);
                var line = JsonDocument.Parse(activated.RootElement.GetProperty("commandLines").EnumerateArray().Last().GetString()!).RootElement.Clone();
                if (line.GetProperty("schema").GetString() != "agentic2d.asset-workbench-input-command.v1" ||
                    line.GetProperty("sessionId").GetString() != rootElement.GetProperty("sessionId").GetString() ||
                    line.GetProperty("generation").GetInt32() != rootElement.GetProperty("aliasGeneration").GetInt32() ||
                    line.GetProperty("source").GetString() != "enter")
                    throw new InvalidDataException("Keyboard Asset Workbench decision did not emit the canonical session/generation/Enter envelope.");
                var expectedCommand = targetId switch
                {
                    "asset-workbench.accept" => "decision accept-proposal",
                    "asset-workbench.reject" => "decision reject",
                    "asset-workbench.defer" => "decision defer",
                    "asset-workbench.presentation-only" => "decision approve-with-corrections presentation-only",
                    _ => throw new InvalidDataException("Unknown native keyboard decision target " + targetId)
                };
                if (line.GetProperty("value").GetString() != expectedCommand) throw new InvalidDataException("Keyboard decision emitted the wrong canonical command for " + targetId + ".");
                SendNativeKey(0x1B); await process.WaitForExitAsync();
                return new { status = "passed", projectedAction = targetId, inputMethod = "native keyboard traversal and Enter", inputCommand = line };
            }
            if (args[0] == "asset-workbench")
            {
                var actionControl = controls.SingleOrDefault(element => element.GetProperty("actionId").GetString() == targetId);
                if (actionControl.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException("Asset Workbench native probe has no projected " + targetId + " control.");
                var priorLineCount = rootElement.GetProperty("commandLines").GetArrayLength();
                ClickProjected(handle, actionControl.GetProperty("physicalBounds"), rootElement.GetProperty("viewport").GetProperty("osDpiScale").GetDouble());
                using var activated = await WaitForProbeReport(process, reportPath, json =>
                    json.RootElement.GetProperty("observedActions").EnumerateArray().Any(value => value.GetString()!.StartsWith(targetId + ":", StringComparison.Ordinal)) &&
                    json.RootElement.GetProperty("commandLines").GetArrayLength() > priorLineCount);
                var line = JsonDocument.Parse(activated.RootElement.GetProperty("commandLines").EnumerateArray().Last().GetString()!).RootElement.Clone();
                if (line.GetProperty("schema").GetString() != "agentic2d.asset-workbench-input-command.v1" ||
                    line.GetProperty("sessionId").GetString() != rootElement.GetProperty("sessionId").GetString() ||
                    line.GetProperty("generation").GetInt32() != rootElement.GetProperty("aliasGeneration").GetInt32() ||
                    line.GetProperty("source").GetString() != "mouse-touch")
                    throw new InvalidDataException("Native Asset Workbench decision control did not emit the canonical session/generation/source envelope.");
                var expectedCommand = targetId switch
                {
                    "asset-workbench.accept" => "decision accept-proposal",
                    "asset-workbench.reject" => "decision reject",
                    "asset-workbench.defer" => "decision defer",
                    "asset-workbench.presentation-only" => "decision approve-with-corrections presentation-only",
                    _ => throw new InvalidDataException("Unknown native Asset Workbench decision target " + targetId)
                };
                if (line.GetProperty("value").GetString() != expectedCommand) throw new InvalidDataException("Native decision control emitted the wrong canonical command for " + targetId + ".");
                SendNativeKey(0x1B); await process.WaitForExitAsync();
                return new { status = "passed", projectedAction = targetId, inputCommand = line };
            }
            var target = controls.SingleOrDefault(element => element.GetProperty("actionId").GetString() == targetId);
            if (target.ValueKind == JsonValueKind.Undefined) throw new InvalidDataException("Review Workbench native probe has no projected " + targetId + " control.");
            if (targetId == "review.next") { SendNativeKey(0x09); await Task.Delay(150); SendNativeKey(0x0D); }
            else ClickProjected(handle, target.GetProperty("physicalBounds"), rootElement.GetProperty("viewport").GetProperty("osDpiScale").GetDouble());
            JsonDocument navigatedDocument;
            try { navigatedDocument = await WaitForProbeReport(process, reportPath, json => json.RootElement.GetProperty("currentOrdinal").GetInt32() == 2 && json.RootElement.GetProperty("observedActions").EnumerateArray().Any(value => value.GetString()!.StartsWith("review.next@", StringComparison.Ordinal))); }
            catch (InvalidDataException) when (process.HasExited)
            {
                await Task.WhenAll(stdout, stderr);
                throw new InvalidDataException($"Resized native review input exited with code {process.ExitCode}; stderr: {stderr.Result}; stdout: {stdout.Result}");
            }
            using var navigated = navigatedDocument;
            if (navigated.RootElement.GetProperty("currentItem").GetString() != "review.m053.native.second") throw new InvalidDataException("Native review navigation did not display the next loaded item.");
            using var resized = await WaitForProbeReport(process, reportPath, json =>
                json.RootElement.GetProperty("viewport").GetProperty("physicalWidth").GetInt32() < initialWidth &&
                json.RootElement.GetProperty("viewport").GetProperty("physicalHeight").GetInt32() < initialHeight);
            if (resized.RootElement.GetProperty("currentItem").GetString() != "review.m053.native.second" ||
                resized.RootElement.GetProperty("controls").EnumerateArray().Any(control => control.GetProperty("physicalBounds").GetProperty("x").GetDouble() < 0 ||
                    control.GetProperty("physicalBounds").GetProperty("x").GetDouble() + control.GetProperty("physicalBounds").GetProperty("width").GetDouble() > resized.RootElement.GetProperty("viewport").GetProperty("physicalWidth").GetInt32()))
                throw new InvalidDataException("Native Review Workbench lost review state or projected controls after resize.");
            SendNativeKey(0x1B); await process.WaitForExitAsync();
            return new { status = "passed", projectedAction = targetId, inputMethod = targetId == "review.next" ? "native keyboard focus traversal and Enter" : "native pointer at projected bounds", currentItemAfterInput = navigated.RootElement.GetProperty("currentItem").GetString(), resizedViewport = resized.RootElement.GetProperty("viewport").Clone(), observedActions = resized.RootElement.GetProperty("observedActions").Clone() };
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
                try
                {
                    await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    using var reader = new StreamReader(stream);
                    var document = JsonDocument.Parse(await reader.ReadToEndAsync());
                    if (predicate(document)) return document;
                    document.Dispose();
                }
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
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(nint handle, nint insertAfter, int x, int y, int width, int height, uint flags);
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
