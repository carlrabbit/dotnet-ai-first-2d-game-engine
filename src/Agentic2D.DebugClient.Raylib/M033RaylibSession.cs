using System.Text.Json;
using Raylib_cs;
using Rl = Raylib_cs.Raylib;

namespace Agentic2D.DebugClient;

/// <summary>Small read-only visualizer for M033 structural transition evidence.</summary>
internal static class M033RaylibSession
{
    public static int Run(string[] arguments)
    {
        string? input = null, capture = null;
        for (var index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] == "--input" && ++index < arguments.Length) input = arguments[index];
            else if (arguments[index] == "--capture" && ++index < arguments.Length) capture = arguments[index];
            else return Usage();
        }
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(capture) || !File.Exists(input)) return Usage();
        try
        {
            var transitions = File.ReadLines(input).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => JsonDocument.Parse(line).RootElement.Clone()).ToArray();
            Rl.InitWindow(960, 540, "Agentic2D M033 fidelity switch evidence");
            try
            {
                Rl.BeginDrawing(); Rl.ClearBackground(new Color(20, 31, 48, 255));
                Rl.DrawText("M033: abstract / detailed region switch", 28, 24, 28, Color.RayWhite);
                var regions = new[] { "alpha", "beta", "gamma" };
                for (var index = 0; index < regions.Length; index++)
                {
                    var active = index == 0;
                    Rl.DrawRectangle(70 + index * 280, 140, 220, 130, active ? new Color(52, 132, 196, 255) : new Color(59, 84, 110, 255));
                    Rl.DrawText("region." + regions[index], 90 + index * 280, 165, 22, Color.RayWhite);
                    Rl.DrawText(active ? "detailed executor" : "abstract executor", 90 + index * 280, 205, 18, Color.RayWhite);
                }
                Rl.DrawText("Transition evidence: " + transitions.Length + " committed mappings", 70, 335, 22, Color.RayWhite);
                Rl.DrawText("materialize -> valid cell -> route rebuild -> abstract next trigger", 70, 375, 18, Color.LightGray);
                Rl.DrawText("Read-only visual adapter; structural JSON remains semantic authority.", 70, 455, 16, Color.LightGray);
                Rl.EndDrawing();
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture))!);
                Rl.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(capture)));
                File.WriteAllText(Path.ChangeExtension(capture, ".metadata.json"), JsonSerializer.Serialize(new { schema = "agentic2d.m033.graphical-switch-capture.v1", input, capture, transitionCount = transitions.Length, readOnly = true }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
            finally { if (Rl.IsWindowReady()) Rl.CloseWindow(); }
            return 0;
        }
        catch (Exception exception) when (exception is IOException or JsonException)
        {
            Console.Error.WriteLine("m033 graphical capture failed: " + exception.Message); return 1;
        }
    }

    private static int Usage() { Console.Error.WriteLine("m033 requires --input <transition-events.jsonl> --capture <png>"); return 2; }
}
