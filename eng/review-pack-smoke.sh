#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
output_dir="${repo_root}/artifacts/review/latest"

require_file "$tools_project"

"${repo_root}/eng/scenario-smoke.sh"
"${repo_root}/eng/content-validate.sh" scenarios
"${repo_root}/eng/content-validate.sh" assets
"${repo_root}/eng/asset-inspect-smoke.sh"

dotnet_cmd run --no-build --project "$tools_project" -- review pack --input artifacts --output "$output_dir"

require_file "${output_dir}/review-summary.md"
require_file "${output_dir}/review-manifest.json"
require_file "${output_dir}/diagnostics.json"
