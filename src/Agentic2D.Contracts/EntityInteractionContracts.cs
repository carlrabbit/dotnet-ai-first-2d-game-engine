namespace Agentic2D.Contracts;

/// <summary>Runtime-owned semantic metadata used by deterministic spatial filters.</summary>
public sealed record SemanticTags(IReadOnlyList<string> Values);
public sealed record TriggerFilter(string? EntityId, IReadOnlyList<string> RequiredTags, IReadOnlyList<string> RequiredComponentTypeIds);
public sealed record TriggerVolume2(double HalfWidth, double HalfHeight, TriggerFilter Filter, string? TriggerId);
public sealed record Interactable(string InteractionKind, double Range, IReadOnlyList<string> AllowedInteractorTags, IReadOnlyList<string> RequiredInteractorComponentTypeIds);
public sealed record BeginInteractionCommand(string Id, string IntentId, string InteractorEntityId, string TargetEntityId, string InteractionKind);
public sealed record RuntimeEntityProvenance(string DefinitionId, string SpawnId, string SourceKind, string SourceId, string? SourcePath, IReadOnlyList<string> OverrideSummary, string? BehaviorSource);
