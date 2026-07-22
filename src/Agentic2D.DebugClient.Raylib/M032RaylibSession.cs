using System.Numerics;
using System.Text.Json;
using Agentic2D.Rendering;
using Agentic2D.Simulation;
using Raylib_cs;
using Rl = Raylib_cs.Raylib;

namespace Agentic2D.DebugClient;

/// <summary>
/// Small M032-only debug adapter. It maps mouse/keyboard or recorded semantic commands to
/// designation commands; it never advances or owns simulation authority.
/// </summary>
internal static class M032RaylibSession
{
    private const int Tile = 48;
    private const int OriginX = 120;
    private const int OriginY = 100;

    public static int Run(string[] arguments)
    {
        string? input = null, capture = null, commandsPath = null;
        var interactive = false;
        for (var index = 0; index < arguments.Length; index++)
        {
            if (arguments[index] == "--input" && ++index < arguments.Length) input = arguments[index];
            else if (arguments[index] == "--capture" && ++index < arguments.Length) capture = arguments[index];
            else if (arguments[index] == "--commands" && ++index < arguments.Length) commandsPath = arguments[index];
            else if (arguments[index] == "--interactive") interactive = true;
            else return 2;
        }
        if (string.IsNullOrWhiteSpace(input) || (!interactive && string.IsNullOrWhiteSpace(capture))) return 2;
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(input));
            var frame = document.RootElement.GetProperty("frame").Deserialize<RenderFrame>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new InvalidOperationException("M032 structural frame is invalid.");
            var overlay = document.RootElement.TryGetProperty("overlay", out var value) ? value.GetString() ?? "" : "";
            var world = M032AutonomousDetailedRegion.CreateInitial();
            if (document.RootElement.TryGetProperty("simulationSave", out var saveValue))
            {
                var save = JsonSerializer.Deserialize<SimulationSave>(saveValue.GetRawText(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() } }) ?? throw new InvalidOperationException("M032 structural simulation state is invalid.");
                var loaded = SimulationWorld.Load(save, M032AutonomousDetailedRegion.Registrations());
                if (!loaded.Success || loaded.World is null) throw new InvalidOperationException("M032 structural simulation state could not load.");
                world = loaded.World;
            }
            var events = new List<object>();
            if (!string.IsNullOrWhiteSpace(commandsPath))
                foreach (var line in File.ReadLines(commandsPath).Where(line => !string.IsNullOrWhiteSpace(line)))
                {
                    using var commandDocument = JsonDocument.Parse(line);
                    Apply(world, commandDocument.RootElement, events, "recorded");
                }
            RunWindow(world, frame, overlay, capture, interactive, events);
            if (!string.IsNullOrWhiteSpace(capture))
            {
                var evidence = Path.ChangeExtension(capture, ".input-evidence.json");
                File.WriteAllText(evidence, JsonSerializer.Serialize(new { schema = "agentic2d.m032.designation-input-evidence.v1", status = "passed", controls = new[] { "mouse-drag-create", "1-extraction-tool", "2-storage-tool", "tab-select", "w-worker-inspect", "e-enable-disable", "plus-minus-priority", "delete-remove" }, events, designations = M032AutonomousDetailedRegion.InspectDesignations(world) }, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("M032 debug session failed: " + exception.Message);
            return 3;
        }
    }

    private static void RunWindow(SimulationWorld world, RenderFrame frame, string overlay, string? capture, bool interactive, List<object> events)
    {
        Rl.InitWindow(960, 540, "Agentic2D M032 detailed region");
        var tool = "resource-extraction"; var selected = "designation.extraction.001"; var selectedWorker = "worker.001"; DetailedCell? dragStart = null; var sequence = 0;
        try
        {
            while (!Rl.WindowShouldClose())
            {
                if (interactive)
                {
                    if (Rl.IsKeyPressed(KeyboardKey.Escape)) break;
                    if (Rl.IsKeyPressed(KeyboardKey.One)) tool = "resource-extraction";
                    if (Rl.IsKeyPressed(KeyboardKey.Two)) tool = "storage";
                    var designations = M032AutonomousDetailedRegion.InspectDesignations(world).OrderBy(x => x.Id, StringComparer.Ordinal).ToArray();
                    var workers = world.Entities.Where(entity => entity.Id.StartsWith("worker.", StringComparison.Ordinal)).OrderBy(entity => entity.Id, StringComparer.Ordinal).Select(entity => entity.Id).ToArray();
                    if (Rl.IsKeyPressed(KeyboardKey.W) && workers.Length != 0) { var currentWorker = Array.IndexOf(workers, selectedWorker); selectedWorker = workers[(currentWorker + 1) % workers.Length]; }
                    if (Rl.IsKeyPressed(KeyboardKey.Tab) && designations.Length != 0) { var current = Array.FindIndex(designations, designation => designation.Id == selected); selected = designations[(current + 1) % designations.Length].Id; }
                    if (Rl.IsKeyPressed(KeyboardKey.E)) { var value = designations.FirstOrDefault(designation => designation.Id == selected); if (value is not null) { var result = M032AutonomousDetailedRegion.SetDesignationEnabled(world, selected, !value.Enabled); events.Add(new { source = "keyboard", action = "set-enabled", selected, result = result.Status, enabled = !value.Enabled }); } }
                    if (Rl.IsKeyPressed(KeyboardKey.Equal) || Rl.IsKeyPressed(KeyboardKey.Minus)) { var value = designations.FirstOrDefault(designation => designation.Id == selected); if (value is not null) { var priority = Math.Max(0, value.Priority + (Rl.IsKeyPressed(KeyboardKey.Equal) ? 1 : -1)); var result = M032AutonomousDetailedRegion.SetDesignationPriority(world, selected, priority); events.Add(new { source = "keyboard", action = "set-priority", selected, result = result.Status, priority }); } }
                    if (Rl.IsKeyPressed(KeyboardKey.Delete) && selected.StartsWith("designation.player.", StringComparison.Ordinal)) { var result = M032AutonomousDetailedRegion.RemoveDesignation(world, selected); events.Add(new { source = "keyboard", action = "remove", selected, result = result.Status }); selected = "designation.extraction.001"; }
                    var mouse = Rl.GetMousePosition();
                    if (Rl.IsMouseButtonPressed(MouseButton.Left)) dragStart = Cell(mouse);
                    if (Rl.IsMouseButtonReleased(MouseButton.Left) && dragStart is { } start)
                    {
                        var end = Cell(mouse); var id = "designation.player." + (tool == "storage" ? "storage" : "extraction") + "." + (++sequence).ToString("D3", System.Globalization.CultureInfo.InvariantCulture);
                        var result = M032AutonomousDetailedRegion.CreateDesignation(world, new WorkDesignation(id, tool, "region.forest.active", Cells(start, end), 10, true, 1));
                        events.Add(new { source = "mouse", action = "create", id, tool, start, end, result = result.Status }); if (result.Status == "accepted") selected = id; dragStart = null;
                    }
                }
                Draw(world, frame, overlay, tool, selected, selectedWorker, dragStart);
                if (!interactive || capture is not null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture!))!); Rl.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(capture!))); break;
                }
            }
        }
        finally { if (Rl.IsWindowReady()) Rl.CloseWindow(); }
    }

    private static void Draw(SimulationWorld world, RenderFrame frame, string overlay, string tool, string selected, string selectedWorker, DetailedCell? dragStart)
    {
        Rl.BeginDrawing(); Rl.ClearBackground(new Color(20, 31, 48, 255));
        foreach (var item in frame.Items.Where(item => item.Layer != "ui" && item.SourceKind != "designation")) DrawItem(item);
        foreach (var designation in M032AutonomousDetailedRegion.InspectDesignations(world).OrderBy(x => x.Id, StringComparer.Ordinal)) foreach (var cell in designation.Cells) { var color = designation.Enabled ? new Color(60, 208, 235, 115) : new Color(130, 130, 130, 85); Rl.DrawRectangle(OriginX + cell.X * Tile, OriginY + cell.Y * Tile, Tile, Tile, color); if (designation.Id == selected) Rl.DrawRectangleLines(OriginX + cell.X * Tile, OriginY + cell.Y * Tile, Tile, Tile, Color.Yellow); }
        if (dragStart is { } start) { var mouse = Cell(Rl.GetMousePosition()); foreach (var cell in Cells(start, mouse)) Rl.DrawRectangleLines(OriginX + cell.X * Tile, OriginY + cell.Y * Tile, Tile, Tile, Color.Orange); }
        var decision = M032AutonomousDetailedRegion.EvaluateWorker(world, selectedWorker, M032AutonomousDetailedRegion.DeriveOpportunities(world, M032AutonomousDetailedRegion.InspectDesignations(world)));
        var activity = world.Activities.LastOrDefault(value => value.ActorEntityId == selectedWorker && value.Status is SimulationActivityStatus.Active or SimulationActivityStatus.Interrupted);
        Rl.DrawText("M032 detailed-region input + projection", 24, 20, 24, Color.White); Rl.DrawText(overlay, 24, 50, 18, Color.SkyBlue); Rl.DrawText($"tool: {tool}; selected designation: {selected}", 24, 76, 16, Color.LightGray); Rl.DrawText($"worker: {selectedWorker}; activity: {activity?.Stage ?? "idle"}; decision: {decision.SelectedOpportunityKey ?? decision.IdleReason}; path: {decision.PathCost}", 24, 96, 16, Color.LightGray); Rl.DrawText("1 extraction  2 storage  mouse drag create  Tab designation  W worker  E toggle  +/- priority  Delete remove", 24, 510, 14, Color.LightGray); Rl.EndDrawing();
    }

    private static void DrawItem(RenderItem item)
    {
        var destination = item.Destination; var color = new Color(item.Tint.R, item.Tint.G, item.Tint.B, item.Tint.A);
        Rl.DrawRectangle(OriginX + (int)Math.Round(destination.Position.X * Tile), OriginY + (int)Math.Round(destination.Position.Y * Tile), Math.Max(2, (int)Math.Round(destination.Size.Width * Tile)), Math.Max(2, (int)Math.Round(destination.Size.Height * Tile)), color);
    }

    private static void Apply(SimulationWorld world, JsonElement command, List<object> events, string source)
    {
        var action = command.GetProperty("action").GetString();
        if (action == "create")
        {
            var id = command.GetProperty("id").GetString()!; var kind = command.GetProperty("kind").GetString()!; var start = new DetailedCell(command.GetProperty("x0").GetInt32(), command.GetProperty("y0").GetInt32()); var end = new DetailedCell(command.GetProperty("x1").GetInt32(), command.GetProperty("y1").GetInt32()); var result = M032AutonomousDetailedRegion.CreateDesignation(world, new WorkDesignation(id, kind, "region.forest.active", Cells(start, end), command.TryGetProperty("priority", out var priority) ? priority.GetInt32() : 10, true, 1)); events.Add(new { source, action, id, kind, start, end, result = result.Status });
        }
        else if (action == "set-enabled") { var id = command.GetProperty("id").GetString()!; var result = M032AutonomousDetailedRegion.SetDesignationEnabled(world, id, command.GetProperty("enabled").GetBoolean()); events.Add(new { source, action, id, result = result.Status }); }
        else if (action == "set-priority") { var id = command.GetProperty("id").GetString()!; var result = M032AutonomousDetailedRegion.SetDesignationPriority(world, id, command.GetProperty("priority").GetInt32()); events.Add(new { source, action, id, result = result.Status }); }
        else if (action == "remove") { var id = command.GetProperty("id").GetString()!; var result = M032AutonomousDetailedRegion.RemoveDesignation(world, id); events.Add(new { source, action, id, result = result.Status }); }
        else throw new InvalidOperationException("unsupported M032 input action " + action);
    }

    private static DetailedCell Cell(Vector2 point) => new(Math.Clamp((int)Math.Floor((point.X - OriginX) / Tile), 0, 12), Math.Clamp((int)Math.Floor((point.Y - OriginY) / Tile), 0, 8));
    private static IReadOnlyList<DetailedCell> Cells(DetailedCell first, DetailedCell second) => Enumerable.Range(Math.Min(first.X, second.X), Math.Abs(first.X - second.X) + 1).SelectMany(x => Enumerable.Range(Math.Min(first.Y, second.Y), Math.Abs(first.Y - second.Y) + 1).Select(y => new DetailedCell(x, y))).OrderBy(cell => cell.Y).ThenBy(cell => cell.X).ToArray();
}
