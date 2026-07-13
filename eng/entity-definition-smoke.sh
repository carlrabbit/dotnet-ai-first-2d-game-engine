#!/usr/bin/env bash
set -euo pipefail
out=artifacts/m014/entity-definition
dotnet run --no-build --project src/Agentic2D.Tools -- content validate entities --output "$out/content"
dotnet run --no-build --project src/Agentic2D.Tools -- scenario run entity.definition-instantiation-smoke --output "$out/scenario"
dotnet run --no-build --project src/Agentic2D.Tools -- runtime inspect --scenario entity.definition-instantiation-smoke --map map.interaction-smoke --output "$out/inspect"
test -s "$out/inspect/entity-instantiations.jsonl"
jq -s -e 'all(.[]; .schema == "agentic2d.entity-instantiation.v1" and (.generatedCommands[0] == "CreateEntity") and (.generatedCommands[1] == "provenance")) and any(.[]; .spawnId == "spawn.dynamic.npc" and .committed == true)' "$out/inspect/entity-instantiations.jsonl" >/dev/null
