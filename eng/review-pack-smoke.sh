#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
output_dir="${repo_root}/artifacts/review/latest"
input_dir="$(mktemp -d)"
trap 'rm -rf "$input_dir"' EXIT

require_file "$tools_project"

"${repo_root}/eng/scenario-smoke.sh"
"${repo_root}/eng/content-validate.sh" scenarios
"${repo_root}/eng/content-validate.sh" assets
"${repo_root}/eng/asset-inspect-smoke.sh"

# Keep this smoke scoped to the artifacts it produced.  The repository artifact
# root legitimately retains expected-failure evidence from other milestone tests.
mkdir -p "$input_dir/scenarios" "$input_dir/content" "$input_dir/assets"
cp -R "${repo_root}/artifacts/scenarios/runtime-smoke" "$input_dir/scenarios/"
cp -R "${repo_root}/artifacts/content/scenarios" "$input_dir/content/"
cp -R "${repo_root}/artifacts/content/assets" "$input_dir/content/"
cp -R "${repo_root}/artifacts/assets/tile-atlas-smoke" "$input_dir/assets/"

dotnet_cmd run --no-build --project "$tools_project" -- review pack --input "$input_dir" --output "$output_dir"

require_file "${output_dir}/review-summary.md"
require_file "${output_dir}/review-manifest.json"
require_file "${output_dir}/diagnostics.json"
