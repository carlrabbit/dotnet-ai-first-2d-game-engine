#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
dotnet_cmd run --project src/Agentic2D.Tools -- animation inspect animation-definition.player.basic --output artifacts/animation/player-basic
require_file artifacts/animation/player-basic/compiled-animation.json
dotnet_cmd run --project src/Agentic2D.Tools -- animation project --scenario animation-player-locomotion-smoke --output artifacts/animation/player-locomotion
require_file artifacts/animation/player-locomotion/animation-samples.jsonl
require_file artifacts/animation/player-locomotion/animation-playback.jsonl
echo "animation-sampling-smoke: passed"
