using System.Runtime.InteropServices;
using System.Text.Json;
using Agentic2D.Tools;
using Agentic2D.UI;
using Raylib_cs;
using Agentic2D.DebugClient.Raylib;
using RaylibApi = Raylib_cs.Raylib;

namespace Agentic2D.DebugClient;

/// <summary>Native input adapter for the canonical provider-side asset-workbench command stream.</summary>
public static class AssetWorkbenchRaylibWindow
{
    public static unsafe int Run(string[] args)
    {
        string? sessionPath = null, commandPath = null, capture = null, initialText = null, initialMessage = null, inputProbeReportPath = null;
        var frames = 0;
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--session" && ++index < args.Length) sessionPath = args[index];
            else if (args[index] == "--commands" && ++index < args.Length) commandPath = args[index];
            else if (args[index] == "--capture" && ++index < args.Length) capture = args[index];
            else if (args[index] == "--frames" && ++index < args.Length && int.TryParse(args[index], out frames)) { }
            else if (args[index] == "--initial-text" && ++index < args.Length) initialText = args[index];
            else if (args[index] == "--message" && ++index < args.Length) initialMessage = args[index];
            else if (args[index] == "--probe-report" && ++index < args.Length) inputProbeReportPath = args[index];
            else return Usage();
        }
        if (string.IsNullOrWhiteSpace(sessionPath) || string.IsNullOrWhiteSpace(commandPath) || !File.Exists(sessionPath)) return Usage();
        using var document = JsonDocument.Parse(File.ReadAllText(sessionPath));
        var root = document.RootElement;
        var sessionId = root.GetProperty("id").GetString() ?? "workbench-session.unknown";
        var candidates = root.GetProperty("candidates").EnumerateArray().Select(x => x.GetString() ?? "candidate.unknown").ToArray();
        var generation = root.GetProperty("aliasGeneration").GetInt32();
        var activeCandidate = root.TryGetProperty("activeCandidate", out var active) ? active.GetString() : null;
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(commandPath))!);
        var draft = initialText ?? string.Empty;
        var feedback = initialMessage ?? "Type a visible choice or bounded command, then use Submit.";
        UiSession? retainedSession = null;
        var definition = M053WorkbenchScreens.AssetWorkbench(new(sessionId, generation, candidates, activeCandidate, draft, feedback));
        RaylibGameWindow.InitDesktopWindow("Agentic2D — Asset Workbench");
        if (!RaylibApi.IsWindowReady()) { Console.Error.WriteLine("asset workbench could not initialize the Raylib window"); return 1; }
        try
        {
            RaylibApi.SetTargetFPS(60);
            var session = new UiSession(definition, RaylibGameWindow.CurrentViewport());
            retainedSession = session;
            var observedActions = new List<string>();
            void WriteProbe(string status)
            {
                if (inputProbeReportPath is null) return;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(inputProbeReportPath))!);
                File.WriteAllText(inputProbeReportPath, JsonSerializer.Serialize(new
                {
                    status,
                    processId = Environment.ProcessId,
                    windowHandle = (long)(nint)RaylibApi.GetWindowHandle(),
                    viewport = session.Layout.Viewport,
                    observedActions,
                    commandFile = commandPath,
                    commandLines = File.Exists(commandPath) ? File.ReadAllLines(commandPath) : [],
                    controls = session.Layout.Elements.Where(element => element.Enabled && (element.ActionId == "asset-workbench.choose" || element.Id == "asset-workbench.command"))
                        .Select(element => new { element.Id, element.ActionId, element.PhysicalBounds }).ToArray()
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
            WriteProbe("ready");
            var viewport = session.Layout.Viewport;
            for (var frame = 0; !RaylibApi.WindowShouldClose() && (frames <= 0 || frame < frames); frame++)
            {
                var currentViewport = RaylibGameWindow.CurrentViewport();
                if (currentViewport != viewport) { viewport = currentViewport; session.Recompose(definition, viewport); }
                if (!RaylibApi.IsWindowFocused() && session.CapturedId is not null) session.Dispatch(new UiInputEvent("pointer-cancel"));
                session.Dispatch(new UiInputEvent("time", Delta: RaylibApi.GetFrameTime()));
                var inputSource = RaylibWorkbenchInput.Dispatch(session);
                foreach (var action in session.DrainActions())
                {
                    observedActions.Add(action.ActionId + ":" + action.Value + "@" + inputSource);
                    switch (action.ActionId)
                    {
                        case "asset-workbench.choose":
                            var selected = Convert.ToInt32(action.Value, System.Globalization.CultureInfo.InvariantCulture);
                            if (selected >= 0 && selected < candidates.Length)
                            {
                                Emit((selected + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), inputSource is "enter" or "mouse-touch" ? inputSource : "mouse-touch");
                                feedback = $"Choice {selected + 1} submitted with current session generation {generation}.";
                            }
                            break;
                        case "asset-workbench.submit":
                            draft = action.Value?.ToString() ?? session.Snapshot().Drafts.GetValueOrDefault("asset-workbench.command", draft);
                            Submit(draft, action.SourceId == "asset-workbench.command" ? "enter" : "submit-button");
                            break;
                        case "asset-workbench.clear":
                            draft = string.Empty; session.UpdateTextDraft("asset-workbench.command", draft); feedback = "Entry cleared; no command was submitted.";
                            break;
                        case "asset-workbench.paste":
                            var clipboard = Marshal.PtrToStringUTF8((IntPtr)RaylibApi.GetClipboardText());
                            if (!string.IsNullOrEmpty(clipboard)) { draft = session.Snapshot().Drafts.GetValueOrDefault("asset-workbench.command", draft) + clipboard; session.UpdateTextDraft("asset-workbench.command", draft); feedback = "Pasted text is editable; use Submit to act."; }
                            else feedback = "Clipboard is empty; no command was submitted.";
                            break;
                        case "asset-workbench.accept": Emit("decision accept-proposal", inputSource is "enter" or "mouse-touch" ? inputSource : "mouse-touch"); feedback = "Accept decision submitted through the canonical command model."; break;
                        case "asset-workbench.reject": Emit("decision reject", inputSource is "enter" or "mouse-touch" ? inputSource : "mouse-touch"); feedback = "Reject decision submitted through the canonical command model."; break;
                        case "asset-workbench.defer": Emit("decision defer", inputSource is "enter" or "mouse-touch" ? inputSource : "mouse-touch"); feedback = "Defer decision submitted through the canonical command model."; break;
                        case "asset-workbench.presentation-only": Emit("decision approve-with-corrections presentation-only", inputSource is "enter" or "mouse-touch" ? inputSource : "mouse-touch"); feedback = "Presentation-only decision submitted; no gameplay binding is created."; break;
                    }
                }
                var currentDraft = session.Snapshot().Drafts.GetValueOrDefault("asset-workbench.command", draft);
                if (currentDraft != draft) draft = currentDraft;
                if (feedback != ReadFeedback(definition))
                {
                    definition = M053WorkbenchScreens.AssetWorkbench(new(sessionId, generation, candidates, activeCandidate, draft, feedback));
                    session.Recompose(definition, currentViewport);
                }
                RaylibWorkbenchInput.Render(session);
                WriteProbe("ready");
                if (capture is not null && frame == 1)
                {
                    var absolute = Path.GetFullPath(capture); Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
                    RaylibApi.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), absolute));
                }
            }
            return 0;
        }
        finally { if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow(); }

        void Submit(string value, string source)
        {
            if (string.IsNullOrWhiteSpace(value)) { feedback = "Enter a number or bounded command before submitting."; return; }
            Emit(value, source); draft = string.Empty; retainedSession?.UpdateTextDraft("asset-workbench.command", draft); feedback = "Submitted; the provider validates the canonical command.";
        }
        void Emit(string value, string source) => File.AppendAllText(commandPath!, JsonSerializer.Serialize(new { schema = "agentic2d.asset-workbench-input-command.v1", sessionId, source, value, generation }) + "\n");
    }

    private static string ReadFeedback(UiDefinition definition) => definition.Root.Items.SelectMany(Flatten).FirstOrDefault(item => item.Id == "asset-workbench.feedback")?.Text ?? string.Empty;
    private static IEnumerable<UiElementDefinition> Flatten(UiElementDefinition item) { yield return item; foreach (var child in item.Items) foreach (var nested in Flatten(child)) yield return nested; }
    private static int Usage() { Console.Error.WriteLine("asset-workbench requires --session <review-session.json> --commands <input-command.jsonl> [--capture <png>] [--frames <count>] [--initial-text <text>] [--message <text>]"); return 2; }
}
