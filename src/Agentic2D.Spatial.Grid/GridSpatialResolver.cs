using Agentic2D.Contracts;
using Agentic2D.Validation;

namespace Agentic2D.Spatial.Grid;

public sealed record GridPosition(int X, int Y);
public sealed record GridResolutionDetail(SpatialResolution Resolution, GridPosition? Destination, string SemanticSource, string? SemanticValue, string? AssetId, string? TileId);

public sealed class GridSpatialResolver : ISpatialResolver
{
    public const string ModuleId = "spatial.grid";
    private readonly MapContentSource map;
    private readonly Dictionary<string, GridPosition> positions;
    public GridSpatialResolver(MapContentSource map, IReadOnlyDictionary<string, GridPosition> initialPositions) { this.map = map; positions = new(initialPositions, StringComparer.Ordinal); }
    public string Id => ModuleId;
    public SpatialResolution Resolve(MoveIntent intent) => ResolveDetailed(intent).Resolution;
    public GridResolutionDetail ResolveDetailed(MoveIntent intent)
    {
        if (!positions.TryGetValue(intent.EntityId, out var position)) return Reject(intent, "invalid-position", null, "none", null, null, null, "GRID0001");
        var destination = intent.Direction switch { "north" => new GridPosition(position.X, position.Y - 1), "east" => new GridPosition(position.X + 1, position.Y), "south" => new GridPosition(position.X, position.Y + 1), "west" => new GridPosition(position.X - 1, position.Y), _ => null };
        if (destination is null) return Reject(intent, "unsupported-intent", null, "none", null, null, null, "GRID0007");
        if (destination.X < 0 || destination.X >= map.Width || destination.Y < 0 || destination.Y >= map.Height) return Reject(intent, "out-of-bounds", destination, "bounds", null, null, null, "GRID0002");
        var overrideValue = map.CellOverrides.FirstOrDefault(cell => cell.X == destination.X && cell.Y == destination.Y)?.PhysicalBehavior;
        if (!string.IsNullOrEmpty(overrideValue)) return overrideValue == "walkable" ? Accept(intent, destination, "map-cell-override", overrideValue, null, null) : Reject(intent, "blocked", destination, "map-cell-override", overrideValue, null, null, "GRID0004");
        var cell = map.Layers.OrderBy(layer => layer.Id, StringComparer.Ordinal).SelectMany(layer => layer.Cells).FirstOrDefault(item => item.X == destination.X && item.Y == destination.Y);
        if (cell is null) return Reject(intent, "unresolved", destination, "none", null, null, null, "GRID0003");
        var asset = AssetMetadataLocator.ResolveById(cell.AssetId); var metadata = asset.IsSuccess ? new AssetMetadataValidator().ValidateFile(asset.MetadataPath).Metadata : null; var tile = metadata?.Tiles.FirstOrDefault(item => item.Id == cell.TileId); var physical = tile?.PhysicalBehaviorsApproved.FirstOrDefault(value => value is "walkable" or "blocked");
        if (physical is null) return Reject(intent, "unresolved", destination, "none", null, cell.AssetId, cell.TileId, "GRID0006");
        return physical == "walkable" ? Accept(intent, destination, "approved-referenced-tile", physical, cell.AssetId, cell.TileId) : Reject(intent, "blocked", destination, "approved-referenced-tile", physical, cell.AssetId, cell.TileId, "GRID0005");
    }
    public void ApplyAccepted(GridResolutionDetail detail) { if (detail.Resolution.Accepted && detail.Destination is not null) positions[detail.Resolution.EntityId] = detail.Destination; }
    public GridPosition? QueryPosition(string entityId) => positions.TryGetValue(entityId, out var value) ? value : null;
    private static GridResolutionDetail Accept(MoveIntent intent, GridPosition destination, string source, string value, string? asset, string? tile) => new(new SpatialResolution(intent.Id, ModuleId, intent.EntityId, true, "walkable", $"command.{intent.Id}", ["spatial.movement-accepted", "entity.grid-position-changed"], []), destination, source, value, asset, tile);
    private static GridResolutionDetail Reject(MoveIntent intent, string reason, GridPosition? destination, string source, string? value, string? asset, string? tile, string diagnostic) => new(new SpatialResolution(intent.Id, ModuleId, intent.EntityId, false, reason, null, ["spatial.movement-rejected"], [diagnostic]), destination, source, value, asset, tile);
}
