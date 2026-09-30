using System.Numerics;
using System.Text.Json;
using Agentic2D.Rendering;
using Raylib_cs;

namespace Agentic2D.DebugClient;

/// <summary>Native, read-only presentation client for one M029 preview session.</summary>
public static class AssetPreviewRaylibWindow
{
    public static int Run(string[] args)
    {
        string? scenePath = null, capture = null; var frames = 0;
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--scene" && ++index < args.Length) scenePath = args[index];
            else if (args[index] == "--capture" && ++index < args.Length) capture = args[index];
            else if (args[index] == "--frames") { if (++index >= args.Length || !int.TryParse(args[index], out frames)) return Usage(); }
            else return Usage();
        }
        if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath)) return Usage();
        using var scene = JsonDocument.Parse(File.ReadAllText(scenePath));
        var candidate = scene.RootElement.TryGetProperty("candidateId", out var candidateId) ? candidateId.GetString() ?? "candidate.unresolved" : "candidate.unresolved";
        var projection = new RenderProjectionService().ProjectScenario("game/scenarios/smoke/runtime-smoke.json", sourceMode: "workbench-preview-ui");
        Texture2D atlas = default; Texture2D candidateTexture = default; Raylib_cs.Sound rawSound = default; Raylib_cs.Sound processedSound = default; var rawAudioPath = Path.Combine(FindRepositoryRoot(), "game", "assets", "raw", "samples", "footstep-a.wav"); var processedAudioPath = rawAudioPath; var atlasLoaded = false; var candidateLoaded = false; var audioDevice = false; var audioInitAttempted = false; var rawExists = false; var processedExists = false; var rawFrames = 0; var processedFrames = 0; var soundLoaded = false; var processedSoundLoaded = false; var captured = false; var mediaKind = "image"; var animationFrames = new List<PreviewFrame>();
        var highContrast = false; var overlays = true; var comparison = "side-by-side"; var filtering = "nearest"; var animationPlaying = false; var animationIndex = 0; var animationAccumulator = 0d; var speed = 1d; var audio = "audio not attempted"; var audioDiagnostic = "";
        try
        {
            global::Raylib_cs.Raylib.InitWindow(1120, 680, "Agentic2D Asset Preview");
            if (!global::Raylib_cs.Raylib.IsWindowReady()) { Console.Error.WriteLine("asset-preview could not initialize the Raylib window"); return 1; }
            global::Raylib_cs.Raylib.SetTargetFPS(60);
            atlas = global::Raylib_cs.Raylib.LoadTexture(Path.Combine(FindRepositoryRoot(), "game", "assets", "raw", "samples", "render-atlas-smoke.png"));
            atlasLoaded = atlas.Id != 0;
            if (scene.RootElement.TryGetProperty("bundlePath", out var bundlePathElement) && bundlePathElement.ValueKind == JsonValueKind.String)
            {
                var bundlePath = Path.GetFullPath(bundlePathElement.GetString()!);
                if (File.Exists(bundlePath))
                {
                    using var bundle = JsonDocument.Parse(File.ReadAllText(bundlePath));
                    mediaKind = bundle.RootElement.GetProperty("mediaKind").GetString() ?? "image";
                    var processed = bundle.RootElement.GetProperty("processedMediaPath").GetString();
                    var processedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bundlePath)!, processed!));
                    if (File.Exists(processedPath) && mediaKind != "audio" && mediaKind != "animation") { candidateTexture = global::Raylib_cs.Raylib.LoadTexture(processedPath); candidateLoaded = candidateTexture.Id != 0; }
                    if (mediaKind == "animation" && bundle.RootElement.TryGetProperty("frameMedia", out var frameMedia) && frameMedia.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var frame in frameMedia.EnumerateArray())
                        {
                            var framePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bundlePath)!, frame.GetProperty("processedPath").GetString()!));
                            if (File.Exists(framePath)) { var texture = global::Raylib_cs.Raylib.LoadTexture(framePath); if (texture.Id != 0) animationFrames.Add(new PreviewFrame(frame.GetProperty("sequenceIndex").GetInt32(), frame.GetProperty("sourceFrameIndex").GetInt32(), texture)); }
                        }
                    }
                    if (mediaKind == "audio") { rawAudioPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(bundlePath)!, bundle.RootElement.GetProperty("baseMediaPath").GetString()!)); processedAudioPath = processedPath; }
                }
            }
            rawExists = File.Exists(rawAudioPath); processedExists = File.Exists(processedAudioPath);
            try
            {
                audioInitAttempted = true; global::Raylib_cs.Raylib.InitAudioDevice(); audioDevice = global::Raylib_cs.Raylib.IsAudioDeviceReady();
                if (audioDevice) { if (rawExists) { rawSound = global::Raylib_cs.Raylib.LoadSound(rawAudioPath); rawFrames = (int)rawSound.FrameCount; soundLoaded = rawFrames > 0; } if (processedExists) { processedSound = global::Raylib_cs.Raylib.LoadSound(processedAudioPath); processedFrames = (int)processedSound.FrameCount; processedSoundLoaded = processedFrames > 0; } }
                audio = audioDevice ? "audio device ready" : "audio device not ready";
            }
            catch (Exception exception) { audio = "audio initialization error"; audioDiagnostic = exception.Message; }
            audioDiagnostic = $"InitAudioDevice attempted={audioInitAttempted}; IsAudioDeviceReady={audioDevice}; raw WAV exists={rawExists}; processed WAV exists={processedExists}; raw LoadSound frames={rawFrames}; processed LoadSound frames={processedFrames}" + (string.IsNullOrWhiteSpace(audioDiagnostic) ? "" : "; error=" + audioDiagnostic);
            Console.Error.WriteLine("asset-preview audio diagnostics: " + audioDiagnostic);
            for (var frame = 0; !global::Raylib_cs.Raylib.WindowShouldClose() && (frames <= 0 || frame < frames); frame++)
            {
                var mouse = global::Raylib_cs.Raylib.GetMousePosition();
                var source = new Rectangle(680, 92, 190, 38); var isolate = new Rectangle(884, 92, 190, 38); var nearest = new Rectangle(680, 142, 190, 38); var smooth = new Rectangle(884, 142, 190, 38);
                var neutral = new Rectangle(680, 192, 190, 38); var contrast = new Rectangle(884, 192, 190, 38); var overlay = new Rectangle(680, 242, 260, 38); var reset = new Rectangle(948, 242, 126, 38);
                var play = new Rectangle(680, 302, 122, 38); var pause = new Rectangle(814, 302, 122, 38); var step = new Rectangle(948, 302, 126, 38);
                var slow = new Rectangle(680, 352, 122, 38); var normal = new Rectangle(814, 352, 122, 38); var fast = new Rectangle(948, 352, 126, 38);
                var raw = new Rectangle(680, 412, 122, 38); var processed = new Rectangle(814, 412, 122, 38); var stop = new Rectangle(948, 412, 126, 38);
                if (Clicked(source, mouse)) comparison = "source"; if (Clicked(isolate, mouse)) comparison = "isolated-region"; if (Clicked(nearest, mouse)) filtering = "nearest"; if (Clicked(smooth, mouse)) filtering = "smooth";
                if (Clicked(neutral, mouse)) highContrast = false; if (Clicked(contrast, mouse)) highContrast = true; if (Clicked(overlay, mouse)) overlays = !overlays; if (Clicked(reset, mouse)) { animationIndex = 0; animationAccumulator = 0; animationPlaying = false; }
                if (Clicked(play, mouse)) animationPlaying = true; if (Clicked(pause, mouse)) animationPlaying = false; if (Clicked(step, mouse) && !animationPlaying && animationFrames.Count > 0) animationIndex = (animationIndex + 1) % animationFrames.Count;
                if (Clicked(slow, mouse)) speed = .5; if (Clicked(normal, mouse)) speed = 1; if (Clicked(fast, mouse)) speed = 2;
                if (animationPlaying && animationFrames.Count > 1)
                {
                    animationAccumulator += global::Raylib_cs.Raylib.GetFrameTime() * speed;
                    while (animationAccumulator >= .25) { animationIndex = (animationIndex + 1) % animationFrames.Count; animationAccumulator -= .25; }
                }
                if (Clicked(raw, mouse))
                {
                    if (soundLoaded) { global::Raylib_cs.Raylib.SetSoundVolume(rawSound, 1); global::Raylib_cs.Raylib.PlaySound(rawSound); audio = "raw playing (explicit request)"; }
                    else audio = audioDevice ? $"raw unavailable (LoadSound frames={rawFrames})" : "raw unavailable (audio device not ready)";
                }
                if (Clicked(processed, mouse))
                {
                    if (processedSoundLoaded) { global::Raylib_cs.Raylib.SetSoundVolume(processedSound, 1); global::Raylib_cs.Raylib.PlaySound(processedSound); audio = "processed playing (explicit request)"; }
                    else audio = audioDevice ? $"processed unavailable (LoadSound frames={processedFrames})" : "processed unavailable (audio device not ready)";
                }
                if (Clicked(stop, mouse)) { if (soundLoaded) global::Raylib_cs.Raylib.StopSound(rawSound); if (processedSoundLoaded) global::Raylib_cs.Raylib.StopSound(processedSound); audio = audioDevice ? "stopped" : "stopped (audio device not ready)"; }

                global::Raylib_cs.Raylib.BeginDrawing(); global::Raylib_cs.Raylib.ClearBackground(highContrast ? Color.White : new Color(18, 27, 42, 255));
                var text = highContrast ? Color.Black : Color.RayWhite; var panel = highContrast ? new Color(224, 230, 237, 255) : new Color(37, 54, 79, 255);
                global::Raylib_cs.Raylib.DrawText("ASSET PREVIEW", 28, 24, 28, text); global::Raylib_cs.Raylib.DrawText(candidate, 28, 58, 17, highContrast ? Color.DarkGray : Color.LightGray);
                global::Raylib_cs.Raylib.DrawRectangle(28, 92, 620, 510, panel); var previewTexture = mediaKind == "animation" && animationFrames.Count > 0 ? animationFrames[animationIndex].Texture : candidateTexture; if (mediaKind == "animation" && animationFrames.Count > 0) { global::Raylib_cs.Raylib.SetTextureFilter(previewTexture, filtering == "smooth" ? TextureFilter.Bilinear : TextureFilter.Point); global::Raylib_cs.Raylib.DrawTexturePro(previewTexture, new Rectangle(0, 0, previewTexture.Width, -previewTexture.Height), new Rectangle(110, 180, 460, 330), Vector2.Zero, 0, Color.White); } else if (candidateLoaded) { global::Raylib_cs.Raylib.SetTextureFilter(candidateTexture, filtering == "smooth" ? TextureFilter.Bilinear : TextureFilter.Point); global::Raylib_cs.Raylib.DrawTexturePro(candidateTexture, new Rectangle(0, 0, candidateTexture.Width, -candidateTexture.Height), new Rectangle(110, 180, 460, 330), Vector2.Zero, 0, Color.White); } else if (atlasLoaded) DrawProjection(projection.Frame.Items, atlas, comparison, filtering); else { global::Raylib_cs.Raylib.DrawRectangle(110, 180, 460, 330, new Color(62, 103, 123, 255)); global::Raylib_cs.Raylib.DrawText("Candidate bundle loaded; texture adapter unavailable", 135, 330, 18, Color.White); }
                if (overlays) { global::Raylib_cs.Raylib.DrawRectangleLines(28, 92, 620, 510, Color.Magenta); global::Raylib_cs.Raylib.DrawLine(338, 92, 338, 602, Color.SkyBlue); global::Raylib_cs.Raylib.DrawLine(28, 347, 648, 347, Color.SkyBlue); global::Raylib_cs.Raylib.DrawText("pivot / bounds / grid / padding", 42, 574, 14, Color.Magenta); }
                global::Raylib_cs.Raylib.DrawText("Comparison and overlays", 680, 62, 20, text); Button(source, "Source", comparison == "source"); Button(isolate, "Isolated", comparison == "isolated-region"); Button(nearest, "Nearest", filtering == "nearest"); Button(smooth, "Smooth", filtering == "smooth"); Button(neutral, "Neutral", !highContrast); Button(contrast, "High contrast", highContrast); Button(overlay, overlays ? "Overlays on" : "Overlays off", overlays); Button(reset, "Reset", !animationPlaying && animationIndex == 0);
                global::Raylib_cs.Raylib.DrawText("Animation", 680, 276, 18, text); Button(play, "Play", animationPlaying); Button(pause, "Pause", !animationPlaying); Button(step, "Step", false); Button(slow, "0.5x", speed == .5); Button(normal, "1x", speed == 1); Button(fast, "2x", speed == 2); global::Raylib_cs.Raylib.DrawText(mediaKind == "animation" && animationFrames.Count > 0 ? $"Frame {animationIndex + 1}/{animationFrames.Count} · source {animationFrames[animationIndex].SourceFrameIndex}" : "No animation frame sequence", 680, 500, 14, text);
                global::Raylib_cs.Raylib.DrawText("Audio — no auto-play", 680, 386, 18, text); Button(raw, "Raw", audio.StartsWith("raw", StringComparison.Ordinal)); Button(processed, "Processed", audio.StartsWith("processed", StringComparison.Ordinal)); Button(stop, "Stop", audio.StartsWith("stopped", StringComparison.Ordinal)); global::Raylib_cs.Raylib.DrawText(audio, 680, 466, 14, text); global::Raylib_cs.Raylib.DrawText(audioDiagnostic.Length > 110 ? audioDiagnostic[..110] : audioDiagnostic, 680, 520, 11, text);
                global::Raylib_cs.Raylib.DrawText("Temporary preview only; decisions remain in the workbench session.", 28, 632, 15, text); global::Raylib_cs.Raylib.EndDrawing();
                if (capture is not null && !captured) { var absolute = Path.GetFullPath(capture); Directory.CreateDirectory(Path.GetDirectoryName(absolute)!); global::Raylib_cs.Raylib.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), absolute)); captured = true; break; }
            }
            return 0;
        }
        finally
        {
            if (soundLoaded) { global::Raylib_cs.Raylib.StopSound(rawSound); global::Raylib_cs.Raylib.UnloadSound(rawSound); }
            if (processedSoundLoaded) { global::Raylib_cs.Raylib.StopSound(processedSound); global::Raylib_cs.Raylib.UnloadSound(processedSound); }
            if (audioDevice && global::Raylib_cs.Raylib.IsAudioDeviceReady()) global::Raylib_cs.Raylib.CloseAudioDevice();
            if (candidateLoaded) global::Raylib_cs.Raylib.UnloadTexture(candidateTexture); foreach (var frame in animationFrames) global::Raylib_cs.Raylib.UnloadTexture(frame.Texture); if (atlasLoaded) global::Raylib_cs.Raylib.UnloadTexture(atlas); if (global::Raylib_cs.Raylib.IsWindowReady()) global::Raylib_cs.Raylib.CloseWindow();
        }
    }

    private static void DrawProjection(IReadOnlyList<RenderItem> items, Texture2D atlas, string comparison, string filtering)
    {
        global::Raylib_cs.Raylib.SetTextureFilter(atlas, filtering == "smooth" ? TextureFilter.Bilinear : TextureFilter.Point);
        var drawn = comparison == "isolated-region" ? items.Where(item => item.SourceKind == "runtime-entity").ToArray() : items.ToArray();
        foreach (var item in drawn)
        {
            var source = item.RegionId switch { "region.ground" => new Rectangle(0, 0, 8, 8), "region.player" => new Rectangle(8, 0, 8, 8), "region.npc" => new Rectangle(16, 0, 8, 8), "region.blocked" => new Rectangle(0, 8, 8, 8), "region.tree-base" => new Rectangle(8, 8, 8, 8), _ => new Rectangle(16, 8, 8, 8) };
            var destination = item.Destination; var x = 58 + (float)(destination.Position.X * 56); var y = 122 + (float)(destination.Position.Y * 56); var width = Math.Max(8, (float)(destination.Size.Width * 56)); var height = Math.Max(8, (float)(destination.Size.Height * 56));
            global::Raylib_cs.Raylib.DrawTexturePro(atlas, source, new Rectangle(x, y, width, height), item.Anchor == "bottom-center" ? new Vector2(width / 2, height) : Vector2.Zero, 0, new Color(item.Tint.R, item.Tint.G, item.Tint.B, item.Tint.A));
        }
    }

    private static bool Clicked(Rectangle rectangle, Vector2 mouse) => global::Raylib_cs.Raylib.IsMouseButtonPressed(MouseButton.Left) && global::Raylib_cs.Raylib.CheckCollisionPointRec(mouse, rectangle);
    private static void Button(Rectangle rectangle, string text, bool active) { var color = active ? new Color(60, 152, 93, 255) : new Color(72, 104, 158, 255); global::Raylib_cs.Raylib.DrawRectangleRec(rectangle, color); global::Raylib_cs.Raylib.DrawRectangleLinesEx(rectangle, 2, Color.RayWhite); global::Raylib_cs.Raylib.DrawText(text, (int)rectangle.X + 12, (int)rectangle.Y + 11, 16, Color.RayWhite); }
    private static string FindRepositoryRoot()
    {
        var directory = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(directory)) { if (File.Exists(Path.Combine(directory, "dotnet-ai-first-2d-game-engine.slnx"))) return directory; directory = Directory.GetParent(directory)?.FullName; }
        return Directory.GetCurrentDirectory();
    }
    private static int Usage() { Console.Error.WriteLine("asset-preview requires --scene <preview-scene.json> [--capture <png>] [--frames <count>]"); return 2; }
    private sealed record PreviewFrame(int SequenceIndex, int SourceFrameIndex, Texture2D Texture);
}
