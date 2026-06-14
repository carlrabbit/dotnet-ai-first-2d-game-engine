#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
target="${1:-}"

[ -n "$target" ] || fail "usage: ./eng/content-validate.sh <scope-or-path>"
require_file "$tools_project"

safe_target="${target//\//-}"
safe_target="${safe_target//./-}"
output_dir="${repo_root}/artifacts/content/${safe_target}"

dotnet_cmd run --no-build --project "$tools_project" -- content validate "$target" --output "$output_dir"

require_file "${output_dir}/result.json"
require_file "${output_dir}/diagnostics.json"
require_file "${output_dir}/validated-items.json"
