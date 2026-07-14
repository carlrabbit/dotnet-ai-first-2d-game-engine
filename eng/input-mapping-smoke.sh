#!/usr/bin/env bash
set -euo pipefail
source "$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/common.sh"
out="$repo_root/artifacts/input/mapping-smoke"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- input inspect input-sequence.mixed-device-approach-and-interact --input-map input-map.player.default --output "$out"
for f in input-map.json raw-input-samples.jsonl input-action-resolutions.jsonl input-frames.jsonl input-recording.json input-replay-result.json input-diagnostics.json; do require_file "$out/$f"; done
echo "input-mapping-smoke: passed"
