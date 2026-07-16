#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/sound-marker-cue"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound project --project . --scenario sound.marker-cue-smoke --output "$out"
require_file "$out/sound-command-frames.jsonl"
