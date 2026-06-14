using System.Text.Json.Serialization;

namespace Agentic2D.Validation;

public sealed class ScenarioSource
{
    [JsonPropertyName("schema")]
    public string Schema { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; init; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("purpose")]
    public string Purpose { get; init; } = string.Empty;

    [JsonPropertyName("seedPolicy")]
    public string SeedPolicy { get; init; } = string.Empty;

    [JsonPropertyName("runtime")]
    public ScenarioRuntimeSource? Runtime { get; init; }

    [JsonPropertyName("initialState")]
    public ScenarioInitialStateSource? InitialState { get; init; }

    [JsonPropertyName("steps")]
    public IReadOnlyList<ScenarioStepSource> Steps { get; init; } = [];

    [JsonPropertyName("expectedEvents")]
    public IReadOnlyList<string> ExpectedEvents { get; init; } = [];

    [JsonPropertyName("assertions")]
    public IReadOnlyList<ScenarioAssertionSource> Assertions { get; init; } = [];

    [JsonPropertyName("artifacts")]
    public ScenarioArtifactsSource? Artifacts { get; init; }

    [JsonPropertyName("humanReview")]
    public ScenarioHumanReviewSource? HumanReview { get; init; }
}

public sealed record ScenarioRuntimeSource(
    [property: JsonPropertyName("ticks")] int Ticks = 0);

public sealed record ScenarioInitialStateSource(
    [property: JsonPropertyName("entities")] IReadOnlyList<ScenarioEntitySource> Entities)
{
    public ScenarioInitialStateSource()
        : this([])
    {
    }
}

public sealed record ScenarioEntitySource(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("position")] int Position);

public sealed record ScenarioStepSource(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("command")] ScenarioCommandSource Command);

public sealed record ScenarioCommandSource(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("entityId")] string EntityId,
    [property: JsonPropertyName("amount")] int Amount);

public sealed record ScenarioAssertionSource(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("entityId")] string? EntityId = null,
    [property: JsonPropertyName("position")] int? Position = null,
    [property: JsonPropertyName("eventType")] string? EventType = null);

public sealed record ScenarioArtifactsSource(
    [property: JsonPropertyName("result")] string Result = "",
    [property: JsonPropertyName("events")] string Events = "",
    [property: JsonPropertyName("diagnostics")] string Diagnostics = "");

public sealed record ScenarioHumanReviewSource(
    [property: JsonPropertyName("required")] bool Required = false);
