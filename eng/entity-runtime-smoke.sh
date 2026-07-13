#!/usr/bin/env bash
set -euo pipefail
out=artifacts/runtime/entity-runtime-smoke
dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario continuous.kinematic-movement-smoke --map map.continuous-smoke --output "$out"
test -s "$out/component-mutations.jsonl"
jq -s -e 'all(.[]; (.Schema == "agentic2d.entity-component-mutation.v1") and ((.EntityId == "entity.player") or (.EntityId == "entity.npc.smoke"))) and any(.[]; .Status == "accepted" and .MutationKind == "component-updated" and .PreviousValue != null and .ResultingValue != null) and (map(.Sequence) == [range(1; length + 1)])' "$out/component-mutations.jsonl" >/dev/null
