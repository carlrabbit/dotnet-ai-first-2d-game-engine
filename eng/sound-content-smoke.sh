#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/sound-content"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- content validate sounds --output "$out"
require_file "$out/result.json"
