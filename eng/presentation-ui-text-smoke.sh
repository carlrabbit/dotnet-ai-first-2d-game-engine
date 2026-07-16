#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m021-ui-text"
for scope in ui text fonts; do dotnet_cmd run --no-build --project src/Agentic2D.Tools -- content validate "$scope" --output "$out/$scope"; done
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- ui inspect ui.player-hud --project . --scenario presentation.ui-hud-smoke --output "$out/presentation"
require_file "$out/presentation/ui-layout.jsonl"
require_file "$out/presentation/text-commands.jsonl"
