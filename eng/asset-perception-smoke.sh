#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
output_dir="${repo_root}/artifacts/assets/perception/tile-atlas-smoke"

require_file "$tools_project"

dotnet_cmd run --no-build --project "$tools_project" -- asset perceive asset.tile-atlas-smoke --output "$output_dir"

require_file "${output_dir}/result.json"
require_file "${output_dir}/diagnostics.json"
require_file "${output_dir}/tile-features.json"
require_file "${output_dir}/semantic-proposals.json"
