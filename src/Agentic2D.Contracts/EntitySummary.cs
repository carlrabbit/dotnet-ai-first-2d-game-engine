using System.Text.Json.Serialization;

namespace Agentic2D.Contracts;

public sealed record EntitySummary(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("position")] int Position);
