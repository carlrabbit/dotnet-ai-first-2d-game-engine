#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M032"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation detailed-forest-logistics --output "$out"
environment="$out/graphical-evidence/environment.json"
graphics_capable="${M032_GRAPHICS_CAPABLE:-}"
if [ -z "$graphics_capable" ] && [ -n "${DISPLAY:-}" ]; then graphics_capable=1; fi
if [ "$graphics_capable" != "1" ]; then
  jq -e '.status == "skipped-not-graphics-capable"' "$environment" >/dev/null
  exit 0
fi
capture="$out/graphical-evidence/m032-detailed-region-post-load.png"
status=0
for state in initial movement interruption post-load; do
  state_capture="$out/graphical-evidence/m032-detailed-region-$state.png"
  dotnet_cmd run --no-build --project src/Agentic2D.DebugClient.Raylib -- m032 --input "$out/structural-frames/$state.json" --commands "$out/review-pack/designation-input.jsonl" --capture "$state_capture" || status=$?
  jq -e '.status == "passed" and ([.events[] | .result] | all(. == "accepted"))' "${state_capture%.png}.input-evidence.json" >/dev/null || status=$?
done
if [ "$status" -eq 0 ]; then
  jq -n --arg capture "$capture" --arg environment "${M032_GRAPHICS_CAPABLE:+M032_GRAPHICS_CAPABLE=1}${M032_GRAPHICS_CAPABLE:+; }${DISPLAY:+DISPLAY-session}" '{schema:"agentic2d.m032.graphical-environment.v1",status:"passed",environment:$environment,capture:$capture}' > "$environment"
  dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation detailed-forest-logistics --output "$out"
else
  jq -n '{schema:"agentic2d.m032.graphical-environment.v1",status:"failed",environment:"requested graphics-capable session"}' > "$environment"
  exit 1
fi
