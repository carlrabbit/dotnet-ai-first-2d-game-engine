#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/readiness/M035"
dashboard="$repo_root/artifacts/simulation/M034/world-dashboard.json"
capture="$out/graphical-soak/m035-operations-session.png"
session="$out/graphical-soak/session.json"
if [ "${M035_GRAPHICS_CAPABLE:-}" != "1" ]; then
  dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation m035-readiness --graphical --output "$out"
  jq -e '.status == "skipped-not-graphics-capable"' "$out/graphical-soak-report.json" >/dev/null
  exit 0
fi
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation m034-settlement --output "$repo_root/artifacts/simulation/M034"
duration="${M035_GRAPHICAL_SOAK_SECONDS:-14400}"
if ! test -f "$session" || ! jq -e '.completedSeconds >= 14400 and .earlyTermination == false' "$session" >/dev/null; then
  dotnet_cmd run --no-build --project src/Agentic2D.DebugClient.Raylib -- m035 --input "$dashboard" --duration-seconds "$duration" --capture "$capture" --output "$session"
fi
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation m035-readiness --graphical --output "$out"
jq --arg capture "graphical-soak/m035-operations-session.png" --arg environment "Raylib graphics-capable session; M035_GRAPHICS_CAPABLE=1" '.capture = $capture | .environment = $environment | .operatorSessionManifest = "../tester-session-index.json" | .session = "graphical-soak/session.json"' "$out/graphical-soak-report.json" > "$out/graphical-soak-report.json.tmp"
mv "$out/graphical-soak-report.json.tmp" "$out/graphical-soak-report.json"
jq -e '.status == "passed"' "$out/graphical-soak-report.json" >/dev/null
