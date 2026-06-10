namespace Agentic2D.Contracts;

public readonly record struct EntityId(Guid Value)
{
    public static EntityId Empty { get; } = new(Guid.Empty);

    public override string ToString() => Value.ToString("D");
}
