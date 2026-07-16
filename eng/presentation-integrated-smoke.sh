#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m021-integrated"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run . --scenario presentation.persistent-world-player-facing-smoke --output "$out"
require_file "$out/presentation/player-facing-presentation-result.json"
require_file "$out/presentation/presentation-composition.jsonl"
