namespace Agentic2D.Tools;

/// <summary>Prevents preview state from reporting playback unless both the device and selected media are ready.</summary>
public static class PreviewAudioPlaybackPolicy
{
    public static bool CanStartPlayback(bool deviceReady, bool mediaLoaded) => deviceReady && mediaLoaded;
}
