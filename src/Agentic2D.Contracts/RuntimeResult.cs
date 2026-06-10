using System.Text.Json.Serialization;

namespace Agentic2D.Contracts;

public sealed record RuntimeResult(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("command")] string Command,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("ticksRequested")] int TicksRequested,
    [property: JsonPropertyName("finalTick")] int FinalTick,
    [property: JsonPropertyName("entities")] IReadOnlyList<EntitySummary> Entities,
    [property: JsonPropertyName("events")] IReadOnlyList<RuntimeEvent> Events,
    [property: JsonPropertyName("assertions")] IReadOnlyList<RuntimeAssertion> Assertions,
    [property: JsonPropertyName("diagnostics")] IReadOnlyList<Diagnostic> Diagnostics);
