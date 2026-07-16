#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m021-resumed"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project resume . --save "$repo_root/artifacts/smoke/m021-integrated/save/save-snapshot.json" --output "$out"
require_file "$out/presentation/player-facing-presentation-result.json"
test ! -s "$out/presentation/effect-requests.jsonl"
test ! -s "$out/presentation/particle-instances.jsonl"
