using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Agentic2D.Validation;

public sealed class EntityDefinitionValidator
{
    public const string EntitiesScope = "entities";
    public const string Schema = "agentic2d.entity-definition.v1";
    private static readonly Regex Stable = new("^[a-z0-9]+([.-][a-z0-9]+)*$", RegexOptions.CultureInvariant);
    private static readonly HashSet<string> KnownComponents = new(StringComparer.Ordinal)
    {
        "component.continuous-transform-2d", "component.kinematic-motion-2d", "component.collision-aabb-2d", "component.spatial-membership", "component.trigger-volume-2d", "component.interactable"
    };

    public EntityDefinitionValidationItem ValidateFile(string path)
    {
        var relative = ContentTargetResolver.ToRepositoryRelativePath(path);
        try
        {
            var definition = JsonSerializer.Deserialize<EntityDefinitionSource>(File.ReadAllText(path), ContentValidationJson.Options);
            if (definition is null) return EntityDefinitionValidationItem.Failed(relative, "", [Diagnostic(relative, "json", "Definition JSON must contain an object.")]);
            var diagnostics = Validate(definition, relative);
            return new(definition, relative, definition.Id, diagnostics.Any(d => d.Severity == ContentDiagnosticSeverity.Error) ? ContentValidationStatus.Failed : ContentValidationStatus.Passed, diagnostics);
        }
        catch (JsonException e) { return EntityDefinitionValidationItem.Failed(relative, "", [Diagnostic(relative, "json", e.Message)]); }
        catch (IOException e) { return EntityDefinitionValidationItem.Failed(relative, "", [Diagnostic(relative, "json", e.Message)]); }
    }

    public IReadOnlyList<ContentValidationDiagnostic> Validate(EntityDefinitionSource definition, string target)
    {
        var result = new List<ContentValidationDiagnostic>();
        if (definition.Schema != Schema) result.Add(Diagnostic(target, "schema", "Entity definition schema must be agentic2d.entity-definition.v1."));
        if (!Stable.IsMatch(definition.Id ?? string.Empty)) result.Add(Diagnostic(target, "id", "Definition ID must use lowercase dotted segments."));
        var tags = new HashSet<string>(StringComparer.Ordinal);
        foreach (var tag in definition.SemanticTags)
            if (!Stable.IsMatch(tag) || !tags.Add(tag)) result.Add(Diagnostic(target, "semanticTags", "Semantic tags must be unique stable lowercase IDs."));
        var components = new HashSet<string>(StringComparer.Ordinal);
        foreach (var component in definition.Components)
        {
            if (!KnownComponents.Contains(component.ComponentType)) result.Add(Diagnostic(target, "components", "Unknown component type: " + component.ComponentType));
            if (!components.Add(component.ComponentType)) result.Add(Diagnostic(target, "components", "Duplicate component type: " + component.ComponentType));
            if (component.Value.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null) result.Add(Diagnostic(target, "components", "Component values must be complete objects."));
        }
        if (definition.Behavior is not null && (!Stable.IsMatch(definition.Behavior.Id) || !Stable.IsMatch(definition.Behavior.BehaviorId) || definition.Behavior.Lifecycle is not ("once" or "each-tick"))) result.Add(Diagnostic(target, "behavior", "Behavior must have stable IDs and a known lifecycle."));
        return result.OrderBy(x => x.Field, StringComparer.Ordinal).ToArray();
    }

    private static ContentValidationDiagnostic Diagnostic(string target, string field, string message) => new("ENTITYDEF0001", ContentDiagnosticSeverity.Error, message, target, field);
}

public sealed class EntityDefinitionCatalog
{
    public EntityDefinitionCatalog(IReadOnlyDictionary<string, EntityDefinitionSource> definitions) => Definitions = definitions;
    public IReadOnlyDictionary<string, EntityDefinitionSource> Definitions { get; }
    public bool TryGet(string id, out EntityDefinitionSource? definition) => Definitions.TryGetValue(id, out definition);
    public static EntityDefinitionCatalog LoadAll(out IReadOnlyList<ContentValidationDiagnostic> diagnostics)
    {
        var root = Path.Combine(ContentTargetResolver.FindRepositoryRoot(), "game", "entities");
        var validator = new EntityDefinitionValidator(); var all = new List<ContentValidationDiagnostic>(); var values = new Dictionary<string, EntityDefinitionSource>(StringComparer.Ordinal);
        foreach (var path in Directory.Exists(root) ? Directory.EnumerateFiles(root, "entity-definition.*.json", SearchOption.AllDirectories).Order(StringComparer.Ordinal).ToArray() : Array.Empty<string>())
        {
            var item = validator.ValidateFile(path); all.AddRange(item.Diagnostics);
            if (item.Definition is not null && item.Status == ContentValidationStatus.Passed && !values.TryAdd(item.Definition.Id, item.Definition)) all.Add(new("ENTITYDEF0002", ContentDiagnosticSeverity.Error, "Duplicate definition ID: " + item.Definition.Id, item.Path, "id"));
        }
        diagnostics = all; return new(values);
    }
}

public sealed class EntityDefinitionSource
{
    [JsonPropertyName("schema")] public string Schema { get; init; } = string.Empty;
    [JsonPropertyName("id")] public string Id { get; init; } = string.Empty;
    [JsonPropertyName("semanticTags")] public IReadOnlyList<string> SemanticTags { get; init; } = [];
    [JsonPropertyName("components")] public IReadOnlyList<EntityDefinitionComponentSource> Components { get; init; } = [];
    [JsonPropertyName("behavior")] public EntityDefinitionBehaviorSource? Behavior { get; init; }
    [JsonPropertyName("visualAssetId")] public string? VisualAssetId { get; init; }
    [JsonPropertyName("visualDefinitionId")] public string? VisualDefinitionId { get; init; }
}
public sealed record EntityDefinitionComponentSource([property: JsonPropertyName("componentType")] string ComponentType, [property: JsonPropertyName("value")] JsonElement Value);
public sealed record EntityDefinitionBehaviorSource([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("behaviorId")] string BehaviorId, [property: JsonPropertyName("lifecycle")] string Lifecycle);
public sealed record EntitySpawnSource([property: JsonPropertyName("id")] string Id, [property: JsonPropertyName("entityId")] string EntityId, [property: JsonPropertyName("definitionId")] string DefinitionId, [property: JsonPropertyName("overrides")] IReadOnlyList<EntityDefinitionComponentSource> Overrides)
{
    public EntitySpawnSource() : this(string.Empty, string.Empty, string.Empty, []) { }
}
public sealed record EntityDefinitionValidationItem(EntityDefinitionSource? Definition, string Path, string Id, string Status, IReadOnlyList<ContentValidationDiagnostic> Diagnostics)
{
    public static EntityDefinitionValidationItem Failed(string path, string id, IReadOnlyList<ContentValidationDiagnostic> diagnostics) => new(null, path, id, ContentValidationStatus.Failed, diagnostics);
}
