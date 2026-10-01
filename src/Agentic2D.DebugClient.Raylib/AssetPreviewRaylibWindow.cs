using System.Numerics;
using System.Text.Json;
using Agentic2D.Rendering;
using Agentic2D.Tools;
using Raylib_cs;

namespace Agentic2D.DebugClient;

/// <summary>One generic native preview process with structured modality-specific review surfaces.</summary>
public static class AssetPreviewRaylibWindow
{
    public static int Run(string[] args)
    {
        string? scenePath = null, capture = null; var frames = 0;
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--scene" && ++index < args.Length) scenePath = args[index];
            else if (args[index] == "--capture" && ++index < args.Length) capture = args[index];
            else if (args[index] == "--frames" && ++index < args.Length && int.TryParse(args[index], out var parsed)) frames = parsed;
            else return Usage();
        }
        if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath)) return Usage();
        using var scene = JsonDocument.Parse(File.ReadAllText(scenePath));
        var candidate = scene.RootElement.TryGetProperty("candidateId", out var candidateId) ? candidateId.GetString() ?? "candidate unresolved" : "candidate unresolved";
        var surface = LoadSurface(scene.RootElement, out var bundleDocument, out var bundleRoot, out var bundleError);
        Console.Error.WriteLine($"asset-preview surface: kind={surface.Kind.ToString().ToUpperInvariant()}; controls={string.Join(",", surface.Controls)}; candidate={surface.Context.CandidateId}; question={surface.Context.SubjectiveQuestion}");
        var state = new PreviewSurfaceState(surface);
        Texture2D atlas = default, baseTexture = default, processedTexture = default; var atlasLoaded = false; var baseLoaded = false; var processedLoaded = false;
        var animationFrames = new List<PreviewFrame>();
        Raylib_cs.Sound rawSound = default, processedSound = default; var audioDevice = false; var rawLoaded = false; var processedSoundLoaded = false; var rawFrames = 0; var processedFrames = 0;
        var audioDiagnostic = "audio playback is manual; device not attempted"; var captured = false;
        try
        {
            global::Raylib_cs.Raylib.InitWindow(1120, 720, "Agentic2D Asset Preview — " + surface.Context.Modality);
            if (!global::Raylib_cs.Raylib.IsWindowReady()) { Console.Error.WriteLine("asset-preview could not initialize the Raylib window"); return 1; }
            global::Raylib_cs.Raylib.SetTargetFPS(60);
            atlas = global::Raylib_cs.Raylib.LoadTexture(Path.Combine(FindRepositoryRoot(), "game", "assets", "raw", "samples", "render-atlas-smoke.png")); atlasLoaded = atlas.Id != 0;
            if (bundleDocument is not null && bundleRoot is not null)
            {
                var bundle = bundleDocument.RootElement;
                if (surface.Kind == PreviewSurfaceKind.Image)
                {
                    baseTexture = LoadTexture(bundleRoot, bundle, "baseMediaPath", out baseLoaded); processedTexture = LoadTexture(bundleRoot, bundle, "processedMediaPath", out processedLoaded);
                }
                else if (surface.Kind == PreviewSurfaceKind.Animation && bundle.TryGetProperty("frameMedia", out var frameMedia) && frameMedia.ValueKind == JsonValueKind.Array)
                {
                    foreach (var frame in frameMedia.EnumerateArray())
                    {
                        var path = Path.GetFullPath(Path.Combine(bundleRoot, frame.GetProperty("processedPath").GetString()!));
                        if (!File.Exists(path)) continue;
                        var texture = global::Raylib_cs.Raylib.LoadTexture(path); if (texture.Id != 0) animationFrames.Add(new(frame.GetProperty("sequenceIndex").GetInt32(), frame.GetProperty("sourceFrameIndex").GetInt32(), texture));
                    }
                }
                if (surface.Kind == PreviewSurfaceKind.Audio)
                {
                    var rawPath = Path.GetFullPath(Path.Combine(bundleRoot, bundle.GetProperty("baseMediaPath").GetString()!));
                    var processedPath = Path.GetFullPath(Path.Combine(bundleRoot, bundle.GetProperty("processedMediaPath").GetString()!));
                    audioDiagnostic = LoadAudio(rawPath, processedPath, ref rawSound, ref processedSound, ref rawLoaded, ref processedSoundLoaded, ref rawFrames, ref processedFrames, out audioDevice);
                    state.SetAudioDiagnostic(audioDiagnostic);
                    Console.Error.WriteLine("asset-preview audio diagnostics: " + audioDiagnostic);
                }
            }
            for (var frame = 0; !global::Raylib_cs.Raylib.WindowShouldClose() && (frames <= 0 || frame < frames); frame++)
            {
                var mouse = global::Raylib_cs.Raylib.GetMousePosition();
                if (global::Raylib_cs.Raylib.IsMouseButtonPressed(MouseButton.Left)) HandleClick(surface, state, animationFrames.Count, mouse, ref rawSound, ref processedSound, rawLoaded, processedSoundLoaded);
                state.Advance(global::Raylib_cs.Raylib.GetFrameTime(), animationFrames.Count);
                global::Raylib_cs.Raylib.BeginDrawing(); global::Raylib_cs.Raylib.ClearBackground(new Color(14, 22, 38, 255));
                DrawHeader(surface, bundleError); DrawSurface(surface, state, animationFrames, baseTexture, processedTexture, baseLoaded, processedLoaded, atlas, atlasLoaded, audioDiagnostic, rawFrames, processedFrames);
                global::Raylib_cs.Raylib.EndDrawing();
                if (capture is not null && !captured) { var absolute = Path.GetFullPath(capture); Directory.CreateDirectory(Path.GetDirectoryName(absolute)!); global::Raylib_cs.Raylib.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), absolute)); captured = true; break; }
            }
            return 0;
        }
        finally
        {
            if (rawLoaded) { global::Raylib_cs.Raylib.StopSound(rawSound); global::Raylib_cs.Raylib.UnloadSound(rawSound); }
            if (processedSoundLoaded) { global::Raylib_cs.Raylib.StopSound(processedSound); global::Raylib_cs.Raylib.UnloadSound(processedSound); }
            if (audioDevice && global::Raylib_cs.Raylib.IsAudioDeviceReady()) global::Raylib_cs.Raylib.CloseAudioDevice();
            if (baseLoaded) global::Raylib_cs.Raylib.UnloadTexture(baseTexture); if (processedLoaded) global::Raylib_cs.Raylib.UnloadTexture(processedTexture); foreach (var frame in animationFrames) global::Raylib_cs.Raylib.UnloadTexture(frame.Texture); if (atlasLoaded) global::Raylib_cs.Raylib.UnloadTexture(atlas); if (global::Raylib_cs.Raylib.IsWindowReady()) global::Raylib_cs.Raylib.CloseWindow();
        }
    }

    private static PreviewSurface LoadSurface(JsonElement scene, out JsonDocument? bundleDocument, out string? bundleRoot, out string error)
    {
        bundleDocument = null; bundleRoot = null; error = string.Empty;
        if (!scene.TryGetProperty("bundlePath", out var path) || path.ValueKind != JsonValueKind.String) { error = "preview scene has no structured bundlePath"; return Unsupported(error); }
        var full = Path.GetFullPath(path.GetString()!); if (!File.Exists(full)) { error = "structured preview bundle is unavailable"; return Unsupported(error); }
        bundleDocument = JsonDocument.Parse(File.ReadAllText(full)); bundleRoot = Path.GetDirectoryName(full); return AssetPreviewSurfaceModel.FromBundle(bundleDocument.RootElement);
    }
    private static PreviewSurface Unsupported(string error) => new(PreviewSurfaceKind.Unsupported, [], new("unsupported", "candidate unresolved", "review subject", "none", [], "raw/base unavailable", "processed/current-draft unavailable", "No review surface can be selected."), error);
    private static Texture2D LoadTexture(string root, JsonElement bundle, string property, out bool loaded)
    {
        loaded = false; var path = Path.GetFullPath(Path.Combine(root, bundle.GetProperty(property).GetString()!)); if (!File.Exists(path)) return default;
        var texture = global::Raylib_cs.Raylib.LoadTexture(path); loaded = texture.Id != 0; return texture;
    }
    private static string LoadAudio(string rawPath, string processedPath, ref Raylib_cs.Sound raw, ref Raylib_cs.Sound processed, ref bool rawLoaded, ref bool processedLoaded, ref int rawFrames, ref int processedFrames, out bool device)
    {
        var rawExists = File.Exists(rawPath); var processedExists = File.Exists(processedPath); var init = false; device = false;
        try
        {
            init = true; global::Raylib_cs.Raylib.InitAudioDevice(); device = global::Raylib_cs.Raylib.IsAudioDeviceReady();
            if (device && rawExists) { raw = global::Raylib_cs.Raylib.LoadSound(rawPath); rawFrames = (int)raw.FrameCount; rawLoaded = rawFrames > 0; }
            if (device && processedExists) { processed = global::Raylib_cs.Raylib.LoadSound(processedPath); processedFrames = (int)processed.FrameCount; processedLoaded = processedFrames > 0; }
        }
        catch (Exception exception) { return $"InitAudioDevice attempted={init}; IsAudioDeviceReady={device}; raw WAV exists={rawExists}; processed WAV exists={processedExists}; raw LoadSound frames={rawFrames}; processed LoadSound frames={processedFrames}; error={exception.Message}"; }
        return $"InitAudioDevice attempted={init}; IsAudioDeviceReady={device}; raw WAV exists={rawExists}; processed WAV exists={processedExists}; raw LoadSound frames={rawFrames}; processed LoadSound frames={processedFrames}";
    }
    private static void HandleClick(PreviewSurface surface, PreviewSurfaceState state, int frameCount, Vector2 mouse, ref Raylib_cs.Sound raw, ref Raylib_cs.Sound processed, bool rawLoaded, bool processedLoaded)
    {
        for (var index = 0; index < surface.Controls.Count; index++)
        {
            var control = surface.Controls[index]; var rect = ControlRect(surface.Kind, index); if (!global::Raylib_cs.Raylib.CheckCollisionPointRec(mouse, rect)) continue;
            if (control == "Play Raw")
            {
                if (rawLoaded) { global::Raylib_cs.Raylib.PlaySound(raw); state.Apply(control, frameCount); }
                else state.SetAudioDiagnostic("raw/base playback unavailable; see audio-device diagnostic");
            }
            else if (control == "Play Processed")
            {
                if (processedLoaded) { global::Raylib_cs.Raylib.PlaySound(processed); state.Apply(control, frameCount); }
                else state.SetAudioDiagnostic("processed/current-draft playback unavailable; see audio-device diagnostic");
            }
            else if (control == "Stop")
            {
                if (rawLoaded) global::Raylib_cs.Raylib.StopSound(raw); if (processedLoaded) global::Raylib_cs.Raylib.StopSound(processed); state.Apply(control, frameCount);
            }
            else state.Apply(control, frameCount);
            break;
        }
    }
    private static void DrawHeader(PreviewSurface surface, string error)
    {
        var layout = ReviewSurfaceLayout.AssetPreview(surface); global::Raylib_cs.Raylib.DrawRectangle(28, layout.Context.Bounds.Y, layout.Context.Bounds.Width, layout.Context.Bounds.Height, new Color(27, 45, 68, 255)); global::Raylib_cs.Raylib.DrawRectangleLines(28, layout.Context.Bounds.Y, layout.Context.Bounds.Width, layout.Context.Bounds.Height, new Color(76, 112, 143, 255));
        for (var index = 0; index < layout.Context.Lines.Count; index++) global::Raylib_cs.Raylib.DrawText(layout.Context.Lines[index], layout.Context.Bounds.X, layout.Context.Bounds.Y + index * layout.Context.LineHeight, layout.Context.FontSize, index switch { 0 => Color.White, 3 => Color.SkyBlue, 4 => new Color(144, 238, 144, 255), 5 => Color.Gold, _ => Color.LightGray });
        if (!string.IsNullOrWhiteSpace(error)) global::Raylib_cs.Raylib.DrawText(error, 680, 175, 14, Color.Orange);
    }
    private static void DrawSurface(PreviewSurface surface, PreviewSurfaceState state, IReadOnlyList<PreviewFrame> frames, Texture2D baseTexture, Texture2D processedTexture, bool baseLoaded, bool processedLoaded, Texture2D atlas, bool atlasLoaded, string audioDiagnostic, int rawFrames, int processedFrames)
    {
        var layout = ReviewSurfaceLayout.AssetPreview(surface); global::Raylib_cs.Raylib.DrawRectangle(layout.Content.X, layout.Content.Y, layout.Content.Width, layout.Content.Height, new Color(27, 45, 68, 255)); global::Raylib_cs.Raylib.DrawRectangleLines(layout.Content.X, layout.Content.Y, layout.Content.Width, layout.Content.Height, new Color(76, 112, 143, 255));
        if (surface.Kind == PreviewSurfaceKind.Image) { DrawImage(state, baseTexture, processedTexture, baseLoaded, processedLoaded, atlas, atlasLoaded); global::Raylib_cs.Raylib.DrawText($"Showing {(state.ImageComparison == "source" ? "raw/base" : "processed/current-draft")} · {(state.IsolatedRegion ? "isolated region" : "source context")} · overlay {(state.Overlays ? "ON" : "OFF")} · filter {state.Filtering}", 680, 275, 15, Color.White); }
        else if (surface.Kind == PreviewSurfaceKind.Animation) DrawAnimation(state, frames);
        else if (surface.Kind == PreviewSurfaceKind.Audio) { global::Raylib_cs.Raylib.DrawText("Manual playback comparison", 80, 275, 25, Color.White); global::Raylib_cs.Raylib.DrawText($"Raw/base duration: {rawFrames} frames", 80, 335, 20, Color.SkyBlue); global::Raylib_cs.Raylib.DrawText($"Processed/current-draft duration: {processedFrames} frames", 80, 375, 20, new Color(144, 238, 144, 255)); global::Raylib_cs.Raylib.DrawText("No audio auto-play", 80, 425, 18, Color.Gold); }
        else global::Raylib_cs.Raylib.DrawText("No preview surface selected", 100, 390, 24, Color.Orange);
        for (var index = 0; index < surface.Controls.Count; index++) Button(ControlRect(surface.Kind, index), surface.Controls[index], state.IsActive(surface.Controls[index]));
        if (surface.Kind == PreviewSurfaceKind.Animation) global::Raylib_cs.Raylib.DrawText($"Frame {state.FrameIndex + 1}/{frames.Count} · source {(frames.Count == 0 ? "-" : frames[state.FrameIndex].SourceFrameIndex)} · {(state.Playing ? "playing" : "paused")}", 680, 620, 17, Color.White);
        if (surface.Kind == PreviewSurfaceKind.Audio) { var lines = ReviewSurfaceLayout.Wrap(audioDiagnostic, 400, 12); for (var index = 0; index < lines.Count; index++) global::Raylib_cs.Raylib.DrawText(lines[index], 680, 620 + index * 16, 12, Color.White); }
    }
    private static void DrawImage(PreviewSurfaceState state, Texture2D baseTexture, Texture2D processedTexture, bool baseLoaded, bool processedLoaded, Texture2D atlas, bool atlasLoaded)
    {
        var texture = state.ImageComparison == "source" ? baseTexture : processedTexture; var loaded = state.ImageComparison == "source" ? baseLoaded : processedLoaded;
        if (loaded) { global::Raylib_cs.Raylib.SetTextureFilter(texture, state.Filtering == "smooth" ? TextureFilter.Bilinear : TextureFilter.Point); global::Raylib_cs.Raylib.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, -texture.Height), new Rectangle(110, 250, 460, 300), Vector2.Zero, 0, Color.White); }
        else if (atlasLoaded) global::Raylib_cs.Raylib.DrawTexturePro(atlas, new Rectangle(0, 0, 8, 8), new Rectangle(110, 250, 460, 300), Vector2.Zero, 0, Color.White); else global::Raylib_cs.Raylib.DrawText("Image candidate unavailable", 150, 390, 22, Color.Orange);
        if (state.Overlays) { global::Raylib_cs.Raylib.DrawRectangleLines(28, 205, 620, 405, Color.Magenta); global::Raylib_cs.Raylib.DrawText(state.IsolatedRegion ? "isolated candidate region" : "source context", 44, 575, 16, Color.Magenta); }
    }
    private static void DrawAnimation(PreviewSurfaceState state, IReadOnlyList<PreviewFrame> frames)
    { if (frames.Count == 0) { global::Raylib_cs.Raylib.DrawText("Animation frame sequence unavailable", 100, 390, 22, Color.Orange); return; } var texture = frames[state.FrameIndex].Texture; global::Raylib_cs.Raylib.SetTextureFilter(texture, TextureFilter.Point); global::Raylib_cs.Raylib.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, -texture.Height), new Rectangle(110, 250, 460, 300), Vector2.Zero, 0, Color.White); }
    private static Rectangle ControlRect(PreviewSurfaceKind kind, int index) { var surface = new PreviewSurface(kind, [], new("", "", "", "", [], "", "", ""), string.Empty); var layout = ReviewSurfaceLayout.AssetPreview(surface); return new(layout.Controls.X + (index % 2) * 200, layout.Controls.Y + (index / 2) * 52, 190, 38); }
    private static void Button(Rectangle rect, string text, bool active) { var color = active ? new Color(60, 152, 93, 255) : new Color(72, 104, 158, 255); global::Raylib_cs.Raylib.DrawRectangleRec(rect, color); global::Raylib_cs.Raylib.DrawRectangleLinesEx(rect, 2, Color.RayWhite); global::Raylib_cs.Raylib.DrawText(text, (int)rect.X + 12, (int)rect.Y + 10, 15, Color.RayWhite); }
    private static string FindRepositoryRoot() { var directory = AppContext.BaseDirectory; while (!string.IsNullOrWhiteSpace(directory)) { if (File.Exists(Path.Combine(directory, "dotnet-ai-first-2d-game-engine.slnx"))) return directory; directory = Directory.GetParent(directory)?.FullName; } return Directory.GetCurrentDirectory(); }
    private static int Usage() { Console.Error.WriteLine("asset-preview requires --scene <preview-scene.json> [--capture <png>] [--frames <count>]"); return 2; }
    private sealed record PreviewFrame(int SequenceIndex, int SourceFrameIndex, Texture2D Texture);
}
