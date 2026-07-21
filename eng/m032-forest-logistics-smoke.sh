#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M032"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation detailed-forest-logistics --output "$out"
jq -e '.status == "passed" and .storedWoodTarget == 6 and .conservation == true' "$out/forest-logistics/comparison.json" >/dev/null
require_file "$out/forest-logistics/fresh-process.json"
jq -e '.status == "passed" and .processExitCode == 0' "$out/forest-logistics/fresh-process.json" >/dev/null
require_file "$out/review-pack/review-manifest.json"
