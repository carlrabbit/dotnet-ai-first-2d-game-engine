#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"

base="$repo_root/artifacts/sound-linkage/M027"
mkdir -p "$base"
if [[ ! -f "$repo_root/artifacts/signal-passage/export/game/agentic2d.export.json" ]]; then
  ./eng/signal-passage-export.sh
fi
if [[ ! -f "$repo_root/artifacts/tic-tac-toe/export/game/agentic2d.export.json" ]]; then
  ./eng/tic-tac-toe-export.sh
fi
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound linkage review-pack "$repo_root/consumers/signal-passage" --export "$repo_root/artifacts/signal-passage/export/game" --output "$base/signal-passage"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- sound linkage review-pack "$repo_root/consumers/autonomous-tic-tac-toe" --export "$repo_root/artifacts/tic-tac-toe/export/game" --output "$base/tic-tac-toe"
for consumer in signal-passage tic-tac-toe; do
  require_file "$base/$consumer/manifest.json"
  require_file "$base/$consumer/index.md"
  require_file "$base/$consumer/generated-sound-linkage.json"
  require_file "$base/$consumer/generated-sound-provenance.json"
  require_file "$base/$consumer/generated-sound-linkage-report.json"
  jq -e '.schema == "agentic2d.generated-sound-review-pack.v1"' "$base/$consumer/manifest.json" >/dev/null
done
