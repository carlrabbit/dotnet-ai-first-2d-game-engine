#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
output_dir="${repo_root}/artifacts/cli/validate"

require_file "$tools_project"

dotnet_cmd run --no-build --project "$tools_project" -- validate --output "$output_dir"

require_file "${output_dir}/result.json"
