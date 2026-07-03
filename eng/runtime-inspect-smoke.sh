#!/usr/bin/env bash

source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"

tools_project="src/Agentic2D.Tools/Agentic2D.Tools.csproj"
output_dir="${repo_root}/artifacts/runtime/inspect"

require_file "$tools_project"

dotnet_cmd run --no-build --project "$tools_project" -- runtime inspect --scenario runtime.smoke --map map.smoke --output "$output_dir"

require_file "${output_dir}/result.json"
require_file "${output_dir}/diagnostics.json"
require_file "${output_dir}/runtime-summary.json"
require_file "${output_dir}/entities.json"
require_file "${output_dir}/commands.jsonl"
require_file "${output_dir}/events.jsonl"
require_file "${output_dir}/final-state.json"
require_file "${output_dir}/assertions.json"
require_file "${output_dir}/content-references.json"
