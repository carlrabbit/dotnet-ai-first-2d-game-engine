#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="artifacts/smoke/m023-metrics"
rm -rf "$out"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- runtime smoke --ticks 8 --metrics per-tick --output "$out"
for file in metrics-summary.json metrics-ticks.jsonl; do require_file "$out/$file"; done
