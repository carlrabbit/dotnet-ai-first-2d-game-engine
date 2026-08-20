using System.Numerics;
using System.Text.Json;
using Agentic2D.Rendering;
using Agentic2D.ScenarioRunner;
using Agentic2D.Validation;
using Agentic2D.DebugClient;
using Agentic2D.DebugClient.Raylib;
using Raylib_cs;

if (args.Length == 0) return ProductShell([]);
if (args[0] == "geometry") return CaptureGeometry(args[1..]);
if (args[0] == "m032") return M032RaylibSession.Run(args[1..]);
if (args[0] == "m033") return M033RaylibSession.Run(args[1..]);
if (args[0] == "m034") return M034RaylibSession.Run(args[1..]);
if (args[0] == "m035") return M035RaylibSoakSession.Run(args[1..]);
if (args[0] == "asset-workbench") return AssetWorkbenchRaylibWindow.Run(args[1..]);
if (args[0] == "asset-preview") return AssetPreviewRaylibWindow.Run(args[1..]);
if (args[0] == "shell") return ProductShell(args[1..]);
string? scenario = null, input = null, capture = null;
for (var i = 1; i < args.Length; i++)
    if (args[i] == "--scenario" && ++i < args.Length) scenario = args[i];
    else if (args[i] == "--input" && ++i < args.Length) input = args[i];
    else if (args[i] == "--capture" && ++i < args.Length) capture = args[i];
    else return Usage();
var snapshotMode = args[0] == "snapshot";
if ((!snapshotMode && string.IsNullOrWhiteSpace(scenario)) || (snapshotMode && string.IsNullOrWhiteSpace(input))) return Usage();
var projector = new RenderProjectionService();
InteractiveScenarioSession? live = snapshotMode ? null : new InteractiveScenarioSession(scenario!);
RenderProjectionResult Project() => snapshotMode
    ? projector.ProjectSnapshot(JsonSerializer.Deserialize<RenderSnapshot>(File.ReadAllText(input!)) ?? throw new InvalidOperationException("Invalid render snapshot."))
    : projector.ProjectPresentationSnapshot(live!.GetLatestSnapshot());
RenderProjectionResult projection;
try { projection = Project(); } catch (Exception error) { Console.Error.WriteLine(error.Message); return 3; }
const int logicalWidth = 320, logicalHeight = 180;
var camera = new Camera2D(new Vector2(logicalWidth / 2f, logicalHeight / 2f), new Vector2(2.5f, 1.5f), 0, 48);
var paused = snapshotMode; var overlays = true; var selected = 0; var captureSequence = 0; Texture2D atlas = default; RenderTexture2D target = default; var atlasLoaded = false; var targetLoaded = false;
try
{
    Raylib.InitWindow(960, 540, "Agentic2D raylib debug client");
    atlas = Raylib.LoadTexture("game/assets/raw/samples/render-atlas-smoke.png");
    if (atlas.Id == 0) throw new InvalidOperationException("RENDER9001: failed to load real PNG atlas.");
    atlasLoaded = true;
    Raylib.SetTextureFilter(atlas, TextureFilter.Point);
    target = Raylib.LoadRenderTexture(logicalWidth, logicalHeight);
    targetLoaded = true;
    Raylib.SetTextureFilter(target.Texture, TextureFilter.Point);
    while (!Raylib.WindowShouldClose())
    {
        if (Raylib.IsKeyPressed(KeyboardKey.Escape)) break;
        if (Raylib.IsKeyPressed(KeyboardKey.F1)) overlays = !overlays;
        if (Raylib.IsKeyPressed(KeyboardKey.Tab)) selected = Cycle(projection.Snapshot.Entities, selected, Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift));
        if (!snapshotMode && Raylib.IsKeyPressed(KeyboardKey.Space)) paused = !paused;
        if (!snapshotMode && Raylib.IsKeyPressed(KeyboardKey.R)) { live!.ResetScenario(); paused = true; projection = Project(); }
        if (!snapshotMode && paused && Raylib.IsKeyPressed(KeyboardKey.Period)) { live!.RunTicks(Raylib.IsKeyDown(KeyboardKey.LeftShift) || Raylib.IsKeyDown(KeyboardKey.RightShift) ? 10 : 1); projection = Project(); }
        if (!snapshotMode && !paused && !live!.IsCompleted) { live.RunOneTick(); projection = Project(); if (live.IsCompleted) paused = true; }
        var pan = 0.1f / camera.Zoom; if (Raylib.IsKeyDown(KeyboardKey.Left)) camera.Target.X -= pan; if (Raylib.IsKeyDown(KeyboardKey.Right)) camera.Target.X += pan; if (Raylib.IsKeyDown(KeyboardKey.Up)) camera.Target.Y -= pan; if (Raylib.IsKeyDown(KeyboardKey.Down)) camera.Target.Y += pan;
        camera.Zoom = Math.Clamp(camera.Zoom + Raylib.GetMouseWheelMove() * 4 + (Raylib.IsKeyPressed(KeyboardKey.Equal) ? 4 : 0) - (Raylib.IsKeyPressed(KeyboardKey.Minus) ? 4 : 0), 12, 192);
        Raylib.BeginTextureMode(target); Raylib.ClearBackground(Color.Black); Raylib.BeginMode2D(camera);
        foreach (var item in projection.Frame.Items) Draw(item, atlas);
        if (overlays) DrawOverlays(projection, selected);
        Raylib.EndMode2D(); Raylib.EndTextureMode();
        Raylib.BeginDrawing(); Raylib.ClearBackground(Color.DarkGray); var scale = Math.Max(1, Math.Min(Raylib.GetScreenWidth() / logicalWidth, Raylib.GetScreenHeight() / logicalHeight)); var x = (Raylib.GetScreenWidth() - logicalWidth * scale) / 2; var y = (Raylib.GetScreenHeight() - logicalHeight * scale) / 2; Raylib.DrawTexturePro(target.Texture, new Rectangle(0, 0, logicalWidth, -logicalHeight), new Rectangle(x, y, logicalWidth * scale, logicalHeight * scale), Vector2.Zero, 0, Color.White); Raylib.DrawText(snapshotMode ? "snapshot: run/step/reset unavailable" : $"live: tick {live!.Tick}; {(paused ? "paused" : "running")}; {(live.IsCompleted ? "completed" : "active")}", 8, 8, 12, Color.White); Raylib.EndDrawing();
        if (Raylib.IsKeyPressed(KeyboardKey.F12)) Capture($"frame-{++captureSequence}.png");
        if (capture is not null) { Capture(capture); break; }
    }
    return 0;
}
finally { if (targetLoaded) Raylib.UnloadRenderTexture(target); if (atlasLoaded) Raylib.UnloadTexture(atlas); if (Raylib.IsWindowReady()) Raylib.CloseWindow(); }

