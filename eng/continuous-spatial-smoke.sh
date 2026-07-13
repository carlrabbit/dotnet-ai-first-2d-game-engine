#!/usr/bin/env bash
set -euo pipefail
accepted=artifacts/runtime/continuous-kinematic-movement-smoke
tree=artifacts/runtime/continuous-kinematic-tree-collision-smoke
dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario continuous.kinematic-movement-smoke --map map.continuous-smoke --output "$accepted"
dotnet run --project src/Agentic2D.Tools -- runtime inspect --scenario continuous.kinematic-tree-collision-smoke --map map.continuous-smoke --output "$tree"
test -s "$accepted/continuous-resolutions.jsonl"
test -s "$tree/continuous-resolutions.jsonl"
jq -s -e 'all(.[]; .Schema == "agentic2d.continuous-spatial-resolution.v1" and .RequestedDisplacement != null and .AppliedDisplacement != null and .XAxis != null and .YAxis != null and .ResultingTransform != null) and any(.[]; .Outcome == "accepted" and .MutationCommandId != null)' "$accepted/continuous-resolutions.jsonl" >/dev/null
jq -s -e 'any(.[]; .Outcome == "clipped" and (.CollisionCandidates | any(.SourceId == "object.tree.large.smoke")) and .MutationCommandId != null)' "$tree/continuous-resolutions.jsonl" >/dev/null
