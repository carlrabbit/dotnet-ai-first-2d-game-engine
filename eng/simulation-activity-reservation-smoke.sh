#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M031"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation wood-workflow --output "$out"
jq -e '(.activities | length == 1) and (.activities[0].status == "Completed") and (.activities[0].stage == "completed")' "$out/activities.json" >/dev/null
jq -e 'all(.reservations[]; .status != "Active")' "$out/reservations.json" >/dev/null
