namespace Agentic2D.Tools;

/// <summary>Owns the close-intent latch for an active review workbench operation.</summary>
public sealed class ReviewWorkbenchCloseLifecycle
{
    public bool IsRequested { get; private set; }

    public void RequestClose() => IsRequested = true;

    public void CancelRequest() => IsRequested = false;

    public bool CanExit(int pendingWrites, int failedWrites, bool resetActive) =>
        pendingWrites == 0 && failedWrites == 0 && !resetActive;

    public bool ShouldExit(int pendingWrites, int failedWrites, bool resetActive) =>
        IsRequested && CanExit(pendingWrites, failedWrites, resetActive);

    public bool BlocksNewActions => IsRequested;
}
