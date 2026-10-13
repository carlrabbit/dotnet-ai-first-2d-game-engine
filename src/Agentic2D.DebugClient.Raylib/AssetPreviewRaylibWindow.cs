using System.Numerics;
using System.Text.Json;
using Agentic2D.Rendering;
using Agentic2D.Tools;
using Agentic2D.UI;
using Raylib_cs;
using Agentic2D.DebugClient.Raylib;
using RaylibApi = Raylib_cs.Raylib;

namespace Agentic2D.DebugClient;

/// <summary>One generic native preview process; UI geometry comes only from the M052 projection.</summary>
public static class AssetPreviewRaylibWindow
{
    public static unsafe int Run(string[] args)
    {
        string? scenePath = null, capture = null, inputProbeReportPath = null; var frames = 0;
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index] == "--scene" && ++index < args.Length) scenePath = args[index];
            else if (args[index] == "--capture" && ++index < args.Length) capture = args[index];
            else if (args[index] == "--probe-report" && ++index < args.Length) inputProbeReportPath = args[index];
            else if (args[index] == "--frames" && ++index < args.Length && int.TryParse(args[index], out var parsed)) frames = parsed;
            else return Usage();
        }
        if (string.IsNullOrWhiteSpace(scenePath) || !File.Exists(scenePath)) return Usage();
        using var scene = JsonDocument.Parse(File.ReadAllText(scenePath));
        var surface = LoadSurface(scene.RootElement, out var bundleDocument, out var bundle, out var bundleRoot, out var bundleError);
        var candidate = surface.Context.CandidateId;
        Console.Error.WriteLine($"asset-preview surface: kind={surface.Kind}; candidate={candidate}; diagnostic={surface.Diagnostic}");
        var state = new PreviewSurfaceState(surface);
        Texture2D baseTexture = default, processedTexture = default; var baseLoaded = false; var processedLoaded = false;
        var animationFrames = new List<PreviewFrame>();
        Raylib_cs.Sound rawSound = default, processedSound = default; var audioDevice = false; var rawLoaded = false; var processedLoadedSound = false; var rawFrames = 0; var processedFrames = 0;
        var audioDiagnostic = "Playback is manual; audio device has not been attempted.";
        var mediaDiagnostic = bundleError;
        try
        {
            RaylibGameWindow.InitDesktopWindow("Agentic2D — Asset Preview · " + surface.Context.Modality);
            if (!RaylibApi.IsWindowReady()) { Console.Error.WriteLine("asset-preview could not initialize the Raylib window"); return 1; }
            RaylibApi.SetTargetFPS(60);
            if (bundle is not null && bundleRoot is not null)
            {
                if (surface.Kind == PreviewSurfaceKind.Image)
                {
                    baseTexture = LoadTexture(bundleRoot, bundle.Value, "baseMediaPath", out baseLoaded);
                    processedTexture = LoadTexture(bundleRoot, bundle.Value, "processedMediaPath", out processedLoaded);
                    if (!baseLoaded || !processedLoaded) mediaDiagnostic = "One or more image candidate media files could not be loaded.";
                }
                else if (surface.Kind == PreviewSurfaceKind.Animation && bundle.Value.TryGetProperty("frameMedia", out var frameMedia) && frameMedia.ValueKind == JsonValueKind.Array)
                {
                    foreach (var frame in frameMedia.EnumerateArray().OrderBy(item => item.GetProperty("sequenceIndex").GetInt32()))
                    {
                        var path = SafeBundlePath(bundleRoot, frame.GetProperty("processedPath").GetString()!);
                        if (!File.Exists(path)) continue;
                        var texture = RaylibApi.LoadTexture(path); if (texture.Id != 0) animationFrames.Add(new(frame.GetProperty("sequenceIndex").GetInt32(), frame.GetProperty("sourceFrameIndex").GetInt32(), texture));
                    }
                    if (animationFrames.Count == 0) mediaDiagnostic = "No ordered animation frames are available.";
                }
                else if (surface.Kind == PreviewSurfaceKind.Audio)
                {
                    var rawPath = SafeBundlePath(bundleRoot, bundle.Value.GetProperty("baseMediaPath").GetString()!);
                    var processedPath = SafeBundlePath(bundleRoot, bundle.Value.GetProperty("processedMediaPath").GetString()!);
                    audioDiagnostic = LoadAudio(rawPath, processedPath, ref rawSound, ref processedSound, ref rawLoaded, ref processedLoadedSound, ref rawFrames, ref processedFrames, out audioDevice);
                    state.SetAudioDiagnostic(audioDiagnostic);
                    Console.Error.WriteLine("asset-preview audio diagnostics: " + audioDiagnostic);
                }
            }
            var definition = Compose();
            var viewport = RaylibGameWindow.CurrentViewport();
            var session = new UiSession(definition, viewport);
            var observedActions = new List<string>();
            void WriteProbe()
            {
                if (inputProbeReportPath is null) return;
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(inputProbeReportPath))!);
                File.WriteAllText(inputProbeReportPath, JsonSerializer.Serialize(new
                {
                    status = "ready",
                    processId = Environment.ProcessId,
                    windowHandle = (long)(nint)RaylibApi.GetWindowHandle(),
                    viewport = session.Layout.Viewport,
                    observedActions,
                    frameIndex = state.FrameIndex,
                    frameCount = animationFrames.Count,
                    playing = state.Playing,
                    imageComparison = state.ImageComparison,
                    mediaDiagnostic,
                    rawMediaLoaded = rawLoaded,
                    processedMediaLoaded = processedLoaded || processedLoadedSound,
                    audioState = state.AudioState,
                    audioDeviceReady = audioDevice && RaylibApi.IsAudioDeviceReady(),
                    audioDiagnostic,
                    controls = session.Layout.Elements.Where(element => element.ActionId?.StartsWith("asset-preview.control.", StringComparison.Ordinal) == true && element.Enabled)
                        .Select(element => new { element.Id, element.ActionId, element.PhysicalBounds, element.VisibleBounds }).ToArray()
                }, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
            }
            WriteProbe();
            RaylibApi.SetMouseCursor(MouseCursor.Default);
            for (var frame = 0; !RaylibApi.WindowShouldClose() && (frames <= 0 || frame < frames); frame++)
            {
                var currentViewport = RaylibGameWindow.CurrentViewport();
                if (currentViewport != viewport) { viewport = currentViewport; session.Recompose(definition, viewport); }
                if (!RaylibApi.IsWindowFocused() && session.CapturedId is not null) session.Dispatch(new UiInputEvent("pointer-cancel"));
                var delta = RaylibApi.GetFrameTime();
                session.Dispatch(new UiInputEvent("time", Delta: delta));
                state.Advance(delta, animationFrames.Count);
                RaylibWorkbenchInput.Dispatch(session);
                foreach (var action in session.DrainActions())
                {
                    if (!action.ActionId.StartsWith("asset-preview.control.", StringComparison.Ordinal)) continue;
                    var control = action.ActionId["asset-preview.control.".Length..];
                    if (!surface.Has(control)) continue;
                    observedActions.Add(action.ActionId);
                    if (control == "Play Raw")
                    {
                        if (PreviewAudioPlaybackPolicy.CanStartPlayback(audioDevice && RaylibApi.IsAudioDeviceReady(), rawLoaded)) { RaylibApi.PlaySound(rawSound); state.Apply(control, animationFrames.Count); }
                        else { audioDiagnostic = "Raw/base playback unavailable; " + audioDiagnostic; state.SetAudioDiagnostic(audioDiagnostic); }
                    }
                    else if (control == "Play Processed")
                    {
                        if (PreviewAudioPlaybackPolicy.CanStartPlayback(audioDevice && RaylibApi.IsAudioDeviceReady(), processedLoadedSound)) { RaylibApi.PlaySound(processedSound); state.Apply(control, animationFrames.Count); }
                        else { audioDiagnostic = "Processed/current-draft playback unavailable; " + audioDiagnostic; state.SetAudioDiagnostic(audioDiagnostic); }
                    }
                    else
                    {
                        if (control == "Stop") { if (rawLoaded) RaylibApi.StopSound(rawSound); if (processedLoadedSound) RaylibApi.StopSound(processedSound); }
                        state.Apply(control, animationFrames.Count);
                    }
                }
                definition = Compose(); session.Recompose(definition, currentViewport);
                RaylibApi.BeginDrawing();
                RaylibApi.ClearBackground(new Color(17, 24, 39, 255));
                var commands = UiDrawProjection.Create(session);
                RaylibAgentUiRenderer.Instance.Render(commands, definition.Theme, viewport, session.PresentationTime, definition.ReducedMotion);
                DrawMediaSlot(session, surface, state, animationFrames, baseTexture, processedTexture, baseLoaded, processedLoaded, mediaDiagnostic);
                RaylibApi.EndDrawing();
                WriteProbe();
                if (capture is not null && frame == 1)
                {
                    var absolute = Path.GetFullPath(capture); Directory.CreateDirectory(Path.GetDirectoryName(absolute)!);
                    RaylibApi.TakeScreenshot(Path.GetRelativePath(Directory.GetCurrentDirectory(), absolute));
                }
            }
            return 0;

            UiDefinition Compose()
            {
                var reference = surface.Kind switch
                {
                    PreviewSurfaceKind.Image => state.ImageComparison == "source" ? "media.raw-image" : "media.processed-image",
                    PreviewSurfaceKind.Animation => "media.animation-frame." + state.FrameIndex,
                    PreviewSurfaceKind.Audio => "media.audio-manual-playback",
                    _ => "media.unsupported"
                };
                var status = surface.Kind == PreviewSurfaceKind.Image
                    ? $"Showing {(state.ImageComparison == "source" ? "raw/base" : "processed/current draft")} · {(state.IsolatedRegion ? "isolated candidate region" : "source context")} · overlays {(state.Overlays ? "on" : "off")} · {state.Filtering} filter"
                    : surface.Kind == PreviewSurfaceKind.Animation ? (animationFrames.Count == 0 ? mediaDiagnostic : "Ordered candidate frames loaded.")
                    : surface.Kind == PreviewSurfaceKind.Audio ? "Manual playback comparison; no autoplay."
                    : surface.Diagnostic;
                var playbackStatus = state.Playing ? "Playing" : surface.Kind == PreviewSurfaceKind.Audio
                    ? state.AudioState.StartsWith("raw/base playing", StringComparison.Ordinal) ? "Raw/base playing"
                    : state.AudioState.StartsWith("processed/current-draft playing", StringComparison.Ordinal) ? "Processed/current draft playing"
                    : state.AudioState == "stopped" ? "Stopped" : "Not playing; playback is manual"
                    : state.AudioState;
                return M053WorkbenchScreens.AssetPreview(new(surface, reference, status,
                    playbackStatus, state.FrameIndex, animationFrames.Count, state.Playing, audioDiagnostic));
            }
        }
        finally
        {
            if (rawLoaded) { RaylibApi.StopSound(rawSound); RaylibApi.UnloadSound(rawSound); }
            if (processedLoadedSound) { RaylibApi.StopSound(processedSound); RaylibApi.UnloadSound(processedSound); }
            if (audioDevice && RaylibApi.IsAudioDeviceReady()) RaylibApi.CloseAudioDevice();
            if (baseLoaded) RaylibApi.UnloadTexture(baseTexture);
            if (processedLoaded) RaylibApi.UnloadTexture(processedTexture);
            foreach (var frame in animationFrames) RaylibApi.UnloadTexture(frame.Texture);
            if (RaylibApi.IsWindowReady()) RaylibApi.CloseWindow();
        }
    }

    private static void DrawMediaSlot(UiSession session, PreviewSurface surface, PreviewSurfaceState state,
        IReadOnlyList<PreviewFrame> frames, Texture2D baseTexture, Texture2D processedTexture,
        bool baseLoaded, bool processedLoaded, string diagnostic)
    {
        var item = session.Layout.Elements.FirstOrDefault(element => element.Id == "asset-preview.media");
        if (item is null || item.VisibleBounds.Width <= 0 || item.VisibleBounds.Height <= 0) return;
        var bounds = item.VisibleBounds;
        RaylibApi.BeginScissorMode((int)bounds.X, (int)bounds.Y, Math.Max(0, (int)bounds.Width), Math.Max(0, (int)bounds.Height));
        if (surface.Kind == PreviewSurfaceKind.Image)
        {
            var texture = state.ImageComparison == "source" ? baseTexture : processedTexture;
            var loaded = state.ImageComparison == "source" ? baseLoaded : processedLoaded;
            if (loaded)
            {
                RaylibApi.SetTextureFilter(texture, state.Filtering == "smooth" ? TextureFilter.Bilinear : TextureFilter.Point);
                DrawFitted(texture, bounds, state.IsolatedRegion);
                if (state.Overlays) RaylibApi.DrawRectangleLinesEx(new Rectangle((float)bounds.X, (float)bounds.Y, (float)bounds.Width, (float)bounds.Height), 2, Color.Magenta);
            }
            else RaylibApi.DrawText(diagnostic.Length == 0 ? "Candidate image unavailable" : diagnostic, (int)bounds.X + 12, (int)bounds.Y + 12, 16, Color.Orange);
        }
        else if (surface.Kind == PreviewSurfaceKind.Animation)
        {
            if (frames.Count > 0) DrawFitted(frames[state.FrameIndex].Texture, bounds, true);
            else RaylibApi.DrawText(diagnostic.Length == 0 ? "Animation frames unavailable" : diagnostic, (int)bounds.X + 12, (int)bounds.Y + 12, 16, Color.Orange);
        }
        else if (surface.Kind == PreviewSurfaceKind.Audio)
        {
            for (var index = 0; index < 48; index++)
            {
                var x = (int)(bounds.X + 18 + index * Math.Max(1, (bounds.Width - 36) / 48));
                var height = 8 + (int)(Math.Abs(Math.Sin(index * .67)) * Math.Min(52, bounds.Height * .18));
                RaylibApi.DrawRectangle(x, (int)(bounds.Y + (bounds.Height - height) / 2), 3, height, new Color(96, 165, 250, 210));
            }
        }
        RaylibApi.EndScissorMode();
    }

    private static void DrawFitted(Texture2D texture, UiBounds bounds, bool crop)
    {
        if (texture.Width <= 0 || texture.Height <= 0) return;
        var scale = crop ? Math.Max(bounds.Width / texture.Width, bounds.Height / texture.Height) : Math.Min(bounds.Width / texture.Width, bounds.Height / texture.Height);
        var width = texture.Width * scale; var height = texture.Height * scale;
        var target = new Rectangle((float)(bounds.X + (bounds.Width - width) / 2), (float)(bounds.Y + (bounds.Height - height) / 2), (float)width, (float)height);
        RaylibApi.DrawTexturePro(texture, new Rectangle(0, 0, texture.Width, -texture.Height), target, Vector2.Zero, 0, Color.White);
    }

    private static PreviewSurface LoadSurface(JsonElement scene, out JsonDocument? bundleDocument, out JsonElement? bundle, out string? bundleRoot, out string error)
    {
        bundleDocument = null; bundle = null; bundleRoot = null; error = string.Empty;
        if (!scene.TryGetProperty("bundlePath", out var path) || path.ValueKind != JsonValueKind.String) { error = "Preview scene has no structured bundlePath."; return Unsupported(error); }
        var full = Path.GetFullPath(path.GetString()!); if (!File.Exists(full)) { error = "Structured preview bundle is unavailable."; return Unsupported(error); }
        bundleDocument = JsonDocument.Parse(File.ReadAllText(full)); bundle = bundleDocument.RootElement; bundleRoot = Path.GetDirectoryName(full);
        return AssetPreviewSurfaceModel.FromBundle(bundle.Value);
    }

    private static PreviewSurface Unsupported(string error) => new(PreviewSurfaceKind.Unsupported, [], new("unsupported", "candidate unresolved", "review subject", "none", [], "raw/base unavailable", "processed/current draft unavailable", "No review surface can be selected."), error);
    private static Texture2D LoadTexture(string root, JsonElement bundle, string property, out bool loaded)
    {
        loaded = false; var path = SafeBundlePath(root, bundle.GetProperty(property).GetString()!); if (!File.Exists(path)) return default;
        var texture = RaylibApi.LoadTexture(path); loaded = texture.Id != 0; return texture;
    }
    private static string SafeBundlePath(string root, string relative)
    {
        var full = Path.GetFullPath(Path.Combine(root, relative));
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Preview media path escapes its exact bundle root.");
        return full;
    }
    private static string LoadAudio(string rawPath, string processedPath, ref Raylib_cs.Sound raw, ref Raylib_cs.Sound processed, ref bool rawLoaded, ref bool processedLoaded, ref int rawFrames, ref int processedFrames, out bool device)
    {
        var rawExists = File.Exists(rawPath); var processedExists = File.Exists(processedPath); var attempted = false; device = false;
        try
        {
            attempted = true; RaylibApi.InitAudioDevice(); device = RaylibApi.IsAudioDeviceReady();
            if (device && rawExists) { raw = RaylibApi.LoadSound(rawPath); rawFrames = (int)raw.FrameCount; rawLoaded = rawFrames > 0; }
            if (device && processedExists) { processed = RaylibApi.LoadSound(processedPath); processedFrames = (int)processed.FrameCount; processedLoaded = processedFrames > 0; }
        }
        catch (Exception exception) { return $"Audio diagnostic: device attempted={attempted}, ready={device}, raw exists={rawExists}, processed exists={processedExists}, loaded frames={rawFrames}/{processedFrames}; {exception.Message}"; }
        return $"Audio diagnostic: device attempted={attempted}, ready={device}, raw exists={rawExists}, processed exists={processedExists}, loaded frames={rawFrames}/{processedFrames}.";
    }
    private static string FindRepositoryRoot() { var directory = AppContext.BaseDirectory; while (!string.IsNullOrWhiteSpace(directory)) { if (File.Exists(Path.Combine(directory, "dotnet-ai-first-2d-game-engine.slnx"))) return directory; directory = Directory.GetParent(directory)?.FullName; } return Directory.GetCurrentDirectory(); }
    private static int Usage() { Console.Error.WriteLine("asset-preview requires --scene <preview-scene.json> [--capture <png>] [--frames <count>]"); return 2; }
    private sealed record PreviewFrame(int SequenceIndex, int SourceFrameIndex, Texture2D Texture);
}
