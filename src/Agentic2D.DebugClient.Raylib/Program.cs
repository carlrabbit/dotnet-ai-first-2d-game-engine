using System.Numerics;
using System.Text.Json;
using Agentic2D.Rendering;
using Agentic2D.ScenarioRunner;
using Raylib_cs;

if (args.Length == 0) return Usage();
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
static int Usage() { Console.Error.WriteLine("usage: scenario --scenario <id> [--capture <png>] | snapshot --input <render-snapshot.json> [--capture <png>]"); return 2; }
