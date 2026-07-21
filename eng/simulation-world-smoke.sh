#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M031"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation wood-workflow --output "$out"
jq -e '(.schema == "agentic2d.simulation-world-inspection.v1") and (.regionOwnedEntityCount == 3) and (.regions | length == 2)' "$out/world-after.json" >/dev/null
