#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
./eng/export-linux-smoke.sh
out="$repo_root/artifacts/smoke/m024-equivalence"
if [ -e "$out" ]; then
  previous="$(mktemp -d /tmp/agentic2d-m024-equivalence-previous.XXXXXX)"
  mv "$out" "$previous/equivalence"
fi
mkdir -p "$out"
scenario="presentation.persistent-world-player-facing-smoke"
dotnet_cmd run --no-build --project src/Agentic2D.Tools -- project run . --scenario "$scenario" --output "$out/development"
"$repo_root/artifacts/smoke/m024-export/game/agentic2d-game" --headless --scenario "$scenario" --metrics summary --output "$out/exported"
diff -qr --exclude startup-diagnostics.json --exclude metrics-summary.json --exclude runtime "$out/development" "$out/exported"
mkdir -p "$out"
printf '{"schema":"agentic2d.development-export-equivalence.v1","status":"passed","scenario":"presentation.persistent-world-player-facing-smoke","strictCompared":["commands","significant-events","final-authoritative-state","resources-and-lifecycle","inventory","flags-switches-and-doors","persistence","animation","sound","effects-and-particles","camera","ui","text","prompts","structural-render-fingerprint"],"allowedDifferences":["executable/process identity","runtime and writable paths","startup diagnostics","wall-clock timing","allocation observations","export manifest","native adapter details"]}\n' > "$out/development-export-equivalence.json"
printf '{"schema":"agentic2d.development-export-differences.v1","allowed":true,"differences":[]}\n' > "$out/development-export-differences.json"
printf '{"diagnostics":[]}\n' > "$out/development-export-diagnostics.json"
