#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/sound-loops"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound project --project . --scenario sound.loop-ownership-smoke --output "$out"
require_file "$out/sound-playback-state.jsonl"
