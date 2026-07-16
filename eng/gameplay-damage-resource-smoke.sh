#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/gameplay-damage"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- gameplay inspect --project . --scenario gameplay.damage-resource-smoke --output "$out"
require_file "$out/damage-resolutions.jsonl"
