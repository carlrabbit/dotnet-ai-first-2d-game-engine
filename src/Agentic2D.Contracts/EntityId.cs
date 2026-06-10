namespace Agentic2D.Contracts;

public readonly record struct EntityId(string Value)
{
    public static EntityId Player { get; } = new("entity.player");

    public override string ToString() => Value;
}
