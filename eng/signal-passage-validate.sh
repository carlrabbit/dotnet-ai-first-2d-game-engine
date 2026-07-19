#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
workspace="$repo_root/consumers/signal-passage"
dotnet_cmd build "$workspace/game-src/SignalPassage.Game/SignalPassage.Game.csproj"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- workspace validate "$workspace" --output "$workspace/artifacts/validation"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project validate "$workspace" --output "$workspace/artifacts/project-validation"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound synthesis validate "$workspace/game-content/sound-synthesis" --output "$workspace/artifacts/sound-validation"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound synthesize "$workspace/game-content/sound-synthesis" --output "$workspace"
test -f "$workspace/game-content/generated/sounds/objective-completed.wav"
