#!/usr/bin/env bash
set -euo pipefail
out=artifacts/m014/interaction
dotnet run --no-build --project src/Agentic2D.Tools -- content validate game/maps/smoke/map-interaction-smoke.map.json --output "$out/content-map"
dotnet run --no-build --project src/Agentic2D.Tools -- scenario run interaction.npc-smoke --output "$out/scenario"
dotnet run --no-build --project src/Agentic2D.Tools -- runtime inspect --scenario interaction.npc-smoke --map map.interaction-smoke --output "$out/inspect"
test -s "$out/inspect/interaction-resolutions.jsonl"
jq -s -e 'any(.[]; .status == "accepted" and .selectedTargetId == "entity.npc.talkable-smoke" and .selectionReason == "nearest-eligible" and .commandReference != null and (.events | index("interaction.started")))' "$out/inspect/interaction-resolutions.jsonl" >/dev/null
jq -s -e 'any(.[]; .type == "interaction.started")' "$out/inspect/events.jsonl" >/dev/null
