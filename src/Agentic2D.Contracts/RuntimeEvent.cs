using System.Text.Json.Serialization;

namespace Agentic2D.Contracts;

public sealed record RuntimeEvent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("tick")] int Tick,
    [property: JsonPropertyName("message")] string Message);
