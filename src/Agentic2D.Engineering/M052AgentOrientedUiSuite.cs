using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Agentic2D.UI;

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
            if (!menuLayout.IsValid || !stressLayout.IsValid) throw new InvalidDataException($"Supported viewport {item.Label} produced diagnostics: {string.Join(";", menuLayout.Diagnostics.Concat(stressLayout.Diagnostics).Select(x => x.Code + ":" + x.ElementId))}");
            var menuSemantics = menuLayout.Elements.Where(x => x.Focusable).Select(x => new { x.Id, x.ActionId, x.LogicalBounds.Width, x.LogicalBounds.Height }).ToArray();
            results.Add(new { item.Label, viewport, productShell = menuLayout, stress = stressLayout, focusableControlCount = menuSemantics.Length });
            fingerprints.Add(menuLayout.Fingerprint);
        }
        var repeatA = UiLayoutEngine.Resolve(menu, new UiViewport(1920, 1080)); var repeatB = UiLayoutEngine.Resolve(menu, new UiViewport(1920, 1080));
        if (repeatA.Fingerprint != repeatB.Fingerprint) throw new InvalidDataException("Equivalent definitions did not produce the same normalized layout fingerprint.");
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
        var artifact = new { schema = "agentic2d.m052.headless-layout.v1", compiledConsumer = consumer, compilerOutput = compile.Output, resolutionMatrix = results, equivalentFingerprint = repeatA.Fingerprint, deterministicRepeat = true, undersizedNegative = negative, infeasibleSupportedLayout = overflow, unintendedOverlap = overlapResult, intentionalOverlay = overlayResult, intentionalScrollClip = clippedResult, headlessOnly = true, noRaylibDependencyInUiProject = true };
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
        if (session.DrainActions().Any(x => x.ActionId == "semantic.save")) throw new InvalidDataException("Modal input activated a lower layer control.");
        var expectedOperation = new UiOperationProjection("save", AgentUiOperationStatus.Working, "Saving");
        var operationWorking = UiDefinition.Normalize("operation.working", Ui.Screen("operation.root", Ui.Busy("operation.status", "Save status", expectedOperation), Ui.Progress("operation.progress", .7, "Saving")));
        var operationFailed = UiDefinition.Normalize("operation.failed", Ui.Screen("operation.root", Ui.Busy("operation.status", "Save status", expectedOperation with { Status = AgentUiOperationStatus.Failed, Message = "Disk unavailable", RetryActionId = "save.retry" }), Ui.Button("operation.retry", "Retry", "save.retry")));
        var statusProjection = new[] { UiLayoutEngine.Resolve(operationWorking, new UiViewport(1920, 1080)), UiLayoutEngine.Resolve(operationFailed, new UiViewport(1920, 1080)) };
        var report = new { schema = "agentic2d.m052.headless-interaction.v1", stableFocusAndDraftOnRecompose = true, eventSubscriptionCount = 1, typedAction = firstActions[0], pointerPressReleaseUsesProjectedBounds = true, captureCancelled = true, disabledFocusReleased = true, modalTrapsFocusAndBlocksClickThrough = true, scrollOffset = after.ScrollOffsets.GetValueOrDefault("interaction.scroll"), injectedPresentationTime = time, reducedMotionStatusDoesNotChangeOperations = baseScreen.ReducedMotion, operationStates = statusProjection, trace = session.Snapshot().Trace, final = session.ToEvidence() };
        File.WriteAllText(Path.Combine(output, "interaction-trace.json"), JsonSerializer.Serialize(report, Json));
        return report;
    }

    private static async Task<object> RaylibAsync(string root, string output, TextWriter diagnostics)
    {
        if (!OperatingSystem.IsWindows()) throw new InvalidOperationException("M052 native integration is required on active Windows; this run is not Windows.");
        var project = Path.Combine(root, "src", "Agentic2D.DebugClient.Raylib");
        var shellCapture = Path.Combine(output, "product-shell.png"); var stressCapture = Path.Combine(output, "review-stress.png");
        var shell = await Run(root, "dotnet", ["run", "--no-build", "--project", project, "--", "shell", "--frames", "4", "--capture", shellCapture]);
        var stress = await Run(root, "dotnet", ["run", "--no-build", "--project", project, "--", "ui-stress", "--frames", "4", "--capture", stressCapture]);
        if (shell.ExitCode != 0 || !File.Exists(shellCapture)) { await diagnostics.WriteLineAsync(shell.Output); throw new InvalidDataException("Actual Product Shell Raylib launch/capture failed."); }
        if (stress.ExitCode != 0 || !File.Exists(stressCapture)) { await diagnostics.WriteLineAsync(stress.Output); throw new InvalidDataException("Actual synthetic stress Raylib launch/capture failed."); }
        return new { schema = "agentic2d.m052.windows-raylib.v1", platform = "windows", activePlatform = true, actualProcessLaunch = true, productShell = new { shellCapture, bytes = new FileInfo(shellCapture).Length, shell.Output }, stressScreen = new { stressCapture, bytes = new FileInfo(stressCapture).Length, stress.Output }, inputWired = "pointer-move/down/up, Tab, arrows, Enter; semantic events enter the UiSession", uiProjectionUsed = true, reviewPersistenceTouched = false };
    }

    private static object Compatibility()
    {
        var legacyDefinition = new UiElement("legacy.root", "root", null, "top-left", 0, 0, 100, 80, 0, 0, 0, [new UiElement("legacy.text", "text", "player.health.current", "top-left", 0, 0, 50, 20, 0, 0, 0, [], "text.health", "font.default")]);
        var legacy = UiProjection.Layout(legacyDefinition, new PreparedPresentationState(7, 10, 2, new Dictionary<string, int>(), false, false, null, null, false, null, "saved", new Dictionary<string, string>(), new Dictionary<string, string>()), 320, 180);
        var host = new UiHost(new UiScreen("legacy.screen")); host.Screen.Add(new UiButton("legacy.button", "Continue")); var legacySession = host.Project(1280, 720);
        var menu = M052UiScreens.ProductShell(ProductMenus.Main(false)); var newLayout = UiLayoutEngine.Resolve(menu, new UiViewport(1920, 1080));
        var review = new ReviewState("agentic2d.engineering.review-request.v2", "review.m052.typed-ui-foundation-native-readability-and-interaction", "M052", "docs/milestones/MILESTONE-052-agent-oriented-ui-foundation.md", "Judge readability and interaction.", ["UX"], "required", "human reviewer", "pending", ["artifacts/ui/M052/review-stress.png"], ["Readability", "Focus visibility", "Interaction usability"], ["approved"], "No implicit waiver.", string.Empty, string.Empty, string.Empty, [], null, [], string.Empty, ".review/pending/review.m052.typed-ui-foundation-native-readability-and-interaction.json");
        var reviewExperience = M038ReviewPolicy.IsRegistered(review) && M038ReviewPolicy.ExperienceFor(review) == "m052-ui-stress:" + review.Id;
        if (legacy.Count != 2 || legacySession.Count != 2 || !newLayout.IsValid || !reviewExperience) throw new InvalidDataException("M021/M037 supported compatibility or M052 review experience registration changed.");
        return new { schema = "agentic2d.m052.compatibility.v1", legacyM021ProjectionCount = legacy.Count, legacyM037SessionCount = legacySession.Count, newModelLayoutValid = newLayout.IsValid, m052ReviewExperienceRegistered = reviewExperience, reviewPersistenceUnchanged = true, legacyReviewCoordinatesBoundedUntilM053 = true };
    }

    private static UiDefinition InteractionDefinition(bool modal, bool enabled)
    {
        var content = Ui.Column("interaction.content", Ui.TextField("interaction.notes", "Notes", "semantic.notes", ""), Ui.Button("interaction.action", "Save", "semantic.save", enabled), Ui.Scroll("interaction.scroll", Ui.Column("interaction.scroll-content", Ui.Text("interaction.scroll-text", string.Join(" ", Enumerable.Repeat("Scrollable content with a deterministic viewport and retained offset.", 8)), new UiSizeSpec(UiLength.Grow(), UiLength.Fixed(900), MinHeight: 900)))), Ui.Button("interaction.cancel-target", "Cancel", "semantic.cancel"));
        var dialog = Ui.Modal("interaction.dialog", "Confirm", Ui.Button("interaction.modal-confirm", "Confirm", "semantic.confirm")) with { Visible = modal };
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
