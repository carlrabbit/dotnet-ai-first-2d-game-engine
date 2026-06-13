#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
output_dir="${repo_root}/artifacts/cli/runtime-smoke"

require_file "$tools_project"

dotnet_cmd run --no-build --project "$tools_project" -- --help
dotnet_cmd run --no-build --project "$tools_project" -- --version
dotnet_cmd run --no-build --project "$tools_project" -- runtime smoke --output "$output_dir"

require_file "${output_dir}/result.json"
