#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M034"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation m034-settlement --output "$out"
if [ -z "${DISPLAY:-}" ] && [ "${M034_GRAPHICS_CAPABLE:-}" != "1" ]; then
  jq -n '{schema:"agentic2d.m034.graphical-environment.v1",status:"skipped-not-graphics-capable",reason:"headless engineering environment; supported Raylib session required",structuralEvidence:"../structural-frames/region.fields.json"}' > "$out/graphical-evidence/environment.json"
  exit 0
fi
capture="$out/graphical-evidence/m034-settlement-operations.png"
dotnet_cmd run --no-build --project src/Agentic2D.DebugClient.Raylib -- m034 --input "$out/world-dashboard.json" --capture "$capture"
jq -n --arg capture "$capture" '{schema:"agentic2d.m034.graphical-environment.v1",status:"passed",environment:"Raylib graphics-capable session",capture:$capture,structuralEvidence:"../world-dashboard.json"}' > "$out/graphical-evidence/environment.json"
