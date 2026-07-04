#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
maps_output_dir="${repo_root}/artifacts/content/maps"
single_output_dir="${repo_root}/artifacts/content/map-smoke"
inspect_output_dir="${repo_root}/artifacts/maps/map-smoke"

require_file "$tools_project"

dotnet_cmd run --no-build --project "$tools_project" -- content validate maps --output "$maps_output_dir"
dotnet_cmd run --no-build --project "$tools_project" -- content validate game/maps/smoke/map-smoke.map.json --output "$single_output_dir"
dotnet_cmd run --no-build --project "$tools_project" -- map inspect map.smoke --output "$inspect_output_dir"

require_file "${maps_output_dir}/result.json"
require_file "${maps_output_dir}/diagnostics.json"
require_file "${maps_output_dir}/validated-items.json"
require_file "${single_output_dir}/result.json"
require_file "${single_output_dir}/diagnostics.json"
require_file "${single_output_dir}/validated-items.json"
require_file "${inspect_output_dir}/result.json"
require_file "${inspect_output_dir}/diagnostics.json"
require_file "${inspect_output_dir}/map-summary.json"
require_file "${inspect_output_dir}/layers.json"
require_file "${inspect_output_dir}/resolved-references.json"
