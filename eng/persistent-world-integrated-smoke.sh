#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m020-persistent-world"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run . --scenario gameplay.persistent-world-resume-smoke --output "$out"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- run inspect "$out" --output "$out/inspection"
require_file "$out/persistent-world/persistent-world-result.json"
require_file "$out/save/save-snapshot.json"

