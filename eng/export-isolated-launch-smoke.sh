#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
./eng/export-linux-smoke.sh
out="$repo_root/artifacts/smoke/m024-isolated-launch"
isolated="$(mktemp -d /tmp/agentic2d-m024-isolated.XXXXXX)"
cp -a "$repo_root/artifacts/smoke/m024-export/game" "$isolated/game"
"$isolated/game/agentic2d-game" --headless --scenario runtime.smoke --metrics summary --output "$out/run"
require_file "$out/run/run-manifest.json"
require_file "$out/run/metrics-summary.json"
mkdir -p "$out"
printf '{"schema":"agentic2d.isolated-launch.v1","status":"passed","launch":"direct-executable","network":"not-required"}\n' > "$out/isolated-launch-result.json"
cp "$out/isolated-launch-result.json" "$out/isolated-launch-process.json"
cp "$out/run/run-manifest.json" "$out/isolated-launch-run-manifest.json"
cp "$out/run/startup-diagnostics.json" "$out/isolated-launch-diagnostics.json"
