namespace Agentic2D.Engine;

/// <summary>Explicit command boundary that commits resolver proposals to the runtime.</summary>
public static class SpatialMutationCommitter
{
    public static EntityComponentBatchResult Commit(EntityComponentWorld world, EntityComponentBatchMutation? proposal, int tick, string? commandId)
        => proposal is null ? new(false, "rejected", []) : world.CommitBatch([proposal], tick, commandId);
}
