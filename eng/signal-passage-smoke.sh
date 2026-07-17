#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
workspace="$repo_root/consumers/signal-passage"
./eng/signal-passage-validate.sh
dotnet_cmd run --no-build --project "$workspace/game-src/SignalPassage.Game/SignalPassage.Game.csproj" -- --output "$workspace/artifacts/journey"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run "$workspace" --scenario signal-passage.geometry-presentation-smoke --output "$workspace/artifacts/runs/geometry"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run "$workspace" --scenario signal-passage.complete-journey --output "$workspace/artifacts/runs/complete-journey"
test -f "$workspace/artifacts/journey/complete-journey.json"
test -f "$workspace/artifacts/journey/save.json"
