#!/usr/bin/env bash
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

output_dir="${repo_root}/artifacts/scenarios/behavior-grid-movement-smoke"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- content validate game/scenarios/smoke/behavior-grid-movement-smoke.json --output "${repo_root}/artifacts/content/behavior-grid-movement-smoke"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- scenario run behavior.grid-movement-smoke --output "$output_dir"
require_file "${output_dir}/result.json"
grep -q '"status": "passed"' "${output_dir}/result.json" || fail "behavior scenario did not pass"
