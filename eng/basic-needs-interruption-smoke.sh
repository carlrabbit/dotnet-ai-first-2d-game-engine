#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M032"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation detailed-forest-logistics --output "$out"
jq -e '.interruption == "mandatory-food"' "$out/needs.json" >/dev/null
