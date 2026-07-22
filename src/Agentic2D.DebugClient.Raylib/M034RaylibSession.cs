using System.Text.Json;
using Raylib_cs;
using Rl = Raylib_cs.Raylib;

namespace Agentic2D.DebugClient;

/// <summary>Read-only M034 operations dashboard renderer over the structural world projection.</summary>
internal static class M034RaylibSession
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
            using var document = JsonDocument.Parse(File.ReadAllText(input));
            var regions = document.RootElement.GetProperty("regions").EnumerateArray().Select(item => item.Clone()).ToArray();
            Rl.InitWindow(1200, 680, "Agentic2D M034 settlement operations");
            try
            {
                Rl.BeginDrawing(); Rl.ClearBackground(new Color(20, 31, 48, 255));
                Rl.DrawText("M034: settlement operations", 30, 24, 30, Color.RayWhite);
                Rl.DrawText("Read-only dashboard: plans, reserves, capacity, maintenance, alerts, and fidelity", 30, 64, 18, Color.LightGray);
                for (var index = 0; index < regions.Length; index++) DrawRegion(regions[index], 30 + index * 390, 120);
                Rl.DrawText("Commands issue validated simulation requests; workers are never ordered directly.", 30, 570, 18, Color.LightGray);
                Rl.DrawText("Structural dashboard and factual journal remain semantic authority.", 30, 610, 16, Color.LightGray);
                Rl.EndDrawing();
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture))!);
                Rl.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(capture)));
                File.WriteAllText(Path.ChangeExtension(capture, ".metadata.json"), JsonSerializer.Serialize(new { schema = "agentic2d.m034.graphical-operations-capture.v1", input, capture, regions = regions.Length, readOnly = true, surface = new[] { "plans", "reserves", "capacity", "maintenance", "alerts", "fidelity" } }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
            finally { if (Rl.IsWindowReady()) Rl.CloseWindow(); }
            return 0;
        }
        catch (Exception exception) when (exception is IOException or JsonException or KeyNotFoundException)
        {
            Console.Error.WriteLine("m034 graphical capture failed: " + exception.Message); return 1;
        }
    }

    private static void DrawRegion(JsonElement region, int x, int y)
    {
        var detailed = region.GetProperty("fidelity").GetString() == "Detailed";
        var color = detailed ? new Color(52, 132, 196, 255) : new Color(59, 84, 110, 255);
        Rl.DrawRectangle(x, y, 350, 385, color); Rl.DrawRectangleLines(x, y, 350, 385, detailed ? Color.Yellow : Color.LightGray);
        var id = region.GetProperty("regionId").GetString() ?? "region";
        Rl.DrawText(id, x + 18, y + 18, 22, Color.RayWhite);
        Rl.DrawText(detailed ? "DETAILED" : "ABSTRACT", x + 18, y + 52, 18, detailed ? Color.Yellow : Color.LightGray);
        Rl.DrawText("water " + region.GetProperty("waterStored").GetInt32() + "/" + region.GetProperty("waterCapacity").GetInt32(), x + 18, y + 96, 20, Color.RayWhite);
        Rl.DrawText("food  " + region.GetProperty("foodStored").GetInt32() + "/" + region.GetProperty("foodCapacity").GetInt32(), x + 18, y + 128, 20, Color.RayWhite);
        Rl.DrawText("comfort " + region.GetProperty("comfortCapacity").GetInt32(), x + 18, y + 160, 20, Color.RayWhite);
        Rl.DrawText("plans: " + region.GetProperty("plans").GetArrayLength() + "  structures: " + region.GetProperty("structures").GetArrayLength(), x + 18, y + 208, 17, Color.RayWhite);
        var activeAlerts = region.GetProperty("alerts").EnumerateArray().Count(alert => alert.GetProperty("status").GetString() == "Active");
        Rl.DrawText("alerts: " + activeAlerts + "  backlog: " + region.GetProperty("backlog").GetString(), x + 18, y + 240, 17, activeAlerts > 0 ? Color.Orange : Color.Lime);
        Rl.DrawText("explain: work and shortages are causal", x + 18, y + 305, 15, Color.LightGray);
    }

    private static int Usage() { Console.Error.WriteLine("m034 requires --input <world-dashboard.json> --capture <png>"); return 2; }
}
