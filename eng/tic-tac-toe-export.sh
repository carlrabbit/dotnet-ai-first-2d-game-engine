#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
workspace="$repo_root/consumers/autonomous-tic-tac-toe"
out="$repo_root/artifacts/tic-tac-toe/export"
consumer_project="$workspace/game-src/AutonomousTicTacToe.Game/AutonomousTicTacToe.Game.csproj"
playable_project="$workspace/game-src/AutonomousTicTacToe.Playable/AutonomousTicTacToe.Playable.csproj"
./eng/tic-tac-toe-validate.sh
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project export "$workspace" --target linux-x64 --output "$out/game"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- export validate "$out/game" --output "$out/validate"
"$out/game/agentic2d-game" --headless --scenario tic-tac-toe.linux-export-smoke --output "$out/run"
test -f "$out/game/game/generated/sounds/round-start.wav"

# The generic package proves the workspace export. This second launch proves the
# consumer-owned rule executable has equivalent development and Linux-export state.
dotnet_cmd publish "$consumer_project" --configuration Release --runtime linux-x64 --self-contained true --output "$out/consumer-linux-x64"
dotnet_cmd publish "$playable_project" --configuration Release --runtime linux-x64 --self-contained true --output "$out/playable-linux-x64"
dotnet_cmd run --no-build --project "$consumer_project" -- --scenario tic-tac-toe.linux-export-smoke --seed tic-tac-toe-export-equivalence --output "$out/development-result"
"$out/consumer-linux-x64/AutonomousTicTacToe.Game" --scenario tic-tac-toe.linux-export-smoke --seed tic-tac-toe-export-equivalence --output "$out/linux-export-result"
cmp "$out/development-result/tic-tac-toe-result.json" "$out/linux-export-result/tic-tac-toe-result.json"
development_hash="$(sha256sum "$out/development-result/tic-tac-toe-result.json" | awk '{print $1}')"
export_hash="$(sha256sum "$out/linux-export-result/tic-tac-toe-result.json" | awk '{print $1}')"
printf '{\n  "schema": "agentic2d.tic-tac-toe-development-export-equivalence.v1",\n  "status": "passed",\n  "target": "linux-x64",\n  "scenario": "tic-tac-toe.linux-export-smoke",\n  "seed": "tic-tac-toe-export-equivalence",\n  "comparison": "byte-identical-tic-tac-toe-result-json",\n  "developmentResultSha256": "%s",\n  "exportResultSha256": "%s"\n}\n' "$development_hash" "$export_hash" > "$out/development-export-equivalence.json"
