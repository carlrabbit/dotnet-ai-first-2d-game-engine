#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M031"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation wood-workflow --output "$out"
jq -e '.status == "passed" and .woodConserved == true and .reservationsReleased == true' "$out/wood-workflow/comparison.json" >/dev/null
require_file "$out/wood-workflow/fresh-process.json"
jq -e '.status == "passed" and .processExitCode == 0' "$out/wood-workflow/fresh-process.json" >/dev/null
require_file "$out/review-pack/review-manifest.json"
