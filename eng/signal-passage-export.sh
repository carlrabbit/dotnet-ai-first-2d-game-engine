#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
workspace="$repo_root/consumers/signal-passage"
./eng/signal-passage-validate.sh
out="$repo_root/artifacts/signal-passage/export"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project export "$workspace" --target linux-x64 --output "$out/game"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- export validate "$out/game" --output "$out/validate"
"$out/game/agentic2d-game" --headless --scenario signal-passage.linux-export-smoke --output "$out/run"
test -f "$out/game/game/generated/sounds/objective-completed.wav"
