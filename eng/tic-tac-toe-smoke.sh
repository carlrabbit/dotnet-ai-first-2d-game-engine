#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
workspace="$repo_root/consumers/autonomous-tic-tac-toe"
./eng/tic-tac-toe-validate.sh
for scenario in workspace-smoke ai-vs-ai-smoke deterministic-random-choice human-takes-x human-takes-o release-control invalid-cell-rejected x-wins o-wins draw round-reset save-during-thinking presentation-smoke generated-sound-linkage; do
  dotnet_cmd run --no-build --project "$workspace/game-src/AutonomousTicTacToe.Game/AutonomousTicTacToe.Game.csproj" -- --scenario "tic-tac-toe.$scenario" --output "$workspace/artifacts/runs/$scenario"
  require_file "$workspace/artifacts/runs/$scenario/tic-tac-toe-result.json"
done
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- geometry preview "$workspace" --output "$workspace/artifacts/geometry"
