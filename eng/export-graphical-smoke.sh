#!/usr/bin/env bash
set -euo pipefail
source "$(dirname "$0")/common.sh"
out="$repo_root/artifacts/smoke/m024-graphical"
mkdir -p "$out"
if [ -z "${DISPLAY:-}${WAYLAND_DISPLAY:-}" ]; then
  printf '{"schema":"agentic2d.export-graphical-review.v1","status":"skipped","reason":"no graphical session available"}\n' > "$out/graphical-review.json"
  exit 0
fi
./eng/export-linux-smoke.sh
"$repo_root/artifacts/smoke/m024-export/game/agentic2d-game" --scenario runtime.smoke --auto-close-after 120 --output "$out/run"
require_file "$out/run/startup-diagnostics.json"
printf '{"schema":"agentic2d.export-graphical-review.v1","status":"passed","adapter":"raylib-isolated-adapter","scenario":"runtime.smoke","validated":"render startup, clean shutdown, writable diagnostics"}\n' > "$out/graphical-review.json"
