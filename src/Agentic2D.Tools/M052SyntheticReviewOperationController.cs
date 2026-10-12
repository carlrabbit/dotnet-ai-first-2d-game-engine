using Agentic2D.UI;

namespace Agentic2D.Tools;

/// <summary>Application-owned deterministic operation fixture for the M052 review stress screen.</summary>
public sealed class M052SyntheticReviewOperationController
{
    private readonly Queue<AgentUiOperationStatus> outcomes;
    private double remaining;
    private long attempt;

    public AgentUiOperationStatus Status { get; private set; } = AgentUiOperationStatus.Idle;
    public long Attempt => attempt;
    public bool CanStart => Status == AgentUiOperationStatus.Idle;
    public bool CanRetry => (Status is AgentUiOperationStatus.Failed or AgentUiOperationStatus.Cancelled) && outcomes.Count > 0;
    public UiOperationProjection Projection => new(
        "preview.synthetic",
        Status,
        Status switch
        {
            AgentUiOperationStatus.Idle => "Preview is ready.",
            AgentUiOperationStatus.Working => "Rendering candidate preview…",
            AgentUiOperationStatus.Succeeded => "Candidate preview rendered.",
            AgentUiOperationStatus.Failed => CanRetry ? "Preview unavailable. Retry is ready." : "Preview unavailable. No retry remains.",
            AgentUiOperationStatus.Cancelled => CanRetry ? "Preview cancelled. Retry is ready." : "Preview cancelled. No retry remains.",
            _ => "Preview status is unknown."
        },
        CanRetry ? "stress.retry" : null);

    public M052SyntheticReviewOperationController(IEnumerable<AgentUiOperationStatus>? outcomes = null)
    {
        this.outcomes = new Queue<AgentUiOperationStatus>(outcomes ?? [AgentUiOperationStatus.Failed, AgentUiOperationStatus.Succeeded]);
        if (this.outcomes.Any(x => x is not (AgentUiOperationStatus.Succeeded or AgentUiOperationStatus.Failed)))
            throw new ArgumentException("Fixture outcomes must be confirmed success or failure results.", nameof(outcomes));
    }

    public bool Start()
    {
        if (Status != AgentUiOperationStatus.Idle) return false;
        return BeginAttempt();
    }

    public bool Retry()
    {
        if (Status is not (AgentUiOperationStatus.Failed or AgentUiOperationStatus.Cancelled)) return false;
        return BeginAttempt();
    }

    public bool Cancel()
    {
        if (Status != AgentUiOperationStatus.Working) return false;
        Status = AgentUiOperationStatus.Cancelled;
        remaining = 0;
        return true;
    }

    public bool Advance(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedSeconds));
        if (Status != AgentUiOperationStatus.Working) return false;
        remaining -= elapsedSeconds;
        if (remaining > 0) return false;
        Status = outcomes.Dequeue();
        return true;
    }

    private bool BeginAttempt()
    {
        if (outcomes.Count == 0) return false;
        attempt++;
        remaining = .35;
        Status = AgentUiOperationStatus.Working;
        return true;
    }
}
