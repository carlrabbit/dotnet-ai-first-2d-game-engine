#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
scenario_path="game/scenarios/smoke/runtime-smoke.json"
output_dir="${repo_root}/artifacts/scenarios/runtime-smoke"

require_file "$tools_project"
require_file "$scenario_path"

dotnet_cmd run --no-build --project "$tools_project" -- scenario run "$scenario_path" --output "$output_dir"

require_file "${output_dir}/result.json"
require_file "${output_dir}/events.jsonl"
require_file "${output_dir}/diagnostics.json"
