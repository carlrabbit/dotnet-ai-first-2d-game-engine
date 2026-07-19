#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
review_output_dir="${repo_root}/artifacts/review/m011"
workbench_output_dir="${repo_root}/artifacts/workbench/asset-curation-m011"
review_input_dir="$(mktemp -d)"
trap 'rm -rf "$review_input_dir"' EXIT

require_file "$tools_project"

before_status="$(capture_git_status)"

"${repo_root}/eng/asset-inspect-smoke.sh"
"${repo_root}/eng/asset-perception-smoke.sh"
"${repo_root}/eng/review-pack-smoke.sh"
"${repo_root}/eng/asset-curation-smoke.sh"
"${repo_root}/eng/asset-review-smoke.sh"
"${repo_root}/eng/content-validate.sh" assets
"${repo_root}/eng/content-validate.sh" maps
"${repo_root}/eng/map-smoke.sh"
"${repo_root}/eng/runtime-inspect-smoke.sh"

# M011 evidence is deliberately staged from this journey only.  The repository-wide
# artifacts root can contain expected-failure diagnostics from other milestones.
mkdir -p "$review_input_dir/assets" "$review_input_dir/content" "$review_input_dir/scenarios" "$review_input_dir/asset-review"
cp -R "${repo_root}/artifacts/assets/tile-atlas-smoke" "$review_input_dir/assets/"
cp -R "${repo_root}/artifacts/assets/perception" "$review_input_dir/assets/"
cp -R "${repo_root}/artifacts/content/assets" "$review_input_dir/content/"
cp -R "${repo_root}/artifacts/scenarios/runtime-smoke" "$review_input_dir/scenarios/"
cp -R "${repo_root}/artifacts/asset-review" "$review_input_dir/"

dotnet_cmd run --no-build --project "$tools_project" -- review pack --input "$review_input_dir" --output "$review_output_dir"
dotnet_cmd run --no-build --project "$tools_project" -- asset curate --asset asset.tile-atlas-smoke --review-pack "$review_output_dir" --output "$workbench_output_dir"

require_file "${review_output_dir}/review-manifest.json"
require_file "${review_output_dir}/review-summary.md"
require_file "${review_output_dir}/diagnostics.json"
require_file "${workbench_output_dir}/index.html"
require_file "${workbench_output_dir}/review-data.json"
require_file "${workbench_output_dir}/diagnostics.json"

grep -q '"schema": "agentic2d.review-pack.manifest.v1"' "${review_output_dir}/review-manifest.json" || fail "final review pack schema mismatch"
grep -q '"schema": "agentic2d.asset-curation-workbench.review-data.v1"' "${workbench_output_dir}/review-data.json" || fail "final workbench schema mismatch"

assert_git_status_unchanged "$before_status"
