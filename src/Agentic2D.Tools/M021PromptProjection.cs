using Agentic2D.Persistence;
using Agentic2D.Presentation;

namespace Agentic2D.Tools;

internal static class M021PromptProjection
{
    public static IReadOnlyList<object> Project(M021AuthoritativeSource source, bool postLoad, object current)
    {
        if (postLoad) return [current];
        return [
            new { promptId = "prompt.collect-crystal", semanticActionId = "action.collect", targetEntityId = PersistentIds.Crystal, textResourceId = "text.prompt.collect-crystal", enabled = true, reasonId = (string?)null, semanticInputActionId = "input.interact", priority = 20, runtimeTick = 1, authoritativeSource = "m020-item.collected-candidate", fingerprint = PresentationDeterminism.Hash("prompt.collect-crystal|1|" + PersistentIds.Crystal) },
            new { promptId = "prompt.locked-door", semanticActionId = "action.interact", targetEntityId = PersistentIds.Door, textResourceId = "text.prompt.locked-door", enabled = false, reasonId = source.LockedDoorCondition.Detail, semanticInputActionId = "input.interact", priority = 10, runtimeTick = 3, authoritativeSource = "m020-condition-evaluation", fingerprint = PresentationDeterminism.Hash("prompt.locked-door|3|" + source.LockedDoorCondition.Detail) },
            new { promptId = "prompt.activate-switch", semanticActionId = "action.interact", targetEntityId = PersistentIds.Switch, textResourceId = "text.prompt.activate-switch", enabled = true, reasonId = (string?)null, semanticInputActionId = "input.interact", priority = 10, runtimeTick = 4, authoritativeSource = "m020-switch-candidate", fingerprint = PresentationDeterminism.Hash("prompt.activate-switch|4|" + PersistentIds.Switch) },
            new { promptId = "prompt.open-door", semanticActionId = "action.interact", targetEntityId = PersistentIds.Door, textResourceId = "text.prompt.open-door", enabled = true, reasonId = (string?)null, semanticInputActionId = "input.interact", priority = 10, runtimeTick = 5, authoritativeSource = "m020-door-open-candidate", fingerprint = PresentationDeterminism.Hash("prompt.open-door|5|" + PersistentIds.Door) }
        ];
    }
}
