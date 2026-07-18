#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

workspace="$repo_root/consumers/autonomous-tic-tac-toe"
if [ "${1:-}" != "--verify-autonomous-rounds" ] && [ -z "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]; then
  fail "tic-tac-toe play requires DISPLAY or WAYLAND_DISPLAY"
fi
dotnet_cmd run --project "$workspace/game-src/AutonomousTicTacToe.Playable/AutonomousTicTacToe.Playable.csproj" -- "$@"
