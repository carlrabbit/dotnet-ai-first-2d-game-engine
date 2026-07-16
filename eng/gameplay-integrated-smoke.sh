#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m019-integrated"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run . --scenario gameplay.sound-damage-collection-lifecycle-smoke --output "$out"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- run inspect "$out" --output "$out/inspection"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- run review "$out" --output "$out/review"
require_file "$out/sound/sound-result.json"
require_file "$out/gameplay/gameplay-result.json"
