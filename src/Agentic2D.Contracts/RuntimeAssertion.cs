using System.Text.Json.Serialization;

namespace Agentic2D.Contracts;

public sealed record RuntimeAssertion(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("passed")] bool Passed,
    [property: JsonPropertyName("message")] string Message);
