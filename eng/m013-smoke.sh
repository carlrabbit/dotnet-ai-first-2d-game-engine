#!/usr/bin/env bash
set -euo pipefail
./eng/entity-runtime-smoke.sh
./eng/continuous-spatial-smoke.sh
out=artifacts/runtime/m013
dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario continuous.kinematic-tree-collision-smoke --map map.continuous-smoke --output "$out"
test -s "$out/component-mutations.jsonl"
test -s "$out/continuous-resolutions.jsonl"
jq -s -e 'any(.[]; .CollisionCandidates | any(.SourceId == "object.tree.large.smoke"))' "$out/continuous-resolutions.jsonl" >/dev/null
dotnet run --project src/Agentic2D.Tools -- review pack --input artifacts --output artifacts/review/m013
test -f artifacts/review/m013/review-manifest.json
