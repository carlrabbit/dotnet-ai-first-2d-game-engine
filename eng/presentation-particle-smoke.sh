#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m021-particles"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- presentation inspect --project . --scenario presentation.particle-collection-smoke --output "$out"
require_file "$out/particle-emitters.json"
require_file "$out/particle-instances.jsonl"
require_file "$out/particle-samples.jsonl"
