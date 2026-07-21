#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M031"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation wood-workflow --output "$out"
for file in world-before.json world-after.json regions.json entities.json activities.json reservations.json invariants.json diagnostics.json performance-baseline.json; do require_file "$out/$file"; done
jq -e '.status == "passed" and .conservation == true' "$out/invariants.json" >/dev/null
