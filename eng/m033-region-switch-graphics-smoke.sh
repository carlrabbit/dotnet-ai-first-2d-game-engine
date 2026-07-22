#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M033"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation m033-multi-region --output "$out"
if [ -z "${DISPLAY:-}" ] && [ "${M033_GRAPHICS_CAPABLE:-}" != "1" ]; then
  jq -e '.status == "skipped-not-graphics-capable"' "$out/graphical-evidence/environment.json" >/dev/null
  exit 0
fi
capture="$out/graphical-evidence/m033-region-switch.png"
dotnet_cmd run --no-build --project src/Agentic2D.DebugClient.Raylib -- m033 --input "$out/transition-events.jsonl" --capture "$capture"
test -s "$capture"
jq -n --arg capture "$capture" '{schema:"agentic2d.m033.graphical-environment.v1",status:"passed",environment:"Raylib graphics-capable session",capture:$capture,structuralEvidence:"../transition-events.jsonl"}' > "$out/graphical-evidence/environment.json"
review_manifest="$out/review-pack/review-manifest.json"
jq '.status = "ready-for-human-review" | .graphicalStatus = "passed"' "$review_manifest" > "$review_manifest.tmp"
mv "$review_manifest.tmp" "$review_manifest"
