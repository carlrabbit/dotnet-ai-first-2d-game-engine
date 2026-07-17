namespace SignalPassage.Game;

public sealed record EnergyFragmentComponent(string FragmentId, bool Collected = false);
public sealed record ContainerComponent(string ContainerId, bool Opened = false);
public sealed record HazardComponent(string HazardId, int Damage = 1);
public sealed record SignalPassageObjectiveComponent(int FragmentsCollected, bool MechanismActive, bool ExitOpen, bool Completed)
{
    public SignalPassageObjectiveComponent Collect() => this with { FragmentsCollected = Math.Min(3, FragmentsCollected + 1) };
    public SignalPassageObjectiveComponent Activate() => FragmentsCollected >= 3 ? this with { MechanismActive = true, ExitOpen = true } : this;
    public SignalPassageObjectiveComponent Complete() => ExitOpen ? this with { Completed = true } : this;
}
