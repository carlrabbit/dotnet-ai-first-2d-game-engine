#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

base="$repo_root/artifacts/geometry/M027"
mkdir -p "$base"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- geometry review-pack "$repo_root/consumers/signal-passage" --output "$base/signal-passage"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- geometry review-pack "$repo_root/consumers/autonomous-tic-tac-toe" --output "$base/tic-tac-toe"
for consumer in signal-passage tic-tac-toe; do
  require_file "$base/$consumer/manifest.json"
  require_file "$base/$consumer/index.md"
  require_file "$base/$consumer/geometry-inspection.json"
  require_file "$base/$consumer/geometry-preview.json"
  require_file "$base/$consumer/geometry-diagnostics.json"
  require_file "$base/$consumer/geometry-projection-comparison.json"
  require_file "$base/$consumer/capture-status.json"
  jq -e '.schema == "agentic2d.geometry-review-pack.v1"' "$base/$consumer/manifest.json" >/dev/null
done
