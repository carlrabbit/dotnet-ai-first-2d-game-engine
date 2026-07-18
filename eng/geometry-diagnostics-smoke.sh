#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
base="$repo_root/artifacts/geometry/M026"
rm -rf "$base"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- geometry inspect "$repo_root/consumers/signal-passage" --output "$base/signal-passage"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- geometry preview "$repo_root/consumers/autonomous-tic-tac-toe" --output "$base/tic-tac-toe"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- geometry inspect "$repo_root/tests/fixtures/geometry/all-supported-shapes.json" --output "$base/all-supported-shapes"
require_file "$base/signal-passage/geometry-inspection.json"
require_file "$base/tic-tac-toe/geometry-projection-comparison.json"
require_file "$base/all-supported-shapes/geometry-inspection.json"
jq -e '[.parts[].shapeKind] | sort == ["circle", "diamond", "line", "rectangle", "regular-polygon", "ring", "triangle"]' "$base/all-supported-shapes/geometry-inspection.json" >/dev/null
