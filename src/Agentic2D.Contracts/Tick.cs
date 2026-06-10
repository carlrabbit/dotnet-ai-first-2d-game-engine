namespace Agentic2D.Contracts;

public readonly record struct Tick(ulong Value) : IComparable<Tick>
{
    public static Tick Zero { get; } = new(0);

    public Tick Next() => new(checked(Value + 1));

    public int CompareTo(Tick other) => Value.CompareTo(other.Value);

    public override string ToString() => Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
