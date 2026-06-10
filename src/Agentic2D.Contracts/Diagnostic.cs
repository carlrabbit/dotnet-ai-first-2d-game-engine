using System.Text.Json.Serialization;

namespace Agentic2D.Contracts;

public sealed record Diagnostic(
    [property: JsonPropertyName("severity")] string Severity,
    [property: JsonPropertyName("code")] string Code,
    [property: JsonPropertyName("message")] string Message);
