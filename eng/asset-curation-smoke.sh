#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
review_pack_dir="${repo_root}/artifacts/review/latest"
output_dir="${repo_root}/artifacts/workbench/asset-curation"

require_file "$tools_project"

"${repo_root}/eng/review-pack-smoke.sh"

dotnet_cmd run --no-build --project "$tools_project" -- asset curate --asset asset.tile-atlas-smoke --review-pack "$review_pack_dir" --output "$output_dir"

require_file "${output_dir}/index.html"
require_file "${output_dir}/review-data.json"
require_file "${output_dir}/diagnostics.json"