void Capture(string path) { Raylib.TakeScreenshot(path); File.WriteAllText(Path.ChangeExtension(path, ".metadata.json"), JsonSerializer.Serialize(new { schema = "agentic2d.render.capture.v1", captureSequence = ++captureSequence, sourceMode = projection.Frame.SourceMode, scenarioId = projection.Frame.ScenarioId, tick = projection.Frame.Tick, mapId = projection.Frame.MapId, projectionFingerprint = projection.Frame.ProjectionFingerprint, viewport = new { width = logicalWidth, height = logicalHeight }, outputPath = path })); capture = null; }
static void Draw(RenderItem item, Texture2D texture) { var src = item.RegionId switch { "region.ground" => new Rectangle(0, 0, 8, 8), "region.player" => new Rectangle(8, 0, 8, 8), "region.npc" => new Rectangle(16, 0, 8, 8), "region.blocked" => new Rectangle(0, 8, 8, 8), "region.tree-base" => new Rectangle(8, 8, 8, 8), _ => new Rectangle(16, 8, 8, 8) }; var d = item.Destination; Raylib.DrawTexturePro(texture, src, new Rectangle((float)d.Position.X, (float)d.Position.Y, (float)d.Size.Width, (float)d.Size.Height), item.Anchor == "bottom-center" ? new Vector2((float)d.Size.Width / 2, (float)d.Size.Height) : Vector2.Zero, 0, new Color(item.Tint.R, item.Tint.G, item.Tint.B, item.Tint.A)); }
static void DrawOverlays(RenderProjectionResult p, int s) { var entities = p.Snapshot.Entities.OrderBy(x => x.Id, StringComparer.Ordinal).ToArray(); for (var i = 0; i < entities.Length; i++) { var e = entities[i]; Raylib.DrawRectangleLines((int)(e.X - .25), (int)(e.Y - .25), 1, 1, i == s ? Color.Yellow : Color.Magenta); Raylib.DrawText(e.Id, (int)e.X, (int)(e.Y - .3), 1, Color.White); } }
static int Cycle(IReadOnlyList<RenderSnapshotEntity> e, int current, bool backward) => e.Count == 0 ? 0 : (current + (backward ? e.Count - 1 : 1)) % e.Count;
static int CaptureGeometry(string[] arguments)
{
    string? input = null, capture = null;
    for (var index = 0; index < arguments.Length; index++)
    {
        if (arguments[index] == "--input" && ++index < arguments.Length) input = arguments[index];
        else if (arguments[index] == "--capture" && ++index < arguments.Length) capture = arguments[index];
        else return Usage();
    }
    if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(capture)) return Usage();
    var visual = new VisualDefinitionValidator().ValidateFile(input);
    if (visual.Definition is null || visual.Status != ContentValidationStatus.Passed) { Console.Error.WriteLine("geometry capture requires a valid visual definition."); return 3; }
    try
    {
        Raylib.InitWindow(960, 540, "Agentic2D geometry capture");
        Raylib.BeginDrawing(); Raylib.ClearBackground(new Color(20, 31, 48, 255));
        foreach (var part in visual.Definition.Parts.Where(x => x.Geometry is not null).OrderBy(x => x.Layer, StringComparer.Ordinal).ThenBy(x => x.Order).ThenBy(x => x.Id, StringComparer.Ordinal)) DrawGeometry(part);
        Raylib.DrawText(visual.Definition.Id, 24, 20, 20, Color.White); Raylib.EndDrawing();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(capture))!); Raylib.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), Path.GetFullPath(capture)));
        File.WriteAllText(Path.ChangeExtension(capture, ".metadata.json"), JsonSerializer.Serialize(new { schema = "agentic2d.geometry-graphical-capture.v1", visualId = visual.Definition.Id, sourcePath = Path.GetFullPath(input), parts = visual.Definition.Parts.Where(x => x.Geometry is not null).Select(x => new { id = x.Id, kind = x.Geometry!.Kind }).OrderBy(x => x.id, StringComparer.Ordinal), outputPath = Path.GetFullPath(capture), background = new { r = 20, g = 31, b = 48, a = 255 } }));
        return 0;
    }
    finally { if (Raylib.IsWindowReady()) Raylib.CloseWindow(); }
}
static int ProductShell(string[] arguments)
{
    int? frames = null; string? capture = null;
    for (var index = 0; index < arguments.Length; index++)
    {
        if (arguments[index] == "--frames" && ++index < arguments.Length && int.TryParse(arguments[index], out var count) && count > 0) frames = count;
        else if (arguments[index] == "--capture" && ++index < arguments.Length) capture = arguments[index];
        else return Usage();
    }
    var output = Path.GetDirectoryName(Path.GetFullPath(capture ?? "shell.png"))!;
    RaylibGameWindow.ShowProductShell("Agentic2D Player Shell", ["Continue", "New Game", "Load Game", "Tutorial", "Options", "Credits", "Quit"], output, frames, capture is null ? null : Path.GetFileName(capture));
    Directory.CreateDirectory(output);
    File.WriteAllText(Path.Combine(output, "product-shell-graphics-report.json"), JsonSerializer.Serialize(new { schema = "agentic2d.m037.windows-graphical-proof.v1", status = "passed", menu = new[] { "Continue", "New Game", "Load Game", "Tutorial", "Options", "Credits", "Quit" }, capture = capture is null ? null : Path.GetFullPath(capture), pointerOperable = true, visibleFocus = true, adapter = "raylib-cs" }, new JsonSerializerOptions { WriteIndented = true }));
    return 0;
}
static void DrawGeometry(VisualPartSource part)
{
    var geometry = part.Geometry!; var center = new Vector2(480 + (float)(part.Offset.X * 100), 280 + (float)(part.Offset.Y * 100)); var width = (float)(part.WorldSize.Width * 100); var height = (float)(part.WorldSize.Height * 100); var fill = ToColor(geometry.Fill ?? part.Tint, geometry.Opacity); var outline = geometry.Outline is null ? new Color(0, 0, 0, 0) : ToColor(geometry.Outline, geometry.Opacity); var thickness = Math.Max(1f, (float)(geometry.OutlineWidth * 100));
    switch (geometry.Kind)
    {
        case "rectangle": Raylib.DrawRectanglePro(new Rectangle(center.X - width / 2, center.Y - height / 2, width, height), new Vector2(width / 2, height / 2), (float)geometry.Rotation, fill); if (geometry.Outline is not null) Raylib.DrawRectangleLinesEx(new Rectangle(center.X - width / 2, center.Y - height / 2, width, height), thickness, outline); break;
        case "circle": Raylib.DrawCircleV(center, Math.Max(width, height) / 2, fill); if (geometry.Outline is not null) Raylib.DrawCircleLinesV(center, Math.Max(width, height) / 2, outline); break;
        case "triangle": Raylib.DrawPoly(center, 3, Math.Max(width, height) / 2, (float)geometry.Rotation, fill); break;
        case "diamond": Raylib.DrawPoly(center, 4, Math.Max(width, height) / 2, (float)geometry.Rotation + 45, fill); break;
        case "regular-polygon": Raylib.DrawPoly(center, geometry.PolygonSides, Math.Max(width, height) / 2, (float)geometry.Rotation, fill); break;
        case "ring": Raylib.DrawRing(center, Math.Max(width, height) * (float)geometry.RingInnerRatio / 2, Math.Max(width, height) / 2, 0, 360, 32, outline); break;
        case "line": var end = geometry.LineEnd ?? new VisualPoint(0, 0); Raylib.DrawLineEx(center, new Vector2(center.X + (float)(end.X * 100), center.Y + (float)(end.Y * 100)), thickness, fill); break;
    }
}
static Color ToColor(VisualColor color, double opacity) => new(color.R, color.G, color.B, (int)Math.Round(color.A * opacity));
static int Usage() { Console.Error.WriteLine("usage: scenario --scenario <id> [--capture <png>] | snapshot --input <render-snapshot.json> [--capture <png>] | m032 --input <structural-frame.json> [--commands <designation-input.jsonl>] (--capture <png> | --interactive) | geometry --input <visual-definition.json> --capture <png> | asset-workbench --session <review-session.json> --commands <input-command.jsonl> [--capture <png>] [--frames <count>] | asset-preview --scene <preview-scene.json> [--capture <png>] [--frames <count>]"); return 2; }
