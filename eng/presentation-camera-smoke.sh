#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m021-camera"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- content validate cameras --output "$out/content"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- camera inspect --project . --scenario presentation.camera-follow-shake-smoke --output "$out"
require_file "$out/camera-states.jsonl"
require_file "$out/camera-requests.jsonl"
