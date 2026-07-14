using Agentic2D.Contracts;

namespace Agentic2D.Input;

/// <summary>Read-only behavior adapter over one resolved semantic frame.</summary>
public sealed class InputFrameBehaviorQuery(InputFrame frame) : IBehaviorInput
{
    public double Scalar(string actionId) => frame.Scalar(actionId).Value;
    public (double X, double Y) Vector2(string actionId)
    {
        var value = frame.Vector2(actionId);
        return (value.X, value.Y);
    }

    public string DigitalPhase(string actionId) => frame.Digital(actionId).Phase.ToString().ToLowerInvariant();
}
