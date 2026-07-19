#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
base="$repo_root/artifacts/sound-linkage/M026"
rm -rf "$base"
./eng/signal-passage-validate.sh
./eng/tic-tac-toe-validate.sh
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound linkage validate "$repo_root/consumers/signal-passage" --output "$base/signal-passage"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound linkage inspect "$repo_root/consumers/autonomous-tic-tac-toe" --output "$base/tic-tac-toe"
require_file "$base/signal-passage/generated-sound-linkage-report.json"
require_file "$base/tic-tac-toe/generated-sound-linkage-report.md"
