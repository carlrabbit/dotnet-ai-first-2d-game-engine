#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
workspace="$repo_root/consumers/autonomous-tic-tac-toe"
dotnet_cmd build "$workspace/game-src/AutonomousTicTacToe.Game/AutonomousTicTacToe.Game.csproj"
dotnet_cmd build "$workspace/game-src/AutonomousTicTacToe.Playable/AutonomousTicTacToe.Playable.csproj"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- workspace validate "$workspace" --output "$workspace/artifacts/validation"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project validate "$workspace" --output "$workspace/artifacts/project-validation"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound synthesis validate "$workspace/game-content/sound-synthesis" --output "$workspace/artifacts/sound-validation"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound synthesize "$workspace/game-content/sound-synthesis" --output "$workspace"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound linkage validate "$workspace" --output "$workspace/artifacts/sound-linkage"
test -f "$workspace/game-content/generated/sounds/round-start.wav"
