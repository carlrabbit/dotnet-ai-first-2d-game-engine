#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/simulation/M031"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- simulation wood-workflow --output "$out"
jq -e '.validationStatus == "passed" and .loadStatus == "passed" and .freshProcessProof == true' "$out/persistence-report.json" >/dev/null
jq -e '.comparisonStatus == "passed"' "$out/fingerprints.json" >/dev/null
