#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M032"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation detailed-forest-logistics --output "$out"
jq -e '[.activities[] | select(.status == "Completed")] | length >= 3' "$out/activities.json" >/dev/null
