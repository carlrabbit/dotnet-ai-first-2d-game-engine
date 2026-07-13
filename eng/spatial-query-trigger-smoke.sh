#!/usr/bin/env bash
set -euo pipefail
out=artifacts/m014/spatial-trigger
dotnet run --no-build --project src/Agentic2D.Tools -- scenario run trigger.enter-exit-smoke --output "$out/scenario"
dotnet run --no-build --project src/Agentic2D.Tools -- runtime inspect --scenario trigger.enter-exit-smoke --map map.interaction-smoke --output "$out/inspect"
test -s "$out/inspect/spatial-queries.jsonl"
test -s "$out/inspect/trigger-transitions.jsonl"
jq -s -e 'any(.[]; (.enteredIds | length) > 0) and any(.[]; (.exitedIds | length) > 0) and all(.[]; has("stayed") | not)' "$out/inspect/trigger-transitions.jsonl" >/dev/null
jq -s -e 'all(.[]; .queryKind == "aabb-overlap" or .queryKind == "radius")' "$out/inspect/spatial-queries.jsonl" >/dev/null
