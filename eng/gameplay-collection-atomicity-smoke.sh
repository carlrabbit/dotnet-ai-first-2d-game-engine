#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/gameplay-collection"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- gameplay inspect --project . --scenario gameplay.collection-atomicity-smoke --output "$out"
require_file "$out/collection-resolutions.jsonl"
require_file "$out/inventory-transitions.jsonl"
