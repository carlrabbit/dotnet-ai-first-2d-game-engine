using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Raylib_cs;

namespace Agentic2D.DebugClient;

/// <summary>
/// Isolated native workbench surface. It owns presentation and text/mouse translation only;
/// the provider CLI validates and applies the emitted canonical input commands.
/// </summary>
public static class AssetWorkbenchRaylibWindow
{
    public static unsafe int Run(string[] args)
    {
        string? sessionPath = null, commandPath = null, capture = null, initialText = null, initialMessage = null; var frames = 0;
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--session" && ++index < args.Length) sessionPath = args[index];
            else if (args[index] == "--commands" && ++index < args.Length) commandPath = args[index];
            else if (args[index] == "--capture" && ++index < args.Length) capture = args[index];
            else if (args[index] == "--frames")
            {
                if (++index >= args.Length || !int.TryParse(args[index], out frames)) return Usage();
            }
            else if (args[index] == "--initial-text" && ++index < args.Length) initialText = args[index];
            else if (args[index] == "--message" && ++index < args.Length) initialMessage = args[index];
            else return Usage();
        }
        if (string.IsNullOrWhiteSpace(sessionPath) || string.IsNullOrWhiteSpace(commandPath) || !File.Exists(sessionPath)) return Usage();
        using var document = JsonDocument.Parse(File.ReadAllText(sessionPath)); var root = document.RootElement;
        var sessionId = root.GetProperty("id").GetString() ?? "workbench-session.unknown";
        var candidates = root.GetProperty("candidates").EnumerateArray().Select(x => x.GetString() ?? "candidate.unknown").ToArray();
        var generation = root.GetProperty("aliasGeneration").GetInt32();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(commandPath))!);
        var buffer = initialText ?? string.Empty; var message = initialMessage ?? "Type a visible choice or bounded command, then use Submit."; var focus = true; var captured = false;
        try
        {
            global::Raylib_cs.Raylib.InitWindow(960, 620, "Agentic2D Asset Workbench"); global::Raylib_cs.Raylib.SetTargetFPS(60);
            for (var frame = 0; !global::Raylib_cs.Raylib.WindowShouldClose() && (frames <= 0 || frame < frames); frame++)
            {
                var mouse = global::Raylib_cs.Raylib.GetMousePosition();
                var charCode = global::Raylib_cs.Raylib.GetCharPressed();
                while (charCode > 0) { if (charCode >= 32 && charCode <= 126) buffer += (char)charCode; charCode = global::Raylib_cs.Raylib.GetCharPressed(); }
                if (global::Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Backspace) && buffer.Length > 0) buffer = buffer[..^1];
                if (global::Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Delete)) buffer = string.Empty;
                if (global::Raylib_cs.Raylib.IsKeyPressed(KeyboardKey.Enter)) Submit(buffer, "enter");
                var field = new Rectangle(32, 420, 580, 52); var submit = new Rectangle(628, 420, 130, 52); var clear = new Rectangle(772, 420, 90, 52); var accept = new Rectangle(32, 486, 155, 42); var reject = new Rectangle(201, 486, 115, 42); var defer = new Rectangle(330, 486, 105, 42); var presentationOnly = new Rectangle(449, 486, 188, 42); var paste = new Rectangle(651, 486, 110, 42);
                if (Clicked(field, mouse)) focus = true;
                if (Clicked(submit, mouse)) Submit(buffer, "submit-button");
                if (Clicked(clear, mouse)) { buffer = string.Empty; message = "Entry cleared; no command was submitted."; }
                if (Clicked(accept, mouse)) { Emit("decision accept-proposal", "mouse-touch"); message = "Accept decision submitted through the canonical command model."; }
                if (Clicked(reject, mouse)) { Emit("decision reject", "mouse-touch"); message = "Reject decision submitted through the canonical command model."; }
                if (Clicked(defer, mouse)) { Emit("decision defer", "mouse-touch"); message = "Defer decision submitted through the canonical command model."; }
                if (Clicked(presentationOnly, mouse)) { Emit("decision approve-with-corrections presentation-only", "mouse-touch"); message = "Presentation-only decision submitted; no gameplay binding is created."; }
                if (Clicked(paste, mouse))
                {
                    var clipboard = Marshal.PtrToStringUTF8((IntPtr)global::Raylib_cs.Raylib.GetClipboardText());
                    if (!string.IsNullOrEmpty(clipboard)) { buffer += clipboard; message = "Pasted text is editable; use Submit to act."; }
                    else message = "Clipboard is empty; no command was submitted.";
                }
                for (var candidate = 0; candidate < candidates.Length; candidate++)
                {
                    var row = new Rectangle(32, 116 + candidate * 58, 830, 46);
                    if (Clicked(row, mouse)) { Emit((candidate + 1).ToString(), "mouse-touch"); message = "Choice " + (candidate + 1) + " submitted through the canonical command model."; }
                }
                global::Raylib_cs.Raylib.BeginDrawing(); global::Raylib_cs.Raylib.ClearBackground(new Color(18, 27, 42, 255));
                global::Raylib_cs.Raylib.DrawText("ASSET WORKBENCH", 32, 28, 28, Color.RayWhite); global::Raylib_cs.Raylib.DrawText("Session " + sessionId, 32, 64, 15, Color.LightGray);
                global::Raylib_cs.Raylib.DrawText("Visible choices — click/touch a row or enter its number below", 32, 92, 17, Color.SkyBlue);
                for (var candidate = 0; candidate < candidates.Length; candidate++)
                {
                    var row = new Rectangle(32, 116 + candidate * 58, 830, 46); global::Raylib_cs.Raylib.DrawRectangleRec(row, new Color(37, 54, 79, 255)); global::Raylib_cs.Raylib.DrawRectangleLinesEx(row, 2, Color.SkyBlue);
                    global::Raylib_cs.Raylib.DrawText((candidate + 1) + ".", 48, 129 + candidate * 58, 21, Color.Gold); global::Raylib_cs.Raylib.DrawText(candidates[candidate], 90, 131 + candidate * 58, 19, Color.RayWhite);
                }
                global::Raylib_cs.Raylib.DrawText("Editable command field", 32, 394, 17, Color.SkyBlue); global::Raylib_cs.Raylib.DrawRectangleRec(field, focus ? new Color(245, 245, 245, 255) : new Color(180, 180, 180, 255)); global::Raylib_cs.Raylib.DrawText(buffer, 44, 435, 20, Color.Black);
                Button(submit, "Submit", new Color(60, 152, 93, 255)); Button(clear, "Clear", new Color(130, 88, 62, 255)); Button(accept, "Accept", new Color(60, 152, 93, 255)); Button(reject, "Reject", new Color(154, 66, 66, 255)); Button(defer, "Defer", new Color(126, 104, 58, 255)); Button(presentationOnly, "Presentation only", new Color(92, 80, 151, 255)); Button(paste, "Paste", new Color(72, 104, 158, 255));
                global::Raylib_cs.Raylib.DrawText(message, 32, 550, 16, Color.LightGray); global::Raylib_cs.Raylib.DrawText("Enter is an accelerator only. Focus changes never submit text.", 32, 578, 14, Color.Gray); global::Raylib_cs.Raylib.EndDrawing();
                if (capture is not null && !captured)
                {
                    var absoluteCapture = Path.GetFullPath(capture);
                    Directory.CreateDirectory(Path.GetDirectoryName(absoluteCapture)!);
                    global::Raylib_cs.Raylib.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), absoluteCapture));
                    captured = true;
                }
            }
            return 0;
        }
        finally { if (global::Raylib_cs.Raylib.IsWindowReady()) global::Raylib_cs.Raylib.CloseWindow(); }

        void Submit(string value, string source)
        {
            if (string.IsNullOrWhiteSpace(value)) { message = "Enter a number or bounded command before submitting."; return; }
            Emit(value, source); buffer = string.Empty; message = "Submitted; the provider validates the canonical command.";
        }
        void Emit(string value, string source) => File.AppendAllText(commandPath, JsonSerializer.Serialize(new { schema = "agentic2d.asset-workbench-input-command.v1", sessionId, source, value, generation }) + "\n");
    }

    private static bool Clicked(Rectangle rectangle, Vector2 mouse) => global::Raylib_cs.Raylib.IsMouseButtonPressed(MouseButton.Left) && global::Raylib_cs.Raylib.CheckCollisionPointRec(mouse, rectangle);
    private static void Button(Rectangle rectangle, string text, Color color) { global::Raylib_cs.Raylib.DrawRectangleRec(rectangle, color); global::Raylib_cs.Raylib.DrawRectangleLinesEx(rectangle, 2, Color.RayWhite); global::Raylib_cs.Raylib.DrawText(text, (int)rectangle.X + 14, (int)rectangle.Y + 15, 18, Color.RayWhite); }
    private static int Usage() { Console.Error.WriteLine("asset-workbench requires --session <review-session.json> --commands <input-command.jsonl> [--capture <png>] [--frames <count>] [--initial-text <text>] [--message <text>]"); return 2; }
}
