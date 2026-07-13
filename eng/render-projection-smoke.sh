#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="artifacts/smoke/m015/render"
dotnet run --project src/Agentic2D.Tools -- render project --scenario interaction.npc-smoke --tick final --output "$out"
for f in render-result.json render-snapshot.json render-frame.json render-items.jsonl render-commands.jsonl asset-bindings.json render-diagnostics.json; do test -s "$out/$f"; done
grep -q 'static-map-object' "$out/render-items.jsonl"
grep -q 'runtime-entity' "$out/render-items.jsonl"
echo "render-projection-smoke: passed (headless; no raylib initialization)"
